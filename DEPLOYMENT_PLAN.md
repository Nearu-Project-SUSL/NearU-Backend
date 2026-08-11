# NearU Backend Deployment and Security Upgrade Plan

This document outlines the step-by-step plan to deploy the NearU Backend to the DigitalOcean Droplet using GitHub Actions, configure it to work behind Nginx, and upgrade the authentication system to use highly secure `HttpOnly` cookies.

## Phase 1: Quality Gate & CI/CD Setup

**Goal:** Establish an automated pipeline that validates code quality and builds a highly efficient Docker image for deployment.

1.  **Automated Testing (The Quality Gate):**
    *   Create a dedicated xUnit test project (`NearU_Backend.Tests`) to house unit and integration tests.
    *   **Workflow Integration:** Update the GitHub Actions workflow to include a `test` job that executes `dotnet test`.
    *   **Strict Dependency:** Configure the `build-and-push` job to depend on the `test` job (`needs: test`). If any test fails, the deployment is automatically aborted.

2.  **Update `Dockerfile` (Green Coding Initiative):**
    *   Modify the existing `Dockerfile` to use the `.NET 10-noble-chiseled` images.
    *   Ensure the correct project file (`NearU_Backend_Revised.csproj`) is referenced.
    *   This reduces the attack surface and significantly lowers the container size.

3.  **GitHub Actions Workflow (`.github/workflows/deploy.yml`):**
    *   Set up the workflow to trigger on pushes to the `dev` branch.
    *   **Test Job:** Restore, build, and run tests.
    *   **Build Job:** (Depends on Test) Authenticate with GHCR, build the Docker image, and push it tagged as `latest`.
    *   **Deploy Job:** (Depends on Build) SSH into the Droplet (`152.42.227.133`) to pull and restart the container.

4.  **Configure GitHub Secrets:**
    *   Ensure `DROPLET_IP`, `SSH_PRIVATE_KEY`, and `GH_PAT` are configured in repository settings.

## Phase 2: Application Configuration for Nginx

**Goal:** Ensure ASP.NET Core understands it is running behind an Nginx reverse proxy with SSL termination.

1.  **Add Forwarded Headers Middleware (`Program.cs`):**
    *   Implement `app.UseForwardedHeaders(...)` to trust `X-Forwarded-For` and `X-Forwarded-Proto` from Nginx.
    *   This is critical for generating correct secure cookies and handling HTTPS context correctly.

## Phase 3: Security Upgrade - HttpOnly Cookies

**Goal:** Migrate from `localStorage` to `HttpOnly` cookies to prevent XSS attacks.

1.  **Update Authentication Endpoints (`Controllers/AuthController.cs`):**
    *   Modify `Login` and `RefreshToken` to append tokens to response cookies via `Response.Cookies.Append`.
    *   Settings: `HttpOnly = true`, `Secure = true`, and `SameSite = SameSiteMode.None` (for cross-domain) or `Lax`.
    *   Modify `Logout` to clear these cookies.

2.  **Update JWT Configuration (`Program.cs`):**
    *   Configure `AddJwtBearer` to extract tokens from cookies in the `OnMessageReceived` event.

## Phase 4: Execution Strategy

1.  **Commit and Push:** Push all architectural and testing changes to the `dev` branch.
2.  **Monitor Actions:** Verify the `test` job passes before the `build` and `deploy` jobs execute.
3.  **Verify Deployment:** Confirm `https://api.nearusab.me` is live and tokens are being set as `HttpOnly` cookies in the browser.
4.  **Frontend Update:** Set `withCredentials: true` in Axios and remove local storage logic.
