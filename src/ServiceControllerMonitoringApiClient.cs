using System.Net.Http.Json;

namespace Collector;

public sealed class ServiceControllerMonitoringApiClient(IHttpClientFactory httpClientFactory) : IServiceControllerMonitoringApiClient
{
    public async Task<List<Endpoint>?> GetEndpointsAsync(CancellationToken cancellationToken)
    {
        const string path = "monitored-endpoints?history=1";

        var httpClient = httpClientFactory.CreateClient("ServiceControllerMonitoringApiClient");
        return await httpClient.GetFromJsonAsync<List<Endpoint>>(path, cancellationToken);
    }
}

public interface IServiceControllerMonitoringApiClient
{
    Task<List<Endpoint>?> GetEndpointsAsync(CancellationToken cancellationToken);

}