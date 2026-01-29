# CLAUDE.md - ThreadForge AI Assistant Guide

This document provides comprehensive context for AI assistants working with the ThreadForge codebase.

## Project Overview

**ThreadForge** is a full-stack AI-powered Twitter/X thread generation platform for indie hackers and #BuildInPublic creators. It helps users overcome "blank page" paralysis by turning short descriptions into ready-to-post thread drafts.

**Primary Goal:** Help users ship more content, grow their personal brand, and get feedback on their projects.

## Tech Stack

| Layer | Technology | Version |
|-------|------------|---------|
| Backend | ASP.NET Core | 10.0 |
| Frontend | Angular | 19.x |
| Database | PostgreSQL | 15 |
| AI Provider | xAI Grok API | grok-3-mini (default) |
| Container | Docker + Docker Compose | 20+ |

## Project Structure

```
ThreadForge/
├── api/                              # .NET Backend
│   ├── Api/                          # Main Web API project
│   │   ├── Controllers/              # API endpoints (5 controllers)
│   │   ├── Services/                 # Business logic layer
│   │   ├── Models/                   # Entities, DTOs, Options
│   │   │   ├── DTOs/                 # Request/Response objects
│   │   │   ├── Entities/             # EF Core entities
│   │   │   └── Options/              # Configuration classes
│   │   ├── Data/                     # EF Core DbContext
│   │   ├── Migrations/               # Database schema versions
│   │   ├── Middleware/               # Exception handling
│   │   ├── Extensions/               # DI registration helpers
│   │   └── Program.cs                # Application entry point
│   └── Api.Tests/                    # Integration tests (xUnit)
├── frontend/                         # Angular Frontend
│   └── src/app/
│       ├── core/                     # Singletons (auth, interceptors, guards)
│       ├── features/                 # Lazy-loaded feature modules
│       │   ├── generator/            # Main thread generation UI
│       │   ├── history/              # Thread history views
│       │   └── profile-analysis/     # Twitter profile analyzer
│       ├── services/                 # API service layer
│       ├── components/               # Reusable UI components
│       ├── models/                   # TypeScript interfaces
│       └── shared/                   # Shared utilities
├── scripts/                          # Database migration scripts
├── .github/
│   ├── Docs/                         # PRD, design specs, roadmap
│   ├── tasks/                        # Task breakdown documents
│   └── agents/                       # Agent configuration files
├── docker-compose.yml                # Multi-container orchestration
├── Dockerfile                        # Multi-stage build
└── README.md                         # Project documentation
```

## Development Workflow

### Quick Start Options

**Option 1: Docker (Recommended for full stack)**
```bash
# Copy and configure environment
cp .env.example .env
# Edit .env with your values (see Environment Variables section)

# Start all services
docker-compose up --build

# Access at http://localhost:8080
```

**Option 2: Local Development**
```bash
# Start PostgreSQL only
docker-compose up db -d

# Start API (terminal 1)
cd api/Api && dotnet run
# Runs on http://localhost:5093

# Start Frontend (terminal 2)
cd frontend && npm install && npm start
# Runs on http://localhost:4200 (proxies /api to backend)
```

### Database Migrations

```bash
# Create new migration
./scripts/db-add-migration.sh <MigrationName>

# Apply pending migrations
./scripts/db-update.sh
```

### Frontend Commands

```bash
cd frontend

npm start           # Dev server on :4200
npm run build       # Production build
npm test            # Run Karma tests
npm run lint        # ESLint check
npm run lint:fix    # Auto-fix linting issues
npm run format      # Prettier format
npm run format:check # Check formatting
```

### Running Tests

**Backend (Integration tests)**
```bash
cd api/Api.Tests
dotnet test
```

**Frontend (Unit tests)**
```bash
cd frontend
npm test
```

## API Endpoints

