# Live Demo Script

Use this script for live demonstrations during the workshop. The application uses Microsoft 365 Agents SDK with Semantic Kernel for AI orchestration.

---

## Pre-Demo Checklist

- [ ] Application running on `http://localhost:5000`
- [ ] Browser open with DevTools ready (Network tab)
- [ ] Code editor open to relevant files
- [ ] Logged out (to show auth flow)
- [ ] Test query works end-to-end

---

## Demo 1: Authentication Flow (2 minutes)

### Script

> "Let me start by showing you the authentication flow. The application uses Microsoft Entra ID with delegated permissions."

**Actions**:
1. Navigate to `http://localhost:5000`
2. Point out the disabled chat input
3. Click "Login with Microsoft"

> "Notice we're redirected to the Microsoft login page. This is the standard OAuth 2.0 authorization code flow with PKCE."

4. Complete login
5. Point out redirect back to app

> "After authentication, we're redirected back. The session is established, and you can see my username in the header. The chat input is now enabled."

**Talking Points**:
- MSAL handles token acquisition and refresh
- Tokens stored server-side for security
- Same auth flow works for any M365-integrated app

---

## Demo 2: Simple Query - General Knowledge (3 minutes)

### Script

> "Let's start with a simple query that doesn't need M365 data."

**Actions**:
1. Type: "What is Kubernetes and why is it useful?"
2. Before sending, point to the trace panel

> "I want you to watch the trace panel on the right. It will show us exactly what happens under the hood."

3. Click Send

> "First, the IntentPlugin analyzes the query. It detects this is a general knowledge question, so it routes to the AzureOpenAIPlugin."

4. Point to trace steps as they appear
5. Point to response

> "The response comes from our AzureOpenAIPlugin's GeneralKnowledge function."

**Talking Points**:
- IntentPlugin classification happens first
- Single intent = single plugin call
- No M365 data needed for general questions

---

## Demo 3: M365 Query - Email (3 minutes)

### Script

> "Now let's try something that requires M365 data."

**Actions**:
1. Type: "Summarize my important emails from today"
2. Click Send
3. Point to trace showing M365CopilotPlugin

> "This time, the IntentPlugin detected an M365Email intent. The OrchestratorAgent routes to M365CopilotPlugin.QueryEmails()."

4. Point to the response

> "The response comes from Copilot, which has access to my actual M365 data - my real emails, calendar, files. This is the power of the integration."

**Talking Points**:
- Copilot has user's M365 context
- M365CopilotPlugin encapsulates Graph API calls
- Response is grounded in actual data

---

## Demo 4: Multi-Intent Query (5 minutes)

### Script

> "Here's where it gets interesting. What if a user asks something that spans multiple domains?"

**Actions**:
1. Type: "Summarize my emails from my manager and explain what REST APIs are"
2. Before sending:

> "This query has two parts - emails from my manager (M365 data) and an explanation of REST APIs (general knowledge). Let's see what happens."

3. Click Send
4. Point to intent analysis in trace

> "Look at the intent analysis - IntentPlugin detected TWO intents: M365Email and GeneralKnowledge."

5. Point to parallel execution

> "Both plugin calls started at the same time - M365CopilotPlugin and AzureOpenAIPlugin run in parallel via Task.WhenAll."

6. Wait for both to complete

> "Now watch the SynthesisPlugin. It takes both responses and combines them into a coherent answer."

7. Point to final response

> "The user sees one unified response. They don't need to know about the underlying complexity."

**Talking Points**:
- Multi-intent detection enables complex queries
- Parallel plugin execution improves performance
- SynthesisPlugin creates coherent user experience

---

## Demo 5: Code Walkthrough - Intent Plugin (3 minutes)

### Script

> "Let me show you how Semantic Kernel plugins work."

**Actions**:
1. Open `Plugins/IntentPlugin.cs`
2. Point to the [KernelFunction] attribute

> "The [KernelFunction] attribute tells Semantic Kernel this method is callable. The [Description] helps with auto function calling."

3. Scroll to the prompt template

> "This is the prompt we send to GPT-4o. It tells the model how to classify intents and return structured JSON."

4. Show the AnalyzeIntent method

> "We call _kernel.InvokePromptAsync with our prompt. Semantic Kernel handles the Azure OpenAI call."

5. Show the fallback handling

> "If JSON parsing fails, we fall back to general knowledge. The system is resilient."

**Talking Points**:
- [KernelFunction] enables declarative AI
- Prompt engineering for structured output
- Error handling is crucial

---

## Demo 6: Code Walkthrough - M365 Copilot Plugin (3 minutes)

### Script

> "Now let's look at how we integrate with the Copilot Chat API using the Kiota-generated SDK."

**Actions**:
1. Open `Plugins/M365CopilotPlugin.cs`
2. Show the QueryEmails method

> "Each query type has its own [KernelFunction]. They all follow the same pattern."

3. Show CreateCopilotClient

> "We create a CopilotApi client using Kiota's HttpClientRequestAdapter with the user's access token."

4. Show CallCopilotChatApiAsync

> "First, we create a conversation. Then we send a chat message and get the response directly - no polling needed."

5. Point out the access token parameter

> "Notice the access token is passed to create the client. This is delegated auth - we're acting on behalf of the user."

**Talking Points**:
- Kiota SDK provides type-safe API calls
- Delegated authentication pattern
- Synchronous response (no polling)

---

## Demo 7: Trace Panel Deep Dive (2 minutes)

### Script

> "Let's look more closely at the trace visualization."

**Actions**:
1. Send a multi-intent query if not recent
2. Point to each trace element

> "Each step shows the agent, the action, and the timing. You can see which steps ran in parallel by their start times."

3. Point to the final summary

> "The total time is displayed at the bottom. For parallel execution, this is close to the slowest single agent, not the sum of all agents."

**Talking Points**:
- Observability is crucial for debugging
- Timing helps identify bottlenecks
- Useful for production monitoring

---

## Troubleshooting During Demo

### If auth fails:
> "Let me check the configuration... [fix and retry]"
> "This is a common issue in setup - make sure your redirect URI matches exactly."

### If Copilot returns empty:
> "Copilot needs a moment to process. Let me try again..."
> "If this persists, check that the Copilot license is properly assigned."

### If response is slow:
> "API calls can vary in latency. In production, you'd want to add caching for repeated queries."

---

## Closing

> "That's the core functionality. The beauty of this architecture is its extensibility - you can add new Semantic Kernel plugins for databases, custom APIs, or any other service. The OrchestratorAgent handles routing based on IntentPlugin classification."

> "Plus, since we're using M365 Agents SDK, this same code can deploy to Teams, M365 Copilot, Slack, and more."

> "Any questions about what we just saw?"
