using Azure.Identity;
using Microsoft.Identity.Client;

namespace AzureApplicationLib;

public partial class AzureApplication
{
    private AuthenticationResult? authenticationResult;

    protected AuthenticationResult AuthenticationResult => authenticationResult;

    public async Task<AuthenticationResult> PublicAuthenticateAsync()
    {
        if (authenticationResult is not null)
            throw new InvalidOperationException("Already authenticated");

        authenticationResult = await Config.PublicAuthenticateAsync();

        return authenticationResult;
    }

    public async Task<AuthenticationResult> ConfidentialAuthenticateAsync()
    {
        if (authenticationResult is not null)
            throw new InvalidOperationException("Already authenticated");

        authenticationResult = await Config.ConfidentialAuthenticateAsync();

        return authenticationResult;
    }
}
