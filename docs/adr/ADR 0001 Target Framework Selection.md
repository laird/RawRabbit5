# ADR 0001 Target Framework Selection

**Status**: Proposed

**Date**: 2025-10-13

**Decision Makers**: Architecture Team, RawRabbit Maintainers

**Technical Story**: RawRabbit currently multi-targets `netstandard1.5` and `net451` (legacy frameworks from 2015-2016 era). We need to modernize to current .NET frameworks to leverage modern language features, improved performance, and continued support.

## Context and Problem Statement

Which .NET target framework(s) should RawRabbit 3.0 target? The current multi-targeting of `netstandard1.5` and `net451` is obsolete and prevents using modern .NET features. We need to select a target that balances stability, support lifecycle, and feature availability.

## Decision Drivers

- **Support Lifecycle**: How long will the framework be supported by Microsoft?
- **Feature Availability**: Access to modern C# language features and runtime improvements
- **Ecosystem Compatibility**: RabbitMQ.Client 7.x requires minimum .NET Standard 2.0
- **Performance**: Runtime optimizations and JIT improvements
- **Adoption Risk**: Stability vs. cutting-edge features
- **Migration Complexity**: Breaking changes from current targets
- **End-of-Life Dates**: Avoiding frameworks near EOL
- **LTS vs STS**: Long-Term Support vs Standard-Term Support trade-offs

## Considered Options

### Option 1: .NET 8 (LTS)

**Framework Details**:
- Release Date: November 2023
- End of Support: November 10, 2026
- Support Type: Long-Term Support (LTS) - 3 years
- Latest Stable Version: 8.0.x

**Pros**:
- LTS designation provides 3-year support window
- Production-proven and widely adopted in enterprise
- Stable API surface with comprehensive documentation
- Full C# 12 language support
- Excellent performance improvements over earlier versions
- Strong ecosystem maturity and library compatibility
- Extended support until late 2026 provides migration runway

**Cons**:
- Support ends November 2026 (16 months from now)
- Will require migration to .NET 10 by 2026
- Lacks some newest features available in .NET 9
- Users will need to upgrade relatively soon after migration

**Trade-offs**:
- Stability and proven production readiness vs. shorter remaining support window
- Wide adoption vs. approaching end-of-support
- Conservative choice that requires another migration in ~16 months

### Option 2: .NET 9 (STS)

**Framework Details**:
- Release Date: November 2024
- End of Support: November 10, 2026 (extended from 18 to 24 months)
- Support Type: Standard-Term Support (STS) - 2 years
- Latest Stable Version: 9.0.x

**Pros**:
- Same support end date as .NET 8 (November 2026) due to extended STS policy
- Latest features and performance improvements
- C# 13 language features
- Most recent runtime optimizations
- Better async/await performance
- Improved JSON serialization performance
- Modern development experience
- No additional migration burden vs. .NET 8 (same EOL)

**Cons**:
- Less production battle-tested than .NET 8
- STS designation may concern conservative enterprises
- Shorter track record in production environments
- Potential for more edge-case bugs vs. mature LTS

**Trade-offs**:
- Latest features and performance vs. less production validation
- Modern capabilities vs. perceived stability concerns
- Actually has same support window as .NET 8 (ending Nov 2026)

### Option 3: Multi-target .NET 8 + .NET 9

**Pros**:
- Consumers can choose their preferred framework
- Maximum compatibility across .NET ecosystem
- Allows testing against both frameworks
- Future-proofs library for .NET 9 adopters

**Cons**:
- Significantly increases build and test complexity
- Conditional compilation complexity for framework-specific code
- Doubles CI/CD pipeline time and costs
- More complex package deployment
- Difficult to manage framework-specific bugs
- Maintenance burden for small team
- Both frameworks EOL on same date anyway (Nov 2026)

**Trade-offs**:
- Flexibility vs. complexity
- Broader compatibility vs. maintenance overhead
- Not justified when both targets have identical EOL dates

### Option 4: Target .NET Standard 2.0/2.1

**Pros**:
- Broad compatibility with .NET Framework, .NET Core, Mono, Xamarin
- Stable API surface
- Safe choice for library authors

