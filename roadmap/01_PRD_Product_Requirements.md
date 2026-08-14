# netcn Phase 2 — Product Requirements Document (PRD)
## Version: 2.0 | Date: July 29, 2026 | Author: Bhandarihansraj

---

## 1. Executive Summary

**netcn** is a visual architecture platform for .NET developers. It allows users to design full-stack application structure via a drag-and-drop canvas, enforce data contracts through a visual "plug & socket" wiring board, and generate runnable .NET projects with a single click.

**Core Thesis:** *Architecture-first development eliminates the #1 cause of full-stack bugs — parameter mismatch between UI, API, and database layers.*

**Differentiator:** Unlike code editors (VS Code) or no-code builders (Bubble), netcn is an **architecture compiler** that generates production-ready .NET projects from visual diagrams with built-in security validation.

---

## 2. Problem Statement

### 2.1 The Parameter Mismatch Epidemic
```
UI sends:      user_id (string, snake_case)
API expects:   UserId (int, PascalCase)
DB stores:     userId (int, camelCase)
Result:        Runtime nulls, silent failures, 10+ hours of debugging
```

### 2.2 Current Pain Points
| Pain Point | Impact | Frequency |
|---|---|---|
| Scaffolding new .NET project | 30-60 min setup | Every new project |
| Writing boilerplate (DI, middleware, mappers) | 2-3 hours | Every project |
| Frontend/backend field naming mismatch | Silent bugs | Every API integration |
| Type mismatches (string → int, DateTime) | Runtime crashes | Every data flow |
| Security validation (SQLi, XSS) | Written inconsistently | Every input field |
| Sharing architecture patterns | Copy-paste from GitHub | Every team onboarding |

### 2.3 Target User Statement
> "As a developer, I want to design my application architecture visually and download a runnable project in under 5 minutes, so I can focus on business logic instead of boilerplate and debugging mismatches."

---

## 3. User Personas

### 3.1 Priya — 2nd Year CS Student
- **Goal:** Complete ASP.NET practicals without getting lost in syntax
- **Pain:** Doesn't understand `IServiceCollection`, DI, or middleware
- **Use Case:** Drags "Login Form" → "Auth API" → "Users DB", clicks Generate
- **Value:** Learns architecture patterns visually before syntax

### 3.2 Rahul — Freelance React Developer
- **Goal:** Needs a .NET backend for a client project
- **Pain:** Knows JavaScript, not C#. Doesn't want to learn full backend
- **Use Case:** Forks `clean-auth` template, customizes fields, downloads
- **Value:** Gets working backend without writing C# from scratch

### 3.3 Ankit — Senior Backend Engineer at Startup
- **Goal:** Standardize architecture across 5 microservices
- **Pain:** Every developer structures projects differently
- **Use Case:** Creates `company/clean-arch-template`, team forks it
- **Value:** Consistent architecture, auto-generated mappers, audit trail

### 3.4 CISO (Chief Information Security Officer)
- **Goal:** Visualize data flow and validate security controls
- **Pain:** Can't see how user input travels through the system
- **Use Case:** Opens wiring board, sees every input → validation → output
- **Value:** Compliance documentation generated from architecture diagram

---

## 4. Product Features

### 4.1 Core Features (MVP)

#### F1: Visual Design Canvas
- Drag-and-drop components: UI Forms, API Controllers, DB Tables, Services
- Connect components with arrows
- Set properties via inline editor
- Dark-themed UI (consistent with current design)

#### F2: Visual Plug & Socket Wiring Board
- Every component exposes **Plugs** (outputs) and **Sockets** (inputs)
- Draw wires between components across layers (UI ↔ API ↔ DB)
- **Live validation:** Type checking, name mapping, mismatch detection
- Color-coded status: Green (valid), Yellow (convertible), Red (blocked)

#### F3: Contract Dashboard (The "One Page")
- Single-page view of ALL connections in the project
- Table format: Source → Target → Status → Generated Mapper Code
- Filter by layer, component, or status
- Export as PDF for compliance/documentation

#### F4: Security Validation Badges
- Drag badges onto input wires: `[SQL Guard]`, `[XSS Shield]`, `[Rate Limit]`
- Each badge generates battle-tested validation code
- **Enforced:** No wire can connect UI to API without at least one validation badge
- Badge library: 10+ pre-built security modules

#### F5: Annotation-Driven Code Generation
- Drag badges onto components: `[ApiController]`, `[Authorize]`, `[JWT]`
- Each badge injects correct NuGet package + code + middleware
- Visual badge palette in sidebar

#### F6: AST-Based Code Generation
- Canvas state serializes to JSON AST (Abstract Syntax Tree)
- Backend uses **Roslyn** (`Microsoft.CodeAnalysis.CSharp`) to generate real C#
- Output: `.csproj`, `Program.cs`, Controllers, Services, Entities, Mappers
- Validates compilation before returning ZIP

#### F7: Template Hub (Docker Hub for .NET)
- Browse, search, fork templates by tags
- Publish templates with versioning
- Download count, ratings, author profiles
- Public templates (free) + Private templates (paid)

#### F8: Local AI Integration
- Business Logic Panel connects to Claude API (local key)
- Prompt: "Write validation for this contract"
- AI generates C# code, user pastes into Logic Panel
- **Privacy:** Logic code never uploaded to template hub

### 4.2 Post-MVP Features

| Feature | Description | Priority |
|---|---|---|
| One-Click Deploy | Deploy generated project to Azure/Railway | P1 |
| Visual Diff | See architecture changes between template versions | P2 |
| Team Collaboration | Real-time multi-user canvas editing | P2 |
| CI/CD Integration | Export GitHub Actions workflow from canvas | P3 |
| Mobile App | View contracts and approve changes on mobile | P3 |
| Plugin SDK | Let developers create custom validation badges | P3 |

---

## 5. Success Metrics

| Metric | MVP Target | 6-Month Target |
|---|---|---|
| Templates Published | 50 | 500 |
| Total Downloads | 1,000 | 50,000 |
| Average Time to First API | 30 min | 5 min |
| Parameter Mismatch Bugs Prevented | N/A (tracked via user survey) | 10,000+ |
| Security Badge Usage | 500 | 25,000 |
| Paid Subscriptions | 0 (free MVP) | 200 |

---

## 6. Constraints & Assumptions

### Constraints
- Must work in browser (no installation for canvas)
- Generated code must compile with `dotnet run` without errors
- Business logic never leaves user's machine
- Free tier must be genuinely useful for students

### Assumptions
- Users have basic understanding of MVC/API concepts
- AI providers (Claude) maintain API availability
- Roslyn can generate valid C# from well-formed AST
- Template marketplace will attract contributors organically

---

## 7. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| AI-generated code has security flaws | High | All AI output passes through validation badge pipeline |
| Template hub flooded with low-quality templates | Medium | Rating system + curated "Staff Picks" |
| Roslyn compilation fails on complex AST | Medium | Start with simple patterns, expand iteratively |
| Users don't understand plug/socket concept | High | Interactive tutorial + tooltips + video guides |
| Competition from Microsoft/Visual Studio | High | Focus on speed and security, not feature parity |

---

*End of PRD v2.0*
