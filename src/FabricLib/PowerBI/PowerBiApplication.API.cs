using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.PowerBI.Api.Models.Credentials;
using FabricLib.Models;
using System.Data;
using System.Text;

namespace FabricLib;

public partial class PowerBiApplication
{
    public async Task<EmbeddedReportViewModel> GetReport(Guid WorkspaceId, Guid ReportId)
    {

        // call to Power BI Service API to get embedding data
        var report = await powerBiClient.Reports.GetReportInGroupAsync(WorkspaceId, ReportId);

        // generate read-only embed token for the report
        var datasetId = report.DatasetId;
        var tokenRequest = new GenerateTokenRequest(TokenAccessLevel.View, datasetId);
        var embedTokenResponse = await powerBiClient.Reports.GenerateTokenAsync(WorkspaceId, ReportId, tokenRequest);
        var embedToken = embedTokenResponse.Token;

        // return report embedding data to caller
        return new EmbeddedReportViewModel
        {
            ReportId = report.Id.ToString(),
            EmbedUrl = report.EmbedUrl,
            Name = report.Name,
            Token = embedToken
        };
    }

    public Dataset GetDataset(Guid WorkspaceId, string DatasetName)
    {
        var datasets = powerBiClient.Datasets.GetDatasetsInGroup(WorkspaceId).Value;
        foreach (var dataset in datasets)
        {
            if (dataset.Name.Equals(DatasetName))
            {
                return dataset;
            }
        }
        return null;
    }

    public async Task<IList<Group>> GetTenantWorkspaces()
    {
        var workspaces = (await powerBiClient.Groups.GetGroupsAsync()).Value;
        return workspaces;
    }

    public PowerBiTenant OnboardNewTenant(PowerBiTenant tenant, string adminUser)
    {
        GroupCreationRequest request = new GroupCreationRequest("AODSK: " + tenant.Name);
        Group workspace = powerBiClient.Groups.CreateGroup(request);

        //// assign new workspace to dedicated capacity 
        //if (targetCapacityId != "")
        //{
        //    client.Groups.AssignToCapacity(workspace.Id, new AssignToCapacityRequest
        //    {
        //        CapacityId = new Guid(targetCapacityId),
        //    });
        //}

        tenant.WorkspaceId = workspace.Id.ToString();
        tenant.WorkspaceUrl = "https://app.powerbi.com/groups/" + workspace.Id.ToString() + "/";

        // This code adds service principal as workspace Contributor member
        // -- Adding service principal as member is required due to security bug when 
        // -- creating embed token for paginated report using service princpal profiles 
        var userServicePrincipal = powerBiClient.Groups.GetGroupUsers(workspace.Id).Value[0];
        string servicePrincipalObjectId = userServicePrincipal.Identifier;
        powerBiClient.Groups.AddGroupUser(workspace.Id, new GroupUser
        {
            Identifier = servicePrincipalObjectId,
            PrincipalType = PrincipalType.App,
            GroupUserAccessRight = "Contributor"
        });

        // add user as new workspace admin to make demoing easier
        if (!string.IsNullOrEmpty(adminUser))
        {
            powerBiClient.Groups.AddGroupUser(workspace.Id, new GroupUser
            {
                Identifier = adminUser,
                PrincipalType = PrincipalType.User,
                EmailAddress = adminUser,
                GroupUserAccessRight = "Admin"
            });
        }

        // upload sample PBIX file #1
        string importName = "Sales";
        PublishPBIX(workspace.Id, importName, default);

        Dataset dataset = GetDataset(workspace.Id, importName);

        UpdateMashupParametersRequest req = new UpdateMashupParametersRequest(new List<UpdateMashupParameterDetails>() {
            new UpdateMashupParameterDetails { Name = "DatabaseServer", NewValue = tenant.DatabaseServer },
            new UpdateMashupParameterDetails { Name = "DatabaseName", NewValue = tenant.DatabaseName }
        });

        powerBiClient.Datasets.UpdateParametersInGroup(workspace.Id, dataset.Id, req);

        PatchSqlDatasourceCredentials(workspace.Id, dataset.Id, tenant.DatabaseUserName, tenant.DatabaseUserPassword);

        powerBiClient.Datasets.RefreshDatasetInGroup(workspace.Id, dataset.Id);

        //if (targetCapacityId != "")
        //{
        //    // only import paginated report if workspace has been associated with dedicated capacity
        //    string PaginatedReportName = "Sales Summary";
        //    PublishRDL(workspace, PaginatedReportName, dataset, "");
        //}

        return tenant;
    }

