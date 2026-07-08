using GarageSale.DTOs;

namespace GarageSale.Services;

public interface ISellerService
{
    List<SellerDto> GetSellerHeroImages();

    SellerDto GetSellerHeroImageById(string sellerId);
}
