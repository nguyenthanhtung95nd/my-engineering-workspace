---
name: security-audit
description: >
  Load automatically when reviewing authentication code, authorization logic,
  payment processing, data access endpoints, JWT handling, password management,
  or any code that touches user data, API keys, or sensitive operations.
  Also load when the user mentions security, vulnerability, OWASP, or audit.
context: auto
patterns:
  - "**/*Auth*.cs"
  - "**/*Controller*.cs"
  - "**/*Payment*.cs"
  - "**/*Token*.cs"
  - "**/*Password*.cs"
---

# Security Audit Patterns

## OWASP Top 10 — .NET Quick Reference

### A01 — Broken Access Control (highest frequency in .NET APIs)

The most common issue: fetching a resource by ID without verifying the caller owns it.

```csharp
// ❌ IDOR — any authenticated user can access any order by guessing the ID
[HttpGet("orders/{orderId}")]
public async Task<IActionResult> GetOrder(int orderId)
{
    var order = await _db.Orders.FindAsync(orderId);
    return Ok(order);
}

// ✅ Ownership verified before returning data
[HttpGet("orders/{orderId}")]
public async Task<IActionResult> GetOrder(int orderId)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var order = await _db.Orders
        .AsNoTracking()
        .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

    return order is null ? NotFound() : Ok(order);
}
```

### A02 — Cryptographic Failures

```csharp
// ❌ Weak hashing — MD5 is broken for password storage
var hash = MD5.HashData(Encoding.UTF8.GetBytes(password));

// ❌ Plaintext storage
string stored = password;

// ✅ bcrypt with appropriate work factor (via BCrypt.Net-Next)
string stored = BCrypt.HashPassword(password, workFactor: 12);
bool valid   = BCrypt.Verify(password, stored);
```

### A03 — Injection

```csharp
// ❌ SQL injection via string interpolation
var query = $"SELECT * FROM Users WHERE Email = '{email}'";

// ✅ EF Core — parameterized automatically via expression trees
var user = await _db.Users
    .FirstOrDefaultAsync(u => u.Email == email);

// ✅ Dapper — explicit parameterization
var user = await conn.QueryFirstOrDefaultAsync<User>(
    "SELECT * FROM Users WHERE Email = @Email",
    new { Email = email });
```

### A07 — Authentication Failures

```csharp
// JWT validation — all four checks must be enabled
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,  // verify signature
    ValidateIssuer           = true,  // verify token origin
    ValidateAudience         = true,  // verify intended recipient
    ValidateLifetime         = true,  // reject expired tokens
    ClockSkew = TimeSpan.FromMinutes(1)
};
```

### A09 — Logging Failures

```csharp
// ❌ PII in logs — GDPR violation risk
_logger.LogInformation(
    "User {Email} logged in with password {Password}", email, password);

// ✅ Log security events without sensitive data
_logger.LogInformation(
    "Login attempt for user {UserId} — result: {Result}", userId, success);
```

## Security Review Checklist

Run against every endpoint that handles:

- [ ] Authentication or authorization logic
- [ ] Resource access by ID — check for IDOR
- [ ] User input flowing into a database query — check for injection
- [ ] Passwords or tokens — check hashing and validation
- [ ] File uploads — check for path traversal
- [ ] Outbound HTTP calls with user-controlled URLs — check for SSRF

## Immediate Red Flags

| Pattern in code | Risk |
|-----------------|------|
| `$"...{userId}..."` in SQL string | SQL injection |
| `.FindAsync(id)` with no ownership check | IDOR |
| Password stored or logged as plain text | Cryptographic failure |
| `new HttpClient(userProvidedUrl)` | SSRF |
| `Process.Start(userInput)` | Command injection |
| `[AllowAnonymous]` on sensitive endpoint | Broken access control |
