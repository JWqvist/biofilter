# BioFilter — Product Backlog

## Epics & Sprint Plan

---

### 🏗️ Sprint 0 — Project Foundation
**Goal:** Godot project running, dev branch, basic scene structure, nothing interactive yet.

Tasks:
- [ ] Initialize Godot 4 C# project (project.godot, folder structure)
- [ ] Create `dev` branch and branch protection on `main`
- [ ] Main scene with empty grid (30x20 visual tiles, 16x16px)
- [ ] Basic color palette constants file

**Acceptance criteria:** Game opens, shows empty dark grid, no errors.

---

### 🗺️ Sprint 1 — Grid & Building
**Goal:** Player can place and remove wall blocks on the grid.

Tasks:
- [ ] GridManager: tile data model (empty, wall, tower)
- [ ] Click to place/remove wall tiles
- [ ] Visual feedback (hover highlight, placed block color)
- [ ] Grid boundaries enforced (can't build on spawn/exit tiles)

**Acceptance criteria:** Player can place/remove walls with mouse. Blocks are visible. Spawn/exit protected.

---

### 💨 Sprint 2 — Airflow System
**Goal:** Airflow meter calculated and displayed. Building blocked when airflow too low.

Tasks:
- [ ] AirflowCalculator: flood-fill from spawn to exit
- [ ] Choke point detection (corridor width measurement)
- [ ] Restriction score calculation
- [ ] Airflow % UI meter (green → yellow → red)
- [ ] Block placement rejected when airflow < 20%

**Acceptance criteria:** Meter updates on every placement. Narrowing paths reduces %. Building blocked at threshold.

---

### 🦠 Sprint 3 — Particles
**Goal:** Bio Particles spawn, pathfind, and reach the exit.

Tasks:
- [ ] ParticleEntity: movement, health, speed
- [ ] A* pathfinding on grid (recalculates when grid changes)
- [ ] Smooth movement with momentum (no sharp corners)
- [ ] Spawn from single left-edge point
- [ ] Exit detection (right edge = life lost)
- [ ] Lives counter in HUD

**Acceptance criteria:** Particles spawn, flow around walls, reach exit, lives decrease.

---

### 🔫 Sprint 4 — Filter Modules (Towers)
**Goal:** Three tower types can be placed and attack particles.

Tasks:
- [ ] TowerBase: placement, range, targeting
- [ ] Basic Filter: damage aura in radius
- [ ] Electrostatic: slow aura in radius
- [ ] UV Sterilizer: projectile targeting nearest particle
- [ ] Tower UI in build panel (with costs)
- [ ] Currency system (earn on kill, spend on placement)

**Acceptance criteria:** All 3 towers placed, damage particles, economy works.

---

### 🌊 Sprint 5 — Wave System & Game Loop
**Goal:** Full playable game loop — waves, win, lose.

Tasks:
- [ ] WaveManager: wave definitions, spawn intervals
- [ ] "Start Wave" button, build phase between waves
- [ ] Wave counter in HUD
- [ ] Game Over screen (population = 0)
- [ ] Win screen (survive 10 waves)
- [ ] Basic balance pass

**Acceptance criteria:** Full game loop playable start to finish.

---

### 🎨 Sprint 6 — Polish (Post-MVP)
- [ ] Upgrade system for towers
- [ ] Particle visual variety (types 2+)
- [ ] Sound effects
- [ ] Screen scaling / fullscreen toggle
- [ ] Map 2

---

## Backlog Items (unscheduled)
- Map editor
- Multiple particle types (radiation, chemical)
- Save/load
- Selling/refunding modules
- Particles with "prefer wider paths" behavior
- Split flow convergence logic

---

## Current Sprint: Sprint 0
Status: 🔴 Not started
