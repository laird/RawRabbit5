# ADR 0003 Serialization Strategy

**Status**: Proposed

**Date**: 2025-10-13

**Decision Makers**: Architecture Team, RawRabbit Maintainers

**Technical Story**: RawRabbit currently uses Newtonsoft.Json 10.0.1 (2017) for JSON serialization. With the modernization to .NET 9, we need to evaluate whether to continue with Newtonsoft.Json (upgraded to 13.0.3) or migrate to System.Text.Json, which is built into the .NET runtime.

## Context and Problem Statement

Which JSON serialization library should RawRabbit 3.0 use as its default serializer? JSON is the primary message format for most RawRabbit users. The choice affects performance, compatibility, features, and dependencies.

## Decision Drivers

- **Performance**: Serialization/deserialization speed and memory efficiency
- **Default Experience**: Out-of-box behavior for majority of users
- **Feature Set**: Support for complex types, polymorphism, custom converters
- **Backward Compatibility**: Can users migrate existing messages?
- **Dependencies**: External packages vs. built-in runtime support
- **Memory Allocation**: Garbage collection pressure
- **Standards Compliance**: JSON specification adherence
- **Security**: Vulnerability exposure and patch availability
- **Plugin Architecture**: Ability to swap serializers

## Considered Options

### Option 1: Newtonsoft.Json 13.0.3 (Upgrade from 10.0.1)

**Library Details**:
- Package: Newtonsoft.Json (Json.NET)
- Current Version: 13.0.3
- Maturity: 15+ years, industry standard
- License: MIT
- Package Size: ~700 KB

**Pros**:
- **Backward Compatible**: Upgrading from 10.0.1 → 13.0.3 maintains compatibility
- **Feature Rich**: Comprehensive support for complex scenarios
  - LINQ to JSON for query operations
  - Polymorphic deserialization with `$type` metadata
  - Extensive custom converter ecosystem
  - Flexible attribute-based configuration
  - Excellent handling of circular references
  - DateTimeOffset, TimeSpan, and Guid built-in support
- **Proven Track Record**: 15+ years production use, extremely stable
- **Community Knowledge**: Massive documentation, Stack Overflow answers
- **Existing Converters**: Large ecosystem of third-party converters
- **Flexible by Default**: Handles edge cases gracefully
- **RawRabbit Users**: Existing 2.x users likely already using it
- **Zero Migration**: No breaking changes for existing message formats

**Cons**:
- **External Dependency**: Requires NuGet package (not in runtime)
- **Performance**: Slower than System.Text.Json for most scenarios
  - 50%+ slower serialization in benchmarks
  - Higher memory allocations
  - More GC pressure
- **Security Concerns**: History of CVEs (though 13.0.3 addresses known issues)
- **Not The Future**: Microsoft is not investing in it
- **Package Size**: Adds ~700 KB to deployment

**Trade-offs**:
- Features and flexibility vs. performance
- Stability and compatibility vs. modern .NET patterns
- External dependency vs. runtime built-in

**Migration from RawRabbit 2.x**:
- Zero breaking changes for users
- Message format remains identical
- No code changes required

### Option 2: System.Text.Json (Built-in)

**Library Details**:
- Package: Part of .NET runtime (no external package)
- Introduced: .NET Core 3.0
- Maturity: 6 years (since 2019)
- License: MIT (part of .NET)
- Package Size: 0 (runtime included)

**Pros**:
- **Performance**: 50%+ faster serialization/deserialization
  - Optimized for modern .NET runtime
  - Lower memory allocations
  - Reduced GC pressure
  - Span<T> and Memory<T> optimizations
- **Built-in**: No external dependencies, smaller deployment
- **Security**: Maintained by Microsoft, quick security patches
- **Modern .NET**: Designed for async/await and modern patterns
- **Standards Compliant**: Strict JSON specification adherence
- **Future-Proof**: Actively developed, improving each .NET release
- **UTF-8 Support**: Native UTF-8 handling (most efficient)
- **Deterministic**: Predictable behavior, less "magic"

