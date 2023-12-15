using System.Diagnostics.Metrics;

namespace Collector;

public sealed class Worker(IServiceControlApiClient serviceControlApiClient, IServiceControlMonitoringApiClient serviceControllerMonitoringApiClient, IConfiguration configuration) : BackgroundService
{
    public const string MeterName = "servicecontrol";
    private Meter? serviceControlMeter;

    private int numberOfFailedMessages = 0;
    private readonly Dictionary<string, ObservableGauge<double>> metricGauges = [];
    private readonly Dictionary<string, double> metricValues = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string serviceName = OpenTelemetryHelper.GetServiceName(configuration);
        string serviceVersion = OpenTelemetryHelper.GetServiceVersion(configuration);
        string serviceNamespace = OpenTelemetryHelper.GetServiceNamespace(configuration);

        serviceControlMeter = new(MeterName, serviceVersion);
        serviceControlMeter.CreateObservableGauge($"{MeterName}.failedmessages", () => numberOfFailedMessages, unit: "Messages", description: "Number of failed messages.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                numberOfFailedMessages = await serviceControlApiClient.GetNumberOfFailedMessages(stoppingToken);
                var endpoints = await serviceControllerMonitoringApiClient.GetEndpointsAsync(stoppingToken);
                foreach (var endpoint in endpoints)
                {
                    SetMetricValue($"{endpoint.Name}.{nameof(ProcessingTime)}.avg", endpoint.Metrics.ProcessingTime.Average);
                    SetMetricValue($"{endpoint.Name}.{nameof(ProcessingTime)}", endpoint.Metrics.ProcessingTime.Points);
                    SetMetricValue($"{endpoint.Name}.{nameof(CriticalTime)}.avg", endpoint.Metrics.CriticalTime.Average);
                    SetMetricValue($"{endpoint.Name}.{nameof(CriticalTime)}", endpoint.Metrics.CriticalTime.Points);
                    SetMetricValue($"{endpoint.Name}.{nameof(QueueLength)}.avg", endpoint.Metrics.QueueLength.Average);
                    SetMetricValue($"{endpoint.Name}.{nameof(QueueLength)}", endpoint.Metrics.QueueLength.Points);
                    SetMetricValue($"{endpoint.Name}.{nameof(Retries)}.avg", endpoint.Metrics.Retries.Average);
                    SetMetricValue($"{endpoint.Name}.{nameof(Retries)}", endpoint.Metrics.Retries.Points);
                    SetMetricValue($"{endpoint.Name}.{nameof(Throughput)}.avg", endpoint.Metrics.Throughput.Average);
                    SetMetricValue($"{endpoint.Name}.{nameof(Throughput)}", endpoint.Metrics.Throughput.Points);


                    CreateMetricGaugeIfNotExists($"{endpoint.Name}.{nameof(ProcessingTime)}.avg",  "ms", "Average time it takes for an endpoint to successfully invoke all handlers and sagas for a single incoming message");
                    CreateMetricGaugeIfNotExists($"{endpoint.Name}.{nameof(ProcessingTime)}",      "ms", "Time it takes for an endpoint to successfully invoke all handlers and sagas for a single incoming message");
                    CreateMetricGaugeIfNotExists($"{endpoint.Name}.{nameof(CriticalTime)}.avg",    "ms", "Average time between when a message is sent and when it is fully processed.");
                    CreateMetricGaugeIfNotExists($"{endpoint.Name}.{nameof(CriticalTime)}",        "ms", "Time between when a message is sent and when it is fully processed.");
                    CreateMetricGaugeIfNotExists($"{endpoint.Name}.{nameof(QueueLength)}.avg",     "msg", "Average number of messages in the main input queue of an endpoint.");
                    CreateMetricGaugeIfNotExists($"{endpoint.Name}.{nameof(QueueLength)}",         "msg", "Number of messages in the main input queue of an endpoint.");
                    CreateMetricGaugeIfNotExists($"{endpoint.Name}.{nameof(Retries)}.avg",         "count", "Average number of retries scheduled by the endpoint (immediate or delayed).");
                    CreateMetricGaugeIfNotExists($"{endpoint.Name}.{nameof(Retries)}",             "count", "Number of retries scheduled by the endpoint (immediate or delayed).");
                    CreateMetricGaugeIfNotExists($"{endpoint.Name}.{nameof(Throughput)}.avg",      "msg/s", "Average number of messages that the endpoint successfully processes per second.");
                    CreateMetricGaugeIfNotExists($"{endpoint.Name}.{nameof(Throughput)}",          "msg/s", "Number of messages that the endpoint successfully processes per second.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            await Task.Delay(30_000, stoppingToken);
        }
    }

    void SetMetricValue(string key, double value)
    {
        key = key.ToLowerInvariant();
        if(!metricValues.TryAdd(key, value))
        {
            metricValues[key] = value;
        }
    }

    void SetMetricValue(string key, IReadOnlyList<double> points)
    {
        double value = 0;
        if(points.Count > 0)
            value = points[points.Count - 1];

        key = key.ToLowerInvariant();
        if (!metricValues.TryAdd(key, value))
        {
            metricValues[key] = value;
        }
    }


    void CreateMetricGaugeIfNotExists(string key, string unit, string description)
    {
        ArgumentNullException.ThrowIfNull(serviceControlMeter);
        
        key = key.ToLowerInvariant();
        if (metricGauges.ContainsKey(key))
            return;

        metricGauges.Add(key, serviceControlMeter.CreateObservableGauge(key, () => metricValues.GetValueOrDefault(key, 0), unit, description));

    }
}