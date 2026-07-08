using System.ComponentModel.DataAnnotations;

namespace GarageSale.Data;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    // Mapped to char(1) via value converter in Fluent API.
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public byte[] RowVersion { get; set; } = [];

    public ApplicationUser User { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
