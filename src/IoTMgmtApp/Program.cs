using AzureApplicationLib;
using IoTMgmtApp.Components;
using IoTMgmtApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

IConfigurationBuilder configBuilder = builder.Configuration;
var tenantConfigurationFile = Environment.GetEnvironmentVariable("TenantConfigurationFile");
var tenantConfigurationName = Environment.GetEnvironmentVariable("TenantConfigurationName");

Console.WriteLine($"{nameof(tenantConfigurationFile)}={tenantConfigurationFile}");
Console.WriteLine($"{nameof(tenantConfigurationName)}={tenantConfigurationName}");

var configurationReady = false;
if (!string.IsNullOrWhiteSpace(tenantConfigurationFile))
{
    try
    {
        if (tenantConfigurationFile.StartsWith("http"))
        {
            Console.WriteLine($"Loading configuration from web {tenantConfigurationFile}");
            using var configHttpClient = new HttpClient();
            using var stream = await configHttpClient.GetStreamAsync(tenantConfigurationFile);
            configBuilder = configBuilder.AddJsonStream(stream);
            stream.Close();
        }
        else
        {
            Console.WriteLine($"Loading configuration from file system {tenantConfigurationFile}");
            configBuilder = configBuilder.AddJsonFile(tenantConfigurationFile);
        }
        configurationReady = true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed loading configuration {tenantConfigurationFile}");
    }
}

if (!configurationReady)
{
    Console.WriteLine($"Failed to load configuration. Startup interrupted.");
    return;
}

var config = configBuilder.Build();

var appConfig = new DeviceMgmtApplicationConfig 
{ 
    TenantId = config[$"{tenantConfigurationName}:TenantId"],
    SubscriptionId = config[$"{tenantConfigurationName}:SubscriptionId"],
    ClientId = config[$"{tenantConfigurationName}:ClientId"],
    ClientSecret = config[$"{tenantConfigurationName}:ClientSecret"],
    ClientSecretName = config[$"{tenantConfigurationName}:ClientSecretName"],
    ClientSecretExpiration = DateTimeOffset.Parse(config[$"{tenantConfigurationName}:ClientSecretExpiration"]),
    Username = config[$"{tenantConfigurationName}:Username"],
    Password = config[$"{tenantConfigurationName}:Password"],
    Resource = AzureResource.Parse(config[$"{tenantConfigurationName}:Resource"]),
    ResourceGroupName = config[$"{tenantConfigurationName}:ResourceGroupName"],
    NamespaceName = config[$"{tenantConfigurationName}:NamespaceName"]
};

builder.Services.AddKeyedSingleton("iotmgmtAppConfig", appConfig);
builder.Services.AddKeyedTransient<DeviceMgmtApplication>("iotmgmtApp", (sp, key) => {
    var cfg = sp.GetKeyedService<DeviceMgmtApplicationConfig>($"iotmgmtAppConfig");
    var app = new DeviceMgmtApplication(cfg);
    return app;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/api/namespaces/{namespaceName}/clients", async (HttpContext ctx, [FromKeyedServices("iotmgmtApp")] DeviceMgmtApplication app, string namespaceName) =>
{
    await app.ConfidentialAuthenticateAsync();
    var clients = await app.GetClientsAsync();

    return Results.Ok(clients);
});

app.MapPost("/api/namespaces/{namespaceName}/clients", async (HttpContext ctx, [FromKeyedServices("iotmgmtApp")] DeviceMgmtApplication app, string namespaceName, [FromBody] JsonElement body) =>
{
    await app.ConfidentialAuthenticateAsync();
    await app.CreateNewAsync(
        body.GetProperty("clientName").GetString(),
        body.GetProperty("thumbprint").GetString()
    );

    return Results.Ok();
});


app.Run();
