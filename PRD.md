# netcn — Product Requirements Document (PRD)
**Product:** netcn — Visual Architecture Platform for .NET
**Author:** Bhandarihansraj
**Version:** 1.0
**Status:** Draft — derived from Vision Blueprint v1.0
**Date:** July 29, 2026

---

## 1. Purpose

This PRD defines *what* netcn must do to deliver on the vision: a visual canvas where developers design .NET application architecture, and the platform generates a real, runnable .NET project — with a contract layer that guarantees zero field-mismatch bugs between UI, API, and database.

This document does not cover implementation detail (see TRD) or threat handling (see SECURITY.md).

---

## 2. Problem Statement

Full-stack .NET development wastes time on two repetitive, error-prone tasks:

1. **Boilerplate setup** — 30–60 minutes scaffolding `Program.cs`, DI, folders, middleware before any real logic is written.
2. **Field mismatch bugs** — naming/type drift between UI (`user_id`), API (`UserId`), and DB (`user_id`/`userId`) causes silent nulls and hours of debugging.

netcn removes both by making architecture a first-class visual artifact that *is* the source of truth for generated code, not documentation of it.

---

## 3. Goals

- G1: Let a user visually design Controllers, Services, Entities, and their connections, and export a real, compiling .NET project.
- G2: Guarantee that any two connected fields across UI/API/DB layers are name- and type-consistent in generated code (the "wiring board" contract).
- G3: Let users publish and reuse architecture designs as versioned, forkable templates (a template hub).
- G4: Make architecture understandable to someone who does not yet know C# syntax.

## 4. Non-Goals (Out of Scope for MVP)

- Replacing an IDE for day-to-day coding (VS Code remains the coding tool).
- Full visual programming of arbitrary business logic (business logic stays in a code panel, not drag-and-drop).
- Supporting stacks beyond ASP.NET Core initially (other languages are future scope only).
- Real-time multi-user collaborative editing (future scope).

---

## 5. Target Users

| Persona | Need | How netcn Helps |
|---|---|---|
| **Student / beginner** | Learn architecture before syntax | Drag-and-drop teaches DI, routing, layering visually |
| **Freelancer / rapid prototyper** | Working API skeleton fast | 3-minute design → download → `dotnet run` |
| **Frontend/React dev needing a backend** | A working .NET backend without deep C# | Pick a template, customize visually, download |
| **Interview candidate** | Study a known-good architecture | Download reference templates (Clean Architecture, JWT auth, etc.) |
| **Enterprise team** | Standardize architecture patterns | Shared org templates everyone forks from |

---

## 6. Core User Flows

### 6.1 Design → Run (primary flow)
1. User drags Controller, Service, Entity nodes onto canvas.
2. User connects nodes and defines fields.
3. User opens the Wiring Board and connects matching fields across layers (UI → API → DB).
4. Wiring Board flags any unconnected or type-mismatched field (red/yellow).
5. Once all required wires are green, user clicks **Generate**.
6. Backend produces a real .NET project (via Roslyn) and returns a downloadable ZIP.
7. User runs `dotnet restore && dotnet run` — no manual edits needed to get it running.

### 6.2 Publish Template
1. User finishes a design.
2. Clicks **Publish Template**, sets name (`author/template-name`) and tags.
3. Template (AST JSON) is stored in the Template Registry, publicly listed.

### 6.3 Use Existing Template
1. User searches/browses Template Hub.
2. Selects a template, optionally forks/customizes it on canvas.
3. Clicks **Use Template** → same generation flow as 6.1 step 5 onward.

### 6.4 Add Cross-Cutting Behavior (Annotations)
1. User applies a badge (e.g. `[JWT]`, `[Authorize]`, `[Cache]`) to a node.
2. Generator wires in the matching NuGet package, middleware, and attribute at generation time.

---

## 7. Functional Requirements

### FR1 — Canvas & AST
- Canvas must represent Controllers, Services, Entities, and their fields/methods as nodes.
- Canvas state must serialize to a versioned, language-agnostic AST (JSON) — not HTML/string templates.

### FR2 — Wiring Board (Contract Layer)
- Every node must expose declared **Plugs** (outputs) and **Sockets** (inputs).
- User must be able to draw a connection ("wire") between a plug and a compatible socket.
- Each wire has a state: connected (green), convertible-with-transform (yellow), incompatible (red/blocked).
- A Contract Dashboard must list every wire, its status, and the exact line of generated mapping code it will produce.
- Code generation must be blocked while any required wire is red.

### FR3 — Code Generation
- Generation must read the AST + wire contracts and produce a real Roslyn-compiled `.csproj` project (not string substitution).
- Generated project must compile successfully before it is offered for download (compile-check gate).
- Output options: downloadable ZIP at minimum; Docker image and live preview URL are stretch goals.

### FR4 — Annotation System
- A fixed set of canvas badges (`[ApiController]`, `[Authorize]`, `[Validate]`, `[Cache]`, `[Audit]`, `[JWT]`) must each map deterministically to a specific NuGet package + generated code + middleware registration.

### FR5 — Template Hub
- Users must be able to publish a design as a named, tagged template.
- Users must be able to browse/search templates and fork one into their own canvas.
- Each template tracks author, tags, and download count.

### FR6 — Business Logic Panel
- For logic that can't be represented visually, a code panel must let the user write C# directly against pre-typed variables that come from the Contract Panel (so the logic panel's inputs are already guaranteed to be correctly wired).

---

## 8. Success Metrics

| Metric | Target (MVP) |
|---|---|
| Time from blank canvas to running project | ≤ 5 minutes |
| Generated project compile success rate | 100% (hard gate — must not ship a non-compiling ZIP) |
| Field-mismatch bugs in generated code | 0 (by construction — enforced by Wiring Board) |
| Templates published in Template Hub (first month) | Track as adoption signal, no fixed target for MVP |

---

## 9. MVP Phasing (from Vision Blueprint, Section 12)

1. **Foundation** — Canvas emits AST JSON; basic Template CRUD API.
2. **Roslyn Integration** — Generate one real controller + `Program.cs`; verify it compiles.
3. **Wiring Board** — Plug/socket exposure + visual wire drawing + type validation.
4. **Contract Dashboard** — One-page view of all wires with color status and generated mapper preview.
5. **Annotation System** — 5 initial badges, each injecting real package/code/middleware.
6. **Template Hub** — Browse, search, fork, author profiles, download counts, one-click run verification.

---

## 10. Open Questions

- Should generation validate against a real compiler on every request, or cache compiled results per AST hash?
- What is the minimum viable auth model for template authorship (see TRD/SECURITY for detail)?
- How are breaking changes to a published template's AST schema versioned for existing forks?

---

*This PRD is derived from `netcn_vision_blueprint.md` (July 29, 2026). See TRD.md for technical design and SECURITY.md for the security model.*
