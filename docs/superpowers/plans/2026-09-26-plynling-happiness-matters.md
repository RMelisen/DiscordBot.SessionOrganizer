# Plynlings: happiness matters — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Meals worth ±15/25 % with mood, a sulk at 0 % happiness (starving overrides), and a daily happy gift.

**Architecture:** Pure rules in `PlynlingLife` (`MealFactor`, `IsSulking`, `GiftDraw`), applied in `PlynlingService.FeedAsync` and a new `TryGiftAsync`; wording in `PlynlingText`; lines added by `PlynlingCareService` and `/plynling view`.

Spec: `docs/superpowers/specs/2026-09-26-plynling-happiness-matters-design.md`.

## Global Constraints
- Happy > 80 % → hunger gain ×1.15; sad < 30 % → ×0.75; price and happiness gain unchanged.
- Sulk: happiness < 0.5 % and hunger ≥ 25 % → every meal refused, free.
- Gift: owner only, happy, once per Paris day (stored), 50 % × 5–15 cailloux; not dead, frozen or asleep; an unhappy look keeps the day's draw.
- Build `-warnaserror`; commit per task; never push; final "Updated version".

### Task 1: Meals and the sulk
- [ ] Checks: `MealFactor` at 0.81 / 0.80 / 0.30 / 0.29; `Feed` scales hunger only; `IsSulking` at 0 % with hunger 30 % / 20 % and at 1 %; `FeedAsync` refuses a sulk without charging (in-memory DB); card lines per gender.
- [ ] Implement; commit « Made a Plynling's mood change what its meals are worth ».

### Task 2: The gift
- [ ] Checks (in-memory DB): happy owner → one draw, day stored; same day → none; next day → again; not the owner / not happy → none and the day kept; asleep / frozen → none; amounts 5–15 and roughly half the draws win over seeds.
- [ ] `Plynling.LastGiftDay` + migration, `PlynlingLife.GiftDraw`, `PlynlingService.TryGiftAsync`, hooks in `/plynling view` and the care service. Commit.

### Task 3: Docs and version
