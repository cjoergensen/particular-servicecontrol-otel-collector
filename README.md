# Description

This application serves as a bridge between the [Particular ServiceControl REST API](https://github.com/Particular/ServiceControl) and [OpenTelemetry](https://github.com/open-telemetry) metrics. It collects data about the number of failed messages and details of all endpoints from the ServiceControl REST API, and then exposes these as OpenTelemetry metrics.

OpenTelemetry is a collection of tools, APIs, and SDKs used to instrument, generate, collect, and export telemetry data (metrics, logs, and traces) for analysis in order to understand your software's performance and behavior. 

The metrics from this application are exposed using the [OpenTelemetry Protocol Exporter](https://github.com/open-telemetry/oteps/blob/main/text/0035-opentelemetry-protocol.md), making them compatible with a wide range of observability tools such as Prometheus and Jaeger.

For each endpoint, the application generates metrics for processing time, critical time, queue length, retries, and throughput. This provides a comprehensive view of the performance and health of your endpoints.

Please note that this is intended as a temporary solution until NServiceBus provides these values as native OpenTelemetry metrics, as discussed in this [issue](https://github.com/Particular/NServiceBus/issues/6868).

# Environment Variables

The application uses the following environment variables:

| Name | Description |  Default Value |
| ---- | ----------- |  ------------- |
| METER_NAME | This is the name of the meter used for OpenTelemetry metrics collection. | *servicecontrol* |
| COLLECTION_INTERVAL | This is the interval at which metrics are collected | *00:00:30 (30 seconds)* |
| SERVICE_CONTROL_API_URL | This is the URL of the ServiceControl  API | N/A |
| SERVICE_CONTROL_MONITORING_API_URL | This is the URL of the ServiceControl Monitoring API | N/A |
| OTEL_SERVICE_NAME | This variable sets the name of the service. It is used by OpenTelemetry to distinguish between different services in your distributed system. For example, you might have services named 'user-service', 'shopping-cart', etc. | AssemblyName.FullName |
| OTEL_SERVICE_VERSION | This variable sets the version of the service. It is used by OpenTelemetry to track different versions of your service, which can be useful for identifying performance regressions or errors associated with a particular release. |  AssemblyName.Version |
| OTEL_SERVICE_NAMESPACE | This variable sets the namespace for the service. It is used by OpenTelemetry to logically group services in a larger system or microservices architecture. This can be particularly useful in multi-tenant environments, where you might have multiple instances of the same service running under different namespaces | N/A |
| OTEL_EXPORTER_OTLP_PROTOCOL | This variable sets the protocol for the OpenTelemetry Protocol (OTLP) exporter. It determines whether the exporter uses gRPC (`grpc`) or HTTP/1.1 (`http/protobuf`) to send telemetry data to the collector. | grpc |
| OTEL_METRIC_EXPORT_INTERVAL | This variable sets the interval, in seconds, at which metrics are exported from your application. It controls how frequently the OpenTelemetry SDK collects and exports metrics data to the backend. | 60000 (1 minute) |




