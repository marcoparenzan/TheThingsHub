using AzureApplicationLib;
using Microsoft.Extensions.Logging;

namespace FabricLib;

public partial class PowerBiApplication : AzureApplication<AzureApplicationConfig>
{
    public PowerBiApplication(AzureApplicationConfig config = null, ILogger logger = null) : base(config, logger)
    {
    }
}