**Cons**:
- .NET Standard is no longer receiving new versions
- Locks us out of modern language features (C# 12, 13, pattern matching improvements)
- Cannot use async/await improvements from .NET 6+
- RabbitMQ.Client 7.x requires minimum .NET Standard 2.0 anyway
- Misses performance improvements in .NET runtime
- Prevents using modern nullable reference types fully
- Does not align with Microsoft's "future of .NET" direction
- Still requires .NET 6+ for modern hosting scenarios

**Trade-offs**:
- Maximum compatibility vs. technical debt and limited features
- This keeps us in "legacy mode" rather than modernizing

## Decision Outcome

**Chosen Option**: ".NET 9 (STS)"

**Rationale**:

1. **Identical Support Window**: Due to Microsoft's 2025 policy change extending STS releases from 18 to 24 months, .NET 9 has the **exact same end-of-support date as .NET 8** (November 10, 2026). This eliminates the traditional LTS advantage.

2. **Better Performance**: .NET 9 includes the latest runtime optimizations, improved async/await performance, and better JSON serialization - all directly beneficial for a message bus library.

3. **Modern Features**: C# 13 features and latest language capabilities provide better development experience and code quality.

4. **Migration Timing**: Since both .NET 8 and .NET 9 reach EOL in November 2026, we'll need to migrate to .NET 10 (LTS, releasing Nov 2025, supported until Nov 2028) at the same time regardless of choice.

5. **RabbitMQ.Client Compatibility**: RabbitMQ.Client 7.x supports .NET 6+ without issues, making .NET 9 fully compatible.

6. **Breaking Change Window**: RawRabbit 3.0 is already a major version bump, making this the ideal time for framework modernization.

7. **Development Experience**: Latest SDK and tooling improvements benefit both maintainers and contributors.

**Key Insight**: The traditional "choose LTS for stability" argument is **nullified** by the fact that .NET 8 and .NET 9 have the same support end date. Therefore, choosing the more modern framework provides benefits with no additional migration burden.

**Consequences**:

**Positive**:
- Access to latest .NET runtime performance improvements
- C# 13 language features available
- Modern async/await optimizations
- Better development tooling and experience
- Same support window as .NET 8 (no disadvantage)
- Positions library as modern and up-to-date

**Negative**:
- Requires .NET 9 SDK for consumers (not a .NET 8 project)
- May concern extremely conservative enterprises (minimal risk)
- Slightly less production validation than .NET 8 (acceptable for 3.0 major version)
- Users on .NET Framework must remain on RawRabbit 2.x (acceptable breaking change)

**Risks**:
- **Low**: Potential edge-case bugs in .NET 9 runtime (mitigated by thorough testing)
- **Low**: Perception issues with STS vs LTS (mitigated by communication about identical EOL)
- **Medium**: Must plan migration to .NET 10 by late 2026 (affects all .NET 8/9 projects)

**Migration to .NET 10**:
By November 2026, both .NET 8 and .NET 9 reach end-of-support. The next migration should target **.NET 10 (LTS)**, which releases November 2025 and is supported until November 2028, providing a proper 3-year support window.

## Implementation Notes

1. **Project Files**: Update all `.csproj` files to target `net9.0` instead of multi-targeting
2. **Language Version**: Enable `<LangVersion>13.0</LangVersion>` or `latest` for C# 13 features
3. **SDK Requirement**: Update documentation to specify .NET 9 SDK requirement
4. **CI/CD**: Update build pipelines to use .NET 9 SDK
5. **Documentation**: Clearly communicate framework requirements and migration path
6. **Version Bump**: This is part of RawRabbit 3.0 breaking changes

## Links

- Related: [ADR 0002 RabbitMQ Client Version Strategy](./ADR%200002%20RabbitMQ%20Client%20Version%20Strategy.md)
- Related: [ADR 0004 Breaking Changes Strategy](./ADR%200004%20Breaking%20Changes%20Strategy.md)
- [.NET Support Policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
- [.NET 9 Release Notes](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9)
- [Extended STS Support Announcement](https://devblogs.microsoft.com/dotnet/dotnet-sts-releases-supported-for-24-months/)
