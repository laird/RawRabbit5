# ADR 0005 Deprecated Package Strategy

**Status**: Proposed

**Date**: 2025-10-13

**Decision Makers**: Architecture Team, RawRabbit Maintainers

**Technical Story**: Several RawRabbit enricher and integration packages depend on unmaintained or obsolete third-party libraries (ZeroFormatter, Ninject) or target .NET Framework-only APIs (HttpContext). We need to determine which packages to deprecate, remove, or maintain in RawRabbit 3.0.

## Context and Problem Statement

Which enricher and integration packages should be deprecated or removed in RawRabbit 3.0? How do we handle packages that depend on unmaintained libraries or .NET Framework-specific APIs?

## Decision Drivers

- **Third-Party Maintenance**: Is the underlying library actively maintained?
- **Security**: Are there known vulnerabilities or security concerns?
- **.NET 9 Compatibility**: Does the package support modern .NET?
- **User Base**: How many users depend on this package?
- **Alternatives Available**: Are there modern replacements?
- **Maintenance Burden**: Cost of updating vs. removing
- **Migration Path**: Can users easily migrate to alternatives?

## Package Analysis

### RawRabbit.Enrichers.ZeroFormatter

**Status**: **DEPRECATED** - Remove in 3.0

**Current State**:
- Depends on: `ZeroFormatter 1.6.4` (2017, last release)
- Targets: `netstandard1.6`, `net451`
- Usage: Binary serialization format

**Assessment**:
- ❌ **ZeroFormatter Unmaintained**: No releases since 2017 (8 years)
- ❌ **Creator Moved On**: Same author (neuecc) now focuses on MessagePack-CSharp
- ❌ **Security Concerns**: No security updates in 8 years
- ❌ **No .NET 9 Support**: Package not updated for modern .NET
- ✅ **Better Alternative Exists**: MessagePack-CSharp (actively maintained)

**Migration Path**:
- Recommended: `RawRabbit.Enrichers.MessagePack` (already exists, maintained)
- MessagePack provides better performance and active maintenance
- MessagePack supports 50+ languages (broader ecosystem)

**Decision**: **REMOVE** from RawRabbit 3.0

### RawRabbit.DependencyInjection.Ninject

**Status**: **DEPRECATED** - Remove in 3.0

**Current State**:
- Depends on: `Ninject 3.2.2` (net451), `Ninject 4.0.0-beta-0134` (netstandard1.5)
- Targets: `netstandard1.5`, `net451`
- Usage: DI container integration

**Assessment**:
- ❌ **No .NET Core Support**: Ninject never supported .NET Core officially
- ❌ **Beta Version for netstandard**: Using beta build for .NET Standard (unstable)
- ❌ **Low Adoption**: Less popular than Autofac or Microsoft DI
- ✅ **Better Alternatives**: Microsoft.Extensions.DependencyInjection (built-in), Autofac

**Migration Path**:
- Recommended: `RawRabbit.DependencyInjection.ServiceCollection` (Microsoft DI, built-in)
- Alternative: `RawRabbit.DependencyInjection.Autofac` (maintained)
- Migration Guide: Provide Ninject → Microsoft.Extensions.DI conversion examples

**Decision**: **REMOVE** from RawRabbit 3.0

### RawRabbit.Enrichers.HttpContext (NetFxHttpContextMiddleware)

**Status**: **PARTIAL REMOVAL** - Remove .NET Framework-specific code

**Current State**:
- Contains: `AspNetCoreHttpContextMiddleware` and `NetFxHttpContextMiddleware`
- `NetFxHttpContextMiddleware`: Uses `System.Web.HttpContext` (.NET Framework only)
- `AspNetCoreHttpContextMiddleware`: Uses `IHttpContextAccessor` (.NET Core compatible)

