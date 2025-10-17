# Generic .NET Migration Planning Guide

**Version**: 1.0
**Purpose**: Universal planning framework for .NET framework migrations
**Applicability**: Any .NET project migrating between framework versions

---

## Overview

This guide provides a systematic approach to planning .NET framework migrations, from analysis through execution and validation.

**Target Scenarios**:
- .NET Framework → .NET Core/.NET 5+
- .NET Standard → .NET 6+
- Legacy .NET → Modern .NET (6, 8, 9+)
- .NET Core 3.1 → .NET 8/9
- Cross-framework consolidation

---

## Planning Phases

### Phase 1: Discovery & Assessment

#### 1.1 Current State Analysis

**Project Inventory**:
```bash
# List all projects in solution
dotnet sln list

# Identify target frameworks
grep -r "<TargetFramework" . --include="*.csproj"

# Count projects by framework
grep -r "<TargetFramework" . --include="*.csproj" | cut -d'>' -f2 | cut -d'<' -f1 | sort | uniq -c
```

**Document**:
- Total project count
- Target framework distribution
- Multi-targeting scenarios
- Build output types (library, console, web, test)
- Test coverage

**Template**:
```markdown
## Current State

### Projects
- Total: X projects
- Libraries: Y
- Executables: Z
- Tests: N

### Target Frameworks
- netstandard1.5: X projects
- netstandard2.0: Y projects
- net451: Z projects
- net462: N projects
- netcoreapp2.0: M projects
```

---

#### 1.2 Dependency Analysis

**Analyze NuGet Dependencies**:
```bash
# List all packages
dotnet list package --include-transitive > packages.txt

# Check for outdated packages
dotnet list package --outdated

# Identify packages with compatibility issues
dotnet list package --vulnerable
```

**Key Questions**:
- Which packages need updates?
- Are there deprecated packages?
- Do packages support target framework?
- Are there security vulnerabilities?

**Document**:
```markdown
## Dependency Analysis

### Critical Dependencies
- [Package]: [Current] → [Target] (Breaking: Yes/No)
- [Package]: [Current] → [Target] (Breaking: Yes/No)

### Deprecated Dependencies
- [Package]: Deprecated, migrate to [Alternative]

### Vulnerable Dependencies
- [Package]: CVE-YYYY-NNNNN (CVSS X.X)
```

---

#### 1.3 Code Compatibility Analysis

**Analyze Code Patterns**:
```bash
# Find API usage that may have changed
grep -r "using System\." --include="*.cs" | cut -d':' -f2 | sort | uniq -c | sort -rn

# Find conditional compilation
grep -r "#if NET" --include="*.cs"

# Find obsolete API usage (if analyzer enabled)
dotnet build /p:TreatWarningsAsErrors=true
```

**Key Areas**:
- API removals/changes
- Behavioral changes
- Conditional compilation
- Platform-specific code
- Reflection usage
- Serialization

**Document**:
```markdown
## Code Compatibility

### Breaking API Changes
- [API]: Removed/changed in [framework]
- Usage count: X locations
- Migration path: [Description]

### Conditional Compilation
- [X] instances of #if NET451
- [Y] instances of #if NETSTANDARD1_5
```

---

#### 1.4 Test Coverage Assessment

**Analyze Tests**:
```bash
# Count tests
dotnet test --list-tests

# Run tests and measure coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:./coverage-report
```

Key Questions:
- Is there sufficient test coverage to enable validation of migrated software? A good benchmark is 80% of lines of code covered.
- Is there test coverage at all relevant levels, unit test and integration test and end-to-end tests, with sufficient coverage?
- If test coverage is lacking, add a pre-migration phase for adding test coverage to the legacy system. This phase would involve only adding tests, with no changes allowed to the system being tested.

**Document**:
```markdown
## Test Coverage

### Test Statistics
- Total Tests: X
- Unit Tests: Y
- Integration Tests: Z
- Code Coverage: N%

### Coverage by Component
- Component A: X%
- Component B: Y%
- Component C: Z%
```

---

### Phase 2: Migration Strategy

#### 2.1 Target Framework Selection

**Decision Factors**:
- **LTS (Long-Term Support)**: .NET 8 (until Nov 2026) - or current LTS version
- **STS (Standard Term Support)**: Current STS version (shorter support window)
- **Feature requirements**: What features do you need?
- **Support timeline**: How long will you support this version?
- **Ecosystem compatibility**: Do dependencies support it?

**Recommended Approach**:
```markdown
## Target Framework Decision

**Selected**: .NET [8/9]
**Rationale**:
- [Reason 1]
- [Reason 2]
- [Reason 3]

**Multi-Targeting Strategy**: [Single/Multi]
- If multi: `<TargetFrameworks>net8.0;net7.0</TargetFrameworks>` (specify versions as needed)
- If single: `<TargetFramework>net8.0</TargetFramework>`
```

