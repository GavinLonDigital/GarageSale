using GarageSale.Data;

namespace GarageSale.DTOs;

public class ProductDto
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? ProductDescription { get; set; }

    public int Price { get; set; }

    public string? ImagePath { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int ConditionRatingNumber { get; set; }

    public string ConditionRatingNumberDescription { get; set; } = string.Empty;

    public string? AdminReview { get; set; }

    public string SellerBusinessName { get; set; } = string.Empty;

    public string SellerEmail { get; set; } = string.Empty;

    public string SellerId { get; set; } = string.Empty;

    public ProductSalesStatus SalesStatus { get; set; }
}