    public PowerBiTenantDetails GetTenantDetails(PowerBiTenant tenant)
    {
        //SetCallingContext(tenant.ProfileId);

        return new PowerBiTenantDetails
        {
            Name = tenant.Name,
            DatabaseName = tenant.DatabaseName,
            DatabaseServer = tenant.DatabaseServer,
            DatabaseUserName = tenant.DatabaseUserName,
            DatabaseUserPassword = tenant.DatabaseUserPassword,
            ProfileId = tenant.ProfileId,
            Created = tenant.Created,
            WorkspaceId = tenant.WorkspaceId,
            WorkspaceUrl = tenant.WorkspaceUrl,
            Members = powerBiClient.Groups.GetGroupUsers(new Guid(tenant.WorkspaceId)).Value,
            Datasets = powerBiClient.Datasets.GetDatasetsInGroup(new Guid(tenant.WorkspaceId)).Value,
            Reports = powerBiClient.Reports.GetReportsInGroup(new Guid(tenant.WorkspaceId)).Value
        };

    }

    public void DeleteTenant(PowerBiTenant tenant)
    {
        // delete workspace as service principal profile
        //SetCallingContext(tenant.ProfileId);
        Guid workspaceIdGuid = new Guid(tenant.WorkspaceId);
        powerBiClient.Groups.DeleteGroup(workspaceIdGuid);

        // switch back to service principal to delete service principal profile
        //SetCallingContext();
        powerBiClient.Profiles.DeleteProfile(new Guid(tenant.ProfileId));
    }

    public void PublishPBIX(Guid workspaceId, string importName, Stream pbixStream)
    {
        var import = powerBiClient.Imports.PostImportWithFileInGroup(workspaceId, pbixStream, importName);

        while (import.ImportState != "Succeeded")
        {
            import = powerBiClient.Imports.GetImportInGroup(workspaceId, import.Id);
        }
    }

    public void PatchSqlDatasourceCredentials(Guid WorkspaceId, string DatasetId, string SqlUserName, string SqlUserPassword)
    {
        var datasources = (powerBiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId)).Value;

