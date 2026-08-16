# Architecture Documentation

## Overview

AISupportTriage is a single ASP.NET Core Web API project for AI-powered support ticket triage using Microsoft.Extensions.AI. The architecture is intentionally focused and practical rather than implementing heavyweight enterprise patterns.

## Technology Choices

### Why ASP.NET Core Web API?
- Modern .NET hosting infrastructure
- Built-in dependency injection
- Integrates naturally with Microsoft.Extensions.* ecosystem
- Swagger/OpenAPI for easy testing without custom client

### Why SQLite?
- **GitHub portability**: Reviewers can clone and run without database setup
- **Local-first design**: No server installation required
- **EF Core standard**: Can easily swap to SQL Server or other providers
- **Seed data included**: Pre-populated known issues for immediate testing

The data layer is ordinary EF Core. Changing to SQL Server requires only updating the connection string and provider package.

### Why Ollama?
- **No cloud credentials required**: Runs entirely locally
- **No API costs**: Free to use for development and testing
- **Strong function calling**: Modern local models support tool invocation
- **GitHub-friendly**: Reviewers can install Ollama and test without account signup

## Project Structure

```
AISupportTriage/
├── AI/                          # AI-specific code
│   ├── AiOptions.cs            # Strongly typed configuration
│   ├── SupportTriagePrompt.cs  # System prompt and instructions
│   ├── SupportFunctions.cs     # AI-callable functions
│   └── Models/
│       └── AiTriageResponse.cs # Structured AI output DTO
│
├── Controllers/                 # ASP.NET Core controllers
│   ├── TicketsController.cs    # Ticket lifecycle endpoints
│   └── KnownIssuesController.cs # Known issue CRUD
│
├── Data/                        # EF Core data layer
│   ├── AppDbContext.cs         # Database context
│   ├── SeedData.cs             # Initial known issues
│   └── Migrations/             # EF Core migrations
│
├── Models/
│   ├── Entities/               # EF Core entities
│   │   ├── SupportTicket.cs
│   │   ├── TriageResult.cs
│   │   ├── TriageRecommendation.cs
│   │   ├── KnownIssue.cs
│   │   └── TicketStatusHistory.cs
│   ├── Enums/
│   │   ├── TicketStatus.cs
│   │   ├── TicketSeverity.cs
│   │   └── TicketCategory.cs
│   ├── Requests/               # API request DTOs
│   └── Responses/              # API response DTOs
│
├── Observability/
│   └── Telemetry.cs            # OpenTelemetry helpers
│
├── Services/
│   ├── Interfaces/
│   │   ├── ITicketService.cs
│   │   ├── ITicketTriageService.cs
│   │   └── IKnownIssueService.cs
│   ├── TicketService.cs        # Ticket lifecycle logic
│   ├── TicketTriageService.cs  # AI triage orchestration
│   └── KnownIssueService.cs    # Known issue CRUD + caching
│
└── Program.cs                   # Application composition root
```

## Architectural Layers

### API Layer (Controllers)
- Handle HTTP requests/responses
- Validate input via data annotations
- Map between service results and DTOs
- Return consistent ProblemDetails errors
- **No business logic** - delegate to services

### Service Layer
Three focused services with single responsibilities:

#### TicketService
- Create, read, update tickets
- Manage ticket lifecycle (open, close, reopen)
- Change ticket status
- Create status history records

#### TicketTriageService
- **The AI orchestration layer**
- Build messages for IChatClient
- Call AI with structured output expectations
- Parse and validate AI responses
- Persist TriageResult and Recommendations
- Match returned known issue codes against database
- Update ticket status after successful triage
- Emit OpenTelemetry spans

#### KnownIssueService
- CRUD operations for known issues
- Search known issues (used by AI functions)
- Manage IMemoryCache for known issues
- Invalidate cache on data changes

### AI Functions (SupportFunctions)
- Expose [Description]-attributed methods
- Called by AI during function invocation
- Delegate to IKnownIssueService
- Return JSON-serialized results
- **Never contain database logic directly**

### Data Layer
- EF Core DbContext
- Entity configurations
- Relationship mappings
- No repository abstraction (DbContext already provides Unit of Work)

## Database Model

### Entity Relationships

```
SupportTicket (1) ──→ (*) TriageResult
                  ──→ (*) TicketStatusHistory

TriageResult (1) ──→ (*) TriageRecommendation
             (*) ←── (1) KnownIssue (optional)
```

### Key Design Decisions

**Historical Triage Results**: Each triage creates a NEW TriageResult record. This allows:
- Viewing how AI analysis changed as ticket description evolved
- Comparing triage results before/after known issues were added
- Audit trail of all AI analyses

**Status History**: Every status change creates a TicketStatusHistory record:
- Complete lifecycle audit
- Notes for context
- Timestamp for each transition

**Known Issue Soft Delete**: DELETE sets `IsActive = false`:
- Preserves referential integrity
- Historical triage results still reference deleted issues
- Can analyze which known issues were matched most often

**Recommendations as Entities**: Rather than comma-separated text:
- Maintains order via Sequence field
- Easy to query/filter
- Better database normalization

## AI Integration Architecture

### Flow: Ticket Triage Request

