using Microsoft.AspNetCore.Identity;

namespace GarageSale.Data;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? ZipOrPostCode { get; set; }

    public ICollection<CategoriesToUsers> CategoriesToUsers { get; set; } = [];
    public SellerDetails? SellerDetails { get; set; }
}
