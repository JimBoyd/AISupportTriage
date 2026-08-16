# AI Pipeline Documentation

This document explains the AI integration architecture and how Microsoft.Extensions.AI is used in AISupportTriage.

## Overview

The application uses **IChatClient** with a complete middleware pipeline:

```csharp
IChatClient ollamaClient = new OllamaChatClient(
    new Uri(options.Endpoint),
    options.ModelName);

return ollamaClient
    .AsBuilder()
    .ConfigureOptions(opt => opt.Temperature = options.Temperature)
    .UseDistributedCache(cache)
    .UseFunctionInvocation()
    .UseLogging(loggerFactory)
    .UseOpenTelemetry(sourceName: "AISupportTriage")
    .Build(services);
```

This pipeline wraps the underlying Ollama provider with cross-cutting concerns, creating a production-ready AI client.

## Why IChatClient?

### Provider Abstraction

The `TicketTriageService` depends on `IChatClient`, not `OllamaChatClient`:

```csharp
public class TicketTriageService : ITicketTriageService
{
    private readonly IChatClient _chatClient; // Interface, not concrete type

    public TicketTriageService(IChatClient chatClient, ...)
    {
        _chatClient = chatClient;
    }
}
```

**Benefits:**
- Service code is provider-agnostic
- Can swap Ollama for OpenAI/Azure/Anthropic without changing service
- Easier to test with mock IChatClient
- Middleware applies regardless of underlying provider

### Middleware Consistency

Using `IChatClient` ensures middleware (caching, logging, telemetry) applies consistently:

```csharp
// This middleware works with ANY IChatClient implementation
.UseDistributedCache(cache)
.UseFunctionInvocation()
.UseLogging(loggerFactory)
.UseOpenTelemetry(sourceName: "AISupportTriage")
```

If we bypassed IChatClient and called OllamaClient directly, we'd lose these capabilities.

## Middleware Pipeline Breakdown

### .AsBuilder()

Converts the IChatClient into a builder that supports middleware composition.

**Why it matters:** Enables the fluent middleware pipeline pattern.

### .ConfigureOptions(opt => ...)

Configures AI request options:

```csharp
.ConfigureOptions(opt => opt.Temperature = options.Temperature)
```

**Temperature:** Set to 0.2 in this application (from appsettings.json)

**Why 0.2?**
- Support triage benefits from **deterministic** output
- Same ticket should produce similar analysis
- Lower temperature = more predictable categorization
- Chat applications often use 0.7-1.0 for creativity

**Other options we could set:**
- MaxTokens
- TopP
- FrequencyPenalty
- PresencePenalty

### .UseDistributedCache(cache)

Caches AI responses using IDistributedCache.

**Cache Key Composition:**
- Hashes the ChatMessage list
- Includes chat options (temperature, etc.)
- Result: Identical requests get cached responses

**Example:**

```csharp
// First call - hits Ollama
POST /api/tickets/1/triage
→ AI request to Ollama
→ Response: "Category: Deployment, Severity: High..."
→ Cached with key: hash(messages + options)

// Second call - same unchanged ticket
POST /api/tickets/1/triage
→ Cache hit!
→ Returns cached response
→ No Ollama call
```

**Cache Invalidation:**
If ticket description changes, the message content changes → new cache key → cache miss → new AI call.

**Why IDistributedCache?**
- Abstraction allows swapping implementations
- In-memory (DistributedMemoryCache) for dev
- Redis for production horizontal scaling
- Middleware manages it transparently

### .UseFunctionInvocation()

Enables AI function/tool calling.

**How it works:**

1. Middleware discovers methods with `[Description]` attributes:
   ```csharp
   [Description("Search known issues database")]
   public async Task<string> SearchKnownIssues(string category, string symptoms)
   ```

2. Sends function schemas to the model

3. When AI decides to call a function:
   - Middleware intercepts the function call request
   - Invokes the actual C# method
   - Returns result to AI
   - AI incorporates result into final response

**Example Flow:**

