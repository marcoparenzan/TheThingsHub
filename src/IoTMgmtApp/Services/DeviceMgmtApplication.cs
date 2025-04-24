using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.EventGrid;
using Azure.ResourceManager.EventGrid.Models;
using AzureApplicationLib;

namespace IoTMgmtApp.Services;

public class DeviceMgmtApplicationConfig : AzureApplicationConfig
{
    public string ResourceGroupName { get; set; }
    public string NamespaceName { get; set; }
}


public class DeviceMgmtApplication(DeviceMgmtApplicationConfig config) : AzureApplication<DeviceMgmtApplicationConfig>(config: config)
{
    public async Task CreateNewAsync(string clientName, string thumbprint)
    {
        EventGridNamespaceClientCollection collection = ClientCollection();

        var clientCertificateAuthentication = new ClientCertificateAuthentication
        {
            ValidationScheme = ClientCertificateValidationScheme.ThumbprintMatch,
        };
        clientCertificateAuthentication.AllowedThumbprints.Add(thumbprint);

        var data = new EventGridNamespaceClientData
        {
            Description = $"{clientName}",
            ClientCertificateAuthentication = clientCertificateAuthentication,
            State = EventGridNamespaceClientState.Enabled,
            //Attributes = {
            //    ["deviceTypes"] = new BinaryData(new { 

            //        Hello= "World"

            //    })
            //}
        };

        var lro = await collection.CreateOrUpdateAsync(WaitUntil.Completed, clientName, data);
        EventGridNamespaceClientResource result = lro.Value;

        // the variable result is a resource, you could call other operations on this instance as well
        // but just for demo, we get its data from this resource instance
        EventGridNamespaceClientData resourceData = result.Data;
        // for demo we just print out the id
    }

    private EventGridNamespaceClientCollection ClientCollection()
    {
        var eventGridNamespace = Namespace();
        var collection = eventGridNamespace.GetEventGridNamespaceClients();
        return collection;
    }

    private EventGridNamespaceResource Namespace()
    {
        var cred = new ClientSecretCredential(Config.TenantId, Config.ClientId, Config.ClientSecret);

        // authenticate your client
        var client = new ArmClient(cred);

        // this example assumes you already have this EventGridNamespaceResource created on azure
        // for more information of creating EventGridNamespaceResource, please refer to the document of EventGridNamespaceResource
        var eventGridNamespaceResourceId = EventGridNamespaceResource.CreateResourceIdentifier(Config.SubscriptionId, Config.ResourceGroupName, Config.NamespaceName);
        var eventGridNamespace = client.GetEventGridNamespaceResource(eventGridNamespaceResourceId);
        return eventGridNamespace;
    }

    public async Task<string[]> GetClientsAsync()
    {
        var collection = ClientCollection();


        var result = collection.GetAllAsync().ToBlockingEnumerable().Select(xx => xx.Data.Name).ToArray();

        return result;
    }
}
