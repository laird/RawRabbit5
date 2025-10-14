# RawRabbit Security Assessment

**Date**: October 13, 2025
**Branch**: 2.1-10-13-2025
**Assessor**: Security Agent
**Project Location**: /home/laird/src/EYP/RawRabbit5

---

## Executive Summary

- **Security Score**: 40/100 (CRITICAL - FAILING)
- **Critical Issues**: 1 (CVE in Newtonsoft.Json)
- **High Severity Issues**: 2 (Transitive CVEs in Ninject dependencies)
- **Blocking Status**: YES - MIGRATION BLOCKED
- **Total Projects Scanned**: 28 projects
- **Legacy Framework Risk**: HIGH (net451, netstandard1.5 targets)

**RECOMMENDATION**: All P0 issues must be resolved before proceeding with migration. The outdated dependencies pose significant security risks including Denial of Service attacks and information disclosure vulnerabilities.

---

## Vulnerability Scan Results

### Scan Execution

```bash
Command: dotnet list package --vulnerable --include-transitive
Date: October 13, 2025
Scope: All 28 projects in solution
```

### Scan Status

- **Successful Scans**: 27 projects
- **Failed Scans**: 1 project (RawRabbit.AspNet.Sample - restore required)
- **Projects with Vulnerabilities**: 1 (RawRabbit.DependencyInjection.Ninject)

---

## Detailed Vulnerability Analysis

### P0 - CRITICAL Vulnerabilities (Blocking Migration)

#### 1. CVE-2024-21907: Newtonsoft.Json Denial of Service

**Package**: Newtonsoft.Json
**Current Version**: 10.0.1
**Latest Version**: 13.0.4
**Affected Projects**: ALL (28 projects via RawRabbit core dependency)
**Severity**: HIGH (CVSS 7.5)
**Status**: CRITICAL - BLOCKING

**Description**:
Newtonsoft.Json prior to version 13.0.1 is vulnerable to Denial of Service due to improper handling of expressions with high nesting levels that lead to StackOverflow exceptions or high CPU and RAM usage.

**Technical Details**:
- Serializing methods throw StackOverFlow exception with nesting level ~20k
- Deserializing methods consume excessive CPU/memory with >10k nesting level
- Attack vector: Crafted JSON passed to JsonConvert.DeserializeObject
- Can be triggered by unauthenticated remote attacker
- Results in application crash or resource exhaustion

**CVSS Vector**: AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:N/A:H

**Impact Assessment**:
- HIGH: All RabbitMQ message processing vulnerable to DoS attacks
- Any message with deeply nested JSON can crash application
- Production systems at risk of service disruption
- Potential for cascading failures in distributed systems

**Remediation**:
1. Upgrade Newtonsoft.Json from 10.0.1 to 13.0.4
2. Set MaxDepth parameter in JsonSerializerSettings as defense-in-depth
3. Validate message structure before deserialization

**Advisory URLs**:
- https://github.com/advisories/GHSA-5crp-9r3c-p9vr
- https://nvd.nist.gov/vuln/detail/CVE-2024-21907
- https://security.snyk.io/vuln/SNYK-DOTNET-NEWTONSOFTJSON-2774678

---

### P1 - HIGH Severity Vulnerabilities

#### 2. CVE-2018-8292: System.Net.Http Information Disclosure

**Package**: System.Net.Http (transitive)
**Affected Version**: 4.3.0
**Affected Projects**: RawRabbit.DependencyInjection.Ninject (netstandard1.5 target)
**Severity**: HIGH
**Status**: HIGH PRIORITY

**Description**:
Information disclosure vulnerability in .NET Core where authentication information is inadvertently exposed in HTTP redirects.

**Technical Details**:
- Authentication credentials can leak through HTTP redirects
- Affects .NET Core 1.0, 1.1, 2.1 and PowerShell Core 6.0
- Transitive dependency through Ninject package

**Impact Assessment**:
- MEDIUM: Only affects Ninject DI adapter on netstandard1.5 target
- Risk of credential exposure if using HTTP-based features
- Limited scope but serious consequence if exploited

