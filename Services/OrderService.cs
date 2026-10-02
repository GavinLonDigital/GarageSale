using GarageSale.Data;
using GarageSale.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GarageSale.Services;

public class OrderService(
    IDbContextFactory<ApplicationDbContext> dbFactory) : IOrderService
{
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
            // Payment records must survive admin demo cleanup, including active holds.
            if (await db.PaymentAttempts.AnyAsync())
                throw new CheckoutException("Orders with Stripe payment history cannot be cleared. Keep these records for reconciliation.");

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
