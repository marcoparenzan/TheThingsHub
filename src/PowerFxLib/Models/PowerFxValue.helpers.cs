using Microsoft.PowerFx.Types;

namespace PowerFxLib.Models;

public partial struct PowerFxValue
{
    public static PowerFxValue Error(string errorValue) => new()
    {
        ValueType = PowerFxValueType.Error,
        ErrorValue = errorValue
    };

    public static PowerFxValue Formula(string formulaValue) => new()
    {
        ValueType = PowerFxValueType.Formula,
        FormulaValue = formulaValue
    };

    public static PowerFxValue From(bool value)
    {
        if (value) return True;
        return False;
    }

    public static PowerFxValue From(DateTime value) => new PowerFxValue
    {
        ValueType = PowerFxValueType.DateTime,
        DateTimeValue = value
    };

    public static PowerFxValue From(DateTimeOffset value) => new PowerFxValue
    {
        ValueType = PowerFxValueType.DateTime,
        DateTimeValue = value
    };

    public static PowerFxValue From(Guid value) => new PowerFxValue
    {
        ValueType = PowerFxValueType.Guid,
        GuidValue = value
    };

    public static PowerFxValue From(double value) => new PowerFxValue
    {
        ValueType = PowerFxValueType.Number,
        NumberValue = Convert.ToDecimal(value)
    };

    public static PowerFxValue From(int value) => new PowerFxValue
    {
        ValueType = PowerFxValueType.Number,
        NumberValue = value
    };

    public static PowerFxValue From(decimal value) => new PowerFxValue
    {
        ValueType = PowerFxValueType.Number,
        NumberValue = value
    };

    public static PowerFxValue From(RecordValue value)
    {
        var returnValue = new PowerFxValue
        {
            ValueType = PowerFxValueType.Record,
            RecordValue = value
        };
        return returnValue;
    }

    public static PowerFxValue From(FormulaValue value)
    {
        if (value.Type == FormulaType.String) return From(value.ToString());
        if (value.Type == FormulaType.Boolean) return From(value.AsBoolean());
        if (value.Type == FormulaType.Number) return From(value.AsDecimal());
        //if (value.Type == FormulaType.DateTime) return From((DateTimeOffset)value);
        //if (value.Type == FormulaType.Color) return From();
        
        throw new NotSupportedException("");
    }

    public static PowerFxValue From(PowerFxColor value) => new PowerFxValue
    {
        ValueType = PowerFxValueType.Color,
        ColorValue = value
    };

    public static PowerFxValue From(string scalarText)
    {
        var returnValue = PowerFxValue.NULL;

        var tempValue = scalarText.TrimStart();
        if (tempValue.StartsWith('='))
        {
            returnValue = Formula(tempValue.Substring(1).Trim());
        }
        else if (DateTimeOffset.TryParse(tempValue, out var dateTimeValue))
        {
            returnValue = From(dateTimeValue);
        }
        else if (int.TryParse(tempValue, out var intValue))
        {
            returnValue = From(intValue);
        }
        else if (decimal.TryParse(tempValue, out var decimalValue))
        {
            returnValue = From(decimalValue);
        }
        else if (bool.TryParse(tempValue, out var booleanValue))
        {
            returnValue = From(booleanValue);
        }
        else if (Guid.TryParse(tempValue, out var guidValue))
        {
            returnValue = From(guidValue);
        }
        else if (PowerFxColor.TryParse(tempValue, out var colorValue))
        {
            returnValue = From(colorValue);
        }
        else
        {
            returnValue.ValueType = PowerFxValueType.String;
            returnValue.StringValue = scalarText;
        }
        return returnValue;
    }
}
