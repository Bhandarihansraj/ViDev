# netcn Phase 2 — Implementation Playbook
## Version: 2.0 | Date: July 29, 2026 | Author: Bhandarihansraj
## Purpose: Turn vision into executable sprints

---

## 1. Philosophy

> **"You are the architect. Claude is the builder. This playbook is the blueprint."**

This document breaks the entire project into **12 weekly sprints**. Each sprint has:
- Clear deliverables
- Claude prompts (copy-paste ready)
- Success criteria
- What to own vs. what to delegate to AI

---

## 2. Tech Stack (Locked)

| Layer | Technology | Why |
|---|---|---|
| **Frontend** | React 18 + Vite + React-Flow | Fast, modern, excellent node-graph library |
| **State** | Zustand | Simple, no boilerplate, persists to localStorage |
| **Styling** | Tailwind CSS + shadcn/ui | Dark theme out of box, rapid UI development |
| **Backend** | ASP.NET Core 8 Web API | Native .NET, Roslyn integration, your domain |
| **ORM** | Entity Framework Core + PostgreSQL | JSONB for AST, migrations, well-documented |
| **Code Gen** | Roslyn (Microsoft.CodeAnalysis.CSharp) | Real compilation, syntax tree manipulation |
| **Storage** | Cloudflare R2 (S3-compatible) | Cheap, fast, no egress fees |
| **Auth** | ASP.NET Core Identity + JWT | Standard, secure, well-understood |
| **AI** | Claude API (client-side key) | Privacy-first, user brings their own key |
| **Hosting** | Railway (API) + Vercel (Frontend) | Free tier, GitHub auto-deploy |

---

## 3. 12-Week Sprint Plan

---

### SPRINT 1: Foundation — AST Schema & Canvas Refactor
**Week:** 1  
**Goal:** The canvas outputs a valid AST JSON instead of HTML strings.

#### Deliverables
- [ ] Define complete AST schema (Project, Node, Wire, Annotation)
- [ ] Refactor current canvas to use React-Flow
- [ ] Implement node types: UI, API, DB
- [ ] Each node exposes Plugs and Sockets visually
- [ ] Canvas state serializes to AST JSON
- [ ] Save/load AST to localStorage

#### Claude Prompt
```
I am building a visual architecture tool called netcn. I need to refactor 
my vanilla JS canvas to React + React-Flow. 

Current state: I have a drag-and-drop canvas that generates HTML strings.
Target state: A React-Flow canvas where each node has "plugs" (outputs) 
and "sockets" (inputs) that can be connected.

Requirements:
1. Three node types: UI (blue), API (green), DB (orange)
2. Each node has handles on left (sockets) and right (plugs)
3. Clicking a plug and dragging to a socket creates a connection
4. All state is stored in Zustand
5. Export a function getAST() that returns the full graph as JSON

Please generate:
- React component for custom node with plugs/sockets
- Zustand store for canvas state
- React-Flow wrapper component
- getAST() serializer function

Use TypeScript. Use Tailwind CSS for styling (dark theme).
```

#### Success Criteria
- [ ] Can drag 3 node types onto canvas
- [ ] Can connect plug to socket
- [ ] `getAST()` returns valid JSON matching schema
- [ ] State persists after page refresh

---

### SPRINT 2: Wiring Board — Visual Contract Layer
**Week:** 2  
**Goal:** The wiring board validates connections in real-time.

#### Deliverables
- [ ] Wire data model (from, to, transform, badges)
- [ ] Type checking on connection (string → int = yellow warning)
- [ ] Name mapping visualization (user_id ↔ UserId)
- [ ] Red/Yellow/Green wire coloring
- [ ] Validation: cyclic detection, orphan nodes

#### Claude Prompt
```
I need to build the "wiring board" validation engine for netcn.

Context: Users connect UI outputs (plugs) to API inputs (sockets) via wires.
Each plug/socket has: name, type (string/int/bool/DateTime), isArray, nullable.

Requirements:
1. When a wire connects plug to socket, check type compatibility:
   - Exact match = green
   - Convertible (string→int with ParseInt) = yellow + suggest transform
   - Incompatible (object→string without toString) = red + block
2. Check for circular dependencies (UI→API→Service→UI = red)
3. Check for orphan nodes (no connections = gray)
4. Generate a "contract" object showing all mappings

Please generate:
- Type compatibility matrix
- Wire validation engine (pure TypeScript functions)
- Contract generator
- React component to display wire status
```

