# NearU Backend

A location-based backend service built with ASP.NET Core (.NET 10) for the NearU platform.

## 📋 Description

NearU Backend provides RESTful API endpoints for location-based services, enabling users to discover nearby places, services, and people.

## 🚀 Tech Stack

- **.NET 10** - Latest .NET framework
- **ASP.NET Core Web API** - RESTful API framework
- **C#** - Primary programming language
- **Entity Framework Core** - (Add if using)
- **SQL Server / PostgreSQL / MongoDB** - (Specify your database)

## 📦 Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2026](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)
- Database server (if applicable)

## 🛠️ Setup Instructions

### 1. Clone the Repository

```bash
git clone <your-repository-url>
cd NearU-Backend
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Configure Settings

1. Copy `appsettings.json` to `appsettings.Development.json`
2. Update connection strings and API keys in `appsettings.Development.json`
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "YOUR_CONNECTION_STRING_HERE"
     },
     "Jwt": {
       "Secret": "YOUR_JWT_SECRET_HERE",
       "Issuer": "NearU",
       "Audience": "NearU-Client"
     }
   }
   ```

### 4. Apply Database Migrations (if using EF Core)

```bash
dotnet ef database update
```

### 5. Run the Application

```bash
cd NearU-Backend.Server
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## 📚 API Documentation

Once the application is running, visit the Swagger UI at `https://localhost:5001/swagger` for interactive API documentation.

### Sample Endpoints

- `GET /api/weatherforecast` - Get weather forecast (sample endpoint)
- `POST /api/auth/login` - User authentication
- `GET /api/places/nearby` - Get nearby places
- ... (Add your actual endpoints)

## 🏗️ Project Structure

```
NearU-Backend/
├── NearU-Backend.Server/          # Main API project
│   ├── Controllers/                # API controllers
│   ├── Models/                     # Data models
│   ├── Services/                   # Business logic
│   ├── Data/                       # Database context & repositories
│   ├── DTOs/                       # Data transfer objects
│   ├── Middleware/                 # Custom middleware
│   ├── Program.cs                  # Application entry point
│   └── appsettings.json           # Configuration
├── nearu-backend.client/          # Frontend client (if applicable)
└── README.md                       # This file
```

## 🤝 Contributing

We welcome contributions! Please follow these steps:

1. **Fork the repository**
2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. **Commit your changes**
   ```bash
   git commit -m "Add: your feature description"
   ```
4. **Push to your branch**
   ```bash
   git push origin feature/your-feature-name
   ```
5. **Open a Pull Request**

### Coding Standards

- Follow C# naming conventions
- Use meaningful variable and method names
- Add XML comments for public APIs
- Write unit tests for new features
- Ensure all tests pass before submitting PR

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## 🧪 Testing

Run tests using:

```bash
dotnet test
```

## 📄 License

[Specify your license here - MIT, Apache 2.0, etc.]

## 👥 Team / Contributors

- [Your Name](https://github.com/yourusername)
- [Add team members]

## 📞 Contact

For questions or support, please contact:
- Email: your-email@example.com
- Issues: [GitHub Issues](https://github.com/yourusername/NearU-Backend/issues)

## 🗺️ Roadmap

- [ ] User authentication & authorization
- [ ] Location-based search
- [ ] Real-time notifications
- [ ] Admin dashboard API
- [ ] Mobile app integration
- [ ] ... (Add your planned features)

---

**Note:** Remember to update configuration files with your actual database connections and secrets before running locally. Never commit sensitive information to the repository.
