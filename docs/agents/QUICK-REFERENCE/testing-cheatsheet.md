# Continuous Testing Quick Reference

**Protocol**: `CORE-PROTOCOLS/CONTINUOUS-TESTING-PROTOCOL.md`
**Purpose**: Catch bugs early through systematic testing after every stage

---

## Core Principle: Fix-Before-Proceed

**RULE**: Never advance to the next stage with failing tests
**LIMIT**: Maximum 3 fix-and-retest iterations per stage
**GOAL**: 100% test pass rate before proceeding

---

## Quick Start (3 Commands)

### Step 1: Run Stage-Specific Tests
```bash
./scripts/run-stage-tests.sh [STAGE_NUM] "[STAGE_NAME]" strict
```

**Example:**
```bash
./scripts/run-stage-tests.sh 3 "Operations" strict
```

### Step 2: If Tests Fail - Analyze
```bash
./scripts/analyze-test-failure.sh [STAGE_NUM]
```

**Output provides:**
- Pattern-based root cause analysis
- Specific fix suggestions
- References to pattern library

### Step 3: Fix and Retest
```bash
# Fix the issues identified
# Then rerun:
./scripts/run-stage-tests.sh [STAGE_NUM] "[STAGE_NAME]" strict
```

**Track iterations**: Script tracks automatically (max 3)

---

## Stage-Specific Test Categories

| Stage | Test Filter | Pass Rate Required | Typical Issues |
|-------|-------------|-------------------|----------------|
| **2** (Core) | `Category=Unit&Component=Channel` | 100% | RabbitMQ.Client API changes |
| **3** (Operations) | `Category=Unit&Component=Operations` | 100% | Middleware pipeline ordering |
| **4** (Enrichers) | `Category=Unit&Component=Enrichers` | 100% | Serialization compatibility |
| **5** (DI) | `Category=Unit&Component=DI` | 100% | IoC container registration |
| **7** (Integration) | `Category=Integration` | 95% | RabbitMQ connection issues |

---

## Common Test Patterns

### Pattern 1: NullReferenceException on ConsumerTag
**Symptom**: `consumer.ConsumerTag` throws null reference
**Cause**: RabbitMQ.Client 6.x removed ConsumerTag property
**Fix**: Capture tag at `BasicConsume()` - see Pattern Library #1

### Pattern 2: BasicProperties Constructor Protected
**Symptom**: `new BasicProperties()` fails to compile
**Cause**: Constructor made protected in 6.x
**Fix**: Use `channel.CreateBasicProperties()` - see Pattern Library #2

### Pattern 3: Channel Disposal Exception
**Symptom**: `channel.Dispose()` throws NullReferenceException
**Cause**: AutorecoveringModel.Abort() bug in 6.x
**Fix**: Call `Close()` before `Dispose()` with try-catch - see Pattern Library #3

### Pattern 4: Middleware Pipeline Order
**Symptom**: InvalidOperationException in middleware
**Cause**: BasicProperties middleware before Channel middleware
**Fix**: Reorder - Channel MUST come before BasicProperties

---

## Test Execution Modes

### Strict Mode (Blocking)
```bash
./scripts/run-stage-tests.sh 3 "Operations" strict
```
- **Blocks** progression on any failure
- **Exit code 1** if pass rate < required
- **Enforces** fix-before-proceed rule

### Permissive Mode (Warning Only)
```bash
./scripts/run-stage-tests.sh 3 "Operations" permissive
```
- **Warns** on failures but allows progression
- Use only for exploratory testing
- **Not recommended** for migration stages

---

## Integration with Master Script

Using `scripts/migrate-stage.sh`:
```bash
./scripts/migrate-stage.sh 3 "Operations"

# Automatically runs:
# 1. Stage migration work
# 2. ./scripts/run-stage-tests.sh 3 "Operations" strict  # BLOCKING
# 3. Validates quality gates
# 4. Captures baseline
```

**Migration BLOCKED if tests fail** - cannot proceed until fixed.

---

## Iteration Tracking

Script automatically tracks fix iterations:

**Iteration 1:**
```
❌ GATE FAILED: 2 of 32 tests failed
   Fix issues and rerun
   Iteration 1 of 3 maximum
```

**Iteration 2:**
```
❌ GATE FAILED: 1 of 32 tests failed
   Progress made (2→1 failures)
   Iteration 2 of 3 maximum
```

**Iteration 3:**
```
✅ GATE PASSED: All 32 tests passed
```

**If exceeds 3:**
```
⚠️  WARNING: Exceeded 3 fix iterations
   Consider escalating to senior engineer
```

---

## Baseline Comparison

**Capture baseline** after successful stage:
```bash
./scripts/capture-test-baseline.sh
```

**Compare with baseline** before next stage:
```bash
./scripts/compare-with-baseline.sh
```

**Regression detected:**
- Pass rate decreased > 5%
- Fewer tests passing than baseline
- New failures introduced

**Action**: Fix regressions before proceeding

---

## Test Requirements by Stage

### Early Stages (2-4): Unit Tests Only
- Fast execution (< 30 seconds)
- No external dependencies
- 100% pass rate required
- Focus on API compatibility

### Middle Stages (5-6): Component Tests
- Medium speed (< 2 minutes)
- Minimal external dependencies
- 100% pass rate required
- Focus on integration points

### Final Stage (7): Full Integration
- Slower execution (< 10 minutes)
- Requires RabbitMQ running
- 95% pass rate acceptable (flaky tests possible)
- Focus on end-to-end scenarios

---

## Checklist

Before running tests:
- [ ] Stage migration work complete
- [ ] Build successful (0 errors, 0 warnings)
- [ ] RabbitMQ running (if integration tests)
- [ ] Test environment clean (no leftover state)

During test failures:
- [ ] Run `analyze-test-failure.sh` for root cause
- [ ] Check Pattern Library for known issues
- [ ] Apply fixes from pattern documentation
- [ ] Track iteration count (max 3)

After tests pass:
- [ ] Capture baseline: `capture-test-baseline.sh`
- [ ] Log to HISTORY.md: `append-to-history.sh`
- [ ] Proceed to next stage

---

## Time Investment vs. Savings

**Time to run tests**: 2-5 minutes per stage
**Time saved from early detection**: 30-60 minutes per bug

**Real data from RawRabbit:**
- No testing Stage 2→7: 5 bugs discovered in Stage 7
- Debugging time: 55 minutes
- If caught early: ~15 minutes (73% time savings)

---

## Need More Detail?

**Full Protocol**: `docs/agents/CORE-PROTOCOLS/CONTINUOUS-TESTING-PROTOCOL.md`
**Test Scripts**: `scripts/run-stage-tests.sh`, `scripts/analyze-test-failure.sh`
**Pattern Library**: `docs/migrations/rabbitmq-client-6x-patterns.md` (if it exists)

---

**Last Updated**: 2025-10-17
**Version**: 1.0