#### Success Criteria
- [ ] Connecting string→int shows yellow + ParseInt suggestion
- [ ] Connecting incompatible types shows red + blocks
- [ ] Circular dependency detected and flagged
- [ ] Contract object lists all active mappings

---

### SPRINT 3: Backend API — Template CRUD
**Week:** 3  
**Goal:** Backend can store and retrieve templates.

#### Deliverables
- [ ] ASP.NET Core 8 project scaffold
- [ ] PostgreSQL database with EF Core
- [ ] Template entity + CRUD endpoints
- [ ] Search, filter, pagination
- [ ] JWT authentication setup

#### Claude Prompt
```
I need an ASP.NET Core 8 Web API for a template marketplace.

Requirements:
1. Entity: Template (Id, Slug, Name, Description, AstJson, AuthorId, 
   Version, IsPublic, PriceCents, DownloadCount, Tags[])
2. Endpoints:
   - POST /api/templates (create, requires auth)
   - GET /api/templates (list, search by name/tag, paginate)
   - GET /api/templates/{slug} (get single)
   - POST /api/templates/{slug}/fork (copy, requires auth)
3. Use Entity Framework Core with PostgreSQL
4. Use JWT authentication with ASP.NET Core Identity
5. AstJson stored as JSONB in PostgreSQL

Please generate:
- Complete Program.cs with services configuration
- DbContext with entity configuration
- TemplateController with all endpoints
- DTOs for request/response
- JWT setup in Program.cs
```

#### Success Criteria
- [ ] Can create template via API
- [ ] Can search templates
- [ ] Can fork template
- [ ] JWT auth protects create/fork endpoints

---

### SPRINT 4: Roslyn Integration — Hello World Compiler
**Week:** 4  
**Goal:** Backend can generate and compile a simple C# project.

#### Deliverables
- [ ] Roslyn service that generates SyntaxTree from AST
- [ ] Generate: Program.cs, one Controller, one Entity
- [ ] Compile to DLL and verify success
- [ ] Package as ZIP with .csproj

#### Claude Prompt
```
I need a Roslyn code generation service in C#.

Context: I have an AST (Abstract Syntax Tree) representing a .NET project.
The AST has nodes like: Controller, Service, Entity.

Requirements:
1. Read AST JSON and generate C# SyntaxTrees using Roslyn
2. For a Controller node, generate:
   - Class with [ApiController] and [Route] attributes
   - Methods based on node.methods array
   - Parameters with [FromBody] where applicable
3. For an Entity node, generate:
   - Class with properties
   - EF Core annotations ([Key], [Required], etc.)
4. Generate Program.cs with:
   - builder.Services.AddControllers()
   - app.MapControllers()
5. Compile all SyntaxTrees into a CSharpCompilation
6. Emit to MemoryStream, verify success
7. Package all source files + .csproj into a ZIP

Please generate:
- ICodeGenerationService interface
- RoslynCodeGenerationService implementation
- SyntaxTree generators for Controller and Entity
- ZIP packaging logic
```

#### Success Criteria
- [ ] POST AST → returns compiled ZIP
- [ ] ZIP contains valid .csproj
- [ ] `dotnet run` on extracted ZIP starts API

---

### SPRINT 5: Security Badges — Input Validation Engine
**Week:** 5  
**Goal:** Validation badges generate real C# validation code.

#### Deliverables
- [ ] Badge data model (name, category, generated code template)
- [ ] 5 core badges: Required, Length, Regex, SQL Guard, XSS Shield
- [ ] Badge injection into generated mappers
- [ ] Visual badge palette in frontend

#### Claude Prompt
```
I need a validation badge system for netcn.

Context: Users drag "badges" onto wires between UI and API. 
Each badge generates C# validation code.

Badge definitions:
1. [Required]: checks string.IsNullOrWhiteSpace
2. [Length:50]: checks input.Length <= max
3. [Regex]: checks Regex.IsMatch(input, pattern)
4. [SQL Guard]: parameterized query + blacklist check
5. [XSS Shield]: HtmlEncoder.Default.Encode(input)

Requirements:
1. Each badge has a "code template" with {{placeholders}}
2. When generating mapper code, badges are injected in order
3. Generated code is a static validation method
4. If wire has no badges and connects UI→API, generation BLOCKS

Please generate:
- Badge entity and seed data
- ValidationCodeGenerator service
- Generated code examples for each badge
- Frontend badge palette component (React)
```

