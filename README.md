# Job Portal Full Stack

A full-stack Job Portal built with ASP.NET Core Web API, React, PostgreSQL and Docker.

## Technology Stack

- Backend: ASP.NET Core Web API
- Database: PostgreSQL 17
- ORM: Entity Framework Core
- Frontend: React
- Database runtime: Docker Compose
- Version control: Git and GitHub

## Current Database Structure

The application uses five main tables:

- Users
- CandidateProfiles
- Companies
- Jobs
- Applications

## Prerequisites

Install the following:

- .NET SDK
- Docker Desktop with WSL 2
- Git
- VS Code

## Database Setup

Create your local environment file:

```powershell
Copy-Item .env.example .env

```

Open `.env` and replace the example password with your own local database password.

Start PostgreSQL:

```powershell
docker compose up -d
```

Verify that the container is healthy:

```powershell
docker compose ps
```

PostgreSQL runs with the following local configuration:

```text
Host: localhost
Port: 5433
Database: job_portal_db
```

## API Configuration

Restore the repository-local Entity Framework tool:

```powershell
dotnet tool restore
```

Store the database connection string using .NET User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=job_portal_db;Username=job_portal_user;Password=replace_with_your_local_password" --project src/JobPortal.Api/JobPortal.Api.csproj
```

The password must match the password stored in your local `.env` file.

## Apply the Database Migration

```powershell
dotnet ef database update --project src/JobPortal.Api --startup-project src/JobPortal.Api --context JobPortalDbContext
```

## Build and Run

Build the solution:

```powershell
dotnet build
```

Run the API:

```powershell
dotnet run --project src/JobPortal.Api
```

## Stop the Database

```powershell
docker compose down
```

The named Docker volume keeps the database data when the container stops.

## Branch Workflow

```text
feature/* -> Pull Request -> develop -> final Pull Request -> main
```
