# Plynlings: inventory and collections — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

Spec: `docs/superpowers/specs/2026-09-26-plynling-inventory-collections-design.md`.

### Task 1: Catalog and storage
- [ ] `Helpers/ItemCatalog` (foods, 32 collectibles, seasons, draws), `InventoryItem`, `CollectionCompletion`, `PebbleWallet.LastForageAt`, migration `AddInventory`; `InventoryService` (add/take in the caller's context). Checks first; commit.

### Task 2: Phase 1 — shop, pantry-first feeding, give, inventory
- [ ] Commands in `PlynlingModule.Inventory.cs` (partial), autocomplete handler, pantry in `FeedAsync`. Checks (in-memory DB); commit.

### Task 3: Phase 2 — forage, finds, the book, completion
- [ ] Forage, gift/game/visit finds, `/plynling collection`, set rewards. Checks; commit.

### Task 4: Phase 2 — trade and sell
- [ ] In-memory offers, atomic swap, sell. Checks; commit.

### Task 5: Docs and version