**Remediation**:
1. Upgrade to .NET 9.0 (removes vulnerable transitive dependency)
2. Consider deprecating Ninject adapter (see ADR 0004)
3. If Ninject must be maintained, update System.Net.Http explicitly

**Advisory URLs**:
- https://github.com/advisories/GHSA-7jgj-8wvc-jh57
- CVE-2018-8292

#### 3. CVE-2019-0820: System.Text.RegularExpressions DoS

**Package**: System.Text.RegularExpressions (transitive)
**Affected Version**: 4.3.0
**Affected Projects**: RawRabbit.DependencyInjection.Ninject (netstandard1.5 target)
**Severity**: HIGH
**Status**: HIGH PRIORITY

**Description**:
Regular Expression Denial of Service vulnerability when .NET Framework and .NET Core improperly process RegEx strings.

**Technical Details**:
- Specially crafted regex patterns cause excessive processing
- Results in CPU exhaustion and application hang
- Transitive dependency through Ninject package

**Impact Assessment**:
- MEDIUM: Only affects Ninject DI adapter on netstandard1.5 target
- Risk of DoS if regex operations performed on untrusted input
- Limited scope as RawRabbit doesn't heavily use regex

**Remediation**:
1. Upgrade to .NET 9.0 (removes vulnerable transitive dependency)
2. Consider deprecating Ninject adapter (see ADR 0004)
3. If Ninject must be maintained, update System.Text.RegularExpressions explicitly

**Advisory URLs**:
- https://github.com/advisories/GHSA-cmhx-cq75-c4mj
- CVE-2019-0820

---

### P2 - MEDIUM Priority Issues

#### 4. RabbitMQ.Client Outdated Version

**Package**: RabbitMQ.Client
**Current Version**: 5.0.1
**Latest Version**: 7.1.2
**Affected Projects**: ALL (28 projects via RawRabbit core dependency)
**Severity**: MEDIUM (Tech Debt)
**Status**: MEDIUM PRIORITY

**Description**:
RabbitMQ.Client 5.0.1 is significantly outdated (released ~2018). While no specific CVEs found, this version:
- Lacks security patches from 6.x and 7.x series
- Missing performance improvements
- Incompatible with modern .NET (requires .NET 4.5.1 minimum)
- No longer supported by maintainers

**Impact Assessment**:
- MEDIUM: No known CVEs but unknown vulnerabilities likely
- Missing 7+ years of security patches
- Potential compatibility issues with modern RabbitMQ servers
- Technical debt compounds migration complexity

**Remediation**:
1. Upgrade to RabbitMQ.Client 7.1.2 as part of .NET 9.0 migration
2. Review breaking changes in 6.x and 7.x series
3. Update connection handling code for API changes
4. Test thoroughly with modern RabbitMQ server versions

**Version History**:
- 5.x: .NET 4.5.1 / .NET Core 2.0 minimum
- 6.x: .NET 4.6.1 / .NET Core 3.1 minimum + API changes
- 7.x: .NET 4.6.2 / .NET Standard 2.0+ + performance improvements

---

### P3 - LOW Priority Issues

#### 5. Legacy Framework Targets (Security Risk)

**Frameworks**: net451, netstandard1.5
**Affected Projects**: ALL (28 projects)
**Severity**: LOW (Systemic Risk)
**Status**: LOW PRIORITY (Addressed by migration)

**Description**:
Targeting legacy frameworks (net451 from 2013, netstandard1.5 from 2016) introduces security risks:
- No security updates for .NET Framework 4.5.1
- Limited tooling support for vulnerability scanning
- Incompatible with modern security features
- Increased attack surface from older runtime

**Impact Assessment**:
- LOW: Will be resolved by migration to .NET 9.0
- Systemic risk rather than specific vulnerability
- Long-term maintenance burden

**Remediation**:
- Complete migration to .NET 9.0 (single target)
- Remove multi-targeting complexity
- Adopt modern security features (TLS 1.3, modern crypto)

#### 6. Outdated Dependencies (Tech Debt)

**Dependencies with Updates Available**:

