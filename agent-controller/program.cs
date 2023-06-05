using k8s;
using k8s.Models;

var pollInterval = Environment.GetEnvironmentVariable("POLL_INTERVAL") ?? "10000";
var targetNamespaceName = Environment.GetEnvironmentVariable("TARGET_NAMESPACE") ?? "ado-agent";

var kubernetesConfigurationPath = Environment.GetEnvironmentVariable("KUBECONFIG") ?? 
                                  String.Format("{0}/.kube/config", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

var kubernetesConfiguration = KubernetesClientConfiguration.LoadKubeConfig(kubernetesConfigurationPath);
var kubernetesClientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubernetesConfigurationPath, kubernetesConfiguration.CurrentContext);

var client = new Kubernetes(kubernetesClientConfiguration);

var namespaces = client.CoreV1.ListNamespace();

var targetNamespace = new V1Namespace {
  Metadata = new V1ObjectMeta {
    Name = targetNamespaceName
  }
};

if (!namespaces.Items.Any(item => item.Metadata.Name == targetNamespaceName)) {
  client.CoreV1.CreateNamespace(targetNamespace);
}
