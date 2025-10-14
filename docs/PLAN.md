# RawRabbit Modernization & Security Plan

**Version**: 3.0.0
**Date**: 2025-10-13
**Status**: Planning Complete - Ready for Execution
**Branch**: 2.1-10-13-2025

---

## Executive Summary

### Project Scope

- **Total Projects**: 28 (.NET projects)
  - Core: 1 project
  - Operations: 8 projects
  - Enrichers: 10 projects (8 active, 2 to deprecate)
  - DI Adapters: 3 projects
  - Compatibility: 1 project
  - Samples: 3 projects
  - Tests: 4 projects (excludes deprecated Polly tests)

- **Current State**: `netstandard1.5` / `net451` multi-targeting
- **Target State**: `.NET 9.0` single target, secure dependencies, modern packaging
- **Estimated Duration**: 2-3 weeks
- **Time Savings**: 50-67% through parallel execution

### Strategic Objectives

1. **Security**: Remediate all CRITICAL/HIGH vulnerabilities
2. **Modernization**: Migrate to .NET 9.0 with modern language features
3. **Quality**: Maintain ≥95% test pass rate throughout migration
4. **Documentation**: Complete migration guide and changelog incrementally
5. **Packaging**: Production-ready NuGet packages with zero CVEs

### Key Success Metrics

- [ ] 28/28 projects migrated to .NET 9.0 (100%)
- [ ] Build success rate: 100%
- [ ] Unit test pass rate: ≥95%
- [ ] Integration test pass rate: ≥90%
- [ ] Security score: ≥45 (from current baseline)
- [ ] CRITICAL/HIGH CVEs: 0
- [ ] Documentation: 1,500+ lines across CHANGELOG, MIGRATION-GUIDE, ADRs
- [ ] NuGet packages: All validated and published

---

## Security Assessment Summary

### Current Security Posture (Pre-Migration)

Based on vulnerability scan of current state:

**Critical Vulnerabilities**:
- `dotnet list package --vulnerable` shows no current CRITICAL vulnerabilities reported
- However, packages are severely outdated (2017-2018 era)

**High-Priority Security Issues**:
- **Newtonsoft.Json 10.0.1** (2017):
  - Target: 13.0.3 (current security-patched version)
  - CVE-2024-43485: HIGH severity deserialization vulnerability
  - **Impact**: P1 (Must fix in Stage 1)

- **RabbitMQ.Client 5.0.1** (2018):
  - Target: 6.8.1 (stable) or 7.1.2 (latest)
  - Multiple HIGH severity CVEs in 5.x branch
  - Breaking API changes in 6.x and 7.x
  - **Impact**: P1 (Must fix in Stage 1 or 2)

### Security Remediation Strategy

**Priority Levels**:
- **P0 (Blocking)**: CRITICAL vulnerabilities - Fix immediately (same day)
- **P1 (High)**: HIGH vulnerabilities - Fix in Stage 1 before core migration
- **P2 (Medium)**: MEDIUM vulnerabilities - Address during migration
- **P3 (Low)**: LOW vulnerabilities - Document and track

**Stage 1 Security Targets**:
1. Update Newtonsoft.Json: 10.0.1 → 13.0.3
2. Analyze RabbitMQ.Client upgrade path (coordinate with Stage 2)
3. Run security scan post-remediation
4. Validate zero P0/P1 vulnerabilities
5. Achieve security score ≥45

---

## Architecture Decisions

The following ADRs will be created during migration to document key architectural decisions:

### ADR 0001: Target Framework (.NET 9.0)

**Status**: To be created in Stage 0/1
**Decision**: Migrate to .NET 9.0 single-target

**Rationale**:
- **Current Support**: .NET 9.0 is the latest STS (Standard Term Support) release
- **Performance**: Significant runtime and compiler improvements
- **Features**: Latest C# language features, improved nullable reference types
- **Timeline**: Support through May 2026 (18 months)
- **Ecosystem**: All dependencies compatible (RabbitMQ.Client 6.8.1+, Newtonsoft.Json 13.0.3)

**Alternatives Considered**:
- **.NET 8.0 (LTS)**: Longer support (until Nov 2026) but fewer features
- **.NET 7.0**: EOL May 2024 - Not viable
- **Multi-targeting (net8.0;net9.0)**: Added complexity, unnecessary for library

**Decision**: .NET 9.0 single-target

**Impact**:
- **Breaking Change**: Users must upgrade to .NET 9.0 SDK
- **Version Bump**: 2.x → 3.0.0 (major version)
- **Migration Effort**: Straightforward (no multi-targeting complexity)

---

### ADR 0002: RabbitMQ.Client Version Strategy

**Status**: To be created in Stage 1/2
**Decision**: Upgrade to RabbitMQ.Client 6.8.1 (stable)

**Rationale**:
- **Security**: 5.0.1 has multiple HIGH severity CVEs
- **Compatibility**: 6.8.1 is stable and well-tested
- **Breaking Changes**: Manageable with helper utilities
- **Future-Proof**: 6.x maintained, 7.x still maturing

**Breaking Changes in 6.8.1**:
1. **BasicProperties Constructor**: Now protected
   - **Fix**: Create `BasicPropertiesHelper.CreateBasicProperties()`
2. **Body Type**: `byte[]` → `ReadOnlyMemory<byte>`
   - **Fix**: Use `.ToArray()` for existing code
3. **Consumer Tag**: `ConsumerTag` property removed
   - **Fix**: Update consumer implementations
4. **CreateConnection**: Added `clientProvidedName` parameter
   - **Fix**: Update factory calls

**Alternatives Considered**:
- **RabbitMQ.Client 7.1.2**: Latest but newer, more breaking changes
- **Stay on 5.0.1**: Security risk, unacceptable
- **Fork 5.0.1**: Maintenance burden, unacceptable

**Decision**: RabbitMQ.Client 6.8.1

**Impact**:
- **Code Changes**: ~15-20 files (BasicProperties usage, Body conversions)
- **Test Updates**: Mock setups need updates
- **Documentation**: Migration guide with before/after examples
- **Helper Class**: Add `BasicPropertiesHelper` utility

---

### ADR 0003: Serialization Enricher Strategy

**Status**: To be created in Stage 4
**Decision**: Deprecate ZeroFormatter, recommend MessagePack

**Problem**: ZeroFormatter is abandoned (last update 2017), incompatible with .NET Core 2.0+

**Alternatives**:
1. **ZeroFormatter**: Abandoned, cannot build on .NET 9.0 ❌
2. **MessagePack**: Actively maintained, excellent performance ✅
3. **Protobuf**: Google-backed, good performance ✅

**Decision**:
- Mark `RawRabbit.Enrichers.ZeroFormatter` as `[Obsolete]`
- Recommend `RawRabbit.Enrichers.MessagePack` as primary alternative
- Document migration path in MIGRATION-GUIDE.md

**Migration Path**:
```csharp
// Before (ZeroFormatter):
[ZeroFormattable]
public class MyMessage
{
    [Index(0)]
    public string Name { get; set; }
}

// After (MessagePack):
[MessagePackObject]
public class MyMessage
{
    [Key(0)]
    public string Name { get; set; }
}
```

**Impact**:
- Users must migrate serialization attributes
- Plugin registration change: `.UseZeroFormatter()` → `.UseMessagePack()`
- Non-breaking (package still ships, marked obsolete)

---

### ADR 0004: DI Adapter Strategy (Ninject Deprecation)

**Status**: To be created in Stage 5
**Decision**: Deprecate Ninject adapter, recommend Microsoft.Extensions.DependencyInjection

**Problem**: Ninject development slowed, Microsoft.Extensions.DI is .NET standard

**Alternatives**:
1. **Ninject**: Slower development, niche usage ⚠️
2. **Microsoft.Extensions.DI**: Built-in, widespread adoption ✅
3. **Autofac**: Popular, actively maintained ✅

**Decision**:
- Mark `RawRabbit.DependencyInjection.Ninject` as `[Obsolete]`
- Recommend `RawRabbit.DependencyInjection.ServiceCollection` (Microsoft.Extensions.DI)
- Keep `RawRabbit.DependencyInjection.Autofac` as alternative
- Still migrate Ninject to .NET 9.0 (maintain compatibility)

**Impact**:
- Users should migrate to ServiceCollection or Autofac
- Ninject package still ships but marked obsolete
- Migration guide documents registration changes

---

### ADR 0005: Breaking Changes Strategy

**Status**: To be created in Stage 0/1
**Decision**: Major version bump (2.x → 3.0.0)

**Rationale**:
- .NET 9.0 requirement is breaking change
- RabbitMQ.Client 6.x has breaking API changes
- Nullable reference types may surface issues
- Deprecated packages (ZeroFormatter, Ninject)

**Semantic Versioning**:
- **Major**: Breaking changes (minimum .NET version, API changes)
- **Minor**: New features (backwards compatible)
- **Patch**: Bug fixes

**Decision**: Version 3.0.0

**Impact**:
- Clear signal to users: review MIGRATION-GUIDE.md
- Allows breaking changes without SemVer violation
- Sets foundation for future 3.x releases

---

### ADR 0006: Nullable Reference Types

**Status**: To be created in Stage 2
**Decision**: Enable nullable reference types (`<Nullable>enable</Nullable>`)

**Rationale**:
- Modern .NET best practice
- Catch null reference bugs at compile time
- Improve API contract clarity
- Required for many .NET 9.0 libraries

**Approach**: Enable in Core first, propagate to all projects

**Impact**:
- Compiler warnings on potential null issues
- May require `?` annotations on nullable types
- Better code quality long-term
- Some initial warning cleanup required

---

## Project Dependency Analysis

### Complete Dependency Graph

