using AzureApplicationLib;
using CatalogLib;
using CatalogLib.Database;
using FabricLib;
using FabricLib.LakeHouse;
using KustoDashboardLib;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using MudBlazor.Services;
using PowerBIEmbeddingLib;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using TheThingsHubPortalApp.Components;
using TheThingsHubPortalApp.Services;
using UnifiedNamespaceLib.Services;
using WebLib;

var builder = WebApplication.CreateBuilder(args);
IConfigurationBuilder configBuilder = builder.Configuration;
var tenantConfigurationFile = Environment.GetEnvironmentVariable("TenantConfigurationFile");
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

var fabricAppConfig = RegisterAzureApplication<FabricApp>("FabricApp");
var iotMgmtAppConfig = RegisterAzureApplication<AzureApplication>("IoTMgmtApp");

builder.Services.AddKeyedSingleton<WorkspaceConfig>("FabricWorkspaceConfig", (sp, key) => config.GetSection("FabricWorkspaceConfig").Get<WorkspaceConfig>());
builder.Services.AddTransient<LakeHouseService>(sp =>
    new LakeHouseService(
        sp.GetKeyedService<FabricApp>("FabricApp"),
        sp.GetKeyedService<WorkspaceConfig>("FabricWorkspaceConfig")
    )
);

RegisterService<AzureEventGridMgmtLib.EventGridIdentityService, AzureEventGridMgmtLib.EventGridIdentityServiceConfig>("EventGridIdentityService", (valueOf) => new AzureEventGridMgmtLib.EventGridIdentityServiceConfig
{
    AppName = valueOf("AppName"),
    SubscriptionId = valueOf("SubscriptionId"),
    ResourceGroupName = valueOf("ResourceGroupName"),
    NamespaceName = valueOf("NamespaceName")
});
builder.Services.AddTransient<CatalogLib.CertificateService>();

RegisterService<AzureIoTHubMgmtLib.IoTHubIdentityService, AzureIoTHubMgmtLib.IoTHubIdentityServiceConfig>("IoTHubIdentityService", (valueOf) => new AzureIoTHubMgmtLib.IoTHubIdentityServiceConfig
{
    AppName = valueOf("AppName"),
    HostName = valueOf("HostName")
});

RegisterService<AzureAksMgmtLib.AksIdentityService, AzureAksMgmtLib.AksIdentityServiceConfig>("AksIdentityService", (valueOf) => new AzureAksMgmtLib.AksIdentityServiceConfig
{
    AppName = valueOf("AppName"),
    NamespaceName = valueOf("NamespaceName"),
    ImageName = valueOf("ImageName"),
    KubeConfig = Path.Combine(Path.GetDirectoryName(tenantConfigurationFile), valueOf("KubeConfig"))
});

#region Identity Config

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("EntraIdConfig"));
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Events.OnSignedOutCallbackRedirect = async context =>
    {
        context.HttpContext.Response.Redirect(context.Options.SignedOutRedirectUri ?? "/");
        context.HandleResponse();
    };
});

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy
    //options.FallbackPolicy = options.DefaultPolicy;
});

#endregion

#region Catalog

AuthenticationResult catalogResult = default;
builder.Services.AddSingleton(new DbContextOptionsBuilder<CatalogContext>()
            .UseSqlServer(config["Catalog:ConnectionString"])
            .AddInterceptors(new AzureAuthInterceptor(async () =>
            {
                // CHECK INVALIDATION
                if (catalogResult is null)
                {
                    catalogResult = await fabricAppConfig.ConfidentialAuthenticateAsync();
                }
                return catalogResult.AccessToken;

            }))
            .Options);
builder.Services.AddTransient<CatalogContext>();

#endregion

builder.Services.AddSingleton(new UriTools(config["UriTools:SigningKey"]));

builder.Services.AddScoped<CatalogService>();

//builder.Services.AddScoped<HmiJsInterop>();
builder.Services.AddScoped<KustoDashboardJsInterop>();

//#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//builder.Services.AddHybridCache();
//#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

