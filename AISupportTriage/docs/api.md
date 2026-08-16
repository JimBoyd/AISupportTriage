# API Documentation

Base URL: `https://localhost:7000` (or as configured)

All endpoints return JSON. Errors use ProblemDetails format.

## Tickets API

### Create Ticket

Create a new support ticket.

**Endpoint:** `POST /api/tickets`

**Request Body:**
```json
{
  "title": "Order submission fails after deployment",
  "description": "Users receive HTTP 500 errors when submitting orders after last night's production deployment."
}
```

**Validation:**
- `title`: Required, 3-200 characters
- `description`: Required, 10-5000 characters

**Response:** `201 Created`
```json
{
  "id": 1,
  "title": "Order submission fails after deployment",
  "description": "Users receive HTTP 500 errors when submitting orders...",
  "status": "Open",
  "createdAtUtc": "2026-08-16T20:00:00Z",
  "updatedAtUtc": "2026-08-16T20:00:00Z",
  "closedAtUtc": null
}
```

---

### List Tickets

List tickets with optional filtering and pagination.

**Endpoint:** `GET /api/tickets`

**Query Parameters:**
- `status` (optional): Filter by TicketStatus (Open, Triaged, InProgress, Resolved, Closed)
- `page` (optional): Page number, default 1
- `pageSize` (optional): Items per page, default 25

