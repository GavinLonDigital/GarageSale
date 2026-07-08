using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GarageSale.Data;

public class Products
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ProductName { get; set; } = string.Empty;

    public string? ProductDescription { get; set; }

    [Required]
    public int Price { get; set; }

    public string? ImagePath { get; set; }

    public string? AdminReview { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Categories Category { get; set; } = null!;

    [Required]
    public int ConditionRatingId { get; set; }

    [ForeignKey(nameof(ConditionRatingId))]
    public ConditionRatings ConditionRating { get; set; } = null!;

    [Required]
    public string SellerId { get; set; } = string.Empty;

    [ForeignKey(nameof(SellerId))]
    public ApplicationUser Seller { get; set; } = null!;

    // Mapped to char(1) via value converter in Fluent API.
    // 'A' = Available, 'S' = Sold / Not Available.
    public ProductSalesStatus SalesStatus { get; set; } = ProductSalesStatus.Available;
}
