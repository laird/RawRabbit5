# RawRabbit 3.0 Architecture Recommendations

**Date**: 2025-10-13
**Author**: Architecture Team
**Status**: Approved for Implementation

This document provides executive recommendations based on comprehensive technology research and six Architecture Decision Records (ADRs).

---

## Executive Summary

RawRabbit 3.0 modernization requires strategic technology upgrades across the entire stack. After evaluating alternatives and analyzing trade-offs, we recommend:

| Decision | Recommendation | Impact | Risk Level |
|----------|---------------|--------|------------|
| **Target Framework** | .NET 9 (STS) | High (enables all other decisions) | Low |
| **RabbitMQ.Client** | Version 7.1.2 | High (complete async rewrite) | Medium |
| **JSON Serializer** | System.Text.Json (default) | Medium (50%+ performance gain) | Low |
| **Breaking Changes** | Full breaking changes | High (major version bump) | Low |
| **Deprecated Packages** | Remove ZeroFormatter, Ninject | Medium (small user base) | Low |
| **Nullable Types** | Enable globally | Medium (type safety) | Low |

**Key Insight**: All recommended changes align into a cohesive modernization strategy. RawRabbit 3.0 becomes a fully async, high-performance, modern .NET library.

---

## Strategic Recommendations

### 1. Target .NET 9 (Not .NET 8)

**Recommendation**: Target .NET 9 exclusively, dropping all legacy frameworks.

**Why .NET 9 Over .NET 8?**
- **Identical Support Window**: Both reach EOL on November 10, 2026 (Microsoft extended STS to 24 months)
- **Latest Features**: C# 13, improved async/await, better JSON performance
- **No Downside**: Since EOL dates match, choosing .NET 9 provides benefits with no additional migration burden

**Framework Changes**:
```diff
- <TargetFrameworks>netstandard1.5;net451</TargetFrameworks>
+ <TargetFramework>net9.0</TargetFramework>
```

**Impact**:
- .NET Framework users must stay on RawRabbit 2.x (supported until Nov 2026)
- Modern .NET users get latest features and performance

**See**: [ADR 0001](adr/ADR%200001%20Target%20Framework%20Selection.md)

---

### 2. Migrate to RabbitMQ.Client 7.x (Async All The Way)

**Recommendation**: Direct migration from 5.0.1 → 7.1.2, skipping 6.x.

**Why Version 7.x?**
- **Modern Async API**: Full Task-based async pattern (TAP)
- **Better Performance**: Memory efficiency, higher throughput
- **Future-Proof**: Official recommended version, active development
- **Single Migration**: Avoid two-step migration (5→6→7)

**Major API Changes**:
| 5.x/6.x (Old) | 7.x (New) | Impact |
|---------------|-----------|---------|
| `IModel` | `IChannel` | All code references |
| `BasicPublish()` | `BasicPublishAsync()` | All operations |
| `IBasicConsumer` | `IAsyncBasicConsumer` | Consumer model |
| `byte[]` | `ReadOnlyMemory<byte>` | Message handling |

**Migration Complexity**: **High** - Requires rewriting all middleware components

**Benefits**:
- No thread blocking (better scalability)
- Reduced memory allocations
- Aligns with modern .NET async patterns

**See**: [ADR 0002](adr/ADR%200002%20RabbitMQ%20Client%20Version%20Strategy.md)

---

### 3. Use System.Text.Json as Default Serializer

**Recommendation**: Replace Newtonsoft.Json with System.Text.Json as default, offer Newtonsoft as plugin.

**Why System.Text.Json?**
- **Performance**: 50-100% faster serialization
- **Memory**: 50% fewer allocations
- **Zero Dependencies**: Built into .NET runtime (smaller deployment)
- **Security**: Microsoft-maintained, quick patches
- **Future-Proof**: Actively developed, improving each release

**Compatibility Strategy**:
```
Core Package:
  ├─ System.Text.Json (default, built-in)
  └─ ISerializer interface (extensible)

Plugin Package:
  └─ RawRabbit.Enrichers.Newtonsoft.Json
     └─ Newtonsoft.Json 13.0.3
```

