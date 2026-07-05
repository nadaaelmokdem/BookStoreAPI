# 📚 BookStore Web API

A clean, production-style **.NET 8 Web API** for an online bookstore — customers browse and
search books and place orders, admins manage the full catalog, and everything is secured with
JWT and documented in Swagger.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4)
![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-CC2927?logo=microsoftsqlserver)
![JWT](https://img.shields.io/badge/Auth-JWT-000000?logo=jsonwebtokens)
![Swagger](https://img.shields.io/badge/Docs-Swagger-85EA2D?logo=swagger)
![License](https://img.shields.io/badge/License-MIT-blue)

---

## Table of contents

- [Features](#features)
- [Tech stack](#tech-stack)
- [Project structure](#project-structure)
- [Getting started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [1. Clone the repo](#1-clone-the-repo)
  - [2. Configure secrets](#2-configure-secrets)
  - [3. Create the database](#3-create-the-database)
  - [4. Run the API](#4-run-the-api)
  - [5. Create the first admin account](#5-create-the-first-admin-account)
- [Testing the API](#testing-the-api)
- [API reference](#api-reference)
- [Error response format](#error-response-format)
- [Design decisions](#design-decisions)
- [Roadmap / possible extensions](#roadmap--possible-extensions)

---

## Features

- 🔐 **JWT authentication** with `Customer` and `Admin` roles
- 📖 **Book catalog** — browse, full-text search, filter by category/author/price range, sort, paginate
- 🗂️ **Categories & Authors** — full CRUD, admin-only writes
- 🛒 **Orders** — customers place multi-item orders and view only their own history; admins see everyone's
- ✅ **Consistent validation** — every bad request comes back with a clear, structured error, never a stack trace
- 🌍 **CORS-ready** for any frontend (React, Angular, Vue, mobile, etc.) running on a different origin
- 📑 **Swagger UI** with a working "Authorize" button for testing protected endpoints
- 🧱 **Clean architecture** — controllers hold no business logic; everything is injected via interfaces

## Tech stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core 8 |
| Database | SQL Server |
| Auth | JWT Bearer + BCrypt password hashing |
| Validation | FluentValidation |
| Docs | Swashbuckle (Swagger / OpenAPI) |

## Project structure

```
src/BookStoreApi/
├── Controllers/        # HTTP endpoints only — no business logic
├── Services/
│   ├── Interfaces/      # Contracts consumed by controllers
│   └── Implementations/ # All business logic lives here
├── Models/               # EF Core entities — never returned to clients
├── Dtos/                 # Request/response contracts, grouped by feature
│   ├── Auth/  Books/  Categories/  Authors/  Orders/  Common/
├── Data/                  # AppDbContext
├── Validators/            # FluentValidation rules per DTO
├── Middleware/            # Global exception-handling middleware
├── Exceptions/            # Custom exceptions mapped to HTTP status codes
├── Extensions/             # DI, JWT, Swagger, CORS setup + claims helpers
├── Program.cs
└── appsettings.json
```

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A running SQL Server instance

Don't have SQL Server handy? Spin one up with Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 --name bookstore-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

### 1. Clone the repo

```bash
git clone https://github.com/<your-username>/BookStoreApi.git
cd BookStoreApi
```

### 2. Configure secrets

Never commit real secrets to `appsettings.json`. Use `dotnet user-secrets` instead:

```bash
cd src/BookStoreApi
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=BookStoreDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:SigningKey" "<a long random string, at least 32 characters>"
```

### 3. Create the database

Migrations are intentionally **not** committed (they're tied to the EF tools version you have
installed) — generate them once locally:

```bash
dotnet tool install --global dotnet-ef   # skip if already installed
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> **PowerShell users:** run these as two separate commands (`&&` isn't a valid separator in
> PowerShell) and make sure you're inside `src/BookStoreApi` when you run them — that's the
> folder with the `.csproj` file.

### 4. Run the API

```bash
dotnet run
```

The API starts at **`http://localhost:5080`** and Swagger opens automatically at
`http://localhost:5080/swagger`. On every startup it applies any pending migrations for you.

### 5. Create the first admin account

There's deliberately no public "make me admin" endpoint. Instead:

1. Register a normal account:
   ```http
   POST /api/auth/register
   Content-Type: application/json

   { "fullName": "Admin User", "email": "admin@bookstore.com", "password": "P@ssword123" }
   ```
2. Promote it directly in the database:
   ```sql
   UPDATE Users SET Role = 1 WHERE Email = 'admin@bookstore.com';
   -- 0 = Customer, 1 = Admin
   ```
3. Log in again (`POST /api/auth/login`) — the **new** token carries the `Admin` role claim.
   Tokens issued before the promotion still say `Customer` until refreshed.

## Testing the API

**Swagger UI** — the easiest way:
1. `POST /api/auth/login` → copy the `token` from the response.
2. Click **Authorize** at the top of the Swagger page → enter `Bearer <token>` → Authorize.
3. Every "Try it out" call now sends the token automatically.

**`.http` file** — `src/BookStoreApi/BookStoreApi.http` has ready-made requests for every
endpoint. Works with the VS Code "REST Client" extension or the built-in HTTP client in
Visual Studio 2022+ / Rider.

**curl**:
```bash
curl -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@bookstore.com","password":"P@ssword123"}'
```

## API reference

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | — | Register a customer account |
| POST | `/api/auth/login` | — | Log in, returns a JWT |
| GET | `/api/books` | — | Browse/search/filter/paginate books |
| GET | `/api/books/{id}` | — | Book details |
| POST | `/api/books` | Admin | Create a book |
| PUT | `/api/books/{id}` | Admin | Update a book |
| DELETE | `/api/books/{id}` | Admin | Delete a book |
| GET | `/api/categories` | — | List categories |
| GET | `/api/categories/{id}` | — | Category details |
| POST / PUT / DELETE | `/api/categories(/{id})` | Admin | Manage categories |
| GET | `/api/authors` | — | List authors |
| GET | `/api/authors/{id}` | — | Author details |
| POST / PUT / DELETE | `/api/authors(/{id})` | Admin | Manage authors |
| POST | `/api/orders` | Customer/Admin | Place an order |
| GET | `/api/orders/mine` | Customer/Admin | View my own orders |
| GET | `/api/orders/{id}` | Customer/Admin | View one order (owner or admin only) |
| GET | `/api/orders` | Admin | View every order from every customer |

`GET /api/books` query parameters:

| Param | Type | Notes |
|---|---|---|
| `page` | int | default 1 |
| `pageSize` | int | default 10, max 100 |
| `search` | string | matches title or description |
| `categoryId` | int | filter by category |
| `authorId` | int | filter by author |
| `minPrice` / `maxPrice` | decimal | price range |
| `sortBy` | string | `title`, `price`, or `publishedDate`; prefix `-` for descending, e.g. `-price` |

## Error response format

Every non-2xx response — validation failures, not-found, forbidden, or an unexpected
server error — comes back in the same shape, so frontend clients only need one error handler:

```json
{
  "statusCode": 400,
  "message": "One or more validation errors occurred.",
  "traceId": "0HN8H2example",
  "errors": {
    "price": ["'Price' must be greater than '0'."]
  }
}
```

Internal exception details, stack traces, and database errors are never sent to the client —
unexpected errors are logged server-side and returned as a generic 500.

## Design decisions

- **DTOs everywhere.** Controllers and services never accept or return EF entities — only the
  DTOs in `Dtos/`. The public contract stays stable even if the database schema changes.
- **Global exception middleware** maps known exceptions (`NotFoundException`,
  `ForbiddenException`, `ConflictException`, etc.) to the correct HTTP status with a clear
  message, and turns anything unexpected into a clean 500.
- **Ownership is enforced server-side.** The order endpoints read the user id from the JWT
  claims — never from the request body or query string — so a customer can't view or edit
  another customer's orders by guessing an id.
- **Price snapshotting.** `OrderItem.UnitPrice` captures the book's price at the moment of
  purchase, so a later price change never rewrites historical order totals.
- **Stock control.** Placing an order decrements stock and is rejected with a 400 if there
  isn't enough available.
- **No business logic in controllers.** Every controller method is a thin pass-through to an
  injected service interface — easy to unit test, easy to swap implementations.

## Roadmap / possible extensions

- Refresh tokens / token revocation
- Unit + integration test project (`WebApplicationFactory<Program>` is already wired up for it)
- Docker Compose file for API + SQL Server together
- Rate limiting on `/api/auth/login`
- Soft-delete for books/categories/authors instead of hard delete

---

Built as a learning/portfolio project demonstrating clean architecture, JWT auth, and REST API
design in ASP.NET Core.
