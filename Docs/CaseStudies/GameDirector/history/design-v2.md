# Game Director: design

Status: **draft for review, version 2**. Nothing here is built yet. Decisions still open are listed at the end.

Version 2 changes: threat is no longer "enemy count × cost". It now depends on the formation, where the wave spawns,
how close enemies are, how many are charging at once, and which types are mixed together. The director is built as a
**tool** (an editor window with simulation, live view and playtest recording), not just a component.

---

## 1. What the director is for

The director decides **when** enemies come, **which** enemies, in **what formation**, **where**, and **how many**, so
that a run follows the intensity you designed while still playing out differently every time.

It does not move enemies, create GameObjects or draw game UI. It makes decisions and announces them:
- "spawn this wave here, now" goes to the spawner;
- "stage 2 started", "a peak started" and similar moments go out as events for UI, power-ups and music.

You configure and tune it entirely in the **Game Director window** and the assets it edits. Code only changes when a
new *kind* of behaviour is needed (a new enemy, a new threat factor), never to rebalance a level.

---

## 2. Why "count × cost" is not enough

Two waves with the same number of the same enemy can feel completely different:

| Aspect | Easier | Harder | Why |
|---|---|---|---|
| **Formation** | Horizontal line | Wave queue | A line can be shot from a standing position; a wave forces the player to move *and* aim |
| **Spawn location** | Far side of the cylinder | On screen, near the player | Near and visible means seconds to react; far away means time to prepare |
| **Engagement** | Still flying in formation | Broken off, charging | Charging enemies are the immediate danger |
| **Crowding** | 3 charging at once | 12 charging at once | Danger grows faster than the count: escape routes close |
| **Combination** | One type at a time | Chasers + Gunners together | Different threats demand different responses at the same moment |
| **Count** | 5 | 25 | More of everything above |

So threat is a **product of several factors**. The director needs to measure it the way the player feels it.

---

## 3. The threat model: two measurements

The director uses two numbers, both in the same unit ("threat"):

### 3a. Wave threat (a prediction, worked out before a wave is bought)

How much pressure this wave is expected to add once it arrives:

```
wave threat = count × enemy threat × formation factor × spawn zone factor
```

| Factor | Where it is set | Example values |
|---|---|---|
| **Enemy threat** | On the enemy type | Explosive 1, Drifter 1, Chaser 2, Gunner 5 |
| **Formation factor** | On each formation asset | Horizontal line 0.8 (stand and shoot), vertical line 1.0, grid 1.1, wave queue 1.3 (move and shoot) |
| **Spawn zone factor** | In the threat model, per zone | Far side 0.7, off-screen 1.0, on screen far 1.2, on screen near 1.5 |

The tool shows the **breakdown** for every wave card, so you can see *why* a wave is expensive
(for example "25 × 2 × 1.3 × 1.5 = 97.5: mostly because it spawns near the player").

The formation factor starts as your judgement. The tool can also suggest a value from the formation's settings
(wave amplitude, vertical spread, speed) and, once playtest data exists, from how often players got hit by that formation (section 8).

### 3b. Live pressure (a measurement, taken every frame from what is actually happening)

How much danger the player is in *right now*:

```
live pressure = crowding( sum over alive enemies of:  enemy threat × state factor × proximity factor )
               × mix factor
```

| Factor | Meaning | Tuned as |
|---|---|---|
| **State factor** | In formation (approaching) vs charging (broken off, steering at the player) | e.g. 0.5 vs 1.0 |
| **Proximity factor** | Closer enemies count more; on-screen enemies count more than off-screen | A curve: distance to player → factor |
| **Crowding** | Many enemies charging at once is worse than the sum of them one by one | A curve: raw pressure → final pressure (bends upward) |
| **Mix factor** | Several enemy types engaging at once | e.g. +10% per extra type engaging |

All of these are curves and numbers in one **ThreatModel** asset, edited in the tool with a live preview.

### 3c. How the director uses them

```
target    = intensity (from the line) × peak threat
room      = target − live pressure − threat of waves already on their way
buy a wave whose wave threat fits in the room, if the spawn gap has passed
```

- Waves "already on their way" are counted, so the director doesn't buy three waves in a row before the first one arrives.
- When the player kills fast, live pressure drops, room opens, and the next wave comes sooner: a player who is on fire
  gets challenged more without a special rule.
- When enemies crowd around the player, live pressure climbs past the target and the director **holds back**, even if
  few enemies are alive in total. This is the difference from version 1.