**Assessment**:
- ❌ **NetFx Code**: `System.Web.HttpContext` not available in .NET 9
- ✅ **ASP.NET Core Code**: `IHttpContextAccessor` works in .NET 9
- ❌ **File Deleted**: `NetFxHttpContextMiddleware.cs` already removed in git status

**Migration Path**:
- .NET Framework users: Stay on RawRabbit 2.x
- .NET 9 users: Use `AspNetCoreHttpContextMiddleware` (already available)

**Decision**: **KEEP** package, removed .NET Framework code (already done)

### RawRabbit.Enrichers.MessagePack

**Status**: **KEEP** - Maintain in 3.0

**Current State**:
- Depends on: `MessagePack` (actively maintained)
- Usage: High-performance binary serialization

**Assessment**:
- ✅ **Actively Maintained**: Regular updates, latest .NET support
- ✅ **High Performance**: Faster than JSON for binary scenarios
- ✅ **Broad Adoption**: 50+ language support, large ecosystem
- ✅ **Specialized Use Case**: Valuable for performance-critical scenarios
- ✅ **.NET 9 Compatible**: Works with modern .NET

**Decision**: **KEEP** and maintain

### RawRabbit.Enrichers.Protobuf

**Status**: **KEEP** - Maintain in 3.0

**Current State**:
- Depends on: `Google.Protobuf` (actively maintained by Google)
- Usage: Protocol Buffers serialization

**Assessment**:
- ✅ **Actively Maintained**: Google-backed, regular updates
- ✅ **Industry Standard**: Widely used for microservices
- ✅ **Schema Evolution**: Built-in versioning support
- ✅ **Cross-Platform**: Protocol Buffers standard across languages
- ✅ **.NET 9 Compatible**: Fully supported

**Decision**: **KEEP** and maintain

### RawRabbit.Enrichers.Polly

**Status**: **KEEP** - Maintain and update in 3.0

**Current State**:
- Depends on: `Polly` (actively maintained)
- Usage: Resilience and transient fault handling

**Assessment**:
- ✅ **Actively Maintained**: Polly is industry-standard resilience library
- ✅ **Critical Functionality**: Retry, circuit breaker, timeout policies
- ✅ **High Value**: Essential for production scenarios
- ✅ **.NET 9 Compatible**: Fully supported
- ⚠️ **Needs Update**: Must update to async/await patterns for RabbitMQ.Client 7.x

**Decision**: **KEEP** and update to async patterns

### RawRabbit.Enrichers.GlobalExecutionId

**Status**: **KEEP** - Maintain in 3.0

**Assessment**:
- ✅ **No External Dependencies**: Self-contained
- ✅ **Valuable Feature**: Distributed tracing support
- ✅ **.NET 9 Compatible**: No framework-specific code

**Decision**: **KEEP** and maintain

### RawRabbit.Enrichers.MessageContext

**Status**: **KEEP** - Maintain in 3.0

**Assessment**:
- ✅ **Core Functionality**: Message metadata and context propagation
- ✅ **No External Dependencies**: Self-contained
- ✅ **Widely Used**: Common use case

**Decision**: **KEEP** and maintain

### RawRabbit.Enrichers.QueueSuffix

**Status**: **KEEP** - Maintain in 3.0

**Assessment**:
- ✅ **No External Dependencies**: Self-contained
- ✅ **Useful Feature**: Dynamic queue naming

**Decision**: **KEEP** and maintain

### RawRabbit.Enrichers.RetryLater

**Status**: **KEEP** - Maintain in 3.0

**Assessment**:
- ✅ **No External Dependencies**: Self-contained
- ✅ **Valuable Pattern**: Delayed retry implementation

**Decision**: **KEEP** and maintain

### RawRabbit.Enrichers.Attributes

**Status**: **KEEP** - Maintain in 3.0

**Assessment**:
- ✅ **No External Dependencies**: Self-contained
- ✅ **Useful Feature**: Attribute-based configuration

**Decision**: **KEEP** and maintain

### RawRabbit.DependencyInjection.ServiceCollection

