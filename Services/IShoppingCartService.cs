using GarageSale.DTOs;

namespace GarageSale.Services;

public interface IShoppingCartService
{
    Task<List<ShoppingCartItemsDto>> GetShoppingCartItems(string userId);

    Task AddItemToShoppingCart(ShoppingCartItemsDto shoppingCartItemsDto);

    Task DeleteItemFromShoppingCart(int productId, string userId);

    Task ClearShoppingCart(string userId);
}
