namespace Collector;

public interface IServiceControlApiClient
{
    Task<int> GetNumberOfFailedMessages(CancellationToken cancellationToken);
}

public sealed class ServiceControlApiClient(IHttpClientFactory httpClientFactory) : IServiceControlApiClient
{
    public async Task<int> GetNumberOfFailedMessages(CancellationToken cancellationToken)
    {
        const string path = "api/errors?status=unresolved";

        var httpClient = httpClientFactory.CreateClient("ServiceControlApiClient");
        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, path), cancellationToken);
        response.EnsureSuccessStatusCode();

        response.Headers.TryGetValues("Total-Count", out var values);
        if (values == null || !values.Any())
        {
            return 0;
        }

        _ = int.TryParse(values.First(), out int totalCount);

        return totalCount;
    }
}