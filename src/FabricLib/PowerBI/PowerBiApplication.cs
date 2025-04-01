using AzureApplicationLib;
using Microsoft.Extensions.Logging;

namespace FabricLib;

public partial class PowerBiApplication : AzureApplication
{
    public PowerBiApplication(AzureApplicationConfig config = null, ILogger logger = null) : base(config, logger)
    {
    }
}