using System.Linq.Expressions;
using GarageSale.Data;
using GarageSale.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GarageSale.Services;

public class ProductService(IDbContextFactory<ApplicationDbContext> dbFactory) : IProductService
{
    public async Task<List<ProductDto>> GetProductsOrderedByCondition(string? userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await GetProductsOrderedByPreference(db, userId, baseFilter: null);
    }

    // Groups products by the user's preferred categories (in the order those preferences are
    // stored), each group ordered by condition rating, followed by everything else ordered the
    // same way. An optional baseFilter (e.g. seller-scoping) is applied before grouping, so
    // callers like the seller's garage page can reuse the same personalisation logic.
    private static async Task<List<ProductDto>> GetProductsOrderedByPreference(
        ApplicationDbContext db,
        string? userId,
        Expression<Func<Products, bool>>? baseFilter)
    {
        List<int> preferredCategoryIds = userId is null
            ? []
            : await db.CategoriesToUsers
                .Where(ctu => ctu.UserId == userId)
                .Select(ctu => ctu.CategoryId)
                .ToListAsync();

        var products = new List<ProductDto>();

        foreach (var categoryId in preferredCategoryIds)
        {
            var categoryProducts = await BuildProductsQuery(db, baseFilter, product => product.CategoryId == categoryId)
                .ToListAsync();
            products.AddRange(categoryProducts);
        }

        // Everything left over (categories the user has no preference for) as a final group.
        var remainingProducts = await BuildProductsQuery(db, baseFilter, product => !preferredCategoryIds.Contains(product.CategoryId))
            .ToListAsync();
        products.AddRange(remainingProducts);

        return products;
    }

    private static IQueryable<ProductDto> BuildProductsQuery(
        ApplicationDbContext db,
        Expression<Func<Products, bool>>? baseFilter,
        Expression<Func<Products, bool>> categoryFilter)
    {
        var query = db.Products.Where(categoryFilter);
        if (baseFilter is not null)
            query = query.Where(baseFilter);

        return query
            .Join(db.Categories,
                product  => product.CategoryId,
                category => category.Id,
                (product, category) => new { product, category })
            .Join(db.ConditionRatings,
                x               => x.product.ConditionRatingId,
                conditionRating => conditionRating.Id,
                (x, conditionRating) => new { x.product, x.category, conditionRating })
            .Join(db.SellerDetails,
                x            => x.product.SellerId,
                sellerDetail => sellerDetail.UserId,
                (x, sellerDetail) => new { x.product, x.category, x.conditionRating, sellerDetail })
            .Join(db.Users,
                x    => x.sellerDetail.UserId,
                user => user.Id,
                (x, user) => new ProductDto
                {
                    Id                               = x.product.Id,
                    ProductName                      = x.product.ProductName,
                    ProductDescription               = x.product.ProductDescription,
                    Price                            = x.product.Price,
                    ImagePath                        = x.product.ImagePath,
                    CategoryName                     = x.category.CategoryName,
                    ConditionRatingNumber            = x.conditionRating.ConditionRatingNumber,
                    ConditionRatingNumberDescription = x.conditionRating.ConditionRatingNumberDescription,
                    SellerBusinessName               = x.sellerDetail.SellerBusinessName ?? string.Empty,
                    SellerEmail                      = user.Email ?? string.Empty,
                    SellerId                         = x.product.SellerId,
                    AdminReview                      = x.product.AdminReview,
                    SalesStatus                      = x.product.SalesStatus,
                })
            .OrderBy(dto => dto.ConditionRatingNumber);
    }

    public async Task<List<ProductDto>> GetProductsForSellersGarage(string sellerId, string? userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await GetProductsOrderedByPreference(db, userId, product => product.SellerId == sellerId);
    }

    public async Task<List<ProductDto>> GetProductsByCategoryAndSellerOrderedByCondition(int categoryId, string sellerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Products
            .Where(product => product.CategoryId == categoryId && product.SellerId == sellerId)
            .Join(db.Categories,
                product  => product.CategoryId,
                category => category.Id,
                (product, category) => new { product, category })
            .Join(db.ConditionRatings,
                x               => x.product.ConditionRatingId,
                conditionRating => conditionRating.Id,
                (x, conditionRating) => new { x.product, x.category, conditionRating })
            .Join(db.SellerDetails,
                x            => x.product.SellerId,
                sellerDetail => sellerDetail.UserId,
                (x, sellerDetail) => new { x.product, x.category, x.conditionRating, sellerDetail })
            .Join(db.Users,
                x    => x.sellerDetail.UserId,
                user => user.Id,
                (x, user) => new ProductDto
                {
                    Id                               = x.product.Id,
                    ProductName                      = x.product.ProductName,
                    ProductDescription               = x.product.ProductDescription,
                    Price                            = x.product.Price,
                    ImagePath                        = x.product.ImagePath,
                    CategoryName                     = x.category.CategoryName,
                    ConditionRatingNumber            = x.conditionRating.ConditionRatingNumber,
                    ConditionRatingNumberDescription = x.conditionRating.ConditionRatingNumberDescription,
                    SellerBusinessName               = x.sellerDetail.SellerBusinessName ?? string.Empty,
                    SellerEmail                      = user.Email ?? string.Empty,
                    SellerId                         = x.product.SellerId,
                    AdminReview                      = x.product.AdminReview,
                    SalesStatus                      = x.product.SalesStatus,
                })
            .OrderBy(dto => dto.ConditionRatingNumber)
            .ToListAsync();
    }

