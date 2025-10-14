# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RawRabbit is a modern .NET client library for RabbitMQ communication using a **middleware-oriented architecture** with a **pipe-based execution model**. Version 2.x uses middleware chains for all message operations (publish, subscribe, request/response, etc.).

**Current State**: Multi-targeting `netstandard1.5` and `net451` (legacy frameworks)
**Target**: Migrate to modern .NET 9.0

## Build & Test Commands

### Building

```bash
# Build entire solution
~/.dotnet/dotnet build RawRabbit.sln --configuration Release

# Build specific project
~/.dotnet/dotnet build src/RawRabbit/RawRabbit.csproj

# Clean build
~/.dotnet/dotnet clean && ~/.dotnet/dotnet build RawRabbit.sln --configuration Release
```

### Testing

```bash
# Run all unit tests
~/.dotnet/dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj --logger "console;verbosity=detailed"

# Run integration tests (requires RabbitMQ on localhost:5672)
~/.dotnet/dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj

# Run specific test
~/.dotnet/dotnet test --filter "FullyQualifiedName~RawRabbit.Tests.ChannelFactoryTests"

# Performance tests
~/.dotnet/dotnet build test/RawRabbit.PerformanceTest/RawRabbit.PerformanceTest.csproj
```

### Integration Test Setup

Integration tests require RabbitMQ:

```bash
# Start RabbitMQ via Docker
docker run -d --name rabbitmq-test -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Stop after tests
docker stop rabbitmq-test && docker rm rabbitmq-test
```

## Architecture

### Modular Package Structure

RawRabbit is organized into **28 separate projects**:

**Core Library:** `src/RawRabbit/`
- Base client (`IBusClient`, `RawRabbitFactory`)
- Pipe infrastructure (`IPipeContext`, `IPipeBuilder`, `Middleware`)
- Channel management and lifecycle
- Configuration (`RawRabbitConfiguration`)

**Operations** (message patterns): `src/RawRabbit.Operations.*/`
- `Publish` - Fire-and-forget
- `Subscribe` - Message consumption
- `Request` / `Respond` - RPC (uses direct reply-to)
- `Get` - Single message retrieval
- `MessageSequence` - Choreographed flows
- `StateMachine` - Stateful handling
- `Tools` - Utilities

**Enrichers** (plugins): `src/RawRabbit.Enrichers.*/`
- `Attributes` - Attribute-based config
- `MessageContext` - Message metadata
- `Polly` - Resilience policies
- `GlobalExecutionId` - Distributed tracing
- `QueueSuffix` - Dynamic queue naming
- `HttpContext` - ASP.NET integration
- `RetryLater` - Delayed retry
- `Protobuf`, `MessagePack`, `ZeroFormatter` - Serialization

**DI Adapters:** `src/RawRabbit.DependencyInjection.*/`
- `ServiceCollection` (Microsoft DI)
- `Autofac`
- `Ninject`

**Samples:** `sample/`
- `ConsoleApp.Sample`
- `AspNet.Sample`
- `Messages.Sample`

### Pipe and Middleware Architecture

RawRabbit 2.x uses a **middleware pipeline pattern** (inspired by ASP.NET Core):

**Core Concepts:**
- `IPipeContext` - Request-scoped property bag (dictionary) flowing through pipeline
- `IPipeBuilder` - Fluent API for building middleware chains
- `Middleware` - Base class for pipeline components (each calls `next()`)
- `StagedMiddleware` - Executes at specific pipeline stages

**Execution Flow:**
1. Operation (Publish, Subscribe, etc.) builds middleware pipeline via `IPipeBuilder`
2. Pipeline executes sequentially, each middleware calls `next()`
3. Middleware can short-circuit (not call next) or modify context properties
4. Context carries configuration, messages, and operation-specific data

**Example Pipeline for PublishAsync:**
```
[PublishConfigurationMiddleware] → [ExchangeDeclareMiddleware]
→ [BodySerializationMiddleware] → [BasicPublishMiddleware]
```

**Pipeline Customization:**
```csharp
await client.PublishAsync(message, ctx => ctx
    .UsePublishConfiguration(cfg => cfg
        .OnExchange("custom_exchange")
        .WithRoutingKey("custom_key")));
```

### Acknowledgement Model

RawRabbit treats acknowledgements as **first-class return types**:

```csharp
await client.SubscribeAsync<MyMessage>(msg => {
    if (CannotProcess(msg))
        return new Nack(requeue: true);

    Process(msg);
    return new Ack();
});
```

**Return types:** `Ack()`, `Nack(requeue)`, `Reject(requeue)`, `Retry.In(TimeSpan)`

### Key Files

