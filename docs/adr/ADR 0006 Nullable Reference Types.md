# ADR 0006 Nullable Reference Types

**Status**: Proposed

**Date**: 2025-10-13

**Decision Makers**: Architecture Team, RawRabbit Maintainers

**Technical Story**: Modern C# (since C# 8.0) supports nullable reference types, providing compile-time null safety. RawRabbit 3.0 targets .NET 9 with C# 13, making nullable reference types available. We need to decide whether to enable this feature and how aggressively to adopt it.

## Context and Problem Statement

Should RawRabbit 3.0 enable nullable reference types? How should we balance null safety benefits with migration complexity and API ergonomics?

## Decision Drivers

- **Type Safety**: Compile-time null checking prevents NullReferenceExceptions
- **Code Quality**: Explicit nullability improves API clarity
- **Ecosystem Alignment**: Modern .NET libraries enable nullable reference types
- **Migration Effort**: Requires annotating entire codebase
- **Breaking Changes**: May expose nullability issues in consuming code
- **Developer Experience**: Better IntelliSense and tooling support
- **API Design**: Forces explicit decisions about null handling

## Considered Options

### Option 1: Enable Nullable Reference Types Globally

**Implementation**:
```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

**Approach**: Enable for all RawRabbit projects, annotate entire codebase, treat warnings as errors.

**Pros**:
- **Maximum Type Safety**: Compile-time null checks across entire library
- **Clear API Contracts**: Users know exactly what can be null
- **Tooling Support**: IntelliSense shows nullability information
- **Ecosystem Standard**: Aligns with modern .NET libraries
- **Fewer Runtime Errors**: Catches null reference bugs at compile time
- **Code Quality**: Forces explicit null handling decisions

**Cons**:
- **High Migration Cost**: Must annotate thousands of lines of code
- **Initial Complexity**: Learning curve for nullable annotations
- **Warning Noise**: Many warnings during transition period
- **Generic Constraints**: Complex nullability in generic middleware
- **API Changes**: Some method signatures may need adjustments

**Trade-offs**:
- Upfront annotation effort vs. long-term maintainability
- Strict null safety vs. annotation complexity

### Option 2: Enable Nullable Reference Types, Warnings Only

**Implementation**:
```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
</PropertyGroup>
```

**Approach**: Enable nullable reference types but allow warnings without breaking builds.

**Pros**:
- **Gradual Adoption**: Can annotate code incrementally
- **Flexible Timeline**: No rush to fix all warnings immediately
- **Lower Barrier**: Easier to enable initially
- **Still Provides Guidance**: Warnings guide toward better null handling

**Cons**:
- **Warning Fatigue**: Many warnings may be ignored
- **Inconsistent**: Some code annotated, some not
- **False Safety**: Looks safe but warnings indicate issues
- **Technical Debt**: Warnings accumulate over time
- **CI/CD Noise**: Build output cluttered with warnings

**Trade-offs**:
- Gradual migration vs. inconsistent codebase
- Flexibility vs. technical debt accumulation

### Option 3: Disable Nullable Reference Types

**Implementation**:
```xml
<PropertyGroup>
  <Nullable>disable</Nullable>
</PropertyGroup>
```

**Approach**: Do not enable nullable reference types, maintain status quo.

**Pros**:
- **Zero Migration Cost**: No annotation work required
- **Simple**: No new concepts to learn
- **No Breaking Changes**: API remains unchanged

**Cons**:
- **Missed Opportunity**: RawRabbit 3.0 is major rewrite, perfect time to adopt
- **Behind Ecosystem**: Modern .NET libraries enable nullable reference types
- **Runtime Errors**: No compile-time null checking
- **Poor Tooling**: IntelliSense cannot show nullability information
- **Code Quality**: Implicit null handling is less clear
- **Future Migration**: Will need to enable eventually anyway

**Trade-offs**:
- Avoiding migration pain vs. missing modern .NET features
- Not viable for a modern library in 2025

### Option 4: Enable Selectively (Per-Project or Per-File)

**Implementation**:
```xml
<!-- Core projects -->
<Nullable>enable</Nullable>

