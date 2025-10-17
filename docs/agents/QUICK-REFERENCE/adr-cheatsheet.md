# ADR Lifecycle Quick Reference

**Protocol**: `DOCUMENTATION-PROTOCOLS/ADR-LIFECYCLE-DETAILED.md`
**Purpose**: Document architectural decisions systematically with full lifecycle tracking

---

## Core Principle: Document Before Deciding

**RULE**: Create ADR BEFORE making significant architectural decisions
**FORMAT**: `ADR #### Title With Spaces.md` (note: spaces, not dashes)
**LOCATION**: `docs/adr/`

---

## Quick Start (4 Steps)

### Step 1: Create ADR from Template
```bash
# Next ADR number
NEXT_NUM=$(ls -1 docs/adr/ | grep "^ADR " | wc -l | awk '{print $1+1}')
NEXT_NUM_PADDED=$(printf "%04d" $NEXT_NUM)

# Create file
cat > "docs/adr/ADR ${NEXT_NUM_PADDED} Your Decision Title.md" << 'EOF'
# ADR #### Your Decision Title

**Date**: YYYY-MM-DD
**Status**: Proposed
**Deciders**: [Names/Roles]
**Technical Story**: [Issue/Task Reference]

## Context and Problem Statement

[Describe the context and problem requiring a decision]

## Decision Drivers

- [Driver 1]
- [Driver 2]
- [Driver 3]

## Considered Options

1. **Option 1**: [Brief description]
2. **Option 2**: [Brief description]
3. **Option 3**: [Brief description]

## Decision Outcome

**Chosen option**: "[Option X]"

**Rationale**: [Why this option was selected]

**Positive Consequences**:
- [Benefit 1]
- [Benefit 2]

**Negative Consequences**:
- [Tradeoff 1]
- [Tradeoff 2]

## Validation

**Success Criteria**:
- [ ] [Criterion 1]
- [ ] [Criterion 2]

**Risks**:
- [Risk 1 and mitigation]
- [Risk 2 and mitigation]

## Links

- [Related ADRs]
- [Implementation Tasks]
- [Discussion Threads]
EOF
```

### Step 2: Document the Decision
- Fill in context and problem statement
- List all options considered (minimum 2)
- Document decision drivers (technical, business, constraints)
- Record chosen option with rationale

### Step 3: Update Status Through Lifecycle
- **Proposed** → Under review, not yet approved
- **Accepted** → Decision approved and active
- **Implemented** → Decision executed in code
- **Validated** → Success criteria met
- **Superseded** → Replaced by newer ADR (link to successor)
- **Deprecated** → No longer recommended but not replaced

### Step 4: Log to HISTORY.md
```bash
./scripts/append-to-history.sh \
  "ADR ${NEXT_NUM_PADDED}: [Title]" \
  "Created ADR documenting decision on [topic]. Status: Proposed. Decision drivers: [key factors]." \
  "Document architectural decision before implementation" \
  "ADR created for team review and approval. Proceed with implementation after acceptance."
```

---

## The 7 Lifecycle Stages

### Stage 1: Creation (Status: Proposed)
- **When**: Before making decision
- **Action**: Document problem, options, drivers
- **Output**: ADR file in `docs/adr/`

### Stage 2: Review (Status: Proposed)
- **When**: Team review period
- **Action**: Gather feedback, refine options
- **Output**: Comments, alternative suggestions

### Stage 3: Acceptance (Status: Accepted)
- **When**: Decision finalized
- **Action**: Update status to Accepted, record date
- **Output**: Approved ADR ready for implementation

### Stage 4: Implementation (Status: Implemented)
- **When**: Decision executed in code
- **Action**: Update status, link to PRs/commits
- **Output**: Working implementation

### Stage 5: Validation (Status: Validated)
- **When**: Success criteria met
- **Action**: Verify outcomes match expectations
- **Output**: Validation report in ADR

### Stage 6: Post-Implementation Review
- **When**: 1-3 months after implementation
- **Action**: Assess actual vs. expected outcomes
- **Output**: Lessons learned, adjustments

### Stage 7: Deprecation/Superseding
- **When**: Decision no longer valid
- **Action**: Mark as Superseded/Deprecated, link to replacement
- **Output**: Updated ADR with deprecation notice

---

## Naming Convention (CRITICAL)

### ✅ CORRECT Format
```
ADR 0001 Target Framework Selection.md
ADR 0002 RabbitMQ Client Version Strategy.md
ADR 0003 Middleware Pipeline Architecture.md
```

