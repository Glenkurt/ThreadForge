# ThreadForge – User Stories & Execution Plan
## Task 6: Swagger/OpenAPI Documentation for API

---

## Global Constraints

- No authentication (unless explicitly specified)
- Single user session (no multi-tenant logic)
- English documentation
- Desktop-first UX for Swagger UI
- API must remain fully functional with or without Swagger
- Swagger UI only available in Development and Staging environments
- Production environment does NOT serve Swagger UI

---

## Definitions (Shared Vocabulary)

- **Swagger**: UI tool for visualizing and testing REST APIs
- **OpenAPI**: Specification format for describing REST APIs (formerly Swagger Specification)
- **Swagger UI**: Interactive HTML/JS interface for exploring API endpoints
- **Endpoint**: A specific API route (e.g., POST /api/v1/threads/generate)
- **Schema**: JSON structure defining request/response data models
- **Try It Out**: Swagger UI feature allowing direct API testing from the browser
- **XML Comments**: C# documentation comments that Swagger reads to generate descriptions

---

## User Stories

---

### User Story 6.1 – Swagger UI for API Documentation

#### Intent

Provide developers with an interactive, always-up-to-date API documentation interface for testing endpoints without external tools like Postman.

#### Preconditions

- .NET API is running in Development or Staging mode
- API is accessible at http://localhost:5000 (or configured port)
- No additional setup required beyond starting the API

#### User Flow (Step-by-Step)

1. Developer starts the API with `dotnet run`
2. Developer navigates to `http://localhost:5000/swagger`
3. Browser displays Swagger UI with all API endpoints
4. Developer expands an endpoint (e.g., POST /api/v1/threads/generate)
5. Developer clicks "Try it out"
6. Developer enters request body JSON
7. Developer clicks "Execute"
8. Swagger UI displays full response including status code, headers, body

#### System Behavior

When API starts in Development mode:
- Swagger UI is served at `/swagger` route
- OpenAPI JSON is served at `/swagger/v1/swagger.json`
- Swagger UI auto-discovers all controllers and endpoints
- XML documentation comments are read and displayed
- All DTOs are documented with property descriptions
- Example request/response payloads are auto-generated

When API starts in Production mode:
- `/swagger` route returns 404
- `/swagger/v1/swagger.json` returns 404
- No Swagger middleware is registered

#### Inputs

No user inputs required for accessing Swagger UI.

For testing endpoints via Swagger:
- User provides request body JSON directly in Swagger UI
- Swagger UI validates JSON syntax before sending

#### Outputs

**Swagger UI displays:**
- API title: "ThreadForge API"
- API version: "v1"
- API description: "AI-powered Twitter thread generation for indie hackers"
- List of all endpoints grouped by controller
- For each endpoint:
  - HTTP method (GET, POST, etc.)
  - Route path
  - Description from XML comments
  - Parameters (path, query, header, body)
  - Request body schema with examples
  - Response schemas for each status code (200, 400, 500)
  - "Try it out" button for live testing

**OpenAPI JSON schema available at:**
`http://localhost:5000/swagger/v1/swagger.json`

#### Endpoint Documentation Requirements

**All endpoints MUST include:**

1. Summary (short description)
2. Description (detailed explanation)
3. Request parameters with:
   - Name
   - Type
   - Required/Optional
   - Description
   - Example value
4. Request body schema (for POST/PUT)
5. Response schemas for:
   - 200 (Success)
   - 400 (Bad Request)
   - 500 (Internal Server Error)
6. Example request payload
7. Example response payload

**Specific Endpoints to Document:**

**POST /api/v1/threads/generate**
- Summary: "Generate AI-powered Twitter thread"
- Description: "Creates a Twitter thread based on topic, tone, and other parameters using X.AI Grok"
- Request body: `GenerateThreadRequestDto`
  - topic: string, required, 1-120 chars, "The main subject for the thread"
  - tone: string, optional, enum [indie_hacker, educational, provocative, storytelling, analytical], "Writing style"
  - audience: string, optional, max 100 chars, "Target audience description"
  - tweetCount: integer, required, 5-7, default 5, "Number of tweets to generate"
  - keyPoints: array of strings, optional, max 5 items, "Specific points to include"
  - feedback: string, optional, max 500 chars, "Feedback for regeneration"
- Responses:
  - 200: `GenerateThreadResponseDto` with example
  - 400: `{ "message": "Invalid request", "errors": [...] }`
  - 429: `{ "message": "Rate limit exceeded" }`
  - 500: `{ "message": "Thread generation failed" }`

**GET /api/v1/health**
- Summary: "API health check"
- Description: "Returns API health status and database connectivity"
- No request body
- Responses:
  - 200: `HealthResponseDto` with status "Healthy"
  - 503: `HealthResponseDto` with status "Unhealthy"

#### Error Cases

- User navigates to `/swagger` in Production → 404 Not Found
- User navigates to `/swagger` before API fully started → "Loading..." page
- OpenAPI JSON generation fails → Log error, return empty schema
- XML comments file missing → Swagger works but descriptions are missing

#### Acceptance Criteria

- [ ] Swagger UI accessible at `/swagger` in Development mode
- [ ] Swagger UI returns 404 in Production mode
- [ ] OpenAPI JSON available at `/swagger/v1/swagger.json`
- [ ] All controllers auto-discovered (Health, Threads, Auth)
- [ ] All endpoints display HTTP method and route
- [ ] Request/response schemas auto-generated from DTOs
- [ ] "Try it out" functionality works for all endpoints
- [ ] Example request payloads are syntactically valid JSON
- [ ] Response status codes (200, 400, 500) documented for each endpoint
- [ ] XML comments from C# code appear in Swagger UI
- [ ] DTO properties have descriptions from XML comments
- [ ] Enum values display all allowed options
- [ ] Required vs optional parameters clearly marked
- [ ] API version displays as "v1"
- [ ] API title displays as "ThreadForge API"
- [ ] Swagger UI theme is default (light mode, standard colors)
- [ ] No console errors when loading Swagger UI
- [ ] No breaking changes to existing API functionality

