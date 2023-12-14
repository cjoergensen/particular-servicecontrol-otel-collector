using System.Diagnostics.Metrics;

namespace Collector;

public sealed class Worker(IServiceControlApiClient serviceControlApiClient, IServiceControlMonitoringApiClient serviceControllerMonitoringApiClient, IConfiguration configuration) : BackgroundService
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
            try
            {
                numberOfFailedMessages = await serviceControlApiClient.GetNumberOfFailedMessages(stoppingToken);
                var endpoints = await serviceControllerMonitoringApiClient.GetEndpointsAsync(stoppingToken);
                foreach (var endpoint in endpoints)
                {
                    CreateOrUpdateMetricGauge($"{endpoint.Name}.{nameof(ProcessingTime)}.avg", endpoint.Metrics.ProcessingTime.Average, "ms", "Average time it takes for an endpoint to successfully invoke all handlers and sagas for a single incoming message");
                    if (endpoint.Metrics.ProcessingTime.Points.Count > 0)
                        CreateOrUpdateMetricGauge($"{endpoint.Name}.{nameof(ProcessingTime)}", endpoint.Metrics.ProcessingTime.Points[endpoint.Metrics.ProcessingTime.Points.Count - 1], "ms", "Time it takes for an endpoint to successfully invoke all handlers and sagas for a single incoming message");

                    CreateOrUpdateMetricGauge($"{endpoint.Name}.{nameof(CriticalTime)}.avg", endpoint.Metrics.CriticalTime.Average, "ms", "Average time between when a message is sent and when it is fully processed.");
                    if (endpoint.Metrics.CriticalTime.Points.Count > 0)
                        CreateOrUpdateMetricGauge($"{endpoint.Name}.{nameof(CriticalTime)}", endpoint.Metrics.CriticalTime.Points[endpoint.Metrics.CriticalTime.Points.Count - 1], "ms", "Time between when a message is sent and when it is fully processed.");

                    CreateOrUpdateMetricGauge($"{endpoint.Name}.{nameof(QueueLength)}.avg", endpoint.Metrics.QueueLength.Average, "msg", "Average number of messages in the main input queue of an endpoint.");
                    if (endpoint.Metrics.QueueLength.Points.Count > 0)
                        CreateOrUpdateMetricGauge($"{endpoint.Name}.{nameof(QueueLength)}", endpoint.Metrics.QueueLength.Points[endpoint.Metrics.QueueLength.Points.Count - 1], "msg", "Number of messages in the main input queue of an endpoint.");

                    CreateOrUpdateMetricGauge($"{endpoint.Name}.{nameof(Retries)}.avg", endpoint.Metrics.Retries.Average, "count", "Average number of retries scheduled by the endpoint (immediate or delayed).");
                    if (endpoint.Metrics.Retries.Points.Count > 0)
                        CreateOrUpdateMetricGauge($"{endpoint.Name}.{nameof(Retries)}", endpoint.Metrics.Retries.Points[endpoint.Metrics.Retries.Points.Count - 1], "count", "Number of retries scheduled by the endpoint (immediate or delayed).");

                    CreateOrUpdateMetricGauge($"{endpoint.Name}.{nameof(Throughput)}.avg", endpoint.Metrics.Throughput.Average, "msg/s", "Average number of messages that the endpoint successfully processes per second.");
                    if (endpoint.Metrics.Throughput.Points.Count > 0)
                        CreateOrUpdateMetricGauge($"{endpoint.Name}.{nameof(Throughput)}", endpoint.Metrics.Throughput.Points[endpoint.Metrics.Throughput.Points.Count - 1], "msg/s", "Number of messages that the endpoint successfully processes per second.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            await Task.Delay(30_000, stoppingToken);
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