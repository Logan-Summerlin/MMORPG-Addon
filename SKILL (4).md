---
name: "Status Reporting"
description: "Standard format for progress updates"
---

## Format

STATUS 
- Time: [HH:MM]
- Active: [current tasks]
- Completed: [tasks] since last update
- Blockers: [none OR description + proposed solution]
- Next: [upcoming tasks]

## Rules

1. Maximum 200 lines
2. Don't commit to git unless requested
3. Always include solution with any failure
4. Don't repeat info from previous reports

## Failure Reporting

Every failure mention must include:
- What failed
- Why it failed (if known)
- Proposed solution
