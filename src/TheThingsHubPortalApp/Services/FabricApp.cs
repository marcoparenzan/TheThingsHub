using AzureApplicationLib;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using FabricLib;
using Microsoft.Identity.Client;

namespace TheThingsHubPortalApp.Services;

public partial class FabricApp : PowerBiApplication
{
    public FabricApp([FromKeyedServices("FabricAppConfig")] AzureApplicationConfig config = null, ILogger logger = null) : base(config, logger)
    {
    }

    public async Task<AuthenticationResult> AuthenticateAsync()
    {
        return await ConfidentialAuthenticateAsync();
    }

    public async Task<Microsoft.PowerBI.Api.Models.Report> GetReportInGroupAsync(string workspaceId, string reportId)
    {
        var report = await this.PowerBiClient.Reports.GetReportInGroupAsync(Guid.Parse(workspaceId), Guid.Parse(reportId));
        return report;
    }

    public async Task<string> GenerateTokenAsync(string workspaceId, string reportId, string accessLevel = null)
    {
        var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: accessLevel ?? "view");
        var tokenResponse = await this.PowerBiClient.Reports.GenerateTokenAsync(
            Guid.Parse(workspaceId),
            Guid.Parse(reportId),
            generateTokenRequestParameters);
        return tokenResponse.Token;
    }
}