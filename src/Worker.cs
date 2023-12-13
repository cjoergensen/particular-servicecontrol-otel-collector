using System.Diagnostics.Metrics;

namespace Collector;

public sealed class Worker(IServiceControlApiClient serviceControlApiClient, IServiceControllerMonitoringApiClient serviceControllerMonitoringApiClient, IConfiguration configuration) : BackgroundService
{
    public const string MeterName = "servicecontrol";
    private int numberOfFailedMessages = 0;
    private Meter? serviceControlMeter;
    private readonly Dictionary<string, EndpointMetricGauge> endpointMetricGauges = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string serviceName = OpenTelemetryHelper.GetServiceName(configuration);
        string serviceVersion = OpenTelemetryHelper.GetServiceVersion(configuration);
        string serviceNamespace = OpenTelemetryHelper.GetServiceNamespace(configuration);

        serviceControlMeter = new(MeterName, serviceVersion);
        serviceControlMeter.CreateObservableGauge($"{MeterName}.failedmessages", () => numberOfFailedMessages, unit: "Messages", description: "Number of failed messages.");

        while (!stoppingToken.IsCancellationRequested)
        {
            numberOfFailedMessages = await serviceControlApiClient.GetNumberOfFailedMessages(stoppingToken);
            
            var endpoints = await serviceControllerMonitoringApiClient.GetEndpointsAsync(stoppingToken);
            foreach (var endpoint in endpoints)
            {
                CreateOrUpdateMetricGauge($"{MeterName}.{endpoint.Name}.{nameof(ProcessingTime)}", endpoint.Metrics.ProcessingTime.Average, "ms", "Processing time is the time it takes for an endpoint to successfully invoke all handlers and sagas for a single incoming message");
                CreateOrUpdateMetricGauge($"{MeterName}.{endpoint.Name}.{nameof(CriticalTime)}", endpoint.Metrics.CriticalTime.Average, "ms", "Critical time is the time between when a message is sent and when it is fully processed.");
                CreateOrUpdateMetricGauge($"{MeterName}.{endpoint.Name}.{nameof(QueueLength)}", endpoint.Metrics.QueueLength.Average, "msg", "This metric tracks the number of messages in the main input queue of an endpoint.");
                CreateOrUpdateMetricGauge($"{MeterName}.{endpoint.Name}.{nameof(Retries)}", endpoint.Metrics.Retries.Average, "count", "This metric measures the number of retries scheduled by the endpoint (immediate or delayed).");
                CreateOrUpdateMetricGauge($"{MeterName}.{endpoint.Name}.{nameof(Throughput)}", endpoint.Metrics.Throughput.Average, "msg/s", "This metric measures the total number of messages that the endpoint successfully processes per second.");
            }
            await Task.Delay(10_000, stoppingToken);
        }
    }

    void CreateOrUpdateMetricGauge(string key, double value, string unit, string description)
    {
        ArgumentNullException.ThrowIfNull(serviceControlMeter);
        key = key.ToLowerInvariant();
        if (!endpointMetricGauges.TryGetValue(key, out EndpointMetricGauge? metricGauge))
        {
            metricGauge = new(key, value, serviceControlMeter.CreateObservableGauge(key, () => endpointMetricGauges[key].Value, unit, description));
            endpointMetricGauges.Add(key, metricGauge);
        }

        metricGauge.Value = value;
    }
}

public sealed record EndpointMetricGauge
{
    public EndpointMetricGauge(string Key, double Value, ObservableGauge<double> Gauge)
    {
        this.Key = Key; 
        this.Value = Value;
        this.Gauge = Gauge;
    }

    public string Key { get; private set; }
    public ObservableGauge<double> Gauge{ get; private set; }

    public double Value { get; set; }
}