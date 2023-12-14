using Collector;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

ArgumentNullException.ThrowIfNull(builder.Configuration["ServiceControlApiClient:Url"]);
ArgumentNullException.ThrowIfNull(builder.Configuration["ServiceControlMonitoringApiClient:Url"]);

builder.Services.AddHttpClient("ServiceControlApiClient", httpClient =>
{
    httpClient.BaseAddress = new Uri(builder.Configuration["ServiceControlApiClient:Url"]!);
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient("ServiceControlMonitoringApiClient", httpClient =>
{
    httpClient.BaseAddress = new Uri(builder.Configuration["ServiceControlMonitoringApiClient:Url"]!);
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddSingleton<IServiceControlApiClient, ServiceControlApiClient>();
builder.Services.AddSingleton<IServiceControllerMonitoringApiClient, ServiceControllerMonitoringApiClient>();
builder.Services.AddHostedService<Worker>();

var serviceName = OpenTelemetryHelper.GetServiceName(builder.Configuration);
var serviceVersion = OpenTelemetryHelper.GetServiceVersion(builder.Configuration);
var serviceNamespace = OpenTelemetryHelper.GetServiceNamespace(builder.Configuration);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(configure => configure.AddService(serviceName: serviceName, serviceNamespace: serviceNamespace, serviceVersion: serviceVersion, serviceInstanceId: Environment.MachineName))
    .WithMetrics(otelBuilder => 
        otelBuilder
            .AddMeter(Worker.MeterName)
                .AddOtlpExporter());

var app = builder.Build();
app.Run();