<!-- Enricher projects -->
<Nullable>annotations</Nullable>
```

Or per-file:
```csharp
#nullable enable
// Code here has nullable reference types enabled
#nullable restore
```

**Approach**: Enable nullable reference types for new/critical code, leave legacy code without annotations.

**Pros**:
- **Targeted Approach**: Focus on critical paths first
- **Incremental Migration**: Spread work over time
- **Priority-Based**: Annotate public APIs first

**Cons**:
- **Inconsistent**: Different projects have different rules
- **Confusion**: Developers must remember which files/projects are annotated
- **Partial Safety**: Only some code is null-checked
- **Maintenance Complexity**: Managing different nullable contexts

**Trade-offs**:
- Incremental vs. consistent approach
- Flexibility vs. confusion

## Decision Outcome

**Chosen Option**: "Enable Nullable Reference Types Globally with Warnings as Errors"

**Rationale**:

1. **Major Version Window**: RawRabbit 3.0 is a complete rewrite with breaking changes. This is the optimal time to enable nullable reference types.

2. **Ecosystem Alignment**: Modern .NET libraries (ASP.NET Core, EF Core, etc.) enable nullable reference types. Users expect this from 2025 libraries.

3. **Type Safety**: Messaging libraries handle user data and configuration. Null reference exceptions in production are unacceptable. Compile-time checking prevents these bugs.

4. **API Clarity**: Explicitly marking what can be null (`string?` vs `string`) improves API usability and documentation.

5. **RabbitMQ.Client 7.x**: The new RabbitMQ.Client already uses nullable reference types, making this adoption smoother.

6. **Async Rewrite**: Since we're already rewriting middleware for async/await (ADR 0002), the additional effort to annotate nullability is relatively small.

7. **Developer Experience**: Modern tooling (IntelliSense, analyzers) provides excellent support for nullable reference types, making adoption easier.

**Phased Implementation**:

While the end goal is full annotation, we'll implement in phases to manage complexity:

**Phase 1: Core Interfaces** (Week 1)
- `IBusClient`, `IChannel`, `IPipeContext`
- Public-facing API contracts
- Configuration classes

**Phase 2: Middleware Pipeline** (Week 2-3)
- All middleware base classes
- Middleware implementations (Publish, Subscribe, Request, etc.)
- Pipeline infrastructure

**Phase 3: Enrichers** (Week 4-5)
- Polly, MessageContext, GlobalExecutionId
- Serialization enrichers
- DI containers

**Phase 4: Internal Infrastructure** (Week 6)
- Channel management
- Consumer factory
- Connection handling

**Consequences**:

**Positive**:
- Compile-time null safety prevents NullReferenceExceptions
- Clear API contracts (users know what can be null)
- Better IntelliSense and tooling support
- Improved code quality and maintainability
- Aligns with modern .NET ecosystem standards
- Forces explicit null handling decisions (better design)
- Easier to reason about code flow
- Documentation auto-generated from nullability annotations

**Negative**:
- Annotation effort required for entire codebase
- Learning curve for nullable reference type patterns
- Some method signatures may become more verbose
- Generic type constraints become more complex
- May expose nullability issues in consuming code (actually a positive)
- Initial build may have many warnings to resolve

**Risks**:
- **Medium**: Complex generic scenarios may be difficult to annotate (mitigated by gradual approach)
- **Low**: Annotation errors may introduce false safety (mitigated by comprehensive testing)
- **Low**: Developers unfamiliar with nullable reference types (mitigated by documentation)

## Implementation Notes

### Project File Configuration

```xml
<PropertyGroup>
  <!-- Enable nullable reference types -->
  <Nullable>enable</Nullable>

  <!-- Treat warnings as errors for CI/CD -->
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

  <!-- Optional: Stricter analysis -->
  <AnalysisMode>AllEnabledByDefault</AnalysisMode>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

### Common Patterns

**1. Configuration Classes**:
```csharp
public class RawRabbitConfiguration
{
    // Required properties (non-nullable)
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    // Optional properties (nullable)
    public TimeSpan? RequestTimeout { get; set; }
    public string? VirtualHost { get; set; }
}
```

