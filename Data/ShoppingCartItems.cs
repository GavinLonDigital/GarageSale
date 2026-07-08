using System.ComponentModel.DataAnnotations;

namespace GarageSale.Data;

public class ShoppingCartItems
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int ProductId { get; set; }

    [Required]
    public string SellerId { get; set; } = string.Empty;

    [Required]
    public int Price { get; set; }

    [Required]
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    // Navigation properties — configured via Fluent API because both User and
    // Seller resolve to ApplicationUser, which is ambiguous for data annotations.
    public ApplicationUser User { get; set; } = null!;
    public Products Product { get; set; } = null!;
    public ApplicationUser Seller { get; set; } = null!;
}