### 3d. Variety: avoiding the same demand twice

Each formation also has a **demand tag**: *Stand and shoot*, *Move and shoot*, *Dodge*. The wave picker lowers the
weight of cards whose demand matches the last few waves (tunable). That way a run alternates what it asks of the
player instead of repeating the same kind of wave, even when the numbers allow it.

---

## 4. What the director tracks

| Input | Used for | Available |
|---|---|---|
| **Run time** | Position on the intensity line, current stage | Now |
| **Every alive enemy**: type, position, in formation or charging | Live pressure | Now (enemies report through the pool and the target registry) |
| **Player position** | Proximity, spawn zones | Now |
| **Waves on their way** | Room calculation | Now |
| **Recent demand tags** | Variety | Now |
| **Player heat** (-1 struggling to +1 on fire) | Small push of intensity up or down | Partly now; fully once the multiplier exists |

**Player heat** is built from signals behind `IPlayerPerformance`: kills per second recently (up), time since last hit
(up), life lost (sharp drop, mercy), and later the **multiplier** (up). Score is not used: it only ever goes up, so it
can't say how the player is doing *right now*; the multiplier can.

```
final intensity = designed intensity + heat × heat strength      (heat strength e.g. 0.15; 0 while tuning)
```

---

## 5. Spawn zones

Spawn location becomes a choice on each wave card instead of always "hidden":

| Zone | Where | Feels like |
|---|---|---|
| **Far side** | Opposite side of the cylinder, behind the arena | Slow build; the wave travels toward the player |
| **Off-screen** | Just outside the camera's view, left or right | Arrives soon, from a known side |
| **On screen, far** | Visible, at a safe distance | Player sees it form up; time to react |
| **On screen, near** | Visible, close to the player (never closer than a safe distance) | Urgent |

On-screen zones need the visible spawn-in (backlog 1: enemies appear one by one, harmless until the formation is
complete). Until that exists, the spawner supports **far side** and **off-screen**, and cards set to on-screen zones are
shown as "not available yet" in the tool.

---

## 6. Configuring a level

A level is a list of **stages** played in order. Each stage has its own piece of the intensity line, its own wave
cards and its own events.

```
intensity
 1.0 |                                              ____
     |                              /\    /\      /    \
 0.5 |            /\    /\        /    \/    \  /        \
     |   /\     /    \/    \____/                         \
 0.0 |__/  \___/
     +-------+--------------+---------------+------------+--> time
      Warm-up   Swarm 1         Pressure       Finale
```

### Assets (all in `Assets/Tuning/Director/`)

**`EnemyDefinition`** (one per enemy type): prefab, enemy threat, max alive at once, pool prewarm count.

**`FormationDefinition`** (existing assets, with two new optional fields): formation factor, demand tag.

**`WaveDefinition`** (a "wave card": one group of one enemy type, flying in one formation, bought in one go):

| Field | Meaning |
|---|---|
| Enemy | Which `EnemyDefinition` |
| Formations | One or more; one is picked at random each time |
| Spawn zones | One or more allowed zones; one is picked at random |
| Count range | Fewest and most; scaled with intensity and never beyond the room |
| Entry side | Left, right or random |
| Intensity window | Min and max intensity at which the card can be picked |
| Cooldown | Seconds before the same card can be picked again |
| Weight | Relative chance among allowed cards |

**`ThreatModel`**: zone factors, state factors, proximity curve, crowding curve, mix factor, variety settings. Usually one
shared by all levels.

**`LevelProfile`**: the stages (duration, intensity curve, optional build-up/peak/relax cycle, wave cards, timed
markers and signals), peak threat, spawn gap, max enemies alive (performance cap, about 150), end-of-run behaviour.

**`DifficultyProfile`** (Easy, Normal, Hard): multipliers on top of any level (peak threat, spawn gap, heat strength,
later enemy speed), so a level isn't copied three times.

**`DirectorSignal`**: a named moment (`PowerUpDrop`, `ShowStageTitle`, `MusicLayer2`), an asset instead of a string so
links can't break silently.

### Which enemy types come when

1. **Stage wave list:** a Gunner card simply isn't in Warm-up, so no Gunners appear there.
2. **Intensity window** on each card: big waves only near peaks.
3. **Weight and variety:** among allowed cards, how likely each is, lowered for repeated demand.

---

## 7. Baking a level and keeping it unpredictable

