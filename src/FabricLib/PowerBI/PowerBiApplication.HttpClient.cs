using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;

namespace FabricLib;

public partial class PowerBiApplication
{
    private HttpClient httpClient;

    public override HttpClient HttpClient
    {
        get
        {
            if (this.httpClient != null) return httpClient;

            httpClient = PowerBiClient.HttpClient;
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(AuthenticationResult.TokenType, AuthenticationResult.AccessToken);

            return httpClient;
        }
    }
}
