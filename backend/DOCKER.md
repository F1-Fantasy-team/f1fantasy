# Docker Deployment Guide

## Overview

The F1 Fantasy API uses Docker for containerized deployment with automatic database migrations.

## Quick Start

### Using Docker Compose (Recommended for Development)

```bash
# Make sure your .env file exists with database credentials
docker-compose up --build
```

The API will be available at:
- HTTP: http://localhost:5000
- HTTPS: http://localhost:5001

### Using Docker Directly

```bash
# Build the image
docker build -t f1fantasy-api .

# Run with environment variables
docker run -p 5000:8080 \
  -e "ConnectionStrings__DefaultConnection=Host=dpg-xxx.render.com;Database=fantasyf1;Username=fantasyf1;Password=xxx;SSL Mode=Require;Trust Server Certificate=true" \
  f1fantasy-api
```

## Environment Variables

The container requires these environment variables:

- `ConnectionStrings__DefaultConnection` - Full PostgreSQL connection string
- `DATABASE_URL` - Render.com database URL (optional, used by some deploy platforms)
- `DB_PASSWORD` - Database password (optional backup)

## Automatic Migrations

The Dockerfile includes an entrypoint script that:
1. Runs `dotnet ef database update` on startup
2. Applies any pending migrations
3. Starts the application

This ensures your database schema is always up to date.

## GitHub Actions Secrets

For CI/CD, configure these secrets in your GitHub repository:

1. Go to Settings → Secrets and variables → Actions
2. Add these repository secrets:
   - `DATABASE_CONNECTION_STRING` - Full connection string for tests
   - `DATABASE_URL` - Render.com database URL
   - `DB_PASSWORD` - Database password

## Deployment to Render.com

Render will automatically:
1. Build the Docker image
2. Set environment variables from your Render dashboard
3. Run migrations via the entrypoint script
4. Start the API

No manual migration steps needed!

## Health Check

The container includes a health check at `/health` endpoint (if configured in your API).

## Troubleshooting

### Migrations Fail
- Check that `ConnectionStrings__DefaultConnection` is set correctly
- Verify the database is accessible from the container
- Check logs: `docker logs <container-id>`

### Connection Issues
- Ensure PostgreSQL allows connections from Docker network
- Verify SSL settings in connection string
- For local development, use `SSL Mode=Prefer` instead of `Require`

### Build Errors
- Clear Docker cache: `docker build --no-cache -t f1fantasy-api .`
- Check that all NuGet packages restore correctly
