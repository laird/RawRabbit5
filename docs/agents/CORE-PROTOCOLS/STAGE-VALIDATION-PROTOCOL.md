# Stage Validation Protocol

**Version**: 1.0
**Date**: 2025-10-13
**Purpose**: Enforce protocol adherence and quality gates after each migration stage
**Applicability**: All .NET migrations
**Impact**: Prevents protocol violations, ensures consistent quality

---

## Overview

This protocol provides automated validation of migration stage completion to ensure all quality gates are met before proceeding to the next stage.

**Core Principle**: Every stage must pass validation before the next stage begins.

---

## What Gets Validated

### 1. Build Success ✅

**Check**: All projects build successfully without errors

```bash
~/.dotnet/dotnet build --configuration Release --nologo --verbosity quiet
```

**Success Criteria**:
- Exit code = 0
- No "error" lines in output
- All projects compile

**Common Failures**:
- Compilation errors
- Missing dependencies
- API breaking changes not addressed

---

### 2. HISTORY.md Updated ✅

**Check**: HISTORY.md has entry for current stage

```bash
git diff HEAD~1 docs/HISTORY.md | grep -q "^+## .*Stage $STAGE_NUM"
```

**Success Criteria**:
- New HISTORY.md entry added
- Entry describes stage work
- Entry follows format (What/Why/Impact)

**Common Failures**:
- Forgot to log
- Incomplete logging
- Wrong format

---

### 3. Tests Executed ✅

**Check**: Recent test results exist (stages 2+)

```bash
# Check for test results in last 60 minutes
find TestResults -name "*.trx" -mmin -60
```

**Success Criteria**:
- Test results exist
- No failures in .trx file
- Pass rate = 100%

**Common Failures**:
- Tests not run
- Test failures not fixed
- Timeouts

---

### 4. No CVEs ✅

**Check**: No vulnerable packages (stages 2+)

```bash
~/.dotnet/dotnet list package --vulnerable --configuration Release
```

**Success Criteria**:
- No CRITICAL/HIGH vulnerabilities
- Output doesn't contain "has the following vulnerable packages"
- Clean security posture

**Common Failures**:
- Vulnerable dependencies not updated
- CVEs introduced by new packages
- Security regressions

---

### 5. CHANGELOG.md Updated ✅

**Check**: CHANGELOG.md has recent updates (stages 2+)

```bash
git diff HEAD~5..HEAD CHANGELOG.md | grep -q "^+"
```

**Success Criteria**:
- CHANGELOG has new content
- Breaking changes documented
- Version info present

**Common Failures**:
- Incremental documentation not followed
- CHANGELOG deferred to end
- Missing breaking change documentation

---

### 6. ADRs Created ✅

**Check**: ADRs exist for architectural decisions (stages 4+)

```bash
find docs/adr -name "ADR *.md" -mtime -1
```

**Success Criteria**:
- ADRs created for major decisions
- ADRs follow lifecycle protocol
- ADRs properly named

**Common Failures**:
- ADRs created after implementation
- Missing ADRs for key decisions
- Incorrect naming format

---

## Automation Script

### Create: `scripts/validate-migration-stage.sh`

