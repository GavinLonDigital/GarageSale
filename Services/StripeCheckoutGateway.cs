using GarageSale.Configuration;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GarageSale.Services;

public class StripeCheckoutGateway(IOptions<StripeOptions> options, HttpClient httpClient) : IStripeCheckoutGateway
{
    private SessionService Sessions()
    {
        options.Value.ValidateCheckout();
        return new StripeClient(options.Value.SecretKey, httpClient: new SystemNetHttpClient(httpClient, maxNetworkRetries: 0)).V1.Checkout.Sessions;
    }

    public Task<Session> CreateAsync(SessionCreateOptions request, string key, CancellationToken cancellationToken) =>
        Sessions().CreateAsync(request, new RequestOptions { IdempotencyKey = key }, cancellationToken);

    public Task<Session> GetAsync(string id, CancellationToken cancellationToken) =>
        Sessions().GetAsync(id, cancellationToken: cancellationToken);
}
