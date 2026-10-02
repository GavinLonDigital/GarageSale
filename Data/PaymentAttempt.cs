namespace GarageSale.Data;

public class PaymentAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    // A unique nullable key permits only one pending checkout per buyer.
    public string? PendingUserId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string? SessionId { get; set; }
    public string? PaymentIntentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SessionExpiresAt { get; set; }
    // Freeze every create parameter so retries use exactly the same idempotency request.
    public string RequestJson { get; set; } = "";
}

public class InventoryReservation
{
    public int ProductId { get; set; }
    public Products Product { get; set; } = null!;
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
}

public class StripeWebhookReceipt
{
    public string Id { get; set; } = "";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
