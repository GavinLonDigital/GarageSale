using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using GarageSale.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using GarageSale.Configuration;
using GarageSale.Data;
using GarageSale.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Xunit;

namespace GarageSale.Tests;

public class StripePaymentTests
{
    private static ClaimsPrincipal User(string id = "buyer") => new(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.NameIdentifier, id) }, "test"));
    private static StripeOptions Settings() => new()
    {
        SecretKey = "sk_test_synthetic", WebhookSecret = "whsec_synthetic", AppBaseUrl = "http://localhost:8080"
    };

    [Fact]
    public async Task MissingSettingsDoNotPreventServiceCreationButFailOnCheckout()
    {
        await using var fixture = await Fixture.Create();
        var service = fixture.Service(new StripeOptions());
        var error = await Assert.ThrowsAsync<CheckoutException>(() => service.CheckoutAsync(User()));
        Assert.Contains("STRIPE_SECRET_KEY", error.Message);
        Assert.Equal(0, fixture.Gateway.Calls);
    }

    [Theory]
    [InlineData("sk_live_synthetic")]
    [InlineData("pk_test_synthetic")]
    public void RejectsNonSandboxSecretKeys(string key)
    {
        var settings = Settings(); settings.SecretKey = key;
        Assert.Throws<CheckoutException>(() => settings.ValidateCheckout());
    }

    [Theory]
    [InlineData("http://garagesale:8080")]
    [InlineData("http://192.168.1.1:8080")]
    [InlineData("https://example.com/path")]
    [InlineData("https://user:password@example.com")]
    [InlineData("https://example.com?redirect=bad")]
    public void RejectsUntrustedBaseUrls(string url)
    {
        var settings = Settings(); settings.AppBaseUrl = url;
        Assert.Throws<CheckoutException>(() => settings.ValidateCheckout());
    }

    [Fact]
    public async Task CalculatesFromDatabaseAndReusesPersistedAttempt()
    {
        await using var f = await Fixture.Create();
        var result = await f.Service().CheckoutAsync(User());
        Assert.Equal(1234, f.Gateway.Request!.LineItems.Single().PriceData.UnitAmount);
        Assert.Equal(1, f.Gateway.Request.LineItems.Single().Quantity);
        Assert.Null(f.Gateway.Request.PaymentMethodTypes);
        Assert.Null(f.Gateway.Request.ExcludedPaymentMethodTypes);
        Assert.Equal("test+location_US@example.com", f.Gateway.Request.CustomerEmail);
        Assert.Equal($"http://localhost:8080/checkout/success/{result.OrderId}", f.Gateway.Request.SuccessUrl);
        await f.Service().CheckoutAsync(User());
        Assert.Equal(1, f.Gateway.Calls);
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(ProductSalesStatus.Reserved, (await db.Products.SingleAsync()).SalesStatus);
        Assert.Equal("buyer@example.com", (await db.Users.SingleAsync(u => u.Id == "buyer")).Email);
        Assert.Equal(1, await db.ShoppingCartItems.CountAsync());
    }

    [Fact]
    public async Task RequiresAuthenticationAndChecksOrderOwnership()
    {
        await using var f = await Fixture.Create();
        await Assert.ThrowsAsync<CheckoutException>(() => f.Service().CheckoutAsync(new ClaimsPrincipal()));
        var result = await f.Service().CheckoutAsync(User());
        Assert.Null(await f.Service().GetStatusAsync(User("other"), result.OrderId));
        Assert.NotNull(await f.Service().GetStatusAsync(User(), result.OrderId));
    }

    [Fact]
    public async Task UnpaidCompletionHoldsStockAndDuplicateSuccessFulfilsOnce()
    {
        await using var f = await Fixture.Create();
        var result = await f.Service().CheckoutAsync(User());
        f.Gateway.Session.Status = "complete";
        await f.Service().ProcessEventAsync(f.Event("checkout.session.completed"));
        Assert.Equal(OrderStatus.Pending, (await f.Service().GetStatusAsync(User(), result.OrderId))!.Status);
        await using (var db = f.Factory.CreateDbContext()) Assert.Equal(1, await db.InventoryReservations.CountAsync());
        f.Gateway.Session.PaymentStatus = "paid";
        f.Gateway.Session.PaymentIntentId = "pi_synthetic";
        var success = f.Event("checkout.session.async_payment_succeeded");
        await f.Service().ProcessEventAsync(success);
        await f.Service().ProcessEventAsync(success);
        await f.Service().ProcessEventAsync(f.Event("checkout.session.expired"));
        await f.Service().ProcessEventAsync(f.Event("checkout.session.async_payment_failed"));
        Assert.Equal(OrderStatus.Paid, (await f.Service().GetStatusAsync(User(), result.OrderId))!.Status);
        await using var state = f.Factory.CreateDbContext();
        Assert.Equal(ProductSalesStatus.Sold, (await state.Products.SingleAsync()).SalesStatus);
        Assert.Empty(await state.InventoryReservations.ToListAsync());
        Assert.Empty(await state.ShoppingCartItems.ToListAsync());
        Assert.Equal(4, await state.StripeWebhookReceipts.CountAsync());
        Assert.Equal("pi_synthetic", (await state.PaymentAttempts.SingleAsync()).PaymentIntentId);
    }

    [Theory]
    [InlineData("checkout.session.expired", "expired", OrderStatus.Expired)]
    [InlineData("checkout.session.async_payment_failed", "complete", OrderStatus.Failed)]
    public async Task TerminalUnpaidEventsReleaseStockAndPreserveCart(string type, string status, OrderStatus expected)
    {
        await using var f = await Fixture.Create();
        var result = await f.Service().CheckoutAsync(User());
        f.Gateway.Session.Status = status;
        await f.Service().ProcessEventAsync(f.Event(type));
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(expected, (await db.Orders.SingleAsync()).Status);
        Assert.Equal(ProductSalesStatus.Available, (await db.Products.SingleAsync()).SalesStatus);
        Assert.Equal(1, await db.ShoppingCartItems.CountAsync());
        Assert.Null((await db.PaymentAttempts.SingleAsync()).PendingUserId);
    }

    [Fact]
    public async Task RejectsLiveEventsAndIncorrectAmountsWithoutChangingState()
    {
        await using var f = await Fixture.Create();
        await f.Service().CheckoutAsync(User());
        var live = f.Event("checkout.session.completed"); live.Livemode = true;
        await Assert.ThrowsAsync<CheckoutException>(() => f.Service().ProcessEventAsync(live));
        f.Gateway.Session.AmountTotal++;
        await Assert.ThrowsAsync<CheckoutException>(() => f.Service().ProcessEventAsync(f.Event("checkout.session.completed")));
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(OrderStatus.Pending, (await db.Orders.SingleAsync()).Status);
        Assert.Empty(await db.StripeWebhookReceipts.ToListAsync());
    }

    [Fact]
    public async Task LostCreateResponseRetainsStockAndRetriesIdenticalRequest()
    {
        await using var f = await Fixture.Create();
        f.Gateway.TimeoutOnce = true;
        await Assert.ThrowsAsync<CheckoutException>(() => f.Service().CheckoutAsync(User()));
        var firstKey = f.Gateway.Key;
        await f.Service().CheckoutAsync(User());
        Assert.Equal(firstKey, f.Gateway.Key);
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(1, await db.InventoryReservations.CountAsync());
    }

    [Fact]
    public async Task CompetingBuyersCannotReserveSameUniqueProduct()
    {
        await using var f = await Fixture.Create();
        await using (var db = f.Factory.CreateDbContext())
        {
            db.ShoppingCartItems.Add(new ShoppingCartItems { UserId = "other", SellerId = "seller", ProductId = 1, Price = 1 });
            await db.SaveChangesAsync();
        }
        // Separate contexts race through the actual conditional update and unique keys.
        var results = await Task.WhenAll(new[] { "buyer", "other" }.Select(id => Task.Run(async () =>
        {
            try { await f.Service().CheckoutAsync(User(id)); return true; }
            catch (Exception ex) when (ex is CheckoutException or SqliteException or SqlException or DbUpdateException) { return false; }
        })));
        Assert.Single(results, success => success);
        await using var state = f.Factory.CreateDbContext();
        Assert.Equal(1, await state.Orders.CountAsync());
        Assert.Equal(1, await state.InventoryReservations.CountAsync());
    }

    [Fact]
    public async Task ConcurrentWebhookDeliveryCommitsOnceWithRetry()
    {
        await using var f = await Fixture.Create();
        await f.Service().CheckoutAsync(User());
        f.Gateway.Session.Status = "complete"; f.Gateway.Session.PaymentStatus = "paid";
        var evt = f.Event("checkout.session.completed");
        await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
        {
            try { await f.Service().ProcessEventAsync(evt); }
            catch (Exception ex) when (ex is SqliteException or SqlException or DbUpdateException || ex.InnerException is SqlException { Number: 1205 }) { }
        })));
        await f.Service().ProcessEventAsync(evt); // Stripe's retry after a concurrency rollback.
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(1, await db.StripeWebhookReceipts.CountAsync());
        Assert.Equal(OrderStatus.Paid, (await db.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task WebhookEndpointVerifiesRawBodyWithoutLogin()
    {
        await using var f = await Fixture.Create();
        await f.Service().CheckoutAsync(User());
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IDbContextFactory<ApplicationDbContext>>(f.Factory);
        builder.Services.AddSingleton<IStripeCheckoutGateway>(f.Gateway);
        builder.Services.Configure<StripeOptions>(settings =>
        {
            var valid = Settings();
            settings.SecretKey = valid.SecretKey; settings.WebhookSecret = valid.WebhookSecret;
            settings.AppBaseUrl = valid.AppBaseUrl;
        });
        builder.Services.AddScoped<StripePaymentService>();
        await using var app = builder.Build();
        app.MapStripeWebhook();
        await app.StartAsync();
        var client = app.GetTestClient();
        var payload = Newtonsoft.Json.JsonConvert.SerializeObject(new Event
        {
            Id = "evt_signed", Object = "event", Type = "checkout.session.completed", ApiVersion = StripeConfiguration.ApiVersion,
            Data = new EventData { Object = f.Gateway.Session }
        });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var digest = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Settings().WebhookSecret),
            Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant();
        var unsigned = await client.PostAsync("/api/stripe/webhook", new StringContent(payload));
        Assert.Equal(HttpStatusCode.BadRequest, unsigned.StatusCode);
        using var signed = new HttpRequestMessage(HttpMethod.Post, "/api/stripe/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        signed.Headers.Add("Stripe-Signature", $"t={timestamp},v1={digest}");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(signed)).StatusCode);
        using var modified = new HttpRequestMessage(HttpMethod.Post, "/api/stripe/webhook")
        {
            Content = new StringContent(payload + " ")
        };
        modified.Headers.Add("Stripe-Signature", $"t={timestamp},v1={digest}");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(modified)).StatusCode);
    }

    [Fact]
    public async Task DeclinedCreateReleasesReservationWithoutClearingCart()
    {
        await using var f = await Fixture.Create();
        f.Gateway.RejectCreate = true;
        await Assert.ThrowsAsync<CheckoutException>(() => f.Service().CheckoutAsync(User()));
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(OrderStatus.Failed, (await db.Orders.SingleAsync()).Status);
        Assert.Equal(ProductSalesStatus.Available, (await db.Products.SingleAsync()).SalesStatus);
        Assert.Equal(1, await db.ShoppingCartItems.CountAsync());
        Assert.Empty(await db.InventoryReservations.ToListAsync());
    }

    [Fact]
    public async Task StatusReadsDoNotConfirmOrCancelAndReconciliationRecoversMissedExpiry()
    {
        await using var f = await Fixture.Create();
        var order = await f.Service().CheckoutAsync(User());
        f.Gateway.Session.Status = "expired";
        Assert.Equal(OrderStatus.Pending, (await f.Service().GetStatusAsync(User(), order.OrderId))!.Status);
        await f.Service().ReconcilePendingAsync(default);
        Assert.Equal(OrderStatus.Expired, (await f.Service().GetStatusAsync(User(), order.OrderId))!.Status);
    }

    [Fact]
    public async Task DelayedPaymentKeepsStockBeyondSessionExpiryTimestamp()
    {
        await using var f = await Fixture.Create();
        var result = await f.Service().CheckoutAsync(User());
        f.Gateway.Session.Status = "complete";
        f.Gateway.Session.ExpiresAt = DateTime.UtcNow.AddDays(-2);
        await f.Service().ReconcilePendingAsync(default);
        Assert.Equal(OrderStatus.Pending, (await f.Service().GetStatusAsync(User(), result.OrderId))!.Status);
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(1, await db.InventoryReservations.CountAsync());
        Assert.Equal(ProductSalesStatus.Reserved, (await db.Products.SingleAsync()).SalesStatus);
    }

    [Fact]
    public async Task AncientUnknownCreateCannotProduceAnotherSession()
    {
        await using var f = await Fixture.Create();
        f.Gateway.TimeoutOnce = true;
        await Assert.ThrowsAsync<CheckoutException>(() => f.Service().CheckoutAsync(User()));
        await using (var db = f.Factory.CreateDbContext())
        {
            var attempt = await db.PaymentAttempts.SingleAsync();
            attempt.CreatedAt = DateTime.UtcNow.AddDays(-2);
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<CheckoutException>(() => f.Service().CheckoutAsync(User()));
        Assert.Equal(1, f.Gateway.Calls);
        await using var state = f.Factory.CreateDbContext();
        Assert.Equal(1, await state.InventoryReservations.CountAsync());
    }

    [Fact]
    public async Task FailedInventoryPersistenceDoesNotAcknowledgeOrMarkPaid()
    {
        await using var f = await Fixture.Create();
        await f.Service().CheckoutAsync(User());
        await using (var db = f.Factory.CreateDbContext())
            await db.InventoryReservations.ExecuteDeleteAsync(); // Simulate a broken inventory invariant.
        f.Gateway.Session.PaymentStatus = "paid";
        f.Gateway.Session.Status = "complete";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service().ProcessEventAsync(f.Event("checkout.session.completed")));
        await using var state = f.Factory.CreateDbContext();
        Assert.Equal(OrderStatus.Pending, (await state.Orders.SingleAsync()).Status);
        Assert.Equal(0, await state.StripeWebhookReceipts.CountAsync());
        Assert.Equal(1, await state.ShoppingCartItems.CountAsync());
    }

    [Fact]
    public async Task RepeatedConcurrentClicksCreateOnlyOneOrderAndSession()
    {
        await using var f = await Fixture.Create();
        await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
        {
            try { await f.Service().CheckoutAsync(User()); }
            catch (Exception ex) when (ex is CheckoutException or SqliteException or DbUpdateException ||
                                       ex.InnerException is SqlException { Number: 1205 }) { }
        })));
        await f.Service().CheckoutAsync(User());
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(1, await db.PaymentAttempts.CountAsync());
        Assert.Equal(1, await db.InventoryReservations.CountAsync());
        // Real Stripe deduplicates calls with this persisted key; a repeated call is safe.
        Assert.Equal($"garagesale-checkout-{(await db.PaymentAttempts.SingleAsync()).Id}", f.Gateway.Key);
    }

    private sealed class TestDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // SQLite has no SQL Server-generated rowversion or GETUTCDATE().
            builder.Entity<Order>().Property(o => o.RowVersion).IsRowVersion().HasDefaultValue(new byte[] { 1 });
            builder.Entity<Order>().Property(o => o.DateAdded).HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Entity<Order>().Property(o => o.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Entity<ShoppingCartItems>().Property(c => c.DateAdded).HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
    private sealed class Factory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() =>
            options.Extensions.Any(e => e.GetType().Name.Contains("SqlServer"))
                ? new ApplicationDbContext(options) : new TestDbContext(options);
    }
    private sealed class FakeStripe : IStripeCheckoutGateway
    {
        public Session Session { get; private set; } = null!;
        public SessionCreateOptions? Request;
        public string? Key;
        public int Calls;
        public bool TimeoutOnce;
        public bool RejectCreate;
        public Task<Session> CreateAsync(SessionCreateOptions options, string key, CancellationToken ct)
        {
            Calls++; Request = options; Key = key;
            if (RejectCreate) throw new StripeException(System.Net.HttpStatusCode.BadRequest,
                new StripeError { Type = "invalid_request_error" }, "Synthetic validation failure");
            Session ??= new Session
            {
                Id = "cs_test_synthetic", Object = "checkout.session", Mode = "payment", Status = "open", PaymentStatus = "unpaid",
                Currency = "usd", AmountTotal = options.LineItems.Sum(l => l.PriceData.UnitAmount * l.Quantity),
                ClientReferenceId = options.ClientReferenceId, Metadata = options.Metadata,
                Url = "https://checkout.stripe.com/synthetic", ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
            if (TimeoutOnce) { TimeoutOnce = false; throw new HttpRequestException("Simulated lost response"); }
            return Task.FromResult(Session);
        }
        public Task<Session> GetAsync(string id, CancellationToken ct) => Task.FromResult(Session);
    }
    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string path = Path.Combine(Path.GetTempPath(), $"garagesale-test-{Guid.NewGuid()}.db");
        public Factory Factory { get; private set; } = null!;
        public FakeStripe Gateway { get; } = new();
        public StripePaymentService Service(StripeOptions? settings = null) => new(Factory, Gateway,
            Options.Create(settings ?? Settings()), NullLogger<StripePaymentService>.Instance);
        public Event Event(string type) => new()
        {
            Id = $"evt_{Guid.NewGuid():N}", Type = type, Data = new EventData { Object = Gateway.Session }
        };
        public static async Task<Fixture> Create()
        {
            var f = new Fixture();
            var testConnection = Environment.GetEnvironmentVariable("GARAGESALE_TEST_SQLSERVER");
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (string.IsNullOrEmpty(testConnection))
                dbOptions.UseSqlite($"Data Source={f.path};Pooling=False;Default Timeout=5");
            else
            {
                // Always create a uniquely named disposable test database, never use an existing one.
                var connection = new SqlConnectionStringBuilder(testConnection)
                {
                    InitialCatalog = $"GarageSaleStripeTests_{Guid.NewGuid():N}", Pooling = false
                };
                dbOptions.UseSqlServer(connection.ConnectionString);
            }
            f.Factory = new Factory(dbOptions.Options);
            await using var db = f.Factory.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            db.Users.AddRange(new ApplicationUser { Id = "buyer", Email = "buyer@example.com" },
                new ApplicationUser { Id = "other" }, new ApplicationUser { Id = "seller" });
            db.Products.Add(new Products { ProductName = "Unique lamp", Price = 1234,
                CategoryId = 1, ConditionRatingId = 1, SellerId = "seller" });
            await db.SaveChangesAsync(); // Generate the identity value (one product in this fresh database).
            // Deliberately stale/client-originated cart price must never be charged.
            db.ShoppingCartItems.Add(new ShoppingCartItems { UserId = "buyer", SellerId = "seller", ProductId = 1, Price = 1 });
            await db.SaveChangesAsync();
            return f;
        }
        public async ValueTask DisposeAsync()
        {
            await using var db = Factory.CreateDbContext();
            await db.Database.EnsureDeletedAsync();
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
}
