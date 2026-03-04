# CLAUDE.md — Fontana AI Chatbot

This file provides a comprehensive overview of the codebase for AI assistants working on this project.

---

## Project Overview

**Fontana AI Chatbot** is a .NET 10 Web API that powers an AI-driven customer support chatbot for Fontana, a Greek/Cypriot family food business. The chatbot uses OpenAI GPT-4o and answers questions based on:
- A curated FAQ database
- Product data synced from the DABAS food information API

**Language**: C#
**Runtime**: .NET 10
**Database**: SQL Server via Entity Framework Core
**AI model**: OpenAI GPT-4o

---

## Solution Structure

```
Fontana.AI.WebAPI.slnx          # Solution file
├── Fontana.AI.Models/          # Domain entities (no dependencies)
├── Fontana.AI.Data/            # EF Core DbContext + migrations
├── Fontana.AI.Services/        # Business logic (OpenAI, DABAS)
└── Fontana.AI.WebAPI/          # ASP.NET Core controllers + startup
```

Each layer only references layers below it. Models has no references, Data references Models, Services references Data and Models, WebAPI references all three.

---

## Layer Responsibilities

### Fontana.AI.Models
Pure POCO entities used throughout the solution.

- `FaqItem.cs` — FAQ entry: `Id`, `Question`, `Answer`, `Category`
- `DabasProduct.cs` — Product record: `Id`, `Gtin`, `ProductName`, `Ingredients`, `Allergens`, `Origin`, `Nutrition`, `LastSynced`

### Fontana.AI.Data
EF Core data access.

- `ApplicationDbContext.cs` — DbContext with `DbSet<FaqItem> Faqs` and `DbSet<DabasProduct> DabasProducts`
- `Migrations/` — Code-first migrations (do not edit manually)

### Fontana.AI.Services
All external integrations and business logic.

- `IChatService` / `ChatService` — Builds a system prompt from DB data (FAQs + products), calls OpenAI GPT-4o, returns answer
- `DabasClient` — HTTP client wrapper for `https://api.dabas.com`. Fetches products by GTIN or supplier GLN

### Fontana.AI.WebAPI
HTTP layer only. Controllers delegate to services.

- `ChatController` — `POST /api/chat/ask` → returns `{ "answer": "..." }`
- `DabasController` — `POST /api/dabas/sync` → syncs products from DABAS, returns `{ "message": "...", "synced": N }`
- `WeatherForecastController` — Sample template controller; can be deleted

---

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/chat/ask` | Send user question, get AI answer |
| POST | `/api/dabas/sync` | Sync products from DABAS API into DB |
| GET | `/scalar/v1` | Interactive API docs (Development only) |
| GET | `/openapi/v1.json` | OpenAPI spec |

---

## Required Configuration

The file `appsettings.json` is **not tracked in git**. Create it locally with:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<SQL Server connection string>"
  },
  "OpenAI": {
    "ApiKey": "<OpenAI API key>"
  },
  "Dabas": {
    "ApiKey": "<DABAS API key>",
    "SupplierGln": "<Fontana GLN number>"
  }
}
```

`appsettings.Development.json` is tracked and contains placeholder values for `Dabas:ApiKey` and `Dabas:SupplierGln`.

---

## Database & Migrations

- Code-first migrations in `Fontana.AI.Data/Migrations/`
- Auto-applied on startup in `Development` environment
- SQL Server transient error retry: 5 retries, 10-second delays

**To add a new migration** (run from solution root):
```bash
dotnet ef migrations add <MigrationName> --project Fontana.AI.Data --startup-project Fontana.AI.WebAPI
```

**To apply manually**:
```bash
dotnet ef database update --project Fontana.AI.Data --startup-project Fontana.AI.WebAPI
```

---

## Development Workflow

### Run locally
```bash
dotnet run --project Fontana.AI.WebAPI
```
Opens Scalar docs at `http://localhost:5248/scalar/v1` on startup.

### Build solution
```bash
dotnet build
```

### Publish
A `FolderProfile.pubxml` publish profile is included for file system deployment (Release, Any CPU).

---

## Code Conventions

### Naming
- PascalCase for classes, methods, and properties
- camelCase for local variables and parameters
- Interface names prefixed with `I` (e.g., `IChatService`)
- File names match class names exactly

### Architecture rules
- Controllers must not contain business logic — delegate to services
- Services must not reference `Microsoft.AspNetCore.*`
- Models must have no external dependencies
- Use constructor injection; never resolve services manually

### Async/await
All I/O operations are async. Always use `Task<T>` return types for service methods and controller actions.

### Error handling
- Services catch exceptions and return user-friendly strings or null (graceful degradation)
- Controllers return appropriate HTTP status codes (`BadRequest`, `Ok`, `StatusCode(500, ...)`)
- `DabasClient` returns `null` or empty collections on failure so sync continues where possible

### Comments
Code-level comments are written in **Swedish** (the development team's language). Class and method names are in English.

### Configuration access
All external keys and connection strings come from `IConfiguration`. Never hardcode secrets.

---

## ChatService Behavior (Important for AI)

The system prompt in `ChatService.cs` enforces strict guardrails:
- Only answer based on provided FAQ and product data
- Do not discuss prices
- Do not make medical or health claims
- Do not mention competitors
- Do not invent information

When modifying the system prompt, maintain these constraints. The prompt is assembled dynamically from DB content at request time.

---

## DABAS Sync Flow

1. `POST /api/dabas/sync` is called (e.g., by a scheduled job or manually)
2. `DabasController` reads `Dabas:SupplierGln` from config
3. `DabasClient.GetProductsBySupplierGlnAsync(gln)` fetches all products
4. Existing `DabasProducts` table is **cleared** and replaced with fresh data
5. `LastSynced` is set to `DateTime.UtcNow` on all records

This is a full-replace sync — no incremental/delta logic.

---

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.2 | ORM / SQL Server |
| OpenAI | 2.8.0 | GPT-4o integration |
| Microsoft.Extensions.Http | 10.0.2 | Named HttpClient for DabasClient |
| Scalar.AspNetCore | 2.12.12 | Interactive API docs |
| Microsoft.AspNetCore.OpenApi | 10.0.2 | OpenAPI spec generation |

---

## Git Branch Conventions

Branches follow the pattern `claude/<descriptor>`. Feature work is done on dedicated branches and merged to `master` via pull requests.

---

## What Does Not Exist (Yet)

- **No tests** — no test project or test infrastructure
- **No CI/CD** — no GitHub Actions, no Dockerfile, no Azure pipelines
- **No authentication** — CORS is open (`AllowAnyOrigin`); no JWT or API key validation on endpoints
- **No rate limiting** on the chat endpoint