```
User: POST /api/tickets/1/triage
→ TicketTriageService builds messages
→ IChatClient.GetResponseAsync(messages)
→ AI: "I should search known issues for deployment + missing column"
→ Middleware: Intercepts function call
→ Invokes: SearchKnownIssues("Deployment", "missing column")
  → SupportFunctions.SearchKnownIssues()
  → IKnownIssueService.SearchKnownIssuesAsync()
  → EF Core query
  → Returns: [{"code":"ORD-1042", "title":"Database migration mismatch"...}]
→ AI receives function result
→ AI: "Found known issue ORD-1042, including in analysis"
→ Final response includes matched known issue
```

**Why this matters:**
- AI cannot fabricate known issues
- Functions query real database
- Application logic, not LLM hallucination
- Demonstrates "LLM as orchestrator" pattern

### .UseLogging(loggerFactory)

Logs AI requests and responses.

**What gets logged:**
- AI request messages
- Chat options used
- Function calls invoked
- AI responses
- Errors/exceptions

**Log levels:**
- Information: Request/response summaries
- Debug: Full message content
- Error: AI failures

**Example logs:**
```
[Information] Microsoft.Extensions.AI: Sending chat request
[Information] Microsoft.Extensions.AI: Function invoked: SearchKnownIssues
[Information] Microsoft.Extensions.AI: Chat response received
```

**Configured in appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Extensions.AI": "Information"
    }
  }
}
```

### .UseOpenTelemetry(sourceName: "AISupportTriage")

Creates distributed trace spans for AI operations.

**Activity Source:** "AISupportTriage" (same as application-level telemetry)

**Automatic spans:**
- AI request preparation
- Model API call
- Function invocation
- Response parsing

**Example trace:**

```
HTTP POST /api/tickets/1/triage
  ├─ support-ticket.triage (application span)
  │   ├─ AI chat request (middleware span)
  │   │   ├─ Function: SearchKnownIssues (middleware span)
  │   │   │   └─ Database query (EF Core span)
  │   │   └─ Ollama API call (middleware span)
  │   ├─ triage.persist (application span)
  │   └─ ticket.status-change (application span)
```

**Why it matters:**
- Observe AI pipeline performance
- Identify slow function calls
- Trace errors through the stack
- Production troubleshooting

**Exporter:** Console (for development)

Production would use OTLP exporter → Application Insights/Jaeger/Honeycomb.

### .Build(services)

Builds the final configured IChatClient with all middleware.

Accepts `IServiceProvider` to enable middleware that needs DI services (like IDistributedCache).

## AI Request Flow

### Step-by-Step: Ticket Triage

**1. Build Messages**

```csharp
var messages = new List<ChatMessage>
{
    new(ChatRole.System, SupportTriagePrompt.SystemPrompt),
    new(ChatRole.User, $"Analyze this ticket: Title: {title}, Description: {description}")
};
```

**System Prompt:** Explains AI's role, categories, severity guidelines, function usage rules.

**2. Call IChatClient**

```csharp
var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
```

**Middleware pipeline executes:**
- Distributed cache checks cache
- Function invocation prepares function schemas
- Logging logs request
- OpenTelemetry starts span
- Ollama client sends request to local model

**3. AI Processing**

Model receives:
- System prompt with instructions
- User message with ticket details
- Available function schemas

Model may:
- Analyze ticket directly
- Decide to invoke SearchKnownIssues()
- Use function results in analysis

**4. Function Invocation (if AI calls function)**

```csharp
// AI decides: "I need to search for deployment issues"
→ Middleware intercepts
→ SupportFunctions.SearchKnownIssues("Deployment", "missing column")
→ Returns JSON: [{"code":"ORD-1042",...}]
→ AI receives results
→ AI continues processing with real data
```

**5. Parse Response**

```csharp
var jsonText = response.Text.Trim();
var aiResponse = JsonSerializer.Deserialize<AiTriageResponse>(jsonText);
```

**AiTriageResponse structure:**
```csharp
{
    TicketCategory Category
    TicketSeverity Severity
    string Summary
    string LikelyCause
    List<string> RecommendedActions
    string? MatchedKnownIssueCode
}
```

**6. Validate Known Issue**

```csharp
if (!string.IsNullOrWhiteSpace(aiResponse.MatchedKnownIssueCode))
{
    var knownIssue = await _knownIssueService.GetByCodeAsync(
        aiResponse.MatchedKnownIssueCode);

    if (knownIssue != null)
    {
        triageResult.KnownIssueId = knownIssue.Id;
    }
    else
    {
        _logger.LogWarning("AI suggested code {Code} but it was not found");
    }
}
```

**Why validate?**
- AI might return invalid/outdated code
- AI might hallucinate a code that sounds plausible
- We MUST verify against real database
- Only link if code actually exists

**7. Persist Results**

Save TriageResult, TriageRecommendations, update ticket status.

## AI Functions (Tools)

### SearchKnownIssues

**Purpose:** Search known issues database

**Signature:**
```csharp
[Description("Search the known issues database for issues matching a specific category and symptoms")]
public async Task<string> SearchKnownIssues(
    [Description("The category of the issue")] string category,
    [Description("The symptoms or keywords")] string symptoms)
