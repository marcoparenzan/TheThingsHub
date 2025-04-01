using Microsoft.PowerFx.Types;
using System.Diagnostics;

namespace PowerFxLib.Models;

[DebuggerDisplay("{Value}[{ValueType}]")]
public partial struct PowerFxValue
{
    public static readonly PowerFxValue NULL = new()
    {
        ValueType = PowerFxValueType.Null
    };

    public static readonly PowerFxValue False = new()
    {
        ValueType = PowerFxValueType.Boolean,
        BooleanValue = false
    };

    public static readonly PowerFxValue True = new()
    {
        ValueType = PowerFxValueType.Boolean,
        BooleanValue = true
    };

    public PowerFxValueType ValueType { get; set; }
    public PowerFxColor? ColorValue { get; set; }
    public decimal? NumberValue { get; set; }
    public string? StringValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTimeOffset? DateTimeValue { get; set; }
    public Dictionary<string, PowerFxValue> SetValue { get; set; }
    public string ErrorValue { get; set; }
    public string? FormulaValue { get; set; }
    public RecordValue RecordValue { get; set; }
    public Guid? GuidValue { get; set; }

    public object Value => ValueType switch
    {
        PowerFxValueType.Null => null,
        PowerFxValueType.Formula => FormulaValue,
        PowerFxValueType.Number => NumberValue,
        PowerFxValueType.DateTime => DateTimeValue,
        PowerFxValueType.String => StringValue,
        PowerFxValueType.Set => SetValue,
        PowerFxValueType.Error => ErrorValue,
        PowerFxValueType.Record => RecordValue,
        _ => null
    };
}
