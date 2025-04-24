using AzureApplicationLib;
using CatalogLib;
using CatalogLib.Database;
using FabricLib;
using FabricLib.LakeHouse;
using KustoDashboardLib;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using MudBlazor.Services;
using PowerBIEmbeddingLib;
using System.Text.Json;
using TheThingsHubPortalApp.Components;
using TheThingsHubPortalApp.Services;
using UnifiedNamespaceLib.Services;
using UnifiedNamespaceLib.Services.Workers;

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

#region Fabric Config

var fabricAppConfig = new AzureApplicationConfig { 
    TenantId = config["FabricAppConfig:TenantId"],
    SubscriptionId = config["FabricAppConfig:SubscriptionId"],
    ClientId = config["FabricAppConfig:ClientId"],
    ClientSecret = config["FabricAppConfig:ClientSecret"],
    Username = config["FabricAppConfig:Username"],
    Password = config["FabricAppConfig:Password"],
    Resource = AzureResource.Parse(config["FabricAppConfig:Resource"])
};
builder.Services.AddKeyedSingleton("FabricAppConfig", fabricAppConfig);
builder.Services.AddKeyedTransient<FabricApp>("FabricApp");
var workspaceInfo = config.GetSection("FabricWorkspaceConfig").Get<WorkspaceConfig>();
builder.Services.AddKeyedSingleton<WorkspaceConfig>("FabricWorkspaceConfig", workspaceInfo);
builder.Services.AddTransient<LakeHouseService>(sp => new LakeHouseService(sp.GetKeyedService<FabricApp>("FabricApp"), workspaceInfo));

#endregion

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
            .AddInterceptors(new AzureAuthInterceptor(async () => {
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

builder.Services.AddScoped<CatalogService>();

//builder.Services.AddScoped<HmiJsInterop>();
builder.Services.AddScoped<KustoDashboardJsInterop>();

//#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//builder.Services.AddHybridCache();
//#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

builder.Services.AddKeyedTransient<IMessagingService, MqttClientService>("MqttClientTimers");
builder.Services.AddKeyedTransient<IMessagingService, MqttClientService>("MqttClientPortalApp");
builder.Services.AddKeyedTransient<IMessagingService, MqttClientService>("MqttClientFabricHook");
//builder.Services.AddTransient(sp => new UnifiedNamespaceContext(new DbContextOptionsBuilder<UnifiedNamespaceContext>().UseSqlServer(conf["Database:connectionString"]).Options));
//builder.Services.AddTransient<IRetainedMessageService, EFRetainedMessageService>();

builder.Services.AddKeyedTransient<IWorkerService, TimerServices>("timerService");
builder.Services.AddKeyedTransient<IWorkerService, VirtualSignalsService>("virtualSignalsService");
//builder.Services.AddKeyedTransient<IWorkerService, MqttBrokerService>("mqttBrokerService");

builder.Services.AddHostedService<WorkerServicesManager>();

builder.Services.AddTransient<PowerBIEmbeddingJsInterop>();

builder.Services.AddMudServices();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddMicrosoftIdentityConsentHandler();

builder.Services.AddProblemDetails();

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
await mqttClientFabricHook.ConnectAsync();

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
        var result = await timerApp.HttpClient.PostAsJsonAsync("https://api.fabric.microsoft.com/v1/workspaces/a1badeec-a4dc-480b-a8a6-7065c012dee1/items/cc610143-b66b-4fb8-b9a3-ef707e7bbef9/jobs/instances?jobType=Pipeline", new { 
        });
        return Results.Ok();
    }
    catch (Exception ex)
    {
        throw;
    }
});

app.Run();