```

**Implementation:**
- Delegates to IKnownIssueService
- Queries database via EF Core
- Returns top 5 matches as JSON

**AI receives:**
```json
[
  {
    "code": "ORD-1042",
    "title": "Database migration mismatch after deployment",
    "category": "Deployment",
    "symptoms": "HTTP 500 following application deployment",
    "resolution": "Verify and apply database migrations."
  }
]
```

### GetKnownIssueDetails

**Purpose:** Get details of specific known issue

**Signature:**
```csharp
[Description("Get detailed information about a specific known issue by its code")]
public async Task<string> GetKnownIssueDetails(
    [Description("The unique code (e.g., ORD-1042)")] string code)
```

**Use case:** AI finds multiple candidates via search, needs details on one.

## Structured Output

### Why JSON Response Format?

We instruct the AI to return JSON matching AiTriageResponse:

```csharp
messages.Add(new ChatMessage(ChatRole.User,
    "Respond ONLY with a valid JSON object matching this structure: " +
    "{ \"Category\": \"<TicketCategory>\", \"Severity\": \"<TicketSeverity>\", ..."));
```

**Benefits vs string parsing:**
- Strongly typed .NET object
- Compiler checks properties
- Easy to validate
- No brittle regex parsing

**Handling markdown code blocks:**

Some models wrap JSON in:
````
```json
{ ... }
```
````

We clean this up:
```csharp
if (jsonText.StartsWith("```json")) {
    jsonText = jsonText.Substring(7);
}
// ... trim backticks
```

## Caching Deep Dive

### Cache Key Generation

Microsoft.Extensions.AI hashes:
- All ChatMessage objects (roles + content)
- ChatOptions (temperature, max tokens, etc.)

**Result:** Byte-identical requests get cached.

### Cache Hit Example

```csharp
// Request 1
Messages: [
  System: "You are a support triage assistant..."
  User: "Analyze: Title: Order fails, Description: HTTP 500..."
]
Options: { Temperature: 0.2 }
→ Cache miss → Call Ollama → Cache response