#### Implementation Details

**Required NuGet Packages:**
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />
```

**Modify: api/Api/Program.cs**

Add after `var builder = WebApplication.CreateBuilder(args);`:
```csharp
// Register Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ThreadForge API",
        Version = "v1",
        Description = "AI-powered Twitter thread generation for indie hackers",
        Contact = new OpenApiContact
        {
            Name = "ThreadForge Support",
            Email = "support@threadforge.dev"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
```

Add after `var app = builder.Build();` and before `app.UseHttpsRedirection();`:
```csharp
// Enable Swagger only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ThreadForge API v1");
        options.RoutePrefix = "swagger"; // Access at /swagger
    });
}
```

**Modify: api/Api/Api.csproj**

Add inside `<PropertyGroup>`:
```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);1591</NoWarn>
```

**Add XML Comments to Controllers:**

Example for ThreadsController.cs:
```csharp
/// <summary>
/// Generate an AI-powered Twitter thread based on topic and parameters
/// </summary>
/// <param name="request">Thread generation parameters</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Generated thread with tweets and metadata</returns>
/// <response code="200">Thread generated successfully</response>
/// <response code="400">Invalid request parameters</response>
/// <response code="429">Rate limit exceeded</response>
/// <response code="500">Thread generation failed</response>
[HttpPost("generate")]
[EnableRateLimiting("threadgen")]
[ProducesResponseType(typeof(GenerateThreadResponseDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status429TooManyRequests)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<GenerateThreadResponseDto>> Generate(...)
```

**Add XML Comments to DTOs:**

Example for ThreadDtos.cs:
```csharp
/// <summary>
/// Request parameters for thread generation
/// </summary>
public sealed record GenerateThreadRequestDto(
    /// <summary>
    /// Main subject for the Twitter thread (1-120 characters)
    /// </summary>
    /// <example>How to build in public as an indie hacker</example>
    string Topic,

    /// <summary>
    /// Writing style for the thread
    /// </summary>
    /// <example>indie_hacker</example>
    string? Tone,

    /// <summary>
    /// Target audience description (max 100 characters)
    /// </summary>
    /// <example>Technical founders building SaaS products</example>
    string? Audience,

    /// <summary>
    /// Number of tweets to generate (5-7)
    /// </summary>
    /// <example>5</example>
    int TweetCount,

    /// <summary>
    /// Specific points to include in the thread (max 5 items)
    /// </summary>
    /// <example>["Share revenue numbers", "Be authentic", "Post daily"]</example>
    string[]? KeyPoints,

    /// <summary>
    /// Feedback for regenerating a previous thread (max 500 characters)
    /// </summary>
    /// <example>Make it more actionable and less theoretical</example>
    string? Feedback
);
```

**Create: api/Api/Models/DTOs/ErrorResponseDto.cs**
```csharp
namespace Api.Models.DTOs;

/// <summary>
/// Error response returned for failed requests
/// </summary>
public sealed record ErrorResponseDto(
    /// <summary>
    /// Human-readable error message
    /// </summary>
    string Message,

    /// <summary>
    /// Detailed validation errors (if applicable)
    /// </summary>
    string[]? Errors = null
);
```

#### Testing Checklist

- [ ] Start API with `dotnet run`
- [ ] Navigate to `http://localhost:5000/swagger`
- [ ] Verify Swagger UI loads without errors
- [ ] Expand POST /api/v1/threads/generate
- [ ] Click "Try it out"
- [ ] Enter valid request body:
```json
{
  "topic": "Building in public",
  "tone": "indie_hacker",
  "tweetCount": 5
}
```
- [ ] Add header: `X-Client-Id: test-client`
- [ ] Click "Execute"
- [ ] Verify 200 response with thread data
- [ ] Test GET /api/v1/health endpoint
- [ ] Verify XML comments display in descriptions
- [ ] Test with invalid request (missing topic)
- [ ] Verify 400 response with error details
- [ ] Check OpenAPI JSON at `/swagger/v1/swagger.json`
- [ ] Verify JSON is valid and contains all endpoints

---

## Non-Goals (Explicitly Out of Scope)

- Custom Swagger UI theme/branding
- Authentication for Swagger UI
- Multiple API versions (only v1)
- Swagger for frontend (frontend uses Angular)
- GraphQL documentation (API is REST only)
- Postman collection export
- Code generation from OpenAPI schema
- Swagger UI in Production environment
- Advanced OpenAPI features (callbacks, links, etc.)
- API versioning beyond v1

---

## Execution Notes for Dev Agent

- Install `Swashbuckle.AspNetCore` version 7.2.0 or newer
- Enable XML documentation file generation in .csproj
- Suppress warning 1591 (missing XML comments) to avoid build noise
- Add XML comments to ALL public controllers and DTOs
- Use `/// <summary>`, `/// <param>`, `/// <returns>`, `/// <response>` tags
- Use `/// <example>` tags for realistic sample values
- Ensure all ProducesResponseType attributes are present
- Test Swagger UI manually before marking complete
- Do NOT modify existing API logic or routing
- Do NOT add authentication to Swagger unless explicitly requested
- Environment check must use `app.Environment.IsDevelopment()`
- Route prefix is "swagger" (not "api-docs" or other alternatives)
