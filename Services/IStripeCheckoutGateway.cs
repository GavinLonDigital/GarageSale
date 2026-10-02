using Stripe.Checkout;

namespace GarageSale.Services;

// The SDK boundary lets tests simulate Stripe without credentials or network calls.
public interface IStripeCheckoutGateway
{
    Task<Session> CreateAsync(SessionCreateOptions options, string idempotencyKey, CancellationToken cancellationToken);
    Task<Session> GetAsync(string sessionId, CancellationToken cancellationToken);
}
