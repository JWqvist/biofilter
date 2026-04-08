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
**Goal:** Pathogens spawn, pathfind, and reach the exit.

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

### 🗺️ Sprint 7 — QA & Final Tweaks ✅
**Goal:** Pre-player QA pass — fix all bugs, verify all systems, add balance report.

Tasks:
- [x] Full code review all source files
- [x] Fix Particle double-die bug (multiple towers could fire Died signal twice)
- [x] Fix reroute teleport bug (Initialize() was resetting health/position mid-wave)
- [x] Fix WaveManager _particlesAlive negative guard
- [x] Fix Main.cs game over/win (SetProcess → GetTree().Paused = true)
- [x] Fix GameOver/WinScreen restart (unpause tree before reload)
- [x] Fix AirflowCalculator airflow normalization (open grid now correctly shows 100%)
- [x] Fix WaveSimulator slow multiplier (was using ParticleSlowFactor, now matches actual game)
- [x] Add Particle.Reroute() method for mid-wave path updates
- [x] Add src/Tests/SimulationReport.cs — 4-scenario balance report

**Status:** ✅ Ready for player testing

---

## Known Issues

### Balance (Simulator)
- **Simulator is optimistic**: The headless simulator models DPS as continuous per-corridor-fraction.
  In real gameplay, particles move faster (steering, not straight corridors), towers have fixed tick
  rates, and projectile travel time matters. Real difficulty will be harder than simulator shows.
- **Wave scaling cliff**: With 3 Basic Filters only, W2-3 spike hard (0 kills) then become trivial
  after upgrades. The upgrade system causes binary difficulty swings. Deferred to post-player feedback.
- **W10 target label**: Simulator Summary shows W10 target as "DEATH SPIRAL" instead of "VERY HARD".
  This is intentional (W10 should be lethal), but misleads the ✓/✗ match. Minor cosmetic issue.
- **Economy**: Player starts with $200 but balanced builds cost $275+. Forces early waves with minimal
  towers; works as intended but may feel punishing. Monitor in playtesting.

### Gameplay
- **No path recovery after game over**: If player causes complete airflow block and escapes,
  particles on screen have no path. They stay still until the wave ends. Non-breaking.
- **Tower range visual**: No range circle shown when placing or hovering towers.
  Players may not know what they're buying. Sprint 8 candidate.
- **Slow stacking**: Multiple Electrostatic towers don't stack slow (only max applies).
  This is intentional but not communicated to the player.

---

### 🗺️ Sprint 8 — Content & Feel (Next)
**Goal:** More content, sound, visual polish.

Tasks:
- [ ] Sound effects (placement, kill, wave start/end)
- [ ] Particle visual variety (type 2: radiation particle)
- [ ] Map 2 (different grid layout)
- [ ] Selling/refunding towers
- [ ] Particle health bars
- [ ] Tower range circles on hover

**Acceptance criteria:** Game has audio feedback. At least 2 particle types. Map selection screen.

---

### 🎨 Sprint 9 — UI/UX Improvements ✅
**Goal:** Range preview, hotkeys, wave preview, wave bonuses, speed button, airflow warning.

Tasks:
- [x] Range preview circle: translucent circle shown when hovering tiles with tower selected
- [x] Hotkeys: 1/2/3 select towers, W = wall mode, R = deselect
- [x] Hotkey hints shown in BuildMenu items: "Basic Filter [1] $50"
- [x] Pre-wave intel screen: 3-second WavePreview overlay before each wave
- [x] Wave bonuses: +50 perfect wave, +25 efficiency; HUD notifications
- [x] Speed button (▶ 1x / ▶▶ 2x) in BottomBar
- [x] Airflow warning: flashing AirflowMeter below 30%; red vignette below 20%
- [x] AirflowCritical signal added to GridManager

**Completed:** 2026-04-05 — PR: Sprint 9: UI/UX Improvements

---

## Backlog Items (unscheduled)
- Map editor
- Multiple particle types (radiation, chemical)
- Save/load
- Selling/refunding modules
- Particles with "prefer wider paths" behavior
- Split flow convergence logic

---


## Current Sprint: Sprint 13C
Status: ✅ Done

### Sprint 13C — Pixel Art Menus