//builder.Services.AddKeyedTransient<IMessagingService, MqttClientService>("MqttClientTimers");
//builder.Services.AddKeyedTransient<IMessagingService, MqttClientService>("MqttClientPortalApp");
//builder.Services.AddKeyedTransient<IMessagingService, MqttClientService>("MqttClientFabricHook");
//builder.Services.AddTransient(sp => new UnifiedNamespaceContext(new DbContextOptionsBuilder<UnifiedNamespaceContext>().UseSqlServer(conf["Database:connectionString"]).Options));
//builder.Services.AddTransient<IRetainedMessageService, EFRetainedMessageService>();

//builder.Services.AddKeyedTransient<IWorkerService, TimerServices>("timerService");
//builder.Services.AddKeyedTransient<IWorkerService, VirtualSignalsService>("virtualSignalsService");
//builder.Services.AddKeyedTransient<IWorkerService, MqttBrokerService>("mqttBrokerService");

//builder.Services.AddHostedService<WorkerServicesManager>();


builder.Services.AddTransient<PowerBIEmbeddingJsInterop>();

builder.Services.AddMudServices();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddMicrosoftIdentityConsentHandler();

builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.MapPost("/api/handleNewTopic", async (HttpContext ctx, IServiceProvider sp, JsonElement element) =>
{
    try
    {
        var messaging = sp.GetService<IMessagingService>();
        await messaging.PublishAsync(
            element.GetProperty("topic").GetString(),
            element.GetProperty("body")
        );
    }
    catch (Exception ex)
    {
        throw;
    }
});

var httpClient = new HttpClient();

var mqttClientFabricHook = app.Services.GetKeyedService<IMessagingService>("MqttClientFabricHook");
//await mqttClientFabricHook.ConnectAsync();

app.MapPost("/api/fabrichook", async (HttpContext ctx, IServiceProvider sp, JsonElement element) =>
{
    try
    {
        //var messaging = sp.GetService<IMessagingService>();
        //await messaging.PublishAsync(
        //    element.GetProperty("topic").GetString(),
        //    element.GetProperty("body")
        //);
        var json = JsonSerializer.Serialize(element);
        await mqttClientFabricHook.PublishAsync("devicestats/device24", element);
        var callBackUri = element.GetProperty("callBackUri").GetString();
        var result = await httpClient.PostAsJsonAsync(callBackUri, new
        {
        });
        return Results.Ok();
    }
    catch (Exception ex)
    {
        throw;
    }
});

var timerApp = app.Services.GetKeyedService<FabricApp>("FabricApp");
await timerApp.ConfidentialAuthenticateAsync();

app.MapPost("/api/pipelinetimer", async (HttpContext ctx, IServiceProvider sp, JsonElement element) =>
{
    try
    {
        var result = await timerApp.HttpClient.PostAsJsonAsync("https://api.fabric.microsoft.com/v1/workspaces/a1badeec-a4dc-480b-a8a6-7065c012dee1/items/cc610143-b66b-4fb8-b9a3-ef707e7bbef9/jobs/instances?jobType=Pipeline", new
        {
        });
        return Results.Ok();
    }
    catch (Exception ex)
    {
        throw;
    }
});
Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

app.MapGet("/api/catalog/things/{thingId}/properties/{propertyName}", async (HttpContext ctx, IServiceProvider sp, UriTools uriTools, int thingId, string propertyName) =>
{
    //if (IsRemoteRequest(ctx))
    //{
    //    if (!uriTools.Validate(ctx.Request.Query))
    //    {
    //        return Results.BadRequest("Invalid signature");
    //    }
    //}

    try
    {
        var catalog = sp.GetService<CatalogService>();
        var thingProperty = await catalog.GetThingPropertyAsync(thingId, propertyName);
        if (thingProperty is null)
        {
            return Results.Empty;
        }
        var extension = provider.Mappings.FirstOrDefault(x => x.Value == thingProperty.ContentType).Key;
        if (thingProperty.Type == "byte[]")
        {
            var bytes = Convert.FromBase64String(thingProperty.Value);
            return Results.File(bytes, thingProperty.ContentType, $"{thingId}-{propertyName}{extension}"); // {.pfx}
        }
        var bytes1 = Encoding.UTF8.GetBytes(thingProperty.Value);
        return Results.File(bytes1, thingProperty.ContentType, $"{thingId}-{propertyName}{extension}"); // {.pfx}
    }
    catch (Exception ex)
    {
        throw;
    }
});