```
1. HTTP Request
   ↓
2. TicketsController.Triage()
   ↓
3. TicketTriageService.TriageTicketAsync()
   ├── Load ticket from database
   ├── Build ChatMessage list with system prompt + ticket info
   ├── Call IChatClient.GetResponseAsync()
   │   ├── .UseDistributedCache() ─→ Check cache
   │   ├── .UseFunctionInvocation() ─→ AI calls SearchKnownIssues()
   │   │   └── SupportFunctions ─→ IKnownIssueService ─→ Database
   │   ├── .UseLogging() ─→ Log AI request/response
   │   ├── .UseOpenTelemetry() ─→ Create trace span
   │   └── OllamaChatClient ─→ Local Ollama instance
   ├── Parse JSON response into AiTriageResponse
   ├── Validate MatchedKnownIssueCode against database
   ├── Persist TriageResult + Recommendations
   └── Update ticket status Open → Triaged
```

### Why IChatClient Abstraction?

The application depends on `IChatClient`, not `OllamaChatClient`:
- Provider-agnostic service code
- Middleware applies consistently regardless of provider
- Future: swap Ollama for OpenAI/Azure without changing TicketTriageService
- Easier to test with mock IChatClient

### AI Middleware Pipeline

Each middleware has a specific responsibility:

**ConfigureOptions**: Sets temperature, max tokens, etc.
**UseDistributedCache**: Caches responses based on messages + options
**UseFunctionInvocation**: Intercepts function calls from AI
**UseLogging**: Logs requests, responses, errors
**UseOpenTelemetry**: Creates spans for observability

Middleware executes in **pipeline order** (like ASP.NET Core middleware).

## Caching Strategy

### Two-Level Caching

#### Level 1: Distributed AI Cache (IDistributedCache)
- **Scope**: AI responses
- **Implementation**: DistributedMemoryCache (in-process for demo)
- **Cache key**: Hash of messages + chat options
- **Purpose**: Avoid redundant LLM calls for identical requests
- **Managed by**: Microsoft.Extensions.AI middleware

#### Level 2: Application Cache (IMemoryCache)
- **Scope**: Known issues
- **Implementation**: MemoryCache
- **Cache key**: Known issue ID or Code
- **TTL**: 30 minutes
- **Purpose**: Reduce database queries during AI function invocation
- **Managed by**: KnownIssueService
- **Invalidation**: On create/update/disable

### Why Two Caches?

Different concerns:
- AI cache is **cross-cutting** - middleware manages it transparently
- Application cache is **domain-specific** - service controls invalidation logic

## OpenTelemetry Strategy

### Activity Sources
- `AISupportTriage` - main application source
- Registered in Program.cs: `.AddSource("AISupportTriage")`

### Application-Created Spans
- `support-ticket.triage` - overall triage operation
- `triage.persist` - database save operations
- `ticket.status-change` - status update operations
- `known-issue.search` - search function execution

### AI Middleware Spans
The `.UseOpenTelemetry()` middleware automatically creates spans for:
- AI request preparation
- Function invocation
- Model API calls
- Response processing

### Exporter
Console exporter for development - easy to see traces in terminal output.

Production would use OTLP exporter to send to Application Insights, Jaeger, etc.

## Configuration and Options

### Strongly Typed Options Pattern

```csharp
public sealed class AiOptions
{
    public const string SectionName = "AI";
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ModelName { get; set; } = "gemma4:26b";
    public float Temperature { get; set; } = 0.2f;
}
```

Registered via:
```csharp
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection(AiOptions.SectionName));
```

Consumed via:
```csharp
IOptions<AiOptions> options
```

### Why Temperature 0.2?

Support triage benefits from **deterministic** responses:
- Same ticket should produce similar analysis
- Creativity less important than consistency
- Lower temperature = more predictable categorization

Chat applications often use 0.7-1.0 for variety.

## Error Handling

### AI Service Failures

If Ollama is unavailable or returns errors:
- **Do not** create empty triage result
- **Do not** change ticket status
- **Do not** fabricate fallback response
- **Return** 503 Service Unavailable with ProblemDetails

### Function Invocation Failures

If known issue search fails:
- AI receives empty result or error message
- AI proceeds without known issue match
- Triage completes but MatchedKnownIssueCode is null

### Validation Failures

ASP.NET Core model validation:
- Data annotation validation on request DTOs
- Automatic 400 Bad Request with ProblemDetails
- Consistent error format across all endpoints

## Why This Architecture?

### Pragmatic Design
This architecture prioritizes clarity and maintainability over complexity:

**Avoided:**
- Repository abstraction over EF Core (DbContext already provides UoW)
- CQRS/MediatR (unnecessary indirection for this domain)
- Multiple projects (single project is more maintainable)
- Microservices (not required at this scale)

**Focused on:**
- Clear AI integration patterns
- Microsoft.Extensions.AI middleware
- Function invocation mechanics
- Proper separation of concerns
- Production-quality code

### Extensibility Points

Despite simplicity, the architecture allows:
- Swapping AI providers (change IChatClient registration)
- Swapping databases (change EF provider)
- Adding authentication (add middleware)
- Adding background workers (register IHostedService)
- Scaling horizontally (use Redis for distributed cache)

## Deployment Considerations

### Local Development
- SQLite database created automatically
- Ollama must be installed and running
- Migrations apply on startup
- Seed data inserts once

### Production Considerations (Future)
1. Use SQL Server or PostgreSQL
2. Use Redis for distributed cache
3. Use Azure OpenAI or cloud LLM (add authentication)
4. Configure OTLP exporter for telemetry
5. Add authentication/authorization
6. Move database migrations to deployment pipeline
7. Add health checks for AI service availability

## Summary

The architecture balances:
- **Simplicity**: Maintainable and easy to understand
- **Production patterns**: Real logging, caching, observability
- **AI capabilities**: Leverages Microsoft.Extensions.AI effectively
- **Extensibility**: Can evolve without major refactoring
