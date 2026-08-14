# netcn Phase 2 — API Contract & Data Specification
## Version: 2.0 | Date: July 29, 2026 | Author: Bhandarihansraj

---

## 1. Overview

This document defines the complete data schema and API contract for netcn. It serves as the single source of truth for frontend-backend communication, AST structure, and wiring board validation.

---

## 2. AST (Abstract Syntax Tree) Schema

### 2.1 Root Project AST

```json
{
  "$schema": "https://netcn.dev/schemas/project-ast-v2.json",
  "projectId": "uuid",
  "name": "ecommerce-auth",
  "version": "1.0.0",
  "author": {
    "id": "uuid",
    "username": "bhandarihansraj"
  },
  "metadata": {
    "createdAt": "2026-07-29T10:00:00Z",
    "lastModified": "2026-07-29T10:30:00Z",
    "targetFramework": "net8.0",
    "projectType": "webapi"
  },
  "layers": {
    "ui": { "components": [...] },
    "api": { "controllers": [...], "services": [...] },
    "db": { "tables": [...] }
  },
  "wires": [...],
  "annotations": [...]
}
```

### 2.2 Layer: UI Components

```json
{
  "type": "ui",
  "id": "login-form-001",
  "componentType": "LoginForm",
  "label": "User Login",
  "position": { "x": 100, "y": 200 },
  "properties": {
    "title": "Sign In",
    "submitButtonText": "Login",
    "showRememberMe": true
  },
  "plugs": [
    {
      "id": "plug-login-form-output",
      "name": "formData",
      "type": "object",
      "schema": {
        "email": { "type": "string", "format": "email" },
        "password": { "type": "string", "minLength": 8 },
        "rememberMe": { "type": "boolean" }
      }
    }
  ],
  "sockets": [],
  "annotations": []
}
```

### 2.3 Layer: API Controllers

```json
{
  "type": "api",
  "id": "auth-controller-001",
  "componentType": "AuthController",
  "label": "Auth API",
  "position": { "x": 400, "y": 200 },
  "properties": {
    "routePrefix": "api/auth",
    "version": "v1"
  },
  "plugs": [
    {
      "id": "plug-auth-response",
      "name": "loginResponse",
      "type": "object",
      "schema": {
        "token": { "type": "string" },
        "expiresAt": { "type": "string", "format": "date-time" },
        "user": { "type": "ref", "target": "UserDto" }
      }
    }
  ],
  "sockets": [
    {
      "id": "socket-login-input",
      "name": "loginRequest",
      "type": "object",
      "required": true,
      "schema": {
        "email": { "type": "string", "format": "email" },
        "password": { "type": "string", "minLength": 8 }
      }
    }
  ],
  "annotations": [
    { "type": "ApiController" },
    { "type": "Route", "value": "api/[controller]" },
    { "type": "Authorize", "condition": "optional" }
  ]
}
```

### 2.4 Layer: Database Tables

```json
{
  "type": "db",
  "id": "users-table-001",
  "componentType": "Table",
  "label": "Users",
  "position": { "x": 700, "y": 200 },
  "properties": {
    "tableName": "users",
    "engine": "postgresql"
  },
  "plugs": [
    {
      "id": "plug-user-record",
      "name": "userRecord",
      "type": "object",
      "schema": {
        "id": { "type": "integer", "primaryKey": true },
        "email": { "type": "string", "maxLength": 255, "unique": true },
        "password_hash": { "type": "string", "maxLength": 255 },
        "created_at": { "type": "timestamp" }
      }
    }
  ],
  "sockets": [
    {
      "id": "socket-user-insert",
      "name": "insertData",
      "type": "object",
      "required": true,
      "schema": {
        "email": { "type": "string" },
        "password_hash": { "type": "string" }
      }
    }
  ],
  "annotations": []
}
```

### 2.5 Wire Schema

