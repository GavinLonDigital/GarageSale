using GarageSale.Data;
using GarageSale.DTOs;
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

            // Step 3b — mark the purchased products as sold
            var productIds = cartItems.Select(ci => ci.ProductId).ToList();
            var products = await db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            foreach (var product in products)
                product.SalesStatus = ProductSalesStatus.Sold;

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

    public async Task<List<OrderDto>> GetOrderHistory()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Orders
            .OrderByDescending(o => o.DateAdded)
            .Select(o => new OrderDto
            {
                Id           = o.Id,
                CustomerName = o.User.FirstName ?? string.Empty,
                CustomerEmail = o.User.Email ?? string.Empty,
                DateAdded    = o.DateAdded,
                Status       = o.Status,
                Total        = o.OrderItems.Sum(oi => oi.Price),
                Items = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id                 = oi.Id,
                    ProductId          = oi.ProductId,
                    ProductName        = oi.Product.ProductName,
                    SellerBusinessName = oi.Seller.SellerDetails != null
                        ? oi.Seller.SellerDetails.SellerBusinessName ?? string.Empty
                        : string.Empty,
                    Price = oi.Price,
                }).ToList(),
            })
            .ToListAsync();
    }

    public async Task ClearOrders()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            // OrderItems first — FK references Orders.
            var orderItems = await db.OrderItems.ToListAsync();
            db.OrderItems.RemoveRange(orderItems);
            await db.SaveChangesAsync();

            var orders = await db.Orders.ToListAsync();
            db.Orders.RemoveRange(orders);
            await db.SaveChangesAsync();

            // Reset sold products back to available now that their orders are gone.
            var soldProducts = await db.Products
                .Where(p => p.SalesStatus == ProductSalesStatus.Sold)
                .ToListAsync();

            foreach (var product in soldProducts)
                product.SalesStatus = ProductSalesStatus.Available;

            await db.SaveChangesAsync();

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
