using System;
using System.Collections.Generic;

namespace UnifiedNamespaceLib.Models.Database;

public partial class TopicValue
{
    public long Id { get; set; }

    public int TopicId { get; set; }

    public DateTimeOffset From { get; set; }

    public DateTimeOffset? To { get; set; }

    public int ValueTypeId { get; set; }

    public string? Value { get; set; }

    public virtual Topic Topic { get; set; } = null!;

    public virtual ValueType ValueType { get; set; } = null!;
}