```json
{
  "id": "wire-login-flow",
  "from": {
    "layer": "ui",
    "componentId": "login-form-001",
    "plugId": "plug-login-form-output",
    "fieldPath": "email"
  },
  "to": {
    "layer": "api",
    "componentId": "auth-controller-001",
    "socketId": "socket-login-input",
    "fieldPath": "email"
  },
  "transform": {
    "type": "none",
    "description": "Direct mapping"
  },
  "validationBadges": [
    {
      "badgeId": "required-v1",
      "name": "Required",
      "config": {}
    },
    {
      "badgeId": "email-format-v1",
      "name": "EmailFormat",
      "config": {}
    },
    {
      "badgeId": "sql-guard-v2",
      "name": "SQLGuard",
      "config": { "strictMode": true }
    }
  ],
  "isActive": true,
  "metadata": {
    "createdAt": "2026-07-29T10:05:00Z",
    "modifiedAt": "2026-07-29T10:10:00Z"
  }
}
```

### 2.6 Annotation Schema

```json
{
  "id": "annotation-jwt-v1",
  "type": "JWTAuth",
  "targetType": "controller",
  "targetId": "auth-controller-001",
  "properties": {
    "issuer": "netcn-app",
    "audience": "netcn-users",
    "expiryHours": 24,
    "algorithm": "HS256"
  },
  "generatedCode": {
    "nugetPackages": ["Microsoft.AspNetCore.Authentication.JwtBearer"],
    "middleware": "app.UseAuthentication(); app.UseAuthorization();",
    "serviceRegistration": "builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)..."
  }
}
```

---

## 3. REST API Endpoints

### 3.1 Authentication

```yaml
POST /api/auth/register
  Request:
    email: string (required, email format)
    username: string (required, 3-30 chars, alphanumeric)
    password: string (required, min 8 chars)
  Response 201:
    user: { id, email, username }
    token: JWT string

POST /api/auth/login
  Request:
    email: string
    password: string
  Response 200:
    token: JWT string
    expiresAt: ISO 8601 datetime

POST /api/auth/refresh
  Request:
    refreshToken: string
  Response 200:
    token: JWT string
```

### 3.2 Templates

```yaml
GET /api/templates
  Query Parameters:
    search: string (optional) — search in name, description, tags
    tags: string[] (optional) — filter by tags
    author: string (optional) — filter by username
    sort: "downloads" | "rating" | "newest" (default: newest)
    page: integer (default: 1)
    limit: integer (default: 20, max: 100)
  Response 200:
    items: TemplateSummary[]
    total: integer
    page: integer
    totalPages: integer

GET /api/templates/{slug}
  Path: slug — e.g., "bhandarihansraj/clean-auth"
  Response 200:
    id: UUID
    slug: string
    name: string
    description: string
    author: UserSummary
    ast: ProjectAST
    version: string
    downloadCount: integer
    rating: { average: float, count: integer }
    tags: string[]
    priceCents: integer
    isPublic: boolean
    createdAt: ISO 8601
    updatedAt: ISO 8601

POST /api/templates
  Headers: Authorization: Bearer {JWT}
  Request:
    name: string (required, 3-100 chars)
    description: string (required, max 2000 chars)
    ast: ProjectAST (required)
    tags: string[] (max 10 tags)
    isPublic: boolean (default: true)
    priceCents: integer (default: 0, min: 0)
  Response 201:
    id: UUID
    slug: string
    status: "published" | "pending_review"

POST /api/templates/{slug}/fork
  Headers: Authorization: Bearer {JWT}
  Response 201:
    newSlug: string
    parentSlug: string
    ast: ProjectAST

POST /api/templates/{slug}/generate
  Headers: Authorization: Bearer {JWT}
  Request:
    customizations: object (optional) — override AST properties
    includeLogic: boolean (default: false) — include business logic stubs
  Response 200:
    downloadUrl: string (presigned, expires in 15 min)
    expiresAt: ISO 8601
    compilationSuccess: boolean
    diagnostics: Diagnostic[]
```

### 3.3 Projects (Generation)

