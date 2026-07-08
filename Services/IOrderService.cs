namespace GarageSale.Services;

public interface IOrderService
{
    Task<int> CreateOrder(string userId);
}