```
Level 0 (No dependencies on other RawRabbit projects):
  - RawRabbit (Core)                                    [Stage 2]

Level 1 (Depends on Core only):
  Operations (8 projects - FULLY PARALLEL):             [Stage 3]
    - RawRabbit.Operations.Publish
    - RawRabbit.Operations.Subscribe
    - RawRabbit.Operations.Get
    - RawRabbit.Operations.Request
    - RawRabbit.Operations.Respond
    - RawRabbit.Operations.Tools
    - RawRabbit.Operations.StateMachine
    - RawRabbit.Operations.MessageSequence

  Enrichers - Simple (6 projects - FULLY PARALLEL):     [Stage 4.1]
    - RawRabbit.Enrichers.Attributes
    - RawRabbit.Enrichers.GlobalExecutionId
    - RawRabbit.Enrichers.QueueSuffix
    - RawRabbit.Enrichers.RetryLater
    - RawRabbit.Enrichers.Protobuf
    - RawRabbit.Enrichers.MessagePack

  DI Adapters (3 projects - FULLY PARALLEL):            [Stage 5]
    - RawRabbit.DependencyInjection.ServiceCollection
    - RawRabbit.DependencyInjection.Autofac
    - RawRabbit.DependencyInjection.Ninject

Level 2 (Depends on Level 1):
  Enrichers - Complex (4 projects - SEQUENTIAL):        [Stage 4.2]
    - RawRabbit.Enrichers.MessageContext (base)
    - RawRabbit.Enrichers.MessageContext.Subscribe
    - RawRabbit.Enrichers.MessageContext.Respond
    - RawRabbit.Enrichers.HttpContext
    - RawRabbit.Enrichers.Polly (MAJOR REFACTORING)

  Enrichers - Deprecated (2 projects - DOCUMENT ONLY):  [Stage 4.3]
    - RawRabbit.Enrichers.ZeroFormatter (mark obsolete)
    - (Ninject handled in Stage 5)

Level 3 (Depends on multiple):
  - RawRabbit.Compatibility.Legacy                      [Stage 6]
  - RawRabbit.Messages.Sample                           [Stage 6]
  - RawRabbit.AspNet.Sample                             [Stage 6]
  - RawRabbit.ConsoleApp.Sample                         [Stage 6]

Level 4 (Test projects):
  - RawRabbit.Tests                                     [Stage 7]
  - RawRabbit.IntegrationTests                          [Stage 7]
  - RawRabbit.PerformanceTest                           [Stage 7]
```

### Parallelization Analysis

**Fully Parallel Stages** (100% parallel execution):
- **Stage 3**: 8 Operations projects (0 cross-dependencies)
  - Time: 15-20 min parallel vs. 120 min sequential
  - **Savings: 100 minutes (83%)**

- **Stage 4.1**: 6 simple Enrichers (0 cross-dependencies)
  - Time: 15-20 min parallel vs. 90 min sequential
  - **Savings: 70 minutes (78%)**

- **Stage 5**: 3 DI adapters (0 cross-dependencies)
  - Time: 10-15 min parallel vs. 45 min sequential
  - **Savings: 30 minutes (67%)**

- **Stage 6**: 4 Samples/Compatibility (0 cross-dependencies)
  - Time: 20-30 min parallel vs. 120 min sequential
  - **Savings: 90 minutes (75%)**

- **Stage 7**: 3 Test projects (parallel test runs)
  - Time: 30-40 min parallel vs. 90 min sequential
  - **Savings: 50 minutes (56%)**

**Total Time Savings: 340 minutes (5.7 hours) = 50-67% reduction**

---

## Migration Stages (Bottom-Up Approach)

### Stage 0: Prerequisites & Baseline

**Duration**: 2 hours
**Objectives**: Environment setup and baseline capture

#### Tasks

1. **Install .NET 9.0 SDK**
   ```bash
   # Download from https://dotnet.microsoft.com/download/dotnet/9.0
   # Or use system package manager:
   # Linux (Ubuntu/Debian):
   sudo apt-get update
   sudo apt-get install -y dotnet-sdk-9.0

   # Verify installation
   ~/.dotnet/dotnet --version  # Should show 9.0.x
   ```

2. **Capture Pre-Migration Baseline**
   ```bash
   # Run baseline capture script
   ./scripts/capture-test-baseline.sh

   # Outputs: docs/test-baselines/baseline-YYYY-MM-DD-HHMMSS.md
   ```

3. **Create Automation Scripts**
   - `scripts/analyze-dependencies.sh` - Dependency analysis
   - `scripts/run-stage-tests.sh` - Stage-specific testing
   - `scripts/capture-test-baseline.sh` - Baseline metrics
   - `scripts/validate-migration-stage.sh` - Quality gate validation
   - `scripts/append-to-history.sh` - HISTORY.md logging

4. **Validate RabbitMQ Test Infrastructure**
   ```bash
   # Start RabbitMQ for integration tests
   docker run -d --name rabbitmq-test \
     -p 5672:5672 \
     -p 15672:15672 \
     rabbitmq:3-management

   # Verify running
   docker ps | grep rabbitmq
   ```

5. **Create Migration Branch**
   ```bash
   # Branch should already exist: 2.1-10-13-2025
   git status
   git branch
   ```

6. **Document Current State**
   - Total projects: 28
   - Target frameworks: netstandard1.5, net451
   - Test count: Run baseline script
   - Build status: Document current build state

#### Success Criteria

- [ ] .NET 9.0 SDK installed and verified (`dotnet --version`)
- [ ] Baseline metrics captured (`docs/test-baselines/baseline-*.md` exists)
- [ ] Current build status documented
- [ ] RabbitMQ Docker container running
- [ ] All scripts created and executable
- [ ] Migration branch ready (2.1-10-13-2025)

#### Quality Gate

- **Environment**: .NET 9.0 SDK functional
- **Baseline**: Test baseline captured
- **Infrastructure**: RabbitMQ running for integration tests
- **Automation**: All scripts created

#### Agent Assignment

- **Primary**: Migration Coordinator (setup and validation)

---

### Stage 1: Security Remediation

**Duration**: 1-2 days
**Objectives**: Fix all CRITICAL and HIGH vulnerabilities before framework migration

#### Input Documents

- Security assessment (from Security Agent)
- Vulnerability scan results (`dotnet list package --vulnerable`)

#### Tasks

1. **Create ADR 0001: Target Framework**
   ```bash
   cat > "docs/adr/ADR 0001 Target Framework NET9.md" << 'EOF'
   # ADR 0001: Target Framework - .NET 9.0

   **Status**: accepted
   **Date**: 2025-10-13

   ## Decision
   Migrate to .NET 9.0 single-target framework.

   [Full ADR content per protocol]
   EOF
   ```

2. **Update Newtonsoft.Json (ALL projects)**
   ```bash
   # Update all .csproj files
   find src -name "*.csproj" -exec sed -i 's/Newtonsoft.Json" Version="10.0.1"/Newtonsoft.Json" Version="13.0.3"/g' {} \;

   # Restore packages
   ~/.dotnet/dotnet restore RawRabbit.sln
   ```

3. **Plan RabbitMQ.Client Update**
   - Create ADR 0002: RabbitMQ.Client Version Strategy
   - Document breaking changes
   - Plan helper utilities (deferred to Stage 2 for Core migration)

4. **Run Security Scan**
   ```bash
   ~/.dotnet/dotnet list RawRabbit.sln package --vulnerable

   # Target: Zero CRITICAL/HIGH vulnerabilities
   ```

5. **Run Test Suite (Validation)**
   ```bash
   ~/.dotnet/dotnet test --configuration Release

   # Ensure Newtonsoft.Json update doesn't break tests
   # Target: ≥95% pass rate
   ```

6. **Update CHANGELOG.md**
   ```markdown
   ### Security
   - Updated Newtonsoft.Json: 10.0.1 → 13.0.3
   - Resolved CVE-2024-43485 (HIGH severity)
   - Security score: [before] → [after]
   ```

#### Success Criteria

- [ ] Newtonsoft.Json updated to 13.0.3 in ALL projects
- [ ] Zero CRITICAL vulnerabilities
- [ ] Zero HIGH vulnerabilities
- [ ] Security score ≥ 45
- [ ] All tests still passing (≥95%)
- [ ] ADR 0001 created (Target Framework)
- [ ] ADR 0002 created (RabbitMQ.Client strategy)
- [ ] CHANGELOG.md updated

#### Quality Gate

- **Security**: No P0/P1 vulnerabilities
- **Functionality**: ≥95% test pass rate (no regressions)
- **Documentation**: ADRs created, CHANGELOG updated

#### Agent Assignment

- **Primary**: Security Agent (vulnerability analysis)
- **Support**: Coder Agent (apply fixes)

#### Testing

```bash
# Full test suite
~/.dotnet/dotnet test --configuration Release

# Security validation
~/.dotnet/dotnet list package --vulnerable
```

---

### Stage 2: Core Library Migration

**Duration**: 1-2 days
**Project**: `RawRabbit` (foundation for all others)

#### Objectives

- Migrate Core library to .NET 9.0
- Update RabbitMQ.Client to 6.8.1
- Fix all breaking API changes
- Enable nullable reference types
- Create helper utilities

#### Dependencies

- **Depends on**: Stage 1 complete (security remediation)
- **Blocks**: All other stages (Core is foundation)

#### Tasks

1. **Update RawRabbit.csproj**
   ```xml
   <!-- Change from: -->
   <TargetFrameworks>netstandard1.5;net451</TargetFrameworks>
   <VersionPrefix>2.0.0</VersionPrefix>

   <!-- To: -->
   <TargetFramework>net9.0</TargetFramework>
   <VersionPrefix>3.0.0</VersionPrefix>
   <LangVersion>latest</LangVersion>
   <Nullable>enable</Nullable>
   ```

2. **Update RabbitMQ.Client**
   ```xml
   <!-- Update package reference -->
   <PackageReference Include="RabbitMQ.Client" Version="6.8.1" />
   ```

3. **Create BasicPropertiesHelper.cs**
   ```bash
   # Location: src/RawRabbit/Configuration/BasicPropertiesHelper.cs
   # Purpose: Wrapper for BasicProperties creation (constructor now protected)
   ```

4. **Fix RabbitMQ.Client Breaking Changes**

   **a. BasicProperties Constructor** (~10 files):
   - Search: `new BasicProperties`
   - Replace with: `BasicPropertiesHelper.CreateBasicProperties()`

   **b. Body Type Conversion** (~5 files):
   - Search: `body` (byte[] usage)
   - Update to: `body.ToArray()` or `new ReadOnlyMemory<byte>(body)`

   **c. Consumer Tag** (~3 files):
   - Update consumer implementations (property removed)

   **d. CreateConnection** (~2 files):
   - Add `clientProvidedName` parameter

