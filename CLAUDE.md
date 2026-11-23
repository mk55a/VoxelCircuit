# VoxelCircuit: project guide for Claude

Read this before changing anything. It holds the project's rules, architecture, art direction, decisions made so far
and the backlog. Keep it up to date when a rule or decision changes.

---

## 1. The project

**VoxelCircuit** is a side-view arena shooter on a wrap-around cylinder, made in **Unity 6 (6000.3.26f1), URP 17.3, C#**.
Ships and enemies are built from cubes (voxels) and break back into cubes when destroyed.

- Public GitHub portfolio project: `github.com/mk55a/VoxelCircuit`. Progress is pushed after each finished feature.
  The README is written in the owner's own voice (plain language, no sales tone, short sections). Don't rewrite it
  into marketing copy; change only what is asked.
- Inspired by Resogun (played two-player). **Not a clone**: the goal is that core feeling of a skill you build through
  repeated play, unpredictable enemies, intensity that rises and falls, and a real sense of reward. Nothing is copied
  from Resogun or any other game: no names, assets, layouts or designs.
- The owner designs the game and its systems, sets the architecture and code standards, and reviews, tests and
  integrates every change. A big goal is building their own tools for tuning and testing movement, attacks,
  animation, VFX and environment: everything from bare bones.

### Scope (where it is heading)

- A **10-minute survival mode**: a spawn director sends enemies in waves of rising and falling intensity, chosen by
  rules and a budget (enemy cap, cooldowns), so every run plays differently.
- **Several enemy types** built from shared, reusable components, flying in **formations**. Expect up to
  **100-150 enemies alive at once**, of mixed types.
- Player special move: the **shockwave** (built; the charge/build-up meter that unlocks it comes later).
- Planned: Gunner enemy (first multi-hit enemy), player death explosion (fire-like, with a shockwave ring),
  off-screen enemy indicators, hit-stop and screen shake, a playable Windows build.
- Long-term: the transparent arena glass doubles as a curved in-game canvas for messages (level started, failed),
  probably via a RenderTexture.

---

## 2. Rules (always follow)

### Code
1. **SOLID principles, game design patterns and clean code.** Systems must be flexible and reusable: one system for
   everything of a kind (e.g. one debris effect for everything destructible), not one-offs.
