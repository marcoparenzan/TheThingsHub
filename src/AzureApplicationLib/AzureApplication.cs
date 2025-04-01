using Microsoft.Extensions.Logging;

namespace AzureApplicationLib;

public partial class AzureApplication
{
    private ILogger logger;

    public AzureApplicationConfig Config { get; set; }

    public AzureApplication(AzureApplicationConfig config = null, ILogger logger = null)
    {
        this.Config = config;
        this.logger = logger ?? ConsoleLogger.Default;
    }
}
