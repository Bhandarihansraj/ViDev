# netcn Phase 2 — Technical Architecture Document (TAD)
## Version: 2.0 | Date: July 29, 2026 | Author: Bhandarihansraj

---

## 1. System Overview

### 1.1 Architecture Philosophy
netcn follows a **contract-first, security-by-design** architecture. The visual wiring board is the single source of truth. All code, documentation, and security policies derive from the contract layer.

### 1.2 High-Level Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT LAYER                                    │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐│
│  │  Design Canvas  │  │  Wiring Board   │  │  Contract Dashboard         ││
│  │  (React-Flow)   │  │  (SVG Canvas)   │  │  (Data Table + Preview)     ││
│  └────────┬────────┘  └────────┬────────┘  └─────────────┬───────────────┘│
│           │                    │                         │                │
│           └────────────────────┼─────────────────────────┘                │
│                                ▼                                          │
│                    ┌─────────────────────┐                               │
│                    │   Canvas State Mgr   │                               │
│                    │   (Zustand/Redux)    │                               │
│                    └──────────┬──────────┘                               │
│                               │                                          │
│                    ┌──────────▼──────────┐                            │
│                    │   AST Serializer      │                            │
│                    │   (JSON Schema)        │                            │
│                    └──────────┬──────────┘                            │
└───────────────────────────────┼────────────────────────────────────────────┘
                                │
                                ▼ HTTPS/JSON
┌─────────────────────────────────────────────────────────────────────────────┐
│                             API GATEWAY                                      │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │  Rate Limiting | Auth (JWT) | Request Validation | CORS | Logging       ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└───────────────────────────────┬────────────────────────────────────────────┘
                                │
        ┌───────────────────────┼───────────────────────┐
        ▼                       ▼                       ▼
┌───────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Template     │    │  Code Generation │    │  User/Auth      │
│  Service      │    │  Service         │    │  Service        │
│  (CRUD +      │    │  (Roslyn Engine) │    │  (Identity)     │
│   Search)     │    │                  │    │                 │
└───────┬───────┘    └────────┬────────┘    └─────────────────┘
        │                     │
        ▼                     ▼
┌───────────────┐    ┌─────────────────┐
│  PostgreSQL   │    │  File Storage   │
│  (Templates,  │    │  (S3/R2)        │
│   AST, Users) │    │  (Generated ZIPs)│
└───────────────┘    └─────────────────┘
```

---

## 2. Component Architecture

### 2.1 Frontend (Browser)

#### Canvas Engine (React-Flow)
```typescript
interface CanvasNode {
  id: string;
  type: 'ui' | 'api' | 'db' | 'service' | 'logic';
  position: { x: number; y: number };
  data: {
    label: string;
    componentType: string;      // e.g., "LoginForm", "AuthController"
    properties: Record<string, any>;
    plugs: Plug[];
    sockets: Socket[];
    annotations: Annotation[];
  };
}

interface CanvasEdge {
  id: string;
  source: string;              // node id
  sourcePlug: string;           // plug id
  target: string;              // node id
  targetSocket: string;         // socket id
  validationBadges: Badge[];   // security badges on this wire
}
```

#### Wiring Board Engine
```typescript
interface Plug {
  id: string;
  name: string;                // e.g., "user_id"
  type: string;                // e.g., "string", "int", "DateTime"
  isArray: boolean;
  nullable: boolean;
  constraints: Constraint[];   // e.g., maxLength, regex
}

interface Socket {
  id: string;
  name: string;                // e.g., "UserId"
  type: string;
  isArray: boolean;
  nullable: boolean;
  required: boolean;
}

interface Wire {
  id: string;
  from: { layer: string; component: string; field: string };
  to: { layer: string; component: string; field: string };
  transform: TransformType;    // e.g., "ParseInt", "ToLower", "None"
  validationBadges: string[];  // e.g., ["SQLGuard", "XSSShield"]
  isActive: boolean;
}
```

#### State Management (Zustand)
```typescript
interface CanvasStore {
  nodes: CanvasNode[];
  edges: CanvasEdge[];
  wires: Wire[];

  // Actions
  addNode: (node: CanvasNode) => void;
  connectPlugs: (plugId: string, socketId: string) => ValidationResult;
  addValidationBadge: (wireId: string, badge: string) => void;
  generateAST: () => ProjectAST;

