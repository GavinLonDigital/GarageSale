using GarageSale.DTOs;

namespace GarageSale.Services;

public interface IOrderService
{
    Task<int> CreateOrder(string userId);

    Task<List<OrderDto>> GetOrderHistory();

    Task ClearOrders();
}
