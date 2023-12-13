using Collector;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("ServiceControlApiClient", httpClient =>
{

    ArgumentNullException.ThrowIfNull(builder.Configuration["ServiceControlApiClient:Url"]);

    httpClient.BaseAddress = new Uri(builder.Configuration["ServiceControlApiClient:Url"]!);
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient("ServiceControllerMonitoringApiClient", httpClient =>
{
    ArgumentNullException.ThrowIfNull(builder.Configuration["ServiceControllerMonitoringApiClient:Url"]);

    httpClient.BaseAddress = new Uri(builder.Configuration["ServiceControllerMonitoringApiClient:Url"]!);
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
