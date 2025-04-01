using System;
using System.Collections.Generic;

namespace CatalogLib.Database;

public partial class Thing
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<ThingsAttachment> ThingsAttachments { get; set; } = new List<ThingsAttachment>();

    public virtual ICollection<ThingsProperty> ThingsProperties { get; set; } = new List<ThingsProperty>();
}
