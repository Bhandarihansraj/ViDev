# 🏗️ ViDev — Visual Developer for .NET

**Figma for .NET Developers** — Design your architecture visually, generate production-ready C# code.

> Drop nodes on a canvas. Wire them together. Hit generate. Get a real .NET project.

---

## 🎯 What is ViDev?

ViDev is a **visual architecture compiler** where the canvas is the source of truth. You design Controllers, Services, and Entities on an interactive canvas, and ViDev generates a complete, compiling .NET project using **Roslyn**.

## 🚀 Stack

| Layer | Technology |
|---|---|
| **Frontend** | React + TypeScript + React Flow |
| **Backend** | ASP.NET Core 10 Web API |
| **Database** | PostgreSQL (EF Core, JSONB) |
| **Code Gen** | Roslyn SyntaxFactory |
| **Auth** | JWT Bearer + BCrypt |
| **Sandbox** | Podman (process isolation) |
| **Validation** | FluentValidation |

## 📦 Architecture

```
Canvas (React Flow)
    ↓ AST JSON
Backend API (ASP.NET Core)
    ↓ Roslyn SyntaxFactory
Generated .NET Project
    ↓ Compile in Sandbox
Downloadable ZIP
```

## 🛡️ Security

- Annotation allow-list (no arbitrary code injection)
- Name sanitization via regex
- Sandboxed compilation (`--network none`, CPU/memory limits)
- BCrypt password hashing
- JWT with configurable secrets

## 📄 License

MIT — Built by [@Bhandarihansraj](https://github.com/Bhandarihansraj)
