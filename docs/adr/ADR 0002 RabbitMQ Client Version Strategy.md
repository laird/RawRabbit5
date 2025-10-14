# ADR 0002 RabbitMQ Client Version Strategy

**Status**: Proposed

**Date**: 2025-10-13

**Decision Makers**: Architecture Team, RawRabbit Maintainers

**Technical Story**: RawRabbit currently uses RabbitMQ.Client 5.0.1 (released 2017), which is 8 years old and does not support modern .NET frameworks. We need to upgrade to a modern version to support .NET 9 and gain access to performance improvements and API enhancements.

## Context and Problem Statement

Which version of RabbitMQ.Client should RawRabbit 3.0 target? The current version 5.0.1 is obsolete and prevents modernization. RabbitMQ.Client has undergone significant breaking changes through versions 6.x and 7.x, requiring careful migration planning.

## Decision Drivers

- **Breaking API Changes**: Major interface and method signature changes across versions
- **.NET Framework Compatibility**: Version 6+ requires .NET Standard 2.0 minimum
- **Performance Improvements**: Memory efficiency and throughput gains
- **Async/Await Support**: Modern asynchronous programming model
- **Active Maintenance**: Security patches and bug fixes
- **Migration Complexity**: Amount of code changes required
- **Backward Compatibility**: Impact on RawRabbit's middleware architecture
- **Community Adoption**: Stability and production usage

## Considered Options

### Option 1: RabbitMQ.Client 6.x (Latest: 6.8.1)

**Version Details**:
- Release: 2020
- Current: 6.8.1 (maintenance releases)
- Status: Stable, in maintenance mode
- .NET Requirement: .NET Standard 2.0+

**Key Breaking Changes from 5.x**:
- Requires .NET Framework 4.6.1 or .NET Standard 2.0 minimum
- Message payloads changed from `byte[]` to `ReadOnlyMemory<byte>`
- Consumer delivery payloads now use `ReadOnlyMemory<byte>`
- Improved memory management using System.Memory library
- Better throughput and reduced memory footprint

**Pros**:
- Well-tested and production-proven
- Fewer breaking changes than 7.x (incremental migration)
- Stable API surface
- Large production deployment base
- Intermediate step to eventual 7.x upgrade
- Good documentation and community knowledge

**Cons**:
- Still synchronous API (no async/await)
- Will eventually need 7.x migration anyway
- Missing latest performance optimizations
- Older programming patterns
- Not taking full advantage of modern .NET
- Delays inevitable migration to async model

**Trade-offs**:
- Easier migration now vs. deferred migration pain later
- Stability vs. modern async patterns
- Two-step migration (6.x then 7.x) vs. single jump to 7.x

### Option 2: RabbitMQ.Client 7.x (Latest: 7.1.2)

**Version Details**:
- Release: 2024
- Current: 7.1.2
- Status: Active development, recommended version
- .NET Requirement: .NET 6+

**Key Breaking Changes from 5.x/6.x**:
- **Async/Await Model**: Entire API converted to Task-based async (TAP)
  - All methods renamed with `Async` suffix
  - Example: `BasicPublish` → `BasicPublishAsync`
- **Interface Renaming**: `IModel` → `IChannel`
- **Consumer Changes**: `IBasicConsumer` → `IAsyncBasicConsumer`
- **BasicProperties**: `CreateBasicProperties()` removed, use `new BasicProperties()`
- **Publisher Confirmations**: Built-in efficient tracking system
- **Memory Model**: Uses `ReadOnlyMemory<byte>` for message payloads
- **Connection Factory**: API improvements

