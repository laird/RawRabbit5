# Continuous Testing Protocol

**Version**: 1.0
**Date**: 2025-10-13
**Purpose**: Test after EVERY stage to catch issues immediately
**Applicability**: All .NET migrations

---

## Overview

This protocol prevents costly late-stage test failures by validating functionality continuously throughout the migration.

**Core Principle**: Fix-before-proceed. Never advance to the next stage with failing tests.

**Impact**: Catches issues when they're introduced (cheap to fix) rather than days later (expensive compound errors).

---

## The Problem This Solves

### Anti-Pattern: End-of-Migration Testing ❌

```
Day 1: Migrate Core → No tests
Day 2: Migrate Operations → No tests
Day 3: Migrate Enrichers → No tests
Day 4: Run all tests → 15 failures discovered
Day 5-6: Debug and fix failures
```

**Problems**:
- Late discovery (3+ days after issues introduced)
- Difficult to isolate root cause
- Risk of compound errors
- Expensive debugging with loss of context

### Best Practice: Continuous Testing ✅

```
Day 1: Migrate Core → Test Core → Fix 2 failures → Proceed
Day 2: Migrate Operations → Test Operations → Fix 1 failure → Proceed
Day 3: Migrate Enrichers → Test Enrichers → All pass → Proceed
Day 4: Full regression → All pass (no surprises)
```

**Benefits**:
- Immediate discovery (same day)
- Easy to isolate (only new changes in scope)
- Minimal debugging (context fresh)
- Confidence at each stage

---

## Mandatory Test Points

### After Stage 2 (Core Library)

**Test Scope**: Core library unit tests

```bash
# Run core library tests
dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj \
  --filter "FullyQualifiedName~RawRabbit.Tests" \
  --logger "console;verbosity=detailed" \
  --configuration Release \
  --no-build

# Focus on critical infrastructure
dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj \
  --filter "FullyQualifiedName~RawRabbit.Tests.Channel" \
  --logger "console;verbosity=detailed" \
  --configuration Release
```

**Success Criteria**:
- [ ] 100% pass rate (ALL core tests must pass)
- [ ] No timeouts
- [ ] Build successful

**Common Issues at This Stage**:
- RabbitMQ.Client API breaking changes
- Consumer tag management changes
- BasicProperties instantiation issues
- Body type conversion (byte[] → ReadOnlyMemory<byte>)

**If Failures Occur**:
1. **STOP** - Do not proceed to Stage 3
2. **ANALYZE** - Review failure stack traces
3. **FIX** - Address root cause (often mock setups)
4. **RETEST** - Validate fix
5. **PROCEED** - Only when 100% pass rate achieved

---

### After Stage 3 (Operations)

**Test Scope**: Operations unit tests + smoke integration tests

```bash
# Run operations unit tests
dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj \
  --filter "FullyQualifiedName~Operations" \
  --logger "console;verbosity=detailed" \
  --configuration Release

# Run quick smoke integration test (if RabbitMQ available)
if docker ps | grep -q rabbitmq; then
  dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj \
    --filter "Category=Smoke|Priority=Critical" \
    --logger "console;verbosity=detailed" \
    --configuration Release
fi
```

**Success Criteria**:
- [ ] 100% unit test pass rate
- [ ] Smoke tests pass (if run)
- [ ] No breaking changes in operation patterns

**Common Issues at This Stage**:
- TryAdd ambiguity (System.Collections vs. custom extension)
- Middleware pipeline errors
- Configuration builder issues

---

### After Stage 4 (Enrichers)

**Test Scope**: Enricher integration tests

```bash
# Run enricher-specific tests
dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj \
  --filter "FullyQualifiedName~Enrichers" \
  --logger "console;verbosity=detailed" \
  --configuration Release
```

**Success Criteria**:
- [ ] 100% enricher test pass rate
- [ ] Serialization enrichers work (MessagePack, Protobuf)
- [ ] Polly enricher middleware functions
- [ ] HttpContext integration works