Base URL: `/api/v1/`

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/health` | GET | No | Health check with DB status |
| `/auth/login` | POST | No | User authentication |
| `/auth/refresh` | POST | No | Refresh JWT token |
| `/auth/me` | GET | Yes | Get current user info |
| `/threads/generate` | POST | No | Generate AI thread |
| `/threads/history` | GET | No | List thread history |
| `/threads/history/{id}` | GET | No | Get thread details |
| `/profiles/analyze` | POST | No | Analyze Twitter profile |
| `/brand-guidelines` | GET/PUT | No | Manage brand guidelines |

### Rate Limits
- General endpoints: 100 requests/minute
- Auth endpoints: 10 requests/minute
- Thread generation: 20 requests/day per client

## Environment Variables

Required variables for production:

```bash
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<secure_password>     # Required
POSTGRES_DB=templatedb
JWT_SECRET=<min_32_char_secret>         # Required
XAI_API_KEY=<your_xai_api_key>          # Required
XAI_MODEL=grok-3-mini                   # Options: grok-3, grok-3-mini, grok-4
```

## Code Conventions

### Backend (C#/.NET)

**Naming**
- Classes: PascalCase (`ThreadGenerationService`)
- Methods: PascalCase (`GenerateAsync`)
- Private fields: _camelCase (`_dbContext`)
- DTOs: Suffix with `Dto` (`GenerateThreadRequestDto`)

**Architecture Patterns**
- Controllers → Services → DbContext (layered architecture)
- Each service has interface + implementation
- Extension methods for clean DI registration
- RFC 7807 ProblemDetails for error responses

**Example Service Pattern**
```csharp
public interface IMyService
{
    Task<Result> DoSomethingAsync(Request request);
}

public class MyService : IMyService
{
    private readonly AppDbContext _dbContext;

    public MyService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> DoSomethingAsync(Request request)
    {
        // Implementation
    }
}
```

**DTO Pattern**
```csharp
/// <summary>
/// Request to generate a thread
/// </summary>
public sealed record GenerateThreadRequestDto(
    string Topic,
    string? Tone,
    int? ThreadLength
);
```

### Frontend (Angular/TypeScript)

**Naming**
- Components: PascalCase (`GeneratorComponent`)
- Files: kebab-case (`thread-preview.component.ts`)
- Variables: camelCase (`isGenerating`)
- Constants: UPPER_SNAKE_CASE (`CLIENT_ID_KEY`)

**Component Pattern (Standalone)**
```typescript
@Component({
  selector: 'app-my-component',
  standalone: true,
  imports: [CommonModule, MatButtonModule],
  templateUrl: './my-component.component.html'
})
export class MyComponent {
  private service = inject(MyService);

  // Use Signals for reactive state
  readonly isLoading = signal(false);
  readonly data = signal<Data | null>(null);

  // Computed properties
  readonly canSubmit = computed(() =>
    !this.isLoading() && this.data() !== null
  );
}
```

**Service Pattern**
```typescript
@Injectable({ providedIn: 'root' })
export class MyService {
  private http = inject(HttpClient);

