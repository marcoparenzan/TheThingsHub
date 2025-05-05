using CatalogLib.Database;
using CatalogLib.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace CatalogLib;

public partial class CatalogService
{
    private async Task<(bool result, X509Certificate2 certificate)> EnsureCertificatesAsync()
    {
        var (_, certificates) = await EnsureThingAsync("Certificates", "Certificates");
        var (_, certificatesPassword) = await EnsurePropertyAsync(certificates, "password", $"{Guid.NewGuid()}");

        var (_, caRoot, caRootCert) = await EnsureCertificatePropertyAsync(certificates, "RootCACert", certificatesPassword, (name) =>
        {
            return certs.GenerateCACertificate($"CN={name}", certificatesPassword);
        });

        var (_, caIntermediateRoot, caIntermediateRootCert) = await EnsureCertificatePropertyAsync(certificates, "IntermediateCACert", certificatesPassword, (name) =>
        {
            return certs.GenerateIntermediateCertificate(caRootCert, $"CN={name}", certificatesPassword);
        });
        return (true, caIntermediateRootCert);
    }

    private async Task<(bool ok, Thing result)> EnsureThingAsync(ThingModel model)=>await EnsureThingAsync(model.Name, model.Description);

    private async Task<(bool ok, Thing result)> EnsureThingAsync(string name, string description)
    {
        var item = await context.Things.SingleOrDefaultAsync(xx => xx.Name == name);
        if (item is null)
        {
            var newItem = new Thing
            {
                Name = name,
                Description = description ?? name,
            };
            var result = await context.AddAsync(newItem);
            item = result.Entity;
        }

        return (true, item);
    }

    private async Task<(bool ok, ThingsProperty? newProperty, X509Certificate2 newCert)> EnsureCertificatePropertyAsync(Thing thing, string name, string password, Func<string, X509Certificate2> generateNew)
    {
        var item = await context.ThingsProperties.SingleOrDefaultAsync(xx => xx.ThingId == thing.Id && xx.Name == name);
        X509Certificate2 cert = default;
        if (item is null)
        {
            cert = generateNew(name);
            var newItem  = new ThingsProperty
            {
                Thing = thing,
                Name = name,
                Type = "string",
                ContentType = "application/x-pkcs12",
                Value = certs.ToPfxBase64(cert, password)
            };
            var result = await context.AddAsync(newItem);
            item = result.Entity;
        }
        else
        {
            cert = certs.FromPfxBase64(item.Value, password);
        }
        return (true, item, cert);
    }

    private async Task<(bool result, TValue value)> EnsurePropertyAsync<TValue>(Thing thing, string propertyName, TValue newPropertyValue)
    {
        var item = await context.ThingsProperties.SingleOrDefaultAsync(xx => xx.ThingId == thing.Id && xx.Name == propertyName);
        TValue currentValue = default(TValue);
        if (item is null)
        {
            var newItem = new ThingsProperty
            {
                Thing = thing,
                Name = propertyName,
            };
            currentValue = Serialize(newPropertyValue, newItem);
            var result = await context.AddAsync(newItem);
            item = result.Entity;
        }
        else
        {
            currentValue = Deserialize<TValue>(item);
        }
        return (true, currentValue);
    }
}