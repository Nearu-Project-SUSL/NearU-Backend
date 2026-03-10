# Database Migration Instructions

## Prerequisites

Ensure you have the following installed:
- .NET 10 SDK
- Entity Framework Core CLI tools

Install EF Core tools globally:
```bash
dotnet tool install --global dotnet-ef
```

Or update if already installed:
```bash
dotnet tool update --global dotnet-ef
```

## Step 1: Update Connection String

Update `appsettings.json` with your Azure SQL connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=NearU;Persist Security Info=False;User ID=YOUR_USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

For local development, you can use SQL Server Express:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NearUDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

## Step 2: Create Initial Migration

Navigate to the solution directory and run:

```bash
cd NearU-Backend.Server
dotnet ef migrations add InitialCreate
```

This will create a `Migrations` folder with the migration files.

## Step 3: Apply Migration to Database

```bash
dotnet ef database update
```

This will:
- Create the database if it doesn't exist
- Create all tables (Users, Roles, RefreshTokens)
- Seed initial data (4 roles: Admin, Student, Business, Rider)

## Step 4: Verify Database

Check that these tables were created:
- Users
- Roles
- RefreshTokens
- __EFMigrationsHistory (tracks migrations)

## Seeded Data

The migration automatically seeds 4 roles:

1. **Admin** - System Administrator
2. **Student** - Student User
3. **Business** - Business User
4. **Rider** - Delivery Rider

## Creating an Admin User

Admin users cannot be created via the API for security. Create one manually:

### Option 1: Via SQL

```sql
-- First, get the Admin role ID
SELECT Id FROM Roles WHERE Name = 'Admin';

-- Create admin user (replace RoleId with the actual Admin role ID from above)
INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, RoleId, IsActive, EmailConfirmed, CreatedAt)
VALUES (
    'admin',
    'admin@nearu.com',
    '$2a$11$YourBCryptHashHere', -- Generate using BCrypt
    'System',
    'Administrator',
    1, -- Admin RoleId
    1, -- IsActive
    1, -- EmailConfirmed
    GETUTCDATE()
);
```

### Option 2: Generate BCrypt Hash

Use this C# code to generate a BCrypt hash:

```csharp
using BCrypt.Net;

string password = "YourAdminPassword123!";
string hash = BCrypt.HashPassword(password);
Console.WriteLine(hash);
```

Or use an online BCrypt generator (for development only): https://bcrypt-generator.com/

## Common Migration Commands

### List migrations
```bash
dotnet ef migrations list
```

### Remove last migration (if not applied to database)
```bash
dotnet ef migrations remove
```

### Generate SQL script instead of applying directly
```bash
dotnet ef migrations script --output migration.sql
```

### Apply migration to specific database
```bash
dotnet ef database update --connection "YourConnectionString"
```

### Rollback to previous migration
```bash
dotnet ef database update PreviousMigrationName
```

### Drop database (CAUTION!)
```bash
dotnet ef database drop
```

## Troubleshooting

### Error: "A network-related or instance-specific error occurred"

- Check your connection string
- Ensure SQL Server is running
- Check firewall rules (Azure SQL requires IP whitelist)

### Error: "Unable to resolve service for type 'NearUDbContext'"

- Ensure `NearUDbContext` is registered in `Program.cs`
- Check that the connection string is properly configured

### Error: "The certificate chain was issued by an authority that is not trusted"

For local development, modify connection string:
```
...;TrustServerCertificate=True;...
```

### Error: "Login failed for user"

- Verify username and password in connection string
- Check user permissions on the database

## Production Deployment

### Azure SQL Database

1. Create Azure SQL Database:
```bash
az sql server create --name nearuserver --resource-group NearU --location eastus --admin-user youradmin --admin-password YourPassword123!
az sql db create --name NearUDb --server nearuserver --resource-group NearU --service-objective S0
```

2. Configure firewall:
```bash
az sql server firewall-rule create --resource-group NearU --server nearuserver --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```

3. Get connection string:
```bash
az sql db show-connection-string --name NearUDb --server nearuserver --client ado.net
```

4. Apply migrations to production:
```bash
dotnet ef database update --connection "ProductionConnectionString"
```

### Automated Migrations in CI/CD

Add this to your deployment pipeline (Azure DevOps, GitHub Actions):

```yaml
- name: Apply Database Migrations
  run: |
    dotnet tool restore
    dotnet ef database update --project NearU-Backend.Server --connection "${{ secrets.DB_CONNECTION_STRING }}"
```

## Maintenance

### Clean up expired refresh tokens

Run this SQL query periodically (or create a scheduled job):

```sql
DELETE FROM RefreshTokens WHERE ExpiresAt < GETUTCDATE();
```

Or use the repository method:
```csharp
await refreshTokenRepository.DeleteExpiredTokensAsync();
```

### Backup Database

Azure SQL automatic backups are enabled by default. To create a manual backup:

```bash
az sql db export --name NearUDb --server nearuserver --resource-group NearU --admin-user youradmin --admin-password YourPassword123! --storage-key-type SharedAccessKey --storage-key "YourStorageKey" --storage-uri "https://yourstorage.blob.core.windows.net/backups/nearu.bacpac"
```

## Next Steps

After successful migration:

1. ✅ Create an admin user
2. ✅ Test registration endpoint
3. ✅ Test login endpoint
4. ✅ Verify JWT token generation
5. ✅ Test role-based endpoints
6. ✅ Configure CORS for your frontend
7. ✅ Deploy to Azure

## Support

If you encounter issues:
1. Check the migration error logs
2. Verify connection string
3. Review Entity Framework documentation: https://docs.microsoft.com/en-us/ef/core/
4. Contact your database administrator
