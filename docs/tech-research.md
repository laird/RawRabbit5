# RawRabbit 3.0 Technology Research Summary

**Date**: 2025-10-13
**Author**: Architecture Team
**Status**: Research Complete

This document summarizes the technology research conducted for RawRabbit 3.0 modernization. All findings inform the Architecture Decision Records (ADRs) located in `docs/adr/`.

## Executive Summary

RawRabbit 3.0 requires modernization from legacy .NET Framework/netstandard1.5 to modern .NET. Research identified optimal technology choices across six key decision areas:

| Decision Area | Current (2.x) | Recommended (3.0) | Rationale |
|--------------|---------------|-------------------|-----------|
| **Target Framework** | netstandard1.5, net451 | .NET 9 (STS) | Same EOL as .NET 8 (Nov 2026), latest features |
| **RabbitMQ.Client** | 5.0.1 (2017) | 7.1.2 (2024) | Async/await, modern .NET, performance |
| **JSON Serialization** | Newtonsoft.Json 10.0.1 | System.Text.Json | 50%+ faster, built-in, zero dependencies |
| **Breaking Changes** | N/A | Full breaking changes | Major version, clean async architecture |
| **Deprecated Packages** | ZeroFormatter, Ninject | Remove both | Unmaintained, security risk |
| **Nullable Reference Types** | Disabled | Enabled globally | Type safety, ecosystem alignment |

## 1. Target Framework Analysis

### Research Question
Which .NET target framework provides the best balance of support lifecycle, features, and stability?

### Frameworks Evaluated

#### .NET 8 (LTS)
- **Release**: November 2023
- **End of Support**: November 10, 2026 (3-year LTS)
- **Support Status**: Long-Term Support (LTS)
- **Maturity**: 1 year in production, widely adopted

**Strengths**:
- LTS designation (traditionally 3-year support)
- Production-proven, stable
- Full C# 12 support
- Excellent performance improvements over .NET 6/7
- Strong ecosystem maturity

**Weaknesses**:
- Support ends in 16 months (Nov 2026)
- Lacks newest .NET 9 features
- Users will need to upgrade soon after migration

**Production Readiness**: Excellent (enterprise-ready)

#### .NET 9 (STS)
- **Release**: November 2024
- **End of Support**: November 10, 2026 (2-year STS, extended from 18 months)
- **Support Status**: Standard-Term Support (STS)
- **Maturity**: 11 months in production

**Strengths**:
- **Critical**: Same EOL date as .NET 8 due to Microsoft's 2025 policy change
- Latest runtime optimizations and performance improvements
- C# 13 language features
- Improved async/await performance (beneficial for messaging)
- Better JSON serialization performance
- Most modern development experience

**Weaknesses**:
- STS designation may concern conservative enterprises
- Less production validation than .NET 8 (acceptable for major version)
- Shorter track record

**Production Readiness**: Good (stable, but newer)

**Key Insight**: Microsoft's 2025 policy change extending STS releases from 18 to 24 months means .NET 8 (LTS) and .NET 9 (STS) reach end-of-support on the **exact same date**: November 10, 2026. This eliminates the traditional LTS advantage.

#### .NET Standard 2.0/2.1
- **Status**: No new versions, maintenance mode
- **Compatibility**: Broad (.NET Framework, Mono, Xamarin)

**Strengths**:
- Maximum compatibility across platforms