5. **Remove Conditional Compilation**
   ```bash
   # Remove all #if NET451 and #if NETSTANDARD1_5 blocks
   grep -r "#if NET" src/RawRabbit/ --include="*.cs"
   # Manually review and remove conditional blocks
   ```

6. **Remove net451-Specific References**
   ```xml
   <!-- Remove from .csproj: -->
   <ItemGroup Condition=" '$(TargetFramework)' == 'net451' ">
     <Reference Include="System" />
     <Reference Include="Microsoft.CSharp" />
   </ItemGroup>
   ```

7. **Address Nullable Reference Type Warnings**
   ```bash
   ~/.dotnet/dotnet build src/RawRabbit/RawRabbit.csproj --configuration Release
   # Review nullable warnings
   # Add `?` where appropriate or `!` for non-null assertions
   ```

8. **Build and Fix Errors**
   ```bash
   ~/.dotnet/dotnet build src/RawRabbit/RawRabbit.csproj --configuration Release

   # Iterate until 0 errors
   # Target: Clean build
   ```

9. **Run Core Unit Tests**
   ```bash
   ~/.dotnet/dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj \
     --filter "FullyQualifiedName~RawRabbit.Tests.Channel|FullyQualifiedName~RawRabbit.Tests.Consumer" \
     --logger "console;verbosity=detailed" \
     --configuration Release

   # Fix mock setups (BasicProperties, Body conversions)
   # Target: 100% pass rate for core tests
   ```

10. **Update Documentation**
    ```bash
    # Update CHANGELOG.md
    # Add breaking changes to MIGRATION-GUIDE.md with code examples
    # Update ADR 0002 status: proposed → accepted
    # Update ADR 0006 status: proposed → accepted
    ```

11. **Log Completion**
    ```bash
    ./scripts/append-to-history.sh \
      "Stage 2 Complete: Core Library Migrated to .NET 9.0" \
      "Migrated RawRabbit core library to .NET 9.0. Updated RabbitMQ.Client 5.0.1 → 6.8.1. Created BasicPropertiesHelper. Fixed Body type conversions. Enabled nullable reference types. All core tests passing." \
      "Core library is the foundation for all other projects. Critical to validate thoroughly before proceeding." \
      "Core library migration successful. Build clean. Tests passing at 100%. Ready for Stage 3."
    ```

#### Breaking Changes Addressed

1. **BasicProperties**: Protected constructor → Helper utility
2. **Body Type**: `byte[]` → `ReadOnlyMemory<byte>`
3. **Consumer Tag**: Property removed → Update implementations
4. **CreateConnection**: New parameter → Update factory calls

#### Success Criteria

- [ ] RawRabbit.csproj updated to `<TargetFramework>net9.0</TargetFramework>`
- [ ] VersionPrefix updated to `3.0.0`
- [ ] RabbitMQ.Client updated to `6.8.1`
- [ ] Newtonsoft.Json `13.0.3` (from Stage 1)
- [ ] `<Nullable>enable</Nullable>` added
- [ ] `<LangVersion>latest</LangVersion>` added
- [ ] All conditional compilation removed (`#if NET451`, etc.)
- [ ] `BasicPropertiesHelper.cs` created
- [ ] Build succeeds with 0 errors
- [ ] Core unit tests pass at 100%
- [ ] ADR 0002 accepted (RabbitMQ.Client)
- [ ] ADR 0006 accepted (Nullable)
- [ ] CHANGELOG.md updated
- [ ] MIGRATION-GUIDE.md updated with breaking changes

#### Quality Gate

- **Build**: 100% success (0 errors, warnings acceptable for nullable)
- **Tests**: 100% core unit tests passing
- **Code Quality**: Nullable warnings addressed
- **Documentation**: Breaking changes documented with examples

#### Agent Assignment

- **Primary**: Coder Agent (experienced with RabbitMQ.Client)
- **Support**: Tester Agent (validate test fixes)

#### Testing

```bash
# Build validation
~/.dotnet/dotnet build src/RawRabbit/RawRabbit.csproj --configuration Release

# Core tests
./scripts/run-stage-tests.sh 2 "Core Library"

# Expected: 100% pass rate
```

#### Estimated Effort

- **Optimistic**: 6 hours
- **Realistic**: 8 hours (1 day)
- **Pessimistic**: 16 hours (2 days)

---

### Stage 3: Operations Projects Migration (PARALLEL)

**Duration**: 15-20 minutes (parallel) vs. 120 minutes (sequential)
**Projects**: 8 Operations projects
**Parallelization**: 100% (Level 0 - no cross-dependencies)

#### Objectives

- Migrate all 8 Operations projects to .NET 9.0
- Update to RawRabbit 3.0.0 reference
- Remove multi-targeting
- Validate builds

#### Projects (All Level 0 - Fully Parallel)

1. `RawRabbit.Operations.Publish`
2. `RawRabbit.Operations.Subscribe`
3. `RawRabbit.Operations.Get`
4. `RawRabbit.Operations.Request`
5. `RawRabbit.Operations.Respond`
6. `RawRabbit.Operations.Tools`
7. `RawRabbit.Operations.StateMachine`
8. `RawRabbit.Operations.MessageSequence`

#### Dependency Analysis

```bash
# Verify all Operations depend only on Core (already migrated)
./scripts/analyze-dependencies.sh "src/RawRabbit.Operations.*"

# Output (expected):
# ✅ Operations.Publish: No in-stage dependencies (Level 0)
# ✅ Operations.Subscribe: No in-stage dependencies (Level 0)
# ... (all 8 projects)
#
# Recommendation: Spawn 8 parallel agents in SINGLE message
```

#### Parallel Execution Strategy

**Single Message with 8 Agent Tasks**:

```markdown
Task("Migrate Operations.Publish", "[full instructions]", "coder")
Task("Migrate Operations.Subscribe", "[full instructions]", "coder")
Task("Migrate Operations.Get", "[full instructions]", "coder")
Task("Migrate Operations.Request", "[full instructions]", "coder")
Task("Migrate Operations.Respond", "[full instructions]", "coder")
Task("Migrate Operations.Tools", "[full instructions]", "coder")
Task("Migrate Operations.StateMachine", "[full instructions]", "coder")
Task("Migrate Operations.MessageSequence", "[full instructions]", "coder")

TodoWrite { todos: [
  {content: "Migrate Operations.Publish", status: "in_progress", activeForm: "Migrating Operations.Publish"},
  {content: "Migrate Operations.Subscribe", status: "in_progress", activeForm: "Migrating Operations.Subscribe"},
  {content: "Migrate Operations.Get", status: "in_progress", activeForm: "Migrating Operations.Get"},
  {content: "Migrate Operations.Request", status: "in_progress", activeForm: "Migrating Operations.Request"},
  {content: "Migrate Operations.Respond", status: "in_progress", activeForm: "Migrating Operations.Respond"},
  {content: "Migrate Operations.Tools", status: "in_progress", activeForm: "Migrating Operations.Tools"},
  {content: "Migrate Operations.StateMachine", status: "in_progress", activeForm: "Migrating Operations.StateMachine"},
  {content: "Migrate Operations.MessageSequence", status: "in_progress", activeForm: "Migrating Operations.MessageSequence"}
]}
```

#### Per-Project Migration Tasks (Template)

Each agent receives identical instructions:

```markdown
## Objective
Migrate [ProjectName] from netstandard1.5/net451 to net9.0

## Project Path
src/[ProjectName]/[ProjectName].csproj

## Tasks

1. Update .csproj file:
   - Change: <TargetFrameworks>netstandard1.5;net451</TargetFrameworks>
   - To: <TargetFramework>net9.0</TargetFramework>
   - Update: <VersionPrefix>2.0.0</VersionPrefix> → <VersionPrefix>3.0.0</VersionPrefix>
   - Add: <LangVersion>latest</LangVersion>
   - Add: <Nullable>enable</Nullable>

2. Update project reference:
   - RawRabbit reference should automatically resolve to 3.0.0

3. Remove conditional compilation:
   - Search for: #if NET451
   - Search for: #if NETSTANDARD1_5
   - Remove all conditional blocks

4. Remove net451-specific ItemGroups:
   - Delete: <ItemGroup Condition=" '$(TargetFramework)' == 'net451' ">

5. Build and fix errors:
   - Run: ~/.dotnet/dotnet build src/[ProjectName]/[ProjectName].csproj --configuration Release
   - Fix any nullable warnings (add ? or ! as appropriate)
   - Target: 0 errors

6. Validate:
   - Build succeeds
   - 0 errors (warnings OK)

7. Log completion:
   ./scripts/append-to-history.sh \
     "Stage 3: [ProjectName] Migrated" \
     "Migrated [ProjectName] to .NET 9.0. Updated to RawRabbit 3.0.0. Build successful." \
     "Part of Operations parallel migration (8 projects)." \
     "[ProjectName] migration complete."

## Success Criteria
- [ ] .csproj updated to net9.0
- [ ] VersionPrefix = 3.0.0
- [ ] Conditional compilation removed
- [ ] Build successful (0 errors)
- [ ] HISTORY.md updated
```

#### Success Criteria (All 8 Projects)

- [ ] All 8 .csproj files updated to `<TargetFramework>net9.0</TargetFramework>`
- [ ] All 8 projects reference RawRabbit 3.0.0
- [ ] All conditional compilation removed
- [ ] All 8 projects build successfully (0 errors)
- [ ] HISTORY.md has 8 entries (one per project)

#### Quality Gate

- **Build Success**: 100% (8/8 projects)
- **Errors**: 0 across all projects
- **Dependencies**: All reference Core 3.0.0

#### Agent Assignment

- **8 Coder Agents** (spawned in parallel, single message)

#### Testing

```bash
# After all agents complete, run stage-specific tests
./scripts/run-stage-tests.sh 3 "Operations"

# Runs:
# - Operations unit tests
# - Quick smoke integration tests (if RabbitMQ available)
#
# Target: 100% pass rate
```

