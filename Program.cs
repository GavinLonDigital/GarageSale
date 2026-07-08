using GarageSale.Components;
using GarageSale.Configuration;
using GarageSale.Data;
using GarageSale.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<OpenAIOptions>(builder.Configuration);

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
});

builder.Services.AddScoped<IdentitySeeder>();
builder.Services.AddScoped<ProductSeeder>();
builder.Services.AddScoped<ISellerService, SellerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddHttpClient<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<CartStateService>();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var retryCount = 0;
    while (retryCount < 10)
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
            break;
        }
        catch
        {
            retryCount++;
            if (retryCount >= 10) throw;
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }

    var seedLogger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup.Seeder");
    try
    {
        seedLogger.LogInformation("Identity seeder starting...");
        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();
        seedLogger.LogInformation("Identity seeder completed successfully.");
    }
    catch (Exception ex)
    {
        seedLogger.LogError(ex,
            "Identity seeder failed. Type: {ExceptionType} | Message: {Message}",
            ex.GetType().FullName, ex.Message);
        if (ex.InnerException is not null)
            seedLogger.LogError("  Inner: {InnerType} | {InnerMessage}",
                ex.InnerException.GetType().FullName, ex.InnerException.Message);
    }

    try
    {
        seedLogger.LogInformation("Product seeder starting...");
        var productSeeder = scope.ServiceProvider.GetRequiredService<ProductSeeder>();
        await productSeeder.SeedAsync();
        seedLogger.LogInformation("Product seeder completed successfully.");
    }
    catch (Exception ex)
    {
        seedLogger.LogError(ex,
            "Product seeder failed. Type: {ExceptionType} | Message: {Message}",
            ex.GetType().FullName, ex.Message);
        if (ex.InnerException is not null)
            seedLogger.LogError("  Inner: {InnerType} | {InnerMessage}",
                ex.InnerException.GetType().FullName, ex.InnerException.Message);
    }
}

app.Run();
