using Microsoft.PowerFx.Types;

namespace PowerFxLib.Models;

public partial struct PowerFxValue
{
    public static PowerFxValue FromJson(string json)
    {
        // CAST TO RecordValue
        var formulaValue = (RecordValue) FormulaValueJSON.FromJson(json);
        return From(formulaValue);
    }
}
