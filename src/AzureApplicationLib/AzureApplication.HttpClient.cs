using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;

namespace AzureApplicationLib;

public partial class AzureApplication
{
    private HttpClient httpClient;

    public virtual HttpClient HttpClient
    {
        get
        {
            if (this.httpClient != null) return httpClient;

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(authenticationResult.TokenType, authenticationResult.AccessToken);

            return httpClient;
        }
    }

    public Task<TValue?> GetFromJsonAsync<TValue>(string? requestUri, CancellationToken cancellationToken = default)
    {
        return HttpClient.GetFromJsonAsync<TValue>(requestUri, cancellationToken);
    }

    public async Task<HttpResponseMessage> PatchAsJsonAsync<TValue>(string? requestUri, TValue value)
    {
        var result = await HttpClient.PatchAsJsonAsync(requestUri, value);
        return result;
    }
}