  // Computed
  validationErrors: ValidationError[];
  unmappedInputs: Plug[];
}
```

### 2.2 Backend (ASP.NET Core 8)

#### Service Layer
```
┌─────────────────────────────────────────────────────────┐
│                      API Controllers                       │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐ │
│  │ Template    │ │ Project     │ │ Validation          │ │
│  │ Controller  │ │ Controller  │ │ Controller          │ │
│  │             │ │             │ │                     │ │
│  │ POST /api/  │ │ POST /api/  │ │ POST /api/          │ │
│  │ templates   │ │ projects/   │ │ validate            │ │
│  │ GET /api/   │ │ generate    │ │ GET /api/           │ │
│  │ templates   │ │ GET /api/   │ │ badges              │ │
│  │ /{id}       │ │ projects/   │ │                     │ │
│  │             │ │ {id}/zip    │ │                     │ │
│  └──────┬──────┘ └──────┬──────┘ └──────────┬──────────┘ │
│         │               │                   │            │
│         └───────────────┼───────────────────┘            │
│                         ▼                                │
│  ┌─────────────────────────────────────────────────────┐ │
│  │                   Application Services                 │ │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │ │
│  │  │ Template │ │ Project  │ │ Roslyn   │ │ Security │  │ │
│  │  │ Service  │ │ Service  │ │ Code Gen │ │ Audit    │  │ │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘  │ │
│  └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

#### Roslyn Code Generation Service
```csharp
public interface ICodeGenerationService
{
    // Generate syntax tree from AST
    SyntaxTree GenerateController(ControllerNode node);
    SyntaxTree GenerateService(ServiceNode node);
    SyntaxTree GenerateEntity(EntityNode node);
    SyntaxTree GenerateMapper(Wire[] wires);
    SyntaxTree GenerateProgramFile(ProjectAST ast);

    // Full compilation
    CompilationResult Compile(ProjectAST ast);

    // Output
    byte[] GenerateZip(ProjectAST ast);
}

public class RoslynCodeGenerationService : ICodeGenerationService
{
    public CompilationResult Compile(ProjectAST ast)
    {
        var trees = ast.Nodes.Select(n => GenerateSyntaxTree(n));

        var compilation = CSharpCompilation.Create(ast.ProjectName)
            .AddSyntaxTrees(trees)
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ControllerBase).Assembly.Location)
            )
            .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        return new CompilationResult
        {
            Success = result.Success,
            Diagnostics = result.Diagnostics,
            AssemblyBytes = ms.ToArray()
        };
    }
}
```

### 2.3 Database Schema (PostgreSQL)

```sql
-- Templates
CREATE TABLE templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    slug VARCHAR(255) UNIQUE NOT NULL,          -- e.g., "bhandarihansraj/clean-auth"
    name VARCHAR(255) NOT NULL,
    description TEXT,
    author_id UUID REFERENCES users(id),
    ast_json JSONB NOT NULL,                      -- The full AST
    version VARCHAR(20) DEFAULT '1.0.0',
    is_public BOOLEAN DEFAULT true,
    price_cents INTEGER DEFAULT 0,                -- 0 = free
    download_count INTEGER DEFAULT 0,
    rating_avg DECIMAL(2,1) DEFAULT 5.0,
    tags TEXT[],
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Template Versions (for forking)
CREATE TABLE template_versions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_id UUID REFERENCES templates(id),
    version VARCHAR(20) NOT NULL,
    ast_json JSONB NOT NULL,
    change_notes TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Users
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    username VARCHAR(100) UNIQUE NOT NULL,
    display_name VARCHAR(255),
    avatar_url TEXT,
    is_verified BOOLEAN DEFAULT false,
    subscription_tier VARCHAR(20) DEFAULT 'free',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Validation Badges (Security Modules)
CREATE TABLE validation_badges (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) UNIQUE NOT NULL,            -- e.g., "SQLGuard"
    display_name VARCHAR(255),                   -- e.g., "SQL Injection Guard"
    description TEXT,
    category VARCHAR(50),                        -- "security", "format", "business"
    generated_code_template TEXT,                 -- C# template with {{placeholders}}
    nuget_dependencies TEXT[],
    is_premium BOOLEAN DEFAULT false,
    created_by UUID REFERENCES users(id)
);

-- Generated Projects (for tracking)
CREATE TABLE generated_projects (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id),
    template_id UUID REFERENCES templates(id),
    ast_json JSONB NOT NULL,                    -- Snapshot at generation time
    zip_url TEXT,                                 -- S3/R2 URL
    compilation_success BOOLEAN,
    diagnostics JSONB,                            -- Roslyn errors/warnings
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

## 3. Data Flow

### 3.1 Template Creation Flow
```
User drags components → Canvas State updates → AST Serializer
                                                      │
                                                      ▼
                                              ┌───────────────┐
                                              │  Validation   │
                                              │  Engine       │
                                              │  (Check wires)  │
                                              └───────┬───────┘
                                                      │
                                          ┌───────────┴───────────┐
                                          ▼                       ▼
                                    ┌──────────┐            ┌──────────┐
                                    │  Errors  │            │  Valid   │
                                    │  (Show   │            │  (Save   │
                                    │   red)   │            │   to DB) │
                                    └──────────┘            └──────────┘
