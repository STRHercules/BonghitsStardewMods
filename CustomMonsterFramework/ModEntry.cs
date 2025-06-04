using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using StardewValley.Locations;
using HarmonyLib;

namespace CustomMonsterFramework
{
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
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            LoadCustomMonsters();
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

        public CustomMonsterData(string name, string data)
        {
            Name = name;
            rawData = data;
            var fields = data.Split('/');
            if (fields.Length > Monster.index_displayName + 1)
                TextureName = fields[Monster.index_displayName + 1];
            if (string.IsNullOrWhiteSpace(TextureName))
                TextureName = $"Mods/{ModEntry.Instance.ModManifest.UniqueID}/Monsters/{name}";
        }

        public bool CanSpawnHere(int level)
        {
            // simple example: check for spawn range in custom fields
            return true;
        }

        public Monster CreateMonster(int xTile, int yTile)
        {
            Vector2 tile = new(xTile, yTile);
            GenericMonster monster = new(Name, tile * 64f, rawData, TextureName);
            return monster;
        }
    }

    internal class GenericMonster : Monster
    {
        private readonly string rawData;
        private readonly string textureName;

        public GenericMonster(string name, Vector2 position, string rawData, string textureName)
            : base(name, position)
        {
            this.rawData = rawData;
            this.textureName = textureName;
            parseMonsterInfo(name);
        }

        public override void reloadSprite(bool onlyAppearance = false)
        {
            Sprite = new AnimatedSprite(textureName);
        }
    }
}
