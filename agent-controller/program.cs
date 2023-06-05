using k8s;
using k8s.Models;
using System.Text;

var pollInterval = Environment.GetEnvironmentVariable("POLL_INTERVAL") ?? "10000";
var targetNamespaceName = Environment.GetEnvironmentVariable("TARGET_NAMESPACE") ?? "ado-agent";
var agentInstanceName = Environment.GetEnvironmentVariable("AGENT_INSTANCE_NAME") ?? "agent";
var agentImage = Environment.GetEnvironmentVariable("AGENT_IMAGE") ?? "docker.io/btungut/azure-devops-agent:2.214.1";
var agentPoolName = Environment.GetEnvironmentVariable("AGENT_POOL_NAME") ?? "AKS";
var agentWorkDirectory = Environment.GetEnvironmentVariable("AGENT_WORK_DIRECTORY") ?? "_work";
var personalAccessToken = Environment.GetEnvironmentVariable("PAT");
var organizationUrl = Environment.GetEnvironmentVariable("ORGANIZATION_URL") ?? "https://dev.azure.com/oshelu";


var kubernetesConfigurationPath = Environment.GetEnvironmentVariable("KUBECONFIG") ?? 
                                  String.Format("{0}/.kube/config", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

var kubernetesConfiguration = KubernetesClientConfiguration.LoadKubeConfig(kubernetesConfigurationPath);
var kubernetesClientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubernetesConfigurationPath, kubernetesConfiguration.CurrentContext);

var client = new Kubernetes(kubernetesClientConfiguration);

var targetNamespace = new V1Namespace {
  ApiVersion = "v1",
  Kind = "Namespace",
  Metadata = new V1ObjectMeta {
    Name = targetNamespaceName
  }
};

var namespaces = client.CoreV1.ListNamespace();
if (!namespaces.Items.Any(item => item.Metadata.Name == targetNamespaceName)) {
  client.CoreV1.CreateNamespace(targetNamespace);
}

var agentPod = new V1Pod {
  ApiVersion = "v1",
  Kind = "Pod",
  Metadata = new V1ObjectMeta {
    Name = String.Format("{0}-{1}", agentInstanceName, Guid.NewGuid().ToString()).Substring(0, 63),
    NamespaceProperty = targetNamespaceName
  },
  Spec = new V1PodSpec {
    Containers = new List<V1Container>() {
      new V1Container() {
        Name = "agent",
        Image = agentImage,
        ImagePullPolicy = "IfNotPresent",
        Env = new List<V1EnvVar>() {
          new V1EnvVar() {
            Name = "AZP_TOKEN",
            ValueFrom = {
              SecretKeyRef = {
                Name = String.Format("{0}-pat", agentInstanceName),
                Key = "pat",
                Optional = false
              }
            }
          },
          new V1EnvVar() {
            Name = "AZP_URL",
            Value = organizationUrl
          },
          new V1EnvVar() {
            Name = "AZP_POOL",
            Value = agentPoolName
          },
          new V1EnvVar() {
            Name = "AZP_WORK",
            Value = agentWorkDirectory
          },
        },
        Resources = new V1ResourceRequirements() {
          Requests = new Dictionary<string, ResourceQuantity> {
            { "cpu", new ResourceQuantity("100m") },
            { "memory", new ResourceQuantity("128Mi") }
          },
          Limits = new Dictionary<string, ResourceQuantity> {
            { "cpu", new ResourceQuantity("500m") },
            { "memory", new ResourceQuantity("512Mi") }
          }
        }
      }
    }
  }
};
