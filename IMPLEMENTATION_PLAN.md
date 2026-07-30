# netcn — Implementation Plan (Ponytail-Scaled)
**Version:** 1.0 | Date: July 29, 2026
**Method:** Every task below was passed through the Ponytail 7-Rung Ladder before being scheduled. Anything that could be skipped, reused, pattern-copied, or solved with stdlib/a library was cut or replaced — only genuinely new work (netcn's actual differentiator: the wiring/contract engine) gets custom-built time.

Companion to: PRD.md, TRD.md, SECURITY.md.

---

## 0. Ground Rules (apply every single day)

1. **Ladder check before any new file.** Rung 1 → 5 first. If you're about to hand-write something a library/stdlib already does (auth, drag-drop canvas, JSON schema validation, rate limiting) — stop, install instead.
2. **No day ships without:** input validation on new endpoints, error handling, and — since this project compiles untrusted user input — the sandbox boundary must never be weakened "just for now."
3. **Compile-check gate is non-negotiable from Day 6 onward.** No generation feature is "done" until it runs inside the isolated sandbox, not the API host.
4. **100-line rule:** if a single day's custom code exceeds ~150 lines outside of generation-engine logic, you skipped a rung — stop and re-check 2–5.
5. **Cut list (do NOT build custom):** auth system, canvas drag-drop engine, JSON parsing, rate limiter, ZIP packaging, DB ORM, logging. All rungs 4–5 — use the libraries named in each day below.

---

## Phase 1 — Foundation (Days 1–5)
*Goal: AST schema + Template CRUD API. No Roslyn yet, no wiring yet.*

| Day | Task | Ponytail Decision |
|---|---|---|
| 1 | Define AST JSON schema (Controller/Service/Entity node shapes) | Rung 6 — this is netcn's own format, write it directly; validate with a schema library (e.g. `System.Text.Json` + JSON Schema, not hand-rolled validation) |
| 2 | Scaffold ASP.NET Core 8 Web API project + PostgreSQL connection | Rung 5 — `dotnet new webapi`, Entity Framework Core for DB access (don't hand-write SQL/ORM) |
| 3 | `templates` + `users` tables via EF Core migrations | Rung 4/5 — EF Core migrations do this; don't write raw migration SQL |
| 4 | `POST /templates`, `GET /templates`, `GET /templates/{id}` | Rung 7 (real work) — but auth middleware is Rung 5 (ASP.NET Core Identity or Clerk, not custom sessions) |
| 5 | Auth wiring (login/signup) + input validation on template endpoints | Rung 5 — use the chosen auth provider's SDK; use `FluentValidation` (NuGet) for request validation, don't hand-write validators |

**Phase 1 exit check:** Can save/fetch an AST via API with an authenticated user. Nothing generates code yet.

---

## Phase 2 — Roslyn Spike (Days 6–10)
*Goal: prove the riskiest assumption first — one controller, generated and actually compiling, inside a sandbox. Everything else depends on this working.*

| Day | Task | Ponytail Decision |
|---|---|---|
| 6 | Stand up isolated compile sandbox (ephemeral container, no network egress, CPU/time limit) | Rung 5 — use existing container runtime (Docker) for isolation, don't build a custom sandbox from scratch |
| 7 | Roslyn: generate one `Controller` class + `[ApiController]` attribute from AST | Rung 7 (this IS the product — real custom work, no shortcut exists) |
| 8 | Generate `Program.cs` + minimal DI registration from AST | Rung 3 — copy the standard ASP.NET Core `Program.cs` template pattern, parameterize it, don't invent a new bootstrap pattern |
| 9 | Compile-check gate: run `dotnet build` inside sandbox, capture pass/fail | Rung 5 — shell out to the real `dotnet` CLI inside the sandbox; don't reimplement a compiler |
| 10 | Package output as ZIP, return via `/generate/{jobId}` | Rung 4 — `System.IO.Compression` (stdlib), don't write a custom zipper |

**Phase 2 exit check:** One AST → one real downloadable project → `dotnet run` works with zero manual edits. **If this doesn't work reliably, stop and fix before Phase 3 — do not proceed on faith.**

---

## Phase 3 — Wiring Board, Minimal (Days 11–16)
*Goal: prove the contract layer for exactly 2 layers (UI → API). DB layer and Contract Dashboard UI polish are deferred.*

| Day | Task | Ponytail Decision |
|---|---|---|
| 11 | Wiring data model (`wires[]` with from/to/transform/isActive) | Rung 6 — small, direct JSON shape, write it |
| 12 | Backend: `CanMap(sourceType, targetType, transform)` type-check function | Rung 7 (core differentiator, no shortcut) — but the transform allow-list itself is Rung 6 (hardcode a small fixed list: `ParseInt`, `ParseDate`, `ToString`, nothing dynamic) |
| 13 | Canvas: draggable wire-drawing between plug/socket nodes | Rung 5 — React-Flow already supports edge-drawing between node handles; configure it, don't build a canvas line-drawing engine |
| 14 | Wire validation on generation request — block generation on red wires | Rung 7 (real work) |
| 15 | Generate mapper file (`UserMapper.g.cs`) from wire list | Rung 3 — this is a templated code-emission pattern (loop over wires → emit assignment lines), copy the pattern from Day 7's generator, don't design a new emission strategy |
| 16 | End-to-end test: UI field → API field, mismatched type → blocked, matched → mapper generated correctly | Rung 7 — write the one required happy-path + one required failure-path test |

**Phase 3 exit check:** A wrong-type wire is blocked before generation; a correct wire produces a real, correct mapper line in the output ZIP.

---

## Phase 4 — Annotations (Days 17–19)
*Goal: 3 badges only for MVP (not 5) — cut to the ones that prove the pattern.*

| Day | Task | Ponytail Decision |
|---|---|---|
| 17 | `[ApiController]` + `[Authorize]` badge → fixed code/package injection | Rung 3 — copy the standard ASP.NET Core attribute pattern per badge; badge→code mapping is a fixed lookup table, not a dynamic rule engine |
| 18 | `[JWT]` badge → JWT Bearer middleware + package reference injection | Rung 5 — use `Microsoft.AspNetCore.Authentication.JwtBearer` (NuGet), inject its standard setup snippet, don't hand-roll JWT validation |
| 19 | Badge UI in canvas sidebar (palette of 3 badges) | Rung 6 — a static list + click-to-apply, trivial UI |

*(Cache and Audit badges: defer to post-MVP — cut per Rung 1: "does this need to exist yet?" No.)*

---

## Phase 5 — Template Hub, Minimal (Days 20–23)
*Goal: publish + browse + fork. Search relevance, download-count leaderboards, author profile pages — deferred.*

| Day | Task | Ponytail Decision |
|---|---|---|
| 20 | `POST /templates/{id}/fork` | Rung 3 — this is a copy-row-with-new-owner pattern, standard CRUD, no new design needed |
| 21 | Basic browse/search page (list + tag filter) | Rung 5 — use EF Core's built-in `LIKE`/tag filtering; don't build a search engine for MVP |
| 22 | "Compiled & verified" badge — run every published template through Day 9's compile-check before listing | Rung 7 (security-critical, must-build per SECURITY.md §5) |
| 23 | Rate limiting on publish/generate endpoints | Rung 5 — `AspNetCoreRateLimit` (NuGet) or built-in ASP.NET Core rate limiting middleware; don't hand-write a token bucket |

**Phase 5 exit check:** A second user can find, fork, and successfully generate/run someone else's published template.

---

## Deferred (explicitly out of the day-wise plan — do not schedule until above ships and is stable)
- DB layer wiring (API ↔ DB), Contract Dashboard visual polish
- `[Cache]`, `[Audit]` badges
- Docker image output, live-preview URL
- Author profile pages, download-count leaderboards, advanced search

This mirrors PRD §4 (non-goals) and is the direct fix for the over-scoping risk flagged in the earlier review — ship Phases 1–5 (23 days of real work) as a working, honestly-scoped MVP before touching anything on this list.

---

## Security Checkpoints (from SECURITY.md, mapped to days above)
- Day 6: sandbox isolation must exist before Day 7's generator ever runs real user AST.
- Day 12: transform allow-list must stay fixed/hardcoded — never let AST content specify arbitrary transform code (this is the injection vector SECURITY.md §4 warns about).
- Day 22: compile-check gate applied to *every* published template, not just the author's own generation request — this is what SECURITY.md §5 calls the supply-chain control.

---

*Derived from `netcn_vision_blueprint.md`, PRD.md, TRD.md, and SECURITY.md, scaled down via the Ponytail 7-Rung Ladder.*
