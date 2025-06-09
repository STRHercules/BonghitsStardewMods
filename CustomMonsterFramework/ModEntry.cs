using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using StardewValley.Locations;
using HarmonyLib;

namespace CustomMonsterFramework
{
    public enum MonsterBehavior
    {
        Normal,
        FastLowHealth,
        SlowHighHealth,
        Kamikaze,
        Stalking,
        Passive,
        Grouping,
        Cloaking,
        Duplicating,
        Explosive
    }

    internal class SpawnRule
    {
        public string Location { get; set; } = "";
        public int StartTime { get; set; } = 0;
        public int EndTime { get; set; } = 2600;
        public double Chance { get; set; } = 1.0;
    }

    public class ModEntry : Mod
    {
        private readonly List<CustomMonsterData> customMonsters = new();
        private Harmony harmony;

        public override void Entry(IModHelper helper)
        {
            harmony = new Harmony(ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(MineShaft), nameof(MineShaft.getMonsterForThisLevel)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(After_GetMonsterForThisLevel))
            );

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            LoadCustomMonsters();
        }

        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            GameLocation location = Game1.player.currentLocation;
            foreach (var data in customMonsters)
                data.TrySpawn(location, e.NewTime);
        }

        private void LoadCustomMonsters()
        {
            var data = Game1.content.Load<Dictionary<string, string>>("Data/Monsters");
            foreach (var pair in data)
            {
                if (pair.Key.StartsWith("custom_", StringComparison.OrdinalIgnoreCase))
                {
                    customMonsters.Add(new CustomMonsterData(pair.Key, pair.Value));
                }
            }
        }

        public static void After_GetMonsterForThisLevel(ref Monster __result, int level, int xTile, int yTile)
        {
            if (__result != null)
                return; // vanilla already spawned something

            var mod = ModEntry.Instance;
            foreach (var data in mod.customMonsters)
            {
                if (data.CanSpawnHere(level))
                {
                    __result = data.CreateMonster(xTile, yTile);
                    return;
                }
            }
        }

        internal static ModEntry Instance { get; private set; }

        public ModEntry() => Instance = this;
    }

    internal class CustomMonsterData
    {
        private readonly string rawData;
        public string Name { get; }
        public string TextureName { get; }
        private readonly List<SpawnRule> spawnRules = new();
        public MonsterBehavior Behavior { get; }

        public CustomMonsterData(string name, string data)
        {
            Name = name;
            rawData = data;
            var fields = data.Split('/');
            if (fields.Length > Monster.index_displayName + 1)
                TextureName = fields[Monster.index_displayName + 1];
            if (string.IsNullOrWhiteSpace(TextureName))
                TextureName = $"Mods/{ModEntry.Instance.ModManifest.UniqueID}/Monsters/{name}";

            if (fields.Length > Monster.index_displayName + 2)
            {
                string rules = fields[Monster.index_displayName + 2];
                foreach (var part in rules.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var pieces = part.Split('|');
                    if (pieces.Length >= 1)
                    {
                        SpawnRule rule = new();
                        rule.Location = pieces[0];
                        if (pieces.Length >= 2)
                        {
                            var times = pieces[1].Split('-');
                            if (times.Length == 2 && int.TryParse(times[0], out int s) && int.TryParse(times[1], out int e))
                            {
                                rule.StartTime = s;
                                rule.EndTime = e;
                            }
                        }
                        if (pieces.Length >= 3 && double.TryParse(pieces[2], out double chance))
                            rule.Chance = chance;
                        spawnRules.Add(rule);
                    }
                }
            }

            if (fields.Length > Monster.index_displayName + 3)
            {
                Enum.TryParse(fields[Monster.index_displayName + 3], true, out MonsterBehavior parsed);
                Behavior = parsed;
            }
            else
            {
                Behavior = MonsterBehavior.Normal;
            }
        }

        public bool CanSpawnHere(int level) => true;

        public void TrySpawn(GameLocation location, int time)
        {
            foreach (var rule in spawnRules)
            {
                if (!string.Equals(rule.Location, location.Name, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (time < rule.StartTime || time > rule.EndTime)
                    continue;
                if (Game1.random.NextDouble() > rule.Chance)
                    continue;

                Vector2 tile = Utility.getRandomPositionInThisRectangle(new Microsoft.Xna.Framework.Rectangle(0, 0, location.Map.Layers[0].LayerWidth, location.Map.Layers[0].LayerHeight), Game1.random);
                Monster monster = CreateMonster((int)tile.X, (int)tile.Y);
                location.addCharacter(monster);
                break;
            }
        }

        public Monster CreateMonster(int xTile, int yTile)
        {
            Vector2 tile = new(xTile, yTile);
            GenericMonster monster = new(Name, tile * 64f, rawData, TextureName, Behavior);
            return monster;
        }
    }

    internal class GenericMonster : Monster
    {
        private readonly string rawData;
        private readonly string textureName;
        private readonly MonsterBehavior behavior;

        public GenericMonster(string name, Vector2 position, string rawData, string textureName, MonsterBehavior behavior)
            : base(name, position)
        {
            this.rawData = rawData;
            this.textureName = textureName;
            this.behavior = behavior;
            parseMonsterInfo(name);

            switch (behavior)
            {
                case MonsterBehavior.FastLowHealth:
                    Health /= 2;
                    MaxHealth = Health;
                    speed *= 2;
                    break;
                case MonsterBehavior.SlowHighHealth:
                    Health *= 2;
                    MaxHealth = Health;
                    speed = Math.Max(1, speed / 2);
                    break;
            }
        }

        public override void behaviorAtGameTick(GameTime time)
        {
            switch (behavior)
            {
                case MonsterBehavior.Passive:
                    break;
                case MonsterBehavior.Stalking:
                    moveTowardPlayer(1);
                    break;
                default:
                    base.behaviorAtGameTick(time);
                    break;
            }

            if (behavior == MonsterBehavior.Cloaking && Game1.random.NextDouble() < 0.01)
                IsInvisible = !IsInvisible;

            if (behavior == MonsterBehavior.Kamikaze && withinPlayerThreshold(1))
            {
                currentLocation.explode(Tile, 2, Game1.player);
                Health = -1;
                deathAnimation();
            }
        }

        public override void deathAnimation()
        {
            if (behavior == MonsterBehavior.Duplicating)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 t = Tile + new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
                    currentLocation.addCharacter(new GenericMonster(Name, t * 64f, rawData, textureName, behavior));
                }
            }
            if (behavior == MonsterBehavior.Explosive)
            {
                currentLocation.explode(Tile, 2, Game1.player);
            }
            base.deathAnimation();
        }

        public override void reloadSprite(bool onlyAppearance = false)
        {
            Sprite = new AnimatedSprite(textureName);
        }
    }
}
