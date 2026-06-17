# CRN Products API

A RESTful **Products API** built in **.NET 8** using **Clean Architecture**, EF Core + SQL Server, JWT authentication with refresh-token rotation, FluentValidation, Serilog, Swagger, and a full xUnit/Moq + WebApplicationFactory test suite. Containerised with Docker Compose.

> Built for the CRN Technosoft .NET developer assessment.

---

## Contents
- [Architecture](#architecture)
- [Tech stack](#tech-stack)
- [Features](#features)
- [Getting started](#getting-started)
  - [Option A — Docker Compose (recommended)](#option-a--docker-compose-recommended)
  - [Option B — Run locally](#option-b--run-locally)
- [Authentication flow](#authentication-flow)
- [API endpoints](#api-endpoints)
- [Running the tests](#running-the-tests)
- [Configuration](#configuration)
- [Project structure](#project-structure)
- [Production notes](#production-notes)

---

## Architecture

Clean Architecture — dependencies point **inward** only:

```
        API  ─────────────┐
         │                │
   Infrastructure ────────┤   (both depend inward)
         │                │
    Application ──────────┤
         │                │
       Domain  ◄──────────┘   (depends on nothing)
```

- **Domain** — entities (`Product`, `Item`, `User`, `RefreshToken`) and exceptions. Zero external dependencies.
- **Application** — use cases (`ProductService`, `AuthService`), DTOs, interfaces (repositories, services), and FluentValidation validators. Depends only on Domain; has no knowledge of EF Core or ASP.NET.
- **Infrastructure** — EF Core `DbContext`, configurations, repositories, Unit of Work, JWT token service, PBKDF2 password hasher, migrations.
- **API** — controllers, JWT/authorization wiring, exception-handling middleware, security headers, Swagger. The composition root.

## Tech stack

.NET 8 · ASP.NET Core Web API · EF Core 8 + SQL Server 2022 · JWT bearer auth · FluentValidation · Serilog · Swashbuckle (Swagger/OpenAPI) · Asp.Versioning · xUnit + Moq + WebApplicationFactory · Docker / Docker Compose.

## Features

- Full **Products CRUD** with pagination (`page`/`pageSize`, bounded to a max page size).
- **Get related items** for a product.
- **JWT authentication** with short-lived access tokens (15 min) and long-lived **refresh tokens (7 days) with rotation** + replay/theft detection.
- **Role-based authorization** — any authenticated user can read; only **Admin** can create/update/delete.
- **FluentValidation** on all write models; **consistent JSON error responses** (RFC 7807 ProblemDetails) with correct status codes via global exception middleware.
- **Serilog** structured logging, **response compression** (Brotli/Gzip), **security headers**, **CORS**.
- `AsNoTracking()` on all read paths; `async/await` throughout.
- **Swagger UI** with an Authorize button; light URL-segment API **versioning** (`/api/v1/...`).
- Auto-applies EF migrations and seeds a default admin on startup.

## Getting started

### Option A — Docker Compose (recommended)

Requires Docker. From the repo root:

```bash
docker compose up --build
```

This builds the API image, starts SQL Server, waits for it to be healthy, applies migrations, seeds the admin user, and starts the API. Then open:

**http://localhost:5078/swagger**

> If host port `1433` is already in use (e.g. another local SQL Server), stop it first or remove the `db` `ports:` mapping in `docker-compose.yml` (the API reaches SQL internally and doesn't need it exposed).

Tear down with `docker compose down` (add `-v` to also drop the database volume).

### Option B — Run locally

Requires the .NET 8 SDK and a SQL Server instance. Start one with Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_strong_Pass123" \
  -p 1433:1433 --name crn-sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

Then run the API (migrations are applied automatically on startup):

```bash
dotnet run --project src/API
```

Open **http://localhost:5078/swagger**. The connection string is in `src/API/appsettings.json` / `appsettings.Development.json`.

## Authentication flow

A default admin is seeded on first startup:

| Username | Password    | Role  |
|----------|-------------|-------|
| `admin`  | `Admin@123` | Admin |

To call protected endpoints from Swagger:

1. `POST /api/v1/auth/login` with `{ "username": "admin", "password": "Admin@123" }`.
2. Copy the `accessToken` from the response.
3. Click **Authorize** (top-right in Swagger), paste the token (no `Bearer ` prefix), confirm.
4. Calls are now authenticated. `POST /api/v1/auth/register` creates a plain **User** (read-only on products).

**Tokens & rotation:**

```
login ─> access token (15m)  +  refresh token (7d, stored)
         use access token on every request (Authorization: Bearer <jwt>)
access expires ─> POST /auth/refresh with the refresh token
                  └─> issues a NEW access + NEW refresh, invalidates the old refresh (rotation)
                  └─> replaying an already-used refresh token revokes the whole token family (theft defence)
logout ─> POST /auth/revoke with the refresh token
```

## API endpoints

All under `/api/v1`. 🔒 = requires a valid token; 👑 = requires Admin.

| Method | Route | Access | Description |
|--------|-------|--------|-------------|
| POST | `/auth/register` | public | Register a new user (role User) |
| POST | `/auth/login` | public | Log in, get token pair |
| POST | `/auth/refresh` | public | Rotate refresh token, get new pair |
| POST | `/auth/revoke` | public | Revoke a refresh token (logout) |
| GET | `/products` | 🔒 | List products (paginated) |
| GET | `/products/{id}` | 🔒 | Get a product (with items) |
| GET | `/products/{id}/items` | 🔒 | Get a product's items |
| POST | `/products` | 👑 | Create a product |
| PUT | `/products/{id}` | 👑 | Update a product |
| DELETE | `/products/{id}` | 👑 | Delete a product |

## Running the tests

```bash
dotnet test
```

- **Application.Tests** — unit tests for `ProductService` and `AuthService` (repositories mocked with Moq) and the validators.
- **Infrastructure.Tests** — PBKDF2 hasher, JWT token service, and `ProductRepository` against EF Core's in-memory provider.
- **API.Tests** — end-to-end integration tests via `WebApplicationFactory` (the database is swapped for the in-memory provider, so no SQL Server is needed to run the tests).

## Configuration

`src/API/appsettings.json` (override via environment variables, e.g. `ConnectionStrings__DefaultConnection`):

| Setting | Purpose |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `JwtSettings:Issuer` / `Audience` | JWT issuer / audience |
| `JwtSettings:Key` | HMAC-SHA256 signing key (**dev placeholder — replace in production**) |
| `JwtSettings:AccessTokenMinutes` | Access token lifetime (default 15) |
| `JwtSettings:RefreshTokenDays` | Refresh token lifetime (default 7) |
| `Cors:AllowedOrigins` | Allowed origins (empty = allow any, dev only) |

## Project structure

```
src/
  Domain/          entities, constants, exceptions
  Application/     DTOs, interfaces, services, validators, mapping
  Infrastructure/  EF Core (DbContext, configs, repositories, UoW, migrations), auth (JWT, hashing)
  API/             controllers, middleware, Swagger, Program.cs
tests/
  Application.Tests/     unit tests (xUnit + Moq)
  Infrastructure.Tests/  hasher / token / repository tests
  API.Tests/             integration tests (WebApplicationFactory)
Dockerfile · docker-compose.yml
```

## Production notes

- The JWT signing key and the seeded admin password are **development defaults**. In a real deployment these come from a secret store (user-secrets / environment / a vault) and the admin password is rotated.
- Auto-migrate on startup is convenient for a single-instance demo; in a multi-instance deployment, migrations would run as a separate gated step.