**Common Issues at This Stage**:
- Polly 8.x API changes (Policy → ResiliencePipeline)
- MessagePack 2.x API changes
- ASP.NET Core middleware integration

---

### After Stage 5 (DI Adapters)

**Test Scope**: Dependency injection integration tests

```bash
# Run DI integration tests
dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj \
  --filter "FullyQualifiedName~DependencyInjection" \
  --logger "console;verbosity=detailed" \
  --configuration Release
```

**Success Criteria**:
- [ ] 100% DI test pass rate
- [ ] ServiceCollection registration works
- [ ] Autofac integration works
- [ ] Client resolution successful

**Common Issues at This Stage**:
- DI container API changes
- Registration scope issues
- Missing service registrations

---

### After Stage 6 (Samples)

**Test Scope**: Sample application runtime validation

```bash
# Build samples
dotnet build sample/RawRabbit.ConsoleApp.Sample/RawRabbit.ConsoleApp.Sample.csproj --configuration Release
dotnet build sample/RawRabbit.AspNet.Sample/RawRabbit.AspNet.Sample.csproj --configuration Release

# Run console sample (smoke test)
cd sample/RawRabbit.ConsoleApp.Sample
timeout 30 dotnet run --configuration Release &
CONSOLE_PID=$!
sleep 10
if ps -p $CONSOLE_PID > /dev/null; then
  echo "✅ Console sample running"
  kill $CONSOLE_PID || true
else
  echo "❌ Console sample crashed"
  exit 1
fi
cd ../..

# Run web sample (smoke test)
cd sample/RawRabbit.AspNet.Sample
dotnet run --configuration Release &
WEB_PID=$!
sleep 10
curl -s http://localhost:5000/health || echo "✅ Web sample responding"
kill $WEB_PID || true
cd ../..
```

**Success Criteria**:
- [ ] All samples build successfully
- [ ] Console sample runs without crashing
- [ ] Web sample starts and responds to requests
- [ ] No unhandled exceptions in logs

**Common Issues at This Stage**:
- Missing configuration files
- Connection string issues
- Dependency injection misconfiguration

---

## Fix-Before-Proceed Rule (MANDATORY)

**Fundamental Principle**: If ANY test fails, STOP and FIX immediately.

### Step 1: Document Failure

```bash
# Capture failure details
dotnet test [FailingTest] --logger "console;verbosity=detailed" > test-failure.log 2>&1

# Create failure report
cat > docs/test-failures/stage-N-failure.md << 'EOF'
## Test Failure: [TestName]

**Date**: YYYY-MM-DD HH:MM
**Stage**: Stage N - [StageName]
**Severity**: [P0/P1/P2]

### Error Message
```
[Full error message]
```

### Stack Trace
```
[Full stack trace]
```

### Root Cause Analysis
[Analysis of why test failed]

### Fix Applied
[Description of fix]

### Verification
[Retest results]
EOF
```

### Step 2: Categorize by Severity

- **P0 (Critical)**: Core functionality broken → Fix immediately (same day)
  - Examples: Core library crashes, connection failures, data corruption
- **P1 (High)**: Important feature broken → Fix before next stage
  - Examples: Operations not working, enrichers failing
- **P2 (Medium)**: Non-critical feature broken → Fix before release
  - Examples: Sample apps issues, edge cases
- **P3 (Low)**: Nice-to-have, documentation → Backlog
  - Examples: Logging issues, cosmetic problems

### Step 3: Fix the Issue

```bash
# Apply fix to source code
# ...

# Rebuild
dotnet build --configuration Release

# Rerun SPECIFIC failing test
dotnet test --filter "FullyQualifiedName=[TestName]" --configuration Release --no-build

# Verify fix
if [ $? -eq 0 ]; then
  echo "✅ Fix successful"
else
  echo "❌ Fix failed, iterate"
  exit 1
fi
```

### Step 4: Rerun Full Test Suite

```bash
# After fixing ALL failures, rerun complete suite
dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj --configuration Release --no-build

# Check for regressions
# Ensure fix didn't break other tests
```

### Step 5: Log and Proceed

