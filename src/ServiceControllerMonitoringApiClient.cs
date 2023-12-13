namespace Collector;

public interface IServiceControllerMonitoringApiClient
{
    Task<IReadOnlyList<Endpoint>> GetEndpointsAsync(CancellationToken cancellationToken);

}

public sealed class ServiceControllerMonitoringApiClient(IHttpClientFactory httpClientFactory) : IServiceControllerMonitoringApiClient
{
    public async Task<IReadOnlyList<Endpoint>> GetEndpointsAsync(CancellationToken cancellationToken)
    {
        const string path = "monitored-endpoints?history=1";
        var httpClient = httpClientFactory.CreateClient("ServiceControllerMonitoringApiClient");
        return await httpClient.GetFromJsonAsync<IReadOnlyList<Endpoint>>(path, cancellationToken) ?? [];
    }
}   