**"Level 1 Easy" = the `Level1` LevelProfile + the `Easy` DifficultyProfile.** That pair is the baked level: same
stages, curves and cards every time.

A **random seed** chosen at the start of each run drives every random choice (card, formation, zone, count, side,
exact spawn position, small timing jitter on cycles). The shape of the level is designed and repeatable; what the player
sees is different every run. The seed is shown in the tool and overlay so any run can be replayed exactly. The director
uses its own `System.Random`, so other systems can't change its sequence.

---

## 8. The Game Director tool

One editor window, **Cylinder Shooter > Game Director**, built with UI Toolkit. It works on the selected
`LevelProfile` and has these tabs.

### 8a. Timeline (design)

- The whole run as a horizontal timeline: stages as coloured blocks, the intensity line drawn across them, cycle phases
  shaded, timed markers and signals as pins.
- Drag stage edges to change durations; drag markers to move them; click a stage to edit its curve and cycle inline.
- The difficulty dropdown redraws the line with Easy / Normal / Hard applied.

### 8b. Wave library

- Every wave card as a row: enemy, formations, zones, count range, intensity window, weight, and its **wave threat
  breakdown** (count × enemy × formation × zone) at low and high counts.
- Filter by stage, enemy or demand tag. A matrix view shows which cards are available in which stage, so gaps are
  visible ("nothing for the 0.2-0.4 intensity range in Pressure").
