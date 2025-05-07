using k8s;
using k8s.Models;

namespace AzureAksMgmtLib;

public class AksIdentityServiceConfig
{
    public string AppName { get; set; }
    public string NamespaceName { get; set; }
    public string KubeConfig { get; set; }
    public string ImageName { get; set; }
}


public class AksIdentityService(AksIdentityServiceConfig config, IServiceProvider sp)
{
    Kubernetes _client;

    Kubernetes Client
    {
        get
        {
            if (_client is not null) return _client;
            var k8sConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(config.KubeConfig);
            _client = new Kubernetes(k8sConfig);
            return _client;
        }
    }

    public async Task<(V1ObjectMeta meta, int port, Dictionary<string, string> labels)> NewAspNetCoreDeploymentAsync(string name, string password)
    {
        var meta = new V1ObjectMeta();
        meta.Name = name;

        var labels = new Dictionary<string, string>();
        labels.Add("app", name);
        meta.Labels = labels;

        var services = await Client.ListNamespacedServiceAsync(config.NamespaceName);
        var ports = services.Items.SelectMany(s => s.Spec.Ports).Select(p => p.Port).Distinct().ToArray();
        var existingPorts = ports.Where(xx => xx >= 9000 && xx < 10000).ToArray();
        var containerPort = 9001;
        if (existingPorts.Length > 0)
        {
            containerPort = existingPorts.Max() + 1;
        }

        // Update the container port to 8080
        var port = new V1ContainerPort();
        port.ContainerPort = containerPort;

        var env = new List<V1EnvVar>(new V1EnvVar[] {
            new V1EnvVar("NAME", name),
            new V1EnvVar("PORT", $"{containerPort}"),
            new V1EnvVar("PASSWORD", password),
            // Add an environment variable to tell ASP.NET Core to listen on port 8080
            new V1EnvVar("ASPNETCORE_URLS", $"http://+:{containerPort}")
        });

        var container = new V1Container();
        container.Name = name;
        container.Image = config.ImageName;
        container.ImagePullPolicy = "IfNotPresent";
        container.Ports = new[] { port };
        container.Env = env;

        var spec = new V1PodSpec();
        spec.Containers = new[] { container };

        var podTemplateSpec = new V1PodTemplateSpec();
        podTemplateSpec.Metadata = new V1ObjectMeta { Labels = labels };
        podTemplateSpec.Spec = spec;

        var deploymentSpec = new V1DeploymentSpec();
        deploymentSpec.Replicas = 1;
        deploymentSpec.Selector = new V1LabelSelector { MatchLabels = labels };
        deploymentSpec.Template = podTemplateSpec;

        var podBody = new V1Deployment();
        podBody.ApiVersion = "apps/v1";
        podBody.Kind = "Deployment";
        podBody.Metadata = meta;
        podBody.Spec = deploymentSpec;

        var createPodResponse = await Client.CreateNamespacedDeploymentAsync(podBody, config.NamespaceName);
        return (meta, containerPort, labels);
    }

    public async Task<V1ObjectMeta> NewServiceAsync(string name, int port, Dictionary<string, string> labels)
    {
        var serviceMeta = new V1ObjectMeta
        {
            Name = name,
            Labels = labels
        };

        // Update the service port to target port 8080
        var servicePort = new V1ServicePort
        {
            Port = port,
            TargetPort = port,
            Protocol = "TCP",
            Name = "http"
        };

        // Create the service spec with type LoadBalancer
        var serviceSpec = new V1ServiceSpec
        {
            Type = "LoadBalancer",
            Ports = new List<V1ServicePort> { servicePort },
            Selector = labels
        };

        // Create the service object
        var serviceBody = new V1Service
        {
            ApiVersion = "v1",
            Kind = "Service",
            Metadata = serviceMeta,
            Spec = serviceSpec
        };

        // Deploy the service
        var createdService = await Client.CreateNamespacedServiceAsync(serviceBody, config.NamespaceName);
        return serviceMeta;
    }

    async Task ListPodsInNamespaceAsync(string @namespace)
    {
        Console.WriteLine($"Listing pods in {@namespace} namespace:");

        var podList = await Client.ListNamespacedPodAsync(@namespace);

        if (podList.Items.Count == 0)
        {
            Console.WriteLine("  No pods found in this namespace.");
            return;
        }

        foreach (var pod in podList.Items)
        {
            Console.WriteLine($"  Pod: {pod.Metadata.Name}");
            Console.WriteLine($"    Status: {pod.Status.Phase}");

            if (pod.Status.ContainerStatuses != null)
            {
                foreach (var containerStatus in pod.Status.ContainerStatuses)
                {
                    Console.WriteLine($"    Container: {containerStatus.Name}, Ready: {containerStatus.Ready}");
                }
            }

            Console.WriteLine();
        }
    }

    async Task GetPodNamesAsync(string @namespace)
    {
        var podList = await Client.ListNamespacedPodAsync(@namespace);
        Console.WriteLine($"Pods in {@namespace} namespace:");

        foreach (var pod in podList.Items)
        {
            Console.WriteLine($"  {pod.Metadata.Name}");
        }
    }

    // New method to get logs from the last 5 minutes
    async Task PodLogsAsync(string appName, int? sinceMinutes = null, int? tailLines = null)
    {
        // Find pods in the namespace, optionally filtered by label selector
        var podList = await Client.ListNamespacedPodAsync(config.NamespaceName, labelSelector: $"app={appName}");

        if (podList.Items.Count == 0)
        {
            return;
        }

        var pod = podList.Items[0];

        // Get the container name (using the first container if there are multiple)
        var containerName = pod.Spec.Containers.First().Name;

        // Get logs for the last 5 minutes
        using var logStream = await Client.ReadNamespacedPodLogAsync(
            pod.Metadata.Name,
            config.NamespaceName,
            containerName,
            sinceSeconds: (sinceMinutes ?? 1) * 60,
            tailLines: tailLines ?? 1000
        );

        using var reader = new StreamReader(logStream);
        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line is null) break;

            Console.WriteLine(line);
        }
    }

    // Add this function to delete a pod by app name (forcing it to restart)
    async Task DeletePodAsync(string appName)
    {
        Console.WriteLine($"Finding pod with label app={appName} to delete...");

        // Find pods in the namespace with the given app label
        var podList = await Client.ListNamespacedPodAsync(config.NamespaceName, labelSelector: $"app={appName}");

        if (podList.Items.Count == 0)
        {
            Console.WriteLine($"No pods found with label app={appName}");
            return;
        }

        foreach (var pod in podList.Items)
        {
            Console.WriteLine($"Deleting pod: {pod.Metadata.Name}");

            try
            {
                // Delete the pod - no need for a specific delete options object
                await Client.DeleteNamespacedPodAsync(
                    pod.Metadata.Name,
                    config.NamespaceName
                );

                Console.WriteLine($"Pod {pod.Metadata.Name} deleted successfully. Kubernetes will automatically create a replacement pod.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting pod {pod.Metadata.Name}: {ex.Message}");
            }
        }
    }
}
