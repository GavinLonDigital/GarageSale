using GarageSale.Data;
using GarageSale.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GarageSale.Services;

public class SellerService(IDbContextFactory<ApplicationDbContext> dbFactory) : ISellerService
{
    public List<SellerDto> GetSellerHeroImages()
    {
        using var db = dbFactory.CreateDbContext();
        return db.Users
            .Join(db.UserRoles,
                user     => user.Id,
                userRole => userRole.UserId,
                (user, userRole) => new { user, userRole })
            .Join(db.Roles,
                x    => x.userRole.RoleId,
                role => role.Id,
                (x, role) => new { x.user, role })
            .Where(x => x.role.NormalizedName == "SELLER")
            .Join(db.SellerDetails,
                x            => x.user.Id,
                sellerDetail => sellerDetail.UserId,
                (x, sellerDetail) => new SellerDto
                {
                    Id                 = sellerDetail.Id,
                    UserId             = x.user.Id,
                    FirstName          = x.user.FirstName          ?? string.Empty,
                    Email              = x.user.Email              ?? string.Empty,
                    LogoPath           = sellerDetail.LogoPath      ?? string.Empty,
                    SellerBusinessName = sellerDetail.SellerBusinessName ?? string.Empty,
                })
            .ToList();
    }

    public SellerDto GetSellerHeroImageById(string sellerId)
    {
        using var db = dbFactory.CreateDbContext();
        return db.Users
            .Join(db.UserRoles,
                user     => user.Id,
                userRole => userRole.UserId,
                (user, userRole) => new { user, userRole })
            .Join(db.Roles,
                x    => x.userRole.RoleId,
                role => role.Id,
                (x, role) => new { x.user, role })
            .Where(x => x.role.NormalizedName == "SELLER" && x.user.Id == sellerId)
            .Join(db.SellerDetails,
                x            => x.user.Id,
                sellerDetail => sellerDetail.UserId,
                (x, sellerDetail) => new SellerDto
                {
                    Id                 = sellerDetail.Id,
                    UserId             = x.user.Id,
                    FirstName          = x.user.FirstName          ?? string.Empty,
                    Email              = x.user.Email              ?? string.Empty,
                    LogoPath           = sellerDetail.LogoPath      ?? string.Empty,
                    SellerBusinessName = sellerDetail.SellerBusinessName ?? string.Empty,
                })
            .First();
    }
}
