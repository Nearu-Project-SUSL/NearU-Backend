# NearU Backend Deployment and Security Upgrade Plan

This document outlines the step-by-step plan to deploy the NearU Backend to the DigitalOcean Droplet using GitHub Actions, configure it to work behind Nginx, and upgrade the authentication system to use highly secure `HttpOnly` cookies.

## Phase 1: Infrastructure and CI/CD Setup

**Goal:** Establish an automated pipeline that builds a highly efficient Docker image and deploys it to the Droplet upon pushing to the `dev` branch.

1.  **Update `Dockerfile` (Green Coding Initiative):**
    *   Modify the existing `Dockerfile` to use the `.NET 10-noble-chiseled` images.
    *   Ensure the correct project file (`NearU_Backend_Revised.csproj`) is referenced.
    *   This reduces the attack surface and significantly lowers the container size.

2.  **Create GitHub Actions Workflow (`.github/workflows/deploy.yml`):**
    *   Set up the workflow to trigger on pushes to the `dev` branch.
    *   **Build Job:** Authenticate with GitHub Container Registry (GHCR), build the Docker image, and push it tagged as `latest`.
    *   **Deploy Job:** Use SSH to connect to the Droplet (`152.42.227.133`), pull the new image via `docker compose pull`, and restart the service using `docker compose up -d`.

3.  **Configure GitHub Secrets:**
    *   Ensure the following repository secrets are set before pushing:
        *   `DROPLET_IP`: `152.42.227.133`
        *   `SSH_PRIVATE_KEY`: The private SSH key for Droplet access.
        *   `GH_PAT`: Personal Access Token with `write:packages` permission.

## Phase 2: Application Configuration for Nginx

**Goal:** Ensure ASP.NET Core understands it is running behind an Nginx reverse proxy with SSL termination.

1.  **Add Forwarded Headers Middleware (`Program.cs`):**
    *   Since Nginx handles HTTPS and forwards traffic via HTTP to the container, ASP.NET must trust Nginx's headers to know the original request scheme.
    *   Implement `app.UseForwardedHeaders(...)` configuring `X-Forwarded-For` and `X-Forwarded-Proto`.
    *   This is crucial for generating correct redirect URIs and secure cookies.

## Phase 3: Security Upgrade - HttpOnly Cookies

**Goal:** Migrate from storing JWTs in `localStorage` to using highly secure `HttpOnly` cookies to prevent XSS attacks.

1.  **Update Authentication Endpoints (`Controllers/AuthController.cs`):**
    *   Modify `Login` and `RefreshToken` endpoints. Instead of returning the `AccessToken` and `RefreshToken` in the JSON response body, append them to the response cookies using `Response.Cookies.Append`.
    *   Configure cookie options: `HttpOnly = true`, `Secure = true` (required since we are on `https://api.nearusab.me`), and `SameSite = SameSiteMode.None` (if frontend is on Vercel) or `SameSiteMode.Lax` (if frontend shares the `.nearusab.me` domain).
    *   Modify the `Logout` endpoint to clear these cookies by setting their expiration date to the past.

2.  **Update JWT Configuration (`Program.cs`):**
    *   Modify the `AddJwtBearer` configuration.
    *   Add logic within the `OnMessageReceived` event to extract the JWT from the incoming request's cookies instead of expecting it in the `Authorization` header.

## Phase 4: Execution Strategy

Once the code changes in Phases 1-3 are ready:

1.  **Commit and Push:** Push all changes to the `dev` branch.
    ```bash
    git add .
    git commit -m "feat: setup CI/CD, Nginx headers, and HttpOnly cookies"
    git push origin dev
    ```
2.  **Monitor Actions:** Watch the GitHub Actions tab to ensure the build and deploy jobs succeed.
3.  **Verify Deployment:** The **502 Bad Gateway** at `https://api.nearusab.me` should resolve to a successful response.
4.  **Frontend Update:** Coordinate with the frontend team to update Axios configuration (`withCredentials: true`) and remove `localStorage` token management.