#### Success Criteria
- [ ] Can drag badge onto wire
- [ ] Generated code includes validation method
- [ ] Wire without badges blocks generation
- [ ] SQL Guard generates parameterized query code

---

### SPRINT 6: Contract Dashboard — The "One Page"
**Week:** 6  
**Goal:** Single page showing all connections, status, and generated code.

#### Deliverables
- [ ] Table view: Source → Target → Status → Code Preview
- [ ] Filter by layer, component, status
- [ ] Export as PDF
- [ ] Real-time updates as canvas changes

#### Claude Prompt
```
I need a "Contract Dashboard" React component for netcn.

Context: Users want to see ALL wire connections in their project 
in one view. Each wire has: source (UI plug), target (API socket), 
status (green/yellow/red), and generated mapper code.

Requirements:
1. Table with columns: Source Layer | Source Field | → | Target Layer | Target Field | Status | Preview
2. Filter dropdowns: by layer (UI/API/DB), by status, by component
3. Status badge: green (valid), yellow (warning), red (blocked)
4. Expandable row showing generated C# mapper code
5. "Export PDF" button using jsPDF
6. Auto-updates when Zustand store changes

Please generate:
- ContractDashboard React component
- Filter logic
- PDF export function
- Sample data for testing
```

#### Success Criteria
- [ ] Dashboard shows all wires
- [ ] Filter by status works
- [ ] PDF export contains all mappings
- [ ] Updates in real-time with canvas

---

### SPRINT 7: Template Hub UI — Browse, Search, Fork
**Week:** 7  
**Goal:** Users can discover and use community templates.