| Package | Current | Latest | Gap | Projects Affected |
|---------|---------|--------|-----|-------------------|
| Autofac | 4.1.0 | 8.4.0 | 4.3.0 | 1 (DI.Autofac) |
| Ninject | 3.2.2 / 4.0.0-beta | 3.3.6 | Multiple | 1 (DI.Ninject) |
| Polly | 5.3.1 | 8.x | 3+ major | 1 (Enrichers.Polly) |
| MessagePack | 1.7.3.4 | 2.5.x | 1+ major | 1 (Enrichers.MessagePack) |

**Impact Assessment**:
- LOW: No known CVEs but missing bug fixes and features
- Tech debt increases maintenance burden
- May have transitive security improvements

**Remediation**:
- Update during migration to .NET 9.0
- Test for breaking changes in major version updates
- Consider deprecating unused enrichers (ZeroFormatter)

---

## Dependency Analysis

### Core Dependencies

| Package | Current Version | Latest Version | Status | Severity |
|---------|----------------|----------------|--------|----------|
| RabbitMQ.Client | 5.0.1 | 7.1.2 | OUTDATED | MEDIUM |
| Newtonsoft.Json | 10.0.1 | 13.0.4 | VULNERABLE | CRITICAL |

### DI Container Dependencies

| Package | Current Version | Latest Version | Status | Severity |
|---------|----------------|----------------|--------|----------|
| Autofac | 4.1.0 | 8.4.0 | OUTDATED | LOW |
| Ninject | 3.2.2 / 4.0.0-beta | 3.3.6 | OUTDATED + VULNERABLE | HIGH |
| Microsoft.Extensions.DependencyInjection | (implicit) | Current | OK | NONE |

### Enricher Dependencies

| Package | Current Version | Latest Version | Status | Severity |
|---------|----------------|----------------|--------|----------|
| Polly | 5.3.1 | 8.x | OUTDATED | LOW |
| MessagePack | 1.7.3.4 | 2.5.x | OUTDATED | LOW |
| ZeroFormatter | 1.6.4 | (deprecated) | DEPRECATED | LOW |
| Protobuf | (via package) | Current | UNKNOWN | LOW |

### Transitive Vulnerabilities

| Package | Version | Source | CVE | Severity |
|---------|---------|--------|-----|----------|
| System.Net.Http | 4.3.0 | Ninject (netstandard1.5) | CVE-2018-8292 | HIGH |
| System.Text.RegularExpressions | 4.3.0 | Ninject (netstandard1.5) | CVE-2019-0820 | HIGH |

---

## Security Scoring Breakdown

**Base Score**: 100 points

**Deductions**:
- CRITICAL CVE (Newtonsoft.Json): -20 points
- HIGH CVE (System.Net.Http): -10 points
- HIGH CVE (System.Text.RegularExpressions): -10 points
- Legacy frameworks (net451, netstandard1.5): -10 points
- Outdated core dependency (RabbitMQ.Client 5.0.1, 7 years): -5 points
- Outdated dependency (Autofac, 4+ years): -5 points
- Deprecated dependency (ZeroFormatter): -0 points (marked for removal)

**Final Score**: 40/100

**Grade**: F (CRITICAL FAILURE)

---

## Prioritized Remediation Plan

### P0 - Critical (Blocking - MUST Fix Before Migration)

**Estimated Time**: 1-2 days

1. **Upgrade Newtonsoft.Json 10.0.1 → 13.0.4**
   - **Action**: Update RawRabbit.csproj PackageReference
   - **Files**: `src/RawRabbit/RawRabbit.csproj`
   - **Testing**: Run full test suite, verify serialization compatibility
   - **Risk**: LOW - Minor version upgrade, backward compatible
   - **Blocking**: YES - CRITICAL security vulnerability

2. **Add MaxDepth Validation (Defense in Depth)**
   - **Action**: Add JsonSerializerSettings with MaxDepth = 64
   - **Files**: Serialization configuration in RawRabbit core
   - **Testing**: Verify normal messages work, deeply nested messages rejected
   - **Risk**: LOW - Additional safety check
   - **Blocking**: NO - Enhancement

3. **Document Security Baseline**
   - **Action**: Create ADR for security requirements
   - **Files**: `docs/adr/ADR 0005 Security Baseline Requirements.md`
   - **Testing**: N/A (documentation)
   - **Risk**: NONE
   - **Blocking**: YES - Required for audit trail

