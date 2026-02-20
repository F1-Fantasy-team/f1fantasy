# F1 Fantasy Backend

ASP.NET Core Web API for F1 Fantasy application with PostgreSQL database.

## Prerequisites

- .NET 10.0 SDK
- PostgreSQL database (or use the provided Render.com instance)

## Local Development Setup

### 1. Environment Variables

Copy the `.env.example` file to `.env`:

```bash
cp .env.example .env
```

Then update the `.env` file with your actual database credentials:

```env
DATABASE_URL=postgresql://username:password@host:port/database
DB_PASSWORD=your_database_password
ConnectionStrings__DefaultConnection=Host=your-host;Database=your-database;Username=your-username;Password=your-password;SSL Mode=Require;Trust Server Certificate=true
```

**Note:** The `.env` file is already in `.gitignore` and will NOT be committed to version control.

### 2. Install Dependencies

```bash
cd F1Fantasy
dotnet restore
```

### 3. Run the Application

```bash
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## GitHub Actions Secrets

For CI/CD, configure the following secrets in your GitHub repository:

1. Go to **Settings** → **Secrets and variables** → **Actions**
2. Add the following secrets:

| Secret Name | Description | Example Value |
|------------|-------------|---------------|
| `DATABASE_CONNECTION_STRING` | Full EF Core connection string | `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true` |
| `DATABASE_URL` | PostgreSQL connection URL | `postgresql://user:pass@host:port/db` |
| `DB_PASSWORD` | Database password | `your_secure_password` |

## Database Configuration

The application uses **PostgreSQL** with **Entity Framework Core**. All data is now persisted to the database instead of in-memory storage.

### Database Setup

1. **Connection String**: The connection is loaded from `.env` file (development) or environment variables (production)

2. **Initial Migration**: Already created. To apply to a new database:
```bash
dotnet ef database update
```

3. **Create New Migration** (after model changes):
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Connection String Format

```
Host=your-host;Database=your-database;Username=your-username;Password=your-password;SSL Mode=Require;Trust Server Certificate=true
```

### Database Schema

The application creates the following tables:
- **Races** - F1 race information with embedded circuit and session data
- **Seasons** - F1 seasons from 1950 to present
- **Circuits** - Race circuit information with location data
- **Constructors** - F1 constructor teams
- **Drivers** - F1 driver information

All data is fetched from the Ergast API and cached in PostgreSQL for performance and offline availability.

## API Endpoints

### Races
- `GET /api/race` - Get all races with pagination
- `GET /api/race/{season}` - Get races for a specific season
- `GET /api/race/{season}/{round}` - Get specific race
- `GET /api/race/cached` - Get cached races

### Seasons
- `GET /api/season` - Get all seasons
- `GET /api/season/{year}` - Get specific season
- `GET /api/season/cached` - Get cached seasons

### Circuits
- `GET /api/circuit` - Get all circuits
- `GET /api/circuit/{circuitId}` - Get specific circuit
- `GET /api/circuit/cached` - Get cached circuits

### Constructors
- `GET /api/constructor` - Get all constructors
- `GET /api/constructor/season/{season}` - Get constructors for a season
- `GET /api/constructor/{constructorId}` - Get specific constructor
- `GET /api/constructor/cached` - Get cached constructors

### Drivers
- `GET /api/driver` - Get all drivers
- `GET /api/driver/season/{season}` - Get drivers for a season
- `GET /api/driver/{driverId}` - Get specific driver
- `GET /api/driver/cached` - Get cached drivers

## Project Structure

```
F1Fantasy/
├── Controllers/          # API Controllers
├── Models/              # Data models
├── Repository/          # In-memory repositories
├── Services/            # Business logic & API integration
├── Program.cs           # Application entry point
└── appsettings.json     # Configuration

F1Fantasy.Tests/
└── *IntegrationTests.cs # Integration tests
```

## Data Source

This API integrates with the [Ergast F1 API](https://ergast.com/mrd/) for F1 data with built-in:
- Rate limiting protection
- Exponential backoff retry logic
- Pagination state tracking
- Fallback to cached data

## Testing

Run all tests:
```bash
cd F1Fantasy.Tests
dotnet test
```

Run specific test category:
```bash
dotnet test --filter "FullyQualifiedName~DriverServiceIntegrationTests"
```

## Notes

- The application uses in-memory repositories for caching API responses
- Rate limiting is handled with exponential backoff (500ms → 8s)
- Pagination state is tracked for resumable fetches after failures
- All endpoints support fallback to cached data when API is unavailable