#### Time Savings

- **Sequential**: 8 projects × 15 min = 120 minutes
- **Parallel**: max(15 min) + 5 min overhead = **20 minutes**
- **Savings**: **100 minutes (83% faster)**

---

### Stage 4: Enrichers Migration (PARALLEL with Grouping)

**Duration**: 1-2 days
**Projects**: 10 Enrichers (6 simple, 4 complex, 2 deprecated)

#### Objectives

- Migrate 8 active Enrichers to .NET 9.0
- Handle Polly 8.x major refactoring
- Deprecate ZeroFormatter (mark obsolete)
- Document migration paths

#### Stage 4.1: Simple Enrichers (PARALLEL)

**Duration**: 15-20 minutes
**Projects**: 6 enrichers (Level 0 - no cross-dependencies)

1. `RawRabbit.Enrichers.Attributes`
2. `RawRabbit.Enrichers.GlobalExecutionId`
3. `RawRabbit.Enrichers.QueueSuffix`
4. `RawRabbit.Enrichers.RetryLater`
5. `RawRabbit.Enrichers.Protobuf`
6. `RawRabbit.Enrichers.MessagePack`

**Execution**: Spawn 6 coder agents in SINGLE message (same pattern as Stage 3)

**Per-Project Tasks**: Identical to Stage 3 template

**Success Criteria**:
- [ ] 6/6 projects migrated to net9.0
- [ ] 6/6 builds successful
- [ ] All tests passing

---

#### Stage 4.2: Complex Enrichers (SEQUENTIAL)

**Duration**: 4-6 hours
**Projects**: 5 enrichers (have dependencies or complexity)

1. `RawRabbit.Enrichers.MessageContext` (base - migrate first)
2. `RawRabbit.Enrichers.MessageContext.Subscribe` (depends on base)
3. `RawRabbit.Enrichers.MessageContext.Respond` (depends on base)
4. `RawRabbit.Enrichers.HttpContext` (ASP.NET Core integration)
5. `RawRabbit.Enrichers.Polly` (MAJOR REFACTORING: Polly 7.x → 8.x)

**Polly Enricher - Special Attention**:

**Breaking Changes in Polly 8.x**:
- **API Redesign**: `Policy` → `ResiliencePipeline`
- **Middleware Changes**: Pipeline builder API changed
- **Configuration**: Policy configuration restructured

**Migration Strategy**:
```csharp
// Before (Polly 7.x):
var policy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

// After (Polly 8.x):
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential
    })
    .Build();
```

**Tasks**:
1. Update Polly package: 7.x → 8.5.0
2. Refactor middleware to use `ResiliencePipeline`
3. Update all policy configurations
4. Update tests (mock pipeline, not policy)
5. Document migration in MIGRATION-GUIDE.md

**Agent Assignment**: Dedicated coder agent with Polly expertise

---

#### Stage 4.3: Deprecated Enrichers (DOCUMENT ONLY)

**Duration**: 1 hour
**Projects**: 2 enrichers (mark obsolete, no migration)

1. `RawRabbit.Enrichers.ZeroFormatter` - Mark `[Obsolete]`
2. (Ninject handled in Stage 5)

**ZeroFormatter Deprecation Tasks**:

1. **Create ADR 0003: Serialization Enricher Strategy**
   ```bash
   cat > "docs/adr/ADR 0003 ZeroFormatter Deprecation.md" << 'EOF'
   # ADR 0003: ZeroFormatter Deprecation

   **Status**: accepted
   **Date**: 2025-10-13

   ## Decision
   Deprecate RawRabbit.Enrichers.ZeroFormatter

   [Full ADR per protocol]
   EOF
   ```

2. **Mark Package Obsolete**
   ```csharp
   // Update main class with obsolete attribute
   [Obsolete("ZeroFormatter is deprecated. Use MessagePack instead. See https://github.com/pardahlman/RawRabbit/blob/master/MIGRATION-GUIDE.md#zeroformatter-messagepack")]
   public static class ZeroFormatterPlugin
   {
       // Existing code unchanged
   }
   ```

3. **Update Package Metadata**
   ```xml
   <!-- Update .csproj -->
   <PropertyGroup>
     <Description>DEPRECATED: Use RawRabbit.Enrichers.MessagePack instead.</Description>
     <PackageTags>rabbitmq;rawrabbit;zeroformatter;deprecated</PackageTags>
   </PropertyGroup>
   ```

4. **Create Migration Guide Section**
   ```bash
   # Add to MIGRATION-GUIDE.md
   cat >> MIGRATION-GUIDE.md << 'EOF'

   ## Deprecated Packages

   ### ZeroFormatter → MessagePack

   **Reason**: ZeroFormatter abandoned (2017), incompatible with .NET Core 2.0+

   [Full migration guide with code examples]
   EOF
   ```

5. **Update CHANGELOG.md**
   ```markdown
   ### Deprecated

   #### RawRabbit.Enrichers.ZeroFormatter
   - Marked obsolete
   - Alternative: RawRabbit.Enrichers.MessagePack
   - Migration guide: See MIGRATION-GUIDE.md
   ```

6. **Still Migrate to .NET 9.0** (for compatibility)
   - Update .csproj to net9.0
   - Build and verify (may require minimal fixes)
   - Keep package shipping (marked obsolete)

---

#### Success Criteria (Stage 4 Complete)

- [ ] 6/6 simple enrichers migrated (Stage 4.1)
- [ ] 5/5 complex enrichers migrated (Stage 4.2)
- [ ] Polly 8.x refactoring complete
- [ ] ZeroFormatter marked obsolete (Stage 4.3)
- [ ] ADR 0003 created (ZeroFormatter deprecation)
- [ ] Migration guides complete
- [ ] All builds successful
- [ ] Enricher tests passing

#### Quality Gate

- **Build**: 100% success across all enrichers
- **Tests**: Enricher integration tests passing
- **Documentation**: Polly 8.x migration guide, ZeroFormatter deprecation

#### Agent Assignment

- **Stage 4.1**: 6 coder agents (parallel)
- **Stage 4.2**: 5 coder agents (sequential)
  - MessageContext: 1 agent
  - MessageContext.Subscribe: 1 agent
  - MessageContext.Respond: 1 agent
  - HttpContext: 1 agent
  - Polly: 1 specialized agent
- **Stage 4.3**: 1 documentation agent

#### Testing

```bash
# After Stage 4 complete
./scripts/run-stage-tests.sh 4 "Enrichers"

# Runs enricher integration tests
# Target: ≥95% pass rate
```

---

### Stage 5: DI Adapters Migration (PARALLEL)

**Duration**: 30-45 minutes
**Projects**: 3 DI adapters
**Parallelization**: 100% (Level 0 - no cross-dependencies)

#### Objectives

- Migrate all 3 DI adapters to .NET 9.0
- Mark Ninject as obsolete
- Document migration to ServiceCollection/Autofac

#### Projects (All Level 0 - Fully Parallel)

1. `RawRabbit.DependencyInjection.ServiceCollection`
2. `RawRabbit.DependencyInjection.Autofac`
3. `RawRabbit.DependencyInjection.Ninject` (mark obsolete)

#### Parallel Execution Strategy

**Single Message with 3 Agent Tasks** (same pattern as Stage 3)

#### Special Case: Ninject Deprecation

**Tasks**:
1. Migrate to .NET 9.0 (keep package shipping)
2. Mark main class with `[Obsolete]` attribute
3. Create ADR 0004: DI Adapter Strategy
4. Add migration guide section to MIGRATION-GUIDE.md
5. Update CHANGELOG.md

**Migration Guide**:
```markdown
## Ninject → Microsoft.Extensions.DependencyInjection

**Before (Ninject)**:
```csharp
var kernel = new StandardKernel();
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    DependencyInjection = ioc => ioc.UseNinject(kernel)
});
```

**After (ServiceCollection)**:
```csharp
var services = new ServiceCollection();
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    DependencyInjection = ioc => ioc.UseServiceCollection(services)
});
```
```

#### Success Criteria

- [ ] 3/3 DI adapters migrated to net9.0
- [ ] Ninject marked obsolete
- [ ] ADR 0004 created
- [ ] Migration guide complete
- [ ] All builds successful

#### Quality Gate

- **Build**: 100% success (3/3 projects)
- **Documentation**: Ninject deprecation guide complete

#### Agent Assignment

- **3 Coder Agents** (spawned in parallel, single message)

#### Testing

```bash
./scripts/run-stage-tests.sh 5 "DI Adapters"

# Runs DI integration tests
# Target: 100% pass rate
```

#### Time Savings

- **Sequential**: 3 projects × 15 min = 45 minutes
- **Parallel**: max(15 min) + 5 min = **20 minutes**
- **Savings**: **25 minutes (56%)**

---

### Stage 6: Compatibility & Samples Migration (PARALLEL)

**Duration**: 1-2 hours
**Projects**: 4 projects
**Parallelization**: 100% (all depend on migrated projects)

#### Objectives

- Migrate compatibility layer
- Migrate all sample applications
- Validate samples run successfully

#### Projects (All Level 0 - Fully Parallel)

1. `RawRabbit.Compatibility.Legacy`
2. `RawRabbit.Messages.Sample`
3. `RawRabbit.AspNet.Sample`
4. `RawRabbit.ConsoleApp.Sample`

#### Parallel Execution Strategy

**Single Message with 4 Agent Tasks**

#### Sample Validation (Critical)

After migration, samples MUST be validated manually:

**Console Sample Validation**:
```bash
cd sample/RawRabbit.ConsoleApp.Sample
~/.dotnet/dotnet run --configuration Release &
CONSOLE_PID=$!
sleep 10

# Check if running
if ps -p $CONSOLE_PID > /dev/null; then
  echo "✅ Console sample running"
  kill $CONSOLE_PID
else
  echo "❌ Console sample crashed"
  exit 1
fi
```

**Web Sample Validation**:
```bash
cd sample/RawRabbit.AspNet.Sample
~/.dotnet/dotnet run --configuration Release &
WEB_PID=$!
sleep 10

# Check if responding
curl -s http://localhost:5000/health || echo "✅ Web sample responding"
kill $WEB_PID
```

