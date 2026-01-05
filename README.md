# Microsoft 365 Copilot Chat API + Agents SDK Lab

## Why Microsoft 365 Copilot in Your Agent Architecture?

As AI applications evolve toward **multi-agent architectures**, the question isn't whether to use multiple agents—it's which agents provide the most value. **Microsoft 365 Copilot** offers unique capabilities that no custom RAG pipeline can easily replicate:

- **Enterprise Data Grounding** — Responses based on real emails, meetings, files, and org data
- **Built-in Security** — Respects M365 permissions; no separate access control layer needed
- **Semantic M365 Understanding** — Knows relationships between people, meetings, documents, and conversations
- **Zero Pipeline Maintenance** — No vector databases or embedding pipelines for M365 content

This lab demonstrates how to build a **.NET 10 Agent** using the **Microsoft 365 Agents SDK** with **Semantic Kernel** orchestration, integrating M365 Copilot as a specialist agent.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    MULTI-CHANNEL CLIENTS                    │
│        Teams  |  M365 Copilot  |  Web  |  Slack             │
└─────────────────────────────────────────────────────────────┘
                              │
                    Activity Protocol
                              │
┌─────────────────────────────────────────────────────────────┐
│              MICROSOFT 365 AGENTS SDK                       │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │         OrchestratorAgent : AgentApplication          │  │
│  │                                                       │  │
│  │   OnActivity(Message) → Semantic Kernel → Plugins     │  │
│  └───────────────────────────────────────────────────────┘  │
│                              │                              │
│  ┌───────────────────────────────────────────────────────┐  │
│  │               SEMANTIC KERNEL PLUGINS                 │  │
│  │  IntentPlugin | AzureOpenAIPlugin | M365CopilotPlugin │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
           │                                    │
           ▼                                    ▼
┌─────────────────────┐          ┌─────────────────────────────┐
│   AZURE OPENAI      │          │   M365 COPILOT CHAT API     │
│   (GPT-4o)          │          │   (/beta/copilot via Kiota) │
└─────────────────────┘          └─────────────────────────────┘
```

## Technology Stack

| Component | Technology |
|-----------|------------|
| **Agent Framework** | Microsoft 365 Agents SDK 1.1.x |
| **Orchestration** | Semantic Kernel 1.54.x |
| **AI Model** | Azure OpenAI (GPT-4o) |
| **M365 Integration** | Microsoft Graph Copilot Chat API |
| **Runtime** | .NET 10 |

## Quick Start

### Prerequisites

- .NET 10 SDK
- Microsoft 365 tenant with Copilot license
- Azure subscription with Azure OpenAI access
- Azure AD app registration

### Setup

1. **Clone and navigate to the project:**
   ```bash
   cd src/AgentOrchestrator
   ```

2. **Configure the application:**

   Update `appsettings.json` with your credentials:
   ```json
   {
     "AzureAd": {
       "TenantId": "<your-tenant-id>",
       "ClientId": "<your-client-id>",
       "ClientSecret": "<your-client-secret>"
     },
     "AzureOpenAI": {
       "Endpoint": "https://<your-resource>.openai.azure.com/",
       "ApiKey": "<your-api-key>",
       "DeploymentName": "gpt-4o"
     }
   }
   ```

3. **Run the application:**
   ```bash
   dotnet run
   ```

4. **Open your browser:**
   Navigate to `http://localhost:5000`

5. **Login and start chatting!**

## Features

- **M365 Agents SDK** - Enterprise-grade agent framework with multi-channel support
- **Semantic Kernel Plugins** - Clean AI function architecture
- **Multi-Intent Detection** - Routes queries to appropriate plugins
- **Parallel Execution** - Multiple plugins execute concurrently
- **M365 Copilot Integration** - Query emails, calendar, files, and people

## Example Queries

| Query Type | Example |
|------------|---------|
| Email | "Summarize my unread emails from this week" |
| Calendar | "What meetings do I have tomorrow?" |
| Files | "Find the latest project proposal document" |
| People | "Who has expertise in Azure in my team?" |
| General | "Explain what microservices are" |
| Multi-Intent | "Summarize my emails and explain REST APIs" |

## Project Structure

```
src/AgentOrchestrator/
├── Program.cs                 # App entry, DI, agent registration
├── Agent/
│   └── OrchestratorAgent.cs   # AgentApplication implementation
├── Plugins/                   # Semantic Kernel plugins
│   ├── IntentPlugin.cs
│   ├── AzureOpenAIPlugin.cs
│   ├── M365CopilotPlugin.cs
│   └── SynthesisPlugin.cs
├── CopilotSdk/                # Kiota-generated API client
├── Auth/                      # Authentication components
├── Models/                    # Data models
└── wwwroot/                   # Web UI
```

## Documentation

- [Design Document](DESIGN.md)
- [Self-Paced Lab Guide](docs/self-paced/LAB_GUIDE.md)
- [Prerequisites](docs/self-paced/PREREQUISITES.md)
- [Troubleshooting](docs/self-paced/TROUBLESHOOTING.md)
- [Instructor Guide](docs/instructor-led/FACILITATOR_GUIDE.md)

## Key Concepts

### Microsoft 365 Agents SDK

The M365 Agents SDK provides the foundation for building enterprise agents:
- **AgentApplication** - Base class for agent logic
- **Activity Protocol** - Standard message format (Bot Framework compatible)
- **Multi-Channel** - Deploy to Teams, M365 Copilot, Web, Slack, and more

### Semantic Kernel

Semantic Kernel provides AI orchestration:
- **Plugins** - Encapsulate AI functions with `[KernelFunction]` attributes
- **Kernel** - Manages AI services and plugin invocation
- **Prompts** - Natural language templates for LLM interactions

### M365 Copilot Chat API

The Copilot Chat API (`/beta/copilot/conversations`) enables:
- Programmatic access to M365 Copilot via Kiota-generated SDK
- Enterprise data grounding (emails, calendar, files, people)
- Synchronous conversational interactions

## License

This lab is provided for educational purposes.
