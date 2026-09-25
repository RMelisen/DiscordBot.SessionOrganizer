# Plynlings: relationships — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Relationships between Plynlings, grown by visits.

Spec: `docs/superpowers/specs/2026-09-26-plynling-relationships-design.md`.

### Task 1: Rules and storage
- [ ] Pure `Helpers/PlynlingBonds` (compatibility, scenes, bond thresholds, confession, visit happiness, wording) + `PlynlingRelation` model + migration `AddPlynlingRelations`; checks first; commit.

### Task 2: Visits make relationships
- [ ] `PlynlingService.VisitAsync` plays the scene, moves affinity and bond, confession/heartbreak/break-up, happiness by bond, badges and moments; grief on death and abandonment; the visit card shows it all. Checks against an in-memory database; commit.

### Task 3: Showing them
- [ ] `/plynling relations [user]`; the journal's « Relations » line. Checks; commit.

### Task 4: Docs and version