**Pattern**: `ADR #### Title With Spaces.md`
- `ADR` (uppercase)
- Space (not dash)
- `####` (4-digit number, zero-padded)
- Space
- Title (title case, spaces between words)
- `.md` extension

### ❌ WRONG Formats
```
ADR-0001-target-framework.md        # Dashes instead of spaces
ADR_0001_Target_Framework.md        # Underscores
adr-0001-decision.md                # Lowercase ADR
ADR 1 Decision.md                   # Not zero-padded
```

---

## When to Create an ADR

✅ **CREATE ADR for:**
- Framework/library version selections
- Architecture pattern choices
- API design decisions
- Database schema changes
- Security approach selections
- Performance optimization strategies
- Technology stack changes

❌ **DON'T CREATE ADR for:**
- Routine bug fixes
- Minor refactoring
- Variable naming
- Code formatting preferences
- Temporary workarounds

**Threshold**: If the decision:
- Affects multiple components
- Is hard to reverse
- Has long-term implications
- Requires team consensus
→ **CREATE ADR**

---

## ADR Examples from RawRabbit

### ADR 0001: Target Framework Selection
**Context**: Multi-targeting vs. single framework
**Decision**: Migrate to .NET 9.0 (single target)
**Rationale**: Simplified maintenance, modern APIs
**Status**: Accepted → Implemented → Validated

### ADR 0002: RabbitMQ.Client Version Strategy
**Context**: RabbitMQ.Client 5.0.1 with known issues
**Decision**: Upgrade to 6.8.1 (breaking changes)
**Rationale**: Security fixes, performance, active support
**Status**: Accepted → Implemented → Validated

### ADR 0003: Testing Strategy
**Context**: Unit vs. integration test balance
**Decision**: 80% integration, 20% unit (real RabbitMQ)
**Rationale**: High confidence in real-world behavior
**Status**: Accepted → Implemented

---

## Integration with Protocols

### With CONTINUOUS-TESTING-PROTOCOL
- ADR for test strategy
- ADR for test infrastructure choices
- Reference ADRs in test failure analysis

### With PARALLEL-MIGRATION-PROTOCOL
- ADR for parallel execution approach
- ADR for dependency management strategy
- Validate ADRs with actual migration data

### With STAGE-VALIDATION-PROTOCOL
- Validate ADR success criteria at stage gates
- Update ADR status based on validation results
- Review ADRs during post-implementation

---

## ADR Metrics

**Track in ADRs:**
- Decision date
- Implementation date
- Validation date
- Time to implement
- Success criteria pass rate
- Post-implementation satisfaction

**Example**:
```markdown
## Metrics

- **Decision Date**: 2025-10-13
- **Implementation Date**: 2025-10-14
- **Time to Implement**: 6 hours
- **Success Criteria**: 4/4 met (100%)
- **Team Satisfaction**: 9/10
```

---

## Checklist

Before creating ADR:
- [ ] Decision has long-term impact
- [ ] Multiple options exist
- [ ] Team input needed
- [ ] Next ADR number identified

During ADR creation:
- [ ] Context clearly explained
- [ ] All options documented (minimum 2)
- [ ] Decision drivers listed
- [ ] Tradeoffs acknowledged
- [ ] Success criteria defined

After ADR acceptance:
- [ ] Status updated to Accepted
- [ ] Implementation tasks created
- [ ] HISTORY.md logged
- [ ] Team notified

Post-implementation:
- [ ] Status updated to Implemented
- [ ] Success criteria evaluated
- [ ] Post-review scheduled (1-3 months)

---

## Common Mistakes

### ❌ Mistake 1: Deciding Then Documenting
**Wrong**: Implement change → Write ADR afterwards
**Right**: Write ADR → Review → Decide → Implement

### ❌ Mistake 2: Single Option ADR
**Wrong**: ADR documents only chosen option
**Right**: ADR compares 2+ options, shows rationale

### ❌ Mistake 3: Forgetting Status Updates
**Wrong**: ADR stays "Proposed" forever
**Right**: Update status through all lifecycle stages

### ❌ Mistake 4: Using Dashes in Filename
**Wrong**: `ADR-0001-decision.md`
**Right**: `ADR 0001 Decision.md` (spaces)

---

## Need More Detail?

**Full Protocol**: `docs/agents/DOCUMENTATION-PROTOCOLS/ADR-LIFECYCLE-DETAILED.md`
**Examples**: See `docs/adr/` directory (6 real ADRs)
**MADR Format**: ADRs follow Modified Alexandrian Decision Record (MADR 3.0.0)

---

**Last Updated**: 2025-10-17
**Version**: 1.0
