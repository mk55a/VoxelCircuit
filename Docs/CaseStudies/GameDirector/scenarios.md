# Game Director: case study scenarios

Each scenario is one problem worth telling. They are filled in as the work happens. **Result stays empty until there is
something real to put there**: a measurement, a recording, or a playtest.

A good scenario answers: what did I need, what did I try first, why wasn't it enough, what did I do instead, how do I know it worked.

| # | Scenario | Decisions | State |
|---|---|---|---|
| A | Enemy count is not difficulty | 3, 5, 6, 7 | Designed |
| B | A level that can be reused but not memorised | 4, 5 | Designed |
| C | The wrong enemy in the wrong stage | 10 | Designed |
| D | From one curve to stages | 5, 10 | Designed |
| E | Testing minute 8 without playing 8 minutes | 11 | Designed |
| F | Adding a boss without breaking what works | 10 | Planned |
| G | Music that follows the fight without flickering | 12 | Planned |
| H | Making the player move without trapping them | 13, 14, 23 | Built, not tested |
| I | Designing less: from a timeline to a director | 17, 19, 20, 21, 22, 23 | Built, not tested |

---

## A. Enemy count is not difficulty
- **Need:** follow a designed intensity line.
- **First ideas:** a budget per second, then the total cost of living enemies.
- **Why not enough:** a formation you can shoot standing still and one that makes you move cost the same.
- **Then (v2, v3):** predicted threat per squad, and live pressure around the player.
- **Now (v4):** back to a budget: a fixed cost per enemy, credits per stage. Threat and pressure are set aside until a
  playtest shows the budget isn't enough.
- **Evidence to collect:** hits taken per formation; pressure when the player was hit, compared with the plain total cost. If
  pressure doesn't predict hits better than the total cost, the extra complexity wasn't worth it, and that is a finding too.
- **Result:**

## B. A level that can be reused but not memorised
- **Need:** "Level 1 Easy" is always the same level, yet enemies shouldn't be predictable.
- **Now:** fixed rosters, stages and curves; a seed for everything else.
- **Evidence to collect:** pressure curves of several seeds drawn together. Same shape, different details?
- **Result:**

## C. The wrong enemy in the wrong stage
- **Need:** enemies only where they belong, even when a level has been edited many times.
- **Now:** warnings while designing, and one gate at run time.
- **Evidence to collect:** a test that tries to spawn a forbidden enemy; mistakes the warnings caught.
- **Early sign (not the result):** tests show a stage-forbidden squad dropped while an allowed one spawns, and that ignoring
  the roster never ignores the enemy limits.
- **Result:**

## D. From one curve to stages
- **Need:** shape intensity without fighting a system.
- **First idea:** one curve with a repeating build-up / peak / relax cycle.
- **Why not enough:** peaks and rests were side effects of a setting, and couldn't be filled or tested on their own.
- **Then (v3):** each was a stage with a role, a curve and its own enemies.
- **Now (v4):** a stage is defined by its numbers (credits, saving, max alive, enemy list). The role and curve did nothing
  in gameplay and were removed; the debugger draws the pace that results instead.
- **Evidence to collect:** steps needed to add a rest after a peak, in the window.
- **Result:**

## E. Testing minute 8 without playing 8 minutes
- **Need:** reach any moment quickly, and test endings.
- **Now:** a Play mode debug window with jump, skip and finish.
- **Evidence to collect:** time to reach a late stage with and without it; a bug it helped find.
- **Result:**

## F. Adding a boss without breaking what works
- **Need:** a new wave type later.
- **Now:** waves create their own runner; the level only knows the interface.
- **Evidence to collect:** which existing files had to change when the boss was added. Goal: none of the standard wave's.
- **Early sign (not the result):** a test plays a wave type without stages next to a standard wave, with no change to the level.
- **Result:**

## G. Music that follows the fight without flickering
- **Need:** music rises and rests with the game.
- **Now:** planned only.
- **Evidence to collect:** number of music switches in a run with and without the two thresholds and the hold time.
- **Result:**

## I. Designing less: from a timeline to a director
- **Need:** set up a level quickly, and still get a different run each time.
- **First idea (step 3):** stages with allowed enemies and scripted squads at set times.
- **Why not enough:** setting up one level showed too many fields, some that did nothing (role, curve) and some that
  repeated each other (roster, allowed enemies, squad list). A timeline also makes every run the same.
- **Now (v4.1, built):** the designer sets credits, saving, limits and one weighted enemy list per stage, and a cost
  table per level; the director picks enemies, formations, counts, lanes and sides.
- **Evidence to collect:** fields to fill in and minutes to set up the same level before and after; several runs of one
  level drawn together in the debugger graph (same shape, different details?).
- **Result:**

## H. Making the player move without trapping them
- **Need:** stop the player resting in two lanes, but always leave a way out, and keep enemies readable.
- **First idea:** four regions around the cylinder.
- **Why not enough:** measured, each is more than two screens wide.
- **Then (v3.1):** a finer grid with an open route rule, planned only.
- **Now (v4.1):** three height lanes. The director crowds the lane the player uses most (Lane focus) and keeps one side of
  it open unless a stage allows pincers.
- **Evidence to collect:** a heatmap of player positions before and after; deaths where no route was open.
- **Result:**

---

## What to save along the way

- A short clip or screenshot **before** replacing something. It can't be recorded afterwards.
- Failed attempts and the reason they failed.
- Old design versions, in `history/`.
- Recorded sessions, once the recorder exists.
