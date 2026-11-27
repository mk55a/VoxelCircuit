# Game Director: design

Status: **version 4.1, built** (2026-11-27), not yet playtested. Open decisions are in section 12.
Older versions are kept in `Docs/CaseStudies/GameDirector/history/` (v2, v3, v3.1, v4). The reasons behind each change are
in `Docs/CaseStudies/GameDirector/decision-log.md` (this version: entries 20 to 23).

**The idea in one sentence:** the designer sets *how much pressure* a stage puts on the player and *which enemies* it may
use; the director decides *what, how many, where and when*, by itself, while the game runs.

What changed from version 4 (owner's answers, decision 23):
- **Cost is per level**, in an *Enemy Cost Table* asset. Levels that should feel alike share a table.
- **No cost per formation.** A group costs count × enemy cost. The formation is chosen to *challenge*, not to be paid for.
- **The director reads the player:** it crowds the **height lane** the player uses most, and keeps **one side of that lane
  open** unless the stage allows pincers.
- **Hot streak:** many kills without being hit raise the credit rate, so a player who is doing well keeps getting more.
- **Entries instead of one spawn distance:** each enemy lists ways its groups may enter (just off-screen, or at a distance),
  with weights. Entering from above or below the arena comes with the formations work.
- **Names:** *Longest wait* is now **Patience**, *Gap between groups* is **Cooldown**.
- **Group size stays a hand-tuned slider** for now. Making it adapt needs measurements first (section 11).

---

## 1. The words

| Word | Meaning | Example |
|---|---|---|
| **Level** | The whole run: waves in order, plus the director's level-wide rules | Level 1, about 10 minutes |
| **Wave** | A chapter of the level. Has a *type* | "First contact" (standard), "Boss" (later) |
| **Stage** | A part of a wave. **The smallest thing you design** | a quiet opening, a peak, a rest |
| **Credits** | What the director earns over time and spends on enemies | 4 credits per second, saved up to 40 |
| **Cost** | Credits for **one** enemy, set per level in a cost table | Chaser 1, Bomber 2, a boss 50 |
| **Weight** | How likely something is to be picked, compared with the others | Chaser 3, Bomber 1 → about 75% / 25% |
| **Group** | One formation of one enemy type, bought in one go. Costs count × cost | 12 Bombers in a line = 24 credits |
| **Plan** | The group the director has decided to buy next, and is saving for | "next: 12 Bombers, line, top lane, from the left" |
| **Lane** | A third of the arena's height: bottom, middle, top (4 m each) | |

---

## 2. What the designer sets

### On each enemy (`EnemyDefinition`, set once)

| Setting | Meaning | Example |
|---|---|---|
| Prefab, Max alive, Prewarm | As before | |
| **Formations** | Formations it may fly in, each with **Min** and **Max** count and a **Weight** | Chaser: Wave Queue 6–20 (×2), Line 5–12, Grid 10–20 |
| **Entries** | How its groups may enter, each with a weight. Empty = just off-screen | *Just off-screen*; *At distance* 120 m (a boss) |

A formation's own *Count* is ignored here; the director picks the count. A grid keeps its columns and adds rows.

### On each level

| Setting | Meaning | Test level |
|---|---|---|
| Waves | In order | |
| **Max enemies alive** | The hard performance cap | 150 |
| **Costs** | An Enemy Cost Table asset | Chaser 1, Bomber 2 |
| Variety dip / recovery | After a pick, its chance × dip, recovering over this many seconds | 0.3, 8 s |
| Group size spread | How far a group's size may stray from the stage's Group Size, at random | 0.2 |
| Lane memory | Seconds of the player's lane history that count | 8 s |
| Pincer window | Seconds a lane keeps the side its last group came from | 6 s |
| Streak kills / window / bonus | Recent kills that count as hot; how fast they fade; credit rate × bonus while hot | 20, 8 s, ×1.5 |

### On each stage (`StageDefinition`)

| Group | Setting | Meaning |
|---|---|---|
| Ending | **End mode**, Duration, Max duration | Section 6 |
| Credits | **Starting credits** | Credits the stage opens with |
| | **Credits per second** | How fast credits are earned: the speed of saving |
| | **Credit cap** | The most it can save. Limits the biggest group and the burst after a clear |
| Spending | **Group size** | Slider, small ↔ big (section 4) |
| | **Patience** | Seconds it saves for one plan before buying smaller or picking again |
| | **Cooldown** | Minimum seconds between two groups |
| | **Max alive** | Enemies alive at once in this stage |
| Enemies | **The enemy list** | One row per enemy: *Enemy · On · Weight · Max alive* (section 3) |
| Where | **Sides** | Either, Ahead (where the ship is heading), Behind |
| | **Lane focus** | 0 to 1: chance a group goes into the player's favourite lane |
| | **Allow pincer** | Allow the player's lane to be closed from both sides at once |

---

## 3. The enemy list: on, weight, max alive

| Enemy | On | Weight | Max alive |
|---|---|---|---|
| Chaser | ✔ | 3 | 60 |
| Bomber | ✔ | 1 | 20 |

- **On** (the unlock toggle): may this enemy spawn in this stage? Missing or off = no.
- **Weight:** its chance compared with the other rows. Only rows that are on, have a cost, and have room for their smallest
  group take part.
- **Max alive:** its limit in this stage. The enemy's own limit and the level's still apply.
- **Variety:** right after an enemy (or a formation) is picked, its chance drops and recovers over a few seconds.

---

## 4. How the director decides

Every frame of a stage:

```
1. credits += credits per second × (streak bonus if hot) × dt, up to the cap
2. no plan? make one:
     enemy     = weighted pick from the stage list (on, has a cost, room for its smallest group, with variety)
     formation = weighted pick from the enemy's formations (with variety)
     count     = between min and max, pushed by Group Size ± spread, kept under every limit and the credit cap
     entry     = weighted pick from the enemy's entries
     lane      = the player's favourite lane (chance = Lane focus), otherwise a random lane
     side      = from Sides; but if that lane was just entered from one side and pincers aren't allowed, the same side
3. plan older than Patience? buy a smaller group that is affordable and fits, or forget it and plan again
4. can't afford it → saving.   cooldown running → wait.   a limit reached → wait for room.
5. ask the gate, then the spawner. Spawned: pay, remember the pick and the lane's side, start the cooldown.
```

**Saving is not a separate mode.** It happens because the director decides *what* to buy before it can afford it. Group
size *small* gives a steady trickle; *big* gives calm, then a burst. Patience keeps a stage from going quiet by accident.

**Credits while full:** they keep coming in, up to the cap, so clearing a crowd can be followed straight away by a group.
**Between stages:** each stage starts with its own Starting credits. **Enemies alive at a stage change** stay.

**Patience is not a cooldown.** The cooldown is a deliberate pause *after* a group. Patience is how long the director is
willing to *save* for one group before settling for something smaller.

---

## 5. Where groups appear

Everything is relative to the player and worked out at runtime.

- **Lane:** the formation's centre goes to the middle of the chosen lane, moved in if the formation is too tall to fit.
  The player's favourite lane comes from where they spent time recently (`LaneTracker`, fading over *Lane memory*). A
  stage with high Lane focus crowds that lane, which pushes the player out of it.
- **Side:** screen-left or screen-right of the player. *Ahead* follows the ship's heading.
- **A way out:** when pincers aren't allowed, a lane that a group just entered from the left keeps getting its groups from
  the left for *Pincer window* seconds, so the other side of that lane stays open. A peak can allow pincers on purpose.
- **Entry:** *Just off-screen* searches outward from the screen edge on that side for the first hidden place (as before).
  *At distance* puts the group that many metres along the ring (up to half a lap, 188 m), so the player sees it coming.
- **Spacing:** a new group's centre keeps *Group spacing* metres (in the Spawn Placement asset, 12 m) from groups already in
  the arena, or the spawner looks further out.

**Planned:** entering from **above or below** the arena (on screen, flying in through the top or bottom), as new entry
styles, together with the formations work the owner wants to do next. Movement stays with the formations and enemies.

---

## 6. How a stage ends

| End mode | What happens |
|---|---|
| **Time up** | The director buys for *Duration* seconds, then the next stage starts. Enemies alive stay |
| **Time up, then cleared** | Buys for *Duration*, stops buying, then waits for every enemy to die. *Max duration* moves on anyway |
| **All cleared** | Buys for *Duration*, but ends as soon as no enemy is left once one appeared. *Max duration* moves on anyway |

Version 4 proposed merging *All cleared* into *Time up, then cleared*. It was kept instead, because it is a different
stage: "clear this to move on early".

---

## 7. Keeping the wrong enemies out

Every request, the director's and the debugger's, passes the **`SpawnGate`**: the enemy is on in the stage's list; under
its limit in the stage, the stage's limit, its own limit and the level's 150. The debugger may ignore the stage list
(the log says so); it never ignores the enemy's own limit or the level's.

Warnings, in the Console when Play starts: a stage with nothing on; no credits at all; an enemy that is on without
formations or without a cost in the level's table; a smallest group that costs more than the credit cap; a limit lower than
the smallest group.

---

## 8. Classes

| Class | Where | One job |
|---|---|---|
| `SpawnDirector` | Core | The loop of section 4 |
| `GroupPlanner`, `GroupPlan` | Core | Makes the plan: enemy, formation, count, entry, lane, side |
| `CreditWallet` | Core | Earn up to a cap, spend |
| `VarietyMemory`, `WeightedPick` | Core | Weighted picks with the dip after a pick |
| `LaneTracker` | Core | The player's lane history and favourite lane |
| `StreakMeter` | Core | Recent kills, ended by a hit |
| `SpawnGate` | Core | Section 7 |
| `IEnemyType`, `IFormationOption`, `IEntryOption`, `IStageEnemy`, `ICostTable`, `IPlayerWatch`, `IKillFeed`, `SpawnRequest` | Core | What the director needs, without Unity |
| `EnemyDefinition` (+ `EnemyFormationOption`, `EnemyEntryOption`), `EnemyCostTable` | Runtime | Assets |
| `StageDefinition` (+ `StageEnemyEntry`), `LevelDefinition` | Runtime | Assets; the level also checks for setup mistakes |
| `EnemySpawner` | Runtime | Exact place: lane height, side, entry, spacing |
| `EnemyPool` | Runtime | Also the kill feed (enemies destroyed in play, not removed by a skip) |
| `PlayerWatch` | Runtime | The ship's height and heading, and lives lost, for the director |
| `LevelManager` | Runtime | Builds the run and the director, ticks both |
| Level Debugger, `DirectorGraph` | Editor | Section 9 |

Removed: `SquadDefinition`, `SquadScheduler`, `EnemyRoster`, stage Role and Intensity, wave Roster, stage Allowed Enemies
and Scripted Squads, `StageIntensityGraph`.

---

## 9. Level Debugger

- **Director:** credits (and the cap), credits per second (marked during a hot streak), what it is doing (saving, cooling
  down, waiting for room, nothing affordable...), the plan (count, enemy, formation, cost, lane, side, how long it has
  waited), the player's favourite lane and its share of recent time, the streak, and the last problem.
- **Graph:** credits against the cap (orange), enemies alive against the stage's limit (cyan), a tick for each group, over
  the last 60 s. This is where the pace shows.
- **Controls:** Give 10 credits, Spawn plan now, New plan, Freeze credits; **Spawn a group** (enemy, formation, count,
  optionally ignoring the stage list); plus skip, finish, jump, restart, pause, clear and game speed as before.
- The log lists each group as it spawns.

---

## 10. Events

`IRunEvents` as built. Stage events no longer carry a role. The director raises `GroupSpawned` (the plan) and
`GroupDropped` (a plan given up, with the reason).

## 11. Later, and what each needs first

| Idea | Needs first |
|---|---|
| **Adaptive group size** (bigger when the player copes, smaller when struggling) | Measurements: how long enemies live after they arrive (killed on entry = too easy), close calls (an enemy within a few metres), hits and deaths. Show them in the debugger, playtest, then decide the rule |
| Entries from above / below | The formations work |
| Seeds (replay a run) | Wanting to replay; the director already takes its random numbers from one source |
| Music | Will follow measured numbers (alive, spending), not stage roles |
| Formation spacing warnings, Chaser separation | Small, not blocked |
| Boss wave | A boss enemy |

## 12. Open decisions (yours)

1. **Death:** when the player dies mid-stage, does the director keep spending, pause, or lose its saved credits?
   Built for now: it keeps going, and the streak ends.
2. Should the streak cool down gradually instead of ending at once on a hit?
3. A pause between waves (for a title on the glass, a power-up)?
4. The starting numbers in the test level are guesses. Playtesting decides them.
