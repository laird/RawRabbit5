# ADR 0004 Breaking Changes Strategy

**Status**: Proposed

**Date**: 2025-10-13

**Decision Makers**: Architecture Team, RawRabbit Maintainers

**Technical Story**: RawRabbit 3.0 involves multiple breaking changes: target framework upgrade (.NET 9), RabbitMQ.Client 7.x migration (async API), and serialization changes. We need a clear strategy for managing these breaking changes and supporting users through migration.

## Context and Problem Statement

How should we handle breaking changes in RawRabbit 3.0? What level of backward compatibility should we provide? How can we minimize migration friction while still modernizing the codebase?

## Decision Drivers

- **Modernization Goals**: Need to upgrade to modern .NET and RabbitMQ.Client
- **User Migration Cost**: Minimize effort required for users to upgrade
- **Technical Debt**: Avoid carrying forward legacy patterns
- **Semantic Versioning**: Major version allows breaking changes
- **Maintenance Burden**: Balance compatibility vs. code complexity
- **Clear Communication**: Users must understand changes and migration path
- **Ecosystem Health**: Long-term sustainability vs. short-term friction

## Considered Options

### Option 1: Full Breaking Changes (Clean Slate)

**Approach**: Completely rewrite RawRabbit 3.0 with modern patterns, no backward compatibility layer.

**Breaking Changes**:
- ✅ Target .NET 9 only (drop .NET Framework, netstandard1.5)
- ✅ RabbitMQ.Client 7.x (full async API, IModel → IChannel)
- ✅ System.Text.Json default (drop Newtonsoft.Json default)
- ✅ Async middleware pipeline (all operations return Task)
- ✅ Remove legacy enrichers (ZeroFormatter, Ninject)
- ✅ Update all public APIs to async methods

**Pros**:
- **Clean Architecture**: No legacy code, modern patterns throughout
- **Optimal Performance**: No compatibility overhead
- **Maintainability**: Single code path, easier to understand
- **Clear Direction**: Opinionated, modern approach
- **Lower Complexity**: No conditional compilation or abstraction layers
- **Best Developer Experience**: Modern async/await patterns

**Cons**:
- **High Migration Cost**: Users must rewrite significant code
- **All-or-Nothing**: Cannot partially upgrade
- **Adoption Friction**: May delay upgrades
- **Risk of Fragmentation**: Users may stay on 2.x longer

**Trade-offs**:
- Short-term migration pain vs. long-term maintainability
- Clean architecture vs. user convenience

### Option 2: Backward Compatibility Layer

**Approach**: Provide compatibility wrappers allowing synchronous API access to async internals.

**Compatibility Features**:
- Sync wrapper methods calling async internals with `.GetAwaiter().GetResult()`
- `IModel` compatibility interface wrapping `IChannel`
- Newtonsoft.Json plugin for drop-in compatibility
- Legacy enricher packages (marked deprecated)

**Example**:
```csharp
// Legacy sync API (compatibility)
public void Publish<T>(T message) => PublishAsync(message).GetAwaiter().GetResult();

// Modern async API
public Task PublishAsync<T>(T message);
```

**Pros**:
- **Easier Migration**: Users can upgrade incrementally
- **Lower Initial Friction**: Existing code may "just work"
- **Gradual Adoption**: Migrate to async at own pace

**Cons**:
- **Deadlock Risk**: Sync-over-async is dangerous (thread pool starvation)
- **Performance Penalty**: Sync wrappers lose async benefits
- **Maintenance Burden**: Two API surfaces to maintain
- **Confusing Documentation**: Which API should users use?
- **Technical Debt**: Carrying forward anti-patterns
- **False Compatibility**: Looks compatible but has hidden issues

**Trade-offs**:
- Lower migration friction vs. maintainability nightmare
- User convenience vs. architectural integrity

### Option 3: Side-by-Side Major Versions (Maintain 2.x and 3.x)

**Approach**: Continue maintaining RawRabbit 2.x for legacy scenarios while developing 3.x.

**Maintenance Plan**:
- RawRabbit 2.x: Security patches only, frozen features
- RawRabbit 3.x: Active development, modern approach
- Documentation for both versions
- Clear migration path guidance

