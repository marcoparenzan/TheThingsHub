using Azure.Identity;
using Microsoft.Identity.Client;
using System.Text.Json.Serialization;

namespace AzureApplicationLib;

public partial class AzureApplicationConfig
{
    private AzureApplicationConfig() { }

    public static AzureApplicationConfig New(string tenantId = null, string subscriptionId = null, string clientId = null, string clientSecret = null, string username = null, string password = null, string resource = null)
    {
        return new AzureApplicationConfig
        {
            TenantId = tenantId,
            SubscriptionId = subscriptionId,
            ClientId = clientId,
            ClientSecret = clientSecret,
            Username = username,
            Password = password,
            Resource = resource
        };
    }

    public string AdminConsentUrl => $"https://login.microsoftonline.com/{TenantId}/adminconsent?client_id={ClientId}";

    public static readonly AzureApplicationConfig Empty = new AzureApplicationConfig();

    public bool IsAdmin { get => this == Empty; }

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; }

    [JsonPropertyName("subscriptionId")]
    public string SubscriptionId { get; set; }

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; }

    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; }

    [JsonPropertyName("adminUserName")]
    public string Username { get; set; }

    [JsonPropertyName("adminPassword")]
    public string Password { get; set; }

    string authority;

    public string Authority
    {
        get
        {
            return authority ?? $"https://login.microsoftonline.com/{TenantId}/v2.0";
        }
        set
        {
            authority = value;

        }
    }

    public string Resource { get; init; }

    string[] scopes;

    public string[] Scopes
    {
        get
        {
            return scopes ?? [$"{Resource}/.default"];
        }
        set
        {
            scopes = value;

        }
    }

    public ClientSecretCredential CreateClientSecretCredential()
    {
        var credentials = new ClientSecretCredential(
            TenantId,
            ClientId,
            ClientSecret
        );
        return credentials;
    }


    public async Task<AuthenticationResult> PublicAuthenticateAsync()
    {
        var app = PublicClientApplicationBuilder.Create(ClientId)
          .WithTenantId(TenantId)
          .WithAuthority(Authority)
          .Build();

        //var accessTokenRequestBuilder = app.AcquireTokenForClient(scopes);
        var accessTokenRequestBuilder = app.AcquireTokenByUsernamePassword(Scopes, Username, Password);
        var ar = await accessTokenRequestBuilder.ExecuteAsync();

        return ar;
    }

    public async Task<AuthenticationResult> ConfidentialAuthenticateAsync()
    {
        var app = ConfidentialClientApplicationBuilder.Create(ClientId)
          .WithTenantId(TenantId)
          .WithClientSecret(ClientSecret)
          .WithAuthority(Authority)
          .Build();

        var accessTokenRequestBuilder = app.AcquireTokenForClient(Scopes);
        var ar = await accessTokenRequestBuilder.ExecuteAsync();

        return ar;
    }
}