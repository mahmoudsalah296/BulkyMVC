using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bulky.MVC.Models;

public class Category
{
    [Key]
    public required int Id { get; set; }

    [DisplayName("Category Name")]
    [MaxLength(32, ErrorMessage = "The name is to long, Max length is 32")]
    public required string Name { get; set; }

    [DisplayName("Display Order")]
    // search for default value
    [Range(1, 100, ErrorMessage = "Display order must be between 1 and 100")]
    public int DisplayOrder { get; set; }
}