- Warnings: cards that can never be picked (window outside every stage's range, or too expensive for peak threat),
  formations that don't fit the cylinder height, prefabs without a pool entry.

### 8c. Threat model

- Edit the factors and curves, with a small preview: a top-down strip of the cylinder where you drag fake enemies
  around a fake player and see live pressure update, factor by factor.

### 8d. Simulate (no Play mode)

- Runs the director for the whole level in a fraction of a second, with a seed (or several seeds at once).
- A **simple player model** (kill rate, how fast enemies close in) stands in for a real player, so pressure rises and
  falls realistically without playing. Version 1 uses an estimate; a later version can simulate enemy movement with the
  existing Core steering code.
- Graphs: target vs live pressure, intensity, waves bought (marks on the timeline, coloured by enemy type, shaped by
  zone), enemies alive, demand tag sequence.
- **Compare seeds:** overlays several runs so you can see how much they differ, and whether any seed produces a bad spike.

### 8e. Live (during Play mode)

- The same graphs, drawn as the game runs, plus a breakdown of live pressure right now (which enemies contribute most and why).
- Controls: jump to a stage or minute, 2× / 4× speed, pause the director, force the next peak, force a specific wave
  card ("spawn this now" to test it), restart with the same seed.

### 8f. Sessions (playtest recording)

- Every Play session records a timeline to a file: intensity, target, live pressure, waves (card, zone, formation),
  player hits, lives lost, kills, heat, seed.
- Open a recorded session over the designed line to see where the player actually got hit and what was on screen at that moment.
- Aggregate several sessions per card or formation: how often it caused a hit, average time to clear. That is real data
  for setting formation factors and enemy threat instead of guessing.

### In the scene view

- Gizmos for spawn zones around the player and the proximity rings from the threat model.

### In the game view

- A small overlay (toggle with a key): time, stage, phase, intensity, target vs pressure, last wave, seed. Useful in
  builds where the editor window isn't available.

---

## 9. Events

Published through `IDirectorEvents` (registered by `GameInstaller`, Observer pattern like `IDestructible.Destroyed`):

| Event | When | Example listeners |
|---|---|---|
| `RunStarted(seed)` | Run begins | UI, music, session recorder |
| `StageStarted(stage)` | A stage begins | Stage title on the arena glass, music |
| `PhaseChanged(phase)` | Build-up → Peak → Relax | Music intensity, warning before a peak |
| `WaveSpawned(wave)` | A wave is bought | Off-screen indicators, recorder |
| `Signal(signal)` | A `DirectorSignal` fires (stage start, timed marker, or every time a phase starts) | Power-up spawner, UI |
| `RunFinished(result)` | End of the last stage, or out of lives | Results screen |

Example: "at the start of every Relax, fire `PowerUpDrop`" delivers power-ups in the breathing room after a peak.

---

## 10. Architecture

```
Game.Core  (pure C#, unit tested, shared by game, tests and the simulator)
  Director/
    SpawnDirector        ticks everything below; outputs SpawnRequests and events
    DirectorClock        time, scale, pause, jump
    StagePlayer          stages, cycles, markers, signals
    IntensityModel       stage curve + cycle + heat
    ThreatModel logic    wave threat (prediction) and live pressure (measurement)
    WaveSelector         filter + weighted pick + variety
    PressureSnapshot     struct list of enemies (type, position, state) filled by Runtime each frame
    DirectorRandom       seeded System.Random
    I*Settings           interfaces the assets implement (like IShockwaveSettings)
  Recording/
    SessionRecorder      appends samples to a buffer (no allocations per frame)

Game.Runtime
  Director/
    GameDirector (MonoBehaviour)   builds the snapshot, ticks SpawnDirector, forwards requests, raises IDirectorEvents
    PlayerPerformance              IPlayerPerformance (kills, hits, later multiplier)
    Assets: EnemyDefinition, WaveDefinition, ThreatModelAsset, LevelProfile, DifficultyProfile, DirectorSignal
  Enemies/Spawning/
    EnemyPool                      shared service, one pool per prefab, prewarmed
    IPooledEnemy                   spawn/return hooks + "in formation or charging" state (Chaser, Drifter, later Explosive)
    FormationSpawner : IEnemySpawner   far side and off-screen zones now; on-screen zones with the spawn-in later
  Debug/
    DirectorOverlay                game-view overlay

Game.Editor
  GameDirectorWindow (UI Toolkit): Timeline, Wave library, Threat model, Simulate, Live, Sessions
  Graph and timeline VisualElements (drawn with Painter2D), scene gizmos
```

Key points:
- **All decisions in Core.** The simulator runs the exact same `SpawnDirector` code as the game, just ticked forward
  with a simulated player instead of a real one.
- **Runtime feeds Core a snapshot**, a reused list of structs (type, position, state). Core never sees GameObjects.
- **The director never spawns.** It sends a `SpawnRequest` (card, formation, zone, count, side) to `IEnemySpawner`.
  The visible spawn-in later is a new spawner; the director doesn't change.
- **New enemy types** implement `IPooledEnemy` and get an `EnemyDefinition`. The director needs nothing else.
- **No per-frame garbage:** snapshots and requests are reused structs; graphs in the window redraw only when data changes.

---

## 11. How you will tune a level

1. **Shape the line** in the Timeline tab.
2. **Fill the stages** with wave cards in the Wave library; fix any warnings.
3. **Simulate** several seeds; check pressure follows the target and no seed spikes badly.
4. **Play** with the Live tab open; jump to later stages with the time controls; force specific cards to test them.
5. **Adjust while playing.** Asset edits apply immediately and are kept after Play stops.
6. **Read the sessions.** Where did players get hit? Which formations caused it? Adjust formation factors and enemy threat from that.
7. **Difficulty:** compare Easy / Normal / Hard on the timeline, then play each.

---

## 12. Build order

Each step ends with something you can use.

1. **Core director + threat model** with EditMode tests (wave threat, live pressure, room, selection, variety, seeds, stages, events).
2. **Assets** and their Inspectors with tooltips.
3. **EnemyPool, IPooledEnemy** (Chaser, Drifter), **FormationSpawner** with far-side and off-screen zones.
4. **GameDirector component, IDirectorEvents, PlayerPerformance**; GameDirector scene setup, click by click. A level is playable here.
5. **Game Director window, first tabs:** Timeline, Wave library, Live (with time controls). Game-view overlay.
6. **Simulate** tab.
7. **Sessions** tab (recorder + viewer + per-card statistics).
8. **Threat model** tab with the drag-around preview.
9. Later, without changing the director: visible spawn-in (on-screen zones), the Explosive enemy, the multiplier as a heat signal.

---

## 13. Open decisions (yours)

1. **Stages:** a list of stages each with its own curve (this design), or one curve for the whole run with stage markers?
2. **Difficulty:** `DifficultyProfile` multipliers on top of a level (this design), or a separate level asset per difficulty?
3. **Heat strength default:** suggest ±15% (0 while tuning).
4. **End of the run:** what happens at 10:00? Results only, a final wave, a boss later?
5. **Death:** does the run end when lives run out? Does the director pause during respawn?
6. **Peak threat starting value:** to be tuned by playing; the simulator will suggest one from the cards.
7. **Tool order:** is the order in section 12 right, or do you want the Simulate tab before the Live tab?
8. **Factors:** anything missing from section 2? (Ideas: enemies attacking from both sides at once, enemies above and
   below the player at the same time, enemies inside the shockwave's reach.)