2. **Never create UI or objects at runtime.** No `new GameObject`, `AddComponent` or `Instantiate` of ad-hoc objects
   while the game runs. Use **prefabs** and **ScriptableObjects**. (Pools instantiating their own prefab up front are
   fine. Plain C# data objects such as a `MaterialPropertyBlock` are fine.)
   Editor-time tools (menu items that build or configure things once and save them) are fine.
3. Gameplay maths and state go in **`Game.Core`**: pure C#, no MonoBehaviours, unit tested.
   Unity components go in **`Game.Runtime`**. URP and render-pipeline code goes in **`Game.Rendering`**. Editor tools go in **`Game.Editor`**.
4. **Depend on interfaces, not concrete classes** (for example `IThrusterSource`, `IShockwaveEmitter`, `IFireInput`).
   Interface references in the Inspector use a `MonoBehaviour` field plus an `as` cast in `Awake`. Add an
   `OnValidate` that swaps in the right component when a GameObject is dragged in: dragging an object picks its
   *first* script, which caused real setup bugs.
5. **Tuning lives in ScriptableObjects** (`*Tuning`, `*Profile`, `CylinderConfig`) with `[Tooltip]`s that explain
   units and effect. No magic numbers in components.
6. **Validate in `Awake`.** On a missing or invalid reference, `Debug.LogError(..., this)` naming the exact field and
   what to put in it, then `enabled = false`.
7. **Shared services** are registered by **`GameInstaller`** (the composition root, execution order -100) in the
   **`ServiceLocator`**. Consumers resolve them in `Awake` (or `Start`). Optional services are optional fields on `GameInstaller`.
8. **Events (Observer pattern)** for reactions: for example `IDestructible.Destroyed` and the emitter's
   `Triggered`/`Released`/`Finished` events. Effects listen; the thing that is destroyed or fired does not know about them.
9. **No per-frame garbage.** No LINQ, allocations, closures or string building in `Update` on hot paths.
   Pool repeated spawns. Iterate registries backwards when an item can remove itself while you loop.
10. **Swept (continuous) hit tests for anything fast:** segment-vs-sphere for bullets, swept band for the shockwave.
    Never test only the end position.
11. **Frame-rate independent smoothing:** `1 - exp(-rate * dt)` or an exponential approach, never a raw `Lerp(a, b, k)`.
12. **Tests:** EditMode tests for every Core class (`Assets/Tests/EditMode`, assembly `Game.Tests`), PlayMode where
    behaviour needs the engine (`Game.PlayModeTests`). Update existing tests and stubs when an interface changes.
13. Code style: `sealed` classes; `private` serialized fields with `_camelCase`; read-only properties; XML `<summary>`
    on public types explaining *why*. Namespaces under `CylinderShooter.*`. **LF line endings, no BOM** (match the existing files).
14. **Preserve existing scene, prefab and asset configuration.** Prefer adding optional fields over renaming or
    removing serialized ones. Removing a field breaks the owner's Inspector setup and means redoing it by hand. When a
    rename is unavoidable, say so and give the re-setup steps.

### Performance
- **The game must hold 120 FPS** (8.3 ms per frame). No feature may cause frame drops. Recent measurements: about
  2.5-3.5 ms per frame. Profile **builds** (Development Build + Autoconnect Profiler), not Play Mode: the editor adds
  `EditorLoop` noise.
- Effects cost nothing when idle (for example the heat haze turns the camera's opaque texture on only while a wave runs).
- **No shadows** on player, enemies, bullets, debris or effects. The city is only a backdrop. Shadows are off in the
  URP asset. SSAO was removed, Opaque Texture is off by default, MSAA is off or low, Forward+ is on.
- Watch transparent overdraw (glass ring, two cloud rings, effects) and big particle bursts (kill bursts).
- Don't suggest ECS/DOTS: the enemy counts don't need it. The order is: profile, fix the algorithm, fix the content,
  remove spikes, then Jobs/Burst if still needed.

### Working with the owner
- Explain in **simple language**. Technical terms are fine, but explain them in brackets. Be thorough and in depth;
  the owner is learning and writes things up in their own words.
- **Setup steps must be click-by-click**: exact menu paths, which object gets which component, what goes in each field.
  Unity UI steps that sound simple (gradient editors, object pickers for meshes inside `.obj` files) have caused confusion.
  Offer an editor tool when a setup is long.
- Before a big visual change, show options (mock-ups) and let the owner choose.
- **Copyright:** don't recreate existing ships, characters, logos or designs (for example the Swordfish II or the
  Endurance were declined); design original ones instead.

---

## 3. Architecture

### Assemblies and folders (`Assets/Scripts`)
| Assembly | Folder | Holds | References |
|---|---|---|---|
| `Game.Core` | `Scripts/Core` | Pure logic: `CylinderSpace`, `CylinderPosition`, `ShipMotor`, `ShipFacing`, `BoostAbility`, `ChaserSteering`, `CylinderContact`, `Overlap`, `ObjectPool<T>`, `ServiceLocator`, `Targets` (`IDamageable`, `ITargetRegistry`), `PlayerLifeState`, `FireRateLimiter`, `FireSideSelector`, `DebrisMath`, `PulseMath`, `ThrusterMath`/`ThrusterLevel`/`ExhaustState`, `ShockwaveFront`/`IShockwaveSettings`, `CylinderMeshBuilder` | UnityEngine (maths types) |
| `Game.Runtime` (root namespace `CylinderShooter`) | `Scripts/Runtime` | Components: `Ship/*`, `Combat/*` (incl. `Shockwave/*`), `Enemies/*`, `Formations/*`, `Effects/*` (incl. `Exhaust/*`), `Cam/*`, `Arena/*`, `City/*`, `CylinderConfig`, FPS overlay | Game.Core, InputSystem |
| `Game.Rendering` | `Scripts/Rendering` | URP renderer features and screen effects: `Outline/*`, `Shockwave/ShockwaveScreenEffects` | Game.Core, Game.Runtime, URP Core/Universal runtime |
| `Game.Editor` | `Scripts/Editor` | Menu builders (scene/arena/environment builders, City generator editor) | |
| `Game.Tests` / `Game.PlayModeTests` | `Assets/Tests/EditMode`, `/PlayMode` | Tests | |

Dependency direction: Core ← Runtime ← Rendering. Core never references Unity components. Runtime never references URP.

### Cylinder space (the core idea)
- Every object stores a **`CylinderPosition`** (angle in radians, always wrapped, plus height). World:
  `(r·sin a, h, r·cos a)`. **Screen-right is `-Tangent(angle)`** (decreasing angle). Outward points toward the camera.
- Wrap-around needs no teleporting. Use `CylinderSpace.ShortestDelta` for anything across the seam.
- `CylinderConfig.asset`: radius **60**, heights **-6..6**. The camera is `CylinderFollowCamera`: outside the ring,
  distance 22, height follow, look-ahead.
- **Side-on rotation:** the ship uses `LookRotation(forward, Vector3.up)`. Chasers, bullets and the shockwave use
  `up = Cross(direction, Outward)` or an equivalent, so the model's side faces the camera. The models are made to be seen from the side.

### Systems in place
- **Ship:** `ShipController` (fixed-step motor, interpolated render position, implements `ICameraTarget`,
  `IAimSource`, `IThrusterSource`), `ShipBanking` (model roll), `PlayerLife`/`PlayerLifeState` (lives, respawn,
  invulnerability incl. `GrantInvulnerability`), `PlayerContact` (swept contact with `IContactHazard`s, LateUpdate order 100).
- **Input:** `InputSystemShipInput` (order -100) implements `IShipInput`, `IFireInput`, `IShockwaveInput`. Bindings are created in code.
- **Combat:** `MainCannon` (`IWeapon.TryFire(direction)`), `Bullet` (swept `Overlap.SegmentSphere`, TrailRenderer),
  `BulletPool` (`ObjectPool<Bullet>`), `WeaponTuning`, `TargetRegistry`.
- **Fire controls:** left mouse / J / LT fires **screen-left**, right mouse / L / RT fires **screen-right**, one side at
  a time (the most recent press wins), **independent of where the ship faces**. Bullets inherit ship velocity only in the travel direction.
- **Shockwave** (player special move, key **K** / E / middle mouse / gamepad Y):
  - `ShockwaveFront` (Core): **build-up** (`FormDuration` 0.5 s; grows from a ship-sized "( )" ring at the player to
    full cylinder height, already killing), then **release** (both halves thrown outward with ease-out until they meet
    on the far side). The kill band is swept just behind the front, so enemies appearing where it already passed survive.
    Only active registered targets are hit, so enemies still spawning will be ignored once spawn-in registers them only when active.
  - `ShockwaveEmitter` (scene object, registered via `GameInstaller` as `IShockwaveEmitter`): places the two wave strips
    and sets `_Fade`/`_CurveDepth`/`_Intensity` on every renderer under them (glow + heat-haze child).
  - `PlayerShockwave` on the ship: triggers it and grants about 0.6 s invulnerability, so it always saves you from an imminent hit.
    The future charge meter gates it here.
  - `ShockwaveScreenEffects` (Rendering): blends authored Volumes over time (a "Charge" dim/vignette/chromatic
    aberration from the trigger, a "Release" white-out/bloom/lens thump from the release) and switches the camera's
    opaque texture on only during a wave (for the heat haze).
  - `ShockwaveProfile` SO: build-up, travel, thickness, damage, curve, glow-over-time curve, visuals.
- **Enemies:** `Chaser` (steering, formation slot, voxel spin), `Drifter`. Both implement `IDamageable`,
  `IContactHazard` and `IDestructible`; they deactivate on death (pool-friendly).
- **Formations:** `FormationDefinition` (SO), `FormationMotion`, `FormationTestSpawner` (debug sandbox: **F** spawns, **R**
  clears; Chaser-only pool inside it; spawns hidden off-screen or behind the arena, with `_hiddenBeyondDistance` for the glass arena).
- **Destruction:** `IDestructible.Destroyed(DestructionEvent)` → `ShatterOnDestroyed` → `IDebrisEmitter` (`VoxelDebris`:
  one shared world-space particle system, cube mesh, floor-plane collision, `DebrisProfile` SO, `VoxelShape` reads
  `*_Voxels.json`). `PulseOnDestroyed` → `IPulseEmitter` (`PulseEmitter`: flash, ring, sparks; `PulseProfile`).
- **Exhaust:** `ThrusterExhaust` reads any `IThrusterSource`. Thrust level 0 = idle, 1 = cruise, 2 = boost; above cruise
  speed it follows leftover boost speed. Rise and fall rates differ. `ExhaustProfile` SO; flame = stepped voxel mesh +
  `VoxelCircuit/Exhaust` shader (`_Intensity` via MaterialPropertyBlock); optional point light. No particles (the owner removed sparks).
- **Arena:** `CylinderBand` ([ExecuteAlways] procedural wall/ring meshes; `_faceInward` for rings seen from inside),
  glass arena ring (`ArenaGlassBuilder`), test section builder.
- **City:** modular voxel kit (`Art/City`: bases, shafts, setback, crowns; 0.5 m voxels, 4 m grid, 4 m height steps,
  one shared `City` material/palette + `City_Emission` for lit windows), `CityGenerator` + editor.
- **Environment:** `Sky_LowCloud` panoramic skybox, two inward cloud rings (`EnvironmentBuilder` menu,
  `VoxelCircuit/Cloud Band` shader: unlit, no fog, UV drift), linear fog, warm street lights, moonlight parented to the camera.

### Rendering and shaders (`Assets/Art/Shaders`, `Art/Environment`)
- `VoxelCircuit/Gameplay Toon`: **the shared look for ship and all enemies**. Two flat light bands from the main light
  with a **minimum brightness**, team-coloured **rim (fresnel)**, optional emission, **no specular, no reflections, no
  shadow caster**, optional fog. Its `GameplayOutlineMask` pass feeds the outline.
- **Outline:** `GameplayOutlineFeature` (renderer feature on `PC_Renderer`, Render Graph API): mask pass on the
  **Gameplay** layer, depth-tested against the scene, then a full-screen composite (`VoxelCircuit/Internal/Gameplay
  Outline Composite`, `Thickness` in px). The colour comes from each material's `_OutlineColor`.
- `VoxelCircuit/Exhaust`, `VoxelCircuit/Shockwave` (+ `ShockwaveCommon.hlsl`), `VoxelCircuit/Shockwave Haze`
  (samples `_CameraOpaqueTexture`, so it only bends opaque objects), `VoxelCircuit/Cloud Band`.
- Post: Global Volume with Bloom (threshold about 1.3, low intensity: only deliberately bright things bloom), Tonemapping,
  Color Adjustments, Vignette.

---

## 4. Art direction

- **Voxel** ships, enemies and city. A cyberpunk night city inside a **transparent glass cylinder**; the ship flies on
  its border. Building bases are the floor where debris lands.
- **Mood: "low cloud, city glow."** Heavy overcast lit from below by the city, warm purple haze, no stars.
  Buildings stay **grey**, lit by scene lights; about 12-25% of windows are lit warm amber. Street lights are warm
  (`#FF8A4C` / `#FFB35C`). **No violet or magenta lights**: that is the enemies' colour.
- **Colour language:** player = **cyan/blue** (ship `Kestrel`, bullets, exhaust, shockwave). Enemies = palette C,
  **hot magenta** (`#D62478` body, `#FF8028` secondary, `#343246` structure, `#FFDC78` accent). Gameplay must always
  be the clearest thing on screen.
- **Ship look:** stands out without glow or shiny reflections (a metallic look comes from the palette, not specular).
  Glow comes only lightly from the exhaust and grows on boost.
- **Voxel style everywhere:** stepped and quantized gradients, blocky clouds, blocky plasma cells, cube debris.
- **Rejected for now:** bullet impact bursts on every hit (too heavy), recoil, muzzle flash, exhaust spark particles.
  Hit feedback for tougher enemies goes **on the enemy** (a hit-flash component listening to damage), not the bullet.

### Art pipeline
- Voxel models are generated by **Python scripts** (outputs: OBJ with hidden faces removed and **X negated**, because
  Unity's OBJ importer flips X; a palette PNG (8 px per slot) and an emission PNG; an editable `.vox` in `ArtSource/`;
  `*_Voxels.json` per-cube data for debris). Pivot: bounding-box centre for ships, bottom centre for city pieces.
- `Tools/CityKit/city_kit.py` (city kit), `Tools/ArenaTextures/gen_arena_textures.py` (arena textures).
- Palette and emission textures import as **Point filter, no compression, no mipmaps**.
- Materials embedded in `.obj` imports are read-only: extract one material and remap the others to it.

---

## 5. Decisions log (keep these)
- Side-view game: models face the camera side-on; banking shows top and bottom.
- Bullet tunnelling was fixed with swept segment-vs-sphere tests. Trail length × fire rate decides whether trails merge (Time about 0.03).
- Glass arena: the spawner counts far points as hidden (`_hiddenBeyondDistance` ≈ 102), because an opaque arena is no longer there to hide spawns.
- Shading option **D** for all gameplay objects: toon bands + minimum brightness + rim + outline.
- Heat haze, not lens distortion, is the shockwave's main distortion (a small lens thump at release is optional).
- Shockwave: weighty 0.5 s build-up, but the attack and kill zone exist from frame one and the player is invulnerable
  during it. The player keeps control while it builds.
- Moonlight is parented to the camera so lighting is consistent all around the ring.

## 6. Backlog (in rough order)
1. **Formation spawn-in** (decided): a formation's enemies spawn **one at a time, visibly**, each holding its slot
   until the whole formation has spawned (**under 2 s total**), then the formation moves and all members activate
   together. Spawning enemies are harmless and **not registered as targets** until active. A spawn animation comes later as polish.
   Spawning on screen also removes the brute-force hidden-spawn search in `FormationTestSpawner`. Needs a safe distance from the player.
2. **Shared `EnemyPool` service** (`ObjectPool<T>` per prefab, prewarmed to the director's caps per type, registered
   in `GameInstaller`, `IPoolable` spawn/return hooks). Move the Chaser pool out of `FormationTestSpawner`
   (that file is editor/dev-build only, so a release build has no enemy pool today). Pool Drifters too.
3. **Survival director:** budget, enemy cap, cooldowns, rising and falling intensity, a 10-minute run. One spawn-in at a time.
4. **Effects budget:** a scene-wide cap on live debris pieces (thin out per kill when busy), a per-frame pulse cap. Kill bursts are the main frame-time risk.
5. Gunner enemy (multi-hit) + on-enemy hit flash; enemy bullets pooled.
6. Shockwave **charge meter** (build-up over time; gates `PlayerShockwave`).
7. Player death explosion (fire-like, with a shockwave ring), off-screen indicators, hit-stop and screen shake, sound.
8. Bullet feel: revisit later (option "A voxel slug" was the recommendation; parked by the owner).
9. A playable Windows build locked at 120 FPS (`Application.targetFrameRate = 120` or VSync on a 120 Hz display).

## 7. Known cleanup (rule violations and housekeeping)
- `FormationTestSpawner` creates "Inactive Chaser Pool" and "Formation Occlusion Probe" GameObjects at runtime, and
  `FpsDebugOverlay` creates its own GameObject. Convert them to prefab or scene objects.
- `Assets/TutorialInfo` and `Assets/Readme.asset` (URP template leftovers) make GitHub report "Wolfram Language"; delete them.
- Loose assets at the `Assets/` root (`Ship.prefab`, `Debris_*.asset`, `Kestral_Exhaust.asset`,
  `GameplayOutline_Composite.mat`, the `mATERIALS` folder, `SpaceFighter.fbx`) are waiting for the **file-organisation
  rules the owner will give**. Ask for those rules before moving files, and move with Unity (or keep `.meta` files with
  their assets) so references survive.
- `.idea/` was committed in the first commit; it is now in `.gitignore`. Untrack it if it still shows up.
- `replay_pid*.log` files in the project root are crash logs and can be deleted.

## 8. Controls (current)
WASD / arrows move · Space boost · Left mouse / J fire left · Right mouse / L fire right · K / E / middle mouse shockwave ·
Gamepad: left stick, A boost, LT/RT fire, Y shockwave · Sandbox: F spawn formation, R clear.

## 9. Scenes
`Assets/Scenes`: **LookTest** (current visual and feature test scene), **PlayArena**, **LevelTest**.
