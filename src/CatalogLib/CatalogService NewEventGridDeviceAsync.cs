using CatalogLib.Database;
using CatalogLib.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace CatalogLib;

public partial class CatalogService
{
    public async Task<(bool ok, X509Certificate2 result)> NewEventGridDeviceAsync(ThingModel model, Func<X509Certificate2, Task<bool>> handleAsync)
    {
        var (_, caIntermediateRootCert) = await EnsureCertificatesAsync();

        var (_, device) = await EnsureThingAsync(model);
        var (_, devicePassword) = await EnsurePropertyAsync(device, "password", $"{Guid.NewGuid()}");
        var (_, deviceType) = await EnsurePropertyAsync(device, "deviceType", $"eventgrid-mqtt");
        var (_, deviceProperty, deviceCert) = await EnsureCertificatePropertyAsync(device, "DeviceCert", devicePassword, (name) =>
        {
            return certs.GenerateDeviceCertificate(caIntermediateRootCert, $"CN={model.Name}", devicePassword);
        });

        if (await handleAsync(deviceCert))
        {
            await context.SaveChangesAsync();
            return (true, deviceCert);
        }
        else
        {
            return (false, default);
        }
    }
}