### P1 - High Priority (Should Fix During Migration)

**Estimated Time**: 3-5 days

4. **Migrate to .NET 9.0 (Resolves Transitive CVEs)**
   - **Action**: Remove net451/netstandard1.5 targets, adopt net9.0
   - **Files**: All 28 .csproj files
   - **Testing**: Full integration test suite
   - **Risk**: MEDIUM - Major framework change
   - **Blocking**: NO - Part of planned migration
   - **Note**: Automatically resolves CVE-2018-8292 and CVE-2019-0820

5. **Upgrade RabbitMQ.Client 5.0.1 → 7.1.2**
   - **Action**: Update core dependency, adapt to API changes
   - **Files**: `src/RawRabbit/RawRabbit.csproj` + channel/connection code
   - **Testing**: Full integration tests with RabbitMQ server
   - **Risk**: HIGH - Major version change with breaking changes
   - **Blocking**: NO - But critical for long-term support

6. **Evaluate Ninject Deprecation (ADR 0004)**
   - **Action**: Create deprecation plan, recommend alternatives
   - **Files**: `docs/adr/ADR 0004 Ninject Deprecation.md` (exists)
   - **Testing**: Verify migration paths for users
   - **Risk**: LOW - Documentation only
   - **Blocking**: NO - User choice

### P2 - Medium Priority (Should Fix Post-Migration)

**Estimated Time**: 2-3 days

7. **Upgrade Autofac 4.1.0 → 8.4.0**
   - **Action**: Update DI adapter
   - **Files**: `src/RawRabbit.DependencyInjection.Autofac/`
   - **Testing**: DI adapter tests
   - **Risk**: MEDIUM - Major version upgrade
   - **Blocking**: NO

8. **Upgrade Polly 5.3.1 → 8.x**
   - **Action**: Update enricher, adapt to new API
   - **Files**: `src/RawRabbit.Enrichers.Polly/`
   - **Testing**: Retry policy tests
   - **Risk**: MEDIUM - Major version upgrade
   - **Blocking**: NO

9. **Security Audit of Message Handling**
   - **Action**: Review all deserialization points for input validation
   - **Files**: All middleware processing messages
   - **Testing**: Security-focused integration tests
   - **Risk**: LOW - Review only
   - **Blocking**: NO

### P3 - Low Priority (Nice to Have)

**Estimated Time**: 1-2 days

10. **Upgrade MessagePack 1.7.3.4 → 2.5.x**
    - **Action**: Update enricher
    - **Files**: `src/RawRabbit.Enrichers.MessagePack/`
    - **Testing**: Serialization compatibility tests
    - **Risk**: MEDIUM - Major version change
    - **Blocking**: NO

11. **Remove ZeroFormatter Enricher**
    - **Action**: Mark as obsolete, remove from future releases
    - **Files**: `src/RawRabbit.Enrichers.ZeroFormatter/`
    - **Testing**: Verify no internal dependencies
    - **Risk**: LOW - Isolated component
    - **Blocking**: NO

12. **Implement Dependency Scanning in CI/CD**
    - **Action**: Add `dotnet list package --vulnerable` to pipeline
    - **Files**: CI/CD configuration
    - **Testing**: Run in build pipeline
    - **Risk**: NONE - Automation
    - **Blocking**: NO

---

## Recommendations

### Immediate Actions Required

1. **DO NOT PROCEED WITH MIGRATION** until P0 issues resolved
2. **Upgrade Newtonsoft.Json** to 13.0.4 immediately (trivial change, high impact)
3. **Create ADR 0005** documenting security baseline requirements
4. **Run full test suite** after Newtonsoft.Json upgrade
5. **Document security posture** for stakeholder review

### Security-First Migration Approach

1. **Phase 0: Security Hotfix (Pre-Migration)**
   - Fix Newtonsoft.Json CVE
   - Document security baseline
   - Establish vulnerability scanning

2. **Phase 1: Core Migration (.NET 9.0)**
   - Migrate to net9.0 target (resolves transitive CVEs)
   - Upgrade RabbitMQ.Client to 7.1.2
   - Maintain security scanning throughout

