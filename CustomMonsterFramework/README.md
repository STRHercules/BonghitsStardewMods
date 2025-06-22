# Custom Monster Framework

This mod lets you define new monsters through Content Patcher and spawn them in the mines.

## Usage
1. Install SMAPI 4.0+ and Content Patcher 2.4+ for Stardew Valley 1.6.
2. Build this mod (see `ModEntry.cs`) and place the compiled DLL alongside `manifest.json`.
3. Create a Content Patcher pack which edits `Data/Monsters` to add entries and loads custom textures. You can copy the sample `assets/content.json`.
4. Each custom monster should have a unique key prefixed with `custom_`. The value uses the same 14-field format as vanilla monsters. You can then add extra fields:
   - **Field 16**: texture asset name for the monster sprite.
   - **Field 17**: spawn rules in the format `Location|start-end|chance` separated by `;` for multiple rules (times use 24h game time like `1800`).
   - **Field 18**: optional behavior name (`FastLowHealth`, `SlowHighHealth`, `Kamikaze`, `Stalking`, `Passive`, `Grouping`, `Cloaking`, `Duplicating`, or `Explosive`).
5. Provide the PNG art using a `Load` patch at the texture asset name. See the example below.
6. Launch the game with SMAPI. Monsters will spawn in the specified locations and times with the chosen behavior.

## Data/Monsters field order
The game parses monster data with the following fields:
1. health
2. damage to farmer
3. (unused)
4. (unused)
5. is glider
6. (unused)
7. drops
8. resilience
9. jitteriness
10. distance threshold to move towards player
11. speed
12. miss chance
13. is mine monster
14. experience points
15. display name

These correspond to indices defined in `Monster.cs` and parsed in `parseMonsterInfo`.

## Example
The included `assets/content.json` adds a simple monster named `Glowing Slime`.
It patches `Data/Monsters` and provides a custom texture under
`Mods/Bonghits.CustomMonsterFramework/Monsters/custom_glowingSlime`.
The entry also demonstrates a spawn rule (`Farm|1800-2600|0.5`) and assigns the
`FastLowHealth` behavior.
