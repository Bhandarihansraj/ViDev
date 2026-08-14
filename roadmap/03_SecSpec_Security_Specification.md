# netcn Phase 2 — Security Specification (SecSpec)
## Version: 2.0 | Date: July 29, 2026 | Author: Bhandarihansraj
## Classification: Internal — Purple Team Reviewed

---

## 1. Security Philosophy

> **"Client-side validation is a myth. The server must never trust the client."**

netcn is designed with **zero-trust architecture** at every layer. The visual wiring board is not just a convenience feature — it is a **security enforcement mechanism** that ensures no data flows from untrusted sources (UI) to trusted destinations (API/DB) without passing through validated security controls.

### Core Principles
1. **Validation by Design** — Security is not bolted on; it is wired into the architecture
2. **Visual Auditability** — Every security control is visible on the contract dashboard
3. **Default Deny** — Unmapped inputs are blocked; wires without validation badges are red
4. **Local Logic, Public Structure** — Business logic and secrets never leave the user's machine
5. **Defense in Depth** — Multiple validation badges can be stacked on a single wire

---

## 2. Threat Model

### 2.1 STRIDE Analysis

| Threat | Component | Mitigation |
|---|---|---|
| **Spoofing** | Template Hub | JWT auth + verified author badges |
| **Tampering** | AST in transit | HTTPS + request signing |
| **Repudiation** | Code generation | Audit logs for every generation event |
| **Information Disclosure** | Business logic | Never uploaded; localStorage only |
| **Denial of Service** | Code generation API | Rate limiting + compilation timeouts |
| **Elevation of Privilege** | Template publishing | Admin approval for premium badges |

### 2.2 Attack Scenarios

#### Scenario A: Burp Suite Bypass
```
Attacker: Intercepts UI → API request with Burp
Action:   Removes client-side validation, sends malicious payload
Defense:  Server-side validation badges execute regardless of client state
Result:   SQL injection blocked at API layer by [SQL Guard] badge
```

#### Scenario B: Malicious Template
```
Attacker: Publishes template with hidden malicious mapper code
Action:   User downloads and runs template
Defense:  Templates only contain AST structure; logic is generated locally
Result:   No executable code in template; user's AI generates clean logic
```

#### Scenario C: Template Hub Breach
```
Attacker: Gains access to template database
Action:   Steals all public templates
Defense:  Templates contain only UI structure + DB schema + API contracts
Result:   Attacker gets skeleton; no business logic, no secrets, no validation rules
```

---

## 3. Validation Badge System

### 3.1 Badge Taxonomy

#### Tier 1: Input Sanitization (Free)
| Badge | Attack Vector | Defense Mechanism | Generated Code Pattern |
|---|---|---|---|
| `[Required]` | Missing field | Null/empty check | `if (string.IsNullOrEmpty(input)) throw;` |
| `[Length]` | Buffer overflow | Hard limit | `if (input.Length > max) throw;` |
| `[Regex]` | Format abuse | Pattern match | `if (!Regex.IsMatch(input, pattern)) throw;` |
| `[Range]` | Integer overflow | Min/max bounds | `if (value < min \|\| value > max) throw;` |
| `[Email]` | Fake/temp mail | MX + regex | `new MailAddress(input).Host` + regex |

#### Tier 2: Security Guards (Pro)
| Badge | Attack Vector | Defense Mechanism | Generated Code Pattern |
|---|---|---|---|
| `[SQL Guard]` | SQL Injection | Parameterized queries + blacklist | `command.Parameters.AddWithValue("@p", input)` |
| `[XSS Shield]` | Cross-site scripting | HTML encoding + CSP | `HtmlEncoder.Default.Encode(input)` |
| `[NoSQL Guard]` | MongoDB injection | Query sanitization | `Builders<BsonDocument>.Filter.Eq("field", sanitized)` |
| `[Path Guard]` | Path traversal | Canonicalization | `Path.GetFullPath(input).StartsWith(baseDir)` |
| `[File Guard]` | Malicious upload | MIME + magic bytes | `!allowedTypes.Contains(file.ContentType)` |

#### Tier 3: Advanced Security (Enterprise)
| Badge | Attack Vector | Defense Mechanism | Generated Code Pattern |
|---|---|---|---|
| `[Rate Limit]` | Brute force | Token bucket | `rateLimiter.AcquireAsync(key, limit)` |
| `[JWT Validate]` | Token tampering | Signature verify | `new JwtSecurityTokenHandler().ValidateToken(...)` |
| `[CSRF Shield]` | Cross-site request | Anti-forgery token | `ValidateAntiForgeryToken` + SameSite cookies |
| `[CORS Lock]` | Cross-origin abuse | Strict origin policy | `policy.WithOrigins(allowed).AllowCredentials()` |
| `[Honeypot]` | Bot detection | Hidden field trap | `if (!string.IsNullOrEmpty(honeypot)) return BadRequest()` |

