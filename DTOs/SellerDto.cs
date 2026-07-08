namespace GarageSale.DTOs;

public class SellerDto
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string LogoPath { get; set; } = string.Empty;

    public string SellerBusinessName { get; set; } = string.Empty;
}
