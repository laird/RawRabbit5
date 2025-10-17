# Parallel Execution Quick Reference

**Protocol**: `CORE-PROTOCOLS/PARALLEL-MIGRATION-PROTOCOL.md`
**Purpose**: Maximize migration efficiency through concurrent agent execution

---

## When to Use Parallel Execution

✅ **USE when:**
- Multiple projects at same dependency level (Level 0)
- No cross-dependencies between projects
- Independent build/test requirements
- 3+ projects in scope

❌ **DON'T USE when:**
- Projects depend on each other
- Shared state between projects
- Sequential requirements (e.g., database schema changes)

---

## Quick Start (3 Steps)

### Step 1: Analyze Dependencies
```bash
./scripts/analyze-dependencies.sh "src/RawRabbit.Operations.*"
```

**Output shows:**
- Level 0 projects: Can run in parallel
- Level 1+ projects: Have dependencies, run after Level 0

### Step 2: Spawn ALL Agents in SINGLE Message

**✅ CORRECT:**
```
[Single Message]:
  Task("Migrate Operations.Publish", "[full instructions]", "coder")
  Task("Migrate Operations.Subscribe", "[full instructions]", "coder")
  Task("Migrate Operations.Get", "[full instructions]", "coder")
  Task("Migrate Operations.Request", "[full instructions]", "coder")
  Task("Migrate Operations.Respond", "[full instructions]", "coder")
  Task("Migrate Operations.Tools", "[full instructions]", "coder")
  Task("Migrate Operations.StateMachine", "[full instructions]", "coder")
  Task("Migrate Operations.MessageSequence", "[full instructions]", "coder")

  TodoWrite { todos: [
    {content: "Migrate Operations.Publish", status: "in_progress", ...},
    {content: "Migrate Operations.Subscribe", status: "in_progress", ...},
    ...all 8 tasks...
  ]}
```

**❌ WRONG:**
```
Message 1: Task("Migrate Operations.Publish", ...)
Message 2: Task("Migrate Operations.Subscribe", ...)
Message 3: Task("Migrate Operations.Get", ...)
... (sequential = slow!)
```

### Step 3: Wait for Parallel Completion
- All agents execute simultaneously
- Completion times cluster within 60-90 seconds
- Review results, proceed to testing

---

## Time Savings Examples

| Projects | Sequential Time | Parallel Time | Savings |
|----------|----------------|---------------|---------|
| 3 projects | 5 min | 90 sec | **70%** |
| 8 projects | 12 min | 2 min | **83%** |
| 11 projects | 16 min | 2-3 min | **81%** |

**Real Data from RawRabbit Migration:**
- Stage 3 (Operations): Could have been 90 sec, took 6 min = **4.5 min wasted**
- Stage 4 (Enrichers): Could have been 2 min, took 11 min = **9 min wasted**

---

## Common Mistakes

### ❌ Mistake 1: Sequential Spawning
**Problem**: Spawning agents one-by-one in separate messages
**Fix**: Batch ALL Level 0 agents in SINGLE message

### ❌ Mistake 2: Not Running analyze-dependencies.sh
**Problem**: Missing parallel opportunities
**Fix**: Run script BEFORE every major stage

### ❌ Mistake 3: Parallel When Dependencies Exist
**Problem**: Race conditions, build failures
**Fix**: Only parallelize Level 0 projects

---

## Integration with Master Script

Using `scripts/migrate-stage.sh`:
```bash
# Automatically enforces parallel execution
./scripts/migrate-stage.sh 3 "Operations" "src/RawRabbit.Operations.*"

# Script will:
# 1. Run analyze-dependencies.sh
# 2. Show parallel opportunities
# 3. Confirm you're using single-message spawn
# 4. Proceed with stage work
```

---

## Checklist

Before spawning agents:
- [ ] Run `analyze-dependencies.sh` to identify Level 0 projects
- [ ] Confirm 3+ Level 0 projects (worth parallelizing)
- [ ] Prepare single message with ALL Task() calls
- [ ] Include ALL todos in single TodoWrite call
- [ ] Verify agents have complete, self-contained instructions

After agents complete:
- [ ] Verify all completed within expected time window
- [ ] Run stage tests: `./scripts/run-stage-tests.sh`
- [ ] Log completion: `./scripts/append-to-history.sh`

---

## Quick Formulas

**Time Estimation:**
- Sequential: `number_of_projects × 15 minutes`
- Parallel: `max(project_complexity) + 2 minutes overhead`
- Savings: `sequential_time - parallel_time`

**Efficiency Score:**
```
efficiency = parallel_time / sequential_time × 100%

Goal: 15-25% (75-85% time savings)
```

---

## Need More Detail?

**Full Protocol**: `docs/agents/CORE-PROTOCOLS/PARALLEL-MIGRATION-PROTOCOL.md`
**Automation Script**: `scripts/analyze-dependencies.sh`
**Example Usage**: See `docs/HISTORY.md` Stage 3 (Operations migration)

---

**Last Updated**: 2025-10-17
**Version**: 1.0