// Request 2 (identical ticket, unchanged)
Messages: [
  System: "You are a support triage assistant..."
  User: "Analyze: Title: Order fails, Description: HTTP 500..."
]
Options: { Temperature: 0.2 }
→ Cache hit! → Return cached response
```

### Cache Miss Example

```csharp
// Request 3 (description updated)
Messages: [
  System: "You are a support triage assistant..."
  User: "Analyze: Title: Order fails, Description: HTTP 500 with CustomerReference column error..."
]
Options: { Temperature: 0.2 }
→ Different message content
→ Different cache key
→ Cache miss → Call Ollama
```

### Why Cache AI Responses?

**Scenario:** User accidentally clicks "Triage" button twice.

Without cache:
- Two identical API calls to Ollama
- Wasted compute
- Wasted time
- Identical results

With cache:
- First call hits Ollama
- Second call returns instantly
- Same result, faster
- Reduced load on AI service

**Cache TTL:** Managed by IDistributedCache implementation (default: varies).

## Error Handling

### AI Service Unavailable

If Ollama is not running:

```csharp
try {
    var response = await _chatClient.GetResponseAsync(messages, cancellationToken);
}
catch (Exception ex) {
    _logger.LogError(ex, "AI triage failed");
    throw new InvalidOperationException("AI triage service unavailable", ex);
}
```

**Result:** Returns 503 Service Unavailable to client.

**Important:** Do NOT create empty/fallback triage result. Either AI succeeds or operation fails cleanly.

### Function Call Failures

If known issue search fails:

```csharp
public async Task<string> SearchKnownIssues(...)
{
    try {
        var results = await _knownIssueService.SearchKnownIssuesAsync(...);
        return results.Count == 0 ? "No matching known issues found."
                                  : JsonSerializer.Serialize(results);
    }
    catch (Exception ex) {
        _logger.LogError(ex, "Function SearchKnownIssues failed");
        return "Error searching known issues.";
    }
}
```

**Result:** AI receives error message, proceeds without known issue data.

### Invalid AI Response

If AI returns malformed JSON:

```csharp
try {
    aiResponse = JsonSerializer.Deserialize<AiTriageResponse>(jsonText);
}
catch (JsonException ex) {
    _logger.LogError(ex, "Failed to parse AI response: {Json}", jsonText);
    throw new InvalidOperationException("AI returned invalid response format");
}
```

**Result:** Returns 500 Internal Server Error to client.

**Mitigation:** Use well-tested models, clear prompts, validate responses.

## Observability

### Logging

**Application logs:**
```csharp
_logger.LogInformation("Starting AI triage for ticket {TicketId}", ticketId);
_logger.LogInformation("AI triage completed for ticket {TicketId}", ticketId);
_logger.LogWarning("AI suggested code {Code} but it was not found", code);
```

**Middleware logs:**
- Automatic via `.UseLogging(loggerFactory)`
- Logs every AI request/response
- Logs function invocations

### Telemetry

**Application spans:**
```csharp
using var activity = Telemetry.StartActivity("support-ticket.triage");
activity?.SetTag("ticket.id", ticketId);
```

**Middleware spans:**
- Automatic via `.UseOpenTelemetry(sourceName: "...")`
- Spans for AI calls, function invocation, response processing

### Viewing Traces

Console exporter outputs to terminal:

```
Activity: support-ticket.triage
  Duration: 2.3s
  Tags: ticket.id=1

  Activity: AI chat request
    Duration: 2.1s

    Activity: Function: SearchKnownIssues
      Duration: 0.05s

    Activity: Ollama API call
      Duration: 1.8s
```

## Production Considerations

### Scaling

**Distributed cache:** Swap DistributedMemoryCache for Redis:

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
```

Now cache is shared across multiple API instances.

### Monitoring

**OpenTelemetry exporter:** Change from Console to OTLP:

```csharp
.WithTracing(tracing => tracing
    .AddSource("AISupportTriage")
    .AddOtlpExporter(options => {
        options.Endpoint = new Uri("https://your-collector");
    }))
```

Send traces to Application Insights, Jaeger, Honeycomb, etc.

### Cost Management

**Caching reduces costs:**
- Fewer API calls to cloud LLM providers
- Lower token consumption
- Faster response times

**Function invocation reduces costs:**
- AI doesn't need to "know" all known issues
- Searches database only when needed
- More accurate than embedding entire knowledge base in context

## Summary

The AI pipeline provides:

1. **Abstraction**: IChatClient separates application from provider
2. **Middleware**: Cross-cutting concerns (cache, logs, telemetry) applied consistently
3. **Function calling**: AI orchestrates real application functionality
4. **Caching**: Avoids redundant expensive AI calls
5. **Observability**: Logging and telemetry for production troubleshooting
6. **Structured output**: Strongly typed responses, not string parsing
7. **Error handling**: Graceful failures without corrupting state

These patterns apply to any AI-powered .NET application, not just support triage.