---

#### 2.2 Migration Phasing

**Common Phasing Strategies**:

**Option A: Bottom-Up (Recommended)**
```
Stage 1: Core libraries (no dependencies)
Stage 2: Mid-level libraries (depend on core)
Stage 3: High-level libraries (business logic)
Stage 4: Executables (depend on libraries)
Stage 5: Tests
Stage 6: Samples/Tools
```

**Option B: Top-Down**
```
Stage 1: Executables (force dependency updates)
Stage 2: Direct dependencies
Stage 3: Transitive dependencies
Stage 4: Tests
```

**Option C: Risk-Based**
```
Stage 1: Low-risk, isolated components
Stage 2: Medium-risk, moderate dependencies
Stage 3: High-risk, critical path components
Stage 4: Integration and validation
```

**Document Your Strategy**:
```markdown
## Migration Phasing

**Strategy**: [Bottom-Up/Top-Down/Risk-Based]

### Stage 1: [Name]
- Projects: [List]
- Dependencies: [List]
- Risk: [Low/Medium/High]
- Effort: [Hours/Days]

### Stage 2: [Name]
- Projects: [List]
- Dependencies: [List]
- Risk: [Low/Medium/High]
- Effort: [Hours/Days]

...
```

---

#### 2.3 Risk Assessment

**Risk Categories**:

| Risk | Description | Mitigation |
|------|-------------|------------|
| Low Test Coverage | Pre-migration testing is less than 80% of lines of code | Add additional test coverage until target coverage is reached. |
| **API Breaking Changes** | APIs removed/changed | Create ADR, update code systematically |
| **Dependency Incompatibility** | Package doesn't support target | Find alternative, fork, or wait |
| **Performance Regression** | New framework slower | Benchmark, optimize, accept trade-off |
| **Serialization Changes** | Binary/JSON format changes | Version contracts, dual-format support |
| **Test Failures** | Tests break on new framework | Fix immediately, don't defer |
| **Build Infrastructure** | CI/CD doesn't support new framework | Update build agents, Docker images |

**Document**:
```markdown
## Risk Assessment

### Risk 1: [Name]
- **Likelihood**: [Low/Medium/High]
- **Impact**: [Low/Medium/High]
- **Mitigation**: [Strategy]
- **Contingency**: [Fallback plan]

### Risk 2: [Name]
...
```

---

### Phase 3: Execution Planning

#### 3.1 Stage Definition Template

```markdown
## Stage N: [Stage Name]

### Objectives
- [Objective 1]
- [Objective 2]

### Scope
**Projects** ([X] total):
- Project.Name1
- Project.Name2

**Dependencies**:
- Depends on: Stage N-1
- Blocks: Stage N+1

### Tasks
1. **Update project files**
   - Change `<TargetFramework>` to `net8.0`
   - Update package references
   - Handle conditional compilation

2. **Build and fix errors**
   - Resolve breaking API changes
   - Fix compilation errors
   - Address warnings

3. **Update tests**
   - Run existing tests
   - Fix failing tests
   - Add new tests for changes

4. **Validate**
   - All builds pass
   - All tests pass (≥95%)
   - No security vulnerabilities

5. **Document**
   - Log to HISTORY.md
   - Update migration status
   - Note issues/resolutions

### Success Criteria
- [ ] All projects build successfully
- [ ] All tests pass (≥95%)
- [ ] No P0/P1 security issues
- [ ] Documentation updated
- [ ] Code reviewed (if team process)

### Estimated Effort
- **Optimistic**: X hours
- **Realistic**: Y hours
- **Pessimistic**: Z hours

### Risk Factors
- [Risk 1]
- [Risk 2]
```

---

#### 3.2 Dependency Update Strategy

**Approach**:
1. **Identify all dependencies** needing updates
2. **Prioritize** by impact and breaking changes
3. **Update incrementally** (one major version at a time if possible)
4. **Test after each update**
5. **Document breaking changes**

**Template**:
```markdown
## Dependency Update Plan

### Critical Path Dependencies
(These block other updates)

#### [Package.Name]
- Current: [X.Y.Z]
- Target: [A.B.C]
- Breaking Changes: [Yes/No]
- Migration Guide: [URL or description]
- Affected Projects: [List]
- Estimated Effort: [Hours]

### Secondary Dependencies
(Can be updated independently)

#### [Package.Name]
...
```

---

#### 3.3 Testing Strategy

