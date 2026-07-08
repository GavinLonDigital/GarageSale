using System.ComponentModel.DataAnnotations;

namespace GarageSale.Data;

public class OrderItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required]
    public int ProductId { get; set; }

    // Configured via Fluent API — two FKs both pointing to AspNetUsers
    // (Seller here, User via Order) would be ambiguous for data annotations.
    [Required]
    public string SellerId { get; set; } = string.Empty;

    [Required]
    public int Price { get; set; }

    public Order Order { get; set; } = null!;
    public Products Product { get; set; } = null!;
    public ApplicationUser Seller { get; set; } = null!;
}
