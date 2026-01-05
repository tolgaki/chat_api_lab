namespace AgentOrchestrator.Models;

public record ExecutionPlan(
    string PlanId,
    List<ExecutionStep> Steps,
    bool EnableParallelExecution
)
{
    public static ExecutionPlan Create(List<ExecutionStep> steps, bool parallel = true) =>
        new(Guid.NewGuid().ToString(), steps, parallel);
}

public record ExecutionStep(
    int StepId,
    string AgentName,
    string Action,
    Intent Intent
);

public enum ExecutionStepStatus
{
    Pending,
    Started,
    Completed,
    Failed
}
