using System;
using System.Collections.Generic;

namespace UnifiedNamespaceLib.Models.Database;

public partial class Topic
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public virtual ICollection<TopicValue> TopicValues { get; set; } = new List<TopicValue>();
}
