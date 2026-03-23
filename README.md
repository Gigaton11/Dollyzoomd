# Dollyzoomd 🎥

Dollyzoomd is a cloud-ready TV discovery and tracking backend built with ASP.NET Core and PostgreSQL.
It powers a web experience for discovering popular shows, tracking watch status, managing favorites, and sharing comments.

## Live Demo

Visit [Dollyzoomd](https://dollyzoomd-847147860815.europe-west1.run.app/#home).

| Field    | Value           |
|----------|-----------------|
| Username | `demo`          |
| Password | `demo123!`       |

## Tech Stack

- ASP.NET Core (.NET 10)
- Entity Framework Core + PostgreSQL
- JWT authentication
- Google Cloud Run + Cloud SQL + Cloud Storage
- Scalar OpenAPI UI

## Cloud Architecture

- Cloud Run hosts and auto-scales the ASP.NET API container. It handles incoming traffic and scales to zero when idle.
- Cloud SQL (PostgreSQL) is the system of record for users, watchlists, favorites, comments, and cached discover data.
- Cloud Storage is used for avatar image objects, so media files are decoupled from the API container filesystem.

Request flow:

1. A client calls the API endpoint on Cloud Run.
2. Cloud Run executes business logic and reads/writes relational data in Cloud SQL.
3. If the operation includes avatar media, the API uploads to or reads from Cloud Storage and stores the file reference in Cloud SQL.
4. The API returns a response that combines structured database data with media URLs when needed.

Why this split:

- Compute, database, and object storage scale independently.
- Stateless containers remain replaceable and safe for autoscaling.
- PostgreSQL handles transactional consistency, while Cloud Storage handles durable binary assets efficiently.

## Local Installation

### 1. Prerequisites

- .NET SDK 10
- PostgreSQL 15+
- Git

### 2. Clone the repository

```bash
git clone <https://github.com/Gigaton11/Dollyzoomd.git>
cd DollyZoomd
```

### 3. Configure environment

Set your connection string and JWT settings through environment variables or local config.

Required values:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Secret` (minimum 32 characters)
- `Jwt__Issuer`
- `Jwt__Audience`

Optional startup migration toggle:

- `APPLY_MIGRATIONS_ON_STARTUP=true`

### 4. Apply database migrations

From the project root:

```bash
dotnet ef database update --project DollyZoomd/DollyZoomd.csproj
```

### 5. Run the API

```bash
dotnet run --project DollyZoomd/DollyZoomd.csproj
```

Default local URL:

- `http://localhost:5265`

## Usage Examples

### Health check

```bash
curl http://localhost:5265/health
```

### Register

```bash
curl -X POST http://localhost:5265/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"neo","email":"neo@example.com","password":"StrongPass123!"}'
```

### Login

```bash
curl -X POST http://localhost:5265/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"identifier":"neo@example.com","password":"StrongPass123!"}'
```

### Search shows

```bash
curl "http://localhost:5265/api/shows/search?query=severance&page=0"
```

### Discover popular shows

```bash
curl "http://localhost:5265/api/discover/popular?take=20&skip=0"
```

### Add to favorites (authenticated)

```bash
curl -X POST http://localhost:5265/api/favorites \
  -H "Authorization: Bearer <JWT_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"tvMazeId":82,"name":"Game of Thrones"}'
```

### API Reference (development)

When running locally in Development mode, open Scalar at:

- `http://localhost:5265/scalar`

