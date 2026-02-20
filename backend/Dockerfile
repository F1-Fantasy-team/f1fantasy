# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install EF Core tools for migrations
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy csproj and restore dependencies
COPY F1Fantasy/F1Fantasy.csproj F1Fantasy/
RUN dotnet restore F1Fantasy/F1Fantasy.csproj

# Copy everything else and build
COPY . .
WORKDIR /src/F1Fantasy
RUN dotnet build F1Fantasy.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish F1Fantasy.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published app
COPY --from=publish /app/publish .

# Copy migration bundle (if you generate one, optional)
# Alternatively, install dotnet-ef in runtime image
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Install dotnet-ef tool in runtime for migrations
RUN dotnet tool install --global dotnet-ef --version 10.0.0
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy the entire project for migrations (needed for DbContext)
COPY --from=build /src/F1Fantasy /src/F1Fantasy

# Environment variables (override at runtime)
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

# Entry point script to run migrations then start app
COPY <<EOF /app/entrypoint.sh
#!/bin/bash
set -e

echo "Running database migrations..."
cd /src/F1Fantasy
dotnet ef database update --no-build || echo "Migration failed or already up to date"

echo "Starting application..."
cd /app
exec dotnet F1Fantasy.dll
EOF

RUN chmod +x /app/entrypoint.sh

ENTRYPOINT ["/app/entrypoint.sh"]