**Weaknesses**:
- Locks out modern language features (C# 12, 13)
- No async/await improvements from modern .NET
- Misses runtime performance improvements
- Not aligned with "future of .NET" direction
- Still requires .NET 6+ for modern hosting

**Production Readiness**: Stable but legacy

### Framework Comparison Matrix

| Feature | .NET 8 (LTS) | .NET 9 (STS) | .NET Standard 2.0 |
|---------|--------------|--------------|-------------------|
| **Support End Date** | Nov 10, 2026 | Nov 10, 2026 | N/A (maintenance) |
| **Support Duration Remaining** | 16 months | 16 months | N/A |
| **C# Version** | 12 | 13 | 7.3 |
| **Async Performance** | Excellent | Better | Good |
| **JSON Performance** | Excellent | Better | Good |
| **Runtime Optimizations** | Latest - 1 | Latest | Limited |
| **Production Validation** | 1 year | 11 months | Many years |
| **Security Patches** | Yes | Yes | Limited |
| **New Features** | No | Yes | No |

### Decision: .NET 9 (STS)

**Rationale**: Since .NET 8 and .NET 9 have identical end-of-support dates, choosing .NET 9 provides latest features and performance with no additional migration burden. The traditional "choose LTS for stability" argument is nullified.

**See**: [ADR 0001 Target Framework Selection](adr/ADR%200001%20Target%20Framework%20Selection.md)

---

## 2. RabbitMQ.Client Version Analysis

### Research Question
Which version of RabbitMQ.Client should RawRabbit target, given major breaking changes between versions?

### Versions Evaluated

#### RabbitMQ.Client 5.x (Current: 5.0.1)
- **Release**: 2017 (8 years old)
- **Status**: Obsolete, no longer maintained
- **API Style**: Synchronous
- **.NET Compatibility**: .NET Framework 4.5+, netstandard1.5+

**Critical Issues**:
- Incompatible with .NET 9 (cannot compile)
- No security patches for 8 years
- Missing performance improvements
- Blocks modernization entirely

**Verdict**: Not viable

#### RabbitMQ.Client 6.x (Current: 6.8.1)
- **Release**: 2020
- **Status**: Stable, maintenance mode
- **API Style**: Synchronous
- **.NET Compatibility**: .NET Framework 4.6.1+, .NET Standard 2.0+

**Key Changes from 5.x**:
- Message payloads: `byte[]` → `ReadOnlyMemory<byte>`
- Requires .NET Standard 2.0 minimum
- Improved memory management using System.Memory
- Better throughput and reduced memory footprint

**Strengths**:
- Well-tested, production-proven
- Fewer breaking changes than 7.x (incremental step)
- Stable API surface
- Large production deployment base

**Weaknesses**:
- Still synchronous API (no async/await)
- Will require 7.x migration eventually
- Missing latest performance optimizations
- Not taking advantage of modern .NET patterns

**Migration Complexity**: Medium (memory model changes)

#### RabbitMQ.Client 7.x (Current: 7.1.2) ⭐
- **Release**: 2024
- **Status**: Active development, officially recommended
- **API Style**: Fully asynchronous (Task-based async pattern)
- **.NET Compatibility**: .NET 6+

**Key Changes from 5.x/6.x**:
1. **Async/Await Model**: All methods converted to async with `Async` suffix
   - `BasicPublish()` → `BasicPublishAsync()`
   - Returns `Task` for all operations
2. **Interface Renaming**: `IModel` → `IChannel`
3. **Consumer Changes**: `IBasicConsumer` → `IAsyncBasicConsumer`
4. **BasicProperties**: `CreateBasicProperties()` removed, use `new BasicProperties()`
5. **Publisher Confirmations**: Built-in efficient tracking
6. **Memory Model**: `ReadOnlyMemory<byte>` for message payloads

**Strengths**:
- Modern async/await programming model
- Significant performance improvements (memory + throughput)
- Latest features and active development
- Built-in publisher confirmation tracking
- Future-proof (official recommendation)
- Aligns with .NET 9 async patterns
- No follow-up migration needed (8.x doesn't exist)

**Weaknesses**:
- Complete rewrite required for RawRabbit middleware
- Breaking changes to all publish/subscribe operations
- Consumer model completely different
- Largest migration effort

**Migration Complexity**: High (complete async conversion)

### Breaking Changes Comparison

| Change Category | 5.x → 6.x | 5.x → 7.x | 6.x → 7.x |
|----------------|-----------|-----------|-----------|
| **API Style** | Sync → Sync | Sync → Async | Sync → Async |
| **Memory Model** | byte[] → ReadOnlyMemory | byte[] → ReadOnlyMemory | Same |
| **Interfaces** | Same | IModel → IChannel | IModel → IChannel |
| **Method Names** | Same | +Async suffix | +Async suffix |
| **Consumer Model** | Same | Sync → Async | Sync → Async |
| **Framework Requirement** | .NET Standard 2.0 | .NET 6+ | .NET 6+ |

### Migration Path Analysis

**Option A: 5.x → 6.x → 7.x (Two-step)**
- **Pros**: Incremental changes, easier per-step
- **Cons**: Two major migrations, extended timeline, deferred async benefits

**Option B: 5.x → 7.x (Direct) ⭐**
- **Pros**: Single migration effort, immediate async benefits, future-proof
- **Cons**: Larger upfront effort (acceptable for major version)

### Decision: RabbitMQ.Client 7.x (Direct Migration)

**Rationale**: RawRabbit 3.0 is already a major rewrite. Combining .NET 9 + RabbitMQ.Client 7.x + async conversion into one breaking change window is more efficient than multiple sequential migrations.

**See**: [ADR 0002 RabbitMQ Client Version Strategy](adr/ADR%200002%20RabbitMQ%20Client%20Version%20Strategy.md)

---

## 3. Serialization Library Analysis

### Research Question
Which JSON serialization library provides the best default experience for RawRabbit users?

### Libraries Evaluated

#### Newtonsoft.Json (Json.NET)
- **Current Version**: 13.0.3 (upgrading from 10.0.1)
- **Maturity**: 15+ years, industry standard
- **Package Size**: ~700 KB
- **Maintainer**: James Newton-King (community-driven)

**Performance Benchmarks** (.NET 8):
- Serialization: Baseline (1.00x)
- Deserialization: Baseline (1.00x)
- Memory Allocations: Baseline

**Strengths**:
- **Feature Rich**: Comprehensive functionality
  - LINQ to JSON for query operations
  - Polymorphic deserialization with `$type` metadata
  - Extensive custom converter ecosystem
  - Flexible attribute-based configuration
  - Circular reference handling
  - DateTimeOffset, TimeSpan, Guid built-in support
- **Proven Track Record**: 15+ years production use
- **Community Knowledge**: Massive documentation, Stack Overflow coverage
- **Backward Compatible**: Upgrading 10.0.1 → 13.0.3 maintains compatibility
- **Flexible by Default**: Handles edge cases gracefully
- **Existing RawRabbit Users**: Already familiar with it

**Weaknesses**:
- **External Dependency**: Requires NuGet package
- **Performance**: 50%+ slower than System.Text.Json for most scenarios
- **Memory**: Higher allocations, more GC pressure
- **Security**: History of CVEs (though 13.0.3 addresses known issues)
- **Not The Future**: Microsoft not investing in it
- **Package Size**: Adds ~700 KB to deployment

**Use Cases**: Complex types, polymorphism, circular references, legacy compatibility

#### System.Text.Json
- **Version**: Built into .NET runtime (no external package)
- **Maturity**: 6 years (since .NET Core 3.0 / 2019)
- **Package Size**: 0 (runtime included)
- **Maintainer**: Microsoft

**Performance Benchmarks** (.NET 8):
- Serialization: 1.5-2.0x faster than Newtonsoft.Json
- Deserialization: 1.5-2.0x faster than Newtonsoft.Json
- Memory Allocations: 50%+ fewer allocations

**Strengths**:
- **Performance**: 50%+ faster serialization/deserialization
  - Optimized for modern .NET runtime
  - Lower memory allocations
  - Reduced GC pressure
  - Span<T> and Memory<T> optimizations
- **Built-in**: Zero external dependencies, smaller deployment
- **Security**: Maintained by Microsoft, quick security patches
- **Modern .NET**: Designed for async/await patterns
- **Standards Compliant**: Strict JSON specification adherence
- **Future-Proof**: Actively developed, improving each .NET release
- **UTF-8 Support**: Native UTF-8 handling (most efficient)
- **Deterministic**: Predictable behavior

**Weaknesses**:
- **Limited Features**: Missing some Newtonsoft.Json capabilities
  - No LINQ to JSON
  - Limited polymorphic support (requires JsonDerivedType attributes)
  - Fewer built-in converters
  - Less flexible with edge cases
- **Strict by Default**: Requires explicit configuration for flexibility
- **Breaking Change**: Different JSON format for some edge cases
  - DateTime format differences
  - Enum handling differences
  - Dictionary key handling
- **Custom Converters**: More verbose to implement

**Use Cases**: Simple DTOs, high-performance scenarios, modern applications

### Feature Comparison Matrix

| Feature | Newtonsoft.Json | System.Text.Json |
|---------|-----------------|------------------|
| **Serialization Speed** | Baseline (1.00x) | 1.5-2.0x faster |
| **Memory Allocations** | Baseline | 50%+ fewer |
| **Package Dependency** | External (~700 KB) | Built-in (0 KB) |
| **LINQ to JSON** | Yes | No |
| **Polymorphism** | Automatic ($type) | Manual (attributes) |
| **Circular References** | Yes | Limited |
| **Custom Converters** | Extensive ecosystem | Growing |
| **Attribute-based Config** | Flexible | Available |
| **DateTime Handling** | Flexible formats | ISO 8601 |
| **Enum Handling** | String or numeric | Numeric default |
| **Security Patches** | Community | Microsoft |
| **Active Development** | Maintenance | Active |

### Compatibility Analysis

**Common Use Cases** (90% of RabbitMQ messages):
- Simple POCOs: ✅ Both compatible
- Lists and arrays: ✅ Both compatible
- DateTime: ⚠️ Format differences (easily configurable)
- Enums: ⚠️ String vs. numeric (configurable)

**Complex Use Cases**:
- Polymorphic types: Newtonsoft.Json easier
- Circular references: Newtonsoft.Json required
- Custom converters: Newtonsoft.Json has larger ecosystem

### Decision: System.Text.Json (Default) + Newtonsoft.Json (Plugin)

**Rationale**:
1. Modern .NET libraries should use built-in serializers by default
2. 50%+ performance improvement benefits all users
3. Zero dependencies reduces deployment size and security surface
4. Users requiring Newtonsoft.Json can install compatibility enricher
5. 90% of use cases work identically

**See**: [ADR 0003 Serialization Strategy](adr/ADR%200003%20Serialization%20Strategy.md)

---

## 4. Breaking Changes Strategy Analysis

### Research Question
How should RawRabbit handle breaking changes between 2.x and 3.0?

### Approaches Evaluated

#### Full Breaking Changes (Clean Slate) ⭐
**Approach**: Complete rewrite with modern patterns, no backward compatibility layers.

**Pros**:
- Clean architecture, no legacy code
- Optimal performance (no compatibility overhead)
- Single code path, easier to maintain
- Clear, opinionated direction
- Best developer experience

**Cons**:
- High migration cost for users
- All-or-nothing upgrade
- Adoption friction initially

**Verdict**: Optimal for major version

#### Backward Compatibility Layer
**Approach**: Sync wrapper methods over async internals (`.GetAwaiter().GetResult()`).

**Pros**:
- Easier initial migration
- Existing code may "just work"

**Cons**:
- **Deadlock Risk**: Sync-over-async causes thread pool starvation
- Performance penalty (loses async benefits)
- Maintenance burden (two API surfaces)
- Confusing documentation
- Technical debt

**Verdict**: Dangerous anti-pattern, rejected

#### Side-by-Side Versions (2.x + 3.x)
**Approach**: Maintain 2.x with security patches while developing 3.x.

**Pros**:
- No forced migration
- Legacy support for .NET Framework users
- Users upgrade when ready

**Cons**:
- Maintenance burden (two versions)
- Resource split
- Documentation overhead

**Verdict**: Acceptable as part of strategy

### Decision: Full Breaking Changes + Side-by-Side Support

**Hybrid Approach**:
1. RawRabbit 3.0: Full breaking changes, clean async architecture
2. RawRabbit 2.x: Security patches until .NET 9 EOL (Nov 2026)
3. No compatibility layer (avoid sync-over-async)
4. Comprehensive migration guide

**See**: [ADR 0004 Breaking Changes Strategy](adr/ADR%200004%20Breaking%20Changes%20Strategy.md)

---

## 5. Deprecated Package Analysis

### Research Question
Which packages should be deprecated or removed due to unmaintained dependencies?

### Packages Evaluated

#### RawRabbit.Enrichers.ZeroFormatter ❌
- **Dependency**: ZeroFormatter 1.6.4 (2017, 8 years old)
- **Status**: Unmaintained (last release 2017)
- **Security**: No updates for 8 years

**Assessment**:
- Creator (neuecc) moved to MessagePack-CSharp
- No .NET 9 support
- Security liability
- Better alternative exists (MessagePack)

**Decision**: **REMOVE** - Migrate users to MessagePack

#### RawRabbit.DependencyInjection.Ninject ❌
- **Dependency**: Ninject 3.2.2 / 4.0.0-beta-0134
- **Status**: No official .NET Core support
- **Security**: Using beta build for .NET Standard

**Assessment**:
- Never supported .NET Core officially
- Less popular than alternatives
- Microsoft.Extensions.DependencyInjection is built-in
- Autofac is better alternative

**Decision**: **REMOVE** - Migrate users to Microsoft DI or Autofac

#### RawRabbit.Enrichers.HttpContext (Partial) ⚠️
- **Issue**: `NetFxHttpContextMiddleware` uses `System.Web.HttpContext` (.NET Framework only)
- **Status**: File already deleted in git

**Assessment**:
- .NET Framework code incompatible with .NET 9
- ASP.NET Core middleware remains functional

**Decision**: **KEEP** package, .NET Framework code removed

#### RawRabbit.Enrichers.MessagePack ✅
- **Dependency**: MessagePack (actively maintained)
- **Status**: Healthy, modern, performant

**Decision**: **KEEP** and maintain

#### RawRabbit.Enrichers.Protobuf ✅
- **Dependency**: Google.Protobuf (Google-backed)
- **Status**: Industry standard, actively maintained

**Decision**: **KEEP** and maintain

#### RawRabbit.Enrichers.Polly ✅
- **Dependency**: Polly (actively maintained)
- **Status**: Industry-standard resilience library

**Decision**: **KEEP** and update to async

### Package Decision Matrix

| Package | Current Status | 3.0 Decision | Reason | Alternative |
|---------|---------------|--------------|---------|-------------|
| **ZeroFormatter** | Unmaintained (2017) | ❌ Remove | Security risk, unmaintained | MessagePack |
| **Ninject** | No .NET Core support | ❌ Remove | Beta version, unpopular | Microsoft DI, Autofac |
| **HttpContext (.NET Fx)** | .NET Fx only | ❌ Remove | .NET 9 incompatible | ASP.NET Core version |
| **MessagePack** | Active | ✅ Keep | Modern, performant | N/A |
| **Protobuf** | Active (Google) | ✅ Keep | Industry standard | N/A |
| **Polly** | Active | ✅ Keep | Essential resilience | N/A |
| **GlobalExecutionId** | Self-contained | ✅ Keep | Useful, no dependencies | N/A |
| **MessageContext** | Self-contained | ✅ Keep | Core functionality | N/A |
| **QueueSuffix** | Self-contained | ✅ Keep | Useful feature | N/A |
| **RetryLater** | Self-contained | ✅ Keep | Valuable pattern | N/A |
| **Attributes** | Self-contained | ✅ Keep | Useful feature | N/A |
| **ServiceCollection** | Built-in DI | ✅ Keep | Primary DI | N/A |
| **Autofac** | Active | ✅ Keep | Popular DI | N/A |

**See**: [ADR 0005 Deprecated Package Strategy](adr/ADR%200005%20Deprecated%20Package%20Strategy.md)

---

## 6. Nullable Reference Types Analysis

### Research Question
Should RawRabbit 3.0 enable nullable reference types, and how aggressively?

### Options Evaluated

#### Enable Globally with Warnings as Errors ⭐
**Configuration**:
```xml
<Nullable>enable</Nullable>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

**Pros**:
- Maximum type safety (compile-time null checks)
- Clear API contracts (users know what can be null)
- Tooling support (IntelliSense shows nullability)
- Ecosystem alignment (modern .NET libraries enable it)
- Fewer runtime NullReferenceExceptions

**Cons**:
- High annotation effort (entire codebase)
- Learning curve for nullable patterns
- Some method signatures more verbose

**Verdict**: Best for modern library

#### Enable with Warnings Only
**Pros**: Gradual adoption, flexible timeline
**Cons**: Warning fatigue, inconsistent codebase, technical debt

**Verdict**: Creates technical debt

#### Disable
**Pros**: Zero migration cost
**Cons**: Misses modern .NET features, behind ecosystem

**Verdict**: Not viable for 2025 library

### Decision: Enable Globally with Warnings as Errors

**Rationale**:
1. RawRabbit 3.0 is a major rewrite (perfect timing)
2. Modern .NET libraries enable nullable reference types
3. Prevents NullReferenceExceptions at compile time
4. RabbitMQ.Client 7.x already uses nullable reference types
5. Async rewrite already requires touching most code

**Implementation Phases**:
1. Phase 1: Core interfaces (Week 1)
2. Phase 2: Middleware pipeline (Week 2-3)
3. Phase 3: Enrichers (Week 4-5)
4. Phase 4: Internal infrastructure (Week 6)

**See**: [ADR 0006 Nullable Reference Types](adr/ADR%200006%20Nullable%20Reference%20Types.md)

---

## Risk Assessment

### High Priority Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| **RabbitMQ.Client 7.x Migration Errors** | Medium | High | Comprehensive testing, integration tests with real RabbitMQ |
| **User Migration Cost** | High | Medium | Detailed migration guide, 2-year 2.x support window |
| **Async/Await Implementation Bugs** | Medium | High | Thorough testing, performance benchmarks, code review |
| **Serialization Edge Cases** | Low | Medium | Migration guide, Newtonsoft enricher available |
| **Nullable Annotation Errors** | Low | Medium | Gradual rollout, comprehensive testing |

### Medium Priority Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| **Performance Regression** | Low | Medium | Benchmark suite, compare against 2.x baseline |
| **Breaking Changes Fragmentation** | Medium | Low | 2.x maintenance for 2 years |
| **Edge Case Bugs in .NET 9** | Low | Low | Thorough integration testing |
| **Adoption Delay** | Medium | Low | Clear value proposition, migration support |

---

## Implementation Complexity Assessment

### Complexity by Component

| Component | Complexity | Estimated Effort | Critical Path | Dependencies |
|-----------|------------|------------------|---------------|--------------|
| **Core Library (RawRabbit)** | High | 3-4 weeks | Yes | None |
| **RabbitMQ.Client 7.x Migration** | High | 2-3 weeks | Yes | Core |
| **Async/Await Conversion** | High | 3-4 weeks | Yes | RabbitMQ.Client |
| **Middleware Pipeline** | High | 2-3 weeks | Yes | Async |
| **Operations (Publish, Subscribe, etc.)** | Medium | 2 weeks | Yes | Middleware |
| **Serialization (System.Text.Json)** | Low | 1 week | No | Core |
| **Nullable Reference Types** | Medium | 2-3 weeks | No | Concurrent |
| **Enrichers (Polly, MessageContext, etc.)** | Medium | 2-3 weeks | No | Core, Middleware |
| **DI Integrations** | Low | 1 week | No | Core |
| **Sample Applications** | Low | 1 week | No | All above |
| **Documentation** | Medium | 2 weeks | No | All above |
| **Migration Guide** | Medium | 1-2 weeks | No | All above |

**Total Estimated Effort**: 20-30 weeks (5-7.5 months) for complete migration

**Critical Path**: Core → RabbitMQ.Client Migration → Async Conversion → Middleware → Operations

---

## Technology Stack Summary

### RawRabbit 3.0 Technology Stack

```
┌─────────────────────────────────────────────────────────┐
│                  RawRabbit 3.0                          │
│                 .NET 9 (C# 13)                          │
│           Nullable Reference Types Enabled               │
└─────────────────────────────────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
   ┌────▼────┐      ┌──────▼──────┐    ┌─────▼─────┐
   │RabbitMQ │      │System.Text  │    │  Async/   │
   │Client   │      │   .Json     │    │  Await    │
   │  7.1.2  │      │  (built-in) │    │  (Task)   │
   └─────────┘      └─────────────┘    └───────────┘
        │
   ┌────▼────────────────────────────────────────┐
   │  Enrichers (Plugins)                        │
   ├─────────────────────────────────────────────┤
   │ • MessagePack (binary serialization)        │
   │ • Protobuf (protocol buffers)               │
   │ • Newtonsoft.Json (compatibility)           │
   │ • Polly (resilience)                        │
   │ • MessageContext (metadata)                 │
   │ • GlobalExecutionId (tracing)               │
   └─────────────────────────────────────────────┘
```

### Dependencies (Core Package)

```xml
<ItemGroup>
  <!-- Only production dependency -->
  <PackageReference Include="RabbitMQ.Client" Version="7.1.2" />

  <!-- Framework-provided (no package needed) -->
  <!-- System.Text.Json - built into .NET 9 runtime -->
  <!-- System.Threading.Tasks - built into .NET 9 runtime -->
</ItemGroup>
```

**Key Point**: Core RawRabbit package has **only one external dependency** (RabbitMQ.Client)

---

## Performance Expectations

### Expected Improvements in RawRabbit 3.0

Based on technology choices, expected performance improvements vs. RawRabbit 2.x:

| Metric | Baseline (2.x) | Expected (3.0) | Improvement | Source |
|--------|----------------|----------------|-------------|--------|
| **Serialization Speed** | 1.00x | 1.5-2.0x | 50-100% faster | System.Text.Json benchmarks |
| **Memory Allocations** | 1.00x | 0.5x | 50% reduction | System.Text.Json + ReadOnlyMemory |
| **Async Throughput** | 1.00x | 1.2-1.5x | 20-50% higher | RabbitMQ.Client 7.x async model |
| **Connection Handling** | 1.00x | 1.1-1.3x | 10-30% better | RabbitMQ.Client 7.x improvements |
| **Package Size** | 1.00x | 0.85x | 15% smaller | Remove Newtonsoft.Json dependency |

**Note**: Actual improvements depend on workload characteristics. Benchmarking required during migration.

---

## Migration Complexity by User Type

### Simple Use Cases (70% of users)
**Characteristics**: Basic pub/sub, simple DTOs, no custom middleware

**Migration Effort**: Low (1-3 days)
- Update package references
- Change handler signatures to async
- Test functionality

**Example**:
```csharp
// 2.x
client.Subscribe<MyMessage>(msg => {
    Process(msg);
});

// 3.0
await client.SubscribeAsync<MyMessage>(async msg => {
    await ProcessAsync(msg);
});
```

### Moderate Use Cases (20% of users)
**Characteristics**: Request/response, custom configuration, DateTime/Enum serialization

**Migration Effort**: Medium (1-2 weeks)
- Async conversion
- Serialization edge case handling
- Custom configuration updates
- IModel → IChannel updates

### Complex Use Cases (10% of users)
**Characteristics**: Custom middleware, enrichers, polymorphic types, Ninject/ZeroFormatter

**Migration Effort**: High (2-4 weeks)
- Custom middleware rewrite (async)
- Migrate to MessagePack (if using ZeroFormatter)
- Migrate to Microsoft DI (if using Ninject)
- Polymorphic serialization updates
- Extensive testing

---

## Ecosystem Alignment

### How RawRabbit 3.0 Compares to Modern .NET Libraries

| Library | Target Framework | Async API | Nullable Types | Built-in Serializer |
|---------|------------------|-----------|----------------|---------------------|
| **ASP.NET Core 9.0** | .NET 9 | ✅ Yes | ✅ Enabled | System.Text.Json |
| **Entity Framework Core 9.0** | .NET 9 | ✅ Yes | ✅ Enabled | N/A |
| **MassTransit 8.x** | .NET 8+ | ✅ Yes | ✅ Enabled | System.Text.Json |
| **Rebus 8.x** | .NET 8+ | ✅ Yes | ⚠️ Partial | Newtonsoft.Json |
| **RawRabbit 2.x (current)** | netstandard1.5 | ⚠️ Partial | ❌ No | Newtonsoft.Json |
| **RawRabbit 3.0 (planned)** | .NET 9 | ✅ Yes | ✅ Enabled | System.Text.Json |

**Insight**: RawRabbit 3.0 will align with modern .NET library standards (ASP.NET Core, EF Core, MassTransit).

---

## Next Steps

### Immediate Actions
1. ✅ **ADRs Created**: All 6 Architecture Decision Records documented
2. ⏭️ **Share with Stakeholders**: Review ADRs with maintainers and community
3. ⏭️ **Create Migration Plan**: Detailed implementation roadmap based on ADRs
4. ⏭️ **Prototype Critical Components**: Validate RabbitMQ.Client 7.x async patterns
5. ⏭️ **Set Up Benchmarking**: Baseline RawRabbit 2.x performance

### Migration Planning
1. Define migration stages (see ADRs)
2. Create dependency graph for parallel work
3. Set up continuous testing infrastructure
4. Draft comprehensive migration guide structure
5. Plan communication strategy (blog post, announcements)

---

## Conclusion

RawRabbit 3.0 modernization requires strategic technology choices across six key areas. Research indicates:

1. **.NET 9** provides optimal framework target (same EOL as .NET 8, latest features)
2. **RabbitMQ.Client 7.x** is necessary for modern .NET compatibility and performance
3. **System.Text.Json** offers best default experience (performance, zero dependencies)
4. **Full breaking changes** with side-by-side 2.x support is cleanest approach
5. **Remove deprecated packages** (ZeroFormatter, Ninject) due to security and maintenance
6. **Enable nullable reference types** to align with modern .NET ecosystem

These decisions position RawRabbit 3.0 as a modern, performant, and maintainable messaging library for the next 5+ years.

**All detailed rationale, trade-offs, and implementation guidance are documented in the ADRs located in `docs/adr/`.**

---

## References

### Official Documentation
- [.NET Support Policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
- [RabbitMQ .NET Client Documentation](https://www.rabbitmq.com/client-libraries/dotnet)
- [RabbitMQ.Client v7 Migration Guide](https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/main/v7-MIGRATION.md)
- [System.Text.Json Documentation](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview)
- [Nullable Reference Types](https://learn.microsoft.com/dotnet/csharp/nullable-references)

### Performance Benchmarks
- [.NET 9 Performance Improvements](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-9/)
- [System.Text.Json vs Newtonsoft.Json Benchmarks](https://trevormccubbin.medium.com/net-performance-analysis-newtonsoft-json-vs-system-text-json-in-net-8-34520c21d054)

### Community Resources
- [RabbitMQ.Client GitHub Discussions](https://github.com/rabbitmq/rabbitmq-dotnet-client/discussions)
- [Semantic Versioning](https://semver.org/)

---

**Document Version**: 1.0
**Last Updated**: 2025-10-13
**Authors**: Architecture Team