**Migration Path**:
- 90% of users: No changes needed (simple DTOs work identically)
- Complex types: Install `RawRabbit.Enrichers.Newtonsoft.Json` plugin
- Edge cases: Follow migration guide for DateTime/Enum handling

**See**: [ADR 0003](adr/ADR%200003%20Serialization%20Strategy.md)

---

### 4. Embrace Full Breaking Changes (No Compatibility Layer)

**Recommendation**: RawRabbit 3.0 is a clean break from 2.x with no sync-over-async wrappers.

**Why No Compatibility Layer?**
- **Sync-over-async is dangerous**: Causes deadlocks and thread pool starvation
- **Performance**: Compatibility wrappers nullify async benefits
- **Maintainability**: Single async codebase is easier to maintain
- **Semantic Versioning**: Major version signals breaking changes

**Support Strategy**:
```
RawRabbit 2.x:
  ├─ Security patches until Nov 2026 (.NET 9 EOL)
  ├─ Critical bug fixes (case-by-case)
  └─ No new features

RawRabbit 3.x:
  ├─ Full async/await API
  ├─ .NET 9 target
  └─ Active development
```

**User Migration Window**: 2 years (until Nov 2026)

**See**: [ADR 0004](adr/ADR%200004%20Breaking%20Changes%20Strategy.md)

---

### 5. Remove Deprecated Packages

**Recommendation**: Remove unmaintained packages immediately in 3.0.

**Packages to Remove**:

| Package | Reason | Alternative | User Impact |
|---------|--------|-------------|-------------|
| **ZeroFormatter** | Unmaintained since 2017 (8 years) | MessagePack | Low (niche use) |
| **Ninject DI** | No .NET Core support, beta versions | Microsoft.Extensions.DI | Low (alternatives exist) |
| **HttpContext (.NET Fx)** | .NET Framework API removed | ASP.NET Core version | Low (already removed) |

**Packages to Keep**:
- ✅ MessagePack (actively maintained, high performance)
- ✅ Protobuf (Google-backed, industry standard)
- ✅ Polly (essential resilience patterns)
- ✅ All self-contained enrichers (GlobalExecutionId, MessageContext, etc.)

**Security Rationale**: Shipping 8-year-old unmaintained dependencies is unacceptable security liability.

**See**: [ADR 0005](adr/ADR%200005%20Deprecated%20Package%20Strategy.md)

---

### 6. Enable Nullable Reference Types Globally

**Recommendation**: Enable nullable reference types for all projects, treat warnings as errors.

**Why Enable?**
- **Type Safety**: Compile-time null checking prevents NullReferenceExceptions
- **API Clarity**: Users know exactly what can be null (`string?` vs `string`)
- **Ecosystem Alignment**: Modern .NET libraries enable this (ASP.NET Core, EF Core)
- **Perfect Timing**: Major rewrite is optimal time to enable
- **RabbitMQ.Client 7.x**: Already uses nullable reference types

