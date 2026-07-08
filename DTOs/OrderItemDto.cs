namespace GarageSale.DTOs;

public class OrderItemDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SellerBusinessName { get; set; } = string.Empty;

    public int Price { get; set; }
}
