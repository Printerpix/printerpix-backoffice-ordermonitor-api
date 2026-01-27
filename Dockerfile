# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY nuget.config .
COPY src/OrderMonitor.Core/OrderMonitor.Core.csproj src/OrderMonitor.Core/
COPY src/OrderMonitor.Infrastructure/OrderMonitor.Infrastructure.csproj src/OrderMonitor.Infrastructure/
COPY src/OrderMonitor.Api/OrderMonitor.Api.csproj src/OrderMonitor.Api/

# Restore dependencies
RUN dotnet restore src/OrderMonitor.Api/OrderMonitor.Api.csproj

# Copy source code
COPY src/ src/

# Publish (combines build + publish in one step)
RUN dotnet publish src/OrderMonitor.Api/OrderMonitor.Api.csproj -c Release -o /app/publish

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
    CMD curl -f http://localhost:8080/health || exit 1

# Run
ENTRYPOINT ["dotnet", "OrderMonitor.Api.dll"]