**Status**: **KEEP** - Maintain as PRIMARY DI integration

**Assessment**:
- ✅ **Built-in to .NET**: Microsoft.Extensions.DependencyInjection
- ✅ **Standard Choice**: Default DI container for .NET
- ✅ **No External Dependencies**: Part of .NET runtime

**Decision**: **KEEP** as primary DI integration

### RawRabbit.DependencyInjection.Autofac

**Status**: **KEEP** - Maintain in 3.0

**Assessment**:
- ✅ **Actively Maintained**: Autofac is well-maintained
- ✅ **Large User Base**: Popular enterprise DI container
- ✅ **.NET 9 Compatible**: Fully supported

**Decision**: **KEEP** and maintain

## Considered Options

### Option 1: Remove All Deprecated Packages Immediately

**Approach**: Delete ZeroFormatter and Ninject packages in 3.0, no deprecation period.

**Pros**:
- Clean codebase, no technical debt
- Clear signal to users about unsupported packages
- Reduced maintenance burden immediately

**Cons**:
- Abrupt change for users depending on these packages
- No migration period

**Trade-offs**:
- Clean break vs. user convenience

### Option 2: Mark Deprecated, Remove in 4.0

**Approach**: Keep packages in 3.0 with deprecation warnings, remove in 4.0.

**Pros**:
- Migration period for users
- Clear warnings in build output

**Cons**:
- Carries technical debt through 3.x
- Must maintain unmaintained dependencies
- Security risk from old packages

**Trade-offs**:
- Gradual deprecation vs. immediate cleanup

### Option 3: Remove from Main Repo, Maintain as Community Packages

**Approach**: Move deprecated packages to separate repositories, invite community maintenance.

**Pros**:
- Available for users who need them
- Not part of core maintenance burden
- Community can maintain if desired

**Cons**:
- Fragmentation of ecosystem
- No guarantee of maintenance
- May give false hope of support

**Trade-offs**:
- Offload maintenance vs. ecosystem fragmentation

## Decision Outcome

**Chosen Option**: "Remove All Deprecated Packages Immediately (in 3.0) + Comprehensive Migration Guide"

**Rationale**:

1. **Security**: Shipping unmaintained dependencies (ZeroFormatter 2017, Ninject beta) is a security liability.

2. **Major Version**: 3.0 is the appropriate time for breaking changes. Users expect deprecated features to be removed.

3. **Clear Alternatives**: MessagePack (for ZeroFormatter), Microsoft.Extensions.DI (for Ninject) are superior alternatives.

4. **No Maintenance Burden**: Keeping deprecated packages requires ongoing work with no benefit.

5. **.NET 9 Compatibility**: Updating ZeroFormatter and Ninject to .NET 9 would require significant work for abandoned dependencies.

**Packages to Remove**:
1. ❌ `RawRabbit.Enrichers.ZeroFormatter` → Use `RawRabbit.Enrichers.MessagePack`
2. ❌ `RawRabbit.DependencyInjection.Ninject` → Use `RawRabbit.DependencyInjection.ServiceCollection`
3. ❌ `NetFxHttpContextMiddleware.cs` (already removed) → Use `AspNetCoreHttpContextMiddleware`

**Packages to Keep**:
1. ✅ `RawRabbit.Enrichers.MessagePack` (actively maintained, high performance)
2. ✅ `RawRabbit.Enrichers.Protobuf` (Google-backed, industry standard)
3. ✅ `RawRabbit.Enrichers.Polly` (resilience patterns, must update to async)
4. ✅ `RawRabbit.Enrichers.GlobalExecutionId` (self-contained, useful)
5. ✅ `RawRabbit.Enrichers.MessageContext` (core functionality)
6. ✅ `RawRabbit.Enrichers.QueueSuffix` (self-contained, useful)
7. ✅ `RawRabbit.Enrichers.RetryLater` (self-contained, useful)
8. ✅ `RawRabbit.Enrichers.Attributes` (self-contained, useful)
9. ✅ `RawRabbit.Enrichers.HttpContext` (ASP.NET Core only, .NET Framework code removed)
10. ✅ `RawRabbit.DependencyInjection.ServiceCollection` (primary DI integration)
11. ✅ `RawRabbit.DependencyInjection.Autofac` (popular, maintained)