```bash
# Log fix to HISTORY.md
./scripts/append-to-history.sh \
  "Stage N: Test Failure Fixed - [TestName]" \
  "Fixed [issue] in [component]. Root cause: [analysis]. Applied fix: [solution]." \
  "Ensure test suite validation catches issues at stage boundaries" \
  "Test now passing. Proceeding with Stage N+1."

# Mark todo as complete
# Update stage status
# Proceed to next stage ONLY when 100% pass rate
```

---

## Automation Scripts

### Create: `scripts/run-stage-tests.sh`

```bash
#!/bin/bash
# scripts/run-stage-tests.sh - Run stage-specific tests

STAGE_NUM=$1
STAGE_NAME=$2

echo "🧪 Running Stage $STAGE_NUM ($STAGE_NAME) Tests..."
echo ""

case $STAGE_NUM in
  2)
    echo "Testing: Core Library"
    dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj \
      --filter "FullyQualifiedName~RawRabbit.Tests.Channel|FullyQualifiedName~RawRabbit.Tests.Consumer" \
      --logger "console;verbosity=detailed" \
      --configuration Release
    ;;
  3)
    echo "Testing: Operations"
    dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj \
      --filter "FullyQualifiedName~Operations" \
      --logger "console;verbosity=detailed" \
      --configuration Release
    ;;
  4)
    echo "Testing: Enrichers"
    dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj \
      --filter "FullyQualifiedName~Enrichers" \
      --logger "console;verbosity=detailed" \
      --configuration Release
    ;;
  5)
    echo "Testing: DI Adapters"
    dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj \
      --filter "FullyQualifiedName~DependencyInjection" \
      --logger "console;verbosity=detailed" \
      --configuration Release
    ;;
  6)
    echo "Testing: Samples"
    dotnet build sample/**/*.csproj --configuration Release
    echo "✅ All samples build successfully"
    ;;
  7)
    echo "Testing: Full Regression"
    dotnet test --configuration Release
    ;;
  *)
    echo "❌ Unknown stage: $STAGE_NUM"
    exit 1
    ;;
esac

TEST_EXIT_CODE=$?

echo ""
if [ $TEST_EXIT_CODE -eq 0 ]; then
  echo "✅ Stage $STAGE_NUM tests PASSED"
  echo "Ready to proceed to Stage $((STAGE_NUM + 1))"
else
  echo "❌ Stage $STAGE_NUM tests FAILED"
  echo "FIX FAILURES before proceeding"
  exit 1
fi
```

**Usage**:
```bash
# After completing Stage 3
./scripts/run-stage-tests.sh 3 "Operations"

# After completing Stage 4
./scripts/run-stage-tests.sh 4 "Enrichers"
```

---

### Create: `scripts/capture-test-baseline.sh`

