# Project Modernization Assessment: RawRabbit

**Date**: 2025-12-11
**Target**: `RawRabbit` Repository
**Decision**: ⚠️ **PROCEED WITH CAUTION**

## 1. Executive Summary

The `RawRabbit` project is a significant legacy codebase (~27k LOC) built on .NET Framework 4.5.1, 4.6, and .NET Standard 1.5. It relies on the EOL `RabbitMQ.Client` v5.0.1. While the project is architecturally structured (middleware pipelines, dependency injection integration), the technical debt is **CRITICAL**. Modernization to .NET 8 and `RabbitMQ.Client` v7 is feasible but represents a major rewriting effort due to breaking changes in the underlying broker client and the .NET ecosystem shifts over the last decade.

**Overall Score**: **62/100**

## 2. Technical Viability

**Score**: **50/100**

- **Frameworks**: Currently targets `net451`, `net46`, `netstandard1.5`. These are End-of-Life.
- **Dependencies**: Relies on `RabbitMQ.Client` 5.0.1. Version 7.x introduces breaking changes (async-only, distinct topology handling).
- **Migration Path**: Difficult. Requires updating the csproj format (done in some places but mixed), retargeting to `net8.0`, and rewriting all consumer/publisher logic to accommodate the new RabbitMQ client async patterns.

## 3. Business Value

**Score**: **90/100**

- **Strategic Alignment**: High. Core messaging infrastructure.
- **Effort-Benefit**: High. Modernization provides critical security updates, performance improvements (System.Text.Json, pipelines), and cross-platform compatibility.

## 4. Risk Assessment

**Profile**: **HIGH**

| Risk Type | Likelihood | Impact | Description |
|-----------|------------|--------|-------------|
| **Breaking Changes** | High | Critical | RabbitMQ.Client v7 API is fundamentally different from v5. |
| **Regression** | Medium | High | Behavior changes in message serialization and error handling. |
| **Dependency Hell** | High | Medium | Old dependencies (Autofac 4, Ninject) may have conflict with .NET 8 DI abstractions. |

## 5. Resource Assessment

- **Timeline**: **Large (2 months+)**
- **Effort**: 26,835 Lines of Code across ~700 files.
- **Skillset**: Requires Deep .NET 8 knowledge, RabbitMQ internals, and Refactoring experience.

## 6. Project Statistics

- **Lines of Code**: 26,835
- **Files**: 698
- **First Commit**: 2015-10-16
- **Last Commit**: 2025-12-11
- **Top Contributor**: pardahlman

## 7. Code Quality & Architecture

**Score**: **70/100**

- **Architecture**: Appears well-structured with a focus on modularity (Enrichers, Operations). This modularity will actually assist in incremental modernization.
- **Complexity**: High volume of small files indicates good separation of concerns, but the proprietary "Pipe" architecture may need adapting to modern `System.Threading.Channels` or `Pipelines`.

## 8. Test Coverage & Stability

**Score**: **30/100** (Current State)

- **Status**: Tests fail to run in the current environment due to missing legacy SDKs/targeting packs.
- **Action**: Essential to restore a baseline passing state before heavy refactoring, or accept "rewrite and fix" strategy.

## 9. Security

**Score**: **40/100**

- **Vulnerabilities**: High probability of known vulnerabilities in `RabbitMQ.Client` 5.x and `Newtonsoft.Json` 10.x. Automated scans failed due to build issues, which is a security risk in itself (unmaintainable).

---
**Recommendation**: The project is a prime candidate for modernization due to its architectural value but carries significant risk. Proceed with a phased approach:

1. **Stabilize (Strict Legacy)**: Install original legacy SDKs/runtimes required. Run original tests **exactly as is** to establish a true baseline. Do NOT retarget or modify project files for stabilization.
2. **Upgrade**: Target .NET 8.
3. **Refactor**: Replace `RabbitMQ.Client` usage.