**Core abstractions:**
- `src/RawRabbit/IBusClient.cs` - Main client interface (single method: `InvokeAsync`)
- `src/RawRabbit/Pipe/IPipeContext.cs` - Context interface (property bag)
- `src/RawRabbit/Pipe/PipeBuilder.cs` - Middleware chain builder

**Factory:**
- `src/RawRabbit/Instantiation/RawRabbitFactory.cs` - Client factory

**Configuration:**
- `src/RawRabbit/Configuration/RawRabbitConfiguration.cs` - Config model

**Channel Management:**
- `src/RawRabbit/Channel/` - Channel pooling and lifecycle

## Key Dependencies

**Production:**
- `RabbitMQ.Client` 5.0.1 (needs update to 6.x+ for modern .NET)
- `Newtonsoft.Json` 10.0.1 (needs update to 13.0.3+ for security)

**Testing:**
- `xunit` for unit tests
- `NSubstitute` for mocking
- `BenchmarkDotNet` for performance tests

## Project Dependencies

Migration order must respect dependency graph:

```
RawRabbit (core)
  ↓
Operations.* (depend on core)
  ↓
Enrichers.* (depend on core, some on operations)
  ↓
DependencyInjection.* (depend on core)
  ↓
Samples (depend on multiple packages)
```

## Configuration

RawRabbit is configured via `RawRabbitOptions`:

```csharp
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
  ClientConfiguration = new ConfigurationBuilder()
    .AddJsonFile("rawrabbit.json")
    .Build()
    .Get<RawRabbitConfiguration>(),
  Plugins = p => p
    .UseProtobuf()
    .UsePolly(/* policies */),
  DependencyInjection = ioc => ioc
    .AddSingleton<IChannelFactory, CustomChannelFactory>()
});
```

**Key settings:**
- Connection (hostnames, credentials, vhost, port)
- Timeouts (request, publish confirm, recovery)
- Topology defaults (exchange type, durability, auto-delete)
- Recovery and SSL options

**Performance presets:**
- `config.AsHighPerformance()` - Non-persistent, Direct exchange
- `config.AsLegacy()` - Compatible with 1.x routing

## Documentation

- `/docs` - User documentation (operations, enrichers, features)
- `/docs/agents` - Agent definitions for AI-assisted development
- `README.md` - Quick start guide with code examples

## Important Notes

- **Multi-targeting**: Currently targets both `netstandard1.5` and `net451` (requires conditional compilation)
- **Security**: Run `~/.dotnet/dotnet list package --vulnerable` to check for CVEs
- **Integration tests**: Require RabbitMQ running on localhost:5672
- **Pipe pattern**: All operations are built as middleware pipelines - understand this pattern before modifying core

## Migration Process Policies

For comprehensive migration guidance, see these protocol documents in `docs/agents/`:

### Core Requirements (MANDATORY)

1. **Parallel Execution** - Spawn agents in parallel for 50-67% time reduction
   - See: `PARALLEL-MIGRATION-PROTOCOL.md`
   - Tool: `scripts/analyze-dependencies.sh`

2. **Continuous Testing** - Test after EVERY stage, fix-before-proceed rule
   - See: `CONTINUOUS-TESTING-PROTOCOL.md`
   - Tool: `scripts/run-stage-tests.sh`

3. **ADR Lifecycle** - Create ADRs before decisions, follow naming: `ADR #### Title With Spaces.md`
   - See: `GENERIC-ADR-LIFECYCLE-PROTOCOL.md`

4. **Incremental Documentation** - Update docs during migration, not at end
   - See: `INCREMENTAL-DOCUMENTATION-PROTOCOL.md`

5. **Stage Validation** - Automated quality gate checks
   - See: `STAGE-VALIDATION-PROTOCOL.md`
   - Tool: `scripts/validate-migration-stage.sh`

### Additional Protocols

- `GENERIC-DOCUMENTATION-PROTOCOL.md` - Documentation standards
- `GENERIC-TESTING-PROTOCOL.md` - Testing requirements
- `GENERIC-AGENT-LOGGING-PROTOCOL.md` - Logging via append-to-history.sh
- `GENERIC-MIGRATION-PLANNING-GUIDE.md` - Migration planning framework

### Automation Scripts

All scripts located in `scripts/`:
- `analyze-dependencies.sh` - Identify parallelizable work
- `run-stage-tests.sh` - Stage-specific testing
- `capture-test-baseline.sh` - Pre-migration baseline
- `validate-migration-stage.sh` - Protocol adherence checks
- `append-to-history.sh` - Log to HISTORY.md

**All agents working on RawRabbit MUST follow these protocols.**
