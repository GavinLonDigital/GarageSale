using GarageSale.Configuration;
using GarageSale.Services;
using Microsoft.Extensions.Options;
using Stripe;

namespace GarageSale.Endpoints;

public static class StripeWebhookEndpoint
{
    public static void MapStripeWebhook(this WebApplication app)
    {
        app.MapPost("/api/stripe/webhook", async (HttpRequest request, IOptions<StripeOptions> options,
            StripePaymentService payments, ILoggerFactory loggers, CancellationToken ct) =>
        {
            try
            {
                options.Value.ValidateWebhook();
                if (string.IsNullOrWhiteSpace(request.Headers["Stripe-Signature"]))
                    return Results.BadRequest("Stripe signature is required.");
                if (request.ContentLength > 1_048_576) return Results.StatusCode(413);
                using var reader = new StreamReader(request.Body);
                var rawBody = await reader.ReadToEndAsync(ct);
                var stripeEvent = EventUtility.ConstructEvent(rawBody, request.Headers["Stripe-Signature"],
                    options.Value.WebhookSecret);
                await payments.ProcessEventAsync(stripeEvent, ct);
                return Results.Ok();
            }
            catch (StripeException ex)
            {
                // Log only a fixed category: SDK exception messages can contain payload details.
                var reason = ex.Message.Contains("API version", StringComparison.OrdinalIgnoreCase)
                    ? "incompatible API version" : "invalid event or signature";
                loggers.CreateLogger("StripeWebhook").LogWarning("Stripe webhook rejected: {Reason}", reason);
                return Results.BadRequest("Invalid Stripe event or signature.");
            }
            catch (ArgumentException) { return Results.BadRequest("Invalid Stripe event or signature."); }
            catch (Newtonsoft.Json.JsonException) { return Results.BadRequest("Invalid Stripe event payload."); }
            catch (CheckoutException) { return Results.BadRequest("Sandbox webhook configuration or event is invalid."); }
            catch
            {
                loggers.CreateLogger("StripeWebhook").LogWarning("Stripe webhook persistence or reconciliation failed; delivery must be retried.");
                return Results.StatusCode(500);
            }
        }).AllowAnonymous().DisableAntiforgery(); // Only this signature-verified endpoint is exempt.
    }
}
