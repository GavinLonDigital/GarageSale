using System.ComponentModel.DataAnnotations;

namespace GarageSale.Data;

public class Categories
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string CategoryName { get; set; } = string.Empty;

    public string? CategoryDescription { get; set; }

    public ICollection<Products> Products { get; set; } = [];

    public ICollection<CategoriesToUsers> CategoriesToUsers { get; set; } = [];
}
