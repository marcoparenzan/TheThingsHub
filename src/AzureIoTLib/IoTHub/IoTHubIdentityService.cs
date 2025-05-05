using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using AzureApplicationLib;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.DependencyInjection;

namespace AzureIoTLib.IoTHub;

public class IoTHubIdentityServiceConfig
{
    public string AppName { get; set; }
    public string HostName { get; set; }
}

public class IoTHubIdentityService(IoTHubIdentityServiceConfig config, IServiceProvider sp)
{
    AzureApplication app;

    protected AzureApplication App
    {
        get
        {
            if (app is not null) return app;
            app = sp.GetKeyedService<AzureApplication>(config.AppName);
            return app;
        }
    }

    public async Task CreateNewAsync(string clientName, string thumbprint)
    {
        var device = new Device(clientName)
        {
            Authentication = new AuthenticationMechanism()
            {
                X509Thumbprint = new X509Thumbprint()
                {
                    PrimaryThumbprint = thumbprint
                }
            }
        };
        var credential = App.Config.CreateClientSecretCredential();
        var registryManager = RegistryManager.Create(config.HostName, credential);
        await registryManager.AddDeviceAsync(device);
    }
}
