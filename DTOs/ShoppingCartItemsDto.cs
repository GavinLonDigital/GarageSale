namespace GarageSale.DTOs;

public class ShoppingCartItemsDto
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? ProductDescription { get; set; }

    public string? ImagePath { get; set; }

    public string SellerId { get; set; } = string.Empty;

    public string SellerBusinessName { get; set; } = string.Empty;

    public int Price { get; set; }
}
