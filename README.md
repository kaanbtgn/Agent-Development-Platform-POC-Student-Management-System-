# Agent Development Platform POC

An abstract, reusable **Agent Development Platform POC** built with .NET 10 and React.  
This repository includes a full **Student Management** reference agent (students, payments, grades) and demonstrates how to build additional domain agents on the same core architecture by changing prompts, MCP tools, and API integrations.

## Recent Updates

- Repositioned the solution as an **Agent Development Platform POC** with a reusable multi-process agent core.
- Added `CreateStudent` MCP tool and end-to-end flow for creating new student records from chat/OCR context.
- Added frontend request cancellation and backend cancellation-safe handling (`499` on client abort).
- Added guardrails against loops:
  - max tool-call iterations/consecutive errors = 3
  - max LLM retry = 3 (transient errors only)
- Hardened document generation pipeline:
  - resilient handling for missing `format` / `contentJson`
  - configurable `AzureOpenAI:MaxOutputTokens` (default: 8000)
  - Excel sheet-name sanitization (invalid chars / >31 chars / empty values)
  - lenient Excel cell parsing (text + numeric values)
  - Word/PDF null-safety guards for omitted content fields

## Highlights from Commit History

The following items are based on recent commits and summarize what was added/refined in the platform:

| Commit | Highlights |
|---|---|
| `53c8d4f` | Document-generation reliability improvements, SystemPrompt updates, and wider output-token budget for larger report payloads. |
| `afe3137` | 3x exponential retry limits, request cancellation button, `CreateStudent` tooling, and Agent-side global exception middleware. |
| `1fa553f` | OCR flow fixes, payment/exam routine optimizations, API port update, and Docker-oriented runtime updates. |
| `d642d08` | Audit log storage/display refactor and UI improvements for payments and audit views. |
| `d29e7f6` | Streaming chat with SSE and improved agent response handling. |
| `9ba1329` | Redis + MongoDB chat session history implementation. |
| `b49b874` | Dynamic/fail-closed CORS `AllowedOrigin` policy refactor. |
| `76743f5` | Docker setup for PostgreSQL, MongoDB, and Redis with environment/Nginx updates. |

## Building New Agents with This Platform

Use the same platform to create different agents (HR, Finance, Support, etc.):

1. Keep the shared runtime (`API + Agent + MCP`) and infrastructure (Redis, MongoDB, PostgreSQL, SignalR).
2. Define a domain-specific system prompt in `StudentManagement.Agent/Services/Prompts/`.
3. Add new MCP tool classes in `StudentManagement.MCP/Tools/` and connect them to target APIs.
4. Expose/extend application endpoints in `StudentManagement.Api`.
5. Reuse the same chat/orchestration pipeline, retry policy, and document-generation capabilities.

## Architecture

The platform follows a **Service-Oriented Clean Architecture** with pure DI (no MediatR). Dependencies flow inward only: Domain ← Application ← Infrastructure ← API. The agent runtime is process-separated: API, Agent orchestrator, and MCP tool server. The included Student Management implementation is a reference domain built on top of this reusable platform.

```
StudentManagementSystem/
├── src/
│   ├── StudentManagement.Domain          # Entities (DDD), enums, repository interfaces
│   ├── StudentManagement.Application     # Business logic, DTOs, service abstractions
│   ├── StudentManagement.Infrastructure  # EF Core, MongoDB, Redis, external services
│   ├── StudentManagement.Api             # ASP.NET Core API  — port 5100
│   ├── StudentManagement.Agent           # LLM orchestration (Azure OpenAI + MCP) — port 5200
│   └── StudentManagement.MCP             # Model Context Protocol server (tools) — port 5300
├── student-management-ui/                # React + Vite + TailwindCSS
└── tests/
    ├── StudentManagement.UnitTests       # 26 unit tests  (xUnit + Moq, no Docker)
    └── StudentManagement.IntegrationTests # 7 integration tests (Testcontainers)
```

### Key design decisions

- **Domain-agnostic agent core** — agent orchestration, retry/cancel behavior, chat streaming, history, and MCP integration are shared; new agents mainly swap prompt + tool set + API surface.
- **No MediatR** — plain service interfaces keep the call graph explicit and easy to trace.
- **Agent is a singleton** — `StudentManagementAgent` and its MCP client are registered as singletons; the tool cache is lazily initialised once and shared across requests.
- **Stateless agent requests** — conversation history is never held in memory. Each request carries the session ID; the infrastructure layer resolves history from a Redis sliding-window cache (last 20 messages, 24 h TTL) with MongoDB as the permanent append-only backing store.
- **DDD entity encapsulation** — `Student` has private setters and exposes mutations through domain methods (`Update`, `Anonymize`), preventing accidental direct assignment.
- **Fail-closed CORS** — production startup throws if `AllowedOrigin` is not configured, making misconfiguration loud rather than silent.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend API | .NET 10, ASP.NET Core, C# 14 |
| AI / Agent | `Microsoft.Extensions.AI`, Azure OpenAI (configurable deployment, currently `gpt-5.5`), Model Context Protocol |
| Document OCR | Azure AI Document Intelligence |
| Document Generation | OpenXml SDK (Word `.docx`), ClosedXML (Excel `.xlsx`) |
| Relational DB | PostgreSQL 15 + Entity Framework Core 10 |
| NoSQL / Audit | MongoDB 6 — chat sessions, structured audit logs |
| Cache / History | Redis 7 — sliding-window conversation history (last 20 msgs, 24 h TTL) |
| Real-time | ASP.NET Core SignalR |
| Logging | NLog with session-ID correlation |
| Rate Limiting | ASP.NET Core built-in fixed-window rate limiter (60 req/min) |
| Frontend | React 18, TypeScript, Vite, TailwindCSS |
| API Docs | Scalar (OpenAPI) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker Desktop (for local infrastructure and integration tests)
- Azure OpenAI resource (model deployment configured in `StudentManagement.Agent/appsettings*.json`)
- Azure AI Document Intelligence resource

