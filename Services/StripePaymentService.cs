using System.Data;
using System.Security.Claims;
using System.Text.Json;
using GarageSale.Configuration;
using GarageSale.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GarageSale.Services;

public record CheckoutResult(int OrderId, string Url);
public record PaymentStatus(int OrderId, OrderStatus Status, long Amount);

public class StripePaymentService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IStripeCheckoutGateway stripe,
    IOptions<StripeOptions> options,
    ILogger<StripePaymentService> logger)
{
    private static string Buyer(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true && user.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } id
            ? id : throw new CheckoutException("Sign in before checking out.");

    public async Task<CheckoutResult> CheckoutAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var userId = Buyer(user);
        var origin = options.Value.ValidateCheckout();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var attempt = await db.PaymentAttempts.AsNoTracking().Include(p => p.Order)
            .SingleOrDefaultAsync(p => p.PendingUserId == userId, ct);
        if (attempt is null)
        {
            // Conditional stock updates and unique database keys arbitrate competing buyers/clicks.
            // No network request happens until this transaction has committed.
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var products = await db.ShoppingCartItems.Where(c => c.UserId == userId)
                .Select(c => c.Product).Distinct().OrderBy(p => p.Id).ToListAsync(ct);
            if (products.Count == 0) throw new CheckoutException("Your cart is empty.");
            if (products.Count > 100) throw new CheckoutException("Checkout supports up to 100 unique items.");
            if (products.Any(p => p.Price <= 0)) throw new CheckoutException("An item has an invalid price. Please contact the store.");
            var amount = products.Sum(p => checked((long)p.Price)); // Database prices are already USD cents.
            if (amount < 50 || amount > 99_999_999) throw new CheckoutException("The USD order total must be between $0.50 and $999,999.99.");
            var order = new Order { UserId = userId };
            order.OrderItems = products.Select(p => new OrderItem
            {
                ProductId = p.Id, SellerId = p.SellerId, Price = p.Price
            }).ToList();
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            foreach (var product in products)
            {
                var claimed = await db.Products.Where(p => p.Id == product.Id && p.SalesStatus == ProductSalesStatus.Available)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.SalesStatus, ProductSalesStatus.Reserved), ct);
                if (claimed != 1) throw new CheckoutException("An item is sold or reserved by another checkout. Please review your cart.");
                db.InventoryReservations.Add(new InventoryReservation { ProductId = product.Id, OrderId = order.Id });
            }
            attempt = new PaymentAttempt { OrderId = order.Id, Order = order, PendingUserId = userId, Amount = amount };
            var request = new SessionCreateOptions
            {
                Mode = "payment",
                ClientReferenceId = order.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Metadata = new() { ["payment_attempt_id"] = attempt.Id.ToString() },
                SuccessUrl = $"{origin}/checkout/success/{order.Id}",
                CancelUrl = $"{origin}/checkout/cancel/{order.Id}",
                CustomerEmail = "test+location_US@example.com", // Sandbox location simulation; never update the user's profile.
                BillingAddressCollection = "required",
                AdaptivePricing = new SessionAdaptivePricingOptions { Enabled = false },
                // Stripe's default 24-hour expiry is fixed by Stripe at creation. Retrying the
                // identical request remains possible even when a create response was lost.
                // Payment methods stay Dashboard-managed, including delayed methods.
                LineItems = products.Select(p => new SessionLineItemOptions
                {
                    Quantity = 1, // Each database product represents one unique garage-sale item.
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd", UnitAmount = p.Price,
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = p.ProductName }
                    }
                }).ToList()
            };
            attempt.RequestJson = JsonSerializer.Serialize(request);
            db.PaymentAttempts.Add(attempt);
            try
            {
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new CheckoutException("Another checkout is being created. Please try again.");
            }
        }
        var session = await EnsureSessionAsync(attempt, ct);
        // Reconcile before redirecting: the existing session may have completed or expired.
        await ApplySessionAsync(attempt.Id, session, null, null, ct);
        if (session.Status != "open" || string.IsNullOrWhiteSpace(session.Url))
            return new CheckoutResult(attempt.OrderId, $"{origin}/checkout/success/{attempt.OrderId}");
        if (!Uri.TryCreate(session.Url, UriKind.Absolute, out var url) || url.Scheme != "https")
            throw new CheckoutException("Stripe returned an invalid checkout URL.");
        return new CheckoutResult(attempt.OrderId, session.Url);
    }

    private async Task<Session> EnsureSessionAsync(PaymentAttempt attempt, CancellationToken ct)
    {
        try
        {
            if (attempt.SessionId is not null) return await stripe.GetAsync(attempt.SessionId, ct);
            // Stripe may prune idempotency keys after 24h. Never create a second session
            // for an ambiguous old request: retain stock and ask for manual reconciliation.
            if (attempt.CreatedAt < DateTime.UtcNow.AddHours(-23))
                throw new CheckoutException("This checkout needs store assistance to reconcile its payment. Its items remain reserved.");
            var request = JsonSerializer.Deserialize<SessionCreateOptions>(attempt.RequestJson)!;
            var session = await stripe.CreateAsync(request, $"garagesale-checkout-{attempt.Id}", ct);
            ValidateSession(attempt, session);
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            // A webhook may have stored the same ID before this response arrived.
            await db.PaymentAttempts.Where(p => p.Id == attempt.Id && p.SessionId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.SessionId, session.Id)
                    .SetProperty(p => p.SessionExpiresAt, session.ExpiresAt), ct);
            return session;
        }
        catch (StripeException ex) when (ex.StripeError?.Type == "invalid_request_error" &&
                                          ex.StripeError.Code != "idempotency_key_in_use")
        {
            // An explicit validation rejection did not create a payable session.
            await FailCreationAsync(attempt.Id, ct);
            logger.LogWarning("Stripe rejected checkout attempt {AttemptId}", attempt.Id);
            throw new CheckoutException("Stripe rejected this checkout. Check your sandbox settings and try again.");
        }
        catch (Exception ex) when (ex is StripeException or HttpRequestException or TaskCanceledException)
        {
            // An uncertain network result must keep its reservation and idempotency key.
            logger.LogWarning("Stripe request unavailable for attempt {AttemptId}", attempt.Id);
            throw new CheckoutException("Stripe is temporarily unavailable. Your checkout is saved; retry to resume it safely.");
        }
    }

    private static void ValidateSession(PaymentAttempt attempt, Session session)
    {
        if (session.Livemode || session.Mode != "payment" || session.AmountTotal != attempt.Amount ||
            session.Currency != attempt.Currency || session.ClientReferenceId != attempt.OrderId.ToString(System.Globalization.CultureInfo.InvariantCulture) ||
            session.Metadata?.GetValueOrDefault("payment_attempt_id") != attempt.Id.ToString() ||
            (attempt.SessionId is not null && attempt.SessionId != session.Id))
            throw new CheckoutException("Stripe session did not match the saved sandbox order.");
    }

    public async Task<PaymentStatus?> GetStatusAsync(ClaimsPrincipal user, int orderId, CancellationToken ct = default)
    {
        var userId = Buyer(user);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.PaymentAttempts.Where(p => p.OrderId == orderId && p.Order.UserId == userId)
            .Select(p => new PaymentStatus(p.OrderId, p.Order.Status, p.Amount)).SingleOrDefaultAsync(ct);
    }

    public async Task ProcessEventAsync(Event stripeEvent, CancellationToken ct = default)
    {
        if (stripeEvent.Livemode) throw new CheckoutException("Live Stripe events are not accepted.");
        if (stripeEvent.Type is not ("checkout.session.completed" or "checkout.session.async_payment_succeeded" or
            "checkout.session.async_payment_failed" or "checkout.session.expired")) return;
        if (stripeEvent.Data.Object is not Session snapshot) throw new CheckoutException("Invalid Checkout event.");
        if (!Guid.TryParse(snapshot.Metadata?.GetValueOrDefault("payment_attempt_id"), out var attemptId)) return;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.StripeWebhookReceipts.AnyAsync(r => r.Id == stripeEvent.Id, ct)) return;
        var attempt = await db.PaymentAttempts.AsNoTracking().SingleOrDefaultAsync(p => p.Id == attemptId, ct);
        if (attempt is null) return; // Other applications in the same sandbox aren't our orders.
        ValidateSession(attempt, snapshot);
        // The current resource resolves stale failure/expiry events after a newer success.
        var current = await stripe.GetAsync(snapshot.Id, ct);
        ValidateSession(attempt, current);
        await ApplySessionAsync(attemptId, current, stripeEvent.Type, stripeEvent.Id, ct);
        logger.LogInformation("Processed Stripe event {EventId} for order {OrderId}", stripeEvent.Id, attempt.OrderId);
    }

    private async Task ApplySessionAsync(Guid id, Session session, string? eventType, string? eventId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Serializable plus Order.RowVersion protects duplicate/concurrent delivery.
        // Deadlocks or persistence failures roll back and receive a non-2xx for Stripe retry.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAttemptAsync(db, id, ct);
        if (eventId is not null && await db.StripeWebhookReceipts.AnyAsync(r => r.Id == eventId, ct)) return;
        var attempt = await db.PaymentAttempts.Include(p => p.Order).SingleAsync(p => p.Id == id, ct);
        ValidateSession(attempt, session);
        attempt.SessionId = session.Id;
        attempt.PaymentIntentId = session.PaymentIntentId;
        attempt.SessionExpiresAt = session.ExpiresAt;
        var order = attempt.Order;
        var next = session.PaymentStatus == "paid" ? OrderStatus.Paid :
            session.Status == "expired" ? OrderStatus.Expired :
            eventType == "checkout.session.async_payment_failed" && session.Status == "complete" ? OrderStatus.Failed :
            OrderStatus.Pending;
        if (order.Status == OrderStatus.Pending && next != OrderStatus.Pending)
        {
            var reservations = await db.InventoryReservations.Where(r => r.OrderId == order.Id).ToListAsync(ct);
            var itemIds = await db.OrderItems.Where(i => i.OrderId == order.Id).Select(i => i.ProductId).ToListAsync(ct);
            if (reservations.Count != itemIds.Count || reservations.Any(r => !itemIds.Contains(r.ProductId)))
                throw new InvalidOperationException("Payment inventory requires reconciliation.");
            foreach (var reservation in reservations.OrderBy(r => r.ProductId))
            {
                var changed = await db.Products.Where(p => p.Id == reservation.ProductId && p.SalesStatus == ProductSalesStatus.Reserved)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.SalesStatus,
                        next == OrderStatus.Paid ? ProductSalesStatus.Sold : ProductSalesStatus.Available), ct);
                if (changed != 1) throw new InvalidOperationException("Reserved inventory was changed.");
            }
            db.InventoryReservations.RemoveRange(reservations);
            if (next == OrderStatus.Paid)
            {
                // Clear only the purchased items; preserve items added during Checkout.
                await db.ShoppingCartItems.Where(c => c.UserId == order.UserId && itemIds.Contains(c.ProductId)).ExecuteDeleteAsync(ct);
            }
            order.Status = next;
            attempt.PendingUserId = null;
        }
        else if (next == OrderStatus.Paid && order.Status is OrderStatus.Failed or OrderStatus.Expired)
        {
            // Never silently fulfil an anomalous late success after releasing unique stock.
            throw new InvalidOperationException("Late payment after reservation release requires reconciliation.");
        }
        // Paid/Delivered never downgrade. Unpaid completion retains reservations indefinitely
        // until async success/failure; session expiry alone isn't a delayed-payment deadline.
        order.UpdatedAt = DateTime.UtcNow;
        if (eventId is not null) db.StripeWebhookReceipts.Add(new StripeWebhookReceipt { Id = eventId });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static async Task LockAttemptAsync(ApplicationDbContext db, Guid id, CancellationToken ct)
    {
        // Serialize writers for this attempt across app instances before reading its state.
        // UPDLOCK prevents two SQL Server serializable readers both upgrading to writers.
        if (db.Database.IsSqlServer())
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT [Id] FROM [PaymentAttempts] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}", ct);
    }

    private async Task FailCreationAsync(Guid id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAttemptAsync(db, id, ct);
        var attempt = await db.PaymentAttempts.Include(p => p.Order).SingleAsync(p => p.Id == id, ct);
        if (attempt.SessionId is not null || attempt.Order.Status != OrderStatus.Pending) return;
        var held = await db.InventoryReservations.Where(r => r.OrderId == attempt.OrderId).ToListAsync(ct);
        foreach (var reservation in held.OrderBy(r => r.ProductId))
            await db.Products.Where(p => p.Id == reservation.ProductId && p.SalesStatus == ProductSalesStatus.Reserved)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.SalesStatus, ProductSalesStatus.Available), ct);
        db.InventoryReservations.RemoveRange(held);
        attempt.PendingUserId = null;
        attempt.Order.Status = OrderStatus.Failed;
        attempt.Order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ReconcilePendingAsync(CancellationToken ct)
    {
        options.Value.ValidateCheckout();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var attempts = await db.PaymentAttempts.AsNoTracking().Where(p => p.PendingUserId != null)
            .OrderBy(p => p.CreatedAt).Take(100).ToListAsync(ct);
        foreach (var attempt in attempts)
        {
            try
            {
                var session = await EnsureSessionAsync(attempt, ct);
                await ApplySessionAsync(attempt.Id, session, null, null, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch
            {
                logger.LogWarning("Pending Stripe attempt {AttemptId} will need another reconciliation", attempt.Id);
            }
        }
    }
}