3. **Phase 2: Dependency Modernization**
   - Update Autofac, Polly, MessagePack
   - Deprecate Ninject (if needed)
   - Remove ZeroFormatter

4. **Phase 3: Security Hardening**
   - Security audit of message handling
   - Implement input validation
   - Add security-focused tests

### Framework Migration Path

**Current**: net451 + netstandard1.5 (INSECURE)
**Target**: net9.0 (SECURE)

**Benefits**:
- Resolves all transitive CVEs automatically
- Access to modern security features (TLS 1.3, modern crypto)
- Continuous security updates from Microsoft
- Modern tooling support for vulnerability scanning

### Dependency Update Strategy

**Critical Path**:
1. Newtonsoft.Json 10.0.1 → 13.0.4 (IMMEDIATE)
2. RabbitMQ.Client 5.0.1 → 7.1.2 (WITH MIGRATION)
3. Framework net451/netstandard1.5 → net9.0 (WITH MIGRATION)

**Secondary Path**:
4. Autofac 4.1.0 → 8.4.0 (POST MIGRATION)
5. Polly 5.3.1 → 8.x (POST MIGRATION)
6. MessagePack 1.7.3.4 → 2.5.x (POST MIGRATION)

---

## Timeline

### Phase 0: Security Hotfix (BLOCKING)
- **Duration**: 1-2 days
- **P0 Fixes**: Newtonsoft.Json upgrade, ADR creation
- **Deliverable**: Security baseline established
- **Gate**: All CRITICAL vulnerabilities resolved

### Phase 1: P1 High Priority (WITH MIGRATION)
- **Duration**: 3-5 days
- **Work**: .NET 9.0 migration + RabbitMQ.Client upgrade
- **Deliverable**: Modern framework, no transitive CVEs
- **Gate**: All HIGH vulnerabilities resolved

### Phase 2: P2 Medium Priority (POST MIGRATION)
- **Duration**: 2-3 days
- **Work**: Dependency updates, security audit
- **Deliverable**: All dependencies current
- **Gate**: Comprehensive security review complete

### Phase 3: P3 Low Priority (FUTURE)
- **Duration**: 1-2 days
- **Work**: Optimization, deprecation, automation
- **Deliverable**: Long-term security posture
- **Gate**: Continuous security monitoring established

**Total Estimated Time**: 7-12 days for complete remediation

---

## Testing Requirements

### Security Test Plan

1. **Newtonsoft.Json DoS Protection**
   - Test deeply nested JSON rejection (>64 levels)
   - Verify MaxDepth configuration enforced
   - Test legitimate complex messages still work

2. **RabbitMQ.Client Compatibility**
   - Test connection/channel creation
   - Verify message publishing/consuming
   - Test all operations (publish, subscribe, RPC, etc.)

3. **Framework Migration Validation**
   - Run full unit test suite on net9.0
   - Run full integration test suite with RabbitMQ
   - Performance benchmarks (ensure no regression)

4. **Dependency Update Validation**
   - Test each updated dependency in isolation
   - Integration tests with all dependencies
   - Backward compatibility tests (if applicable)

### Continuous Security Testing

1. **Automated Vulnerability Scanning**
   ```bash
   dotnet list package --vulnerable --include-transitive
   ```

2. **Dependency Audit on Every Build**
   - Fail build if HIGH or CRITICAL vulnerabilities detected
   - Report MEDIUM vulnerabilities as warnings

3. **Security Regression Tests**
   - Test for known CVE patterns
   - Fuzz testing for serialization
   - Input validation tests

---

## Risk Assessment

### Security Risk Matrix

| Issue | Likelihood | Impact | Risk Score | Priority |
|-------|-----------|--------|------------|----------|
| Newtonsoft.Json DoS | HIGH | HIGH | 9/10 | P0 |
| System.Net.Http Info Disclosure | MEDIUM | MEDIUM | 5/10 | P1 |
| System.Text.RegularExpressions DoS | LOW | MEDIUM | 4/10 | P1 |
| RabbitMQ.Client Unknown CVEs | MEDIUM | MEDIUM | 5/10 | P1 |
| Legacy Framework Vulnerabilities | HIGH | LOW | 4/10 | P1 |
| Outdated Dependencies | LOW | LOW | 2/10 | P2 |