```

### 3.2 Code Generation Flow
```
User clicks "Generate" → Frontend sends AST JSON → API Gateway
                                                          │
                                                          ▼
                                              ┌─────────────────────┐
                                              │  Project Service     │
                                              │  - Parse AST         │
                                              │  - Generate nodes    │
                                              └──────────┬──────────┘
                                                         │
                              ┌──────────────────────────┼──────────────────────────┐
                              ▼                          ▼                          ▼
                    ┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
                    │  Controller     │      │  Service        │      │  Entity         │
                    │  Generator      │      │  Generator      │      │  Generator      │
                    │  (Roslyn)       │      │  (Roslyn)       │      │  (Roslyn)       │
                    └────────┬────────┘      └────────┬────────┘      └────────┬────────┘
                             │                        │                        │
                             └────────────────────────┼────────────────────────┘
                                                      │
                                                      ▼
                                              ┌─────────────────────┐
                                              │  Mapper Generator   │
                                              │  (From Wiring Board)│
                                              └──────────┬──────────┘
                                                         │
                                                         ▼
                                              ┌─────────────────────┐
                                              │  Compilation Check  │
                                              │  (Roslyn.Emit)      │
                                              └──────────┬──────────┘
                                                         │
                                              ┌──────────┴──────────┐
                                              ▼                     ▼
                                        ┌──────────┐          ┌──────────┐
                                        │  Errors  │          │  Success │
                                        │  (Return │          │  (Create │
                                        │  to UI)  │          │  ZIP)    │
                                        └──────────┘          └──────────┘
```

### 3.3 Security Badge Injection Flow
```
Wire connects UI → API
       │
       ▼
┌─────────────────────────────┐
│  Check: Validation Badges?   │
│  on this wire?               │
└─────────────┬───────────────┘
              │
    ┌─────────┴─────────┐
    ▼                   ▼
┌────────┐        ┌────────────┐
│  YES   │        │  NO        │
│        │        │            │
│  Inject│        │  BLOCK     │
│  badge │        │  Wire      │
│  code  │        │  (Red)     │
│  into  │        │            │
│  mapper│        │  "Security │
│        │        │   required"│
└────────┘        └────────────┘
```

---

## 4. API Specification

### 4.1 Template Endpoints
```yaml
POST /api/templates
  Body: { name, description, ast_json, tags[], is_public, price_cents }
  Response: { id, slug, created_at }

GET /api/templates
  Query: ?search=auth&tags[]=jwt&page=1&limit=20
  Response: { items[], total, page }

GET /api/templates/{slug}
  Response: { id, name, description, ast_json, author, download_count, rating }

POST /api/templates/{slug}/fork
  Response: { new_slug, version }

POST /api/templates/{slug}/generate
  Body: { customizations: {} }
  Response: { download_url, compilation_success, diagnostics }
```

### 4.2 Project Generation Endpoints
```yaml
POST /api/projects/validate
  Body: { ast_json }
  Response: { is_valid, errors[], warnings[] }

POST /api/projects/generate
  Body: { ast_json, include_logic: false }
  Response: { zip_url, expires_at }
```

### 4.3 Validation Badge Endpoints
```yaml
GET /api/badges
  Response: { id, name, display_name, category, description }[]

POST /api/badges/{id}/preview
  Body: { input_type, field_name }
  Response: { generated_code, nuget_deps[] }
```

---

## 5. Security Architecture

### 5.1 Zero-Trust Design
| Layer | Trust Model | Enforcement |
|---|---|---|
| **Client** | Untrusted | All inputs validated server-side |
| **API Gateway** | Trusted boundary | Rate limiting, auth, request validation |
| **Template Hub** | Semi-trusted | Public AST only, no logic code |
| **Code Generation** | Trusted | Roslyn compilation sandbox |
| **Generated Output** | User-owned | Downloaded to local machine |

### 5.2 Data Privacy
- **Business Logic:** Never uploaded. Stored in browser localStorage only.
- **AST:** Public templates store structure. Private templates encrypt AST at rest.
- **Generated ZIPs:** Auto-deleted after 24 hours. S3 presigned URLs only.

---

## 6. Tech Stack Summary

| Layer | Technology | Justification |
|---|---|---|
| **Frontend** | React 18 + React-Flow + Zustand | Industry standard, excellent node-graph library |
| **Styling** | Tailwind CSS | Consistent dark theme, rapid development |
| **Backend** | ASP.NET Core 8 | Native .NET ecosystem, Roslyn integration |
| **Code Gen** | Roslyn (Microsoft.CodeAnalysis) | Real C# compilation, syntax tree manipulation |
| **Database** | PostgreSQL + EF Core | JSONB for AST storage, relational for users/templates |
| **File Storage** | Cloudflare R2 (S3-compatible) | Cheap, fast, global CDN for ZIPs |
| **Auth** | ASP.NET Core Identity + JWT | Standard, secure, well-documented |
| **AI Integration** | Claude API (client-side key) | Privacy: user brings their own key |
| **Hosting** | Railway / Render | Free tier for students, easy deploy |
| **Monitoring** | Sentry + LogRocket | Error tracking, session replay |

---

*End of TAD v2.0*
