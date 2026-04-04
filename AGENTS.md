# BioFilter — Project Agents Guide

## Roles

### PO (Product Owner) — Tue + Jeppe
- Owns the GDD and prioritizes the backlog
- Defines acceptance criteria for each sprint
- Reviews completed sprints and gives go/no-go for merge to main

### Scrum Master — SM Agent
- Breaks epics into tasks
- Assigns tasks to developer agents (SEQUENTIALLY — never parallel on same module)
- Tracks progress, unblocks issues
- Reports sprint status to PO

### Developer Agents
- Work on ONE module at a time
- Own specific files during their task (no overlap)
- Commit to `dev` branch, PR to `main` when sprint done

## File Ownership Rules (anti-conflict)
Each task specifies which files the dev agent may touch.
No two agents work on overlapping files simultaneously.

## Sprint Cadence
- Sprint = one set of related features, delivered as a PR
- SM coordinates handoffs between developer agents
- PO reviews each sprint PR before merge

## Current Sprint
See BACKLOG.md