**Cons**:
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
- **Learning Curve**: Different API patterns than Newtonsoft.Json
- **Custom Converters**: More verbose to implement

**Trade-offs**:
- Performance and modern patterns vs. feature completeness
- Zero dependencies vs. learning curve
- Strictness vs. flexibility

**Migration from RawRabbit 2.x**:
- **Potential Breaking Changes** for edge cases:
  - DateTime serialization format (ISO 8601 vs. custom formats)
  - Enum handling (numeric vs. string by default)
  - Property naming (case-sensitive by default)
  - Missing fields (strict vs. lenient)
- Most common use cases: Compatible
- Complex types with polymorphism: Requires code changes

### Option 3: Keep Newtonsoft.Json as Default, Offer System.Text.Json Plugin

**Architecture**:
- Default: Newtonsoft.Json 13.0.3
- Optional: System.Text.Json enricher package
- Interface: `ISerializer` abstraction

**Pros**:
- **Zero Breaking Changes**: Backward compatible with RawRabbit 2.x
- **User Choice**: Performance-focused users can opt into System.Text.Json
- **Gradual Migration**: Users can migrate at their own pace
- **Best of Both**: Feature-rich default, performance option available
- **Reduced Risk**: Conservative approach for major version

**Cons**:
- **Complexity**: Two serialization paths to maintain and test
- **Documentation Burden**: Must document both approaches
- **Default Experience**: Most users get slower performance by default
- **Dependency Still Required**: Still shipping with Newtonsoft.Json
- **Testing Overhead**: Must test both serializers
- **Version Confusion**: Which serializer should users choose?

**Trade-offs**:
- Flexibility vs. maintenance complexity
- Backward compatibility vs. optimal default experience
- User choice vs. opinionated design

### Option 4: System.Text.Json Default, Newtonsoft.Json Compatibility Plugin

**Architecture**:
- Default: System.Text.Json (built-in)
- Optional: Newtonsoft.Json enricher for backward compatibility
- Interface: `ISerializer` abstraction

**Pros**:
- **Modern Default**: Best performance out-of-box
- **Zero External Dependencies**: Core package has no external serializers
- **Future-Proof**: Aligns with .NET direction
- **Backward Compatibility Option**: Users can install Newtonsoft enricher if needed
- **Performance First**: Optimized default experience
- **Clear Direction**: Modern approach, legacy support available

**Cons**:
- **Migration Required**: Users with complex types must update code
- **Breaking Change**: Different default behavior than 2.x
- **Plugin for Compatibility**: Requires enricher for legacy formats
- **Documentation**: Must provide migration guide

**Trade-offs**:
- Modern default vs. migration friction
- Performance optimization vs. backward compatibility
- Opinionated (better default) vs. flexible (user choice)

## Decision Outcome

**Chosen Option**: "System.Text.Json Default, Newtonsoft.Json Compatibility Plugin"

**Rationale**:

1. **Major Version Philosophy**: RawRabbit 3.0 is a major version with breaking changes. This is the appropriate time to modernize the default experience.

2. **Performance Critical**: As a messaging library, serialization performance directly impacts throughput and latency. System.Text.Json's 50%+ performance improvement is significant.

3. **.NET 9 Alignment**: Using the built-in serializer aligns with modern .NET practices and reduces external dependencies.

4. **Zero Dependencies Core**: The core RawRabbit package has no external dependencies for the default use case, simplifying deployment.

5. **Backward Compatibility Path**: Users requiring Newtonsoft.Json compatibility can install `RawRabbit.Enrichers.Newtonsoft.Json` enricher package.

6. **Future-Proof**: System.Text.Json continues to improve with each .NET release, while Newtonsoft.Json is in maintenance mode.

7. **Common Cases Work**: 90%+ of RabbitMQ messages are simple DTOs that serialize identically in both libraries.

8. **Async Patterns**: System.Text.Json's async serialization methods align with RabbitMQ.Client 7.x's async API.

**Consequences**:

