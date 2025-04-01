using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using PowerFxLib.Models;

namespace PowerFxLib.Functions;

public class UnifiedNamespaceFunction : ReflectionFunction
{
    private Func<string, PowerFxValue> action;

    public UnifiedNamespaceFunction(Func<string, PowerFxValue> action) : base("UnifiedNamespace", FormulaType.Unknown, new[] { FormulaType.String }) 
    {
        this.action = action;
    }

    public FormulaValue Execute(StringValue arg)
    {
        var result = action(arg.Value);
        return Map(result);
        return FormulaValue.New(true);
    }

    private static FormulaValue Map(PowerFxValue result)
    {
        switch (result.ValueType)
        {
            case PowerFxValueType.Null:
                return FormulaValue.NewBlank();
            case PowerFxValueType.Number:
                return FormulaValue.New(result.NumberValue.Value);
            case PowerFxValueType.Boolean:
                return FormulaValue.New(result.BooleanValue.Value);
            case PowerFxValueType.String:
                return FormulaValue.New(result.StringValue);
            case PowerFxValueType.DateTime:
                return FormulaValue.New(result.DateTimeValue.Value.DateTime);
            case PowerFxValueType.Set:
                return FormulaValue.NewRecordFromFields(result.SetValue.Select(xx => new NamedValue(xx.Key, Map(xx.Value))));
            case PowerFxValueType.Error:
                return FormulaValue.NewError(new ExpressionError
                {
                    Kind = ErrorKind.Unknown,
                    Message = result.ErrorValue
                });
            default:
                return FormulaValue.NewError(new ExpressionError
                {
                    Kind = ErrorKind.InvalidArgument,
                    Message = $"cannot defnie what's happened"
                });
        }
    }
}