using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using AzureApplicationLib;
using Microsoft.AspNetCore.StaticFiles;

namespace FabricLib.LakeHouse;

public partial class LakeHouseService(AzureApplication<AzureApplicationConfig> identity, WorkspaceConfig workspaceInfo)
{
    DataLakeServiceClient serviceClient;
    DataLakeFileSystemClient fileSystemClient;
    FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

    protected DataLakeFileSystemClient FileSystemClient
    {
        get
        {
            if (fileSystemClient is not null) return fileSystemClient;
            var uri = new Uri(workspaceInfo.Uri ?? "https://onelake.dfs.fabric.microsoft.com");
            serviceClient = new DataLakeServiceClient(uri, identity.Config.CreateClientSecretCredential());
            fileSystemClient = serviceClient.GetFileSystemClient(workspaceInfo.Name);
            return fileSystemClient;
        }
    }

    public async Task UploadFileAsync(string lakeHouseName, string folderPath, string filename, Stream data)
    {
        var directoryClient = FileSystemClient.GetDirectoryClient($"{lakeHouseName}.lakehouse/Files/{folderPath}");
        var fileClient = directoryClient.GetFileClient(filename);
        await fileClient.DeleteIfExistsAsync();
        var ct = provider.TryGetContentType(filename, out var contentType);
        var opt = new DataLakeFileUploadOptions()
        {
            Metadata = new Dictionary<string, string>()
            {
                ["ThingId"] = $"1",
                ["Type"] = Path.GetExtension(filename),
                ["ContentType"] = contentType
            }
        };
        await fileClient.UploadAsync(data, opt);
    }
}
