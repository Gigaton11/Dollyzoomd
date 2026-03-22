# DollyZoomd

DollyZoomd is a cloud-ready TV discovery and tracking backend built with ASP.NET Core and PostgreSQL.
It powers a web experience for discovering popular shows, tracking watch status, managing favorites, and sharing comments.

## Live Demo

Visit [Dollyzoomd](https://dollyzoomd-847147860815.europe-west1.run.app/#home).

## Tech Stack

- ASP.NET Core (.NET 10)
- Entity Framework Core + PostgreSQL
- JWT authentication
- Google Cloud Run + Cloud SQL + Cloud Storage
- Scalar OpenAPI UI

## Installation

### 1. Prerequisites

- .NET SDK 10
- PostgreSQL 15+
- Git

### 2. Clone the repository

```bash
git clone <your-repo-url>
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

## License

No license is currently specified for this repository.
All rights are reserved by default unless a license is added in the future.
