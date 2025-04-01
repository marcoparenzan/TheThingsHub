using Microsoft.Identity.Client;
using Microsoft.PowerBI.Api;
using Microsoft.Rest;

namespace FabricLib;

public partial class PowerBiApplication
{
    private PowerBIClient powerBiClient;

    public PowerBIClient PowerBiClient
    {
        get
        {
            if (powerBiClient != null) return powerBiClient;
            var credentials = new TokenCredentials(AuthenticationResult.AccessToken);

            // Create an instance of PowerBIClient using the TokenCredentials object
            powerBiClient = new PowerBIClient(credentials);
            return powerBiClient;
        }
    }
}
