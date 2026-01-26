# Order Monitor API

.NET Core 8 API service for monitoring stuck orders in the Printerpix Backoffice database.

## Overview

This service:
- Monitors 152 order statuses for stuck orders
- Detects orders exceeding configurable thresholds (6h for prep, 48h for facility)
- Sends email alerts to operations team
- Provides REST API for querying stuck orders

## Project Structure

```
printerpix-backoffice-ordermonitor-api/
├── src/
│   ├── OrderMonitor.Api/           # ASP.NET Core Web API
│   ├── OrderMonitor.Core/          # Domain entities, interfaces, services
│   └── OrderMonitor.Infrastructure/ # Data access, email, background jobs
├── tests/
│   ├── OrderMonitor.UnitTests/     # Unit tests (xUnit)
│   └── OrderMonitor.IntegrationTests/ # Integration tests
├── docs/                           # Documentation
└── .github/workflows/              # CI/CD pipelines
```

## Prerequisites

- .NET 8.0 SDK
- SQL Server (read-only access to Backoffice database)
- SMTP server access (pod51017.outlook.com)

## Configuration

### Environment Variables

```bash
# Required
SMTP_PASSWORD=<your-smtp-password>

# Optional (can also be in appsettings.json)
ConnectionStrings__BackofficeDb=<connection-string>
```

### appsettings.json

See `src/OrderMonitor.Api/appsettings.json` for full configuration options.

## Running Locally

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run API
dotnet run --project src/OrderMonitor.Api
```

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/orders/stuck` | GET | Get all stuck orders |
| `/api/orders/{id}/status-history` | GET | Get order status history |
| `/api/orders/stuck/summary` | GET | Get stuck orders summary |
| `/api/health` | GET | Health check |
| `/api/metrics` | GET | Service metrics |
| `/api/alerts/test` | POST | Test email alert |

## Status Thresholds

| Status Range | Threshold |
|--------------|-----------|
| 3001-3910 (Prep) | 6 hours |
| 4001-5830 (Facility) | 48 hours |

## JIRA

- Epic: [BD-689](https://printerpix.atlassian.net/browse/BD-689)
- Related: [BD-411](https://printerpix.atlassian.net/browse/BD-411)

## License

Proprietary - Printerpix