app.MapGet("/api/catalog/things/{thingId}/certificate/{format}", async (HttpContext ctx, IServiceProvider sp, CertificateService cs, UriTools uriTools, int thingId, string? format) =>
{
    //if (IsRemoteRequest(ctx))
    //{
    //    if (!uriTools.Validate(ctx.Request.Query))
    //    {
    //        return Results.BadRequest("Invalid signature");
    //    }
    //}

    try
    {
        var catalog = sp.GetService<CatalogService>();
        var cert = await catalog.GetThingPropertyAsync(thingId, "DeviceCert");
        //if (cert is null)
        //{
        //    cert = await catalog.GetThingPropertyAsync(thingId, "IntermediateCACert");
        //}
        if (cert is null)
        {
            cert = await catalog.GetThingPropertyAsync(thingId, "RootCACert");
        }
        var password = await catalog.GetThingPropertyAsync(thingId, "password");
        var deviceCertBytes = Convert.FromBase64String(cert.Value);
        switch (format)
        {
            case "pem":
                var pfx = X509CertificateLoader.LoadPkcs12(deviceCertBytes, password.Value);
                var pem = cs.ExportToPem(pfx);
                var pemBytes = Encoding.UTF8.GetBytes(pem);
                // https://pki-tutorial.readthedocs.io/en/latest/mime.html
                return Results.File(pemBytes, "application/x-pem-file", $"{thingId}-{cert.Name}.pem ");
            default:
                return Results.File(deviceCertBytes, cert.ContentType, $"{thingId}-{cert.Name}.pfx");
        }
    }
    catch (Exception ex)
    {
        throw;
    }
});

app.Run();

AzureApplicationConfig RegisterAzureApplication<TApp>(string name, string sectionName = default)
    where TApp : AzureApplication
{
    sectionName ??= $"{name}Config";
    var cfg = new AzureApplicationConfig
    {
        TenantId = config[$"{sectionName}:TenantId"],
        ClientId = config[$"{sectionName}:ClientId"],
        ClientSecret = config[$"{sectionName}:ClientSecret"],
        Username = config[$"{sectionName}:Username"],
        Password = config[$"{sectionName}:Password"],
        Resource = AzureResource.Parse(config[$"{sectionName}:Resource"])
    };
    builder.Services.AddKeyedSingleton(sectionName, cfg);
    builder.Services.AddKeyedTransient<TApp>(name, (sp, key) =>
    {
        var cfg = sp.GetKeyedService<AzureApplicationConfig>(sectionName);
        var app = (TApp)Activator.CreateInstance(typeof(TApp), cfg);
        return app;
    });
    return cfg;
}

void RegisterService<TService, TConfig>(string name, Func<Func<string, string>, TConfig> newConfig, string sectionName = null)
    where TService: class
    where TConfig: class
{
    sectionName ??= $"{name}Config";
    var cfg = newConfig((key) => config[$"{sectionName}:{key}"]);
    builder.Services.AddKeyedSingleton(sectionName, cfg);
    builder.Services.AddKeyedTransient<TService>(name, (sp, key) =>
    {
        var cfg = sp.GetKeyedService<TConfig>(sectionName);
        var app = (TService) Activator.CreateInstance(typeof(TService), cfg, sp);
        return app;
    });
}

bool IsRemoteRequest(HttpContext context)
{
    // Get the current host
    var currentHost = context.Request.Host.Host;

    // Check Referer header
    if (context.Request.Headers.TryGetValue("Referer", out var referer))
    {
        var refererUri = new Uri(referer);
        if (!string.Equals(refererUri.Host, currentHost, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }

    // Check Origin header (useful for CORS requests)
    if (context.Request.Headers.TryGetValue("Origin", out var origin))
    {
        var originUri = new Uri(origin);
        if (!string.Equals(originUri.Host, currentHost, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }

    return false;
}