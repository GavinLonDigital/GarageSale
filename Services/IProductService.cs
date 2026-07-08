using GarageSale.DTOs;

namespace GarageSale.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetProductsOrderedByCondition(string? userId);

    Task<List<ProductDto>> GetProductsForSellersGarage(string sellerId, string? userId);

    Task<ProductDto?> GetProductDetailsById(int productId);

    Task<List<CategoryDto>> GetProductCategories();

    Task<List<ConditionRatingDto>> GetConditionRatings();

    Task<List<ProductDto>> GetProductsByCategoryOrderedByCondition(int categoryId);

    Task<List<ProductDto>> GetProductsByCategoryAndSellerOrderedByCondition(int categoryId, string sellerId);

    Task SaveAdminReview(int productId, string adminReview, int conditionRatingId);
}
