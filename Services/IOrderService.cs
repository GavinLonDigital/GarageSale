using GarageSale.DTOs;

namespace GarageSale.Services;

public interface IOrderService
{
    Task<List<OrderDto>> GetOrderHistory();

    Task ClearOrders();
}