**2. Middleware Context Properties**:
```csharp
public interface IPipeContext
{
    // Type-safe property access
    T? Get<T>(string key) where T : class;
    T GetOrDefault<T>(string key, T defaultValue) where T : notnull;
}
```

**3. Handler Signatures**:
```csharp
// Clear nullability contracts
public delegate Task<Acknowledgement> MessageHandler<T>(
    T message,
    MessageContext? context = null) where T : notnull;
```

**4. Internal Null Checks**:
```csharp
public async Task PublishAsync<T>(T message, Action<IPublishContext>? configure = null)
    where T : notnull
{
    ArgumentNullException.ThrowIfNull(message);

    // configure is explicitly nullable, handle null case
    var context = new PublishContext();
    configure?.Invoke(context);

    await PublishInternalAsync(message, context);
}
```

**5. Generic Constraints**:
```csharp
// Message types cannot be null
public interface IBusClient
{
    Task PublishAsync<T>(T message) where T : notnull;
    Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
        where TRequest : notnull
        where TResponse : notnull;
}
```

### Annotation Guidelines

**Public APIs**:
- Be explicit: Mark parameters as `?` if null is acceptable
- Use `notnull` constraint for generic message types
- Document nullability in XML comments

**Internal Code**:
- Use `ArgumentNullException.ThrowIfNull()` for parameter validation
- Prefer `??` and `?.` operators over null checks
- Use `!` (null-forgiving) operator sparingly, with comments explaining why

**Collections**:
- Prefer `IReadOnlyList<T>` over `IEnumerable<T>?` (empty collection vs. null)
- Use `Array.Empty<T>()` instead of returning null

**Async Methods**:
- Return `Task<T>` not `Task<T?>` unless null is semantically meaningful
- Use `ValueTask<T>` for hot paths (if T is not nullable, neither is ValueTask<T>)

### Migration Checklist

- [ ] Enable nullable reference types in all `.csproj` files
- [ ] Annotate public interfaces (`IBusClient`, `IChannel`, etc.)
- [ ] Annotate public classes and methods
- [ ] Add `ArgumentNullException` checks for non-nullable parameters
- [ ] Review and annotate generic type constraints
- [ ] Update XML documentation to match nullability
- [ ] Run static analysis and address warnings
- [ ] Update code samples and documentation
- [ ] Review PR checklist to enforce nullability annotations

### Testing Strategy

**Nullable Warnings as Test Failures**:
- CI/CD must treat warnings as errors
- Pull requests cannot merge with nullable warnings
- Automated analysis in code review

**Test Patterns**:
```csharp
[Fact]
public async Task PublishAsync_WithNullMessage_ThrowsArgumentNullException()
{
    var client = CreateClient();
    await Assert.ThrowsAsync<ArgumentNullException>(
        () => client.PublishAsync<string>(null!)); // null! only in tests
}

[Fact]
public async Task SubscribeAsync_WithNullHandler_ThrowsArgumentNullException()
{
    var client = CreateClient();
    await Assert.ThrowsAsync<ArgumentNullException>(
        () => client.SubscribeAsync<MyMessage>(null!));
}
```

### Documentation Updates

**Migration Guide Section**:
- Explain nullable reference types for users unfamiliar
- Show before/after code examples
- Document how to handle warnings in consuming code
- Provide guidance on enabling nullable reference types in user projects

**API Reference**:
- Nullability information in method signatures
- XML comments explaining when null is acceptable
- Examples showing null handling patterns

## Links

- Related: [ADR 0001 Target Framework Selection](./ADR%200001%20Target%20Framework%20Selection.md)
- Related: [ADR 0004 Breaking Changes Strategy](./ADR%200004%20Breaking%20Changes%20Strategy.md)
- [Nullable Reference Types Documentation](https://learn.microsoft.com/dotnet/csharp/nullable-references)
- [Nullable Reference Types in Practice](https://learn.microsoft.com/dotnet/csharp/nullable-migration-strategies)
- [Update Libraries to Use Nullable Reference Types](https://learn.microsoft.com/dotnet/csharp/nullable-attributes)
