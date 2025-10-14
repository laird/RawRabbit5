# Security Quick Reference Card

**Date**: October 13, 2025
**Status**: 🔴 MIGRATION BLOCKED

---

## 🚨 CRITICAL - Action Required Now

### Newtonsoft.Json CVE-2024-21907

```xml
<!-- BEFORE (VULNERABLE) -->
<PackageReference Include="Newtonsoft.Json" Version="10.0.1" />

<!-- AFTER (SECURE) -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

**File**: `src/RawRabbit/RawRabbit.csproj` (line 22)
**Time**: 30 minutes
**Testing**: Run test suite
**Impact**: Prevents DoS attacks on ALL 28 projects

---

## 📊 Security Score: 40/100

### Vulnerabilities by Severity

| Severity | Count | Status |
|----------|-------|--------|
| 🔴 CRITICAL | 1 | BLOCKING |
| 🟠 HIGH | 2 | WITH MIGRATION |
| 🟡 MEDIUM | 1 | POST MIGRATION |
| 🟢 LOW | 2 | FUTURE |

---

## 🎯 Quick Fix Checklist

### Phase 0: Unblock Migration (2 hours)

```bash
# 1. Edit RawRabbit.csproj
vim src/RawRabbit/RawRabbit.csproj
# Change line 22: Version="10.0.1" → Version="13.0.4"

# 2. Restore packages
~/.dotnet/dotnet restore

# 3. Build
~/.dotnet/dotnet build RawRabbit.sln --configuration Release

# 4. Test
~/.dotnet/dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj

# 5. Verify no vulnerabilities
~/.dotnet/dotnet list package --vulnerable
```

### Phase 1: With Migration (included)

- ✅ Migrate to .NET 9.0 → Resolves 2 HIGH CVEs
- ✅ Upgrade RabbitMQ.Client 5.0.1 → 7.1.2

---

## 🔍 Scan Commands

```bash
# Check for vulnerabilities
~/.dotnet/dotnet list package --vulnerable --include-transitive

# Check for outdated packages
~/.dotnet/dotnet list package --outdated

# Quick health check (add to CI/CD)
~/.dotnet/dotnet list package --vulnerable | grep -E "(Critical|High)" && echo "FAIL" || echo "PASS"
```

---

## 📋 CVE Summary

### CVE-2024-21907 (CRITICAL)
- **Package**: Newtonsoft.Json 10.0.1
- **Type**: Denial of Service
- **Vector**: Deeply nested JSON
- **Fix**: Upgrade to 13.0.4

### CVE-2018-8292 (HIGH)
- **Package**: System.Net.Http 4.3.0 (transitive)
- **Type**: Information Disclosure
- **Vector**: HTTP redirect credential leak
- **Fix**: Migrate to .NET 9.0

### CVE-2019-0820 (HIGH)
- **Package**: System.Text.RegularExpressions 4.3.0 (transitive)
- **Type**: Denial of Service
- **Vector**: Malicious regex patterns
- **Fix**: Migrate to .NET 9.0

---

## 🛡️ Defense in Depth

After fixing Newtonsoft.Json, add this to serialization config:

```csharp
var settings = new JsonSerializerSettings
{
    MaxDepth = 64, // Prevent deep nesting attacks
    // ... other settings
};
```

---

## 📈 Migration Impact

### Before Migration
- 🔴 Security Score: 40/100
- ⛔ 1 CRITICAL, 2 HIGH CVEs
- 🕸️ Legacy frameworks (net451, netstandard1.5)

### After Phase 0 (Newtonsoft.Json fix)
- 🟡 Security Score: 60/100
- ✅ 0 CRITICAL CVEs
- ⚠️ 2 HIGH CVEs remain

### After Phase 1 (Migration complete)
- 🟢 Security Score: 85/100
- ✅ 0 CRITICAL, 0 HIGH CVEs
- ✅ Modern framework (net9.0)

---

## 🚦 Status Gates

### ⛔ BLOCKED: Cannot Proceed
- Current state with Newtonsoft.Json 10.0.1

### 🟡 CAUTION: Can Proceed with Risk
- After Newtonsoft.Json 13.0.4 upgrade
- Still have 2 HIGH transitive CVEs

### ✅ CLEAR: Safe to Deploy
- After .NET 9.0 migration complete
- All CRITICAL and HIGH CVEs resolved

---

## 📞 Quick Links

- **Full Assessment**: `docs/security-assessment.md`
- **Summary**: `docs/SECURITY-ASSESSMENT-SUMMARY.md`
- **This Card**: `docs/SECURITY-QUICK-REFERENCE.md`

---

## 🎯 Decision Matrix

### Should I proceed with migration?

| Scenario | Answer |
|----------|--------|
| Newtonsoft.Json 10.0.1 | ❌ NO - Fix P0 first |
| Newtonsoft.Json 13.0.4 | ✅ YES - Proceed with migration |
| After .NET 9.0 migration | ✅ YES - Safe to deploy |

---

## ⏱️ Time Estimates

| Phase | Duration | Blocker |
|-------|----------|---------|
| Phase 0: Newtonsoft.Json fix | 2 hours | YES |
| Phase 1: .NET 9.0 + RabbitMQ.Client | 3-5 days | NO |
| Phase 2: Dependency updates | 2-3 days | NO |
| Phase 3: Security hardening | 1-2 days | NO |

**Total**: 7-12 days for 100% remediation

---

**Last Updated**: October 13, 2025
**Next Review**: After Phase 0 completion