#### Deliverables
- [ ] Template browse page with cards
- [ ] Search bar + tag filters
- [ ] Template detail page with preview
- [ ] Fork button (copies to user's account)
- [ ] Download count, ratings display

#### Claude Prompt
```
I need a Template Hub frontend for netcn.

Requirements:
1. Browse page: grid of template cards with name, author, tags, 
   download count, rating stars
2. Search bar: filters by name/description in real-time
3. Tag pills: clickable to filter
4. Template detail page:
   - Large preview of AST structure
   - Author info
   - "Use Template" button (loads into canvas)
   - "Fork" button (copies to my account)
5. All data fetched from /api/templates endpoints

Please generate:
- TemplateCard component
- TemplateBrowse page
- TemplateDetail page
- Search/filter hook
```

#### Success Criteria
- [ ] Can browse templates
- [ ] Can search by name
- [ ] Can filter by tag
- [ ] Can fork template
- [ ] Forked template loads in canvas

---

### SPRINT 8: AI Integration — Business Logic Panel
**Week:** 8  
**Goal:** Users can generate custom business logic via Claude API.

#### Deliverables
- [ ] Logic node type in canvas
- [ ] Code editor panel (Monaco Editor)
- [ ] Claude API integration (client-side key)
- [ ] Prompt templates for common logic patterns

#### Claude Prompt
```
I need an AI-powered business logic panel for netcn.

Context: Users double-click a "Logic" node in the canvas to open 
a code editor. They can write C# or ask Claude to generate it.

Requirements:
1. Monaco Editor embedded in a slide-out panel
2. "Generate with AI" button that sends prompt to Claude API
3. Prompt template: "Write validation logic for LoginRequest with 
   fields: email (string), password (string). Use the validation 
   badges: Required, EmailFormat, SQLGuard."
4. Generated code appears in editor, user can edit
5. Logic is stored in localStorage, NEVER uploaded to server
6. When generating project, logic code is injected into generated files

Please generate:
- LogicPanel React component
- Claude API integration hook
- Prompt template builder
- localStorage persistence for logic code
```

#### Success Criteria
- [ ] Can open logic panel
- [ ] Can prompt Claude to generate code
- [ ] Generated code appears in editor
- [ ] Logic persists in localStorage
- [ ] Logic code included in generated ZIP

---

### SPRINT 9: Annotation System — Spring Boot for .NET
**Week:** 9  
**Goal:** Component badges generate attributes, middleware, and DI.

#### Deliverables
- [ ] Annotation badge system: [ApiController], [Authorize], [JWT], [Cache]
- [ ] Each badge injects correct NuGet package
- [ ] Each badge injects correct middleware in Program.cs
- [ ] Visual annotation palette

#### Claude Prompt
```
I need an annotation/badge system for netcn that works like 
Spring Boot annotations but for .NET.

Annotations needed:
1. [ApiController] → adds [ApiController] attribute, sets up routing
2. [Authorize] → adds [Authorize], sets up JWT/auth middleware
3. [JWT] → adds JWT Bearer package, configures token validation
4. [Cache] → adds ResponseCache, sets up Redis/memory cache
5. [ValidateModel] → adds ModelState validation filter

Requirements:
1. Each annotation has: C# attribute code, middleware code, 
   service registration code, NuGet dependencies
2. When generating project, annotations are collected from all nodes
3. Program.cs is generated with all required services/middleware
4. .csproj includes all required NuGet packages
5. Frontend shows annotation palette per node type

Please generate:
- Annotation definitions (JSON or C# records)
- Program.cs generator that aggregates annotations
- .csproj generator with NuGet deps
- Frontend annotation palette component
```

#### Success Criteria
- [ ] Can add [JWT] badge to controller
- [ ] Generated Program.cs includes JWT setup
- [ ] Generated .csproj includes JwtBearer package
- [ ] Multiple annotations aggregate correctly

---

### SPRINT 10: Polish — UX, Performance, Edge Cases
**Week:** 10  
**Goal:** The product feels professional, not student-project.

#### Deliverables
- [ ] Loading states and skeleton screens
- [ ] Error boundaries and friendly error messages
- [ ] Undo/redo in canvas (Ctrl+Z)
- [ ] Keyboard shortcuts
- [ ] Mobile-responsive template hub
- [ ] Onboarding tutorial (interactive walkthrough)

#### Claude Prompt
```
I need to polish my React app netcn for production.

Current issues:
1. No loading states — buttons feel unresponsive
2. No error handling — crashes show white screen
3. No undo/redo in canvas
4. No keyboard shortcuts
5. Mobile layout is broken

Please generate:
- Loading skeleton components for cards and tables
- ErrorBoundary with fallback UI
- Undo/redo system using Zustand (command pattern)
- Keyboard shortcuts hook (Ctrl+Z, Ctrl+S, Delete)
- Responsive CSS adjustments for mobile
- Simple onboarding tooltip system
```

#### Success Criteria
- [ ] Every async action has loading state
- [ ] Errors show friendly message, not crash
- [ ] Ctrl+Z undoes last canvas action
- [ ] Works on mobile (template hub)
- [ ] New user sees onboarding on first visit

---

### SPRINT 11: Security Hardening — Purple Team Review
**Week:** 11  
**Goal:** The platform is secure enough for real users.

#### Deliverables
- [ ] Rate limiting on all endpoints
- [ ] Input sanitization (AST size limits, depth limits)
- [ ] CORS configuration
- [ ] Security headers (HSTS, CSP, X-Frame-Options)
- [ ] Dependency scanning (Snyk or similar)
- [ ] Penetration test of template hub

#### Claude Prompt
```
I need to harden my ASP.NET Core 8 API for production.

Requirements:
1. Rate limiting: 10 requests/minute per IP for generation, 
   100/minute for reads
2. Request size limits: AST max 10MB, max 50 nested nodes
3. CORS: only allow netcn.dev and localhost:5173
4. Security headers: HSTS, CSP, X-Content-Type-Options, 
   X-Frame-Options, Referrer-Policy
5. Input sanitization: strip HTML from text fields, validate JSON schema
6. Dependency audit: check for known vulnerabilities

Please generate:
- Rate limiting middleware configuration
- Security headers middleware
- CORS policy
- Input validation attributes
- Program.cs with all security configurations
```

#### Success Criteria
- [ ] Rate limits enforced
- [ ] Security headers present on all responses
- [ ] CORS blocks unauthorized origins
- [ ] No known vulnerabilities in dependencies

---

### SPRINT 12: Launch — Deploy, Announce, Iterate
**Week:** 12  
**Goal:** Live product with first real users.

#### Deliverables
- [ ] Deploy frontend to Vercel
- [ ] Deploy backend to Railway
- [ ] Configure custom domain (netcn.dev)
- [ ] Set up monitoring (Sentry, LogRocket)
- [ ] Write launch post for Dev.to/Reddit
- [ ] Create demo video (2 minutes)
- [ ] Soft launch to 10 beta users

#### Claude Prompt
```
I need a launch checklist and announcement copy for netcn.

Product: Visual architecture platform for .NET developers.
Key features: drag-and-drop canvas, wiring board with validation, 
security badges, template hub, AI-generated business logic.

Please generate:
1. Launch checklist (deployment, monitoring, domains)
2. Dev.to article: "I built a Figma for .NET in 12 weeks"
3. Reddit post for r/dotnet (follows community rules)
4. Twitter thread (5 tweets) with key features
5. Demo video script (2 minutes, voiceover)
6. Beta user onboarding email template
```

#### Success Criteria
- [ ] Site live on custom domain
- [ ] Monitoring captures errors
- [ ] Launch post published
- [ ] 10 beta users invited
- [ ] First template published by non-author

---

## 4. Claude Prompt Templates (Reusable)

### Prompt Template A: Generate React Component
```
I need a React component for netcn (visual .NET architecture tool).

Context: [describe where this fits]
Requirements:
1. [list functional requirements]
2. Use TypeScript
3. Use Tailwind CSS (dark theme: bg-gray-900, text-gray-100)
4. Use Zustand for state if needed
5. Make it accessible (ARIA labels, keyboard navigation)

Please generate the complete component with types and sample usage.
```

### Prompt Template B: Generate ASP.NET Endpoint
```
I need an ASP.NET Core 8 endpoint for netcn.

Context: [describe the feature]
Requirements:
1. [list endpoint requirements]
2. Use Entity Framework Core with PostgreSQL
3. Use JWT authentication where needed
4. Return ProblemDetails for errors
5. Include validation attributes

Please generate: Controller, DTOs, and Service method.
```

### Prompt Template C: Generate Roslyn Code
```
I need Roslyn code generation for netcn.

Context: [describe the AST node type]
Input: AST node with properties [list]
Output: C# SyntaxTree

Requirements:
1. Use Microsoft.CodeAnalysis.CSharp
2. Generate compilable C#
3. Include proper usings
4. Add XML documentation comments

Please generate the generator method.
```

---

## 5. What You Own vs. What Claude Builds

| Task | You Own | Claude Builds |
|---|---|---|
| **Architecture decisions** | ✅ | ❌ |
| **AST schema design** | ✅ | ❌ |
| **Feature prioritization** | ✅ | ❌ |
| **Security badge logic** | ✅ | ❌ |
| **Business model** | ✅ | ❌ |
| **React components** | ❌ | ✅ |
| **API endpoints** | ❌ | ✅ |
| **Roslyn generators** | ❌ | ✅ |
| **Database schema** | ❌ | ✅ |
| **CSS/styling** | ❌ | ✅ |
| **Unit tests** | ❌ | ✅ |
| **Deployment config** | ❌ | ✅ |

**Your job:** Review every line Claude generates. Understand it. Modify it. Make it yours.

---

## 6. Daily Workflow

```
Morning (30 min):
  1. Review yesterday's Claude output
  2. Test it locally
  3. Note what works and what doesn't

Mid-day (2-3 hours):
  4. Write detailed prompt for next feature
  5. Feed Claude the prompt + context
  6. Review generated code
  7. Integrate into project
  8. Test end-to-end

Evening (30 min):
  9. Commit working code
  10. Write prompt for tomorrow
  11. Update this playbook with learnings
```

---

## 7. Success Metrics by Sprint

| Sprint | Metric | Target |
|---|---|---|
| 1 | AST JSON valid | 100% schema compliance |
| 2 | Wire validation | 0 false positives |
| 3 | API response time | <200ms p95 |
| 4 | Compilation success | 100% for simple projects |
| 5 | Badge coverage | All UI→API wires have badges |
| 6 | Dashboard accuracy | Matches canvas state exactly |
| 7 | Template discovery | <3 clicks to find template |
| 8 | AI generation | <10s for simple logic |
| 9 | Annotation coverage | All common patterns supported |
| 10 | Lighthouse score | >90 performance |
| 11 | Security scan | 0 critical vulnerabilities |
| 12 | Live users | 10 beta signups |

---

*End of Implementation Playbook v2.0*  
*Now go build it, architect.* 🏗️
