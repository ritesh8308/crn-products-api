# CRN Technosoft — .NET 8 Developer Assessment
## Phased Build Plan (Gated Milestones)

> A RESTful Products API in **.NET 8** using **Clean Architecture**, **EF Core + SQL Server**, **JWT auth with refresh-token rotation**, **xUnit/Moq + WebApplicationFactory**, **Swagger**, **Serilog**, **FluentValidation**, and **Docker Compose**.
>
> Each phase has a **gate** (a concrete "definition of done"). We do not start the next phase until the current gate passes and you've reviewed the commit.

---

## 1. At a glance

| Phase | Focus | Depends on | Laptop needed? | ~Commits |
|------:|-------|-----------|:--------------:|:--------:|
| 0 | Environment + repo + solution skeleton | — | Yes | 1–2 |
| 1 | Domain layer (entities, exceptions) | 0 | Build: yes / Write: mobile-OK | 1 |
| 2 | Application contracts (DTOs, interfaces, mapping) | 1 | Build: yes / Write: mobile-OK | 1–2 |
| 3 | Infrastructure — EF Core data layer + migration | 2 | **Yes** (SQL Server container) | 2–3 |
| 4 | Application services + FluentValidation | 3 | Build: yes / Write: mobile-OK | 2 |
| 5 | API layer — controllers, middleware, Swagger (**first working CRUD**) | 4 | **Yes** (run + screenshot) | 2–3 |
| 6 | JWT authentication + refresh-token rotation | 5 | Yes | 2–3 |
| 7 | Cross-cutting & performance (Serilog, CORS, headers, compression, pagination) | 5 | Yes | 2 |
| 8 | Testing — unit (Moq) + integration (WebApplicationFactory) | 6, 7 | Yes | 2–3 |
| 9 | Docker Compose, docs/README, screenshot, submission | 8 | Yes | 2 |

**The dependency rule (the heart of Clean Architecture):** dependencies only point *inward*. `Domain` knows nothing about anyone. `Application` knows only `Domain`. `Infrastructure` and `API` depend on the inner layers — never the reverse. This is *why* we build inside-out (Phase 1 → 9): you can compile and reason about each layer before the one that depends on it exists.

```
        API  ─────────────┐
         │                │
   Infrastructure ────────┤   (both depend inward)
         │                │
    Application ──────────┤
         │                │
       Domain  ◄──────────┘   (depends on nothing)
```

---

## 2. Timeline & device constraints

You're on **mobile until June 15**, with a tight 3–7 day turnaround. Anything that runs `dotnet`, `docker`, EF migrations, or the app itself **requires the laptop**. So we split the work:

- **June 13–14 (mobile):** Lock the plan, read the concept primers I'll write for each new topic (EF Core, JWT refresh flow, Moq, WebApplicationFactory), and review/approve code I draft. No execution.
- **June 15 onward (laptop, Pop!_OS):** Apply, build, run, migrate, test, containerize, screenshot, submit.

I will flag at the top of every phase whether it can be *understood/drafted* on mobile vs. whether it *must* wait for the laptop to run.

---

## 3. Phases in detail