```bash
#!/bin/bash
# scripts/validate-migration-stage.sh - Enforce protocol adherence

STAGE_NUM=$1
STAGE_NAME=$2

if [ -z "$STAGE_NUM" ] || [ -z "$STAGE_NAME" ]; then
    echo "Usage: $0 <stage-number> <stage-name>"
    echo "Example: $0 3 \"Operations\""
    exit 1
fi

echo "🔍 Validating Stage $STAGE_NUM: $STAGE_NAME"
echo "═══════════════════════════════════════════════════════════════"
echo ""

VALIDATION_FAILED=0

# 1. Verify all projects build
echo "✅ Checking builds..."
~/.dotnet/dotnet build --configuration Release --nologo --verbosity quiet > /tmp/build.log 2>&1
if [ $? -ne 0 ]; then
    echo "❌ FAIL: Build errors detected"
    echo ""
    cat /tmp/build.log | grep "error "
    VALIDATION_FAILED=1
else
    echo "   ✅ PASS: All projects build successfully"
fi
echo ""

# 2. Verify HISTORY.md updated
echo "✅ Checking HISTORY.md..."
if git diff HEAD~1 docs/HISTORY.md 2>/dev/null | grep -q "^+## .*Stage $STAGE_NUM" 2>/dev/null; then
    echo "   ✅ PASS: HISTORY.md updated for Stage $STAGE_NUM"
elif git diff HEAD~1 docs/HISTORY.md 2>/dev/null | grep -q "^+## " 2>/dev/null; then
    echo "   ✅ PASS: HISTORY.md updated"
else
    echo "   ⚠️  WARNING: HISTORY.md may not be updated for Stage $STAGE_NUM"
    echo "      Ensure you logged stage completion"
fi
echo ""

# 3. Verify tests run (if applicable, stages 2+)
if [ "$STAGE_NUM" -ge 2 ]; then
    echo "✅ Checking test execution..."

    # Check if test results exist recently
    if [ -d "TestResults" ] && [ "$(find TestResults -name "*.trx" -mmin -60 2>/dev/null | wc -l)" -gt 0 ]; then
        echo "   ✅ PASS: Recent test results found"

        # Try to extract pass rate
        LATEST_TRX=$(find TestResults -name "*.trx" -mmin -60 2>/dev/null | head -1)
        if [ -n "$LATEST_TRX" ]; then
            # Simple check: look for failures
            if grep -q 'outcome="Failed"' "$LATEST_TRX" 2>/dev/null; then
                echo "   ⚠️  WARNING: Test failures detected in recent run"
                VALIDATION_FAILED=1
            else
                echo "   ✅ PASS: No failures in recent test run"
            fi
        fi
    else
        echo "   ⚠️  WARNING: No recent test results found"
        echo "      Recommended: Run ./scripts/run-stage-tests.sh $STAGE_NUM \"$STAGE_NAME\""
    fi
fi
echo ""

# 4. Verify no CVEs (stages 2+)
if [ "$STAGE_NUM" -ge 2 ]; then
    echo "✅ Checking for CVEs..."
    ~/.dotnet/dotnet list package --vulnerable --configuration Release > /tmp/cve.log 2>&1
    if grep -q "has the following vulnerable packages" /tmp/cve.log; then
        echo "   ❌ FAIL: Vulnerable packages detected"
        echo ""
        cat /tmp/cve.log | grep -A 10 "vulnerable packages"
        VALIDATION_FAILED=1
    else
        echo "   ✅ PASS: No vulnerable packages"
    fi
fi
echo ""

# 5. Verify CHANGELOG updated (stages 2+)
if [ "$STAGE_NUM" -ge 2 ] && [ -f "CHANGELOG.md" ]; then
    echo "✅ Checking CHANGELOG.md..."
    if git diff HEAD~5..HEAD CHANGELOG.md 2>/dev/null | grep -q "^+.*Stage $STAGE_NUM" 2>/dev/null; then
        echo "   ✅ PASS: CHANGELOG.md updated for Stage $STAGE_NUM"
    elif git diff HEAD~5..HEAD CHANGELOG.md 2>/dev/null | grep -q "^+" 2>/dev/null; then
        echo "   ℹ️  INFO: CHANGELOG.md has updates (stage not explicitly mentioned)"
    else
        echo "   ⚠️  WARNING: CHANGELOG.md may not be updated"
        echo "      Recommended for incremental documentation"
    fi
fi
echo ""

# 6. Verify ADRs if architectural decisions made
if [ "$STAGE_NUM" -ge 4 ]; then
    echo "✅ Checking ADRs..."
    if [ -d "docs/adr" ] && [ "$(find docs/adr -name "ADR *.md" -mtime -1 2>/dev/null | wc -l)" -gt 0 ]; then
        echo "   ✅ PASS: Recent ADR activity detected"
    else
        echo "   ℹ️  INFO: No recent ADR updates (may not be needed)"
    fi
fi
echo ""

# Final summary
echo "═══════════════════════════════════════════════════════════════"
if [ $VALIDATION_FAILED -eq 0 ]; then
    echo "✅ Stage $STAGE_NUM validation PASSED"
    echo ""
    echo "   All checks successful. Ready to proceed to Stage $((STAGE_NUM + 1))"
    echo "═══════════════════════════════════════════════════════════════"
    exit 0
else
    echo "❌ Stage $STAGE_NUM validation FAILED"
    echo ""
    echo "   ⚠️  Fix issues above before proceeding to Stage $((STAGE_NUM + 1))"
    echo ""
    echo "   Common fixes:"
    echo "   - Build errors: Review compilation errors above"
    echo "   - CVEs: Update vulnerable packages"
    echo "   - Tests: Run ./scripts/run-stage-tests.sh $STAGE_NUM \"$STAGE_NAME\""
    echo "   - Documentation: Update HISTORY.md, CHANGELOG.md"
    echo "═══════════════════════════════════════════════════════════════"
    exit 1
fi
```