**Pros**:
- **No Forced Migration**: Users upgrade when ready
- **Legacy Support**: .NET Framework users can stay on 2.x
- **Lower Risk**: Users can thoroughly test before migrating
- **Clear Separation**: No mixing of legacy and modern patterns

**Cons**:
- **Maintenance Burden**: Must maintain two major versions
- **Resource Split**: Developer time divided across versions
- **Security Updates**: Must backport critical fixes to 2.x
- **Documentation Overhead**: Two sets of docs
- **Fragmentation**: Community split across versions

**Trade-offs**:
- User flexibility vs. maintainer burden
- Stability for legacy users vs. focus on modern version

### Option 4: Aggressive Deprecation (Break Now, Remove Later)

**Approach**: All breaking changes in 3.0, remove deprecated APIs in 3.1/4.0.

**Deprecation Timeline**:
- 3.0: Introduce breaking changes, mark legacy patterns as obsolete
- 3.0-3.x: Warning messages, documentation pointing to new patterns
- 4.0: Remove all deprecated APIs

**Pros**:
- **Clear Warnings**: Users know what will break
- **Migration Window**: Time to update code
- **Progressive Improvement**: Can remove technical debt incrementally

**Cons**:
- **Compiler Warnings**: Noisy build output
- **Uncertainty**: When will deprecated features be removed?
- **Maintenance**: Still carrying legacy code through 3.x
- **User Confusion**: "Can I use this or not?"

**Trade-offs**:
- Gradual deprecation vs. clean break
- Warning fatigue vs. migration support

## Decision Outcome

**Chosen Option**: "Full Breaking Changes (Clean Slate) + Comprehensive Migration Guide + Side-by-Side Support"

**Hybrid Approach**:
1. **RawRabbit 3.0**: Full breaking changes, modern async architecture
2. **RawRabbit 2.x**: Maintain with security patches for 2 years (until .NET 9 EOL Nov 2026)
3. **No Compatibility Layer**: Clean async architecture, no sync wrappers
4. **Comprehensive Migration Guide**: Detailed documentation for every breaking change
5. **Plugin Architecture**: Newtonsoft.Json available as enricher, not in core

**Rationale**:

1. **Major Version Semantics**: Version 3.0 signals breaking changes. Users expect this.

2. **Avoid Sync-over-Async**: Compatibility wrappers using `.GetAwaiter().GetResult()` are dangerous anti-patterns that cause deadlocks. We will not ship deadlock-prone code.

3. **Maintainability**: A clean async codebase is far easier to maintain than dual sync/async paths with conditional compilation.

4. **Performance**: Async all the way through provides optimal performance. Compatibility layers would nullify RabbitMQ.Client 7.x benefits.

5. **Clear Migration Path**: A comprehensive guide is more valuable than fragile compatibility layers.

6. **Legacy Support Timeline**: Supporting 2.x until .NET 9 EOL (Nov 2026) gives users 2 years to migrate, which is reasonable for a major version change.

7. **Architectural Integrity**: Modern messaging libraries should be async-first. Sync APIs on top of async infrastructure are a code smell.

**Consequences**:

**Positive**:
- Clean, modern, maintainable codebase
- Optimal performance (full async benefits)
- Clear architectural direction
- No technical debt from compatibility layers
- Best developer experience for new users
- Simpler testing and documentation
- Future-proof for 5+ years

**Negative**:
- **High migration cost for users**:
  - Must update all handler code to async
  - Must handle `IChannel` instead of `IModel`
  - May need to adjust serialization for edge cases
- **Adoption friction initially**
- **Cannot run 2.x and 3.x side-by-side in same process** (different RabbitMQ.Client versions)
- **2.x maintenance burden** for 2 years

**Risks**:
- **High**: Users delay migration due to effort (mitigated by 2-year support window)
- **Medium**: Complex applications face significant rewrite (mitigated by detailed migration guide)
- **Low**: Bugs in new async infrastructure (mitigated by thorough testing)

## Implementation Notes

### Breaking Changes Summary

**1. Target Framework**:
- ❌ Remove: `netstandard1.5`, `net451`
- ✅ Add: `net9.0`
- **Impact**: .NET Framework users must stay on 2.x