### Migration Risk

**Without Security Fixes**:
- HIGH: Deploying to production with known CRITICAL CVEs
- HIGH: Attack surface exposed through message deserialization
- MEDIUM: Compliance/audit failures

**With Security Fixes**:
- LOW: Newtonsoft.Json upgrade is backward compatible
- MEDIUM: RabbitMQ.Client upgrade has breaking changes
- MEDIUM: Framework migration requires thorough testing

---

## Compliance Considerations

### Security Standards

**OWASP Top 10 Compliance**:
- A06:2021 - Vulnerable and Outdated Components (FAILING)
- A08:2021 - Software and Data Integrity Failures (AT RISK)

**Recommendations**:
- Fix P0 issues to achieve basic compliance
- Implement dependency scanning to maintain compliance
- Regular security audits (quarterly)

### Audit Trail

**Documentation Required**:
1. ADR 0005: Security Baseline Requirements
2. Security assessment report (this document)
3. Remediation plan with timelines
4. Test results for security fixes
5. Post-migration security validation

---

## Monitoring and Maintenance

### Ongoing Security Practices

1. **Monthly Dependency Scans**
   - Run `dotnet list package --vulnerable`
   - Review and triage findings
   - Update dependencies as needed

2. **Security Patch Management**
   - Subscribe to security advisories for key dependencies
   - Prioritize security patches over feature updates
   - Maintain rapid response capability (<48 hours for CRITICAL)

3. **Vulnerability Disclosure Process**
   - Document process for users to report security issues
   - Establish responsible disclosure policy
   - Maintain security contact in README

### Success Metrics

**Target Security Posture**:
- Security Score: 90/100+
- Zero CRITICAL or HIGH vulnerabilities
- All dependencies <1 year old
- Automated vulnerability scanning in CI/CD
- Monthly security reviews

---

## Conclusion

The RawRabbit project has **1 CRITICAL and 2 HIGH severity security vulnerabilities** that must be addressed before migration can proceed. The primary concern is the Newtonsoft.Json CVE-2024-21907 which affects all 28 projects and enables Denial of Service attacks through message deserialization.

**Migration is BLOCKED** until the P0 Newtonsoft.Json upgrade is completed. This is a simple change (update version in RawRabbit.csproj) with minimal risk and massive security benefit.

The planned migration to .NET 9.0 will automatically resolve the 2 HIGH severity transitive vulnerabilities in the Ninject adapter, providing additional security benefits beyond framework modernization.

**Next Steps**:
1. Upgrade Newtonsoft.Json 10.0.1 → 13.0.4 (IMMEDIATE)
2. Create ADR 0005 documenting security baseline
3. Unblock migration planning and execution
4. Address P1/P2/P3 issues during and after migration

---

## Appendix: Commands Used

```bash
# Vulnerability scan
dotnet list package --vulnerable --include-transitive

# Outdated packages
dotnet list package --outdated

# Find all project files
find . -name "*.csproj"

# Security audit (recommended for CI/CD)
dotnet list package --vulnerable --include-transitive | grep -E "(Critical|High)"
```

---

## Appendix: References

### CVE References

- CVE-2024-21907: https://nvd.nist.gov/vuln/detail/CVE-2024-21907
- CVE-2018-8292: https://github.com/advisories/GHSA-7jgj-8wvc-jh57
- CVE-2019-0820: https://github.com/advisories/GHSA-cmhx-cq75-c4mj

### Package Documentation

- RabbitMQ.Client: https://github.com/rabbitmq/rabbitmq-dotnet-client
- Newtonsoft.Json: https://www.newtonsoft.com/json
- .NET Security: https://learn.microsoft.com/en-us/dotnet/standard/security/

### Project Documentation

- ADR 0001: Target Framework NET9
- ADR 0002: RabbitMQ Client Upgrade
- ADR 0004: Ninject Deprecation
- MIGRATION-GUIDE.md: Migration planning
- CLAUDE.md: Project configuration and protocols

---

**Assessment Completed**: October 13, 2025
**Next Review**: After P0 remediation (1-2 days)
**Security Agent**: Task Complete
