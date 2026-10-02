namespace GarageSale.Configuration;

public class StripeOptions
{
    public string SecretKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public string AppBaseUrl { get; set; } = "";

    // Validate on use, never on startup: the teaching app works without credentials.
    public string ValidateCheckout()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new Services.CheckoutException("Set STRIPE_SECRET_KEY to your Stripe sandbox secret key.");
        if (!SecretKey.StartsWith("sk_test_", StringComparison.Ordinal))
            throw new Services.CheckoutException("Only Stripe sandbox keys beginning with sk_test_ are supported.");
        if (!Uri.TryCreate(AppBaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) || uri.AbsolutePath != "/" ||
            (!uri.IsLoopback && (!uri.Host.Contains('.') || System.Net.IPAddress.TryParse(uri.Host, out _))))
            throw new Services.CheckoutException("Set APP_BASE_URL to the browser-facing origin, for example http://localhost:8080 (HTTPS is required outside localhost).");
        ValidateWebhook();
        return uri.GetLeftPart(UriPartial.Authority);
    }

    public void ValidateWebhook()
    {
        if (string.IsNullOrWhiteSpace(WebhookSecret) || !WebhookSecret.StartsWith("whsec_", StringComparison.Ordinal))
            throw new Services.CheckoutException("Set STRIPE_WEBHOOK_SECRET to the signing secret from your sandbox webhook listener.");
    }
}
