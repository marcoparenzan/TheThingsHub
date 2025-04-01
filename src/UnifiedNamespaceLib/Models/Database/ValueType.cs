using System;
using System.Collections.Generic;

namespace UnifiedNamespaceLib.Models.Database;

public partial class ValueType
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<TopicValue> TopicValues { get; set; } = new List<TopicValue>();
}
