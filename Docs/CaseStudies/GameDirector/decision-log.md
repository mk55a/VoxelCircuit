# Game Director: decision log

How the requirements and decisions changed, and why. New entries go at the bottom. Old entries are never rewritten: when a
decision changes, the old one is marked *Superseded* and a new one explains what was learned.

**Status:** *Requirement* (the owner asked for it) · *Accepted* (the owner chose it) · *Proposed* (suggested, not yet agreed) ·
*Built* · *Superseded*

**Who:** the owner sets requirements and decides. Claude (AI assistant) proposed options and drafted the design documents.
Each entry says which is which, so the case study credits the right one.

Entries 1 to 12 were written on 2026-11-25 from the conversation, not at the time.

---

### 1. Build the director before the pool and the spawn-in
2026-11-23 · **Accepted** · Owner
- **Problem:** the backlog put formation spawn-in and the enemy pool first.
- **Decision:** director first, in its own `GameDirector` scene, after a project clean-up.

### 2. Intensity must be tuned in the editor, by playing
2026-11-23 · **Requirement** · Owner
- **Asked for:** "i need to be able to play test it, and change it."
- **Consequence:** all values live in assets, not code.

### 3. Budget earned per second
2026-11-23 · **Superseded by 5** · Proposed by Claude
- **Idea:** the director earns budget over time, faster at high intensity, and buys groups with it. The owner liked buying groups with a cost.
- **Why it changed:** it measures what was bought in the past, not what the player faces now.

### 4. Enemy types have a cost; runs must not be predictable; stages must send events
2026-11-23 · **Requirement** · Owner
- Cheap enemies should come in larger numbers as intensity rises.
- A finished level must be reusable ("level 1, easy") but still feel different each time.
- UI and power-ups need events at the stages of a level.
- A player who is doing well could be challenged more. A multiplier is planned; score doesn't exist yet.

### 5. Follow the threat that is on screen
2026-11-23 · **Superseded by 7** · Proposed by Claude (design version 1)
- **Idea:** target = intensity × peak threat. Spawn when the total cost of living enemies is below it.
- **Also proposed:** a random seed per run, and difficulty as multipliers on a level.
- **Note:** version 1 of the document was overwritten by version 2 before this log existed, so only this summary remains.

### 6. Enemy count is not difficulty
2026-11-23 · **Requirement** · Owner. **The turning point of the design.**
- **Observation:** near spawns are more urgent; enemies crowding and charging at the player matter; a wave formation forces
  moving and shooting while a horizontal line can be shot standing still. "formations x enemy types x spawn locations x number of enemies".
- **Consequence:** decision 5 was not enough. Two groups of equal cost can feel completely different.

### 7. Two measurements: predicted threat and live pressure
2026-11-23 · **Proposed** · Claude (design version 2), answering 6
- **Idea:** predict a group's threat before buying it; separately measure the danger around the player now (distance, charging, crowding, mix).
- **Trade-off:** many more numbers to tune. Hence the recorder, to set them from playtests.
- **Not proven:** every factor value is a guess so far.

### 8. The director is a tool
2026-11-23 · **Requirement** · Owner
- **Reason:** "since my skill set is to be a tools engineer". The editor tool is part of the result, not a helper.

### 9. Bomber: a fuse, and a chain reaction only when shot while armed
2026-11-23 · **Built** · Behaviour decided by owner, code drafted by Claude
- **Decision:** shot before arming, it pops. Shot while armed, it destroys members of its own formation.
- **Effect on the director:** `IFormationMember` was added, so a spawner no longer needs to know enemy types.

### 10. Level → Waves → Stages, with enemies allowed per stage
2026-11-25 · **Requirement** · Owner
- **Asked for:** design down to the stage; enemies must not appear in stages or waves they don't belong to; new wave types
  later, such as a boss.
- **Effect:** the stage replaces the repeating build-up / peak / relax cycle of version 2 (design version 3, *Proposed* by Claude).
  "Wave card" was renamed "squad", because "wave" now means part of a level.