**2. RabbitMQ.Client 7.x**:
- ❌ `IModel` → ✅ `IChannel`
- ❌ `BasicPublish()` → ✅ `BasicPublishAsync()`
- ❌ `IBasicConsumer` → ✅ `IAsyncBasicConsumer`
- ❌ `channel.CreateBasicProperties()` → ✅ `new BasicProperties()`
- **Impact**: All middleware and user code must be updated

**3. Async API**:
- ❌ `void Publish<T>(T message)` → ✅ `Task PublishAsync<T>(T message)`
- ❌ `void Subscribe<T>(Action<T> handler)` → ✅ `Task SubscribeAsync<T>(Func<T, Task> handler)`
- ❌ `TResponse Request<TRequest, TResponse>()` → ✅ `Task<TResponse> RequestAsync<TRequest, TResponse>()`
- **Impact**: All user code must use async/await

**4. Serialization**:
- ❌ Newtonsoft.Json (default) → ✅ System.Text.Json (default)
- ✅ Newtonsoft.Json available as enricher: `RawRabbit.Enrichers.Newtonsoft.Json`
- **Impact**: Edge cases may require code updates or enricher installation

**5. Deprecated Packages**:
- ❌ Remove: `RawRabbit.Enrichers.ZeroFormatter`
- ❌ Remove: `RawRabbit.DependencyInjection.Ninject`
- ❌ Remove: `RawRabbit.Enrichers.HttpContext` (.NET Framework specific)
- **Impact**: Users must migrate to alternatives (see ADR 0005)

### Migration Guide Structure

**Must Include**:
1. **Quick Start**: "What changed and why"
2. **Framework Requirements**: .NET 9 SDK installation
3. **Step-by-Step Migration**:
   - Update project files (TargetFramework)
   - Update RawRabbit packages
   - Convert handlers to async
   - Update IModel → IChannel
   - Test serialization edge cases
4. **Code Examples**: Before/after comparisons for common scenarios
5. **Troubleshooting**: Common migration issues
6. **Breaking Changes Reference**: Complete list with mitigation strategies
7. **Performance Benchmarks**: Show improvements to justify effort

### Support Timeline

**RawRabbit 2.x**:
- Security patches: Until November 2026 (.NET 9 EOL)
- Bug fixes: Critical only, case-by-case basis
- New features: None
- Documentation: Maintained, clearly marked as legacy
- Support forum: Community support continues

**RawRabbit 3.x**:
- Active development: All new features
- Regular releases: Bug fixes and improvements
- Documentation: Primary focus
- Long-term support: Through .NET 10 migration (~2028)

### Communication Strategy

1. **Announcement Blog Post**: Explain rationale and benefits
2. **Migration Guide**: Comprehensive, published before 3.0 release
3. **Video Walkthrough**: Video showing migration of sample app
4. **Release Notes**: Detailed breaking changes list
5. **GitHub Discussions**: Q&A forum for migration help
6. **Sample Projects**: Updated samples showing 3.0 patterns

### Compatibility Matrix

| Feature | 2.x | 3.0 | Notes |
|---------|-----|-----|-------|
| .NET Framework 4.5+ | ✅ | ❌ | Stay on 2.x |
| .NET Standard 2.0+ | ✅ | ❌ | Upgrade to .NET 9 |
| .NET 6/7/8 | ❌ | ✅ | Can upgrade from .NET 8 |
| .NET 9 | ❌ | ✅ | Required |
| Sync API | ✅ | ❌ | Must use async |
| Async API | ✅ | ✅ | Already supported in 2.x |
| Newtonsoft.Json | ✅ Default | ✅ Plugin | Install enricher |
| System.Text.Json | ❌ | ✅ Default | Built-in |
| RabbitMQ.Client 5.x | ✅ | ❌ | |
| RabbitMQ.Client 7.x | ❌ | ✅ | |

## Links

- Related: [ADR 0001 Target Framework Selection](./ADR%200001%20Target%20Framework%20Selection.md)
- Related: [ADR 0002 RabbitMQ Client Version Strategy](./ADR%200002%20RabbitMQ%20Client%20Version%20Strategy.md)
- Related: [ADR 0003 Serialization Strategy](./ADR%200003%20Serialization%20Strategy.md)
- Related: [ADR 0005 Deprecated Package Strategy](./ADR%200005%20Deprecated%20Package%20Strategy.md)
- [Semantic Versioning](https://semver.org/)
- [Breaking Changes Guidelines](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/breaking-change-rules.md)