**Example:** `GET /api/tickets?status=Open&page=1&pageSize=10`

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": 1,
      "title": "Order submission fails",
      "status": "Triaged",
      "createdAtUtc": "2026-08-16T20:00:00Z",
      "updatedAtUtc": "2026-08-16T20:15:00Z"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1
}
```

---

### Get Ticket

Get ticket details including triage history and status changes.

**Endpoint:** `GET /api/tickets/{id}`

**Response:** `200 OK`
```json
{
  "id": 1,
  "title": "Order submission fails after deployment",
  "description": "Users receive HTTP 500 errors...",
  "status": "Triaged",
  "createdAtUtc": "2026-08-16T20:00:00Z",
  "updatedAtUtc": "2026-08-16T20:15:00Z",
  "closedAtUtc": null,
  "triageResults": [
    {
      "id": 1,
      "category": "Deployment",
      "severity": "High",
      "summary": "Order submission began failing immediately following a production deployment.",
      "likelyCause": "Application and database schema versions may be mismatched.",
      "knownIssue": {
        "id": 1,
        "code": "ORD-1042",
        "title": "Database migration mismatch after deployment"
      },
      "recommendedActions": [
        "Review application logs for database exceptions",
        "Verify the production database migration level",
        "Compare deployed application version with database schema",
        "Consider rollback if business operations remain blocked"
      ],
      "analyzedAtUtc": "2026-08-16T20:15:00Z"
    }
  ],
  "statusHistory": [
    {
      "previousStatus": "Open",
      "newStatus": "Triaged",
      "changedAtUtc": "2026-08-16T20:15:00Z",
      "notes": "AI triage completed."
    }
  ]
}
```

**Error:** `404 Not Found` if ticket doesn't exist.

---

### Update Ticket

Update ticket title and description.

**Endpoint:** `PUT /api/tickets/{id}`

**Request Body:**
```json
{
  "title": "Order submission fails after deployment",
  "description": "Users receive HTTP 500 errors. Logs now show a missing CustomerReference column after last night's deployment."
}
```

**Response:** `200 OK` (returns updated ticket)

**Notes:**
- Updating title/description does NOT automatically rerun AI triage
- Use the triage endpoint explicitly to reanalyze

---

### Run AI Triage

Analyze ticket using AI and persist triage results.

**Endpoint:** `POST /api/tickets/{id}/triage`

**Request Body:** None required

**Response:** `200 OK`
```json
{
  "ticketId": 1,
  "triageResultId": 1,
  "category": "Deployment",
  "severity": "High",
  "summary": "Order submission began failing immediately following a production deployment.",
  "likelyCause": "The deployed application likely expects a database schema change that has not been applied.",
  "knownIssue": {
    "id": 1,
    "code": "ORD-1042",
    "title": "Database migration mismatch after deployment",
    "resolution": "Verify migration history and apply the missing production database migration."
  },
  "recommendedActions": [
    "Review application logs for database exceptions",
    "Verify EF Core/database migration history",
    "Compare application and database deployment versions",
    "Rollback the application if production ordering remains unavailable"
  ],
  "analyzedAtUtc": "2026-08-16T20:15:00Z"
}
```

**Behavior:**
- Creates a NEW TriageResult record (preserves history)
- AI may invoke SearchKnownIssues() function
- If known issue found, it's linked to the triage result
- If ticket status is "Open", changes to "Triaged"
- If ticket is already Triaged/InProgress/etc, status preserved
- Creates status history entry when status changes

**Errors:**
- `404 Not Found` - Ticket doesn't exist
- `503 Service Unavailable` - AI service (Ollama) unavailable

**Caching:**
- Identical ticket triaged twice uses cached AI response
- Changing ticket description invalidates cache

---

### Change Ticket Status

Manually change ticket status.

**Endpoint:** `PATCH /api/tickets/{id}/status`

**Request Body:**
```json
{
  "status": "InProgress",
  "notes": "Assigned for remediation."
}
```

**Response:** `200 OK` (returns updated ticket)

**Validation:**
- `status`: Required, must be valid TicketStatus enum value
- `notes`: Optional, max 500 characters

**Behavior:**
- If status unchanged, returns success without duplicate history
- If status changes, creates TicketStatusHistory record

---

### Close Ticket

Close a ticket.

**Endpoint:** `POST /api/tickets/{id}/close`

**Request Body:**
```json
{
  "notes": "Database migration applied and order processing verified."
}
```

**Response:** `200 OK`

**Behavior:**
- Sets status to "Closed"
- Sets closedAtUtc to current time
- Creates status history entry

---

### Reopen Ticket

Reopen a closed ticket.

**Endpoint:** `POST /api/tickets/{id}/reopen`

**Request Body:**
```json
{
  "notes": "Problem returned after overnight deployment."
}
```

**Response:** `200 OK`

**Behavior:**
- Sets status to "Open"
- Clears closedAtUtc
- Creates status history entry

---

## Known Issues API

### List Known Issues

List all known issues with optional filtering.

**Endpoint:** `GET /api/known-issues`

**Query Parameters:**
- `category` (optional): Filter by TicketCategory enum
- `activeOnly` (optional): Boolean, default false

**Example:** `GET /api/known-issues?category=Deployment&activeOnly=true`

**Response:** `200 OK`
```json
[
  {
    "id": 1,
    "code": "ORD-1042",
    "title": "Database migration mismatch after deployment",
    "description": "Application deployments may require database schema changes that were not applied.",
    "category": "Deployment",
    "symptoms": "HTTP 500 errors following deployment; missing table or column exceptions.",
    "resolution": "Verify migration history and apply the missing production database migration.",
    "isActive": true,
    "createdAtUtc": "2026-08-16T18:00:00Z",
    "updatedAtUtc": "2026-08-16T18:00:00Z"
  }
]
```

---

### Get Known Issue

Get a specific known issue by ID.

**Endpoint:** `GET /api/known-issues/{id}`

**Response:** `200 OK` (same structure as list item)

**Error:** `404 Not Found` if issue doesn't exist.

---

### Create Known Issue

Create a new known issue.

**Endpoint:** `POST /api/known-issues`

**Request Body:**
```json
{
  "code": "AUTH-2001",
  "title": "Expired authentication signing certificate",
  "description": "Authentication fails after certificate expiration.",
  "category": "Authentication",
  "symptoms": "Users cannot sign in; token validation errors appear in logs.",
  "resolution": "Replace the expired signing certificate and restart the authentication service."
}
```

**Validation:**
- `code`: Required, max 50 characters, must be unique
- `title`: Required, max 200 characters
- `description`: Required, max 2000 characters
- `category`: Required, valid TicketCategory enum
- `symptoms`: Required, max 1000 characters
- `resolution`: Required, max 2000 characters

**Response:** `201 Created`

---

### Update Known Issue

Update an existing known issue.

**Endpoint:** `PUT /api/known-issues/{id}`

**Request Body:** Same as create

**Response:** `200 OK`

**Error:** `404 Not Found` if issue doesn't exist.

---

### Delete Known Issue

Soft delete a known issue (sets IsActive = false).

**Endpoint:** `DELETE /api/known-issues/{id}`

**Response:** `204 No Content`

**Error:** `404 Not Found` if issue doesn't exist.

**Notes:**
- Known issue is NOT physically deleted
- Historical triage results still reference it
- AI search functions will not return disabled issues

---

## Health Check

### Application Health

Check application and database health.

**Endpoint:** `GET /health`

**Response:** `200 OK` if healthy
```json
{
  "status": "Healthy"
}
```

**Response:** `503 Service Unavailable` if unhealthy

---

## Enums

### TicketStatus
- `Open` - Initial state
- `Triaged` - AI analysis completed
- `InProgress` - Being worked on
- `Resolved` - Fixed but not yet closed
- `Closed` - Finalized

### TicketSeverity
- `Low` - Minor defect, cosmetic issue
- `Medium` - Limited functionality affected, workaround exists
- `High` - Major functionality degraded, significant users affected
- `Critical` - Business-critical system unavailable, major outage

### TicketCategory
- `ApplicationError`
- `Deployment`
- `Database`
- `Authentication`
- `Authorization`
- `Performance`
- `Integration`
- `Configuration`
- `Networking`
- `Data`
- `UserError`
- `Unknown`

---

## Error Responses

All errors use ProblemDetails format:

```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Ticket not found",
  "status": 404,
  "detail": "Support ticket 42 does not exist."
}
```

Common status codes:
- `400 Bad Request` - Validation failure
- `404 Not Found` - Resource doesn't exist
- `500 Internal Server Error` - Unexpected error
- `503 Service Unavailable` - AI service unavailable

---

## Example Workflow

### Complete Ticket Lifecycle

1. **Create ticket**
   ```
   POST /api/tickets
   ```

2. **Run AI triage**
   ```
   POST /api/tickets/1/triage
   ```

3. **View full details**
   ```
   GET /api/tickets/1
   ```

4. **Start work**
   ```
   PATCH /api/tickets/1/status
   { "status": "InProgress", "notes": "Investigating database migration issue" }
   ```

5. **Close ticket**
   ```
   POST /api/tickets/1/close
   { "notes": "Applied missing migration, verified fix in production" }
   ```

6. **If needed, reopen**
   ```
   POST /api/tickets/1/reopen
   { "notes": "Issue recurred after subsequent deployment" }
   ```

### AI Triage Caching Demo

1. **First triage**
   ```
   POST /api/tickets/1/triage
   ```
   Check console logs - see AI request to Ollama

2. **Second triage (unchanged ticket)**
   ```
   POST /api/tickets/1/triage
   ```
   Check console logs - cached response used, no Ollama call

3. **Update ticket**
   ```
   PUT /api/tickets/1
   { "title": "...", "description": "New information added..." }
   ```

4. **Third triage**
   ```
   POST /api/tickets/1/triage
   ```
   Check console logs - cache invalidated, new AI call made

---

## Rate Limiting

No rate limiting in v1. Production deployments should add:
- API gateway with rate limiting
- Authentication/authorization
- Request throttling middleware

---

## Pagination

List endpoints support pagination:
- Default page size: 25
- Maximum page size: 100 (recommended)
- Page numbers start at 1

Example:
```
GET /api/tickets?page=2&pageSize=50
```

---

## API Testing

### Swagger UI

Access interactive API documentation:
```
https://localhost:7000/swagger
```

### Example HTTP Requests

Using `curl`:

```bash
# Create ticket
curl -X POST https://localhost:7000/api/tickets \
  -H "Content-Type: application/json" \
  -d '{"title":"Test ticket","description":"This is a test ticket for demonstration"}'

# Triage ticket
curl -X POST https://localhost:7000/api/tickets/1/triage

# List tickets
curl https://localhost:7000/api/tickets?status=Triaged
```

---

## Notes

- All timestamps are UTC
- Dates use ISO 8601 format
- Request bodies use JSON
- Response bodies use JSON
- No authentication (local development/demo)
