# BookStore Web API

A .NET 8 Web API for an online bookstore: customers browse/search books and place orders,
admins manage the catalog, and everything is documented in Swagger with JWT-secured endpoints.

## Tech stack

- **.NET 8 / ASP.NET Core Web API**
- **Entity Framework Core 8** + **SQL Server**
- **JWT Bearer authentication**, two roles: `Customer`, `Admin`
- **FluentValidation** for request validation
- **Swashbuckle (Swagger/OpenAPI)** with a working "Authorize" button
- **BCrypt.Net** for password hashing
- Global exception-handling middleware (no stack traces ever reach the client)

## Project structure

```
src/BookStoreApi/
  Controllers/        Thin controllers — HTTP concerns only, no business logic
  Services/            Interfaces + Implementations — all business logic lives here
  Models/               EF Core entities (never returned directly to clients)
  Dtos/                  Request/response contracts, grouped by feature
  Data/                  AppDbContext
  Validators/            FluentValidation rules per DTO
  Middleware/            Global exception handler
  Exceptions/            Custom exceptions mapped to HTTP status codes
  Extensions/             DI/Swagger/JWT/CORS setup, claims helpers
  Program.cs
  appsettings.json
```

Controllers depend only on service **interfaces** (constructor-injected), so business logic,
validation, and persistence are fully decoupled from HTTP concerns and easy to unit test.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A running SQL Server instance (local SQL Server, SQL Server Express, or SQL Server in Docker)

Quick SQL Server via Docker, if you don't already have one:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 --name bookstore-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

## 1. Configure the connection string and JWT secret

Edit `src/BookStoreApi/appsettings.json` (or better, use `dotnet user-secrets` so secrets
never get committed):

```bash
cd src/BookStoreApi
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=BookStoreDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:SigningKey" "<a long random string, at least 32 characters>"
```

## 2. Restore, create the migration, and apply it

The repo intentionally ships **without** a pre-generated `Migrations/` folder, since migration
files are tied to the SDK/EF tools version used to generate them. Create them locally:

```bash
dotnet tool install --global dotnet-ef   # if you don't already have it
cd src/BookStoreApi
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 3. Run the API

```bash
dotnet run
```

By default the API listens on `http://localhost:5080` (see `Properties/launchSettings.json`).
On startup it automatically applies any pending EF Core migrations.

Open **`http://localhost:5080/swagger`** — this launches automatically in dev mode.

## 4. Register the first admin account

There's no public "become admin" endpoint on purpose — that would be a security hole.
Instead:

1. Register a normal account through the API (Swagger, curl, or the included `.http` file):

   ```http
   POST /api/auth/register
   Content-Type: application/json

   { "fullName": "Admin User", "email": "admin@bookstore.com", "password": "P@ssword123" }
   ```

2. Promote that user to Admin directly in the database:

   ```sql
   UPDATE Users SET Role = 1 WHERE Email = 'admin@bookstore.com';
   -- Role 0 = Customer, Role 1 = Admin
   ```

3. Log in again (`POST /api/auth/login`) to get a fresh JWT — the new token will contain the
   `Admin` role claim. Every subsequent admin-only call must use this new token (the old token,
   issued before the promotion, still says "Customer" until it's refreshed).

## 5. Test the API

**Swagger UI** (`/swagger`) supports authenticated calls end-to-end:
1. Call `POST /api/auth/login`, copy the `token` value from the response.
2. Click **Authorize** at the top of the Swagger page, enter `Bearer <token>`, click Authorize.
3. All subsequent "Try it out" calls will include the token automatically.

**`.http` file**: `src/BookStoreApi/BookStoreApi.http` has ready-made requests (register, login,
browse/filter books, admin CRUD, place an order, view orders). Works out of the box with the
VS Code "REST Client" extension or the built-in HTTP client in Rider/Visual Studio 2022+.

**curl**:
```bash
curl -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@bookstore.com","password":"P@ssword123"}'
```

## API overview

All responses are JSON. All error responses share this shape:

```json
{
  "statusCode": 400,
  "message": "One or more validation errors occurred.",
  "traceId": "0HN...",
  "errors": { "price": ["'Price' must be greater than '0'."] }
}
```

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | — | Register a customer account |
| POST | `/api/auth/login` | — | Log in, returns JWT |
| GET | `/api/books` | — | Browse/search/filter/paginate books |
| GET | `/api/books/{id}` | — | Book details |
| POST | `/api/books` | Admin | Create book |
| PUT | `/api/books/{id}` | Admin | Update book |
| DELETE | `/api/books/{id}` | Admin | Delete book |
| GET | `/api/categories` | — | List categories |
| GET | `/api/categories/{id}` | — | Category details |
| POST/PUT/DELETE | `/api/categories(/{id})` | Admin | Manage categories |
| GET | `/api/authors` | — | List authors |
| GET | `/api/authors/{id}` | — | Author details |
| POST/PUT/DELETE | `/api/authors(/{id})` | Admin | Manage authors |
| POST | `/api/orders` | Customer/Admin | Place an order |
| GET | `/api/orders/mine` | Customer/Admin | View my own orders |
| GET | `/api/orders/{id}` | Customer/Admin | View one order (owner or admin only) |
| GET | `/api/orders` | Admin | View every order from every customer |

`GET /api/books` query parameters: `page`, `pageSize` (max 100), `search`, `categoryId`,
`authorId`, `minPrice`, `maxPrice`, `sortBy` (`title` \| `price` \| `publishedDate`, prefix
with `-` for descending, e.g. `-price`).

## Design notes

- **DTOs everywhere**: controllers/services never accept or return EF entities — only DTOs in
  `Dtos/`. This satisfies "never expose database entities to clients" and keeps the public
  contract stable even if the schema changes.
- **Global exception middleware** (`Middleware/ExceptionHandlingMiddleware.cs`) catches every
  exception. Known exceptions (`NotFoundException`, `ForbiddenException`, etc.) map to the
  correct status code with a clear message; anything unexpected becomes a generic 500 with no
  internal details leaked, and is logged server-side with the full exception.
- **Ownership enforcement**: `OrdersController`/`OrderService` read the user id from the JWT
  claims (never from the request body), so a customer can never view or manipulate another
  customer's orders by guessing an id.
- **Order pricing**: `OrderItem.UnitPrice` snapshots the book price at purchase time, so later
  price changes never rewrite historical order totals. Stock is decremented on order creation
  and orders are rejected (400) if stock is insufficient.
- **CORS**: configured in `appsettings.json` under `Cors:AllowedOrigins` — add your frontend's
  origin there (localhost origins for React/Vite/Angular dev servers are pre-populated).

## Running tests

This submission focuses on the working API; if you add a test project, the typical setup is:

```bash
dotnet new xunit -o tests/BookStoreApi.Tests
dotnet add tests/BookStoreApi.Tests reference src/BookStoreApi
dotnet test
```

`Program.cs` exposes a `public partial class Program` for `WebApplicationFactory<Program>`
integration tests.