### 11. A separate debug window, with Skip different from Finish
2026-11-25 · **Requirement** (debug window, skip, finish) · Owner. **Proposed** (the details) · Claude
- **Problem:** testing minute 8 should not take 8 minutes.
- **Idea:** Skip moves on with no rewards; Finish runs the real ending. End events carry the reason.

### 12. Music follows the intensity
2026-11-25 · **Requirement** · Owner. **Proposed** (how) · Claude
- **Asked for:** intense music at intense moments, another track for rest or warm-up, with transitions. Plan now, build later.
- **Risk identified:** pressure changes constantly, so music could flicker. Proposed: two thresholds, a minimum hold, and switching on the bar.

### 13. Enemies should not overlap too much
2026-11-25 · **Requirement** · Owner
- **Found when checked:** every formation uses 1.8 m spacing. The Chaser model is 1.8 m long with a 2.0 m hit circle, and the
  Bomber is 1.9 m tall, so grids and vertical lines already touch or overlap slightly.
- **Proposed (Claude):** spacing warnings, reserved cells, and separation for Chasers that have broken off.

### 14. Use regions of the arena to choose where to spawn
2026-11-25 · **Requirement** · Owner. **Proposed** (the grid) · Claude
- **Asked for:** split the arena into four invisible regions, choose which to populate, leave the player room when wanted, and
  challenge the lanes the player keeps using so they have to move.
- **Finding:** a quarter is 94 m, the screen shows about 36 m. Four is too coarse.
- **Proposed instead:** about 12 sectors x 3 height lanes, tunable, with an open route rule.
- **Risk:** punishing a lane can feel unfair if the player can't see why.
- **Not tested:** whether players really stay in two lanes. Record positions first.

### 15. Build now, or keep designing?
2026-11-25 · **Proposed** · Claude. Waiting for the owner
- **Proposal:** build the runners and debug window now. They don't depend on 13 or 14. Add the grid after a first playable level.

### 16. Build the runners and debug window, then playtest and iterate
2026-11-25 · **Accepted** · Owner (answers 15)
- **Decision:** build the level, wave and stage runners and the debug window now. Regions wait for playtest evidence.
- **Also asked for:** a reminder to take a screen recording whenever a decision is made, as documentation.

### 17. Runners and debug window, first version
2026-11-25 · **Built** · Code drafted by Claude, waiting for the owner's playtest
- **Built:** level, wave and stage runners in plain C#; one MonoBehaviour (`LevelManager`); the Level Debugger; Keep / Revert for
  assets changed during Play.
- **Left out on purpose:** allowed enemies, squads, signals, carry-over and kill count endings. They would be settings that do
  nothing until spawning exists.
- **Result:** 25 new EditMode and 3 new PlayMode tests pass. Not yet tried by hand.
- **Recording:** needed. The Level Debugger running the sample level with no enemies, as the "before" of spawning.

### 18. Spawning, first version: scripted squads, one gate, a shared pool
2026-11-26 · **Accepted** (build spawning next) · Owner. **Proposed** (the details) · Claude
- **Problem:** step 3 needs enemies to appear, but the director that decides *when* (threat, step 4) doesn't exist yet.
- **Decision:** each stage gets **scripted squads**: a squad and a time into the stage. That makes a level playable and fully
  designed by hand now; step 4 adds squads chosen by the director on top.
- **Allowed enemies:** a wave's roster, narrowed by a stage's list. An **empty roster allows nothing**, so a forgotten roster
  shows up as a clear refusal instead of silently letting everything in.
