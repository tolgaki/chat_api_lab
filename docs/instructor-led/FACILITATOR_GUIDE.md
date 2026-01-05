# Facilitator Guide

## Workshop Overview

**Duration**: 3-4 hours (depending on depth)
**Audience**: ISV developers, solution architects, technical decision makers
**Format**: Instructor-led with hands-on exercises

### Learning Objectives

By the end of this workshop, participants will be able to:
1. Build agents using Microsoft 365 Agents SDK
2. Implement AI orchestration with Semantic Kernel plugins
3. Integrate with Microsoft 365 Copilot Chat API
4. Design multi-channel agent deployments
5. Extend agents with custom plugins

---

## Pre-Workshop Preparation

### Instructor Setup (1-2 days before)

- [ ] Verify all demo credentials work
- [ ] Test the application end-to-end
- [ ] Prepare backup credentials/resources
- [ ] Review participant list for licensing concerns
- [ ] Send prerequisites checklist to participants

### Participant Requirements

- [ ] Completed Prerequisites document
- [ ] Azure subscription with OpenAI access
- [ ] M365 tenant with Copilot licenses
- [ ] Development environment ready
- [ ] Configuration values collected

### Room/Virtual Setup

- [ ] Screen sharing capability
- [ ] Access to Azure Portal
- [ ] Code editor for live demos
- [ ] Whiteboard for architecture diagrams

---

## Workshop Agenda

### Opening (15 minutes)

1. **Welcome & Introductions** (5 min)
2. **Workshop objectives** (5 min)
3. **Architecture overview** (5 min)

**Key Message**: This pattern enables ISVs to build intelligent applications that leverage both custom AI and Microsoft 365 Copilot.

### Module 1: Foundations (30 minutes)

**Content**:
- What is the Microsoft 365 Agents SDK?
- How does Semantic Kernel enable AI orchestration?
- Activity Protocol as the communication standard

**Demo**: Show the completed application
- Login flow
- Send a multi-intent query
- Walk through the trace panel

**Discussion Points**:
- When would you use M365 Agents SDK vs Bot Framework?
- What are the alternatives to Semantic Kernel?

### Module 2: Hands-On Setup (30 minutes)

**Activity**: Participants configure and run the application

**Checkpoints**:
1. Application starts without errors
2. Login succeeds
3. Simple query works

**Common Issues**:
- Configuration typos
- Permission consent not granted
- Wrong redirect URI

**Tip**: Have participants share screens if stuck; often it's a simple config issue.

### Module 3: Deep Dive - Semantic Kernel Plugins (30 minutes)

**Content**:
- How Semantic Kernel plugins work
- The `[KernelFunction]` attribute
- Designing prompt-based functions

**Code Walkthrough**: `Plugins/IntentPlugin.cs`
- System prompt structure
- JSON output format
- Error handling

**Exercise**: Modify the intent classifier
- Add a new intent type
- Test with sample queries

**Discussion**:
- How would you handle ambiguous intents?
- What about auto function calling?

### Break (15 minutes)

### Module 4: Agent Architecture (30 minutes)

**Content**:
- OrchestratorAgent extending AgentApplication
- Activity handlers and turn context
- Parallel plugin execution

**Code Walkthrough**:
- `Agent/OrchestratorAgent.cs`
- `Program.cs` (dependency injection)

**Demo**: Show timing differences
- Single intent: ~2 seconds
- Multiple intents (parallel): ~2.5 seconds (not 4 seconds)

**Discussion**:
- When should execution be sequential?
- How to handle partial failures?

### Module 5: Copilot Integration (30 minutes)

**Content**:
- Copilot Chat API via Kiota SDK
- Authentication and permissions
- Request/response format

**Code Walkthrough**: `Plugins/M365CopilotPlugin.cs`
- CopilotApi client creation
- Conversation creation
- Chat message and response

**Demo**: M365-specific queries
- Email summary
- Calendar check
- File search

**Discussion**:
- Copilot licensing considerations
- When to use Copilot vs direct Graph queries

### Module 6: Response Synthesis (20 minutes)

