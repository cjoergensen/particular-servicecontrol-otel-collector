using System.Reflection;

namespace Collector;

internal static class OpenTelemetryHelper
{
    private const string ServiceNameEnvVarName = "OTEL_SERVICE_NAME";
    private const string ServiceVersionEnvVarName = "OTEL_SERVICE_VERSION";
    private const string ServiceNamespaceEnvVarName = "OTEL_SERVICE_NAMESPACE";

    public static string GetServiceName(IConfiguration configuration)
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName();
        return configuration.GetValue(ServiceNameEnvVarName, assemblyName.FullName)!;
    }

    public static string GetServiceVersion(IConfiguration configuration)
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName();
        return configuration.GetValue(ServiceVersionEnvVarName, assemblyName.Version?.ToString())!;
    }

    public static string GetServiceNamespace(IConfiguration configuration)
    {
        return configuration.GetValue(ServiceNamespaceEnvVarName, "")!;
    }
}
