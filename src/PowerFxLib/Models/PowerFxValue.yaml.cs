using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace PowerFxLib.Models;

public partial struct PowerFxValue
{
    public static PowerFxValue FromYaml(string yaml)
    {
        var deserializer = new Deserializer();
        var xxx = deserializer.Deserialize<YamlNode>(yaml);
        return From(xxx);
    }

    public static PowerFxValue From(YamlNode node)
    {
        var returnValue = NULL;
        switch (node.NodeType)
        {
            case YamlNodeType.Mapping:
                returnValue.ValueType = PowerFxValueType.Set;
                returnValue.SetValue = new Dictionary<string, PowerFxValue>();
                var map = (YamlMappingNode)node;
                foreach (var (k, v) in map.Children)
                {
                    var kk = (YamlScalarNode)k;
                    var collection = From(v);
                    returnValue.SetValue.Add((string)kk, collection);
                }
                break;
            case YamlNodeType.Scalar:
                var scalar = (YamlScalarNode)node;
                returnValue = From(scalar.Value);
                break;
            default:
                returnValue.ValueType = PowerFxValueType.Error;
                returnValue.ErrorValue = $"Node {node.NodeType} not recognized";
                break;
        }
        return returnValue;
    }

}
