using System;
using System.Collections.Generic;

namespace CatalogLib.Models;

public partial class ThingPropertyModel
{
    public string Type { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Value { get; set; }

    public string? ContentType { get; set; }
}
