using Microsoft.Extensions.Logging;

namespace AzureApplicationLib;

public partial class AzureApplication
{
    public AzureApplicationConfig Config { get; private set; }

    public AzureApplication(AzureApplicationConfig config = null)
    {
        this.Config = config;
    }
}
