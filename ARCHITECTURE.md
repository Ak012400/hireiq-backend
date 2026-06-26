# HireIQ Career OS — Architecture

Phase 1 of the [Career OS Roadmap](./HireIQ_Career_OS_Roadmap.md) restructures the codebase into **Clean Architecture** so phases 2–12 (Resume Studio, ATS, Career Coach, Interview Agent, etc.) can be layered on without touching foundational concerns.

## Solution layout

```
hireiq-backend/
├── hireiq-backend.sln
├── src/
│   ├── HireIQ.Domain/         # entities + abstractions, no external deps
│   ├── HireIQ.Application/    # DTOs, service interfaces, use cases (deps: Domain)
│   ├── HireIQ.Infrastructure/ # EF Core, Redis, Azure Blob, JWT, Groq, ML (deps: Application)
│   └── HireIQ.API/            # controllers + Program.cs (deps: Infrastructure)
└── _legacy_HireIQ.API/        # pre-restructure snapshot — safe to delete after verification
```

Dependency direction is strictly inward: API → Infrastructure → Application → Domain. Domain has zero outside dependencies.

## Layer responsibilities

### HireIQ.Domain
Entities (`User`, `Resume`, `JobDescription`, `InterviewRoom`, …), repository abstractions (`IRepository<T>`, `IUnitOfWork`), and domain exceptions. This layer should never reference EF Core, ASP.NET, or any cloud SDK.

### HireIQ.Application
Pure application logic. Holds DTOs, service contracts (`IAuthService`, `ITokenService`, `ICacheService`, `IBlobStorageService`, `IEmailService`, `IAiService`, `IPdfService`, `ICurrentUser`), and the `AddApplication()` DI extension.

### HireIQ.Infrastructure
Implementation details:

| Concern         | Implementation                              |
|-----------------|---------------------------------------------|
| Persistence     | `AppDbContext` (PostgreSQL via Npgsql) + EF Migrations |
| Cache           | `RedisCacheService` (StackExchange.Redis)   |
| Blob storage    | `AzureBlobStorageService` (Azure.Storage.Blobs) |
| Identity        | `JwtTokenService` + `AuthService`           |
| Email           | `SmtpEmailService` (MailKit)                |
| AI              | `GroqService` + `MLService` + `GroqAiService` adapter |
| PDF             | `PdfService` (Puppeteer) + `PdfExtractorService` (PdfPig) |
| Mongo           | `MongoDbService` (agent / conversation memory) |

All registrations live in `Infrastructure/DependencyInjection.cs` → `AddInfrastructure(IConfiguration)`.

### HireIQ.API
Controllers, middleware, Swagger, rate-limiter, JWT bearer, CORS. `Program.cs` simply chains `AddApplication()` + `AddInfrastructure(config)` + ASP.NET concerns.

## Required environment variables

See `.env.example`. Minimum to boot locally:

- `ConnectionStrings__DefaultConnection` — Postgres
- `JwtSettings__SecretKey` — ≥32 char random string
- `Redis__ConnectionString` — defaults to `localhost:6379`
- `GroqSettings__ApiKey` — for AI endpoints

Azure Blob is optional in dev — if `AzureBlob__ConnectionString` is empty, `IBlobStorageService` simply isn't registered. Controllers should null-check or fall back to local storage.

## Running EF migrations

The DbContext now lives in `HireIQ.Infrastructure`. Run from `src/HireIQ.API`:

```bash
dotnet ef migrations add <Name> --project ../HireIQ.Infrastructure --startup-project .
dotnet ef database update --project ../HireIQ.Infrastructure --startup-project .
```

## Frontend layout

```
hireiq-frontend/
├── src/
│   ├── App.js
│   ├── index.js
│   ├── store/                       # Redux Toolkit store
│   ├── features/
│   │   ├── auth/        (pages, api, store)
│   │   ├── dashboard/
│   │   ├── resume/      (pages, api, store, templates, editor)
│   │   ├── jobs/
│   │   ├── screening/
│   │   ├── chat/
│   │   ├── interview/
│   │   └── pdf/
│   └── shared/
│       ├── components/  shared/layouts/  shared/context/
│       ├── services/    (http.js, api.js — legacy bag)
│       ├── hooks/       (useAuth)
│       └── utils/
└── tailwind.config.js
```

Auth state moved from raw `localStorage` calls into `features/auth/store/authSlice.js`. Components should use `useAuth()` from `shared/hooks/useAuth.js` instead of reading `localStorage` directly.

## What Phase 2+ adds

| Phase | Lands in                                                                 |
|-------|--------------------------------------------------------------------------|
| 2     | `features/resume/editor/` (canvas + drag-drop) + `Application/Services/ResumeStudioService` |
| 3     | `Infrastructure/Resume/JsonResumeEngine` + template renderers            |
| 4     | `Infrastructure/Ai/DesignAgent`                                          |
| 5     | `Application/Services/AtsScoringService`                                 |
| 6     | `Application/Services/CareerCoachService` + agent prompts                |
| 7     | Embeddings via `IAiService.GetEmbeddingAsync` + pgvector                 |
| 8     | `features/interview/` + `Application/Services/MockInterviewService`      |
| 9     | `Application/Services/PortfolioGeneratorService`                         |
| 10    | `Application/Services/LinkedInOptimizerService`, `CoverLetterService`    |
| 11    | `features/recruiter/` + ranking pipelines                                |
| 12    | Split Infrastructure projects into microservices, add OpenTelemetry      |

Nothing in those phases should require touching Domain or the layer boundaries set here.
