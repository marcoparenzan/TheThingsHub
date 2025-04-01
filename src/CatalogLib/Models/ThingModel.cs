using CatalogLib.Models.Validation;
using System.ComponentModel.DataAnnotations;

namespace CatalogLib.Models;

public partial class ThingModel
{
	[Required(ErrorMessage = "Name is required.")]
	[StringLength(64, ErrorMessage = "Name must be no more than 64 characters.")]
	public string Name { get; set; } = null!;

    [StringLength(256, ErrorMessage = "Description must be no more than 256 characters.")]
    public string Description { get; set; } = null!;
}