**Test Phases**:
```markdown
## Testing Strategy

### Phase 1: Pre-Migration Baseline
- [ ] Run all tests on current framework
- [ ] Document baseline pass rate
- [ ] Document baseline performance
- [ ] Create test environment snapshot

### Phase 2: Continuous Testing
(After each stage)
- [ ] Run affected unit tests
- [ ] Run affected integration tests
- [ ] Compare pass rates to baseline
- [ ] Fix failures before proceeding

### Phase 3: Full Regression
(After all stages)
- [ ] Run complete test suite
- [ ] Validate all integration scenarios
- [ ] Performance benchmark comparison
- [ ] Load testing (if applicable)

### Phase 4: User Acceptance
- [ ] Sample applications validated
- [ ] Documentation tested
- [ ] Migration guide verified
- [ ] Beta testing (if applicable)

### Success Criteria
- Unit tests: ≥95% pass rate
- Integration tests: ≥90% pass rate
- Performance: <10% regression
- Zero P0 issues
```

---

#### 3.4 Rollback Planning

**Rollback Strategy**:
```markdown
## Rollback Plan

### Triggers
Rollback if:
- Critical functionality broken
- >20% test failure rate
- P0 security vulnerability introduced
- >25% performance regression
- Unable to deploy to production

### Rollback Procedure
1. **Revert code**: Git revert to pre-migration commit
2. **Restore packages**: Restore previous package versions
3. **Rebuild**: Clean and rebuild solution
4. **Test**: Validate baseline functionality
5. **Deploy**: Deploy previous version

### Recovery Time Objective (RTO)
- Rollback time: [X hours]
- Validation time: [Y hours]
- Total RTO: [Z hours]

### Data Considerations
- [ ] Check for data format changes
- [ ] Verify backward compatibility
- [ ] Document data migration steps
```

---

### Phase 4: Execution

#### 4.1 Per-Stage Execution Checklist

```markdown
## Stage Execution Checklist

### Pre-Execution
- [ ] Previous stage complete and validated
- [ ] All dependencies available
- [ ] Test environment prepared
- [ ] Rollback plan ready

### Execution
- [ ] Update project files
- [ ] Update dependencies
- [ ] Build all projects
- [ ] Fix compilation errors
- [ ] Fix warnings
- [ ] Run unit tests
- [ ] Run integration tests
- [ ] Fix test failures
- [ ] Performance check
- [ ] Security scan

### Post-Execution
- [ ] All success criteria met
- [ ] Documentation updated (HISTORY.md)
- [ ] Migration status updated
- [ ] Code reviewed (if team process)
- [ ] Commit with clear message
- [ ] Tag milestone (if applicable)

### Validation
- [ ] Independent validation by peer
- [ ] Sample application tested
- [ ] No regression in unrelated areas
```

---

#### 4.2 Issue Tracking Template

```markdown
## Migration Issues Log

### Issue [ID]: [Short Description]

**Discovered**: YYYY-MM-DD
**Stage**: [Stage number/name]
**Category**: [Build Error/Test Failure/Runtime Issue/Performance/Security]
**Priority**: [P0/P1/P2/P3]
**Status**: [Open/In Progress/Resolved/Deferred]

**Description**:
[Detailed description of the issue]

**Error Message/Stack Trace**:
```
[Full error details]
```

**Root Cause**:
[Analysis of why this occurred]

**Resolution**:
[How it was fixed, or plan to fix]

**Verification**:
[How to verify the fix works]

**Related Issues**:
- Issue #[ID]
- Issue #[ID]
```

---

### Phase 5: Validation

#### 5.1 Completion Criteria

**Stage Completion**:
```markdown
## Stage Completion Criteria

### Technical
- [ ] All projects build (100%)
- [ ] Unit tests pass (≥95%)
- [ ] Integration tests pass (≥90%)
- [ ] Code coverage maintained or improved
- [ ] No P0/P1 issues
- [ ] Performance acceptable (<10% regression)

### Documentation
- [ ] HISTORY.md updated
- [ ] ADRs created (if architectural decisions made)
- [ ] Migration status updated
- [ ] Issues documented

### Quality
- [ ] Code reviewed
- [ ] Security scanned
- [ ] Breaking changes documented
```

**Overall Migration Completion**:
```markdown
## Migration Completion Criteria

### All Stages Complete
- [ ] All projects migrated to target framework
- [ ] All tests passing
- [ ] All sample applications validated
- [ ] Documentation complete

### Production Readiness
- [ ] Performance benchmarks acceptable
- [ ] Security assessment passed
- [ ] Load testing passed (if applicable)
- [ ] Deployment validated in staging
- [ ] Rollback tested
- [ ] On-call team trained

### Documentation Complete
- [ ] CHANGELOG.md
- [ ] MIGRATION-GUIDE.md
- [ ] All ADRs
- [ ] Updated README.md
- [ ] Updated API documentation
- [ ] Sample applications documented

### Release Ready
- [ ] NuGet packages created
- [ ] Release notes prepared
- [ ] Deployment plan finalized
- [ ] Support plan documented
```

---

## Planning Templates

### Migration Plan Document Template

