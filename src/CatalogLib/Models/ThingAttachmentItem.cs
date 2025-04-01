namespace CatalogLib.Models;

public partial class ThingAttachmentItem
{
    public int? Id { get; set; }

    public string Type { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Value { get; set; }

    public string? ContentType { get; set; }
}