    public async Task<List<CategoryDto>> GetProductCategories()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Categories
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryDto
            {
                Id                  = c.Id,
                CategoryName        = c.CategoryName,
                CategoryDescription = c.CategoryDescription,
            })
            .ToListAsync();
    }

    public async Task<List<ConditionRatingDto>> GetConditionRatings()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ConditionRatings
            .OrderBy(c => c.ConditionRatingNumber)
            .Select(c => new ConditionRatingDto
            {
                Id                               = c.Id,
                ConditionRatingNumber            = c.ConditionRatingNumber,
                ConditionRatingNumberDescription = c.ConditionRatingNumberDescription,
            })
            .ToListAsync();
    }

    public async Task<List<ProductDto>> GetProductsByCategoryOrderedByCondition(int categoryId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Products
            .Where(product => product.CategoryId == categoryId)
            .Join(db.Categories,
                product  => product.CategoryId,
                category => category.Id,
                (product, category) => new { product, category })
            .Join(db.ConditionRatings,
                x               => x.product.ConditionRatingId,
                conditionRating => conditionRating.Id,
                (x, conditionRating) => new { x.product, x.category, conditionRating })
            .Join(db.SellerDetails,
                x            => x.product.SellerId,
                sellerDetail => sellerDetail.UserId,
                (x, sellerDetail) => new { x.product, x.category, x.conditionRating, sellerDetail })
            .Join(db.Users,
                x    => x.sellerDetail.UserId,
                user => user.Id,
                (x, user) => new ProductDto
                {
                    Id                               = x.product.Id,
                    ProductName                      = x.product.ProductName,
                    ProductDescription               = x.product.ProductDescription,
                    Price                            = x.product.Price,
                    ImagePath                        = x.product.ImagePath,
                    CategoryName                     = x.category.CategoryName,
                    ConditionRatingNumber            = x.conditionRating.ConditionRatingNumber,
                    ConditionRatingNumberDescription = x.conditionRating.ConditionRatingNumberDescription,
                    SellerBusinessName               = x.sellerDetail.SellerBusinessName ?? string.Empty,
                    SellerEmail                      = user.Email ?? string.Empty,
                    SellerId                         = x.product.SellerId,
                    AdminReview                      = x.product.AdminReview,
                    SalesStatus                      = x.product.SalesStatus,
                })
            .OrderBy(dto => dto.ConditionRatingNumber)
            .ToListAsync();
    }

    public async Task<ProductDto?> GetProductDetailsById(int productId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Products
            .Where(product => product.Id == productId)
            .Join(db.Categories,
                product  => product.CategoryId,
                category => category.Id,
                (product, category) => new { product, category })
            .Join(db.ConditionRatings,
                x               => x.product.ConditionRatingId,
                conditionRating => conditionRating.Id,
                (x, conditionRating) => new { x.product, x.category, conditionRating })
            .Join(db.SellerDetails,
                x            => x.product.SellerId,
                sellerDetail => sellerDetail.UserId,
                (x, sellerDetail) => new { x.product, x.category, x.conditionRating, sellerDetail })
            .Join(db.Users,
                x    => x.sellerDetail.UserId,
                user => user.Id,
                (x, user) => new ProductDto
                {
                    Id                               = x.product.Id,
                    ProductName                      = x.product.ProductName,
                    ProductDescription               = x.product.ProductDescription,
                    Price                            = x.product.Price,
                    ImagePath                        = x.product.ImagePath,
                    CategoryName                     = x.category.CategoryName,
                    ConditionRatingNumber            = x.conditionRating.ConditionRatingNumber,
                    ConditionRatingNumberDescription = x.conditionRating.ConditionRatingNumberDescription,
                    SellerBusinessName               = x.sellerDetail.SellerBusinessName ?? string.Empty,
                    SellerEmail                      = user.Email ?? string.Empty,
                    SellerId                         = x.product.SellerId,
                    AdminReview                      = x.product.AdminReview,
                    SalesStatus                      = x.product.SalesStatus,
                })
            .FirstOrDefaultAsync();
    }

    public async Task SaveAdminReview(int productId, string adminReview, int conditionRatingId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var product = await db.Products.FirstOrDefaultAsync(product => product.Id == productId);
        if (product is null)
            return;

        product.AdminReview       = adminReview;
        product.ConditionRatingId = conditionRatingId;

        await db.SaveChangesAsync();
    }
}
