using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Bulky.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }
    public string? Description { get; set; }

    [Required]
    public string ISBN { get; set; }

    [Required]
    public string Author { get; set; }

    [DisplayName("List Price")]
    [Range(1, 1000)]
    public double ListPrice { get; set; }

    [DisplayName("Price for 1-50")]
    [Range(1, 1000)]
    [Required]
    public double Price { get; set; }

    [DisplayName("Price for 50+")]
    [Range(1, 1000)]
    [Required]
    public double Price50 { get; set; }

    [DisplayName("Price for 100+")]
    [Range(1, 1000)]
    [Required]
    public double Price100 { get; set; }

    public int CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    [ValidateNever]
    public Category Category { get; set; }

    //public string? ImageUrl { get; set; }
    [ValidateNever]
    public List<ProductImage>? ProductImages { get; set; }
}