**Make Executable**:
```bash
chmod +x scripts/validate-migration-stage.sh
```

---

## Usage Examples

### After Stage 2 (Core Library)

```bash
# Validate core library migration
./scripts/validate-migration-stage.sh 2 "Core Library"

# Expected checks:
# ✅ Builds
# ✅ HISTORY.md updated
# ✅ Tests executed
# ✅ No CVEs
# ⚠️  CHANGELOG.md (warning acceptable at stage 2)
# ⚠️  ADRs (not required until stage 4)
```

---

### After Stage 3 (Operations)

```bash
# Validate operations migration
./scripts/validate-migration-stage.sh 3 "Operations"

# Expected checks:
# ✅ Builds (all 8 Operations projects)
# ✅ HISTORY.md updated (Stage 3 entry)
# ✅ Tests executed (Operations tests passing)
# ✅ No CVEs
# ✅ CHANGELOG.md updated incrementally
# ⚠️  ADRs (not required until stage 4)
```

---

### After Stage 4 (Enrichers)

```bash
# Validate enrichers migration
./scripts/validate-migration-stage.sh 4 "Enrichers"

# Expected checks:
# ✅ Builds (all Enricher projects)
# ✅ HISTORY.md updated (Stage 4 entry)
# ✅ Tests executed (Enricher tests passing)
# ✅ No CVEs (critical - Polly, serialization updated)
# ✅ CHANGELOG.md updated
# ✅ ADRs created (e.g., ZeroFormatter deprecation, Polly 8.x migration)
```

---

## Integration with Migration Workflow

### Workflow Step-by-Step

```bash
# Step 1: Complete migration stage work
[Perform migration tasks...]

# Step 2: Run stage-specific tests
./scripts/run-stage-tests.sh $STAGE_NUM "$STAGE_NAME"

# Step 3: Validate stage completion
./scripts/validate-migration-stage.sh $STAGE_NUM "$STAGE_NAME"

# Step 4: If validation passes, proceed to next stage
# If validation fails, fix issues and re-validate
```

---

### Automated in Migration Script

```bash
#!/bin/bash
# migrate-stage.sh - Execute and validate migration stage

STAGE_NUM=$1
STAGE_NAME=$2

echo "Starting Stage $STAGE_NUM: $STAGE_NAME"

# Execute migration work
# [Migration code here]

# Test
echo "Running tests..."
./scripts/run-stage-tests.sh $STAGE_NUM "$STAGE_NAME"
if [ $? -ne 0 ]; then
    echo "❌ Tests failed. Fix before proceeding."
    exit 1
fi

# Validate
echo "Validating stage completion..."
./scripts/validate-migration-stage.sh $STAGE_NUM "$STAGE_NAME"
if [ $? -ne 0 ]; then
    echo "❌ Validation failed. Address issues before proceeding."
    exit 1
fi

echo "✅ Stage $STAGE_NUM complete and validated. Ready for Stage $((STAGE_NUM + 1))"
```

---

## Benefits

### 1. Automated Quality Gates

- No human judgment needed
- Consistent enforcement
- Catches protocol violations immediately

### 2. Prevents Common Mistakes

- Forgetting to test
- Skipping documentation
- Introducing security issues
- Building on broken foundation

### 3. Clear Go/No-Go Decision

- Passes → Proceed confidently
- Fails → Fix specific issues
- No ambiguity

### 4. Audit Trail