```markdown
# [Project Name] Migration Plan: [Old Version] → [New Version]

**Version**: 1.0
**Date**: YYYY-MM-DD
**Status**: [Planning/In Progress/Complete]

---

## Executive Summary

**Project**: [Project Name]
**Current Version**: [Old Framework]
**Target Version**: [New Framework]
**Timeline**: [Start] to [End] ([X weeks])
**Effort Estimate**: [Y person-days]
**Risk Level**: [Low/Medium/High]

---

## Current State

[From Phase 1: Discovery & Assessment]

---

## Migration Strategy

[From Phase 2: Migration Strategy]

---

## Stage Breakdown

[From Phase 3: Execution Planning]

---

## Testing Strategy

[From Phase 3.3: Testing Strategy]

---

## Risk Management

[From Phase 2.3: Risk Assessment]

---

## Timeline

| Stage | Start | End | Duration | Dependencies |
|-------|-------|-----|----------|--------------|
| Stage 1 | YYYY-MM-DD | YYYY-MM-DD | X days | None |
| Stage 2 | YYYY-MM-DD | YYYY-MM-DD | Y days | Stage 1 |
| ... | | | | |

---

## Resource Requirements

### Team
- Developer: [X hours/week]
- QA: [Y hours/week]
- DevOps: [Z hours/week]

### Infrastructure
- Build agents: [Specifications]
- Test environments: [Requirements]
- External dependencies: [List]

---

## Success Metrics

- Build success rate: 100%
- Test pass rate: ≥95%
- Code coverage: ≥80%
- Performance: ≤10% regression
- Security: Zero P0/P1 issues

---

## Approvals

- [ ] Technical Lead: _______________ Date: _______
- [ ] Product Owner: _______________ Date: _______
- [ ] Security Team: _______________ Date: _______
```

---

## Common Migration Scenarios

### Scenario 1: .NET Framework → .NET 6+

**Challenges**:
- WinForms/WPF compatibility
- ASP.NET → ASP.NET Core
- AppDomains removed
- BinaryFormatter deprecated
- Configuration system changed

**Recommended Approach**:
1. Use .NET Upgrade Assistant tool
2. Migrate shared libraries first
3. Keep UI/Web apps on .NET Framework initially
4. Gradual cutover

---

### Scenario 2: .NET Standard → .NET 6+

**Challenges**:
- Fewer, actually
- Mostly dependency updates

**Recommended Approach**:
1. Direct migration (single step)
2. Update dependencies
3. Remove unnecessary multi-targeting

---

### Scenario 3: .NET Core 3.1 → .NET 8/9

**Challenges**:
- Breaking changes in ASP.NET Core
- Minimal APIs (optional adoption)
- Nullable reference types (if not already enabled)
- Performance improvements may change behavior

**Recommended Approach**:
1. Straightforward upgrade
2. Address breaking changes
3. Adopt new features incrementally

---

## Tools and Resources

### Microsoft Tools
- [.NET Upgrade Assistant](https://dotnet.microsoft.com/en-us/platform/upgrade-assistant)
- [API Portability Analyzer](https://github.com/microsoft/dotnet-apiport)
- [.NET Framework to .NET Guide](https://docs.microsoft.com/en-us/dotnet/core/porting/)

### Analysis Tools
- NuGet Package Explorer
- dotnet-outdated
- Security scanning (dotnet list package --vulnerable)

### Documentation
- [Breaking Changes by Version](https://docs.microsoft.com/en-us/dotnet/core/compatibility/)
- [Migration Guide](https://docs.microsoft.com/en-us/dotnet/core/migration/)
- [What's New in .NET X](https://docs.microsoft.com/en-us/dotnet/core/whats-new/)

---

## Best Practices

1. **Start Early**: Don't wait until framework EOL
2. **Test Thoroughly**: Comprehensive testing prevents production issues
3. **Document Everything**: Future you will thank you
4. **Incremental Approach**: Small, validated steps
5. **Security First**: Fix vulnerabilities before migrating
6. **Performance Baseline**: Know your starting point
7. **Rollback Ready**: Always have a way back
8. **Team Communication**: Keep stakeholders informed

---

## Customization Checklist

To use this guide for your project:

- [ ] Copy this template
- [ ] Fill in project-specific details
- [ ] Run discovery phase analysis
- [ ] Customize phasing strategy
- [ ] Adjust timelines based on team size
- [ ] Add project-specific risks
- [ ] Define your success criteria
- [ ] Create tracking issues/tasks
- [ ] Schedule regular check-ins
- [ ] Plan celebration for completion! 🎉

---

**Template Version**: 1.0
**Last Updated**: 2025-10-10
**Maintained By**: Migration Planning Team
**Status**: Template - Customize for your project
**Applicability**: Universal - All .NET framework migrations
