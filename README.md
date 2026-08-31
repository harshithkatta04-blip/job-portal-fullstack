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