#### Tier 4: Compliance (Enterprise+)
| Badge | Regulation | Defense Mechanism |
|---|---|---|
| `[GDPR Mask]` | GDPR | PII field encryption at rest |
| `[HIPAA Encrypt]` | HIPAA | AES-256 encryption for health data |
| `[PCI DSS]` | PCI-DSS | Tokenization for payment fields |
| `[Audit Log]` | SOC 2 | Immutable logging for all data access |

### 3.2 Badge Enforcement Rules

```
RULE 1: MANDATORY VALIDATION
─────────────────────────────
IF wire connects UI → API:
    REQUIRE at least one validation badge
    ELSE: Wire is RED, code generation BLOCKED

RULE 2: TYPE MISMATCH = SECURITY RISK
───────────────────────────────────────
IF source_type != target_type AND no transform badge:
    Wire is YELLOW (warning)
    User must approve or add [ParseInt]/[ParseDate] badge

RULE 3: STACKING ALLOWED
────────────────────────
Multiple badges per wire are ENCOURAGED:
    [Required] → [Length: 50] → [SQL Guard] → [XSS Shield]
    Generated code executes in badge order

RULE 4: PREMIUM BADGE AUDIT
────────────────────────────
IF badge is marked [Enterprise]:
    Require admin approval before publishing
    Log all usage for compliance
```

### 3.3 Generated Validation Pipeline

For a wire with badges `[Required] → [Length: 50] → [SQL Guard]`, the generated code is:

```csharp
public static class InputValidators 
{
    public static ValidationResult ValidateUsername(string input)
    {
        // [Required] badge
        if (string.IsNullOrWhiteSpace(input))
            return ValidationResult.Failure("Username is required");

        // [Length: 50] badge
        if (input.Length > 50)
            return ValidationResult.Failure("Username must be under 50 characters");

        // [SQL Guard] badge
        var blacklist = new[] { "'", "--", ";", "DROP", "UNION", "SELECT" };
        if (blacklist.Any(b => input.Contains(b, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Failure("Invalid characters detected");

        // Parameterized query enforcement
        // (Prevents injection even if bypassed above)
        return ValidationResult.Success(input.Trim());
    }
}
```

---

## 4. Zero-Trust Data Flow

### 4.1 Trust Boundaries

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        TRUST BOUNDARY MAP                                    │
│                                                                              │
│  [UNTRUSTED ZONE]              [TRUSTED ZONE]              [RESTRICTED ZONE] │
│                                                                              │
│  ┌─────────────┐              ┌─────────────┐              ┌─────────────┐  │
│  │   Browser   │              │   API       │              │   Database  │  │
│  │   (User)    │───[Wire]────→│   Server    │───[Wire]────→│   (Data)    │  │
│  │             │   [Badges]   │             │   [Badges]   │             │  │
│  └─────────────┘              └─────────────┘              └─────────────┘  │
│        │                            │                            │          │
│        ▼                            ▼                            ▼          │
│   Validation:                   Validation:                   Validation:   │
│   [XSS Shield]                  [SQL Guard]                   [Length]      │
│   [Required]                    [Rate Limit]                  [Type Check]  │
│   [Regex]                       [JWT Validate]                              │
│                                                                              │
│  RULE: Every boundary crossing MUST have at least one validation badge       │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Client-Server Contract

**The server NEVER trusts client-side validation.** The wiring board generates server-side validation code that executes regardless of what the browser sends.

```csharp
// Generated by wiring board — executes SERVER-SIDE
public IActionResult Login([FromBody] LoginRequest request)
{
    // These checks run EVEN if client JavaScript was bypassed
    var usernameResult = InputValidators.ValidateUsername(request.Username);
    if (!usernameResult.IsValid)
        return BadRequest(usernameResult.Errors);

    var passwordResult = InputValidators.ValidatePassword(request.Password);
    if (!passwordResult.IsValid)
        return BadRequest(passwordResult.Errors);

    // Only now proceed to business logic
    return _authService.Login(request);
}
```

---

## 5. Template Hub Security

### 5.1 Data Classification

