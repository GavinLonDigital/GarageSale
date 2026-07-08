using GarageSale.Data;

namespace GarageSale.DTOs;

public class OrderDto
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public DateTime DateAdded { get; set; }

    public OrderStatus Status { get; set; }

    public int Total { get; set; }

    public List<OrderItemDto> Items { get; set; } = [];
}
