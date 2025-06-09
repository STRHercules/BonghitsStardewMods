# ThatKaingKai's Stardew Valley Mod Collection

### [My Current Mod Plans](https://github.com/STRHercules/BonghitsStardewMods/blob/main/Mod%20Plans.md)


### Background
This collection of mods are authored by myself, mostly for my own playthroughs, problem solving and closing gaps.

I started with frustration over Stardew Valley Expanded's custom Bears in the Premium Barn. They acted similarly to truffle pigs, but once you get the honey from them that was it. I was dissapointed in the lack of artisan good for such unique honey, so I decided to make it!

The itch to tinker and 'fix' crept up on me, now we're here!

# Completed Mod Directory

### GitHub Directory

- [Artisanal Honey Mead](https://github.com/STRHercules/BonghitsStardewMods/releases/tag/BearHoney)

- [Bonghits' Weapon and Magic Expansion](https://github.com/STRHercules/BonghitsStardewMods/releases/tag/WeaponPack)

- [Woods Elsewhere Unofficial Update](https://github.com/STRHercules/BonghitsStardewMods/releases/tag/WoodsElsewhere)

- [High-Tech Backpacks](https://github.com/STRHercules/BonghitsStardewMods/tree/main/%5BCP%5D%20High-Tech%20Backpacks)


### Nexus Directory

- [Artisanal Honey Mead](https://www.nexusmods.com/stardewvalley/mods/31786)

- [Bonghits' Weapon and Magic Expansion](https://www.nexusmods.com/stardewvalley/mods/32082)

- [Woods Elsewhere Unofficial Update](https://www.nexusmods.com/stardewvalley/mods/31801)

- [High-Tech Backpacks](https://www.nexusmods.com/stardewvalley/mods/32165)

# Work In Progress Mods

- [Arborist's Catalogue](https://github.com/STRHercules/BonghitsStardewMods/tree/main/%5BCP%5D%20Arborist's%20Catalogue) (Converting all trees and bushes to furniture + catalogue)

## Building

To compile the C# mods, set the `GamePath` MSBuild property to your Stardew Valley install directory and run:

```bash
dotnet build CustomMonsterFramework/CustomMonsterFramework.csproj
```

The resulting `CustomMonsterFramework.dll` will be placed next to the mod's `manifest.json`.