| Data Type | Storage Location | Encryption | Access Control |
|---|---|---|---|
| **AST Structure** | PostgreSQL JSONB | At-rest (AES-256) | Public or private per template |
| **Business Logic** | User's localStorage | N/A (client-side) | Never leaves browser |
| **Generated ZIPs** | R2/S3 | Presigned URLs (15 min) | Auto-deleted after 24h |
| **User Credentials** | PostgreSQL | bcrypt hashed | JWT session only |
| **Validation Badges** | PostgreSQL | Signed code templates | Admin approval for premium |

### 5.2 Template Isolation

```
Template downloaded by User B:
├── ✅ UI components (public)
├── ✅ DB schema (public)
├── ✅ API contracts (public)
├── ✅ Wiring board connections (public)
├── 🔒 Business logic (NOT included — generated locally)
├── 🔒 Validation rules (NOT included — user's badges apply)
├── 🔒 Secrets/keys (NEVER in template)
└── 🔒 Custom mappers (generated from user's wiring, not template author's)
```

**Result:** Even if a malicious template is downloaded, it is just a skeleton. The dangerous parts (logic, validation, secrets) are generated by the user's own AI or locally configured badges.

---

## 6. API Security

### 6.1 Authentication & Authorization

```yaml
Public Endpoints (No Auth):
  - GET /api/templates
  - GET /api/templates/{slug}
  - GET /api/badges

Authenticated Endpoints (JWT):
  - POST /api/templates
  - POST /api/templates/{slug}/fork
  - POST /api/projects/generate
  - GET /api/user/projects

Admin Endpoints (Role-based):
  - POST /api/admin/badges
  - PUT /api/admin/templates/{id}/approve
  - GET /api/admin/audit-logs
```

### 6.2 Rate Limiting

| Endpoint | Limit | Window |
|---|---|---|
| `POST /api/projects/generate` | 10 | per hour |
| `POST /api/templates` | 5 | per hour |
| `GET /api/templates` | 100 | per minute |
| `POST /api/templates/{slug}/fork` | 20 | per hour |

### 6.3 Request Validation

All API requests pass through:
1. **Schema validation** (JSON Schema)
2. **Size limits** (AST max 10MB)
3. **Depth limits** (max 50 nested nodes)
4. **Sanitization** (strip HTML from text fields)

---

## 7. Code Generation Security

### 7.1 Roslyn Sandbox

```csharp
// Compilation runs in restricted context
var compilation = CSharpCompilation.Create("GeneratedApp")
    .WithOptions(new CSharpCompilationOptions(
        OutputKind.ConsoleApplication,
        allowUnsafe: false,           // ❌ No unsafe code
        checkOverflow: true,          // ✅ Overflow checks
        optimizationLevel: OptimizationLevel.Release
    ));

// Whitelist allowed assemblies only
var allowedAssemblies = new[] 
{
    typeof(object).Assembly,           // mscorlib
    typeof(ControllerBase).Assembly,   // ASP.NET Core
    typeof(HttpClient).Assembly        // System.Net.Http
};
```

### 7.2 Output Sanitization

Before returning ZIP:
- Scan for hardcoded secrets (regex for `password=`, `key=`, `token=`)
- Flag suspicious patterns (`eval(`, `Process.Start`, `File.Delete`)
- Warn user if detected (don't block — user may have intentionally added logic)

---

## 8. Compliance & Audit

### 8.1 Audit Events

Every action is logged:
```json
{
  "event": "template_generated",
  "user_id": "uuid",
  "template_id": "uuid",
  "timestamp": "2026-07-29T10:00:00Z",
  "ip": "xxx.xxx.xxx.xxx",
  "validation_badges_used": ["SQLGuard", "XSSShield"],
  "compilation_success": true,
  "diagnostics_count": 0
}
```

### 8.2 Compliance Badges

For enterprise customers, the contract dashboard exports:
- **SOC 2:** All validation controls applied to each data flow
- **GDPR:** PII field mapping and encryption status
- **PCI DSS:** Payment data flow isolation proof

---

## 9. Incident Response

| Severity | Scenario | Response |
|---|---|---|
| **Critical** | Template hub breach | Rotate all presigned URLs, notify users, freeze publishing |
| **High** | Malicious badge discovered | Revoke badge, regenerate all affected projects, audit logs |
| **Medium** | Rate limit bypass | Temporarily lower limits, investigate IP patterns |
| **Low** | False positive in validation | Update badge template, notify affected users |

---

*End of SecSpec v2.0 | Reviewed by Purple Team Principles*
