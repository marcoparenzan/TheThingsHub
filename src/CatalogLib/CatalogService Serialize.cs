using CatalogLib.Database;
using CatalogLib.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace CatalogLib;

public partial class CatalogService
{
    private TValue Deserialize<TValue>(ThingsProperty item)
    {
        if (item.Type == "string")
        {
            if (typeof(TValue)  == typeof(string))
            {
                return (TValue)(object)item.Value;
            }
            else if (typeof(TValue) == typeof(int))
            {
                return (TValue)(object)int.Parse(item.Value);
            }
            else if (typeof(TValue) == typeof(bool))
            {
                return (TValue)(object)bool.Parse(item.Value);
            }
            else if (typeof(TValue) == typeof(byte[]))
            {
                return (TValue)(object)Convert.FromBase64String(item.Value);
            }
        }
        else if (item.Type == "int")
        {
            if (typeof(TValue) == typeof(string))
            {
                return (TValue)(object)item.Value;
            }
            else if (typeof(TValue) == typeof(int))
            {
                return (TValue)(object)int.Parse(item.Value);
            }
            else if (typeof(TValue) == typeof(bool))
            {
                return (TValue)(object)bool.Parse(item.Value);
            }
        }
        else if (item.Type == "bool")
        {
            if (typeof(TValue) == typeof(string))
            {
                return (TValue)(object)item.Value;
            }
            else if (typeof(TValue) == typeof(int))
            {
                return (TValue)(object)int.Parse(item.Value);
            }
            else if (typeof(TValue) == typeof(bool))
            {
                return (TValue)(object)bool.Parse(item.Value);
            }
        }
        else if (item.Type == "byte[]")
        {
            if (typeof(TValue) == typeof(string))
            {
                return (TValue)(object)item.Value;
            }
            else if (typeof(TValue) == typeof(int))
            {
                return (TValue)(object)int.Parse(item.Value);
            }
            else if (typeof(TValue) == typeof(bool))
            {
                return (TValue)(object)bool.Parse(item.Value);
            }
            else if (typeof(TValue) == typeof(byte[]))
            {
                return (TValue)(object)Convert.FromBase64String(item.Value);
            }
        }
        else if(item.Type == "json")
        {
            if (typeof(TValue) == typeof(string))
            {
                return (TValue)(object)item.Value;
            }
            else if (typeof(TValue) == typeof(int))
            {
                return (TValue)(object)int.Parse(item.Value);
            }
            else if (typeof(TValue) == typeof(bool))
            {
                return (TValue)(object)bool.Parse(item.Value);
            }
            else if (typeof(TValue) == typeof(byte[]))
            {
                return (TValue)(object)Convert.FromBase64String(item.Value);
            }
            else
            {
                return JsonSerializer.Deserialize<TValue>(item.Value);
            }
        }
        throw new NotSupportedException($"Unsupported type: {item.Type}");
    }

    private TValue Serialize<TValue>(TValue propertyValue, ThingsProperty newItem)
    {
        if (propertyValue is string strValue)
        {
            newItem.Type = "string";
            newItem.ContentType = "text/plain";
            newItem.Value = strValue;
        }
        else if (propertyValue is int intValue)
        {
            newItem.Type = "int";
            newItem.ContentType = "text/plain";
            newItem.Value = $"{intValue}";
        }
        else if (propertyValue is bool boolValue)
        {
            newItem.Type = "bool";
            newItem.ContentType = "text/plain";
            newItem.Value = $"{boolValue}";
        }
        else if (propertyValue is byte[] byteValue)
        {
            newItem.Type = "byte[]";
            newItem.ContentType = "text/plain";
            newItem.Value = Convert.ToBase64String(byteValue);
        }
        else
        {
            newItem.Type = "json";
            newItem.ContentType = "application/json";
            newItem.Value = JsonSerializer.Serialize(propertyValue);
        }
        return propertyValue;
    }
}