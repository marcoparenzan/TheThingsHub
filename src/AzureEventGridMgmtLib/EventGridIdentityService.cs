using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.EventGrid;
using Azure.ResourceManager.EventGrid.Models;
using AzureApplicationLib;
using Microsoft.Extensions.DependencyInjection;

namespace AzureEventGridMgmtLib;

public class EventGridIdentityServiceConfig
{
    public string AppName { get; set; }
    public string SubscriptionId { get; set; }
    public string ResourceGroupName { get; set; }
    public string NamespaceName { get; set; }
}

public class EventGridIdentityService(EventGridIdentityServiceConfig config, IServiceProvider sp)
{
    AzureApplication app;

    protected AzureApplication App
    {
        get
        {
            if (app is not null) return app;    
            app = sp.GetKeyedService<AzureApplication>(config.AppName);
            return app;
        }
    }

    public async Task CreateNewAsync(string clientName, string thumbprint)
    {
        var clients = ClientCollection();

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

        var lro = await clients.CreateOrUpdateAsync(WaitUntil.Completed, clientName, data);
        var result = lro.Value;

        var groups = ClientGroupCollection();

        var groupName = "default";
        var groupExistsResponse = await groups.GetIfExistsAsync(groupName);
        if (!groupExistsResponse.HasValue)
        {
            await EnsureClientGroupAsync(groups, groupName, $"{clientName}");
        }
        else
        {
            await EnsureClientGroupAsync(groups, groupName, $"{groupExistsResponse.Value.Data.Description};{clientName}");
        }

        // the variable result is a resource, you could call other operations on this instance as well
        // but just for demo, we get its data from this resource instance
        var resourceData = result.Data;
        // for demo we just print out the id
    }

    private async Task EnsureClientGroupAsync(EventGridNamespaceClientGroupCollection groups, string groupName, string description)
    {
        var query = $"authenticationName in [{string.Join(',', description.Split(';').Select(xx => $"'{xx}'"))}]";
        var data1 = new EventGridNamespaceClientGroupData
        {
            Description = description,
            Query = query
        };
        var result1 = await groups.CreateOrUpdateAsync(WaitUntil.Completed, groupName, data1);
    }

    private EventGridNamespaceClientCollection ClientCollection()
    {
        var eventGridNamespace = Namespace();
        var collection = eventGridNamespace.GetEventGridNamespaceClients();
        return collection;
    }

    private EventGridNamespaceClientGroupCollection ClientGroupCollection()
    {
        var eventGridNamespace = Namespace();
        var collection = eventGridNamespace.GetEventGridNamespaceClientGroups();
        return collection;
    }

    private EventGridNamespaceResource Namespace()
    {
        var cred = new ClientSecretCredential(App.Config.TenantId, App.Config.ClientId, App.Config.ClientSecret);

        // authenticate your client
        var client = new ArmClient(cred);

        // this example assumes you already have this EventGridNamespaceResource created on azure
        // for more information of creating EventGridNamespaceResource, please refer to the document of EventGridNamespaceResource
        var eventGridNamespaceResourceId = EventGridNamespaceResource.CreateResourceIdentifier(config.SubscriptionId, config.ResourceGroupName, config.NamespaceName);
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
