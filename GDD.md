# BioFilter — Game Design Document (GDD)

**Version:** 0.1  
**Engine:** Godot 4 (C#)  
**Target Platforms:** Windows, macOS, Linux  
**Genre:** 2D Top-Down Tower Defense  
**Status:** Pre-production / Design phase

---

## 1. Concept

You are managing the air filtration system of an underground bunker. Bio Particles — dangerous microscopic organisms, radiation particles, and chemical agents — are flowing through the air filter grid trying to reach the bunker's intake. Your job is to place filter modules and shape the airflow path to neutralize as many particles as possible before they reach the people inside.

**Core tension:** Every wall you place directs particles through narrower corridors (making your filters more effective) but also restricts total airflow. Too many choke points and the bunker suffocates. You must balance filtration efficiency against breathable air.

---

## 2. Grid & Visual Style

| Property | Value |
|---|---|
| Tile size | 16x16 pixels |
| Grid dimensions | 30 columns x 20 rows |
| Base resolution | 480x320 (scaled up) |
| Art style | Low pixel density, squared blocks only |
| Color palette | Few distinct colors per element type |

### Color palette (draft)
- **Background/empty tiles:** Dark grey `#1a1a2e`
- **Walls:** Mid grey `#4a4a6a`
- **Basic Filter:** Green `#00c853`
- **Electrostatic:** Blue `#2979ff`
- **UV Sterilizer:** Purple `#aa00ff`
- **Bio Particles:** Toxic green/yellow `#aeea00`
- **Bunker intake:** Orange `#ff6d00`
- **Particle spawn:** Red `#d50000`

---

## 3. Map Design

### Map 1 — "The Filter"
- **Entry:** Single spawn point, left edge (column 0), centered vertically (row 10)
- **Exit:** Entire right edge (column 29) is open — all right-edge tiles count as bunker intake
- **Default path:** Horizontal left-to-right, no obstacles at start
- **Buildable area:** All tiles except spawn point and the right-edge intake tiles

---

## 4. Airflow System

The airflow system is the central build limiter. It runs after every block placement.

### How it works
1. After placing a block, run a **flood-fill** from the particle spawn point to all reachable exit tiles
2. Identify **choke points** — corridors where the path narrows to 1 or 2 tiles wide
3. Calculate a **Restriction Score** based on:
   - Number of choke points
   - Width of each choke point (1 tile = high restriction, 2 tiles = medium, 3+ = low)
4. If the new Restriction Score would exceed the **Airflow Threshold**, the placement is rejected

### Airflow meter (UI)
- Displayed as a percentage: **100% = fully open**, **0% = blocked**
- Shown prominently in the HUD
- Color shifts: green → yellow → red as restriction increases
- Building is prevented when meter drops below ~20%

### Airflow calculation (choke point scoring)
```
score += (1 / corridor_width) * weight_per_choke_point
```
Total score is normalized to an Airflow % for display.

---

## 5. Particles

### Behavior
- Pathfind using **A\*** (recalculated after each build action)
- Take the shortest available route from spawn to any exit tile
- **Cannot be fully blocked** (airflow system prevents it)
- **Smooth movement** — no sharp 90° turns; particles have momentum/steering
- If flow splits into multiple paths, particles distribute across all available routes
- Particle speed is affected by the Electrostatic slow field

### Particle types (v1)
| Type | Health | Speed | Color |
|---|---|---|---|
| Bio Particle | 30 | 1.5 tiles/s | Toxic yellow-green |

More types added in later versions (Radiation, Chemical, Hardened, etc.)

---

## 6. Filter Modules (Towers)

All modules are 1x1 tile. Placed during build phase between waves.

| Module | Color | Effect | Range | Cost |
|---|---|---|---|---|
| Basic Filter | Green | Damages particles in radius | 2 tiles | 50 |
| Electrostatic | Blue | Slows particles in radius | 2 tiles | 75 |
| UV Sterilizer | Purple | Shoots projectile at nearest particle | 4 tiles | 100 |

### Upgrade path (v1 — one upgrade tier)
Each module can be upgraded once for 2x cost:
- Basic Filter → increased damage
- Electrostatic → increased slow %
- UV Sterilizer → increased fire rate

---

## 7. Economy

- **Starting currency:** 200
- **Earn currency:** Each particle killed drops currency (amount scales with particle health)
- **Spend currency:** Placing or upgrading modules
- **No selling** in v1 (may add later)

---

## 8. Wave System

- Waves start after player clicks "Start Wave" button
- Between waves: build phase (no time limit in v1)
- Wave composition increases in count and particle health each wave

| Wave | Particles | Health multiplier |
|---|---|---|
| 1 | 10 | 1.0x |
| 2 | 15 | 1.2x |
| 3 | 20 | 1.5x |
| ... | +5 | +0.3x |

---

## 9. Lives / Win-Lose

- **Bunker population:** Starts at 100
- Each particle that reaches the right edge: -1 population
- **Game over:** Population hits 0
- **Win condition v1:** Survive 10 waves

---

## 10. UI Layout

```
┌─────────────────────────────────────────────────┐
│  Wave: 1/10    Lives: 100    Currency: 200       │
│  Airflow: ████████░░ 80%                         │
├─────────────────────────────────────────────────┤
│                                                  │
│              GAME GRID (30x20)                  │
│                                                  │
├─────────────────────────────────────────────────┤
│  [Basic Filter $50] [Electrostatic $75] [UV $100]│
│                          [Start Wave]            │
└─────────────────────────────────────────────────┘
```

---

## 11. Technical Architecture (planned)

### Scene structure
```
Main
├── GridManager       — tile grid, placement logic, airflow calc
├── WaveManager       — spawning, wave progression
├── ParticleManager   — pathfinding, particle movement pool
├── TowerManager      — module placement, upgrades, targeting
├── EconomyManager    — currency, lives
└── UI
    ├── HUD           — wave, lives, currency, airflow meter
    └── BuildPanel    — module selection
```

### Key systems
- **A\* Pathfinding** — recalculated on grid change, cached between recalcs
- **Airflow scoring** — flood-fill + choke point analysis post-placement
- **Flow field** — considered for particle movement (more efficient than per-particle A\*)

---

## 12. Out of Scope (v1)

- Map editor
- Multiple maps
- Sound
- Animations (particles are simple colored squares)
- Save/load
- Selling modules
- Multiple particle types

---

## 13. Open Questions

- [ ] Should particles have a "fear" of narrow corridors (prefer wider paths even if longer)?
- [ ] Should split flows converge at the exit or count as separate streams?
- [ ] Exact choke point weighting formula — needs playtesting
