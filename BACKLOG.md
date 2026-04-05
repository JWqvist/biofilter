# BioFilter — Product Backlog

## Epics & Sprint Plan

---

### 🏗️ Sprint 0 — Project Foundation ✅
**Goal:** Godot project running, dev branch, basic scene structure, nothing interactive yet.

Tasks:
- [x] Initialize Godot 4 C# project (project.godot, folder structure)
- [x] Create `dev` branch and branch protection on `main`
- [x] Main scene with empty grid (30x20 visual tiles, 16x16px)
- [x] Basic color palette constants file

**Acceptance criteria:** Game opens, shows empty dark grid, no errors.

**Completed:** 2026-04-04 — PR: Sprint 0: Project Foundation

---

### 🗺️ Sprint 1 — Grid & Building ✅
**Goal:** Player can place and remove wall blocks on the grid.

Tasks:
- [x] GridManager: tile data model (empty, wall, tower)
- [x] Click to place/remove wall tiles
- [x] Visual feedback (hover highlight, placed block color)
- [x] Grid boundaries enforced (can't build on spawn/exit tiles)

**Acceptance criteria:** Player can place/remove walls with mouse. Blocks are visible. Spawn/exit protected.

**Completed:** 2026-04-04 — PR: Sprint 1: Grid & Building

---

### 💨 Sprint 2 — Airflow System ✅
**Goal:** Airflow meter calculated and displayed. Building blocked when airflow too low.

Tasks:
- [x] AirflowCalculator: flood-fill from spawn to exit
- [x] Choke point detection (corridor width measurement)
- [x] Restriction score calculation
- [x] Airflow % UI meter (green → yellow → red)
- [x] Block placement rejected when airflow < 20%

**Acceptance criteria:** Meter updates on every placement. Narrowing paths reduces %. Building blocked at threshold.

**Completed:** 2026-04-05 — PR: Sprint 2: Airflow System

---

### 🦠 Sprint 3 — Particles ✅
**Goal:** Bio Particles spawn, pathfind, and reach the exit.

Tasks:
- [x] ParticleEntity: movement, health, speed
- [x] A* pathfinding on grid (recalculates when grid changes)
- [x] Smooth movement with momentum (no sharp corners)
- [x] Spawn from single left-edge point
- [x] Exit detection (right edge = life lost)
- [x] Lives counter in HUD

**Acceptance criteria:** Particles spawn, flow around walls, reach exit, lives decrease.

**Completed:** 2026-04-05 — PR: Sprint 3: Particles

---

### 🔫 Sprint 4 — Filter Modules (Towers) ✅
**Goal:** Three tower types can be placed and attack particles.

Tasks:
- [x] TowerBase: placement, range, targeting
- [x] Basic Filter: damage aura in radius
- [x] Electrostatic: slow aura in radius
- [x] UV Sterilizer: projectile targeting nearest particle
- [x] Tower UI in build panel (with costs)
- [x] Currency system (earn on kill, spend on placement)

**Acceptance criteria:** All 3 towers placed, damage particles, economy works.

**Completed:** 2026-04-05 — PR: Sprint 4: Filter Modules

---

### 🌊 Sprint 5 — Wave System & Game Loop ✅
**Goal:** Full playable game loop — waves, win, lose.

Tasks:
- [x] WaveManager: wave definitions, spawn intervals
- [x] "Start Wave" button, build phase between waves
- [x] Wave counter in HUD
- [x] Game Over screen (population = 0)
- [x] Win screen (survive 10 waves)
- [ ] Basic balance pass (deferred to polish sprint)

**Acceptance criteria:** Full game loop playable start to finish.

**Completed:** 2026-04-05 — PR: Sprint 5: Wave System & Game Loop

---

### 🎨 Sprint 6 — Polish & Balance ✅
**Goal:** Upgrade system, wave simulator for balance tuning, window scaling, GameConfig balance pass.

Tasks:
- [x] Upgrade system for towers (TowerUpgrade.cs)
- [x] Upgrade button in BuildPanel (click tower → show upgrade button)
- [x] WaveSimulator.cs — headless balance tool
- [x] Window size: 480x320 @ 2x scale (960x640 window)
- [x] GameConfig balance pass (see simulation notes)
- [ ] Particle visual variety (deferred)
- [ ] Sound effects (deferred)
- [ ] Map 2 (deferred)

**GameConfig changes (simulation-driven):**
- `ParticleBaseHealth`: 30 → 100 (3x — needed for meaningful health scaling)
- `WaveHealthMultiplierIncrease`: 0.3 → 0.45 (steeper ramp, W10 = 5.05x base health)
- `BasicFilterDamage`: 10 → 8 (lower to require more towers for clear)
- `UVSteriliserDamage`: 20 → 15 (balanced down)
- `UVSteriliserFireRate`: 1.0 → 1.2 (fires slightly faster)
- `CurrencyPerKill`: 10 → 12 (slightly better income to enable upgrades)

**Completed:** 2026-04-05 — PR: Sprint 6: Polish & Balance

---

### 🗺️ Sprint 7 — Content & Feel ← Current
**Goal:** More content, sound, visual polish.

Tasks:
- [ ] Sound effects (placement, kill, wave start/end)
- [ ] Particle visual variety (type 2: radiation particle)
- [ ] Map 2 (different grid layout)
- [ ] Selling/refunding towers
- [ ] Particle health bars

**Acceptance criteria:** Game has audio feedback. At least 2 particle types. Map selection screen.

---

## Backlog Items (unscheduled)
- Map editor
- Multiple particle types (radiation, chemical)
- Save/load
- Selling/refunding modules
- Particles with "prefer wider paths" behavior
- Split flow convergence logic

---

## Current Sprint: Sprint 7
Status: 🔴 Not started

## Sprint 6
Status: ✅ Done

## Sprint 5
Status: ✅ Done

## Sprint 4
Status: ✅ Done

## Sprint 3
Status: ✅ Done

## Sprint 2
Status: ✅ Done

## Sprint 1
Status: ✅ Done

## Sprint 0
Status: ✅ Done
