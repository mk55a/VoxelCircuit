# VoxelCircuit Project Plan

## Current foundation

- Cylindrical arena, ship movement, boost, camera and shooting.
- Drifters and Chasers, including reusable Chaser formations and a test sandbox.
- Voxel destruction, kill pulses, bullet trails and an FPS overlay.

## Next: beautify the cylindrical world

Create a focused visual pass for the arena before building the survival director.

1. Develop a small representative section of the cylinder: a dark surface, subtle panel details and restrained emissive accents.
2. Playtest it with the ship, enemies, bullets and effects to check readability and the sense of movement around the ring.
3. Once the style works, extend it around the arena and add a few repeating structures or lights for depth.
4. Explore a subdued background and atmospheric lighting without distracting from gameplay.
5. Check performance with crowded formations and effects, using the FPS overlay and Unity Profiler as needed.

Keep the art direction and amount of detail open to iteration; do not build the entire arena before testing the first section.

## After the arena pass

- Continue the custom shockwave ring shader, currently described as in progress in the README.
- Build a small survival director that automatically selects formations using a budget, an enemy cap and cooldowns. Expand its pacing and rules incrementally toward a varied 10-minute survival mode.
- Develop the planned Gunner enemy, player death explosion, off-screen enemy indicators, hit-stop, screen shake and a playable build as the game needs them.

## Development approach

- Apply SOLID principles and practical game-design patterns without adding unnecessary complexity.
- Keep gameplay maths and state testable in `Game.Core`, Unity components in `Game.Runtime`, and authoring tools in `Game.Editor`.
- Tune gameplay and visuals through reusable Inspector-configurable assets where appropriate.
- Reuse pooled objects or shared particle systems for repeated effects and spawns; measure performance under representative loads.
- Keep visuals readable during gameplay and preserve existing scene and prefab configurations when making changes.
