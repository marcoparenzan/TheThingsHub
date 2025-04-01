using CatalogLib.Models.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CatalogLib.Models;

public partial class ThingItem
{
    public int? Id { get; set; }

	public string Name { get; set; } = null!;

	public string Description { get; set; } = null!;
}
