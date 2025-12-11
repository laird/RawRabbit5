# Modernization Plan: RawRabbit

**Version**: 1.1
**Date**: 2025-12-11
**Based On**: ASSESSMENT.md (Score: 62/100, High Risk)

## 1. Executive Summary

**Objective**: Modernize `RawRabbit` to **.NET 8** and **RabbitMQ.Client v7.0.0**, eliminating EOL frameworks and legacy dependencies while fixing persistent test deadlocks.
**Timeline**: Phased execution over ~2 months (Estimated).
**Team**: Migration Coordinator, Architect, Modernization Engineer.
**Success Criteria**:

- 100% Test Pass Rate (No Deadlocks).
- Zero EOL Frameworks/Dependencies.
- Performance Baseline met or exceeded.

## 2. Assessment Summary

- **Viability**: 50/100 (Legacy .NET 4.5.1/NetStandard 1.5).
- **Risks**: High. Breaking changes in `RabbitMQ.Client` v7 (Async IO) require deep refactoring.
- **Current Status**: Legacy tests build but **HANG** (deadlock) in modern execution environments.

## 3. Scope

**IN SCOPE**:

- Retargeting all projects to `net8.0`.
- Upgrading `RabbitMQ.Client` to v7.0.0.
- Upgrading `Newtonsoft.Json` (or replacing with `System.Text.Json`).
- Refactoring `RawRabbit` middleware engine to support full Async/Await.
- Fixing Unit and Integration Test suites.

**OUT OF SCOPE**:

- New feature development.
- Major API redesigns (unless forced by dependency breaking changes).

## 4. Phase Breakdown

### Phase 0: Discovery & Assessment (Refined)

- [x] Inventory code & dependencies.
- [x] Assess risks & viability.
- [x] **Test Baseline**: Verified on **Legacy SDK/Runtime**.
  - Result: **Hang/Deadlock** (Confirmed on .NET 4.6.2 SDK).
  - Capture Pass Rates & Coverage: N/A (Run Incomplete).
  - **Capture Execution Times**: Infinite.
- [x] **Critical Fix**: Resolve legacy test deadlocks to establish a passing baseline.

### Phase 1: Security Remediation

- [ ] Identify and upgrade packages with critical CVEs (blocked by Framework version).
- [ ] Strategy: Upgrade Framework FIRST (Phase 3) to enable secure package versions.

### Phase 2: Architecture & Design

- [x] `ADR-001`: Target Framework Selection (.NET 8).
- [x] `ADR-002`: RabbitMQ Client Version (v7.0).
- [x] `ADR-003`: Middleware Async Pattern (Design refactor for v7).

### Phase 3: Framework & Dependency Modernization

- [ ] **Infrastructure**: Create `Directory.Build.props`, `global.json`.
- [ ] **Retarget**: Convert all csproj files to SDK-style `net8.0`.
- [ ] **Upgrade**: Update `RabbitMQ.Client` to v7, `Newtonsoft.Json`, and DI adapters.

### Phase 4: API Modernization & Code Quality

- [ ] **Core Engine Refactor**: Rewrite `PipeContext` and Middleware for Async IO.
- [ ] **Fix Compilation Errors**: Address breaking changes in RabbitMQ API.
- [ ] **Extension Migration**: Update all `RawRabbit.Enrichers.*` and `Operations`.

### Phase 5: Performance Optimization

- [ ] Benchmark new Async engine against legacy baseline.
- [ ] Optimize memory allocation (Pipelines).

### Phase 6: Comprehensive Documentation

- [ ] Update README with new supported versions.
- [ ] Create Migration Guide for existing `RawRabbit` users.

### Phase 7: Final Validation & Release

- [ ] Full Regression Test Suite execution.
- [ ] Security Audit (dotnet list package --vulnerable).
- [ ] **Go/No-Go Decision**.

## 5. Timeline & Milestones

- **M1 (Week 1)**: Plan Approved, ADRs Complete, Infrastructure Ready.
- **M2 (Week 3)**: Core Library Compiles (Net8/RabbitMQv7).
- **M3 (Week 5)**: Middleware Engine Refactored & Unit Tests Passing.
- **M4 (Week 8)**: Integration Tests Passing & Performance Verified.

## 6. Risk Management

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| **Async Deadlocks** | Critical | High | Early refactor of Middleware Engine; strict use of `ConfigureAwait(false)`. |
| **API Incompatibility** | High | High | ADR-003 to map legacy `Pipe` concepts to v7 patterns. |
| **Performance Regression** | Medium | Medium | Benchmark early (Phase 5). |

## 7. Quality Gates

1. **Gate 1 (Design)**: All ADRs approved.
2. **Gate 2 (Core Build)**: `RawRabbit` compiles without errors.
3. **Gate 3 (Tests)**: 100% Pass rate on Unit Tests.
4. **Gate 4 (Release)**: Integration Tests Pass + Zero High Vulnerabilities.

## 8. Contingency Plans

- **Scenario**: RabbitMQ v7 refactor proves too complex (>2 weeks).
  - **Plan B**: Downgrade to `RabbitMQ.Client` v6 (Maintenance Mode) to reduce friction, plan v7 for next major.
- **Scenario**: Legacy tests never pass in baseline.
  - **Plan B**: Accept "Gray Box" baseline; rely on manual verification and new .NET 8 test stability.
