using System;
using System.Collections.Generic;

namespace CatalogLib.Database;

public partial class ThingsAttachment
{
    public int Id { get; set; }

    public int ThingId { get; set; }

    public string Type { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Value { get; set; }

    public string? ContentType { get; set; }

    public string? ContentEncoding { get; set; }
    public virtual Thing Thing { get; set; } = null!;
}