Each phase below lists: **Goal · What we build · New concepts (I'll explain before code) · Gate (must pass to proceed) · What to commit.**

### Phase 0 — Environment & repository setup
- **Goal:** A clean, public GitHub repo with an empty, buildable solution skeleton.
- **What we build:** Verify .NET 8 SDK, Docker, and `dotnet-ef` tool; create your own public repo; `.gitignore` (the official Visual Studio/.NET one); the `.sln` + four empty `src/` projects and three `tests/` projects, wired with project references that obey the dependency rule.
- **New concepts:** `dotnet new sln`, adding projects/references, why the reference graph matters.
- **Gate:** `dotnet build` succeeds on the empty solution; initial commit pushed to *your* public repo.
- **Commit:** `chore: scaffold clean architecture solution`

### Phase 1 — Domain layer
- **Goal:** The pure business model, zero external dependencies.
- **What we build:** `Product` and `Item` entities (exact schema), a small `BaseEntity`/audit shape if it helps, and custom exceptions (e.g. `NotFoundException`, `ValidationException`). Enums/Events folders exist but stay minimal — we won't gold-plate.
- **New concepts:** Why Domain references no packages (not even EF Core).
- **Gate:** Domain compiles standalone.
- **Commit:** `feat(domain): add Product and Item entities and domain exceptions`

### Phase 2 — Application contracts
- **Goal:** Define *what* the system does, not *how*.
- **What we build:** DTOs (`ProductDto`, `CreateProductDto`, `UpdateProductDto`, `ItemDto`, a generic `PagedResult<T>`); repository + `IUnitOfWork` interfaces; service interfaces; mapping setup (AutoMapper, or simple manual mappers — I'll recommend one and explain the trade-off).
- **New concepts:** DTO vs entity (and why we never expose entities directly), why interfaces live here, dependency inversion in practice.
- **Gate:** Application compiles referencing only Domain.
- **Commit:** `feat(application): add DTOs, repository/UoW interfaces, and mapping`

### Phase 3 — Infrastructure: EF Core data layer
- **Goal:** Real persistence against SQL Server.
- **What we build:** `ApplicationDbContext`, Fluent API entity configurations mapping the *exact* schema (column types, nullability, the Product→Item FK), generic + Product/Item repositories, `UnitOfWork`, DI registration, the first EF Core **migration**, and a SQL Server container to run it against.
- **New concepts (full primer — new to you):** EF Core end to end — `DbContext`/`DbSet`, change tracking, `AsNoTracking()`, migrations, Fluent API vs data annotations, and how this differs from the raw ADO.NET you already know.
- **Gate:** Migration applies cleanly; you can insert and read a `Product` (via a scratch query or EF tooling).
- **Commit:** `feat(infra): add DbContext, configurations, repositories, UoW, initial migration`

### Phase 4 — Application services + validation
- **Goal:** Business logic and input validation.
- **What we build:** `ProductService` (GET all w/ pagination, GET by id, POST, PUT, DELETE, GET related items), FluentValidation validators for the create/update DTOs, and audit-field handling (`CreatedBy/On`, `ModifiedBy/On`).
- **New concepts:** FluentValidation (declarative rules), what belongs in a service vs a controller vs a repository.
- **Gate:** Services compile and are unit-testable (they depend only on interfaces).
- **Commit:** `feat(application): implement ProductService and FluentValidation validators`

### Phase 5 — API layer (first working CRUD) ⭐
- **Goal:** Hit real endpoints in Swagger — the first visible, demo-able milestone.
- **What we build:** `ProductsController` (+ the related-items endpoint), global error-handling middleware producing a consistent JSON error shape, correct HTTP status codes, `Program.cs` wiring all DI, and Swagger/Swashbuckle. Light API versioning (the assessment mentions it; we keep it minimal).
- **New concepts (new to you):** the ASP.NET Core request pipeline, middleware, model binding, `ActionResult<T>`, and Swagger.
- **Gate:** App runs locally; full Products CRUD works through the Swagger UI. **This is a strong candidate for your required screenshot.**
- **Commit:** `feat(api): add Products controller, error middleware, and Swagger`

### Phase 6 — Authentication: JWT + refresh-token rotation
- **Goal:** Secure the API the way the assessment asks.
- **What we build:** identity/user storage, auth endpoints (register, login, refresh, revoke), access-token generation, refresh-token storage **with rotation**, and `[Authorize]` + role-based checks on sensitive operations.
- **New concepts (full primer — new to you):** what a JWT actually contains, short-lived access vs long-lived refresh tokens, and *why* rotation (issuing a new refresh token on each use and invalidating the old one) protects against token theft. I'll include a small flow diagram.
- **Gate:** Login returns access+refresh tokens; a protected endpoint rejects requests without a valid token; refresh rotates correctly and the old token stops working.
- **Commit:** `feat(auth): JWT issuance with refresh-token rotation and protected endpoints`

### Phase 7 — Cross-cutting & performance
- **Goal:** The production-polish items the assessment lists explicitly.
- **What we build:** Serilog structured logging, a CORS policy, security headers, response compression, and a final pass confirming `AsNoTracking()` on all read paths and `async/await` throughout. Finalize pagination edges (page/size bounds, total count).
- **New concepts:** structured logging (logs as queryable data, not strings), what each security header defends against.
- **Gate:** Logs are structured; headers present on responses; compression verified; reads are no-tracking.
- **Commit:** `feat(api): add Serilog, CORS, security headers, and response compression`

### Phase 8 — Testing
- **Goal:** Confidence, and tests you can defend line-by-line in the interview.
- **What we build:** xUnit + **Moq** unit tests for the service layer (repositories mocked), a few Infrastructure tests, and **WebApplicationFactory** integration tests exercising real endpoints end-to-end.
- **New concepts (full primer — new to you):** xUnit basics, what *mocking* is and why Moq lets us test a service without a database, and how WebApplicationFactory spins up the API in-memory for integration tests.
- **Gate:** `dotnet test` is green across all three test projects.
- **Commit:** `test: unit tests (xUnit/Moq) and integration tests (WebApplicationFactory)`

### Phase 9 — Docker, documentation & submission
- **Goal:** Ship it.
- **What we build:** a multi-stage `Dockerfile`, a `docker-compose.yml` (API + SQL Server) that brings up the whole stack, a `README` (setup, high-level auth flow, deployment notes), an end-to-end run, the screenshot of the app running locally, a final review, and the submission email with the exact subject line **"CRN Technical Assessment Complete Successfully"**.
- **New concepts:** multi-stage Docker builds (small final image) and Compose service networking.
- **Gate:** `docker compose up` runs the full stack; screenshot captured; repo is public and clean; email drafted.
- **Commit:** `chore: add Dockerfile, docker-compose, and README`

---

## 4. Requirements traceability

Every assessment requirement maps to a phase, so nothing is missed:

| Requirement | Phase |
|---|---|
| Clean Architecture folder structure | 0 |
| Product/Item exact schema | 1, 3 |
| EF Core + SQL Server | 3 |
| Full Products CRUD + pagination | 4, 5 |
| Get related Items for a product | 4, 5 |
| Consistent error format + correct status codes | 5 |
| Swagger/OpenAPI (Swashbuckle) | 5 |
| JWT + refresh-token rotation + role-based auth | 6 |
| `AsNoTracking()`, async/await throughout | 3, 7 |
| Serilog, CORS, security headers, compression | 7 |
| xUnit + Moq + WebApplicationFactory | 8 |
| Docker + Docker Compose | 9 |
| Public repo + local-run screenshot + exact subject line | 9 |

---

## 5. Risks & cautions

- **Do not push to CRN's repo.** Everything goes to *your own* public repo (per the assessment).
- **No execution before June 15.** We front-load planning, concept primers, and code review onto the mobile days so the laptop days are pure build + verify.
- **No gold-plating.** Domain Events, heavy versioning, and extra abstractions stay minimal — we build exactly what the assessment asks, kept clean and explainable.
- **Interview-defensibility is a first-class goal.** Code stays commented and conventional so you can explain every line.

---

*Next step: review this plan, then approve so we can begin Phase 0 (or start the concept primers now while you're on mobile).*