        // find the target SQL datasource
        foreach (var datasource in datasources)
        {
            if (datasource.DatasourceType.ToLower() == "sql")
            {
                // get the datasourceId and the gatewayId
                var datasourceId = datasource.DatasourceId;
                var gatewayId = datasource.GatewayId;
                // Create UpdateDatasourceRequest to update Azure SQL datasource credentials
                UpdateDatasourceRequest req = new UpdateDatasourceRequest
                {
                    CredentialDetails = new CredentialDetails(
                    new BasicCredentials(SqlUserName, SqlUserPassword),
                    PrivacyLevel.None,
                    EncryptedConnection.NotEncrypted)
                };
                // Execute Patch command to update Azure SQL datasource credentials
                powerBiClient.Gateways.UpdateDatasource((Guid)gatewayId, (Guid)datasourceId, req);
            }
        };
    }

    public void PublishRDL(Group workspace, string importName, Dataset targetDataset, Stream rdlStream)
    {
        var reader = new StreamReader(rdlStream);
        var rdlFileContent = reader.ReadToEnd();

        rdlFileContent = rdlFileContent.Replace("{{TargetDatasetId}}", targetDataset.Id.ToString())
                                       .Replace("{{PowerBIWorkspaceName}}", workspace.Name)
                                       .Replace("{{PowerBIDatasetName}}", targetDataset.Name);

        var contentSteam = new MemoryStream(Encoding.ASCII.GetBytes(rdlFileContent));

        var rdlImportName = importName + ".rdl";

        var import = powerBiClient.Imports.PostImportWithFileInGroup(workspace.Id, contentSteam, rdlImportName, ImportConflictHandlerMode.Abort);

        // poll to determine when import operation has complete
        do { import = powerBiClient.Imports.GetImportInGroup(workspace.Id, import.Id); }
        while (import.ImportState.Equals("Publishing"));

        Guid reportId = import.Reports[0].Id;

    }

    public async Task<EmbeddedReportViewModel> GetReportEmbeddingData(PowerBiTenant Tenant)
    {
        //SetCallingContext(Tenant.ProfileId);

        var workspaceId = new Guid(Tenant.WorkspaceId);
        var reports = (await powerBiClient.Reports.GetReportsInGroupAsync(workspaceId)).Value;

        var report = reports.Where(report => report.Name.Equals("Sales")).First();

        var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "View");

        // call to Power BI Service API and pass GenerateTokenRequest object to generate embed token
        var embedToken = powerBiClient.Reports.GenerateTokenInGroup(workspaceId, report.Id,
                                                                   generateTokenRequestParameters).Token;

        return new EmbeddedReportViewModel
        {
            ReportId = report.Id.ToString(),
            Name = report.Name,
            EmbedUrl = report.EmbedUrl,
            Token = embedToken,
            TenantName = Tenant.Name
        };
    }
    
    public async Task<EmbeddedViewModel> GetEmbeddedViewModel(string user)
    {
        PowerBIUser currentUser = default;

        if (currentUser.TenantName == null || currentUser.TenantName == "")
        {
            return new EmbeddedViewModel { tenantName = "" };
        }

        PowerBiTenant currentTenant = default;

        //SetCallingContext(currentTenant.ProfileId);

        Guid workspaceId = new Guid(currentTenant.WorkspaceId);

        var datasets = (await powerBiClient.Datasets.GetDatasetsInGroupAsync(workspaceId)).Value;
        var embeddedDatasets = new List<EmbeddedDataset>();
        foreach (var dataset in datasets)
        {
            embeddedDatasets.Add(new EmbeddedDataset
            {
                id = dataset.Id,
                name = dataset.Name,
                createReportEmbedURL = dataset.CreateReportEmbedURL
            });
        }

        var reports = (await powerBiClient.Reports.GetReportsInGroupAsync(workspaceId)).Value;
        var embeddedReports = new List<EmbeddedReport>();
        foreach (var report in reports)
        {
            embeddedReports.Add(new EmbeddedReport
            {
                id = report.Id.ToString(),
                name = report.Name,
                embedUrl = report.EmbedUrl,
                datasetId = report.DatasetId,
                reportType = report.ReportType
            });
        }

        IList<GenerateTokenRequestV2Dataset> datasetRequests = new List<GenerateTokenRequestV2Dataset>();
        IList<string> datasetIds = new List<string>();

        foreach (var dataset in datasets)
        {
            datasetRequests.Add(new GenerateTokenRequestV2Dataset(dataset.Id, xmlaPermissions: XmlaPermissions.ReadOnly));
            datasetIds.Add(dataset.Id);
        };

        IList<GenerateTokenRequestV2Report> reportRequests = new List<GenerateTokenRequestV2Report>();
        foreach (var report in reports)
        {
            Boolean userCanEdit = currentUser.CanEdit && report.ReportType.Equals("PowerBIReport");
            reportRequests.Add(new GenerateTokenRequestV2Report(report.Id, allowEdit: userCanEdit));
        };

        var workspaceRequests = new List<GenerateTokenRequestV2TargetWorkspace>();
        if (currentUser.CanCreate)
        {
            workspaceRequests.Add(new GenerateTokenRequestV2TargetWorkspace(workspaceId));
        }

        GenerateTokenRequestV2 tokenRequest =
          new GenerateTokenRequestV2
          {
              Datasets = datasetRequests,
              Reports = reportRequests,
              TargetWorkspaces = workspaceRequests,
              //LifetimeInMinutes = embedTokenLifetime
          };

        // call to Power BI Service API and pass GenerateTokenRequest object to generate embed token
        var EmbedTokenResult = powerBiClient.EmbedToken.GenerateToken(tokenRequest);

        return new EmbeddedViewModel
        {
            tenantName = currentUser.TenantName,
            reports = embeddedReports,
            datasets = embeddedDatasets,
            embedToken = EmbedTokenResult.Token,
            embedTokenId = EmbedTokenResult.TokenId.ToString(),
            embedTokenExpiration = EmbedTokenResult.Expiration,
            user = currentUser.LoginId,
            userCanEdit = currentUser.CanEdit,
            userCanCreate = currentUser.CanCreate
        };

    }

    public async Task<EmbedTokenResult> GetEmbedToken(string user)
    {
        PowerBIUser currentUser = default;

        if (currentUser.TenantName == null || currentUser.TenantName == "")
        {
            throw new ApplicationException("User not assigned to tenant");
        }

        PowerBiTenant currentTenant = default;

        Guid workspaceId = new Guid(currentTenant.WorkspaceId);

        //SetCallingContext(currentTenant.ProfileId);

        var reports = (await powerBiClient.Reports.GetReportsInGroupAsync(workspaceId)).Value;
        var datasets = (await powerBiClient.Datasets.GetDatasetsInGroupAsync(workspaceId)).Value;

        IList<GenerateTokenRequestV2Dataset> datasetRequests = new List<GenerateTokenRequestV2Dataset>();
        foreach (var dataset in datasets)
        {
            datasetRequests.Add(new GenerateTokenRequestV2Dataset(dataset.Id, xmlaPermissions: XmlaPermissions.ReadOnly));
        };

        IList<GenerateTokenRequestV2Report> reportRequests = new List<GenerateTokenRequestV2Report>();
        foreach (var report in reports)
        {
            Boolean userCanEdit = currentUser.CanEdit && report.ReportType.Equals("PowerBIReport");
            reportRequests.Add(new GenerateTokenRequestV2Report(report.Id, allowEdit: userCanEdit));
        };

        var workspaceRequests = new List<GenerateTokenRequestV2TargetWorkspace>();
        if (currentUser.CanCreate)
        {
            workspaceRequests.Add(new GenerateTokenRequestV2TargetWorkspace(workspaceId));
        }

        GenerateTokenRequestV2 tokenRequest =
          new GenerateTokenRequestV2
          {
              Datasets = datasetRequests,
              Reports = reportRequests,
              TargetWorkspaces = workspaceRequests,
              //LifetimeInMinutes = embedTokenLifetime
          };

        var tokenResult = powerBiClient.EmbedToken.GenerateToken(tokenRequest);

        // call to Power BI Service API and pass GenerateTokenRequest object to generate embed token
        return new EmbedTokenResult
        {
            embedToken = tokenResult.Token,
            embedTokenId = tokenResult.TokenId.ToString(),
            embedTokenExpiration = tokenResult.Expiration
        };

    }

    public async Task<ExportedReport> ExportFile(string user, ExportFileRequestParams request)
    {
        PowerBIUser currentUser = default;

        if (currentUser.TenantName == null || currentUser.TenantName == "")
        {
            throw new ApplicationException("User not assigned to tenant");
        }

        PowerBiTenant currentTenant = default;

        Guid workspaceId = new Guid(currentTenant.WorkspaceId);
        Guid reportId = new Guid(request.ReportId);

        FileFormat fileFormat;
        switch (request.ExportType.ToLower())
        {
            case "pdf":
                fileFormat = FileFormat.PDF;
                break;
            case "pptx":
                fileFormat = FileFormat.PPTX;
                break;
            case "png":
                fileFormat = FileFormat.PNG;
                break;
            default:
                throw new ApplicationException("Power BI reports do not support exort to " + request.ExportType);
        }

        //SetCallingContext(currentTenant.ProfileId);

        var exportRequest = new ExportReportRequest
        {
            Format = fileFormat,
            PowerBIReportConfiguration = new PowerBIReportExportConfiguration()
        };

        if (!string.IsNullOrEmpty(request.Filter))
        {
            string[] filters = request.Filter.Split(";");
            exportRequest.PowerBIReportConfiguration.ReportLevelFilters = new List<ExportFilter>();
            foreach (string filter in filters)
            {
                exportRequest.PowerBIReportConfiguration.ReportLevelFilters.Add(new ExportFilter(filter));
            }
        }

        if (!string.IsNullOrEmpty(request.BookmarkState))
        {
            exportRequest.PowerBIReportConfiguration.DefaultBookmark = new PageBookmark { State = request.BookmarkState };
        }

        if (!string.IsNullOrEmpty(request.PageName))
        {
            exportRequest.PowerBIReportConfiguration.Pages = new List<ExportReportPage>(){
          new ExportReportPage{PageName = request.PageName}
        };
            if (!string.IsNullOrEmpty(request.VisualName))
            {
                exportRequest.PowerBIReportConfiguration.Pages[0].VisualName = request.VisualName;
            }
        }

        Export export = await powerBiClient.Reports.ExportToFileInGroupAsync(workspaceId, reportId, exportRequest);

        string exportId = export.Id;

        do
        {
            System.Threading.Thread.Sleep(3000);
            export = powerBiClient.Reports.GetExportToFileStatusInGroup(workspaceId, reportId, exportId);
        } while (export.Status != ExportState.Succeeded && export.Status != ExportState.Failed);

        if (export.Status == ExportState.Failed)
        {
            Console.WriteLine("Export failed!");
        }

        if (export.Status == ExportState.Succeeded)
        {
            return new ExportedReport
            {
                ReportName = export.ReportName,
                ResourceFileExtension = export.ResourceFileExtension,
                ReportStream = powerBiClient.Reports.GetFileOfExportToFileInGroup(workspaceId, reportId, exportId)
            };
        }
        else
        {
            return null;
        }
    }
}