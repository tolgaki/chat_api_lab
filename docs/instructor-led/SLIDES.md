# Presentation Slides Outline

Use this outline to create slides for the instructor-led workshop.

---

## Slide 1: Title

**Agent-to-Agent Communication Lab**
.NET 10 Agent with Microsoft 365 Agents SDK + Semantic Kernel

[Your name/organization]
[Date]

---

## Slide 2: Agenda

1. Why Agent Orchestration?
2. Architecture Overview
3. Hands-On: Setup & Run
4. Deep Dive: Semantic Kernel Plugins
5. Deep Dive: Intent Classification
6. Deep Dive: Copilot Integration
7. Response Synthesis
8. Multi-Channel Deployment
9. Extensions & Next Steps

---

## Slide 3: The Challenge

**Users want unified, intelligent experiences**

- Questions span multiple domains
- Data lives in different systems
- Single AI models have limitations
- Users don't want to context-switch

**Example**: "Summarize my important emails and explain what actions I should take"

---

## Slide 4: The Solution - Agent Orchestration

**A .NET 10 Agent that coordinates specialist agents**

```
User → Orchestrator → [Agent 1] → Response
                   → [Agent 2] ↗
                   → [Agent n] ↗
```

Benefits:
- Best-of-breed for each domain
- Unified user experience
- Flexible, extensible architecture

---

## Slide 5: Architecture Diagram

```
┌─────────────────────────────────────────────┐
│              USER INTERFACE                 │
│         Chat + Real-Time Trace              │
└─────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────┐
│     MICROSOFT 365 AGENTS SDK                │
│     OrchestratorAgent : AgentApplication    │
└─────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────┐
│           SEMANTIC KERNEL                   │
│  IntentPlugin → Execute → SynthesisPlugin   │
└─────────────────────────────────────────────┘
         │                      │
         ▼                      ▼
┌─────────────────┐  ┌─────────────────────────┐
│ AzureOpenAI     │  │  M365CopilotPlugin      │
│ Plugin          │  │  • QueryEmails()        │
│ • General Q&A   │  │  • QueryCalendar()      │
│                 │  │  • QueryFiles()         │
└─────────────────┘  └─────────────────────────┘
```

---

## Slide 6: Technology Stack

| Layer | Technology |
|-------|------------|
| **Agent Framework** | Microsoft 365 Agents SDK |
| **AI Orchestration** | Semantic Kernel |
| **AI Engine** | Azure OpenAI (GPT-4o) |
| **M365 Integration** | Copilot Chat API (Kiota SDK) |
| **Authentication** | MSAL, Microsoft Entra ID |
| **Backend** | .NET 10, Minimal APIs |
| **Deployment** | Web, Teams, M365 Copilot, Slack |

---

## Slide 7: The Orchestration Flow

```
1. USER QUERY (via Activity Protocol)
   "Summarize emails and explain REST APIs"
              │
              ▼
2. INTENT ANALYSIS (IntentPlugin)
   → M365Email + GeneralKnowledge
              │
              ▼
3. PARALLEL PLUGIN EXECUTION
   → M365CopilotPlugin.QueryEmails()
   → AzureOpenAIPlugin.GeneralKnowledge()
              │
              ▼
4. RESPONSE SYNTHESIS (SynthesisPlugin)
   → Combine into coherent answer
```

---

## Slide 8: Demo Time!

**Let's see it in action**

We'll demonstrate:
- Authentication flow
- Single-intent query
- Multi-intent query
- Real-time trace visualization

---

## Slide 9: Hands-On Exercise 1

**Setup & Configuration**

1. Configure `appsettings.json`
2. Run `dotnet run`
3. Login with Microsoft
4. Send your first query

**Checkpoint**: See your username, chat is enabled

---

## Slide 10: Intent Classification

**How does the orchestrator know what you want?**

IntentPlugin uses GPT-4o to classify:
```json
[
  { "type": "M365Email", "query": "Summarize my emails" },
  { "type": "GeneralKnowledge", "query": "What is REST" }
]
```

