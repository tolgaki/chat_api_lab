namespace AgentOrchestrator.Models;

public record ChatRequest(string Message);

public record ChatMessage(
    string Role,
    string Content,
    DateTime Timestamp
)
{
    public static ChatMessage User(string content) =>
        new("user", content, DateTime.UtcNow);

    public static ChatMessage Assistant(string content) =>
        new("assistant", content, DateTime.UtcNow);

    public static ChatMessage System(string content) =>
        new("system", content, DateTime.UtcNow);
}
