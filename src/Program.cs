using Collector;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string MeterNameEnvVarName = "METER_NAME";
const string CollectionIntervalEnvVarName = "COLLECTION_INTERVAL";
const string ServiceControlApiUrlEnvVarName = "SERVICE_CONTROL_API_URL";
const string ServiceControlMonitoringApiUrlEnvVarName = "SERVICE_CONTROL_MONITORING_API_URL";

var builder = WebApplication.CreateBuilder(args);

string meterName = builder.Configuration.GetValue(MeterNameEnvVarName, "servicecontrol")!;

builder.Services.Configure<WorkerSettings>(workersettings => 
{
    workersettings.CollectionInterval = builder.Configuration.GetValue(CollectionIntervalEnvVarName, TimeSpan.FromSeconds(30)); ;
    workersettings.MeterName = meterName;
});

ArgumentException.ThrowIfNullOrWhiteSpace(builder.Configuration[ServiceControlApiUrlEnvVarName]);
Uri serviceControlApiUri = new(builder.Configuration[ServiceControlApiUrlEnvVarName]!);
builder.Services.AddHttpClient(IServiceControlApiClient.HttpClientName, httpClient =>
{
    httpClient.BaseAddress = serviceControlApiUri;
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
});

ArgumentException.ThrowIfNullOrWhiteSpace(builder.Configuration[ServiceControlMonitoringApiUrlEnvVarName]);
Uri serviceControlMonitoringApiUri = new(builder.Configuration[ServiceControlMonitoringApiUrlEnvVarName]!);
builder.Services.AddHttpClient(IServiceControlMonitoringApiClient.HttpClientName, httpClient =>
{
    httpClient.BaseAddress = serviceControlMonitoringApiUri;
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddSingleton<IServiceControlApiClient, ServiceControlApiClient>();
builder.Services.AddSingleton<IServiceControlMonitoringApiClient, ServiceControlMonitoringApiClient>();
builder.Services.AddHostedService<Worker>();

var serviceName = OpenTelemetryHelper.GetServiceName(builder.Configuration);
var serviceVersion = OpenTelemetryHelper.GetServiceVersion(builder.Configuration);
var serviceNamespace = OpenTelemetryHelper.GetServiceNamespace(builder.Configuration);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(configure => configure.AddService(serviceName: serviceName, serviceNamespace: serviceNamespace, serviceVersion: serviceVersion, serviceInstanceId: Environment.MachineName))
    .WithMetrics(otelBuilder => 
        otelBuilder
            .AddMeter(meterName)
                .AddOtlpExporter());

var app = builder.Build();
app.Run();