**Consequences**:

**Positive**:
- No unmaintained dependencies in RawRabbit 3.0
- Reduced security risk
- Cleaner codebase
- Lower maintenance burden
- Clear direction for users (use modern alternatives)
- Better performance (MessagePack vs. ZeroFormatter)

**Negative**:
- Users depending on ZeroFormatter must migrate to MessagePack
- Users depending on Ninject must migrate to Microsoft.Extensions.DI or Autofac
- .NET Framework users cannot use HttpContext enricher (must stay on 2.x)
- Migration effort required for affected users

**Risks**:
- **Low**: Small user base affected (these were niche packages)
- **Low**: Migration path is straightforward (documented alternatives)
- **Low**: Users can stay on RawRabbit 2.x if unable to migrate

## Implementation Notes

### Package Removal Process

1. **Delete Projects**:
   ```bash
   # Remove from solution and filesystem
   rm -rf src/RawRabbit.Enrichers.ZeroFormatter
   rm -rf src/RawRabbit.DependencyInjection.Ninject
   rm -f src/RawRabbit.Enrichers.HttpContext/NetFxHttpContextMiddleware.cs
   ```

2. **Update Solution File**: Remove project references from `RawRabbit.sln`

3. **Update Documentation**: Remove from package list, add deprecation notice

4. **Create Migration Guide Section**: Document alternatives and migration steps

### Migration Guide Sections

**For ZeroFormatter Users**:
```csharp
// RawRabbit 2.x (ZeroFormatter)
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    Plugins = p => p.UseZeroFormatter()
});

// RawRabbit 3.0 (MessagePack)
// Install: RawRabbit.Enrichers.MessagePack
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    Plugins = p => p.UseMessagePack()
});
```

**For Ninject Users**:
```csharp
// RawRabbit 2.x (Ninject)
var kernel = new StandardKernel();
kernel.RegisterRawRabbit(new RawRabbitConfiguration());

// RawRabbit 3.0 (Microsoft.Extensions.DependencyInjection)
var services = new ServiceCollection();
services.AddRawRabbit(new RawRabbitConfiguration());
var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IBusClient>();
```

**For .NET Framework HttpContext Users**:
```
RawRabbit 3.0 does not support .NET Framework.

Options:
1. Stay on RawRabbit 2.x (supported until Nov 2026)
2. Migrate to .NET 9 and use AspNetCoreHttpContextMiddleware
3. Implement custom context propagation if needed
```

### Package Versioning

- Removed packages: No 3.0 version published (users cannot accidentally install)
- NuGet metadata: Mark 2.x versions with deprecation notice
- README: Add deprecation banner to GitHub repositories

### Communication

1. **Announcement**: Blog post explaining rationale and alternatives
2. **Migration Guide**: Dedicated section for deprecated packages
3. **GitHub Issues**: Create tracking issues for each removed package
4. **Release Notes**: Clearly list removed packages and alternatives

## Links

- Related: [ADR 0001 Target Framework Selection](./ADR%200001%20Target%20Framework%20Selection.md)
- Related: [ADR 0003 Serialization Strategy](./ADR%200003%20Serialization%20Strategy.md)
- Related: [ADR 0004 Breaking Changes Strategy](./ADR%200004%20Breaking%20Changes%20Strategy.md)
- [MessagePack-CSharp GitHub](https://github.com/MessagePack-CSharp/MessagePack-CSharp)
- [ZeroFormatter GitHub](https://github.com/neuecc/ZeroFormatter) (unmaintained)
- [Microsoft.Extensions.DependencyInjection Docs](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