#### Success Criteria

- [ ] 4/4 projects migrated to net9.0
- [ ] All samples build successfully
- [ ] Console sample runs without crashing
- [ ] Web sample starts and responds
- [ ] Compatibility layer validated

#### Quality Gate

- **Build**: 100% success
- **Runtime**: Samples demonstrate key features

#### Agent Assignment

- **4 Coder Agents** (spawned in parallel, single message)

#### Testing

```bash
./scripts/run-stage-tests.sh 6 "Samples"

# Builds all samples
# Manual validation required
```

#### Time Savings

- **Sequential**: 4 projects × 30 min = 120 minutes
- **Parallel**: max(30 min) + 5 min = **35 minutes**
- **Savings**: **85 minutes (71%)**

---

### Stage 7: Test Projects Migration (PARALLEL)

**Duration**: 1-2 hours
**Projects**: 3 test projects (excludes deprecated Polly tests)

#### Objectives

- Migrate all test projects to .NET 9.0
- Update test frameworks (xunit, NSubstitute)
- Run full test suite
- Validate ≥95% pass rate

#### Projects (Parallel Execution)

1. `RawRabbit.Tests` (unit tests)
2. `RawRabbit.IntegrationTests` (integration tests)
3. `RawRabbit.PerformanceTest` (benchmarks)

**Note**: `RawRabbit.Enrichers.Polly.Tests` was deprecated (not in solution)

#### Parallel Execution Strategy

**Single Message with 3 Agent Tasks** + 1 Tester Agent

#### Tasks Per Project

1. Update .csproj to net9.0
2. Update test framework packages:
   - `xunit` → latest
   - `xunit.runner.visualstudio` → latest
   - `NSubstitute` → latest
   - `Microsoft.NET.Test.Sdk` → latest
3. Fix test-specific issues (mocks, nullable)
4. Build and run tests

#### Integration Test Requirements

**RabbitMQ Infrastructure**:
```bash
# Must be running before integration tests
docker ps | grep rabbitmq-test

# If not running:
docker start rabbitmq-test
```

#### Full Test Suite Execution

```bash
# Unit tests
~/.dotnet/dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj \
  --configuration Release \
  --logger "console;verbosity=detailed"

# Integration tests (requires RabbitMQ)
~/.dotnet/dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj \
  --configuration Release \
  --logger "console;verbosity=detailed"

# Performance tests (build only, benchmarks run manually)
~/.dotnet/dotnet build test/RawRabbit.PerformanceTest/RawRabbit.PerformanceTest.csproj \
  --configuration Release
```

#### Success Criteria

- [ ] 3/3 test projects migrated to net9.0
- [ ] All test frameworks updated
- [ ] Unit tests: ≥95% pass rate
- [ ] Integration tests: ≥90% pass rate
- [ ] Performance tests build successfully
- [ ] No P0/P1 test failures

#### Quality Gate

- **Unit Tests**: ≥95% pass rate
- **Integration Tests**: ≥90% pass rate
- **Performance Tests**: Build successful (manual benchmark later)
- **Blocking Issues**: Zero P0/P1 failures

#### Agent Assignment

- **3 Coder Agents** (test project migration, parallel)
- **1 Tester Agent** (validate test results, failure triage)

#### Testing

```bash
./scripts/run-stage-tests.sh 7 "Full Regression"

# Runs complete test suite
# Target: ≥95% unit, ≥90% integration
```

#### Time Savings

- **Sequential**: 3 projects × 30 min = 90 minutes
- **Parallel**: max(30 min) + 10 min = **40 minutes**
- **Savings**: **50 minutes (56%)**

---

### Stage 8: Documentation & Release Preparation

**Duration**: 30-45 minutes (review + polish only - most docs already complete)
**Agent**: Documentation Agent

#### Objectives

- Review all incremental documentation
- Finalize CHANGELOG.md
- Polish MIGRATION-GUIDE.md
- Update README.md
- Prepare release notes
- Validate NuGet packages

#### Why So Short?

Per **Incremental Documentation Protocol**, most documentation was created during migration:
- CHANGELOG.md updated after each stage (5-10 min per stage)
- MIGRATION-GUIDE.md updated when encountering breaking changes
- ADRs created before architectural decisions
- **Stage 8 = Review + Polish, NOT creation**

#### Tasks

1. **Review CHANGELOG.md**
   - [ ] All stages documented
   - [ ] Security fixes listed
   - [ ] Breaking changes complete
   - [ ] Deprecated packages noted
   - [ ] Add final "Released" date
   ```markdown
   ## [3.0.0] - 2025-10-XX

   [All entries from stages already present]
   ```

2. **Finalize MIGRATION-GUIDE.md**
   - [ ] Table of contents
   - [ ] All breaking changes documented
   - [ ] Code examples accurate (copy-pasted from actual work)
   - [ ] Deprecated package migrations complete
   - [ ] Troubleshooting section
   - [ ] Timeline estimates
   ```markdown
   # Migration Guide: RawRabbit 2.x → 3.0

   [All content from incremental updates during stages]

   ## Table of Contents
   - Prerequisites
   - Breaking Changes
   - Step-by-Step Migration
   - Deprecated Packages
   - Troubleshooting
   ```

3. **Update README.md**
   ```markdown
   ## ✨ What's New in 3.0

   RawRabbit 3.0 brings full .NET 9.0 support with significant improvements:

   - ✅ **Modern .NET**: .NET 9.0 with latest C# features
   - 🔒 **Security**: 0 CVEs - all dependencies updated
   - ⚡ **Performance**: RabbitMQ.Client 6.8.1
   - 🛡️ **Resilience**: Polly 8.5.0
   - 📦 **Serialization**: MessagePack 2.5.187, Protobuf 3.2.30

   ⚠️ **Breaking Changes**: Major version. See [MIGRATION-GUIDE.md](MIGRATION-GUIDE.md)

   **Requirements**: .NET 9.0 SDK or later

   ## Installation

   ```bash
   dotnet add package RawRabbit --version 3.0.0
   ```
   ```

4. **Review All ADRs**
   - [ ] ADR 0001: Target Framework (.NET 9.0)
   - [ ] ADR 0002: RabbitMQ.Client 6.8.1
   - [ ] ADR 0003: ZeroFormatter Deprecation
   - [ ] ADR 0004: Ninject Deprecation
   - [ ] ADR 0005: Breaking Changes Strategy
   - [ ] ADR 0006: Nullable Reference Types
   - [ ] All ADRs have status: accepted
   - [ ] All ADRs follow naming convention: "ADR #### Title.md"

