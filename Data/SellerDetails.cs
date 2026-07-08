namespace GarageSale.Data;

public class SellerDetails
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string? SellerBusinessName { get; set; }

    public string? LogoPath { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
