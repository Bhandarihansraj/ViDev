# netcn — Technical Requirements Document (TRD)
**Product:** netcn — Visual Architecture Platform for .NET
**Version:** 1.0
**Status:** Draft — derived from Vision Blueprint v1.0
**Date:** July 29, 2026

Companion to PRD.md (what to build) and SECURITY.md (how to keep it safe). This document defines *how* it is built.

---

## 1. System Architecture

```
Browser (Canvas)  →  AST JSON  →  Backend (Template Registry + Generation API)
                                        │
                                        ▼
                          Roslyn Code Generation Engine
                                        │
                                        ▼
                     Output: ZIP / Docker image / live preview
```

### Components
1. **Canvas (Frontend)** — node-graph editor; produces/consumes AST JSON. No code generation happens client-side.
2. **Template Registry (Backend API)** — stores AST as versioned templates; CRUD + search + fork.
3. **Code Generation Engine** — reads AST + wiring contract, builds a Roslyn `SyntaxTree`, emits real `.cs`/`.csproj` files, compiles to verify before returning output.
4. **Output Delivery** — packages compiled project as a ZIP (MVP); Docker image and live-preview URL are post-MVP.

---

## 2. Technology Stack

| Layer | Technology | Rationale |
|---|---|---|
| Frontend canvas | React + React-Flow | Built-in drag-and-drop node graph primitives |
| Backend API | ASP.NET Core 8 | Native fit for a .NET-generation platform; also dogfoods the stack |
| Database | PostgreSQL (Supabase/Neon) | Stores AST JSON + template metadata |
| Code generation | Roslyn (`Microsoft.CodeAnalysis.CSharp`) | Real compilation, not string templating |
| Template/output storage | Object storage (S3 / Cloudflare R2) | Store generated ZIPs, avoid bloating the DB |
| Auth | ASP.NET Core Identity or a managed provider (e.g. Clerk) | Needed once templates have named authors |
| Hosting | Render.com / Railway (MVP) | Free/low-cost tier suitable for early stage |

---

## 3. Data Model

### 3.1 AST (Template Source)
The AST is the canonical, language-agnostic representation of a design. Example (Controller + Service):

```json
{
  "templateId": "user-auth-flow-v1",
  "author": "bhandarihansraj",
  "nodes": [
    {
      "type": "Controller",
      "id": "AuthController",
      "annotations": ["ApiController", "Route:api/[controller]"],
      "methods": [
        {
          "name": "Login",
          "verb": "POST",
          "route": "login",
          "annotations": ["AllowAnonymous", "ValidateModel"],
          "parameters": [{ "name": "dto", "type": "LoginDto", "fromBody": true }],
          "body": [
            { "type": "ServiceCall", "service": "AuthService", "method": "Validate" },
            { "type": "Return", "value": "JWT token" }
          ]
        }
      ]
    },
    { "type": "Service", "id": "AuthService", "annotations": ["Scoped"], "implements": "IAuthService" }
  ]
}
```

### 3.2 Wiring Data Model (Contract Layer)
```json
{
  "projectId": "ecommerce-app",
  "layers": {
    "ui": { "components": [] },
    "api": { "controllers": [] },
    "db": { "tables": [] }
  },
  "wires": [
    {
      "id": "wire-1",
      "from": { "layer": "ui", "component": "LoginForm", "field": "user_id" },
      "to": { "layer": "api", "component": "AuthController", "field": "UserId" },
      "transform": "ParseInt",
      "isActive": true
    }
  ]
}
```

Each `wire` maps 1:1 to a generated mapper line. Wires are the only source of mapping code — nothing is hand-typed.

### 3.3 Database Schema (minimum viable)
- `templates (id, author_id, name, tags[], ast_json, version, download_count, created_at)`
- `users (id, username, auth_provider_id, created_at)`
- `generation_jobs (id, template_id, status, output_url, compiled boolean, created_at)`

---

## 4. API Surface (MVP)

| Endpoint | Purpose |
|---|---|
| `POST /templates` | Save a new AST as a template |
| `GET /templates` | List/search templates |
| `GET /templates/{id}` | Fetch a template's AST |
| `POST /templates/{id}/fork` | Copy a template into the requesting user's namespace |
| `POST /generate` | Submit AST + wiring contract → generation job |
| `GET /generate/{jobId}` | Poll generation status / retrieve output |

All write endpoints require authentication (see SECURITY.md §2).

---

## 5. Code Generation Pipeline

1. **Validate wiring** — every wire is checked for type compatibility (`CanMap(sourceType, targetType, transform)`); any incompatible/unconnected required wire blocks generation.
2. **Build SyntaxTree** — AST nodes map to Roslyn syntax factory calls (Controllers → `ClassDeclarationSyntax` with `[ApiController]`, methods → `MethodDeclarationSyntax`, etc.).
3. **Apply annotations** — each badge (`[JWT]`, `[Authorize]`, `[Cache]`, ...) triggers a fixed code-generation module that:
   - adds the required NuGet package reference,
   - injects the relevant `Program.cs` service registration,
   - adds the attribute to the generated class/method.
4. **Generate mapper code** — one mapper file per layer boundary (UI→API, API→DB), built directly from the wire list. Mapper files are marked generated (`// DO NOT EDIT — generated from wiring board`).
5. **Compile-check gate** — the emitted project MUST compile successfully in a sandboxed build step before it is returned to the user. A non-compiling result is a generation failure, not a partial success.
6. **Package output** — zip the project (MVP); Docker image build and live-preview deployment are post-MVP output options.

---

## 6. Non-Functional Requirements

| Requirement | Target |
|---|---|
| Generation latency (simple template) | Under a few seconds for MVP-scale templates |
| Compile-check gate | Mandatory — no ungated output ships |
| Template AST versioning | Templates are immutable once published; edits create a new version, forks are independent copies |
| Availability | Best-effort for MVP; no HA requirement at this stage |
| Observability | Log every generation job's status (queued/compiling/success/failed) for debugging and metrics |

---

## 7. Security Requirements (summary — full detail in SECURITY.md)

- All authenticated write paths (publish, fork, generate) require verified identity.
- Generated code execution/compilation must run in an isolated sandbox — the compile-check step executes untrusted, user-authored AST-derived code and must never run with access to production credentials, the host filesystem beyond a scratch dir, or network egress.
- Template content (AST JSON) is user input and must be schema-validated before being handed to the code generator — treat it as untrusted, not as trusted config.
- Standard API hardening applies: input validation, rate limiting on `/generate` and `/templates`, and secrets kept out of generated output and version control.

---

## 8. Phased Technical Build Order

Mirrors PRD §9:
1. AST schema + Template CRUD API (no Roslyn yet).
2. Roslyn integration for one controller + `Program.cs`, with the compile-check gate in place from day one.
3. Wiring Board UI + backend wire validation.
4. Contract Dashboard (read-only view over the wiring data model).
5. Annotation → code/package/middleware injection modules (5 badges).
6. Template Hub browse/search/fork + author profiles + download counts.

---

*Derived from `netcn_vision_blueprint.md` (July 29, 2026). See PRD.md for product scope and SECURITY.md for the threat model.*
