namespace GarageSale.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryDescription { get; set; }
}
