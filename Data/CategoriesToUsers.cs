namespace GarageSale.Data;

public class CategoriesToUsers
{
    public string UserId { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public Categories Category { get; set; } = null!;
}
