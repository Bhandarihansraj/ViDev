# netcn — Security Architecture Document
**Version:** 1.0 | **Date:** July 29, 2026
**Companion to:** PRD.md, TRD.md

netcn's core risk profile is different from a typical CRUD app: it **compiles and executes user-authored input** (the AST) as part of normal operation, and it **redistributes user-generated code** to other users via the Template Hub. Both of these are supply-chain-shaped risks and are treated as such.

---

## 1. Threat Model Summary

| Asset | Threat | Primary Risk |
|---|---|---|
| Code Generation Engine | Malicious/crafted AST triggers arbitrary code execution during compile-check | Full host compromise |
| Template Registry | Malicious template published, then forked/downloaded by other users | Supply-chain attack via "trusted" templates |
| Generated ZIP output | Injected malicious code hidden in generated project | User runs compromised code locally (`dotnet run`) |
| Template/API storage | Credential or PII leakage | Data breach |
| API endpoints | Abuse (spam templates, DoS via generation requests) | Availability/cost impact |

---

## 2. AuthN / AuthZ

- All write operations (`POST /templates`, `POST /generate`, fork) require an authenticated session. No anonymous writes.
- Read/browse of public templates can remain unauthenticated.
- Authorization is owner-scoped: a user may edit/delete only templates they authored; forking creates an independent copy owned by the forker.
- Use an established auth provider (ASP.NET Core Identity or a managed identity provider) rather than hand-rolled session/token logic.

---

## 3. Sandboxing the Code Generation / Compile-Check Step

This is the single highest-risk component in the system, since it takes user input (AST) and runs a real compiler against derived code.

- The Roslyn compile-check MUST run in an isolated environment (ephemeral container/sandbox) — never in the same process or host as the API server.
- The sandbox must have:
  - no network egress,
  - no access to production secrets, database credentials, or other users' data,
  - a hard CPU/memory/time limit per compile job, with the job killed and marked failed on timeout,
  - a disposable filesystem scoped to that single job.
- Treat every AST as hostile input until proven otherwise by schema validation — never string-interpolate AST content directly into shell commands or file paths (path traversal risk via crafted `id`/`name` fields).

---

## 4. AST & Template Input Validation

- Every AST submitted to `/templates` or `/generate` must pass strict schema validation (types, required fields, enum values for annotations) before touching the generation engine.
- Field names, template names, and tags must be validated against an allow-list pattern (alphanumeric, hyphen, underscore) to prevent them being used as injection vectors into generated file paths, class names, or shell invocations.
- Annotation badges (`[JWT]`, `[Authorize]`, etc.) must map to a **fixed, backend-defined** set of code/package injections — never let AST content specify arbitrary NuGet packages or arbitrary code strings to inject.

---

## 5. Template Hub Supply-Chain Controls

- Published templates are public artifacts other users will run locally — apply the same scrutiny as a package registry:
  - Run every published template through the same compile-check sandbox before it's listed as "verified"/"runs successfully."
  - Consider a visible trust signal (e.g., "compiled & verified" badge) separate from raw popularity/download count.
  - Provide a reporting/takedown path for templates later found to embed malicious logic.
- Forking copies AST, not a live reference — a compromised original template cannot retroactively affect prior forks.

---

## 6. Generated Output Integrity

- Generated mapper files and boilerplate should be clearly marked as generated (`// DO NOT EDIT — generated from wiring board`) so users can distinguish platform-generated code from their own logic-panel code during review.
- The ZIP delivered to users should not embed any platform secrets, API keys, or internal service URLs — generated `Program.cs`/config files must use placeholders/environment variables for anything sensitive, never hardcoded credentials.

---

## 7. API Hardening

- Rate limit `/generate` and `/templates` (POST) per user/IP to prevent compute-cost abuse from repeated compile jobs.
- Standard input validation and output encoding on all endpoints; no raw AST content reflected into API responses without sanitization.
- Log generation job outcomes (success/failure/timeout) for abuse detection, without logging full user AST content if it could contain sensitive business logic — log metadata (job id, template id, status, duration) rather than payloads by default.

---

## 8. Secrets & Infrastructure

- Database credentials, object storage keys, and auth provider secrets live in environment/secret manager config — never committed to the repo, never embedded in generated projects.
- Separate credentials for the sandboxed compile-check environment from the main API/database credentials, so a sandbox escape does not directly yield production DB access.

---

## 9. Security Backlog by MVP Phase

| Phase | Security Task |
|---|---|
| Foundation (AST + Template CRUD) | Schema validation on AST input; auth on write endpoints |
| Roslyn Integration | Stand up the isolated compile-check sandbox from the start — do not add it later |
| Wiring Board | Validate wire `transform` values against a fixed allow-list (no arbitrary transform code) |
| Contract Dashboard | No new attack surface (read-only) — ensure it doesn't leak other users' template data |
| Annotation System | Enforce badge → fixed-code mapping; reject unknown annotation values |
| Template Hub | Add "compiled & verified" gating before public listing; rate limiting on publish |

---

*Companion to `netcn_vision_blueprint.md`, PRD.md, and TRD.md.*
