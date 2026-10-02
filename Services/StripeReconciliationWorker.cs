namespace GarageSale.Services;

// Recover a lost create response and missed paid/expired webhooks after a restart.
public class StripeReconciliationWorker(IServiceScopeFactory scopes) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = scopes.CreateScope();
            try { await scope.ServiceProvider.GetRequiredService<StripePaymentService>().ReconcilePendingAsync(stoppingToken); }
            catch (CheckoutException) { /* Missing settings are expected before the tutorial is configured. */ }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch
            {
                scope.ServiceProvider.GetRequiredService<ILogger<StripeReconciliationWorker>>()
                    .LogWarning("Stripe reconciliation unavailable; pending stock remains reserved.");
            }
        }
    }
}
