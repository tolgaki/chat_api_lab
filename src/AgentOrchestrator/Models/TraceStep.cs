using System.Text.Json.Serialization;

namespace AgentOrchestrator.Models;

public record TraceStep(
    int StepId,
    string Agent,
    string Action,
    TraceStatus Status,
    long? DurationMs = null,
    object? Result = null,
    string? Error = null
)
{
    public static TraceStep Started(int stepId, string agent, string action) =>
        new(stepId, agent, action, TraceStatus.Started);

    public static TraceStep Completed(int stepId, string agent, string action, long durationMs, object? result = null) =>
        new(stepId, agent, action, TraceStatus.Completed, durationMs, result);

    public static TraceStep Failed(int stepId, string agent, string action, long durationMs, string error) =>
        new(stepId, agent, action, TraceStatus.Failed, durationMs, Error: error);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TraceStatus
{
    Started,
    Completed,
    Failed
}

public record TraceSummary(
    List<TraceStep> Steps,
    long TotalDurationMs
);