- Validation results logged
- Issues documented
- Quality history preserved

---

## Common Validation Failures

### Failure: Build Errors

**Symptom**: Build check fails with compilation errors

**Common Causes**:
- API breaking changes not addressed
- Missing package references
- Nullable reference warnings as errors

**Fix**:
```bash
# Review errors
~/.dotnet/dotnet build --configuration Release

# Address compilation issues
# Rerun validation
```

---

### Failure: Tests Not Run

**Symptom**: No recent test results found

**Common Causes**:
- Forgot to run stage tests
- Tests timed out
- Test results directory not created

**Fix**:
```bash
# Run stage tests
./scripts/run-stage-tests.sh $STAGE_NUM "$STAGE_NAME"

# Rerun validation
./scripts/validate-migration-stage.sh $STAGE_NUM "$STAGE_NAME"
```

---

### Failure: CVEs Detected

**Symptom**: Vulnerable packages found

**Common Causes**:
- Didn't update all dependencies
- Introduced vulnerable package
- Transitive dependency issue

**Fix**:
```bash
# List vulnerable packages
~/.dotnet/dotnet list package --vulnerable

# Update packages
~/.dotnet/dotnet add package [PackageName] --version [SafeVersion]

# Verify fix
~/.dotnet/dotnet list package --vulnerable

# Rerun validation
```

---

### Failure: HISTORY.md Not Updated

**Symptom**: No HISTORY.md entry for stage

**Common Causes**:
- Forgot to log
- Used wrong logging format
- Typo in stage number

**Fix**:
```bash
# Log stage completion
./scripts/append-to-history.sh \
  "Stage $STAGE_NUM Complete: [Title]" \
  "[What changed]" \
  "[Why]" \
  "[Impact]"

# Commit
git add docs/HISTORY.md
git commit -m "docs: Log Stage $STAGE_NUM completion"

# Rerun validation
```

---

## Checklist

After EVERY stage, run validation:

- [ ] All projects build successfully (0 errors)
- [ ] HISTORY.md updated with stage entry
- [ ] Tests executed and passing (100% pass rate)
- [ ] No CRITICAL/HIGH CVEs
- [ ] CHANGELOG.md updated (stages 2+)
- [ ] ADRs created for decisions (stages 4+)
- [ ] Validation script passes
- [ ] Ready to proceed to next stage

---

## Integration with Other Protocols

### With Continuous Testing

```bash
# After testing passes
./scripts/run-stage-tests.sh $STAGE_NUM "$STAGE_NAME"

# Validate includes test check
./scripts/validate-migration-stage.sh $STAGE_NUM "$STAGE_NAME"
```

### With Parallel Execution

```bash
# After all parallel agents complete
[Wait for all agents to finish]

# Run stage tests
./scripts/run-stage-tests.sh $STAGE_NUM "$STAGE_NAME"

# Validate stage
./scripts/validate-migration-stage.sh $STAGE_NUM "$STAGE_NAME"
```

### With Incremental Documentation

```bash
# Validation checks for CHANGELOG.md updates
# Reminds to document incrementally
# Enforces protocol adherence
```

---

## Exit Codes

The validation script uses standard exit codes:

- **0**: All validation checks passed
- **1**: One or more validation checks failed

Use in scripts:
```bash
if ./scripts/validate-migration-stage.sh 3 "Operations"; then
    echo "Proceeding to Stage 4"
else
    echo "Fix Stage 3 issues first"
    exit 1
fi
```

---

## Customization

### Project-Specific Checks

Add custom checks to `validate-migration-stage.sh`:

```bash
# 7. Project-specific validation
if [ "$STAGE_NUM" -eq 4 ]; then
    echo "✅ Checking custom requirement..."
    # Your custom validation here
fi
```

### Severity Levels

Adjust severity:
- **FAIL** (❌): Blocks progression (exit 1)
- **WARNING** (⚠️): Allowed but noted
- **INFO** (ℹ️): Informational only

---

**Protocol Version**: 1.0
**Last Updated**: 2025-10-13
**Status**: Production Ready - RECOMMENDED
**Applicability**: All .NET migrations

**Key Takeaway**: Automate quality gates. Don't rely on human memory to check everything.
