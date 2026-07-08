using GarageSale.Data;
using Microsoft.EntityFrameworkCore;

namespace GarageSale.Services;

public class OrderService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IShoppingCartService shoppingCartService) : IOrderService
{
    public async Task<int> CreateOrder(string userId)
    {
        // Step 1 — read cart data (separate, read-only context)
        var cartItems = await shoppingCartService.GetShoppingCartItems(userId);
        if (cartItems.Count == 0)
            return 0;

        // Steps 2-4 run inside a single transaction so order creation and
        // cart clearing are committed together or not at all.
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            // Step 2 — create the order header
            var order = new Order { UserId = userId };
            db.Orders.Add(order);
            await db.SaveChangesAsync(); // generates Order.Id

            // Step 3 — create one order item per cart row
            var orderItems = cartItems.Select(ci => new OrderItem
            {
                OrderId   = order.Id,
                ProductId = ci.ProductId,
                SellerId  = ci.SellerId,
                Price     = ci.Price,
            });

            db.OrderItems.AddRange(orderItems);
            await db.SaveChangesAsync();

            // Step 4a — delete cart rows in the same transaction
            var cartRows = await db.ShoppingCartItems
                .Where(sci => sci.UserId == userId)
                .ToListAsync();

            db.ShoppingCartItems.RemoveRange(cartRows);
            await db.SaveChangesAsync();

            await tx.CommitAsync();

            // Step 4b — notify UI state after the commit (outside the transaction)
            await shoppingCartService.ClearShoppingCart(userId);

            return order.Id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
