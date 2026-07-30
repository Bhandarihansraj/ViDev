# PROJECT_INDEX — ViDev (Visual Developer)

## Meta
- Project: ViDev
- Type: React SPA + ASP.NET Backend (Visual Architecture Platform)
- Stack: React, TypeScript, React-Flow, Vite
- Last Updated: 2026-07-29

## Module Map
| File | Role | Exports | Status | Agents Run |
|------|------|---------|--------|------------|
| src/App.tsx | Main Canvas UI | App | ✅ Draft | FORGE, REVIEW |
| src/components/Sidebar.tsx | Drag/Drop Sidebar | Sidebar | ✅ Draft | FORGE |
| src/components/ArchitectureNode.tsx | React Flow Custom Node | ArchitectureNode | ✅ Draft | FORGE, REVIEW |
| src/types/ast.ts | AST Type Definitions | Types | ✅ Draft | FORGE |

## Dependency Graph
App.tsx → Sidebar.tsx, ArchitectureNode.tsx, ast.ts

## Open Issues
- [ ] Components need to be built step-by-step and fully functional.

## Architecture Decisions
- [ADR-001] Using Vite + React-Flow for the AST Canvas engine.
- [ADR-002] Project Forge pipeline enforced for all component creation.

## Context Notes (for next chat)
- Implementing Phase 1 of ViDev (AST Canvas).
