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

You can run the application locally using either .NET SDK or Docker.

#### Option A: Using .NET SDK

```bash
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

#### Option B: Using Docker Compose (Recommended for Development)

1. Make sure your `.env` file exists in the project root with the required environment variables

2. Start the application:
```bash
docker-compose up
```

3. The API will be available at `http://localhost:10000`

4. Swagger UI is available at `http://localhost:10000/swagger` (Development mode)

5. To stop the application:
```bash
docker-compose down
```

#### Option C: Using Docker Directly

1. Build the Docker image:
```bash
docker build -t f1fantasy .
```

2. Run the container:
```bash
docker run -p 10000:10000 -e ASPNETCORE_ENVIRONMENT=Development -e ConnectionStrings__DefaultConnection="your-connection-string" f1fantasy
```

3. The API will be available at `http://localhost:10000`

**Note**: Docker Compose uses port 10000 to match the production environment on Render.com. The `docker-compose.yml` file is only used for local development and does not affect the Render.com build process.

## Production Deployment (Render.com)

The application is deployed to Render.com, which handles automatic builds and deployments from the GitHub repository.

### Environment Variables on Render

Configure the following environment variables in your Render service:

1. Go to your Render service **Dashboard** → **Environment**
2. Add the following environment variables:

| Environment Variable | Description | Example Value |
|------------|-------------|---------------|
| `ConnectionStrings__DefaultConnection` | Full EF Core connection string | `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true` |
| `ASPNETCORE_ENVIRONMENT` | Application environment | `Production` |

**Important Notes:**
- Render automatically provides a `PORT` environment variable (default: 10000)
- HTTPS is handled by Render's proxy - the application runs on HTTP internally
- Database credentials can be copied from your Render PostgreSQL service

### Deployment Process

1. Push changes to your GitHub repository
2. Render automatically detects changes and triggers a build
3. The Dockerfile is used to build and deploy the application
4. The service is available at: `https://your-service.onrender.com`

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
