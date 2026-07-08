using GarageSale.Data;
using GarageSale.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GarageSale.Services;

public class ShoppingCartService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    CartStateService cartStateService) : IShoppingCartService
{
    public async Task<List<ShoppingCartItemsDto>> GetShoppingCartItems(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ShoppingCartItems
            .Where(sci => sci.UserId == userId)
            .Join(db.Products,
                sci     => sci.ProductId,
                product => product.Id,
                (sci, product) => new { sci, product })
            .Join(db.SellerDetails,
                x            => x.sci.SellerId,
                sellerDetail => sellerDetail.UserId,
                (x, sellerDetail) => new ShoppingCartItemsDto
                {
                    Id                 = x.sci.Id,
                    UserId             = x.sci.UserId,
                    ProductId          = x.sci.ProductId,
                    ProductName        = x.product.ProductName,
                    ProductDescription = x.product.ProductDescription,
                    ImagePath          = x.product.ImagePath,
                    SellerId           = x.sci.SellerId,
                    SellerBusinessName = sellerDetail.SellerBusinessName ?? string.Empty,
                    Price              = x.sci.Price,
                })
            .ToListAsync();
    }

    public async Task AddItemToShoppingCart(ShoppingCartItemsDto shoppingCartItemsDto)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var item = new ShoppingCartItems
        {
            UserId    = shoppingCartItemsDto.UserId,
            ProductId = shoppingCartItemsDto.ProductId,
            SellerId  = shoppingCartItemsDto.SellerId,
            Price     = shoppingCartItemsDto.Price,
        };

        db.ShoppingCartItems.Add(item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteItemFromShoppingCart(int productId, string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var item = await db.ShoppingCartItems
            .FirstOrDefaultAsync(sci => sci.ProductId == productId && sci.UserId == userId);

        if (item is null)
            return;

        db.ShoppingCartItems.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task ClearShoppingCart(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var items = await db.ShoppingCartItems
            .Where(sci => sci.UserId == userId)
            .ToListAsync();

        if (items.Count > 0)
        {
            db.ShoppingCartItems.RemoveRange(items);
            await db.SaveChangesAsync();
        }

        cartStateService.SetCount(0);
    }
}