**Positive**:
- 50%+ faster serialization/deserialization for most users
- Lower memory allocations and GC pressure
- Zero external dependencies in core package
- Smaller deployment size
- Future performance improvements with .NET releases
- Modern, maintainable codebase
- Security patches from Microsoft
- UTF-8 optimization benefits

**Negative**:
- Breaking change from RawRabbit 2.x default behavior
- Users with complex types must update code or install compatibility package
- DateTime/Enum handling differences may require migration
- Less flexible with edge cases (requires explicit configuration)
- Custom converters require learning new API
- Polymorphic types require `[JsonDerivedType]` attributes

**Risks**:
- **Medium**: Users with complex message types face migration effort (mitigated by Newtonsoft enricher)
- **Low**: Edge case serialization differences (mitigated by comprehensive migration guide)
- **Low**: Missing features for advanced scenarios (mitigated by plugin architecture)

## Implementation Notes

### Core Package (RawRabbit)

**Default Serializer**:
```csharp
public class SystemTextJsonSerializer : ISerializer
{
    private readonly JsonSerializerOptions _options;

    public SystemTextJsonSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, // Backward compat
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter() // String enums by default
            }
        };
    }

    public Task<byte[]> SerializeAsync<T>(T obj) =>
        Task.FromResult(JsonSerializer.SerializeToUtf8Bytes(obj, _options));

    public Task<T> DeserializeAsync<T>(byte[] data) =>
        Task.FromResult(JsonSerializer.Deserialize<T>(data, _options));
}
```

**Configuration API**:
```csharp
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    Plugins = p => p
        .UseSystemTextJson(options => {
            options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.Converters.Add(new MyCustomConverter());
        })
});
```

### Compatibility Package (RawRabbit.Enrichers.Newtonsoft.Json)

Create new enricher package for users requiring backward compatibility:

```csharp
// Package: RawRabbit.Enrichers.Newtonsoft.Json
public class NewtonsoftJsonSerializer : ISerializer
{
    private readonly JsonSerializerSettings _settings;

    public NewtonsoftJsonSerializer(JsonSerializerSettings? settings = null)
    {
        _settings = settings ?? new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None, // Security
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore
        };
    }
}
```

**Usage**:
```csharp
// Install: RawRabbit.Enrichers.Newtonsoft.Json
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    Plugins = p => p.UseNewtonsoftJson()
});
```

### Migration Guide Sections

1. **No Changes Required**: Simple DTOs, POCOs
2. **Minor Updates**: DateTime, Enum handling
3. **Attribute Changes**: Newtonsoft attributes → System.Text.Json attributes
4. **Custom Converters**: Migration guide for converter patterns
5. **Compatibility Package**: When to use Newtonsoft enricher
6. **Performance Benchmarks**: Show performance gains

### Testing Strategy

1. **Compatibility Tests**: Verify common message types serialize identically
2. **Performance Benchmarks**: Demonstrate speed improvements
3. **Edge Case Testing**: Document differences in edge cases
4. **Plugin Testing**: Test both serializers via enricher pattern
5. **Integration Tests**: Real RabbitMQ scenarios with both serializers

### Deprecation of Other Serializers

- **RawRabbit.Enrichers.MessagePack**: Keep (popular, specialized use case)
- **RawRabbit.Enrichers.Protobuf**: Keep (specialized binary protocol)
- **RawRabbit.Enrichers.ZeroFormatter**: Deprecate (see ADR 0005)

## Links

- Related: [ADR 0001 Target Framework Selection](./ADR%200001%20Target%20Framework%20Selection.md)
- Related: [ADR 0002 RabbitMQ Client Version Strategy](./ADR%200002%20RabbitMQ%20Client%20Version%20Strategy.md)
- Related: [ADR 0005 Deprecated Package Strategy](./ADR%200005%20Deprecated%20Package%20Strategy.md)
- [System.Text.Json Documentation](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview)
- [Newtonsoft.Json → System.Text.Json Migration Guide](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft)
- [Performance Benchmarks](https://trevormccubbin.medium.com/net-performance-analysis-newtonsoft-json-vs-system-text-json-in-net-8-34520c21d054)
