# F1 Fantasy Backend

ASP.NET Core Web API for F1 Fantasy application with PostgreSQL database.

## Technical Specifications

### System Architecture

```mermaid
graph TB
    subgraph "Client Layer"
        Client[Web Client]
    end
    
    subgraph "ASP.NET Core API"
        subgraph "Middleware Pipeline"
            M1[Request Logging]
            M2[Response Compression]
            M3[Cache Headers]
            M4[IP Blacklist]
            M5[Exception Handler]
            M6[Rate Limiter]
            M7[Authentication JWT]
        end
        
        subgraph "Controllers"
            FC[F1 Data Controllers<br/>Race, Driver, Constructor, etc.]
            GC[Fantasy Controllers<br/>Groups, Predictions, Standings]
            AC[Admin Controllers<br/>Blacklist, Health]
        end
        
        subgraph "Services"
            F1S[F1 Data Services<br/>12 Services]
            FS[Fantasy Services<br/>Group, Prediction, Scoring, Standings]
            IS[Infrastructure Services<br/>Clerk, Cache, API Client]
            BS[Background Services<br/>AutoLock]
        end
        
        subgraph "Repository Layer"
            DR[Data Repositories<br/>Race, Driver, Constructor, etc.]
            FR[Fantasy Repositories<br/>Group, Prediction, Standing]
            MR[Metadata Repository<br/>Fetch History, Cache]
        end
    end
    
    subgraph "External Services"
        Ergast[Ergast F1 API<br/>jolpi.ca]
        Clerk[Clerk Auth<br/>Multi-Instance]
    end
    
    subgraph "Data Storage"
        PG[(PostgreSQL<br/>Connection Pool 10-100)]
        MC[Memory Cache<br/>Display Names]
    end
    
    Client --> M1
    M1 --> M2 --> M3 --> M4 --> M5 --> M6 --> M7
    M7 --> FC & GC & AC
    
    FC --> F1S
    GC --> FS
    AC --> IS
    
    F1S --> DR & MR
    FS --> FR & MR
    IS --> MR
    
    DR & FR & MR --> PG
    IS --> MC
    BS --> FR
    
    F1S -.HTTP.-> Ergast
    M7 & IS -.JWT Validation.-> Clerk
    
    style M4 fill:#ffcccc
    style M6 fill:#ffcccc
    style M7 fill:#ccffcc
    style Clerk fill:#ccffcc
```

### Request Processing Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant MW as Middleware Pipeline
    participant Auth as Authentication
    participant Ctrl as Controller
    participant Svc as Service
    participant Cache as Cache Layer
    participant DB as PostgreSQL
    participant API as Ergast API
    
    C->>MW: HTTP Request
    MW->>MW: Request Logging (ID + Stopwatch)
    MW->>MW: Check IP Blacklist
    alt IP Blacklisted
        MW-->>C: 403 Forbidden
    end
    
    MW->>MW: Apply Rate Limit
    alt Rate Limit Exceeded
        MW-->>C: 429 Too Many Requests
    end
    
    MW->>Auth: Validate JWT Token
    Auth->>Auth: Check Multi-Instance Clerk
    alt Invalid Token
        Auth-->>C: 401 Unauthorized
    end
    
    Auth->>Ctrl: Authorized Request
    Ctrl->>Svc: Service Method Call
    
    Svc->>Cache: Check DB Cache
    Cache->>DB: Query Cached Data
    DB-->>Cache: Return Data + Metadata
    
    Cache->>Cache: Evaluate Staleness<br/>(Past: 7d, Current: 1h)
    
    alt Cache Valid
        Cache-->>Svc: Return Cached Data
    else Cache Stale/Missing
        Svc->>API: Fetch from Ergast API
        API->>API: Apply Rate Limit<br/>(100ms polite delay)
        alt API Success
            API-->>Svc: Return Fresh Data
            Svc->>DB: Update Cache + Metadata
        else API Failure (429)
            API->>API: Exponential Backoff<br/>(500ms → 8s, max 5 retries)
            alt Retry Success
                API-->>Svc: Return Data
            else All Retries Failed
                Svc-->>Ctrl: Return Cached Data (Fallback)
            end
        end
    end
    
    Svc-->>Ctrl: Return Data
    Ctrl->>MW: HTTP Response
    MW->>MW: Set Cache Headers
    MW->>MW: Compress Response (Brotli/Gzip)
    MW->>MW: Log Duration
    MW-->>C: Final Response
