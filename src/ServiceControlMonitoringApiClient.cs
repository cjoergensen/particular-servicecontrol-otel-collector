﻿namespace Collector;

public interface IServiceControlMonitoringApiClient
{
    public const string HttpClientName = "ServiceControlMonitoringApiClient";
    Task<IReadOnlyList<Endpoint>> GetEndpointsAsync(CancellationToken cancellationToken);

}

public sealed class ServiceControlMonitoringApiClient(IHttpClientFactory httpClientFactory) : IServiceControlMonitoringApiClient
{
    public async Task<IReadOnlyList<Endpoint>> GetEndpointsAsync(CancellationToken cancellationToken)
    {
        const string path = "monitored-endpoints?history=30";
        var httpClient = httpClientFactory.CreateClient(IServiceControlMonitoringApiClient.HttpClientName);
        return await httpClient.GetFromJsonAsync<IReadOnlyList<Endpoint>>(path, cancellationToken) ?? [];
    }
}   