```yaml
POST /api/projects/validate
  Headers: Authorization: Bearer {JWT}
  Request:
    ast: ProjectAST (required)
  Response 200:
    isValid: boolean
    errors: ValidationError[]
    warnings: ValidationWarning[]
    wireStatus: WireStatus[]

POST /api/projects/generate
  Headers: Authorization: Bearer {JWT}
  Request:
    ast: ProjectAST (required)
    templateId: UUID (optional) — if generating from template
    options:
      includeTests: boolean (default: false)
      includeDockerfile: boolean (default: false)
      targetFramework: "net8.0" | "net9.0" (default: "net8.0")
  Response 200:
    projectId: UUID
    downloadUrl: string
    expiresAt: ISO 8601
    compilationSuccess: boolean
    fileManifest: string[]
    diagnostics: Diagnostic[]

GET /api/projects/{id}
  Headers: Authorization: Bearer {JWT}
  Response 200:
    id: UUID
    status: "generating" | "compiled" | "failed"
    downloadUrl: string (if compiled)
    diagnostics: Diagnostic[]
```

### 3.4 Validation Badges

```yaml
GET /api/badges
  Response 200:
    items: Badge[]
    categories: string[]

GET /api/badges/{id}
  Response 200:
    id: UUID
    name: string
    displayName: string
    description: string
    category: "sanitization" | "security" | "format" | "compliance"
    tier: "free" | "pro" | "enterprise"
    configSchema: JSONSchema
    generatedCodeTemplate: string
    nugetDependencies: string[]
    usageCount: integer

POST /api/badges/{id}/preview
  Request:
    inputType: string
    fieldName: string
    config: object
  Response 200:
    generatedCode: string
    nugetDependencies: string[]
    estimatedPerformance: string
```

### 3.5 User Profile

```yaml
GET /api/user/me
  Headers: Authorization: Bearer {JWT}
  Response 200:
    id: UUID
    email: string
    username: string
    displayName: string
    subscriptionTier: "free" | "pro" | "enterprise"
    templates: TemplateSummary[]
    stats:
      templatesPublished: integer
      totalDownloads: integer
      totalEarningsCents: integer

GET /api/user/projects
  Headers: Authorization: Bearer {JWT}
  Response 200:
    items: GeneratedProject[]
```

---

## 4. WebSocket Events (Real-time)

For collaborative editing (Post-MVP):

```yaml
Connection: wss://api.netcn.dev/ws/canvas/{projectId}

Events:
  client → server:
    - cursor_move: { x, y, userId }
    - node_add: { node }
    - node_update: { id, properties }
    - wire_add: { wire }
    - wire_remove: { id }

  server → client:
    - user_joined: { userId, username }
    - user_left: { userId }
    - canvas_sync: { fullAST }
    - validation_update: { wireId, status }
```

---

## 5. Error Response Format

All errors follow RFC 7807 (Problem Details):

```json
{
  "type": "https://api.netcn.dev/errors/validation-failed",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more wires have validation errors",
  "instance": "/api/projects/validate",
  "errors": [
    {
      "field": "wires[2].validationBadges",
      "code": "MISSING_REQUIRED_BADGE",
      "message": "Wire from UI to API must have at least one validation badge",
      "wireId": "wire-login-flow"
    }
  ]
}
```

---

## 6. Validation Rules (API-Level)

### 6.1 AST Validation
```
RULE: Node IDs must be unique across all layers
RULE: Wire from/to must reference existing nodes
RULE: Wire fieldPath must exist in source plug schema
RULE: Wire fieldPath must exist in target socket schema
RULE: Cyclic dependencies are not allowed
RULE: Max 100 nodes per project
RULE: Max 200 wires per project
```

### 6.2 Badge Validation
```
RULE: [SQL Guard] requires string or object type input
RULE: [XSS Shield] requires string type input
RULE: [Rate Limit] requires API layer target
RULE: [JWT Validate] requires controller with [Authorize] annotation
RULE: Enterprise badges require active subscription
```

---

*End of API Contract v2.0*
