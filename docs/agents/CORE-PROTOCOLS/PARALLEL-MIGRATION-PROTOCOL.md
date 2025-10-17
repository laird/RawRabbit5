# Parallel Migration Execution Protocol

**Version**: 1.0
**Date**: 2025-10-13
**Purpose**: Maximize parallelism within dependency-respecting stage boundaries
**Applicability**: All .NET migrations with multiple independent projects

---

## Overview

This protocol enables **30-50% time reduction** on migration stages by executing independent work concurrently through parallel agent spawning.

**Core Principle**: Within each dependency level, spawn ALL agents in a SINGLE message to maximize parallelism.

---

## Identification of Parallelizable Work

### Stage Analysis Checklist

Before starting each stage, perform dependency analysis:

#### Step 1: List All Projects in Stage Scope

```bash
# List projects for stage
ls -1 src/[StageProjects]/*.csproj
```

#### Step 2: Analyze Dependencies

```bash
# For each project, check ProjectReference dependencies
grep -r "<ProjectReference" src/*.csproj

# Example output:
# src/Operations.Publish/Operations.Publish.csproj:    <ProjectReference Include="..\RawRabbit\RawRabbit.csproj" />
# src/Operations.Subscribe/Operations.Subscribe.csproj:    <ProjectReference Include="..\RawRabbit\RawRabbit.csproj" />
```

#### Step 3: Group into Dependency Levels

**Dependency Level** = Maximum distance from leaf nodes

- **Level 0**: No dependencies on projects in stage (fully parallel)
- **Level 1**: Depends only on Level 0 projects (parallel within level)
- **Level 2**: Depends on Level 1 projects (parallel within level)
- **Level N**: Depends on Level N-1 projects

**Example: Stage 3 (Operations Projects)**

```
Dependency Analysis:
- Operations.Publish → depends on Core (outside stage) → Level 0
- Operations.Subscribe → depends on Core (outside stage) → Level 0
- Operations.Get → depends on Core (outside stage) → Level 0
- Operations.Request → depends on Core (outside stage) → Level 0
- Operations.Respond → depends on Core (outside stage) → Level 0
- Operations.Tools → depends on Core (outside stage) → Level 0
- Operations.StateMachine → depends on Core (outside stage) → Level 0
- Operations.MessageSequence → depends on Core (outside stage) → Level 0

Result: All 8 projects at Level 0 → Fully parallelizable
```

#### Step 4: Create Execution Plan

```markdown
## Stage N Execution Plan

### Level 0 (Fully Parallel)
- Project A
- Project B
- Project C
- Project D

### Level 1 (Parallel within level, after Level 0)
- Project E (depends on A)
- Project F (depends on B)

### Level 2 (Parallel within level, after Level 1)
- Project G (depends on E, F)
```

---

## Parallel Execution Pattern

### Current Sequential Approach (SLOW) ❌

```markdown
Message 1: Migrate Project A
Message 2: Migrate Project B
Message 3: Migrate Project C
Message 4: Migrate Project D
...

Duration: N × (time per project)
```

**Example**: 8 projects × 15 minutes = 120 minutes

### Parallel Approach (FAST) ✅

```markdown
Single Message:
  Task("Migrate Project A", "Full migration instructions...", "coder")
  Task("Migrate Project B", "Full migration instructions...", "coder")
  Task("Migrate Project C", "Full migration instructions...", "coder")
  Task("Migrate Project D", "Full migration instructions...", "coder")
  ...

  TodoWrite { todos: [
    {content: "Migrate Project A", status: "in_progress", activeForm: "Migrating Project A"},
    {content: "Migrate Project B", status: "in_progress", activeForm: "Migrating Project B"},
    {content: "Migrate Project C", status: "in_progress", activeForm: "Migrating Project C"},
    {content: "Migrate Project D", status: "in_progress", activeForm: "Migrating Project D"},
    ...
  ]}

All agents complete concurrently
Duration: max(time per project) ≈ 15-20 minutes (67% faster)
```

---

## Agent Task Template

Each parallel agent should receive complete, self-contained instructions:

```markdown
Task("Migrate [ProjectName]", """
## Objective
Migrate [ProjectName] from [OldFramework] to [NewFramework]

## Project Path
src/[ProjectName]/[ProjectName].csproj

## Dependencies
- Depends on: [List external dependencies]
- No dependencies on: [Other projects in this stage]

## Tasks
1. Update .csproj file:
   - Change <TargetFramework>[OldFramework]</TargetFramework> to <TargetFramework>[NewFramework]</TargetFramework>
   - Update <VersionPrefix>2.0.0</VersionPrefix> to <VersionPrefix>3.0.0</VersionPrefix>
   - Add <LangVersion>latest</LangVersion>
   - Add <Nullable>enable</Nullable>

2. Update package references:
   - [Package]: [OldVersion] → [NewVersion]

3. Remove conditional compilation:
   - Remove #if NET451 blocks
   - Remove #if NETSTANDARD1_5 blocks

4. Build and fix errors:
   - Run: dotnet build src/[ProjectName]/[ProjectName].csproj --configuration Release
   - Fix any compilation errors
   - Fix any nullable warnings (as needed)

5. Validate:
   - Build succeeds
   - 0 errors
   - Log to HISTORY.md using ./scripts/append-to-history.sh

## Success Criteria
- [ ] .csproj updated to [NewFramework]
- [ ] All package references updated
- [ ] Conditional compilation removed
- [ ] Build successful (0 errors)
- [ ] HISTORY.md updated

## Coordination
- Log independently to HISTORY.md
- No shared state with other agents
- Complete work autonomously
""", "coder")
```

---

## Coordination Requirements

### 1. Shared Todo List

All agents update the same TodoWrite in the initial message:

```javascript
TodoWrite { todos: [
  // All parallel tasks batched together
  {content: "Migrate Project A", status: "in_progress", ...},
  {content: "Migrate Project B", status: "in_progress", ...},
  {content: "Migrate Project C", status: "in_progress", ...},
  ...
]}
```

### 2. Independent Logging

Each agent logs to HISTORY.md independently:

```bash
./scripts/append-to-history.sh \
  "Stage N: [ProjectName] Migration Complete" \
  "[Details of what changed]" \
  "[Why this project was migrated]" \
  "[Impact and status]"
```

**No conflicts**: append-to-history.sh uses append operations (no overwrites)

### 3. Build Validation

Each agent validates their project builds independently:

```bash
dotnet build src/[ProjectName]/[ProjectName].csproj --configuration Release
```

**No shared artifacts**: Each project builds into its own bin/ directory

### 4. No Shared State

Agents work on completely independent projects:
- ✅ Different source files
- ✅ Different .csproj files
- ✅ Different build outputs
- ✅ Different HISTORY.md entries (timestamps differentiate)

---

## When to Use Parallel Execution

### ✅ Use Parallel When:

- [ ] Projects have NO dependencies on each other
- [ ] Projects at same dependency level
- [ ] All prerequisites completed (e.g., core libraries migrated)
- [ ] External dependencies resolved (e.g., NuGet packages available)
- [ ] Clear, independent success criteria per project

### ❌ Do NOT Parallelize When:

- [ ] Projects have circular dependencies
- [ ] Debugging sequential issues
- [ ] First-time agent spawning (learn serially first)
- [ ] Shared resource conflicts (rare in .NET projects)
- [ ] Cross-project refactoring required

---

## Real-World Example: Stage 3 (Operations)

### Analysis

```bash
# Check dependencies
grep -r "<ProjectReference" src/RawRabbit.Operations.*/*.csproj

# All 8 Operations projects only depend on Core (already migrated)
# No cross-dependencies between Operations projects
# Result: Fully parallelizable
```

### Sequential Approach (Actual RawRabbit Migration)

```
13:32 - Start Stage 3
14:32 - Migrate Publish, Subscribe, Get, Request, Respond, Tools, StateMachine, MessageSequence
15:21 - Complete Stage 3

Duration: 109 minutes (1h 49m)
```

### Parallel Approach (Recommended)

```markdown
[Single Message at 13:32]:

Task("Migrate Operations.Publish", "[full instructions]", "coder")
Task("Migrate Operations.Subscribe", "[full instructions]", "coder")
Task("Migrate Operations.Get", "[full instructions]", "coder")
Task("Migrate Operations.Request", "[full instructions]", "coder")
Task("Migrate Operations.Respond", "[full instructions]", "coder")
Task("Migrate Operations.Tools", "[full instructions]", "coder")
Task("Migrate Operations.StateMachine", "[full instructions]", "coder")
Task("Migrate Operations.MessageSequence", "[full instructions]", "coder")

TodoWrite { todos: [...all 8 tasks...] }

Expected completion: 14:10 (40 minutes)
Time saved: 69 minutes (63% reduction)
```

---

## Benefits

### 1. **Dramatic Time Savings**