**Pros**:
- Modern async/await programming model (aligns with .NET best practices)
- Significant performance improvements (memory and throughput)
- Latest features and active development
- Built-in publisher confirmation tracking
- Better resource management
- Future-proof (official recommended version)
- Aligns with modern .NET 9 async patterns
- No need for follow-up migration to 8.x (doesn't exist yet)

**Cons**:
- Requires complete rewrite of RawRabbit middleware components
- Breaking changes to all publish/subscribe operations
- IModel → IChannel rename affects all code
- Consumer model completely different (async handlers)
- Larger migration effort than 6.x
- May expose edge-case bugs in new async infrastructure

**Trade-offs**:
- Large upfront migration cost vs. future-proof async architecture
- Breaking changes now vs. incremental migrations later
- Modern patterns vs. familiar synchronous code

### Option 3: Stay on RabbitMQ.Client 5.x

**Pros**:
- No migration required
- Familiar API
- Zero breaking changes

**Cons**:
- **Incompatible with .NET 9**: Cannot compile with modern frameworks
- No security patches or bug fixes
- Missing 8 years of performance improvements
- Prevents using modern memory management
- Blocks entire RawRabbit modernization effort
- Technical debt accumulation
- Not viable option

**Trade-offs**:
- This is not a viable option - eliminates modernization entirely

### Option 4: Multi-target 6.x + 7.x

**Pros**:
- Gradual migration path for consumers
- Allows testing both versions
- Backward compatibility option

**Cons**:
- Extreme complexity managing two completely different APIs
- Conditional compilation nightmare (sync vs async)
- Doubles testing surface area
- Cannot maintain consistent middleware patterns
- Different consumer models (sync vs async)
- Maintenance burden unsustainable
- Pipeline infrastructure complexity

**Trade-offs**:
- This is architecturally infeasible given the sync/async divide

## Decision Outcome

**Chosen Option**: "RabbitMQ.Client 7.x (Latest: 7.1.2)"

**Rationale**:

1. **Architectural Alignment**: RawRabbit 3.0 is a major version rewrite. The complete migration to async/await aligns perfectly with this breaking change window.

2. **Modern .NET Compatibility**: Version 7.x is designed for .NET 6+ and fully leverages modern runtime capabilities, matching our .NET 9 target (ADR 0001).

3. **Future-Proof**: Migrating to 7.x now prevents another major migration later. Version 6.x is in maintenance mode, and all new development targets 7.x.

4. **Performance Critical**: As a messaging library, RawRabbit benefits enormously from 7.x's memory efficiency and throughput improvements. The async model prevents thread pool starvation.

5. **Async Middleware Patterns**: RawRabbit's middleware pipeline architecture adapts naturally to async/await. Middleware already returns `Task`, making async operations a natural fit.

6. **Single Migration Window**: Since we're already doing a major version bump (2.x → 3.0), combining both .NET 9 and RabbitMQ.Client 7.x migrations reduces total disruption.

7. **Industry Direction**: Async/await is the standard pattern in modern .NET. Staying synchronous in 2025 would be architectural regression.

**Consequences**:

**Positive**:
- Modern async/await patterns throughout codebase
- Significant performance improvements (memory + throughput)
- Built-in publisher confirmation tracking simplifies code
- Better resource utilization (no thread blocking)
- Future-proof architecture for 5+ years
- Aligns with .NET ecosystem best practices
- Improved developer experience with async patterns

**Negative**:
- **High migration complexity**: Every middleware component requires rewriting
  - All `IModel` references → `IChannel`
  - All RabbitMQ operations → async methods
  - Consumer implementations → async handlers
  - Error handling → async exception patterns
- Breaking changes to all RawRabbit public APIs
- Users must update all handler code to async
- Documentation requires complete rewrite
- Testing complexity increases (async test patterns)

**Risks**:
- **High**: Migration errors in middleware pipeline (mitigated by comprehensive testing)
- **Medium**: Performance regression if async not implemented correctly (mitigated by benchmarking)
- **Medium**: Backward compatibility completely broken (acceptable for 3.0, documented in migration guide)
- **Low**: Edge cases in async consumer handling (mitigated by thorough integration tests)

## Implementation Notes

### Critical Breaking Changes to Address

1. **IModel → IChannel**:
   ```csharp
   // OLD (5.x/6.x)
   IModel channel = connection.CreateModel();

   // NEW (7.x)
   IChannel channel = await connection.CreateChannelAsync();
   ```

2. **Publishing**:
   ```csharp
   // OLD
   channel.BasicPublish(exchange, routingKey, basicProperties, body);

   // NEW
   await channel.BasicPublishAsync(exchange, routingKey, basicProperties, body);
   ```

3. **BasicProperties Creation**:
   ```csharp
   // OLD
   var props = channel.CreateBasicProperties();

   // NEW
   var props = new BasicProperties();
   ```

4. **Consumer Model**:
   ```csharp
   // OLD
   public class MyConsumer : IBasicConsumer {
       public void HandleBasicDeliver(string consumerTag, ulong deliveryTag,
           bool redelivered, string exchange, string routingKey,
           IBasicProperties properties, byte[] body) { }
   }

   // NEW
   public class MyConsumer : IAsyncBasicConsumer {
       public Task HandleBasicDeliverAsync(string consumerTag, ulong deliveryTag,
           bool redelivered, string exchange, string routingKey,
           IBasicProperties properties, ReadOnlyMemory<byte> body) { }
   }
   ```

5. **Message Body Handling**:
   - Body is `ReadOnlyMemory<byte>`, must copy if storing: `body.ToArray()`
   - Only valid during the event handler scope

### Migration Strategy

1. **Phase 1**: Update core `IChannel` management in RawRabbit.Channel
2. **Phase 2**: Convert all middleware to async (Publish, Subscribe, Request, Respond)
3. **Phase 3**: Update consumer factory for `IAsyncBasicConsumer`
4. **Phase 4**: Migrate enrichers (Polly, MessageContext, etc.)
5. **Phase 5**: Update DI containers and factory patterns
6. **Phase 6**: Migrate samples and integration tests
7. **Phase 7**: Performance benchmarking and optimization

### Testing Requirements

- Comprehensive integration tests with real RabbitMQ instance
- Async/await pattern validation
- Memory management tests (ReadOnlyMemory handling)
- Publisher confirmation testing
- Consumer async handler testing
- Error handling and exception propagation
- Performance benchmarks vs. 2.x baseline

### Documentation Updates

- Complete API reference rewrite (sync → async)
- Migration guide for users upgrading from 2.x
- New code examples showing async patterns
- Middleware development guide updates
- Consumer implementation examples

## Links

- Related: [ADR 0001 Target Framework Selection](./ADR%200001%20Target%20Framework%20Selection.md)
- Related: [ADR 0004 Breaking Changes Strategy](./ADR%200004%20Breaking%20Changes%20Strategy.md)
- [RabbitMQ.Client 7.x Migration Guide](https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/main/v7-MIGRATION.md)
- [RabbitMQ.Client GitHub Repository](https://github.com/rabbitmq/rabbitmq-dotnet-client)
- [RabbitMQ .NET Client API Guide](https://www.rabbitmq.com/client-libraries/dotnet-api-guide)
