using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;

namespace Collector;

public sealed class Worker(IOptions<WorkerSettings> options, IServiceControlApiClient serviceControlApiClient, IServiceControlMonitoringApiClient serviceControllerMonitoringApiClient, IConfiguration configuration) : BackgroundService
{
    private Meter? serviceControlMeter;
    private int numberOfFailedMessages = 0;
    private readonly Dictionary<string, ObservableGauge<double>> metricGauges = [];
    private readonly Dictionary<string, double> metricValues = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string serviceName = OpenTelemetryHelper.GetServiceName(configuration);
        string serviceVersion = OpenTelemetryHelper.GetServiceVersion(configuration);
        string serviceNamespace = OpenTelemetryHelper.GetServiceNamespace(configuration);

        var settings = options.Value;
        serviceControlMeter = new(settings.MeterName, serviceVersion);
        serviceControlMeter.CreateObservableGauge($"{settings.MeterName}.failedmessages", () => numberOfFailedMessages, unit: "Messages", description: "Number of failed messages.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                numberOfFailedMessages = await serviceControlApiClient.GetNumberOfFailedMessages(stoppingToken);
                var endpoints = await serviceControllerMonitoringApiClient.GetEndpointsAsync(stoppingToken);
                foreach (var endpoint in endpoints)
                {
                    var processingTimeKey = $"{endpoint.Name}.{nameof(ProcessingTime)}";
                    SetMetricValue(processingTimeKey, endpoint.Metrics.ProcessingTime.Points);
                    CreateMetricGaugeIfNotExists(processingTimeKey, "ms", "Time it takes for an endpoint to successfully invoke all handlers and sagas for a single incoming message");
                    
                    var criticalTimeKey = $"{endpoint.Name}.{nameof(CriticalTime)}";
                    SetMetricValue(criticalTimeKey, endpoint.Metrics.CriticalTime.Points);
                    CreateMetricGaugeIfNotExists(criticalTimeKey, "ms", "Time between when a message is sent and when it is fully processed.");
                    
                    var queueLengthKey = $"{endpoint.Name}.{nameof(QueueLength)}";
                    SetMetricValue(queueLengthKey, endpoint.Metrics.QueueLength.Points);
                    CreateMetricGaugeIfNotExists(queueLengthKey, "msg", "Number of messages in the main input queue of an endpoint.");
                    
                    var retriesKey = $"{endpoint.Name}.{nameof(Retries)}";
                    SetMetricValue(retriesKey, endpoint.Metrics.Retries.Points);
                    CreateMetricGaugeIfNotExists(retriesKey, "count", "Number of retries scheduled by the endpoint (immediate or delayed).");
                    
                    var throughputKey = $"{endpoint.Name}.{nameof(Throughput)}";
                    SetMetricValue(throughputKey, endpoint.Metrics.Throughput.Points);
                    CreateMetricGaugeIfNotExists(throughputKey, "msg/s", "Number of messages that the endpoint successfully processes per second.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            await Task.Delay(settings.CollectionInterval, stoppingToken);
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