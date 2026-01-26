# CI/CD Pipeline Documentation

## Overview

This repository uses GitHub Actions for continuous integration and deployment.

## Workflows

### 1. CI (`ci.yml`)

**Triggers:** Push to `main`/`develop`, Pull requests to `main`/`develop`

**Jobs:**
- **Build & Test**: Builds the solution and runs all tests with code coverage
- **Code Analysis**: Runs static code analysis
- **Security Scan**: Checks for vulnerable dependencies

**Artifacts:**
- Test results (TRX format)
- Code coverage reports (Cobertura XML)
- Security audit report

### 2. Docker Build (`docker.yml`)

**Triggers:** Push to `main`, Tags (`v*`), Manual dispatch

**Features:**
- Builds multi-platform Docker image (linux/amd64)
- Pushes to GitHub Container Registry (ghcr.io)
- Generates SBOM (Software Bill of Materials)
- Uses build cache for faster builds

**Image Tags:**
- `main` - Latest from main branch
- `sha-<commit>` - Specific commit
- `v1.0.0` - Semantic version tags

### 3. Release (`release.yml`)

**Triggers:** Tags (`v*`)

**Features:**
- Creates GitHub Release
- Generates changelog from PRs
- Publishes release artifacts (ZIP)

## Branch Protection (Recommended)

Configure these settings for the `main` branch in GitHub:

```yaml
Branch Protection Rules:
  - Require pull request reviews: Yes
    - Required approving reviews: 1
    - Dismiss stale reviews: Yes
  - Require status checks to pass:
    - build (CI)
    - lint (CI)
    - security (CI)
  - Require branches to be up to date: Yes
  - Restrict who can push: Enabled
  - Do not allow bypassing: Yes
```

## Environment Variables

### Required Secrets

| Secret | Description | Used By |
|--------|-------------|---------|
| `GITHUB_TOKEN` | Auto-provided by GitHub | All workflows |

### Optional Secrets (for on-prem deployment)

| Secret | Description |
|--------|-------------|
| `DEPLOY_HOST` | On-prem server hostname |
| `DEPLOY_USER` | SSH user for deployment |
| `DEPLOY_KEY` | SSH private key |

## Local Development

### Run CI Locally

```bash
# Build
dotnet build --configuration Release

# Test with coverage
dotnet test --collect:"XPlat Code Coverage"

# Security scan
dotnet list package --vulnerable --include-transitive
```

### Build Docker Image Locally

```bash
docker build -t ordermonitor-api:local .
docker run -p 8080:8080 ordermonitor-api:local
```

## Versioning

This project uses Semantic Versioning:
- `MAJOR.MINOR.PATCH` (e.g., `1.0.0`)
- Pre-release: `1.0.0-alpha.1`, `1.0.0-beta.1`, `1.0.0-rc.1`

To create a release:
```bash
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

## Dependency Updates

Dependabot is configured to:
- Update NuGet packages weekly
- Update GitHub Actions weekly
- Update Docker base images weekly

Review and merge Dependabot PRs promptly to keep dependencies secure.
