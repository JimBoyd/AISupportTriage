# AI Support Triage

A .NET 9 API that uses AI to automatically triage software support tickets. Built with Microsoft.Extensions.AI and Ollama running locally.

## What It Does

- Creates and manages support tickets
- Uses AI to categorize and assess severity
- Searches a database of known issues via AI function calling
- Tracks triage history and status changes
- Runs entirely local (no cloud API keys needed)

## Tech Stack

- .NET 9 Web API
- Microsoft.Extensions.AI with Ollama
- Entity Framework Core + SQLite
- OpenTelemetry for observability
- Swagger/OpenAPI

## Setup

1. Install Ollama from https://ollama.ai/
2. Pull the model:
   ```bash
   ollama pull gemma4:26b
   ```
3. Clone and run:
   ```bash
   git clone https://github.com/yourusername/AISupportTriage.git
   cd AISupportTriage/AISupportTriage
   dotnet run
   ```
4. Open Swagger at https://localhost:7000/swagger

## Quick Test

1. Create a ticket about a deployment issue
2. Run triage: POST /api/tickets/1/triage
3. Check the AI's category, severity, and matched known issue

## API Endpoints

**Tickets:**
- POST /api/tickets - Create ticket
- GET /api/tickets - List tickets
- GET /api/tickets/{id} - Get details
- POST /api/tickets/{id}/triage - Run AI triage
- PATCH /api/tickets/{id}/status - Change status
- POST /api/tickets/{id}/close - Close ticket

**Known Issues:**
- GET /api/known-issues - List known issues
- POST /api/known-issues - Create known issue

See AISupportTriage/docs/api.md for details.

## Configuration

Edit appsettings.json:

```json
{
  "AI": {
    "Endpoint": "http://localhost:11434",
    "ModelName": "gemma4:26b",
    "Temperature": 0.2
  }
}
```

## Notes

- Database is SQLite (supporttriage.db)
- Seed data includes 12 known issues
- No authentication required
- Built for local development and demo