```

### Service Layer Architecture

```mermaid
graph LR
    subgraph "F1 Data Services"
        RS[RaceService]
        DS[DriverService]
        CS[ConstructorService]
        ResS[ResultService]
        QS[QualifyingService]
        PS[PitStopService]
        LS[LapTimingService]
        DSS[DriverStandingService]
        CSS[ConstructorStandingService]
        StS[StatusService]
        SS[SeasonService]
        CiS[CircuitService]
    end
    
    subgraph "Fantasy Services"
        GS[GroupService]
        PrS[PredictionService]
        ScS[ScoringService]
        StdS[StandingsService]
    end
    
    subgraph "Infrastructure Services"
        ClerkS[ClerkService<br/>3-Tier Cache]
        CacheS[CacheStalenessService<br/>Smart Expiration]
        ApiC[ApiHttpClient<br/>Retry + Backoff]
        BL[IpBlacklistService<br/>Thread-Safe]
        PST[PaginationStateTracker<br/>Singleton]
        RVM[RateLimitViolationMonitor<br/>Auto-Blacklist]
    end
    
    subgraph "Shared Dependencies"
        HC[HttpClient]
        MemC[MemoryCache]
        DBCtx[DbContext/Factory]
    end
    
    RS & DS & CS & ResS & QS & PS & LS & DSS & CSS & StS & SS & CiS --> ApiC
    RS & DS & CS & ResS & QS & PS & LS & DSS & CSS & StS & SS & CiS --> CacheS
    RS & DS & CS & ResS & QS & PS & LS & DSS & CSS & StS & SS & CiS --> DBCtx
    
    GS & PrS & ScS & StdS --> DBCtx
    ScS --> ResS & QS & DSS & CSS
    StdS --> ScS
    
    ClerkS --> MemC
    ClerkS --> DBCtx
    ApiC --> HC
    ApiC --> PST
    CacheS --> DBCtx
    BL & RVM --> MemC
    
    style ApiC fill:#ffe6cc
    style CacheS fill:#ffe6cc
    style ClerkS fill:#e6ccff
```

### Data Flow & Caching Strategy

```mermaid
flowchart TD
    Start([API Request]) --> CheckAuth{JWT Valid?}
    CheckAuth -->|No| Unauth[401 Unauthorized]
    CheckAuth -->|Yes| CheckCache{Check DB Cache}
    
    CheckCache --> EvalStale{Evaluate Staleness}
    
    EvalStale -->|Past Season<br/>< 7 days old| ReturnCache[Return Cached Data]
    EvalStale -->|Current Season<br/>< 1 hour old| ReturnCache
    EvalStale -->|Stale or Missing| FetchAPI[Fetch from Ergast API]
    
    FetchAPI --> CheckResponse{API Response}
    CheckResponse -->|Success| UpdateDB[Update DB Cache]
    CheckResponse -->|429 Rate Limit| Backoff[Exponential Backoff<br/>500ms → 8s]
    CheckResponse -->|Error| CheckRetry{Retries < 5?}
    
    Backoff --> CheckRetry
    CheckRetry -->|Yes| FetchAPI
    CheckRetry -->|No| Fallback[Return Stale Cache<br/>if available]
    
    UpdateDB --> UpdateMeta[Update DataFetchMetadata<br/>timestamp, success]
    UpdateMeta --> ReturnFresh[Return Fresh Data]
    
    ReturnCache --> SetHeaders[Set Cache-Control Headers]
    ReturnFresh --> SetHeaders
    Fallback --> SetHeaders
    
    SetHeaders --> Compress[Compress Response<br/>Brotli/Gzip]
    Compress --> End([Return to Client])
    
    style CheckCache fill:#cce6ff
    style UpdateDB fill:#cce6ff
    style Backoff fill:#ffcccc
    style Fallback fill:#ffffcc