  getData(): Observable<Data> {
    return this.http.get<Data>('/api/v1/data');
  }
}
```

**State Management**
- Use Angular Signals for component state (not RxJS BehaviorSubject)
- Services expose Observables for async operations
- Computed signals for derived state

### File Organization

**Backend**
- Group by feature within each folder (Controllers, Services, Models)
- Keep DTOs separate from Entities
- One controller per resource

**Frontend**
- `core/` - Singletons loaded once (auth, interceptors, guards)
- `features/` - Lazy-loaded feature modules (one folder per feature)
- `shared/` - Reusable utilities and pipes
- `components/` - Reusable UI components
- `services/` - API communication layer
- `models/` - TypeScript interfaces

## Key Implementation Details

### Thread Generation Flow

1. User fills form (topic, tone, audience, length, brand guidelines)
2. Frontend calls `POST /api/v1/threads/generate`
3. `ThreadGenerationService` builds AI prompt with user inputs
4. `XaiChatClient` sends request to Grok API
5. Service validates tweets (280 char limit) and persists to DB
6. Response returns generated tweets array

### Authentication Flow

1. User logs in via `POST /api/v1/auth/login`
2. Server returns JWT access token + HttpOnly refresh cookie
3. `authInterceptor` attaches Bearer token to requests
4. On 401, attempt token refresh via `/auth/refresh`
5. Refresh token stored in HttpOnly cookie (secure)

### Anonymous Rate Limiting

- MVP uses `X-Client-Id` header for anonymous tracking
- `clientIdInterceptor` adds UUID to all requests
- Rate limits tracked per client ID (20 generations/day)

## Testing Guidelines

### Backend Integration Tests

- Use `WebApplicationFactory<Program>` for realistic test host
- `CustomWebApplicationFactory` swaps PostgreSQL for in-memory DB
- Test happy paths and error scenarios
- Example: `ThreadGenerationEndpointTests.cs`

### Frontend Testing

- Karma + Jasmine for unit tests
- Test components in isolation
- Mock services with `jasmine.createSpyObj`

## Common Tasks

### Adding a New API Endpoint

1. Create/update controller in `api/Api/Controllers/`
2. Add service interface in `Services/I{Name}Service.cs`
3. Implement service in `Services/{Name}Service.cs`
4. Register in `Extensions/ServiceCollectionExtensions.cs`
5. Add DTOs in `Models/DTOs/`
6. Create migration if DB changes needed

### Adding a New Angular Feature

1. Create feature folder: `frontend/src/app/features/{name}/`
2. Generate component: `ng g c features/{name}`
3. Add route in `app.routes.ts` with lazy loading
4. Create service if API calls needed
5. Add models/interfaces as needed

### Adding Database Entity

1. Create entity in `Models/Entities/`
2. Add DbSet to `Data/AppDbContext.cs`
3. Configure relationships in `OnModelCreating`
4. Create migration: `./scripts/db-add-migration.sh AddMyEntity`
5. Apply: `./scripts/db-update.sh`

## Important Files to Know

| File | Purpose |
|------|---------|
| `api/Api/Program.cs` | App startup, DI, middleware config |
| `api/Api/Services/ThreadGenerationService.cs` | Core AI thread logic |
| `api/Api/Services/XaiChatClient.cs` | Grok API integration |
| `frontend/src/app/app.routes.ts` | Frontend routing |
| `frontend/src/app/app.config.ts` | Angular DI providers |
| `frontend/src/app/features/generator/` | Main generation UI |
| `docker-compose.yml` | Container orchestration |
| `.github/Docs/prd.md` | Product requirements |

## AI Integration Notes

### Grok API Usage

- OpenAI-compatible API at `https://api.x.ai/v1/chat/completions`
- Models: `grok-3-mini` (fast/cheap), `grok-3` (quality), `grok-4` (advanced)
- Response format: Standard OpenAI chat completion
- Prompts optimized for Twitter thread format (hooks, CTAs, <280 chars)

### Prompt Engineering

Thread prompts include:
- User topic, tone, and target audience
- Brand guidelines (optional)
- Example threads for style matching
- Twitter-specific constraints (character limits, hashtags, emojis)

## Security Considerations

- JWT tokens expire in 60 minutes (configurable)
- Refresh tokens stored in HttpOnly cookies
- Rate limiting prevents abuse
- No stack traces in production error responses
- CORS configured for specific origins
- Input validation on all endpoints

## Troubleshooting

**API not starting:** Check PostgreSQL is running and connection string is correct

**Frontend proxy errors:** Ensure API is running on port 5093

**Rate limit errors (429):** Wait for limit reset or use different client ID

**AI generation fails:** Verify `XAI_API_KEY` is set and valid

**Database migration errors:** Ensure DB is running, check connection string
