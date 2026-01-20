# Full-Stack Template

A production-ready full-stack template with:

- **Backend**: .NET 10 Web API with PostgreSQL, JWT Auth, Serilog
- **Frontend**: Angular 19 with standalone components, signals, interceptors
- **Database**: PostgreSQL 15
- **Containerization**: Docker & Docker Compose with health checks
- **CI/CD**: GitHub Actions pipeline

## 📁 Project Structure

```
templateFullStack/
├── api/
│   └── Api/
│       ├── Controllers/      # API endpoints
│       ├── Services/         # Business logic layer
│       ├── Models/DTOs/      # Data transfer objects
│       ├── Middleware/       # Exception handling, etc.
│       ├── Extensions/       # DI registration helpers
│       └── Data/             # EF Core DbContext
├── frontend/
│   └── src/app/
│       ├── core/             # Singletons (auth, interceptors, guards)
│       ├── shared/           # Reusable components
│       ├── features/         # Lazy-loaded feature modules
│       └── services/         # API services
├── .github/workflows/        # CI/CD pipeline
├── docker-compose.yml        # Container orchestration
├── Dockerfile                # Multi-stage build
└── .env.example              # Environment variables template
```

## 🚀 Quick Start

### Option 1: Docker (Production-like)

```bash
# Copy environment template and configure
cp .env.example .env
# Edit .env with your values

# Start all services
docker-compose up --build

# Access the app at http://localhost:8080
```

### Option 2: Local Development

**Prerequisites:**

- .NET 10 SDK
- Node.js 20+
- PostgreSQL 15 (or use Docker for DB only)

**Start PostgreSQL:**

```bash
docker-compose up db -d
```

**Start the API:**

```bash
cd api/Api
dotnet run
# API runs on http://localhost:5093
```

**Start Angular (separate terminal):**

```bash
cd frontend
npm install
npm start
# Frontend runs on http://localhost:4200
```

## 🔗 API Endpoints

| Endpoint                    | Auth | Description                             |
| --------------------------- | ---- | --------------------------------------- |
| `GET /api/v1/health`        | No   | Health check with database connectivity |
| `POST /api/v1/auth/login`   | No   | Authenticate and get JWT tokens         |
| `POST /api/v1/auth/refresh` | No   | Refresh expired access token            |
| `GET /api/v1/auth/me`       | Yes  | Get current user info                   |

## ⚙️ Configuration

### Environment Variables

Copy `.env.example` to `.env` and configure:

```bash
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password
POSTGRES_DB=templatedb
JWT_SECRET=your_jwt_secret_minimum_32_chars
```

### API (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=templatedb;..."
  },
  "Jwt": {
    "Secret": "${JWT_SECRET}",
    "Issuer": "https://localhost",
    "Audience": "https://localhost",
    "ExpiryMinutes": 60
  }
}
```

### Angular Proxy (proxy.conf.json)

```json
{
  "/api": {
    "target": "http://localhost:5093",
    "secure": false,
    "changeOrigin": true
  }
}
```

## 🐳 Docker

**Build and run:**

```bash
docker-compose up --build
```

**Stop services:**

```bash
docker-compose down
```

**Remove volumes (reset database):**

```bash
docker-compose down -v
```

## 📝 Adding Features

### Add a new API endpoint

1. Create a controller in `api/Api/Controllers/`
2. Add your DbContext entities in `api/Api/Data/AppDbContext.cs`
3. Create and apply migrations

### Add an Angular component

1. Generate with: `ng generate component features/your-component`
2. Add routes in `frontend/src/app/app.routes.ts`

## 🧪 Tech Stack

| Layer     | Technology | Version |
| --------- | ---------- | ------- |
| Backend   | .NET       | 10.0    |
| Frontend  | Angular    | 19.x    |
| Database  | PostgreSQL | 15      |
| Container | Docker     | 20+     |

## 📄 License

MIT