**Key**: `[KernelFunction]` decorated methods enable declarative AI

---

## Slide 11: Intent Types & Plugin Routing

| Intent | Semantic Kernel Plugin | Function |
|--------|----------------------|----------|
| `M365Email` | M365CopilotPlugin | QueryEmails() |
| `M365Calendar` | M365CopilotPlugin | QueryCalendar() |
| `M365Files` | M365CopilotPlugin | QueryFiles() |
| `M365People` | M365CopilotPlugin | QueryPeople() |
| `GeneralKnowledge` | AzureOpenAIPlugin | GeneralKnowledge() |

---

## Slide 12: Hands-On Exercise 2

**Test Intent Classification**

Try these queries and observe the trace:
1. "What's the capital of France?"
2. "Summarize my unread emails"
3. "Tell me about AI and check my calendar"

**Observe**: Different intents, different agents

---

## Slide 13: Plugin Execution

**Semantic Kernel invokes plugins in parallel**

```csharp
// In OrchestratorAgent.cs
var tasks = intents.Select(intent =>
    ExecuteAgentForIntentAsync(intent, accessToken, cancellationToken));
var responses = await Task.WhenAll(tasks);

// Each intent maps to a Kernel.InvokeAsync call
await _kernel.InvokeAsync("M365CopilotPlugin", "QueryEmails", ...);
```

**Result**: Multi-intent queries are FAST

---

## Slide 14: M365 Copilot Integration

**M365CopilotPlugin uses Kiota-generated SDK**

```csharp
[KernelFunction]
[Description("Query user emails via Copilot Chat API")]
public async Task<string> QueryEmails(
    [Description("The email query")] string query,
    [Description("User access token")] string accessToken)
{
    var client = CreateCopilotClient(accessToken);
    // 1. Create conversation: POST /beta/copilot/conversations
    // 2. Send chat: POST /beta/copilot/conversations/{id}/chat
    // Response returned synchronously - no polling!
}
```

**Benefits**: Type-safe SDK, M365-grounded, enterprise data

---

## Slide 15: Response Synthesis

**The final step: Make it coherent**

Multiple agent responses → Single user response

```
Agent 1: "You have 5 unread emails from..."
Agent 2: "REST APIs are architectural patterns..."
         │
         ▼ Azure OpenAI Synthesis
         │
"Based on your emails, you have 5 messages
waiting. You asked about REST APIs - these
are architectural patterns that..."
```

---

## Slide 16: Real-Time Streaming

**Server-Sent Events for immediate feedback**

```
event: trace
data: {"step":"intent_analysis","status":"started"}

event: token
data: {"content":"Based on"}

event: token
data: {"content":" your emails..."}

event: done
data: {"totalDurationMs": 2500}
```

---

## Slide 17: Key Takeaways

1. **M365 Agents SDK** provides enterprise-ready agent framework
2. **Semantic Kernel** enables declarative plugin orchestration
3. **Intent classification** routes queries via [KernelFunction]
4. **Parallel execution** improves performance
5. **Multi-channel** deployment to Teams, Web, M365 Copilot

---

## Slide 18: Extension Ideas

- **Add more plugins**: Custom APIs, databases, external services
- **Deploy to Teams**: Use Azure Bot Service channel
- **Conversation memory**: Leverage ITurnState for context
- **Auto function calling**: Let Semantic Kernel route automatically
- **Monitoring**: OpenTelemetry, Application Insights
- **Production**: Azure App Service + Azure Bot Service

---

## Slide 19: Resources

- **Lab Repository**: [GitHub URL]
- **M365 Agents SDK**: github.com/microsoft/agents
- **Semantic Kernel**: github.com/microsoft/semantic-kernel
- **Azure OpenAI Docs**: learn.microsoft.com/azure/ai-services/openai
- **Copilot Chat API**: learn.microsoft.com/graph/api/resources/copilot

---

## Slide 20: Q&A

**Questions?**

Contact: [your email]

Thank you for attending!