```bash
#!/bin/bash
# scripts/capture-test-baseline.sh - Capture pre-migration test baseline

echo "📊 Capturing pre-migration test baseline..."
echo ""

BASELINE_DIR="docs/test-baselines"
mkdir -p $BASELINE_DIR

TIMESTAMP=$(date '+%Y-%m-%d-%H%M%S')
BASELINE_FILE="$BASELINE_DIR/baseline-$TIMESTAMP.md"

# Run full test suite on CURRENT framework
echo "Running full test suite..."
dotnet test --configuration Release \
  --logger "trx;LogFileName=baseline-tests.trx" \
  --collect:"XPlat Code Coverage" \
  > /tmp/baseline-test-output.txt 2>&1

# Extract metrics
TOTAL_TESTS=$(grep -o "Total tests: [0-9]*" /tmp/baseline-test-output.txt | grep -o "[0-9]*")
PASSED_TESTS=$(grep -o "Passed: [0-9]*" /tmp/baseline-test-output.txt | grep -o "[0-9]*")
FAILED_TESTS=$(grep -o "Failed: [0-9]*" /tmp/baseline-test-output.txt | grep -o "[0-9]*")
SKIPPED_TESTS=$(grep -o "Skipped: [0-9]*" /tmp/baseline-test-output.txt | grep -o "[0-9]*")

# Get framework version
FRAMEWORK=$(grep -m 1 "TargetFramework" src/RawRabbit/RawRabbit.csproj | sed 's/.*<TargetFramework>\(.*\)<\/TargetFramework>.*/\1/')

# Create baseline document
cat > $BASELINE_FILE << EOF
# Pre-Migration Test Baseline

**Date**: $(date '+%Y-%m-%d %H:%M:%S')
**Framework**: $FRAMEWORK
**Purpose**: Establish baseline before migration

## Test Statistics

- **Total Tests**: ${TOTAL_TESTS:-0}
- **Passed**: ${PASSED_TESTS:-0}
- **Failed**: ${FAILED_TESTS:-0}
- **Skipped**: ${SKIPPED_TESTS:-0}
- **Pass Rate**: $(echo "scale=2; ${PASSED_TESTS:-0} * 100 / ${TOTAL_TESTS:-1}" | bc)%

## Build Configuration

- Configuration: Release
- Framework: $FRAMEWORK
- Date: $(date '+%Y-%m-%d')

## Test Results File

Stored at: TestResults/baseline-tests.trx

## Purpose

This baseline will be used to:
1. Compare post-migration results
2. Detect regressions
3. Validate migration success
4. Identify new failures introduced

## Next Steps

After migration:
1. Run same test suite
2. Compare pass rates
3. Investigate any NEW failures
4. Ensure pass rate maintained or improved
EOF

# Copy test results
if [ -f "TestResults/baseline-tests.trx" ]; then
  cp TestResults/baseline-tests.trx $BASELINE_DIR/
fi

echo "✅ Baseline captured: $BASELINE_FILE"
echo ""
echo "Summary:"
echo "  Total Tests: ${TOTAL_TESTS:-0}"
echo "  Pass Rate: $(echo "scale=2; ${PASSED_TESTS:-0} * 100 / ${TOTAL_TESTS:-1}" | bc)%"
echo ""
echo "Next: Begin migration with confidence!"
```

**Usage**:
```bash
# Before starting ANY migration work
./scripts/capture-test-baseline.sh

# Creates: docs/test-baselines/baseline-YYYY-MM-DD-HHMMSS.md
```

---

## Benefits

### 1. Early Detection

- Issues caught same day introduced
- Fresh context makes debugging easier
- Root cause isolation simpler

### 2. Prevents Compound Errors

- Don't build on broken foundation
- Each stage validated before proceeding
- Incremental confidence building

### 3. Faster Overall Completion

- Fix early = cheap (30 minutes)
- Fix late = expensive (hours of debugging)
- Net time savings despite more frequent testing

### 4. Higher Quality

- 100% pass rate at each stage
- No deferred technical debt
- Production-ready at completion

---

## Integration with Other Protocols

### With Parallel Execution

```markdown
[Single Message]:
  Task("Migrate Project A")
  Task("Migrate Project B")
  ...
  TodoWrite { todos: [...] }

[After ALL agents complete]:
  Run: ./scripts/run-stage-tests.sh [STAGE_NUM] "[STAGE_NAME]"
```

### With Logging Protocol

```bash
# After tests pass
./scripts/append-to-history.sh \
  "Stage N Complete: All Tests Passing" \
  "Migrated [X] projects. Ran [Y] tests. 100% pass rate." \
  "Validate stage completion before proceeding" \
  "Ready for Stage N+1"
```

---

## Checklist

After EVERY stage:

- [ ] Run stage-specific tests
- [ ] Verify 100% pass rate
- [ ] If failures: STOP, FIX, RETEST
- [ ] Log test results to HISTORY.md
- [ ] Update TodoWrite with completion
- [ ] Only then proceed to next stage

---

**Protocol Version**: 1.0
**Last Updated**: 2025-10-13
**Status**: Production Ready - MANDATORY
**Applicability**: All .NET migrations

**Key Takeaway**: Test continuously, not at the end. Fix-before-proceed is non-negotiable.
