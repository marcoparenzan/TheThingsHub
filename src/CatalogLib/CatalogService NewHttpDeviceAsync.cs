using CatalogLib.Database;
using CatalogLib.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace CatalogLib;

public partial class CatalogService
{
    public async Task<(bool ok, string password, int port)> NewHttpDeviceAsync(ThingModel model, Func<string, Task<(bool result, int port)>> handleAsync)
    {
        var (_, caIntermediateRootCert) = await EnsureCertificatesAsync();

        var (_, device) = await EnsureThingAsync(model);
        var (_, devicePassword) = await EnsurePropertyAsync(device, "password", $"{Guid.NewGuid()}");
        var (_, deviceType) = await EnsurePropertyAsync(device, "deviceType", $"http");

        var (result, devicePort) = await handleAsync(device.Name);
        if (result)
        {
            var (_, devicePortString) = await EnsurePropertyAsync(device, "port", $"{devicePort}");
            await context.SaveChangesAsync();
            return (true, devicePassword, devicePort);
        }
        else
        {
            return (false, default, default);
        }
    }
}