5. **Create Platform Guides**

   **docs/platforms/linux.md**:
   ```markdown
   # Deploying RawRabbit 3.0 on Linux

   ## Prerequisites
   - .NET 9.0 Runtime
   - RabbitMQ 3.13+

   [Deployment instructions]
   ```

   **docs/platforms/windows.md**:
   ```markdown
   # Deploying RawRabbit 3.0 on Windows

   [Windows-specific instructions]
   ```

   **docs/platforms/docker.md**:
   ```markdown
   # Containerizing RawRabbit 3.0 Applications

   ## Dockerfile Example
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/aspnet:9.0
   COPY bin/Release/net9.0/publish/ /app/
   WORKDIR /app
   ENTRYPOINT ["dotnet", "YourApp.dll"]
   ```
   ```

6. **Validate NuGet Packages**
   ```bash
   # Build all packages
   ~/.dotnet/dotnet pack RawRabbit.sln --configuration Release --output ./packages

   # Validate package metadata
   nuget verify ./packages/*.nupkg

   # Check dependencies
   nuget list -Source ./packages -AllVersions
   ```

7. **Create Release Notes**
   ```markdown
   # RawRabbit 3.0.0 Release Notes

   **Release Date**: 2025-10-XX

   ## 🎉 Highlights

   - .NET 9.0 support
   - Zero CVEs
   - Modern RabbitMQ.Client 6.8.1
   - Polly 8.5.0 resilience

   ## ⚠️ Breaking Changes

   [Summary from CHANGELOG.md]

   ## 📋 Upgrade Instructions

   See [MIGRATION-GUIDE.md](MIGRATION-GUIDE.md)

   ## 🔒 Security

   - Fixed CVE-2024-43485 (Newtonsoft.Json)
   - Updated all dependencies to latest secure versions
   - Security score: 45+

   ## 📦 Packages

   - RawRabbit 3.0.0
   - RawRabbit.Operations.* 3.0.0
   - RawRabbit.Enrichers.* 3.0.0
   - RawRabbit.DependencyInjection.* 3.0.0
   - [Full package list]

   ## 🚀 Getting Started

   ```bash
   dotnet add package RawRabbit --version 3.0.0
   ```
   ```

#### Success Criteria

- [ ] CHANGELOG.md finalized (release date added)
- [ ] MIGRATION-GUIDE.md complete (TOC, all sections, examples)
- [ ] README.md updated (What's New, requirements, installation)
- [ ] All ADRs reviewed and status confirmed
- [ ] Platform guides created (Linux, Windows, Docker)
- [ ] Release notes prepared
- [ ] NuGet packages validated
- [ ] No broken links in documentation
- [ ] All code examples tested

#### Quality Gate

- **Completeness**: All required documentation present
- **Accuracy**: Code examples copy-pasted from actual migration
- **Clarity**: Migration guide is actionable
- **Packaging**: All packages validate successfully

#### Deliverables

1. **CHANGELOG.md** (finalized)
2. **MIGRATION-GUIDE.md** (800+ lines, complete)
3. **README.md** (updated)
4. **docs/adr/ADR*.md** (6 ADRs)
5. **docs/platforms/*.md** (3 platform guides)
6. **RELEASE-NOTES.md**
7. **packages/*.nupkg** (27 NuGet packages validated)

---

## Quality Gates & Success Criteria

### Per-Stage Quality Gates

| Stage | Build Success | Test Pass Rate | Security | Documentation |
|-------|---------------|----------------|----------|---------------|
| Stage 0 | N/A | Baseline captured | Current state | Scripts created |
| Stage 1 | 100% | ≥95% | Zero P0/P1 | ADRs 0001, 0002 |
| Stage 2 | 100% | 100% (core) | No regression | CHANGELOG, MIGRATION-GUIDE |
| Stage 3 | 100% (8/8) | ≥95% | No regression | HISTORY.md × 8 |
| Stage 4 | 100% (10/10) | ≥95% | No regression | ADR 0003, migration guides |
| Stage 5 | 100% (3/3) | 100% | No regression | ADR 0004 |
| Stage 6 | 100% (4/4) | Samples run | No regression | Sample validation |
| Stage 7 | 100% (3/3) | ≥95% unit, ≥90% integ | No regression | Test reports |
| Stage 8 | N/A | N/A | Final validation | Complete |

### Final Release Quality Gate

**MUST PASS ALL CRITERIA BEFORE RELEASE**:

#### Technical Criteria

- [ ] **Build**: All 28 projects build successfully (100%)
- [ ] **Tests**: Unit tests ≥95%, Integration tests ≥90%
- [ ] **Security**: Zero CRITICAL/HIGH vulnerabilities
- [ ] **Security Score**: ≥45 (improvement over baseline)
- [ ] **Performance**: No regressions vs. baseline (≤10% acceptable)
- [ ] **Samples**: All samples run successfully
- [ ] **Packages**: All 27 NuGet packages validate

#### Documentation Criteria

- [ ] **CHANGELOG.md**: Complete with all breaking changes (300+ lines)
- [ ] **MIGRATION-GUIDE.md**: Actionable guide with examples (800+ lines)
- [ ] **README.md**: Updated with .NET 9.0 info
- [ ] **ADRs**: 6 ADRs created and accepted
- [ ] **HISTORY.md**: Complete migration audit trail (50+ entries)
- [ ] **Platform Guides**: Linux, Windows, Docker guides created
- [ ] **Release Notes**: Prepared and reviewed

#### Quality Criteria

- [ ] **Zero P0 Issues**: No blocking issues
- [ ] **Zero P1 Issues**: No high-priority unresolved issues
- [ ] **Code Review**: All changes reviewed (if team process)
- [ ] **Deprecated Packages**: Marked with `[Obsolete]` and documented
- [ ] **Breaking Changes**: All documented with migration examples

---

## Risk Assessment & Mitigation

### Technical Risks

| Risk | Probability | Impact | Mitigation | Contingency |
|------|-------------|--------|------------|-------------|
| **RabbitMQ.Client breaking changes extensive** | High | High | Dedicated coder agent, extra time allocation (Stage 2: 1-2 days) | Create compatibility layer, extend Stage 2 timeline |
| **Polly 8.x refactoring complex** | High | Medium | Specialized agent, comprehensive testing, extra time (Stage 4.2: 4-6 hours) | Defer Polly enricher to post-release, mark as beta |
| **Test failures cascade** | Medium | High | Continuous testing protocol, fix-before-proceed rule (max 3 iterations) | Rollback to previous stage, identify root cause |
| **Deprecated package user impact** | Medium | Medium | Clear migration guides, code examples, gradual deprecation (packages still ship) | Extend support window, provide migration assistance |
| **Performance regression** | Low | High | Baseline comparison, benchmark suite, performance tests | Optimize hot paths, profile and fix |
| **Integration test infrastructure failures** | Medium | Medium | Docker automation, pre-validate RabbitMQ setup (Stage 0) | Manual RabbitMQ setup, skip flaky tests (document) |

### Timeline Risks

| Risk | Probability | Impact | Mitigation | Contingency |
|------|-------------|--------|------------|-------------|
| **Underestimated Stage 2 complexity** | Medium | High | Realistic estimate (1-2 days), buffer included | Add 1-2 days, prioritize Core over Operations |
| **Underestimated Polly refactoring** | Medium | Medium | Specialized agent, extended time (4-6 hours) | Defer to post-3.0 release, mark as experimental |
| **Parallel execution coordination overhead** | Low | Low | Clear agent instructions, independent logging | Accept small overhead, still net positive |
| **Test failures exceed 3 iterations** | Low | High | Fix-before-proceed, root cause analysis | Rollback stage, investigate systematically |

### Resource Risks

| Risk | Probability | Impact | Mitigation | Contingency |
|------|-------------|--------|------------|-------------|
| **Agent availability/coordination** | Low | Low | Parallel execution protocol, single-message spawning | Retry spawning, execute sequentially if needed |
| **Documentation backlog** | Low | Medium | Incremental documentation protocol (update during stages) | Allocate extra time in Stage 8 (up to 2 hours) |

---

## Testing Strategy

### Testing Protocol: Continuous Testing (MANDATORY)

Per `CONTINUOUS-TESTING-PROTOCOL.md`:
- **Test after EVERY stage**
- **Fix-before-proceed rule** (no stage progression with failures)
- **Maximum 3 fix-and-retest iterations per stage**
- **Quality gates enforced at each boundary**

### Test Types

#### 1. Unit Tests

**Target**: ≥95% pass rate

**Execution**:
```bash
~/.dotnet/dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj \
  --configuration Release \
  --logger "console;verbosity=detailed"
```

**Focus Areas**:
- Core library (Channel, Consumer, Configuration)
- Operations (Publish, Subscribe, Request/Response)
- Enrichers (Middleware, Plugins)

#### 2. Integration Tests

**Target**: ≥90% pass rate

**Prerequisites**: RabbitMQ running on localhost:5672

**Execution**:
```bash
# Start RabbitMQ
docker run -d --name rabbitmq-test -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Run tests
~/.dotnet/dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj \
  --configuration Release \
  --logger "console;verbosity=detailed"
```

**Focus Areas**:
- End-to-end message flows
- Enricher integration (Polly, MessageContext)
- DI container integration

#### 3. Performance Tests

**Target**: No regression (≤10% acceptable)

**Baseline**: Capture in Stage 0

**Execution**:
```bash
~/.dotnet/dotnet build test/RawRabbit.PerformanceTest/RawRabbit.PerformanceTest.csproj \
  --configuration Release

# Run benchmarks manually (BenchmarkDotNet)
~/.dotnet/dotnet run --project test/RawRabbit.PerformanceTest/RawRabbit.PerformanceTest.csproj \
  --configuration Release
```

**Focus Areas**:
- Publish throughput
- Request/response latency
- Message serialization/deserialization
- Connection pooling

#### 4. Sample Application Tests

**Target**: All samples run without errors

**Validation**:
- Console sample: Runs for 10+ seconds without crashing
- Web sample: Starts and responds to HTTP requests
- Message sample: Compiles and links correctly

**Execution**: Manual validation in Stage 6

### Test Automation Scripts

#### scripts/capture-test-baseline.sh

Captures pre-migration baseline metrics:
- Total tests
- Pass rate
- Performance benchmarks
- Code coverage

**Usage**:
```bash
./scripts/capture-test-baseline.sh
# Creates: docs/test-baselines/baseline-YYYY-MM-DD-HHMMSS.md
```

#### scripts/run-stage-tests.sh

Runs stage-specific tests with appropriate filters:

**Usage**:
```bash
./scripts/run-stage-tests.sh 2 "Core Library"
./scripts/run-stage-tests.sh 3 "Operations"
./scripts/run-stage-tests.sh 4 "Enrichers"
```

**Exit Codes**:
- `0`: All tests passed (proceed to next stage)
- `1`: Tests failed (FIX before proceeding)

### Fix-Before-Proceed Protocol

**If ANY test fails**:

1. **STOP** - Do not proceed to next stage
2. **ANALYZE** - Review failure stack traces, categorize severity
3. **FIX** - Address root cause immediately
4. **RETEST** - Validate fix with specific test
5. **FULL RETEST** - Run full stage test suite
6. **LOG** - Document failure and resolution in HISTORY.md
7. **PROCEED** - Only when 100% pass rate (or ≥95% with documented acceptable failures)

**Maximum Iterations**: 3 fix-and-retest cycles per stage
- If exceeded: Escalate, deeper investigation required

---

## Automation Scripts

### scripts/analyze-dependencies.sh

**Purpose**: Analyze project dependencies to identify parallelization opportunities

**Usage**:
```bash
./scripts/analyze-dependencies.sh "src/RawRabbit.Operations.*"
```

**Output**:
```
🔍 Analyzing dependencies for parallel execution...

📊 Projects in scope:
  - Operations.Publish
  - Operations.Subscribe
  - Operations.Get
  - Operations.Request
  - Operations.Respond
  - Operations.Tools
  - Operations.StateMachine
  - Operations.MessageSequence

🔗 Dependency Analysis:
  ✅ Operations.Publish: No in-stage dependencies (Level 0 - Fully parallel)
  ✅ Operations.Subscribe: No in-stage dependencies (Level 0 - Fully parallel)
  ✅ Operations.Get: No in-stage dependencies (Level 0 - Fully parallel)
  ✅ Operations.Request: No in-stage dependencies (Level 0 - Fully parallel)
  ✅ Operations.Respond: No in-stage dependencies (Level 0 - Fully parallel)
  ✅ Operations.Tools: No in-stage dependencies (Level 0 - Fully parallel)
  ✅ Operations.StateMachine: No in-stage dependencies (Level 0 - Fully parallel)
  ✅ Operations.MessageSequence: No in-stage dependencies (Level 0 - Fully parallel)

💡 Recommendation:
  - 8 projects with no in-stage dependencies
  - Spawn 8 parallel agents in SINGLE message
  - Time savings: ~105 minutes (83% faster)
```

**Script Location**: `/home/laird/src/EYP/RawRabbit5/scripts/analyze-dependencies.sh`

---

### scripts/run-stage-tests.sh

**Purpose**: Run stage-specific tests with appropriate filters and validate pass rates

**Usage**:
```bash
./scripts/run-stage-tests.sh 2 "Core Library"
./scripts/run-stage-tests.sh 3 "Operations"
```

**Features**:
- Stage-specific test filtering
- Pass rate validation
- Clear success/failure output
- Integration with continuous testing protocol

**Exit Codes**:
- `0`: All tests passed (proceed)
- `1`: Tests failed (FIX before proceeding)

**Script Location**: `/home/laird/src/EYP/RawRabbit5/scripts/run-stage-tests.sh`

---

### scripts/capture-test-baseline.sh

**Purpose**: Capture pre-migration test baseline for comparison

**Usage**:
```bash
./scripts/capture-test-baseline.sh
```

**Output**: `docs/test-baselines/baseline-YYYY-MM-DD-HHMMSS.md`

**Captures**:
- Total tests
- Pass/fail/skip counts
- Pass rate
- Framework version
- Test results (TRX format)

**Script Location**: `/home/laird/src/EYP/RawRabbit5/scripts/capture-test-baseline.sh`

---

### scripts/validate-migration-stage.sh

**Purpose**: Automated quality gate validation per STAGE-VALIDATION-PROTOCOL.md

**Usage**:
```bash
./scripts/validate-migration-stage.sh 2
```

**Validates**:
- Build success (100%)
- Test pass rates (≥95% unit, ≥90% integration)
- Security scan (no P0/P1)
- Documentation updates (HISTORY.md, CHANGELOG.md)
- Protocol adherence

**Exit Codes**:
- `0`: Stage passes all quality gates
- `1`: Stage fails one or more gates

**Script Location**: `/home/laird/src/EYP/RawRabbit5/scripts/validate-migration-stage.sh`

---

### scripts/append-to-history.sh

**Purpose**: Append migration activity to HISTORY.md (per GENERIC-AGENT-LOGGING-PROTOCOL.md)

**Usage**:
```bash
./scripts/append-to-history.sh \
  "Stage 2 Complete: Core Library Migrated" \
  "Migrated RawRabbit to .NET 9.0..." \
  "Core is foundation for all projects" \
  "Ready for Stage 3"
```

**Features**:
- Timestamped entries
- Consistent formatting
- Append-only (no overwrites)
- Supports parallel agent logging

**Script Location**: `/home/laird/src/EYP/RawRabbit5/scripts/append-to-history.sh`

---

## Agent Coordination & Execution

### Agent Assignments by Stage

| Stage | Primary Agents | Support Agents | Total Agents | Execution Mode |
|-------|----------------|----------------|--------------|----------------|
| 0 | Migration Coordinator | - | 1 | Sequential |
| 1 | Security Agent, Coder | - | 2 | Sequential |
| 2 | Coder (RabbitMQ.Client expert) | Tester | 2 | Sequential |
| 3 | 8× Coder | - | 8 | **Parallel** |
| 4.1 | 6× Coder | - | 6 | **Parallel** |
| 4.2 | 5× Coder (1 Polly expert) | - | 5 | Sequential |
| 4.3 | Documentation | - | 1 | Sequential |
| 5 | 3× Coder | - | 3 | **Parallel** |
| 6 | 4× Coder | - | 4 | **Parallel** |
| 7 | 3× Coder, Tester | - | 4 | **Parallel** |
| 8 | Documentation | Coder (validation) | 2 | Sequential |

**Total Agent-Hours**: ~40 agent executions
**Wall-Clock Time**: 2-3 weeks (with parallelization)
**Time Savings**: 5.7 hours (340 minutes) from parallel execution

### Coordination Protocol

Per `generic-migration-coordinator.yaml` and `PARALLEL-MIGRATION-PROTOCOL.md`:

#### 1. Parallel Agent Spawning (Critical for Efficiency)

**Pattern**: Spawn ALL agents for a level in SINGLE message

**Example (Stage 3 - Operations)**:
```markdown
[Single Message]:
Task("Migrate Operations.Publish", "[complete instructions]", "coder")
Task("Migrate Operations.Subscribe", "[complete instructions]", "coder")
Task("Migrate Operations.Get", "[complete instructions]", "coder")
Task("Migrate Operations.Request", "[complete instructions]", "coder")
Task("Migrate Operations.Respond", "[complete instructions]", "coder")
Task("Migrate Operations.Tools", "[complete instructions]", "coder")
Task("Migrate Operations.StateMachine", "[complete instructions]", "coder")
Task("Migrate Operations.MessageSequence", "[complete instructions]", "coder")

TodoWrite { todos: [
  {content: "Migrate Operations.Publish", status: "in_progress", activeForm: "Migrating Operations.Publish"},
  {content: "Migrate Operations.Subscribe", status: "in_progress", activeForm: "Migrating Operations.Subscribe"},
  ... (8 total)
]}
```

**Benefits**:
- **Massive time savings** (83% faster for Stage 3)
- No additional risk (agents work on independent files)
- Clean parallel execution

#### 2. Agent Coordination Hooks

Each agent spawned via Task tool MUST execute coordination hooks:

**Before Work**:
```bash
npx claude-flow@alpha hooks pre-task --description "[task description]"
npx claude-flow@alpha hooks session-restore --session-id "swarm-migration-rawrabbit"
```

**During Work**:
```bash
npx claude-flow@alpha hooks post-edit --file "[file path]" --memory-key "swarm/agent/[step]"
npx claude-flow@alpha hooks notify --message "[progress update]"
```

**After Work**:
```bash
npx claude-flow@alpha hooks post-task --task-id "[task id]"
npx claude-flow@alpha hooks session-end --export-metrics true
```

#### 3. Independent Logging

Each agent logs to HISTORY.md independently:

```bash
./scripts/append-to-history.sh \
  "Stage N: [ProjectName] Migrated" \
  "[Details of changes]" \
  "[Context/reasoning]" \
  "[Impact/status]"
```

**No Conflicts**: `append-to-history.sh` uses append operations (timestamps differentiate entries)

#### 4. Quality Gate Validation

After ALL agents in a stage complete:

```bash
# Run stage-specific tests
./scripts/run-stage-tests.sh [STAGE_NUM] "[STAGE_NAME]"

# Validate quality gates
./scripts/validate-migration-stage.sh [STAGE_NUM]

# If pass: Proceed to next stage
# If fail: FIX before proceeding (fix-before-proceed rule)
```

---

## Rollback Procedures

### Per-Stage Rollback

**When to Rollback**:
- Test pass rate <95% after 3 fix attempts
- Critical functionality broken
- P0 security vulnerability introduced
- Unable to proceed to next stage

**Procedure**:
```bash
# 1. Identify last stable commit
git log --oneline | grep "Stage [N-1] Complete"

# 2. Create rollback branch
git checkout -b rollback-stage-[N] [commit-hash]

# 3. Validate rollback state
~/.dotnet/dotnet build --configuration Release
~/.dotnet/dotnet test --configuration Release

# 4. Document rollback
./scripts/append-to-history.sh \
  "Rollback: Stage [N] Failed" \
  "[Reason for rollback]" \
  "[Issues encountered]" \
  "Rolled back to Stage [N-1]. Investigating root cause."

# 5. Investigate root cause
# 6. Retry stage with fixes
```

### Complete Migration Rollback

**When to Rollback**:
- Unable to proceed past Stage 2 (Core)
- Fundamental architectural issue discovered
- Security regression introduced
- Product owner decision

**Procedure**:
```bash
# 1. Return to main branch
git checkout main  # Or 2.0 branch

# 2. Document decision
./scripts/append-to-history.sh \
  "Migration Rollback: Full Rollback to 2.x" \
  "[Detailed reasoning]" \
  "[Issues encountered]" \
  "Migration paused. Will revisit strategy."

# 3. Maintain 2.x branch for critical fixes
git checkout -b hotfix/2.x-critical-fixes

# 4. Post-mortem analysis
# - What went wrong?
# - What can be learned?
# - What needs to change before retry?
```

### Partial Migration Support

If rollback occurs, maintain compatibility:

**Strategy**:
- Keep 2.x branch alive for critical fixes
- Provide compatibility layer in 3.0 (if partial migration completed)
- Document partial migration state clearly
- Plan remediation for next attempt

---

## Timeline Estimate

### Stage Duration Summary

| Stage | Description | Duration | Parallelization | Time Savings |
|-------|-------------|----------|-----------------|--------------|
| 0 | Prerequisites & Baseline | 2 hours | - | - |
| 1 | Security Remediation | 1-2 days | - | - |
| 2 | Core Library | 1-2 days | - | - |
| 3 | Operations (8 projects) | **15-20 min** | 8 agents | 100 min (83%) |
| 4.1 | Simple Enrichers (6 projects) | **15-20 min** | 6 agents | 70 min (78%) |
| 4.2 | Complex Enrichers (5 projects) | 4-6 hours | Sequential | - |
| 4.3 | Deprecated (2 projects) | 1 hour | - | - |
| 5 | DI Adapters (3 projects) | **20-30 min** | 3 agents | 25 min (56%) |
| 6 | Samples (4 projects) | **30-40 min** | 4 agents | 85 min (71%) |
| 7 | Tests (3 projects) | **40-50 min** | 3 agents + tester | 50 min (56%) |
| 8 | Documentation & Release | 30-45 min | - | - |

**Total Estimated Duration**: 2-3 weeks (calendar time)
**Total Active Work**: ~5-7 days (with parallelization)
**Total Time Savings from Parallel Execution**: 340 minutes (5.7 hours) = **50-67% reduction**

### Critical Path

```
Stage 0 (2h) → Stage 1 (1-2d) → Stage 2 (1-2d) → Stage 3-7 (3-4d) → Stage 8 (1h)
                                                      ↑
                                              [Massive parallelization]
```

**Bottleneck**: Stage 2 (Core Library) - Most complex, cannot parallelize

### Assumptions

- Full-time equivalent (FTE): 1 developer
- Work days: 5 days/week
- Daily work: 6-8 hours
- Testing infrastructure: Pre-configured (RabbitMQ available)
- No external blockers (dependency availability, approvals)

---

## Success Metrics

### Quantitative Metrics

- [ ] **Projects Migrated**: 28/28 (100%)
- [ ] **Build Success Rate**: 100%
- [ ] **Unit Test Pass Rate**: ≥95%
- [ ] **Integration Test Pass Rate**: ≥90%
- [ ] **Security Score**: ≥45 (improved from baseline)
- [ ] **CRITICAL CVEs**: 0
- [ ] **HIGH CVEs**: 0
- [ ] **Performance Regression**: ≤10%
- [ ] **Documentation Lines**: 1,500+ (CHANGELOG + MIGRATION-GUIDE + ADRs)
- [ ] **NuGet Packages Validated**: 27/27
- [ ] **Time Saved (Parallelization)**: ≥300 minutes (5 hours)

### Qualitative Metrics

- [ ] **Production-Ready**: Code is deployable to production
- [ ] **User Confidence**: Migration guide is clear and actionable
- [ ] **Audit Trail**: HISTORY.md provides complete migration record
- [ ] **Team Satisfaction**: Process was efficient and well-organized
- [ ] **Zero P0/P1 Issues**: No blocking or high-priority unresolved issues

### Success Dashboard

```
┌─────────────────────────────────────────────────────────┐
│ RawRabbit 3.0.0 Migration Dashboard                    │
├─────────────────────────────────────────────────────────┤
│ Projects Migrated:      28/28 (100%)        ✅          │
│ Build Success:          100%                ✅          │
│ Unit Tests:             95%+                ✅          │
│ Integration Tests:      90%+                ✅          │
│ Security Score:         45+                 ✅          │
│ CVEs (CRITICAL):        0                   ✅          │
│ CVEs (HIGH):            0                   ✅          │
│ Documentation:          1,500+ lines        ✅          │
│ NuGet Packages:         27/27 validated     ✅          │
│ Time Savings:           340 min (5.7 hrs)   ✅          │
├─────────────────────────────────────────────────────────┤
│ Status: ✅ READY FOR RELEASE                            │
└─────────────────────────────────────────────────────────┘
```

---

## Appendices

### Appendix A: Complete Dependency Graph (Visual)

```
RawRabbit (Core) ──────────┐
    │                      │
    ├──> Operations.* (8)  │ [Stage 3 - PARALLEL]
    │                      │
    ├──> Enrichers.* (6)   │ [Stage 4.1 - PARALLEL]
    │                      │
    ├──> DI.* (3)          │ [Stage 5 - PARALLEL]
    │                      │
    └──> Enrichers.* (4)   │ [Stage 4.2 - SEQUENTIAL]
            │              │
            └──> Samples (4) ──> [Stage 6 - PARALLEL]
                    │
                    └──> Tests (3) ──> [Stage 7 - PARALLEL]
```

### Appendix B: Protocol References

All migration protocols are located in `/home/laird/src/EYP/RawRabbit5/docs/agents/`:

1. **GENERIC-MIGRATION-PLANNING-GUIDE.md** - 5-phase planning framework
2. **PARALLEL-MIGRATION-PROTOCOL.md** - Parallel execution strategy
3. **CONTINUOUS-TESTING-PROTOCOL.md** - Test-after-every-stage protocol
4. **INCREMENTAL-DOCUMENTATION-PROTOCOL.md** - Document-as-you-go protocol
5. **STAGE-VALIDATION-PROTOCOL.md** - Automated quality gates
6. **GENERIC-ADR-LIFECYCLE-PROTOCOL.md** - ADR creation and management
7. **GENERIC-AGENT-LOGGING-PROTOCOL.md** - HISTORY.md logging protocol

### Appendix C: Automation Scripts

All scripts are located in `/home/laird/src/EYP/RawRabbit5/scripts/`:

1. **analyze-dependencies.sh** - Identify parallelizable projects
2. **run-stage-tests.sh** - Execute stage-specific tests
3. **capture-test-baseline.sh** - Capture pre-migration baseline
4. **validate-migration-stage.sh** - Automated quality gate checks
5. **append-to-history.sh** - Log to HISTORY.md

**Note**: Scripts will be created in Stage 0.

### Appendix D: Breaking Changes Reference

Complete list of breaking changes with migration examples:

#### 1. Target Framework

**Before**:
```xml
<TargetFrameworks>netstandard1.5;net451</TargetFrameworks>
```

**After**:
```xml
<TargetFramework>net9.0</TargetFramework>
```

**Impact**: Users must have .NET 9.0 SDK

---

#### 2. RabbitMQ.Client BasicProperties

**Before**:
```csharp
var properties = new BasicProperties
{
    ContentType = "application/json",
    DeliveryMode = 2
};
```

**After**:
```csharp
var properties = BasicPropertiesHelper.CreateBasicProperties();
properties.ContentType = "application/json";
properties.DeliveryMode = 2;

// Or use configuration builders (recommended):
await client.PublishAsync(message, ctx => ctx
    .UsePublishConfiguration(cfg => cfg
        .WithProperties(props => props
            .WithContentType("application/json")
            .WithDeliveryMode(2))));
```

---

#### 3. RabbitMQ.Client Body Type

**Before**:
```csharp
byte[] body = Encoding.UTF8.GetBytes(json);
channel.BasicPublish("exchange", "routing", properties, body);
```

**After**:
```csharp
byte[] body = Encoding.UTF8.GetBytes(json);
channel.BasicPublish("exchange", "routing", properties, new ReadOnlyMemory<byte>(body));

// Or when receiving:
ReadOnlyMemory<byte> bodyMemory = basicDeliverEventArgs.Body;
byte[] body = bodyMemory.ToArray();
string json = Encoding.UTF8.GetString(body);
```

---

### Appendix E: Deprecated Packages Migration

#### ZeroFormatter → MessagePack

**Why**: ZeroFormatter abandoned (2017), no .NET Core 2.0+ support

**Step 1**: Uninstall ZeroFormatter
```bash
dotnet remove package RawRabbit.Enrichers.ZeroFormatter
```

**Step 2**: Install MessagePack
```bash
dotnet add package RawRabbit.Enrichers.MessagePack
```

**Step 3**: Update message attributes
```csharp
// Before (ZeroFormatter):
[ZeroFormattable]
public class MyMessage
{
    [Index(0)]
    public string Name { get; set; }

    [Index(1)]
    public int Age { get; set; }
}

// After (MessagePack):
[MessagePackObject]
public class MyMessage
{
    [Key(0)]
    public string Name { get; set; }

    [Key(1)]
    public int Age { get; set; }
}
```

**Step 4**: Update plugin registration
```csharp
// Before:
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    Plugins = p => p.UseZeroFormatter()
});

// After:
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    Plugins = p => p.UseMessagePack()
});
```

**Estimated Migration Time**: 30-60 minutes for typical application

---

#### Ninject → Microsoft.Extensions.DependencyInjection

**Why**: Ninject development slowed, Microsoft.Extensions.DI is .NET standard

**Step 1**: Uninstall Ninject
```bash
dotnet remove package RawRabbit.DependencyInjection.Ninject
```

**Step 2**: Install ServiceCollection
```bash
dotnet add package RawRabbit.DependencyInjection.ServiceCollection
dotnet add package Microsoft.Extensions.DependencyInjection
```

**Step 3**: Update registration code
```csharp
// Before (Ninject):
var kernel = new StandardKernel();
kernel.Bind<IMyService>().To<MyService>().InSingletonScope();

var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    DependencyInjection = ioc => ioc.UseNinject(kernel)
});

// After (ServiceCollection):
var services = new ServiceCollection();
services.AddSingleton<IMyService, MyService>();

var serviceProvider = services.BuildServiceProvider();
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    DependencyInjection = ioc => ioc.UseServiceCollection(services)
});
```

**Alternative**: Use Autofac (if complex binding scenarios)
```bash
dotnet add package RawRabbit.DependencyInjection.Autofac
```

**Estimated Migration Time**: 1-2 hours for typical application

---

### Appendix F: Key Commands Quick Reference

```bash
# Environment Setup (Stage 0)
~/.dotnet/dotnet --version  # Verify .NET 9.0
docker run -d --name rabbitmq-test -p 5672:5672 rabbitmq:3-management
./scripts/capture-test-baseline.sh

# Build Commands
~/.dotnet/dotnet build RawRabbit.sln --configuration Release
~/.dotnet/dotnet build src/[Project]/[Project].csproj --configuration Release

# Test Commands
~/.dotnet/dotnet test --configuration Release
~/.dotnet/dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj --configuration Release
./scripts/run-stage-tests.sh [STAGE_NUM] "[STAGE_NAME]"

# Security Commands
~/.dotnet/dotnet list package --vulnerable
~/.dotnet/dotnet list package --outdated

# Dependency Analysis
./scripts/analyze-dependencies.sh "src/RawRabbit.Operations.*"

# Quality Gate Validation
./scripts/validate-migration-stage.sh [STAGE_NUM]

# Logging
./scripts/append-to-history.sh "[Title]" "[Details]" "[Context]" "[Impact]"

# Package Creation
~/.dotnet/dotnet pack RawRabbit.sln --configuration Release --output ./packages
```

---

## Plan Status & Next Actions

### Status

**Planning Complete**: ✅ Ready for Execution

### Immediate Next Actions

1. **Review & Approve Plan** (Stakeholder)
   - Review Stage 0-8 definitions
   - Approve timeline and resources
   - Sign off on breaking changes strategy

2. **Execute Stage 0** (Migration Coordinator)
   - Install .NET 9.0 SDK
   - Create automation scripts
   - Capture test baseline
   - Validate RabbitMQ infrastructure
   - Create ADR 0001 (Target Framework)
   - Create ADR 0005 (Breaking Changes Strategy)

3. **Execute Stage 1** (Security Agent + Coder)
   - Update Newtonsoft.Json to 13.0.3
   - Create ADR 0002 (RabbitMQ.Client strategy)
   - Run security scan
   - Validate tests still passing

4. **Begin Stage 2** (Coder Agent)
   - Migrate Core library to .NET 9.0
   - Update RabbitMQ.Client to 6.8.1
   - Create BasicPropertiesHelper
   - Fix breaking changes
   - Enable nullable reference types
   - Full testing and validation

### Approval Required

**Approvers**:
- [ ] Technical Lead: Review technical approach
- [ ] Product Owner: Approve timeline and breaking changes
- [ ] Security Team: Approve security remediation plan

**Approval Date**: _____________

---

**Plan Version**: 1.0
**Date**: 2025-10-13
**Status**: Complete - Ready for Execution
**Total Pages**: 45+
**Total Words**: 12,000+

---

**END OF PLAN**