#### Implemented
- **BuildMenu** — dark bg panel with green pixel-art border (#2d5a3d, 2px). Title bar '▣ BUILD MENU' with animated blinking cursor (0.5s). Colored square icon per item. Row selection/hover highlighting. 'WAVE PHASE — BUILD LOCKED' red message during wave. Pixel ✕ close button.
- **PauseMenu** — semi-transparent overlay + terminal-style panel. '▣ SYSTEM PAUSE' blinking header. Military labels: RESUME MISSION / ABORT TO MAIN MENU / TERMINATE PROGRAM. Warning footer.
- **GameOver** — rebuilt entirely in code (removed scene node deps). '██████ CRITICAL FAILURE ██████' blinking red title. Red border (#cc0000). Live stats: waves survived, particles neutralised, bunker population. Buttons: RETRY MISSION / RETURN TO BASE.
- **WinScreen** — rebuilt entirely in code. '██████ MISSION SUCCESS ██████' blinking green title. Green border (#00c853). Stats: particles neutralised, final population, airflow %. Buttons: DEPLOY AGAIN / RETURN TO BASE.
- **MainMenu** — scanline effect sweeping down. Blinking '⚠ BIOHAZARD ALERT ⚠' in hazard yellow. '▶ ENTER BUNKER' button with pixel art border + green glow. Version text bottom-right.
- **GameState** — added `ParticlesKilled` (int), `WavesSurvived` (int), `CurrentAirflow` (float) properties. ParticleManager increments ParticlesKilled on kill. Main.cs increments WavesSurvived on WaveComplete.
- Build: 0 errors, 0 warnings.

---

## Sprint 12
Status: ✅ Done

### Sprint 12 — New Filter Modules

#### Implemented
- **VortexSeparator** — $125, cyan #00bcd4. Registers A* penalty (weight 6) for tiles within 3-tile radius via VortexPenaltyRegistry. Particles route around it taking longer paths. Visual: rotating cyan 4-arm spiral. Penalty removed on refund.
- **PowerCore** — $150, gold #ffd700. Connects to WaveManager.WaveStarted and grants +$5 per wave. Disconnects cleanly on removal. Visual: pulsing gold square with 4 slowly rotating rays.
- **BioNeutraliser** — $100, purple #9c27b0. On placement, sets DamageMultiplier=1.25 on all 8 adjacent towers. Resets to 1.0 on removal. TowerBase gained new DamageMultiplier property. BasicFilter and UVSteriliser apply it. Visual: purple square with connecting lines to boosted neighbors.
- **MagneticCage** — $175, brown #795548. Freezes particles within 2.5 tiles (Speed=0) for 2.0s then releases. Restores original speed on release or tower removal. Visual: brown square with 4 inward-pointing arrow triangles.
- **BuildMenu** updated to 8 items (wall + 7 towers). All 4 new towers shown with name, cost, description.
- **TowerType enum** extended: VortexSeparator=3, PowerCore=4, BioNeutraliser=5, MagneticCage=6.
- **Particle.Speed** changed to public setter to allow MagneticCage freezing.
- **VortexPenaltyRegistry** — new static class, thread-safe registry of penalised tiles for Pathfinder.
- **Pathfinder** updated to add VortexPenaltyWeight to tiles near Vortex towers.
- **GridManager.TriggerAirflowRefresh()** — new public method for Vortex to force path recalculation.
- **Wave 10 boss balance fix**: RadiationBlob health on wave 10 capped at flat 2.0× base (was _healthMultiplier * 3×, leading to ~6000 HP). Now ~800 HP max — still tanky but beatable.
- **Scene files**: VortexSeparator.tscn, PowerCore.tscn, BioNeutraliser.tscn, MagneticCage.tscn
- Build: 0 errors, 0 warnings.

#### Balance Notes
- Wave 10 RadiationBlob: was ~6000 HP (3× wave-10 multiplier ~5×), now ~800 HP (2× base)
- PowerCore ROI: earns back its $150 cost in 30 waves — best used early or mid-game as foundation income
- BioNeutraliser: 25% damage boost to 8 neighbors is strong when placed center of a cluster
- MagneticCage: 2s hold in a 2.5-tile radius — excellent for slow tanky blobs (RadiationBlob stays frozen, bought time for damage towers to shred it)
- VortexSeparator: path detour depends on grid layout — most effective with walls guiding the reroute

## Sprint 11
Status: ✅ Done

### Sprint 11 — New Enemy Types

#### Implemented
- `ParticleType` enum: `BioParticle, SporeSpeck, RadiationBlob, BacterialSwarm, SwarmUnit, CellDivision`
- **SporeSpeck** — HP 30, speed 3.0, size 0.4×tile, lime #aaff00. Fast scout.
- **RadiationBlob** — HP 400, speed 0.8, size 0.9×tile, orange #ff8c00. Immune to slow (ignores SlowMultiplier).
- **BacterialSwarm** — meta-type: spawns 8 SwarmUnits (HP 15, speed 2.0, size 0.3×tile, #88ff44) with random positional offsets.
- **CellDivision** — HP 80, speed 1.5, size 0.7×tile, pink #ff44aa. On death spawns 2 child CellDivisions (HP 30, size 0.4×tile). Children do NOT split further (`IsDivisionChild` guard).
- All stats added to `GameConfig`.
- `WaveManager` selects enemy types per wave (1-3 bio, 4 bio+spore, 5-6 spore+blob, 7 swarm, 8 division, 9 all, 10 boss blobs 3×HP + divisions).
- `WaveManager.RegisterExtraParticle()` — keeps alive counter correct when BacterialSwarm or CellDivision children spawn.
- `WavePreview` shows correct enemy type description per wave.

Build: 0 errors, 0 warnings.

## Sprint 10 — Visual Effects
Status: ✅ Done

### Implemented
- **DeathSplash** (`src/Effects/DeathSplash.cs`) — 7 green squares fly outward on particle death, fade over 0.4s
- **FloatingText** (`src/Effects/FloatingText.cs`) — reusable floating text, floats up 1s then fades
  - `+12` yellow popup on kill (ParticleManager)
  - `-1` red popup when particle escapes (ParticleManager)
- **BasicFilter glow pulse** — green circle flash (0.2s) when dealing damage
- **Electrostatic arc** — zigzag white/cyan lightning lines drawn to each slowed particle
- **UV Steriliser muzzle flash** — white circle flash (0.15s) on firing
- **AirflowVisualizer** (`src/Effects/AirflowVisualizer.cs`) — 8 air dots drift along path; yellow at <30%, red at <20% airflow
- **AmbientDust** (`src/Effects/AmbientDust.cs`) — 10 drifting 1×1 white dots at alpha 0.15

All effects: pure Godot DrawRect/DrawCircle/DrawLine, no sprites. Build: 0 errors.

## Sprint 8
Status: 🔴 Not started

## Sprint 7
Status: ✅ Done — Ready for player testing

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
