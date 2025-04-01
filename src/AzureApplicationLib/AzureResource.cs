using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureApplicationLib;

public static class AzureResource
{
    public static string Parse(string key)
    {
        if (key == nameof(Management)) return Management;
        if (key == nameof(PowerBI)) return PowerBI;
        return "";
    }
    public const string Management = "https://management.azure.com";
    public const string PowerBI = "https://analysis.windows.net/powerbi/api";
}
