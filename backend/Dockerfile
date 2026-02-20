# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

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

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "F1Fantasy.dll"]
