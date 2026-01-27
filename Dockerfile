# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY OrderMonitor.sln .
COPY nuget.config .
COPY src/OrderMonitor.Api/OrderMonitor.Api.csproj src/OrderMonitor.Api/
COPY src/OrderMonitor.Core/OrderMonitor.Core.csproj src/OrderMonitor.Core/
COPY src/OrderMonitor.Infrastructure/OrderMonitor.Infrastructure.csproj src/OrderMonitor.Infrastructure/

# Restore dependencies (API project only, not test projects)
RUN dotnet restore src/OrderMonitor.Api/OrderMonitor.Api.csproj

# Copy source code
COPY src/ src/

# Build API project only
RUN dotnet build src/OrderMonitor.Api/OrderMonitor.Api.csproj -c Release --no-restore

# Publish
RUN dotnet publish src/OrderMonitor.Api/OrderMonitor.Api.csproj -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/api/health || exit 1

# Run
ENTRYPOINT ["dotnet", "OrderMonitor.Api.dll"]