```

### Database Schema

```mermaid
erDiagram
    Race ||--o{ Result : "has many"
    Race ||--o{ Qualifying : "has many"
    Race ||--o{ PitStop : "has many"
    Race ||--o{ LapTiming : "has many"
    Race {
        int Season PK
        int Round PK
        string RaceName
        json Circuit
        json Sessions
        datetime Date
    }
    
    Driver ||--o{ Result : "has many"
    Driver ||--o{ DriverStanding : "has many"
    Driver {
        string DriverId PK
        string GivenName
        string FamilyName
        string Nationality
        string Code
        int PermanentNumber
    }
    
    Constructor ||--o{ Result : "has many"
    Constructor ||--o{ ConstructorStanding : "has many"
    Constructor {
        string ConstructorId PK
        string Name
        string Nationality
    }
    
    Result {
        int ResultId PK
        int Season FK
        int Round FK
        string DriverId FK
        string ConstructorId FK
        int Position
        bool IsSprint
    }
    
    Group ||--o{ GroupMember : "has many"
    Group ||--o{ Prediction : "has many"
    Group ||--o{ Standing : "has many"
    Group {
        int GroupId PK
        string Name
        string InviteCode UK
        string AdminUserId
        string LockMode
        bool PredictionsLocked
    }
    
    GroupMember {
        int GroupMemberId PK
        int GroupId FK
        string UserId
    }
    
    Prediction {
        int PredictionId PK
        int GroupId FK
        string UserId
        string PredictionType
        json PredictionData
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    Standing {
        int StandingId PK
        int GroupId FK
        string UserId
        int TotalScore
        int Rank
        json CategoryScoresJson
        datetime LastRecalculated
    }
    
    DataFetchMetadata {
        int Id PK
        int Season
        string DataType
        datetime LastFetched
        bool Success
    }
    
    UserDisplayNameCache {
        string UserId PK
        string DisplayName
        datetime ExpiresAt
    }
```

### Authentication & Security Layers

```mermaid
flowchart TD
    Request[Incoming Request] --> Layer1{Layer 1:<br/>IP Blacklist}
    
    Layer1 -->|Blacklisted| Block1[403 Forbidden]
    Layer1 -->|Allowed| Layer2{Layer 2:<br/>Rate Limiting}
    
    Layer2 -->|Exceeded| RateCheck[Violation Monitor<br/>Track violations]
    RateCheck --> CountCheck{10+ violations<br/>in 5 minutes?}
    CountCheck -->|Yes| AutoBlock[Auto-Blacklist IP<br/>1 hour timeout]
    CountCheck -->|No| Block2[429 Too Many Requests]
    AutoBlock --> Block2
    
    Layer2 -->|Within Limit| Layer3{Layer 3:<br/>JWT Authentication}
    
    Layer3 -->|Missing/Invalid| Block3[401 Unauthorized]
    Layer3 -->|Valid| Layer4[Layer 4:<br/>Multi-Instance Validation]
    
    Layer4 --> ClerkCheck{Check Clerk Instance}
    ClerkCheck -->|Production| ProdJWT[Validate against<br/>clerk.f1fantasy.no]
    ClerkCheck -->|Development| DevJWT[Validate against<br/>clerk.accounts.dev]
    
    ProdJWT & DevJWT --> Layer5{Layer 5:<br/>Authorization}
    
    Layer5 -->|Role Missing| Block4[403 Forbidden]
    Layer5 -->|Authorized| Success[Process Request]
    
    Success --> Controller[Route to Controller]
    
    style Layer1 fill:#ffcccc
    style Layer2 fill:#ffcccc
    style Layer3 fill:#ccffcc
    style Layer4 fill:#ccffcc
    style Layer5 fill:#cce6ff
    style AutoBlock fill:#ff9999
```

### Background Services & Automation

```mermaid
sequenceDiagram
    participant Timer as Timer (5 min interval)
    participant AutoLock as AutoLockService
    participant GroupRepo as GroupRepository
    participant RaceRepo as RaceRepository
    participant DB as PostgreSQL
    
    loop Every 5 Minutes
        Timer->>AutoLock: Execute Scheduled Task
        AutoLock->>GroupRepo: Get All Groups
        GroupRepo->>DB: Query Groups
        DB-->>GroupRepo: Return Groups List
        GroupRepo-->>AutoLock: Groups Data
        
        loop For Each Group
            AutoLock->>AutoLock: Check if PredictionsLocked = false
            alt Not Yet Locked
                AutoLock->>RaceRepo: Get Current Season Races
                RaceRepo->>DB: Query Races for 2026
                DB-->>RaceRepo: Return Race Schedule
                RaceRepo-->>AutoLock: Race Schedule
                
                AutoLock->>AutoLock: Find First Race
                AutoLock->>AutoLock: Check if Date <= Now
                
                alt First Race Started
                    AutoLock->>GroupRepo: Lock Group Predictions
                    GroupRepo->>DB: UPDATE PredictionsLocked = true
                    DB-->>GroupRepo: Confirm Update
                    AutoLock->>AutoLock: Log: Group {id} auto-locked
                end
            end
        end
        
        AutoLock-->>Timer: Await Next Interval
    end
```

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
