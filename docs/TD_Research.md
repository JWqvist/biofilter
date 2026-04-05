# BioFilter — Tower Defense Research & Design Inspiration

## Games Researched
Kingdom Rush Series · Bloons TD 6 · Plants vs. Zombies · Mindustry · Sanctum 2 · Defender's Quest

---

## 1. Tower/Filter Module Ideas

### From Bloons TD 6 (4 categories: Primary, Military, Magic, Support)
BTD6's category system ensures every tower fills a role. BioFilter should do the same:

| Category | BioFilter equivalent | Role |
|---|---|---|
| **Primary** | Basic Filter, UV Steriliser | Direct damage/kill |
| **Support** | Electrostatic, Catalyst | Debuff, enhance others |
| **Area Control** | Wall, Baffle Plate | Redirect/shape flow |
| **Passive** | HEPA Core, Carbon Trap | Passive continuous effect |

### New filter module ideas inspired by research:

**🧪 Chemical Neutraliser** (new)
- Drops a "neutralisation zone" on the path — particles moving through take DoT damage
- Like a poison tower but themed as chemical spray

**🌀 Vortex Separator** (new — inspired by Mindustry conveyor logic)
- Creates a spiral that pulls particles off the shortest path into a longer route
- Tactical: force particles through more filters before reaching exit

**💡 UV Pulse** (upgrade path for UV Steriliser)
- Instead of single-target shooting, releases a radial pulse every 3 seconds
- Area damage but slow fire rate — tradeoff

**🧲 Magnetic Cage** (new — inspired by Electrostatic)
- Pulls metallic particles (specific enemy type) and holds them for 2 seconds
- Combines slow + bunching effect for other filters to hit

**🌡️ Thermal Oxidiser** (new)
- High damage, short range, melts particles at close range
- Weak against chemical particles (immune to heat)

**💊 Bio Neutraliser** (new — support/synergy tower)
- Boosts damage of adjacent filters by 25%
- No direct damage — pure support
- Inspired by PvZ's Sunflower (passive support role)

**🔋 Power Core** (new — economy/support)
- Generates +5 currency per wave passively
- Inspired by PvZ's sun-generating plants and BTD6's Banana Farm

---

## 2. Build Controls Best Practices

### Kingdom Rush: Build ring
- Click tile → circular radial menu appears with buildable options
- Very tactile, minimal UI clutter
- **BioFilter idea:** Click empty tile → small radial menu with 4 options (Wall, Filter types)

### BTD6: Upgrade sidebar
- Click placed tower → sidebar slides in with upgrade tree (5 tiers, 2 paths)
- **BioFilter idea:** Click placed filter → overlay panel shows 2 upgrade paths

### PvZ: Drag-and-drop from bottom bar
- Plants displayed as cards at bottom, drag to lane to place
- Cards have cooldown timer after use
- **BioFilter idea:** Filter cards with placement cooldown (prevent spam placement)

### Mindustry: Blueprint system
- Save and paste groups of buildings as blueprints
- **BioFilter idea (later):** Save filter configurations as blueprints to paste

### Best practices summary:
- Right-click = sell/refund (universal standard) ✅ already done
- Hotkeys for quick tower selection (1, 2, 3 keys)
- Preview mode: show range circle before placing
- Ghost placement: transparent tile preview while hovering

---

## 3. Menu Structure Recommendations

### What the best TD games have:
1. **Main Menu** — play, settings, credits
2. **Level Select** — map/campaign view (even single map has a "select" screen)
3. **In-game HUD** — compact, no overlap with gameplay
4. **Wave start screen** — brief pause, show what's coming
5. **Pause menu** — resume, settings, quit ✅ already done
6. **End screen** — stats, score, stars rating, next level

### For BioFilter — recommend adding:
- **Pre-wave intel screen**: "Wave 3 incoming — 25 Bio Particles, 3 Radiation Spores" (2-3 sec before wave)
- **End-game stats screen**: particles killed %, currency earned, waves survived, filter efficiency %
- **Settings screen**: volume, fullscreen toggle, speed multiplier (1x/2x/3x)

---

## 4. Phase System Ideas

### Kingdom Rush: Waves auto-start with countdown
- No build phase — constant pressure
- **Not good for BioFilter** — our airflow mechanic needs build time

### BTD6: Round-based, manual start
- Player clicks "Start Round" — can prep between rounds ✅ this is what we do
- Can also enable "auto-start" in settings

### PvZ: Real-time but wave-based
- Plants placed before AND during waves
- Resources regenerate over time (sun drops)

### Recommendation for BioFilter:
Keep build phase → wave phase. But add:
- **Wave preview** (3 second countdown showing enemy types incoming)
- **Speed button** (2x during wave for experienced players)
- **Emergency placement** — allow placing during wave but at 150% cost (airflow still enforced)
- **Airflow warning** at 30% — visual + audio cue before it hits 20% limit

---

## 5. Enemy Type Ideas