## Getting Started

### 1. Start infrastructure

```bash
docker run -d --name postgres \
  -e POSTGRES_USER=studentmanagement \
  -e POSTGRES_PASSWORD=studentmanagement \
  -e POSTGRES_DB=studentmanagement \
  -p 5432:5432 postgres:16-alpine

docker run -d --name mongodb \
  -e MONGO_INITDB_ROOT_USERNAME=admin \
  -e MONGO_INITDB_ROOT_PASSWORD=password \
  -p 27017:27017 mongo:6.0

docker run -d --name redis -p 6379:6379 redis:7-alpine
```

### 2. Configure the API

Edit `src/StudentManagement.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Postgres":  "Host=localhost;Port=5432;Database=studentmanagement;Username=studentmanagement;Password=studentmanagement",
    "Mongo":     "mongodb://admin:password@localhost:27017/student_management?authSource=admin",
    "MongoLog":  "mongodb://admin:password@localhost:27017?authSource=admin",
    "Redis":     "localhost:6379"
  },
  "AzureDocumentIntelligence": {
    "Endpoint": "https://<your-resource>.cognitiveservices.azure.com/",
    "ApiKey":   "<your-api-key>"
  },
  "AllowedOrigin": "http://localhost:5173"
}
```

Edit `src/StudentManagement.Agent/appsettings.Development.json` with your Azure OpenAI endpoint, API key, and deployment name.

### 3. Run all three processes

```bash
# Terminal 1 — API
cd src/StudentManagement.Api && dotnet run

# Terminal 2 — MCP tool server
cd src/StudentManagement.MCP && dotnet run

# Terminal 3 — AI Agent
cd src/StudentManagement.Agent && dotnet run
```

| Process | URL |
|---|---|
| API + Scalar docs | `http://localhost:5100` → `http://localhost:5100/scalar/v1` |
| Agent + Scalar docs | `http://localhost:5200` → `http://localhost:5200/scalar/v1` |
| MCP tool server | `http://localhost:5300` |

### 4. Run the frontend

```bash
cd student-management-ui
npm install && npm run dev
```

Frontend will be available at `http://localhost:5173`.

## Key Features

### AI Document Processing

Upload a payment or exam grade document (PDF/image). Azure Document Intelligence extracts the structured data; the agent maps it to existing student records using fuzzy name matching (Levenshtein distance). Confidence below 85% triggers a **Human-in-the-Loop** step — the user must confirm ambiguous matches before any database write occurs.

### Persistent Chat Sessions

Every agent conversation is stored in two layers:

- **Redis** — sliding window of the last 20 messages with a 24-hour TTL for fast context retrieval.
- **MongoDB** — append-only full history for audit and replay. On cache miss the agent warms Redis from MongoDB automatically.

### Natural Language Queries

Ask the agent questions like "How much was Ahmet paid this month?" or "List all Computer Science students enrolled this year." The agent selects the appropriate MCP tool, fetches the data from the API, and returns a human-readable answer.

### Document Generation

Request a Word exam paper or Excel payment summary in natural language. The agent assembles the content; the infrastructure layer renders a formatted `.docx` or `.xlsx` and returns it as a file download.

### Real-time Progress Updates

A SignalR hub pushes progress events to the frontend during long-running OCR and agent operations, so users see incremental feedback rather than waiting for a full response.

### Audit Logging

An EF Core interceptor records every write operation (`INSERT`, `UPDATE`, `DELETE`) to MongoDB's `AuditLogs` collection, capturing the entity state before and after the change, user context, and session correlation ID.

### GDPR Anonymization

The `Student` entity exposes an `Anonymize()` domain method that blanks personal fields in a single atomic operation, honoring the right to erasure without deleting the record's referential integrity.

## Running Tests

```bash
# Unit tests — no external dependencies
dotnet test tests/StudentManagement.UnitTests

# Integration tests — requires Docker
dotnet test tests/StudentManagement.IntegrationTests
```

> [!NOTE]
> Integration tests use **Testcontainers** and automatically spin up isolated PostgreSQL, Redis, and MongoDB containers. Docker must be running. The test factory injects all required configuration (connection strings, placeholder Azure keys) so no real Azure credentials are needed.

### Test coverage at a glance

| Suite | Count | Scope |
|---|---|---|
| Unit | 26 | Agent orchestration, application services, domain invariants, infrastructure interceptors |
| Integration | 7 | Student CRUD endpoints, payment upsert/query — real DB + cache round-trips |

## CI/CD

A GitHub Actions workflow (`.github/workflows/tests.yml`) runs on every push and pull request to `main` and `develop`:

1. **Unit tests** run first with Cobertura code coverage collected and uploaded as an artifact.
2. **Integration tests** run in a subsequent job (only if unit tests pass), using the Docker socket available on `ubuntu-latest` to start Testcontainers.

## Project Background

This system was built phase-by-phase following a specification-driven workflow. The phase prompts in `.github/prompts/` document each architectural decision from domain modeling through AI agent integration and automated testing.