- **One gate** checks every request, including the debugger's: allowed enemy, the type's cap, the level's total cap.
- **Left out on purpose:** squad size ranges (a squad uses its formation's count), carry-over between stages, seeds,
  on-screen spawn zones. Spawning stays hidden off-screen, as in the sandbox.
- **Trade-off:** scripted timings are predictable. That is fine for a first playable level, and is what step 4 changes.
- **Built (2026-11-26):** `SpawnGate`, `SquadScheduler`, `EnemyRoster` (Core); `EnemyPool`, `EnemySpawner`, `EnemyDefinition`,
  `SquadDefinition` (Runtime); spawn section in the Level Debugger; warnings for squads that will be refused.
- **Result:** 13 new EditMode and 4 new PlayMode tests pass (EditMode 202/204 with the two old failures, PlayMode 31/31).
  The owner has added spawning to the GameDirector scene; how it plays has not been reported yet.
- **Recording:** needed. The Mines stage refusing a Chaser from the debugger, then allowing it with *Ignore allowed enemies*.

### 19. The stage asks for too much, and some of it does nothing
2026-11-26 · **Requirement** (the problem) · Owner. **Proposed** (a simpler stage) · Claude. **Superseded by 20** (the proposal; the problem stands)
- **Found by the owner while setting up a level:** *Role* (warm-up, peak...) only names a moment; what actually makes a
  warm-up or a peak is how many enemies come, when, and from which sides. A stage with an empty *Allowed Enemies* list
  and a scripted squad is confusing and easy to get wrong. Too many things to configure.
- **Checked in the code:** *Role* and the *Intensity* curve are only shown in the debugger and passed in events. Nothing in
  gameplay reads them. They were added for later steps (music, the director), which goes against the rule of decision 17
  ("no settings that do nothing yet"). *Allowed Enemies* and the wave *Roster* repeat what the squad list already says.
- **Lesson:** describing the pace with labels and curves came from the design document, not from building a level. The
  first attempt to build one showed it.
- **Proposed:** a stage is a timeline of squads and nothing else: length, whether to wait for a clear, and a list of
  "squad at time". Allowed enemies come from that list. Pace is *measured* and drawn by the debugger, not typed in.
- **Recording:** needed before anything changes: the current Stage inspector (all its fields) next to a level playing, as
  the "before" of this simplification.

### 20. A director, not a hand-made timeline
2026-11-26 · **Requirement** · Owner. Under discussion, nothing built
- **Rejected:** the timeline proposed in 19. "Not something where I configure the formation, position of formation,
  number of enemies." The director should pick enemies, formations, positions and amounts by itself, in real time.
- **What the designer configures instead (owner's list):** a credit cost per enemy and a credit pool that refills; a
  weighted pool of enemies with on/off toggles and limits; how a wave or stage ends (timer, or all enemies dead); maybe a
  minimum and maximum amount. Spawn points: designer-placed or found at runtime, still an open question.
- **Owner's questions:** should an enemy's cost change per wave (cheaper Chasers at a peak, so more come)? What are
  weights and unlock toggles? Can spawn points or lanes be set by a designer, or found at runtime?
- **Note for the case study:** a refilling budget was the first idea (3), replaced by "threat on screen" (5), replaced
  by a hand-made timeline (19), and now the budget is back, from the owner's research into how shmup directors are built.
  Going round in a circle like this is part of the story: what changed is that each step was tried against a real level.
- **Recording:** still the "before": the current Stage inspector and Level_SpawnTest playing.

### 21. Spawn rules at runtime, fixed cost, a director that can save
2026-11-26 · **Accepted** · Owner (answers Claude's questions on 20)
- **Spawn positions:** found at runtime from rules relative to the player, not placed in the scene. Owner: most enemies
  spawn "little outside offscreen"; how close is configurable. Expensive enemies (a boss costing 50) spawn far away, so
  the player gets a heads-up.
- **Cost:** fixed per enemy type. Stages change the budget and the weights, not the cost. Owner asked which other
  approaches exist; Claude listed per-stage cost multipliers, a cost per formation, adaptive cost, no credits at all, and
  threat on screen (5).
- **Saving credits:** yes, and it should be a setting, along with how fast credits are earned. Calm-then-burst versus a
  steady trickle becomes something a stage tunes.
- **Recording:** still the "before" of 19 and 20.

### 22. Design version 4: a credit director
2026-11-27 · **Proposed** · Claude, written from the owner's requirements in 20 and 21. **Superseded by 23** (changed by the owner's answers)
- **Design:** the director earns credits, plans a group (enemy by weight, formation and count from the enemy's own list),
  saves until it can afford it, and spawns it at a place found from rules relative to the player.
- **Removed from the stage:** Role, Intensity curve, Allowed Enemies, Scripted Squads; the wave Roster; `SquadDefinition`.
  Replaced by credits (start, per second, cap), spending (group size, longest wait, gap), max alive and one enemy list.
- **Proposed by Claude, not asked for:** *All cleared* merged into *Timer, then all dead*; a "variety" dip after each pick;
  *Longest wait* so saving can't stall a stage; formations and spawn distance stored on the enemy.
- **Set aside:** the threat model (7) and arena regions (14), until playtests give evidence.
- **Archived:** version 3.1 in `history/design-v3.1.md`.
- **Recording:** needed before building starts: the current Stage inspector and Level_SpawnTest playing (the "before" of 19 to 22).

### 23. Cost per level, a director that reads the player, then build it
2026-11-27 · **Accepted** (all points below marked Owner) · Owner. **Built** (version 4.1) · code drafted by Claude
- **Spawn positions:** at runtime is right (Owner). Spawn distance belongs on the enemy, but not as one rigid setting: an
  enemy may enter on screen from above or below the arena and fly in, or off-screen and move toward the player (Owner).
  Formations as a whole will be worked through once the architecture is settled (Owner).
- **Cost:** fixed per enemy, but **per level**, so levels can share costs or differ (Owner). Built as an Enemy Cost Table
  asset a level points at (Claude).
- **No cost per formation** (Owner): a group's cost is count × enemy cost. Formations of the same size and enemy cost the
  same. The director picks a formation to challenge the player, not to spend. This rejects the "cost per formation" idea
  Claude listed for later in version 4.
- **Read the player** (Owner): if the player stays at the top, crowd the top to force them down, and the same for the
  bottom. Owner asked whether to crowd a lane from both sides at once. Proposed and built (Claude): three height lanes,
  a favourite lane from recent time, a *Lane focus* per stage, and no pincer (one side of that lane stays open) unless a
  stage allows it.
- **Doing well = more to destroy** (Owner): a threshold, such as a kill spree, raises the budget, "not difficulty". Built
  (Claude): a streak of recent kills, ended by a hit, multiplies credits per second.
- **Group size** (Owner's question: tune after testing, or adapt to the player?): kept as a hand-tuned slider for now
  (Claude). Adapting needs measurements of how the player copes (how fast enemies die after arriving, close calls, hits
  and deaths); those come first, then the rule.
- **Longest wait is not a cooldown** (Owner asked): renamed *Patience*; the gap between groups is the *Cooldown* (Claude).
- **Kept, not merged:** *All cleared* stays its own end mode (Claude, reversing a v4 proposal).
- **Built (2026-11-27):** `SpawnDirector`, `GroupPlanner`, `CreditWallet`, `VarietyMemory`, `WeightedPick`, `LaneTracker`,
  `StreakMeter` (Core); `EnemyCostTable`, `PlayerWatch`, formation and entry lists on `EnemyDefinition`, the new
  `StageDefinition`; the spawner places by lane, side, entry and spacing; the Level Debugger shows credits, the plan, lanes
  and the streak. Removed: scripted squads, `SquadDefinition`, Role, Intensity, Roster, Allowed Enemies.
- **Not built yet:** entries from above or below; adaptive group size; what happens to credits when the player dies.
- **Result:**
- **Recording:** the "before" was recorded by the owner (scripted squads, Stage inspector). Needed next: the first
  playtest of `Level_SpawnTest` with the Level Debugger open, showing the plan, the credits graph and the lane line while
  you deliberately stay in the top lane.

---

## Template

```
### N. Title
date · **Status** · Who
- **Problem:** what was wrong or needed
- **Options:** what was considered
- **Decision:** what was chosen, and why
- **Trade-off:** what it costs
- **Result:** what actually happened (measured or seen), once known
- **Recording:** file name, or what still needs recording
```