**Implementation**:
```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

**Migration Phases**:
1. Week 1: Core interfaces (IBusClient, IChannel, IPipeContext)
2. Week 2-3: Middleware pipeline
3. Week 4-5: Enrichers
4. Week 6: Internal infrastructure

**See**: [ADR 0006](adr/ADR%200006%20Nullable%20Reference%20Types.md)

---

## Architecture Vision: RawRabbit 3.0

### Technology Stack

```
┌──────────────────────────────────────────────────┐
│            RawRabbit 3.0 Core                    │
│                                                  │
│  Framework:  .NET 9 (C# 13)                      │
│  Pattern:    Async/Await (Task-based)            │
│  Null Safety: Nullable Reference Types Enabled   │
└──────────────────────────────────────────────────┘
                      │
       ┌──────────────┼──────────────┐
       │              │              │
   ┌───▼───┐    ┌─────▼─────┐  ┌────▼────┐
   │RabbitMQ│    │System.Text│  │ Modern  │
   │Client  │    │   .Json   │  │  Async  │
   │ 7.1.2  │    │ (built-in)│  │ Pipeline│
   └────────┘    └───────────┘  └─────────┘
```

### Core Principles

1. **Async First**: All operations return `Task`, no blocking calls
2. **Type Safe**: Nullable reference types prevent null reference errors
3. **High Performance**: System.Text.Json + RabbitMQ.Client 7.x optimizations
4. **Zero Core Dependencies**: Only RabbitMQ.Client required
5. **Plugin Architecture**: Extensible via enrichers
6. **Modern .NET**: Leverage latest language and runtime features

---

## Implementation Strategy

### Critical Path

```
Phase 1: Core Library (3-4 weeks)
  └─→ Update to .NET 9
  └─→ RabbitMQ.Client 7.x integration
  └─→ IModel → IChannel conversion

Phase 2: Async Pipeline (3-4 weeks) [DEPENDS ON PHASE 1]
  └─→ Middleware async conversion
  └─→ Operations (Publish, Subscribe, Request, etc.)
  └─→ Consumer factory updates

Phase 3: Enrichers & Features (4-5 weeks) [DEPENDS ON PHASE 2]
  └─→ System.Text.Json integration
  └─→ Newtonsoft.Json enricher
  └─→ Polly async updates
  └─→ Other enrichers

Phase 4: Quality & Documentation (3-4 weeks) [DEPENDS ON PHASE 3]
  └─→ Nullable reference type annotations
  └─→ Comprehensive testing
  └─→ Migration guide
  └─→ Sample applications
```

**Total Timeline**: 20-30 weeks (5-7.5 months)

### Parallelization Opportunities

**Can Run in Parallel**:
- Nullable reference type annotations (ongoing during all phases)
- Documentation updates (concurrent with development)
- Sample application updates (after core stabilizes)
- Enricher updates (after core/middleware complete)

**Must Run Sequentially**:
- Core → Middleware → Operations (critical dependency chain)

---

## Expected Benefits

### Performance Improvements

| Metric | Current (2.x) | Expected (3.0) | Improvement |
|--------|---------------|----------------|-------------|
| **Serialization Speed** | Baseline | 1.5-2.0x | 50-100% faster |
| **Memory Allocations** | Baseline | 0.5x | 50% reduction |
| **Async Throughput** | Baseline | 1.2-1.5x | 20-50% higher |
| **Package Size** | Baseline | 0.85x | 15% smaller |

### Code Quality Improvements

- ✅ **Type Safety**: Compile-time null checking
- ✅ **Modern Patterns**: Full async/await
- ✅ **Clean Architecture**: No legacy code or compatibility layers
- ✅ **Better Tooling**: IntelliSense shows nullability and async patterns
- ✅ **Maintainability**: Single code path, clear patterns

### Developer Experience

- ✅ **Modern IDE Support**: Full IntelliSense for nullability and async
- ✅ **Clear API Contracts**: Explicit nullability and async signatures
- ✅ **Better Error Messages**: Compile-time vs. runtime errors
- ✅ **Ecosystem Alignment**: Consistent with ASP.NET Core, EF Core patterns

---

## Risk Mitigation

### High-Priority Risks

**Risk**: RabbitMQ.Client 7.x async conversion introduces bugs
- **Mitigation**: Comprehensive integration tests with real RabbitMQ
- **Testing**: Every middleware component tested in isolation and integration
- **Validation**: Performance benchmarks comparing 2.x vs 3.0

**Risk**: User migration cost too high, delays adoption
- **Mitigation**:
  - Detailed migration guide with before/after examples
  - 2-year support window for RawRabbit 2.x (until Nov 2026)
  - Video walkthrough demonstrating migration
  - Sample projects showing common patterns

**Risk**: Performance regression despite technology improvements
- **Mitigation**:
  - Comprehensive benchmark suite
  - Compare against RawRabbit 2.x baseline
  - Profile hot paths with BenchmarkDotNet
  - Load testing with real RabbitMQ scenarios

### Medium-Priority Risks

**Risk**: Serialization edge cases break user code
- **Mitigation**:
  - Migration guide documenting all edge cases
  - Newtonsoft.Json enricher for compatibility
  - Test suite covering DateTime, Enum, polymorphic scenarios

**Risk**: Nullable annotation errors introduce false safety
- **Mitigation**:
  - Gradual rollout (phase by phase)
  - Comprehensive testing at each phase
  - Code review focusing on nullability correctness

---

## Success Criteria

### Technical Criteria

- [ ] All projects target .NET 9
- [ ] All operations use RabbitMQ.Client 7.x async API
- [ ] System.Text.Json is default serializer
- [ ] Nullable reference types enabled, zero warnings
- [ ] All tests pass (unit, integration, performance)
- [ ] Performance benchmarks show improvement vs. 2.x
- [ ] Zero external dependencies in core package (except RabbitMQ.Client)

### Documentation Criteria

- [ ] All 6 ADRs reviewed and approved
- [ ] Comprehensive migration guide published
- [ ] API reference documentation updated
- [ ] Code samples demonstrate async patterns
- [ ] Video walkthrough created
- [ ] Breaking changes clearly documented

### Quality Criteria

- [ ] Test coverage ≥ 90% for core library
- [ ] Integration tests with real RabbitMQ passing
- [ ] Performance benchmarks showing improvements
- [ ] Security scan passes (no vulnerabilities)
- [ ] Static analysis passes (no warnings)

---

## Communication Plan

### Phase 1: Announcement (Before Development)
- Blog post explaining modernization rationale
- Outline breaking changes and benefits
- Set expectations for timeline and support

### Phase 2: Progress Updates (During Development)
- Regular updates on GitHub Discussions
- Share early preview packages for feedback
- Demonstrate features via Twitter/blog

### Phase 3: Release Preparation (Before 3.0 Release)
- Publish comprehensive migration guide
- Create video walkthrough
- Update documentation site
- Announce support timeline for 2.x

### Phase 4: Post-Release (After 3.0 Release)
- Monitor GitHub issues closely
- Provide migration support in community forums
- Share success stories and performance improvements
- Plan for .NET 10 migration (Nov 2026)

---

## Next Steps

### Immediate Actions (Week 1)

1. **Review ADRs with stakeholders**
   - Architecture team review
   - Maintainer consensus
   - Community feedback (GitHub Discussions)

2. **Create detailed migration plan**
   - Break down into sprints
   - Identify parallelization opportunities
   - Assign owners for each component

3. **Set up infrastructure**
   - CI/CD for .NET 9
   - Benchmark suite baseline
   - Integration test environment (RabbitMQ)

4. **Prototype critical components**
   - RabbitMQ.Client 7.x async patterns
   - Middleware pipeline async conversion
   - Validate architecture assumptions

5. **Begin documentation**
   - Start migration guide structure
   - Draft breaking changes list
   - Create comparison examples (2.x vs 3.0)

### Implementation Kickoff (Week 2)

- Begin Phase 1 development (Core Library)
- Establish coding standards for nullable reference types
- Set up automated testing infrastructure
- Start parallel documentation efforts

---

## Conclusion

RawRabbit 3.0 represents a comprehensive modernization bringing the library into alignment with modern .NET ecosystem standards. The recommended technology choices form a cohesive strategy:

- **.NET 9**: Latest framework with same support window as .NET 8
- **Async/Await**: Modern patterns matching RabbitMQ.Client 7.x
- **System.Text.Json**: High performance with zero dependencies
- **Clean Break**: No technical debt from compatibility layers
- **Type Safety**: Nullable reference types prevent null errors

This approach positions RawRabbit for long-term success (5+ years) while providing users a clear migration path with 2-year support window for legacy applications.

**All decisions are documented in detail in the ADRs (`docs/adr/`). Implementation should follow the phased approach outlined in this document.**

---

## Related Documents

- [ADR 0001 - Target Framework Selection](adr/ADR%200001%20Target%20Framework%20Selection.md)
- [ADR 0002 - RabbitMQ Client Version Strategy](adr/ADR%200002%20RabbitMQ%20Client%20Version%20Strategy.md)
- [ADR 0003 - Serialization Strategy](adr/ADR%200003%20Serialization%20Strategy.md)
- [ADR 0004 - Breaking Changes Strategy](adr/ADR%200004%20Breaking%20Changes%20Strategy.md)
- [ADR 0005 - Deprecated Package Strategy](adr/ADR%200005%20Deprecated%20Package%20Strategy.md)
- [ADR 0006 - Nullable Reference Types](adr/ADR%200006%20Nullable%20Reference%20Types.md)
- [Technology Research Summary](tech-research.md)

---

**Document Version**: 1.0
**Last Updated**: 2025-10-13
**Status**: Ready for Review
**Next Review**: Upon stakeholder approval
