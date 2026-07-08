using System.ComponentModel.DataAnnotations;

namespace GarageSale.Data;

public class ConditionRatings
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ConditionRatingNumber { get; set; }

    [Required]
    public string ConditionRatingNumberDescription { get; set; } = string.Empty;

    public ICollection<Products> Products { get; set; } = [];
}
