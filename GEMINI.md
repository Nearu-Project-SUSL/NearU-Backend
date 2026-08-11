# NearU Backend - Project Instructions

This document provides foundational mandates, architectural patterns, and development workflows for the NearU Backend project. Adhere to these instructions to ensure consistency and quality across the codebase.

## 🏗️ Architecture & Patterns

### 1. Layered Architecture
We follow a strict Controller-Service-Repository pattern:
- **Controllers:** Handle HTTP requests, validate input, and return HTTP responses. Keep logic minimal.
- **Services:** Contain business logic. Orchestrate multiple repositories if necessary.
- **Repositories:** Abstract data access. Use Entity Framework Core for database operations.
- **DTOs (Data Transfer Objects):** Use DTOs for all API requests and responses. Never expose raw database entities directly.

### 2. Dependency Injection
- Register all services and repositories in `Program.cs`.
- Prefer `Scoped` lifetime for services and repositories interacting with `DbContext`.

### 3. Database & ORM
- **Framework:** Entity Framework Core 10.
- **Database:** PostgreSQL.
- **Migrations:** Use Code-First migrations. Always review generated migrations before applying.
- **Spatial Data:** Use NetTopologySuite for location-based features (e.g., Ride module).

### 4. Authentication & Authorization
- **JWT:** Use JWT Bearer tokens for authentication.
- **Refresh Tokens:** Implement refresh token rotation for secure long-lived sessions.
- **RBAC:** Use Role-Based Access Control (Student, Business Owner, Rider, Admin).

## 🛠️ Development Workflow

### 1. Branching & Commits
- Follow a feature-branch workflow.
- Use descriptive commit messages (e.g., `feat: add accommodation module`, `fix: resolving jwt validation error`).

### 2. API Design
- Adhere to RESTful principles.
- Use `ApiResponse<T>` for consistent API responses.
- Document all endpoints using Swagger (OpenAPI).

### 3. Error Handling
- Use global error handling middleware to catch unhandled exceptions.
- Return meaningful error messages and appropriate HTTP status codes.

## 📏 Coding Standards

### 1. C# Conventions
- Follow standard C# naming conventions (PascalCase for classes/methods, camelCase for local variables).
- Use `Nullable` reference types (`#nullable enable`).
- Prefer explicit types over `var` when the type is not obvious.

### 2. Dependency Management
- Keep NuGet packages up to date.
- Avoid adding unnecessary dependencies.

## 🧪 Testing

### 1. Unit Testing
- Use xUnit for unit tests.
- Aim for high coverage of business logic in the `Services` layer.
- Mock dependencies using libraries like `Moq`.

### 2. Integration Testing
- Test API endpoints using `WebApplicationFactory`.
- Use a separate test database for integration tests.

## 📦 Deployment & CI/CD
- **GitHub Actions:** CI/CD pipelines are defined in `.github/workflows/`.
- **Docker:** Use the provided `Dockerfile` and `docker-compose.yml` for containerization.
- **Reverse Proxy:** Use Nginx as a reverse proxy in production environments.