**Content**:
- Combining multiple agent responses
- Maintaining coherence
- The SynthesisPlugin approach

**Code Walkthrough**: `Plugins/SynthesisPlugin.cs`

**Demo**: Multi-intent response
- Show how two responses become one

### Module 7: Wrap-Up & Q&A (30 minutes)

**Recap**: Key concepts
1. M365 Agents SDK architecture
2. Semantic Kernel plugins
3. Intent classification
4. Parallel execution
5. Multi-channel deployment

**Extension Ideas**:
- Add more Semantic Kernel plugins
- Deploy to Microsoft Teams
- Implement conversation memory with ITurnState
- Production deployment with Azure Bot Service

**Q&A**: Open floor

**Resources**:
- M365 Agents SDK: github.com/microsoft/agents
- Semantic Kernel: github.com/microsoft/semantic-kernel
- Lab GitHub repository
- Follow-up support channels

---

## Facilitation Tips

### Pacing

- **Too fast?** Spend more time on demos; let participants observe
- **Too slow?** Give advanced participants extension challenges
- **Mixed levels?** Pair experienced with beginners

### Common Questions

**Q: Can this work without Copilot licenses?**
A: The M365CopilotPlugin requires Copilot. You could replace it with direct Graph API calls or mock responses for demo purposes.

**Q: How does this compare to Copilot Studio?**
A: Copilot Studio is declarative/low-code. M365 Agents SDK + Semantic Kernel gives full code control, useful for complex ISV scenarios.

**Q: What about costs?**
A: Azure OpenAI has per-token costs. Implement caching and consider query complexity in production.

**Q: Can we use different LLMs?**
A: Yes, Semantic Kernel supports multiple connectors - OpenAI, Azure OpenAI, Ollama, and more. Swap the connector in Program.cs.

**Q: How do we deploy to Teams?**
A: Register with Azure Bot Service, configure the Teams channel, and the same agent code works across channels.

### Troubleshooting During Workshop

| Issue | Quick Fix |
|-------|-----------|
| Auth loop | Clear cookies, restart app |
| 401 from OpenAI | Check API key in config |
| Empty Copilot response | Verify Copilot license |
| Build errors | `dotnet restore`, check SDK version |

---

## Post-Workshop

### Follow-Up Email Template

```
Subject: Agent-to-Agent Communication Lab - Resources & Next Steps

Hi [Name],

Thank you for attending the Agent-to-Agent Communication Lab!

Resources:
- Lab code: [GitHub link]
- Documentation: [Docs link]
- Recording: [if applicable]

Next steps:
1. Try extending the system with your own agent
2. Explore production deployment options
3. Reach out with questions: [contact]

Feedback survey: [link]

Best regards,
[Instructor]
```

### Success Metrics

- [ ] All participants completed setup
- [ ] Multi-intent queries demonstrated
- [ ] Trace visualization understood
- [ ] Positive feedback received

---

## Appendix: Demo Script

### Demo 1: Basic Flow

```
"Let me show you the complete flow. I'll login first..."
[Click Login, complete auth]

"Now I'll send a simple query..."
[Type: "What meetings do I have tomorrow?"]

"Watch the trace panel - you can see IntentPlugin analyzing, then M365CopilotPlugin being called..."
[Point to trace steps]

"And here's the response from Copilot."
```

### Demo 2: Multi-Intent

```
"Now let's try something more complex - a query with multiple intents..."
[Type: "Summarize my recent emails and explain microservices architecture"]

"Notice the trace shows TWO plugin calls - M365CopilotPlugin for emails, AzureOpenAIPlugin for microservices..."
[Point to parallel execution]

"The SynthesisPlugin combines these into one coherent response."
```

### Demo 3: Code Walkthrough

```
"Let me show you how Semantic Kernel plugins work..."
[Open Plugins/IntentPlugin.cs]

"The [KernelFunction] attribute makes this callable from our agent..."
[Highlight attribute]

"This prompt tells GPT-4o how to classify intents..."
[Highlight prompt template]

"And here's where we parse the JSON response..."
[Show ExtractJson method]
```