### From research — enemy archetypes that work well:
| Type | Mechanic | BioFilter version |
|---|---|---|
| **Fast/Scout** | Low HP, fast — reaches exit if not stopped quick | **Spore Speck** — tiny, fast, low HP |
| **Tank/Slow** | High HP, slow — survives many hits | **Radiation Blob** — massive HP, immune to slow |
| **Swarm** | Many small units at once | **Bacterial Swarm** — 20 tiny particles as one wave |
| **Armoured** | Resistant to one damage type | **Chemical Shell** — immune to UV, only chemical damage works |
| **Regenerating** | HP slowly recovers | **Prion Particle** — heals 5HP/sec, needs sustained damage |
| **Split** | Splits into 2 on death | **Cell Division** — splits into 2 smaller particles on death |
| **Flying/Phase** | Ignores certain obstacles | **Airborne Virus** — ignores wall redirects, goes straight |
| **Boss** | One massive unit per 5 waves | **Mega Pathogen** — 500HP, spawns minions on hit |

---

## 6. Scaling & Difficulty Curve

### Best practices from research:
- **BTD6 model**: Every 5 waves introduce a new mechanic/enemy type
- **Kingdom Rush**: Early waves teach one thing at a time
- **Defender's Quest**: Dynamic difficulty — game adjusts based on player performance

### Recommended BioFilter curve:
| Waves | Difficulty | New element introduced |
|---|---|---|
| 1-3 | Tutorial | Basic Bio Particles, learn placement |
| 4-5 | Easy | Spore Specks (fast, low HP) |
| 6-7 | Medium | Radiation Blobs (high HP, slow) |
| 8 | Medium-Hard | Bacterial Swarm (mass attack) |
| 9 | Hard | Chemical Shells (type immunity) |
| 10 | Boss | Mega Pathogen (boss wave) |

### Health scaling (current GameConfig):
- Wave 1: 100 HP · Wave 10: 100 + (10 × 45) = **550 HP** ✅ reasonable

---

## 7. Economy Recommendations

### From research:
- **BTD6**: Cash per pop + end-of-round bonus + Banana Farm passive income
- **Kingdom Rush**: Gold per kill + tower sell at 80% value
- **PvZ**: Sun harvesting (passive + active) + plant cost varies

### Recommended improvements for BioFilter:
1. **Wave completion bonus**: +50 currency if 0 particles escaped that wave
2. **Efficiency bonus**: +25 currency if airflow stayed above 60% all wave
3. **Filter Module interest**: Each placed filter earns tiny passive income per wave (simulates filter operating cost savings)
4. **Tiered kill rewards**: Mini particles = 5cr, normal = 12cr, heavy = 25cr, boss = 150cr

### Current GameConfig economy check:
- Start: $200 — can place 4 Basic Filters
- Kill reward: $12 — need ~5 kills to afford next filter
- This feels slightly tight for early waves — consider $250 start or $15 per kill

---

## 8. Visual & Animation Ideas

### Pixel art effects that would look great in BioFilter:

**Particle death effects:**
- Small green splat/burst (4-8 pixels) when particle dies
- Radiation blob: green ring pulse on death
- Swarm: 3-4 scatter dots flying out

**Filter activation:**
- Basic Filter: brief green glow pulse (2 frames) when damaging
- Electrostatic: electric arc lines between tower and particle (zigzag pixels)
- UV Steriliser: bright white flash at impact point

**Airflow visualization:**
- Subtle animated dots flowing from spawn to exit (background layer)
- Speed of dots = airflow percentage
- When airflow drops below 40%, dots slow and turn yellow
- Below 20%: dots nearly stop, turn red, screen edge vignette

**Ambient effects:**
- Occasional "dust particle" floating across empty tiles (background)
- Spawn point: constant subtle red shimmer/pulse
- Bunker intake (exit): green shimmer effect

**UI animations:**
- Currency counter: brief yellow flash + "+12" popup on kill
- Population: red flash + "-1" popup when particle escapes
- Wave start: scanline wipe effect across screen (old CRT style)
- Airflow meter: segmented LCD bar that flickers when low

**Screen effects:**
- CRT scanline overlay (subtle, toggleable)
- Vignette darkening at screen edges when airflow is critical
- Screen shake (small) when population drops

---

## Priority Recommendations for BioFilter v1.1

**High priority (implement next):**
1. Wave preview screen (show enemy types before wave starts)
2. Tower range preview circle when hovering placement
3. Hotkeys 1/2/3 for filter selection
4. New enemy type: Spore Speck (fast, low HP)
5. Particle death splash effect

**Medium priority:**
1. Power Core (passive currency generator)
2. Wave completion currency bonus
3. Airflow visualization (flowing dots)
4. Pre-wave intel "incoming" message

**Lower priority (later sprints):**
1. Split particles (Cell Division mechanic)
2. Boss wave every 5 waves
3. Blueprint system
4. CRT scanline screen effect
