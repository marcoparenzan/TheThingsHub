using Microsoft.Extensions.Logging;

namespace AzureApplicationLib;

public partial class AzureApplication<TConfig>
    where TConfig : AzureApplicationConfig
{
    private ILogger logger;

    public TConfig Config { get; set; }

    public AzureApplication(TConfig config = null, ILogger logger = null)
    {
        this.Config = config;
        this.logger = logger ?? ConsoleLogger.Default;
    }
}