- Sequential: 8 projects × 15 min = 120 min
- Parallel: max(15 min) = 15-20 min (includes spawn overhead)
- **Savings: 100 minutes (83% faster)**

### 2. **No Additional Risk**

- Agents work on independent projects
- No merge conflicts (different files)
- No shared state issues
- Failures isolated to specific projects

### 3. **Better Resource Utilization**

- Leverages concurrent agent execution
- Maximizes throughput
- Same quality, less time

### 4. **Scalability**

- Works for 2 projects or 20 projects
- Linear time with parallelism (not exponential)
- Consistent performance

---

## Common Pitfalls

### ❌ Pitfall 1: Spawning Agents Sequentially

```markdown
Message 1: Task("Agent 1")
Message 2: Task("Agent 2")
Message 3: Task("Agent 3")
```

**Problem**: Sequential spawning, no parallelism
**Solution**: Spawn ALL agents in SINGLE message

---

### ❌ Pitfall 2: Not Analyzing Dependencies

```markdown
Task("Project A")  // Actually depends on Project B
Task("Project B")
```

**Problem**: Project A might fail due to dependency
**Solution**: Run dependency analysis first, group by level

---

### ❌ Pitfall 3: Incomplete Agent Instructions

```markdown
Task("Migrate Project A", "Migrate the project", "coder")
```

**Problem**: Agent lacks context, makes assumptions
**Solution**: Provide complete, self-contained instructions

---

### ❌ Pitfall 4: Over-Parallelization

```markdown
Task("Project A", "Also refactor Project B while you're at it...")
```

**Problem**: Agents interfere with each other's work
**Solution**: One agent = one project = clear boundaries

---

## Integration with Testing Protocol

After parallel execution completes:

```bash
# Run stage-specific tests for all migrated projects
dotnet test test/[TestProject].csproj \
  --filter "FullyQualifiedName~[StagePattern]" \
  --configuration Release

# MANDATORY: 100% pass rate before proceeding
```

See `CONTINUOUS-TESTING-PROTOCOL.md` for details.

---

## Automation Script

### Create: `scripts/analyze-dependencies.sh`

```bash
#!/bin/bash
# scripts/analyze-dependencies.sh - Analyze project dependencies for parallelization

STAGE_PROJECTS=$1

echo "🔍 Analyzing dependencies for parallel execution..."
echo ""

# Get all project files
PROJECTS=$(find $STAGE_PROJECTS -name "*.csproj")

echo "📊 Projects in scope:"
for proj in $PROJECTS; do
    echo "  - $(basename $proj .csproj)"
done
echo ""

echo "🔗 Dependency Analysis:"
for proj in $PROJECTS; do
    PROJECT_NAME=$(basename $proj .csproj)
    DEPS=$(grep -h "<ProjectReference" $proj | sed 's/.*Include="\.\.\///;s/\/.*//')

    if [ -z "$DEPS" ]; then
        echo "  ✅ $PROJECT_NAME: No in-stage dependencies (Level 0 - Fully parallel)"
    else
        echo "  ⚠️  $PROJECT_NAME: Depends on $DEPS (Analyze level)"
    fi
done
echo ""

echo "💡 Recommendation:"
echo "  - Count projects with no in-stage dependencies"
echo "  - Spawn parallel agents for all Level 0 projects in SINGLE message"
echo "  - Execute Level 1 projects after Level 0 completes"
```

**Usage**:
```bash
./scripts/analyze-dependencies.sh "src/RawRabbit.Operations.*"
```

---

## Checklist

Before using parallel execution:

- [ ] Run dependency analysis: `./scripts/analyze-dependencies.sh`
- [ ] Verify no circular dependencies
- [ ] Group projects by dependency level
- [ ] Prepare complete agent instructions for each project
- [ ] Spawn ALL Level N agents in SINGLE message
- [ ] Include TodoWrite with all tasks batched
- [ ] Wait for level completion before next level
- [ ] Run stage tests after all projects complete

---

## Success Metrics

Track these on your migration:

- [ ] **Time savings**: >50% reduction on parallelizable stages
- [ ] **No failures**: Parallel execution doesn't introduce errors
- [ ] **Clean logs**: All agents log independently without conflicts
- [ ] **Quality maintained**: Same build success rate as sequential

---

**Protocol Version**: 1.0
**Last Updated**: 2025-10-13
**Status**: Production Ready
**Applicability**: All .NET migrations with independent projects

**Key Takeaway**: Parallel execution is the HIGHEST IMPACT efficiency improvement. Use it whenever projects are independent.
