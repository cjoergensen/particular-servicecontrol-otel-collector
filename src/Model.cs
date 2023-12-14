using System.Text.Json.Serialization;

namespace Collector;

public record Endpoint(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("isStale")] bool IsStale,
    [property: JsonPropertyName("endpointInstanceIds")] IReadOnlyList<string> EndpointInstanceIds,
    [property: JsonPropertyName("metrics")] Metrics Metrics,
    [property: JsonPropertyName("disconnectedCount")] int DisconnectedCount,
    [property: JsonPropertyName("connectedCount")] int ConnectedCount
);

public record Metrics(
    [property: JsonPropertyName("processingTime")] ProcessingTime ProcessingTime,
    [property: JsonPropertyName("criticalTime")] CriticalTime CriticalTime,
    [property: JsonPropertyName("retries")] Retries Retries,
    [property: JsonPropertyName("throughput")] Throughput Throughput,
    [property: JsonPropertyName("queueLength")] QueueLength QueueLength
);

public record ProcessingTime(
    [property: JsonPropertyName("average")] double Average,
    [property: JsonPropertyName("points")] IReadOnlyList<double> Points
);

public record CriticalTime(
    [property: JsonPropertyName("average")] double Average,
    [property: JsonPropertyName("points")] IReadOnlyList<double> Points
);

public record Retries(
    [property: JsonPropertyName("average")] double Average,
    [property: JsonPropertyName("points")] IReadOnlyList<double> Points
);

public record Throughput(
    [property: JsonPropertyName("average")] double Average,
    [property: JsonPropertyName("points")] IReadOnlyList<double> Points
);

public record QueueLength(
    [property: JsonPropertyName("average")] double Average,
    [property: JsonPropertyName("points")] IReadOnlyList<double> Points
);