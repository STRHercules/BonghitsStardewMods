using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace StardewValley.Locations;

public class VolcanoDungeon : IslandLocation
{
	public enum TileNeighbors
	{
		N = 1,
		S = 2,
		E = 4,
		W = 8,
		NW = 0x10,
		NE = 0x20
	}

	private const int coalIndexPlaceholder = 1095382;

	private const string coalIndexPlaceholderString = "1095382";

	/// <summary>The main tile sheet ID for volcano dungeon tiles.</summary>
	public const string MainTileSheetId = "dungeon";

	public NetInt level = new NetInt();

	public NetEvent1Field<Point, NetPoint> coolLavaEvent = new NetEvent1Field<Point, NetPoint>();

	/// <summary>The volcano dungeon levels which are currently loaded and ready.</summary>
	/// <remarks>When removing a location from this list, code should call <see cref="M:StardewValley.Locations.VolcanoDungeon.OnRemoved" /> since it won't be called automatically.</remarks>
	public static List<VolcanoDungeon> activeLevels = new List<VolcanoDungeon>();

	public NetVector2Dictionary<bool, NetBool> cooledLavaTiles = new NetVector2Dictionary<bool, NetBool>();

	public Dictionary<Vector2, Point> localCooledLavaTiles = new Dictionary<Vector2, Point>();

	public HashSet<Point> dirtTiles = new HashSet<Point>();

	public NetInt generationSeed = new NetInt();

	public NetInt layoutIndex = new NetInt();

	public Random generationRandom;

	private LocalizedContentManager mapContent;

	[XmlIgnore]
	public int mapWidth;

	[XmlIgnore]
	public int mapHeight;

	public const int WALL_HEIGHT = 4;

	public Layer backLayer;

	public Layer buildingsLayer;

	public Layer frontLayer;

	public Layer alwaysFrontLayer;

	[XmlIgnore]
	public Point? startPosition;

	[XmlIgnore]
	public Point? endPosition;

	public const int LAYOUT_WIDTH = 64;

	public const int LAYOUT_HEIGHT = 64;

	[XmlIgnore]
	public Texture2D mapBaseTilesheet;

	public static List<Microsoft.Xna.Framework.Rectangle> setPieceAreas = new List<Microsoft.Xna.Framework.Rectangle>();

	protected static Dictionary<int, Point> _blobIndexLookup = null;

	protected static Dictionary<int, Point> _lavaBlobIndexLookup = null;

	protected bool generated;

	protected static Point shortcutOutPosition = new Point(29, 34);

	[XmlIgnore]
	protected NetBool shortcutOutUnlocked = new NetBool(value: false)
	{
		InterpolationWait = false
	};

	[XmlIgnore]
	protected NetBool bridgeUnlocked = new NetBool(value: false)
	{
		InterpolationWait = false
	};

	public Color[] pixelMap;

	public int[] heightMap;

	public Dictionary<int, List<Point>> possibleSwitchPositions = new Dictionary<int, List<Point>>();

	public Dictionary<int, List<Point>> possibleGatePositions = new Dictionary<int, List<Point>>();

	public NetList<DwarfGate, NetRef<DwarfGate>> dwarfGates = new NetList<DwarfGate, NetRef<DwarfGate>>();

	[XmlIgnore]
	protected bool _sawFlameSprite;

	private int lavaSoundsPlayedThisTick;

	private float steamTimer = 6000f;

	public VolcanoDungeon()
	{
		mapContent = Game1.game1.xTileContent.CreateTemporary();
		mapPath.Value = "Maps\\Mines\\VolcanoTemplate";
	}

	public VolcanoDungeon(int level)
		: this()
	{
		this.level.Value = level;
		name.Value = GetLevelName(level);
	}

	public override bool BlocksDamageLOS(int x, int y)
	{
		if (cooledLavaTiles.ContainsKey(new Vector2(x, y)))
		{
			return false;
		}
		return base.BlocksDamageLOS(x, y);
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(level, "level").AddField(coolLavaEvent, "coolLavaEvent").AddField(cooledLavaTiles.NetFields, "cooledLavaTiles.NetFields")
			.AddField(generationSeed, "generationSeed")
			.AddField(layoutIndex, "layoutIndex")
			.AddField(dwarfGates, "dwarfGates")
			.AddField(shortcutOutUnlocked, "shortcutOutUnlocked")
			.AddField(bridgeUnlocked, "bridgeUnlocked");
		coolLavaEvent.onEvent += OnCoolLavaEvent;
		bridgeUnlocked.fieldChangeEvent += delegate(NetBool f, bool oldValue, bool newValue)
		{
			if (newValue && mapPath.Value != null)
			{
				UpdateBridge();
			}
		};
		shortcutOutUnlocked.fieldChangeEvent += delegate(NetBool f, bool oldValue, bool newValue)
		{
			if (newValue && mapPath.Value != null)
			{
				UpdateShortcutOut();
			}
		};
	}

	protected override LocalizedContentManager getMapLoader()
	{
		return mapContent;
	}

	public override bool CanPlaceThisFurnitureHere(Furniture furniture)
	{
		return false;
	}

	public virtual void OnCoolLavaEvent(Point point)
	{
		CoolLava(point.X, point.Y);
		UpdateLavaNeighbor(point.X, point.Y);
		UpdateLavaNeighbor(point.X - 1, point.Y);
		UpdateLavaNeighbor(point.X + 1, point.Y);
		UpdateLavaNeighbor(point.X, point.Y - 1);
		UpdateLavaNeighbor(point.X, point.Y + 1);
	}

	public virtual void CoolLava(int x, int y, bool playSound = true)
	{
		if (Game1.currentLocation == this)
		{
			for (int i = 0; i < 5; i++)
			{
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Vector2(x, (float)y - 0.5f) * 64f + new Vector2(Game1.random.Next(64), Game1.random.Next(64)), flipped: false, 0.007f, Color.White)
				{
					alpha = 0.75f,
					motion = new Vector2(0f, -1f),
					acceleration = new Vector2(0.002f, 0f),
					interval = 99999f,
					layerDepth = 1f,
					scale = 4f,
					scaleChange = 0.02f,
					rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f,
					delayBeforeAnimationStart = i * 35
				});
			}
			if (playSound && lavaSoundsPlayedThisTick < 3)
			{
				DelayedAction.playSoundAfterDelay("steam", lavaSoundsPlayedThisTick * 300, null, null);
				lavaSoundsPlayedThisTick++;
			}
		}
		cooledLavaTiles.TryAdd(new Vector2(x, y), value: true);
	}

	public virtual void UpdateLavaNeighbor(int x, int y)
	{
		if (IsCooledLava(x, y))
		{
			setTileProperty(x, y, "Buildings", "Passable", "T");
			int neighbors = 0;
			if (IsCooledLava(x, y - 1))
			{
				neighbors++;
			}
			if (IsCooledLava(x, y + 1))
			{
				neighbors += 2;
			}
			if (IsCooledLava(x - 1, y))
			{
				neighbors += 8;
			}
			if (IsCooledLava(x + 1, y))
			{
				neighbors += 4;
			}
			if (GetBlobLookup().TryGetValue(neighbors, out var offset))
			{
				localCooledLavaTiles[new Vector2(x, y)] = offset;
			}
		}
	}

	public virtual bool IsCooledLava(int x, int y)
	{
		if (x < 0 || x >= mapWidth)
		{
			return false;
		}
		if (y < 0 || y >= mapHeight)
		{
			return false;
		}
		return cooledLavaTiles.ContainsKey(new Vector2(x, y));
	}

	public override bool answerDialogueAction(string questionAndAnswer, string[] questionParams)
	{
		if (questionAndAnswer == null)
		{
			return false;
		}
		if (questionAndAnswer == "LeaveVolcano_Yes")
		{
			UseVolcanoShortcut();
			return true;
		}
		return base.answerDialogueAction(questionAndAnswer, questionParams);
	}

	public void UseVolcanoShortcut()
	{
		DelayedAction.playSoundAfterDelay("fallDown", 200, null, null);
		DelayedAction.playSoundAfterDelay("clubSmash", 900, null, null);
		Game1.player.CanMove = false;
		Game1.player.jump();
		Game1.warpFarmer("IslandNorth", 56, 17, 1);
	}

	public virtual void GenerateContents(bool use_level_level_as_layout = false)
	{
		generated = true;
		if (Game1.IsMasterGame)
		{
			generationSeed.Value = Utility.CreateRandomSeed(Game1.stats.DaysPlayed * (level.Value + 1), level.Value * 5152, Game1.uniqueIDForThisGame / 2);
			switch (level.Value)
			{
			case 0:
				layoutIndex.Value = 0;
				bridgeUnlocked.Value = Game1.MasterPlayer.hasOrWillReceiveMail("Island_VolcanoBridge");
				parrotUpgradePerches.Clear();
				parrotUpgradePerches.Add(new ParrotUpgradePerch(this, new Point(27, 39), new Microsoft.Xna.Framework.Rectangle(28, 34, 5, 4), 5, delegate
				{
					Game1.addMailForTomorrow("Island_VolcanoBridge", noLetter: true, sendToEveryone: true);
					bridgeUnlocked.Value = true;
				}, () => bridgeUnlocked.Value, "VolcanoBridge", "reachedCaldera, Island_Turtle"));
				break;
			case 5:
				layoutIndex.Value = 31;
				waterColor.Value = Color.DeepSkyBlue * 0.6f;
				shortcutOutUnlocked.Value = Game1.MasterPlayer.hasOrWillReceiveMail("Island_VolcanoShortcutOut");
				parrotUpgradePerches.Clear();
				parrotUpgradePerches.Add(new ParrotUpgradePerch(this, new Point(shortcutOutPosition.X, shortcutOutPosition.Y), new Microsoft.Xna.Framework.Rectangle(shortcutOutPosition.X - 1, shortcutOutPosition.Y - 1, 3, 3), 5, delegate
				{
					Game1.addMailForTomorrow("Island_VolcanoShortcutOut", noLetter: true, sendToEveryone: true);
					shortcutOutUnlocked.Value = true;
				}, () => shortcutOutUnlocked.Value, "VolcanoShortcutOut", "Island_Turtle"));
				break;
			case 9:
				layoutIndex.Value = 30;
				break;
			default:
			{
				List<int> valid_layouts = new List<int>();
				for (int i = 1; i < GetMaxRoomLayouts(); i++)
				{
					valid_layouts.Add(i);
				}
				Random layout_random = Utility.CreateRandom(generationSeed.Value);
				float luckMultiplier = 1f + (float)Game1.player.team.AverageLuckLevel() * 0.035f + (float)Game1.player.team.AverageDailyLuck() / 2f;
				if (level.Value > 1 && layout_random.NextDouble() < 0.5 * (double)luckMultiplier)
				{
					bool foundSpecialLevel = false;
					for (int j = 0; j < activeLevels.Count; j++)
					{
						if (activeLevels[j].layoutIndex.Value >= 32)
						{
							foundSpecialLevel = true;
							break;
						}
					}
					if (!foundSpecialLevel)
					{
						for (int k = 32; k < 38; k++)
						{
							valid_layouts.Add(k);
						}
					}
				}
				if (level.Value > 0 && Game1.MasterPlayer.hasOrWillReceiveMail("volcanoShortcutUnlocked") && layout_random.NextDouble() < 0.75)
				{
					for (int l = 38; l < 58; l++)
					{
						valid_layouts.Add(l);
					}
				}
				for (int m = 0; m < activeLevels.Count; m++)
				{
					if (activeLevels[m].level.Value == level.Value - 1)
					{
						valid_layouts.Remove(activeLevels[m].layoutIndex.Value);
						break;
					}
				}
				layoutIndex.Value = layout_random.ChooseFrom(valid_layouts);
				break;
			}
			}
		}
		GenerateLevel(use_level_level_as_layout);
		if (level.Value != 5)
		{
			return;
		}
		ApplyMapOverride("Mines\\Volcano_Well", (Microsoft.Xna.Framework.Rectangle?)null, (Microsoft.Xna.Framework.Rectangle?)new Microsoft.Xna.Framework.Rectangle(25, 29, 6, 4));
		for (int x = 27; x < 31; x++)
		{
			for (int y = 29; y < 33; y++)
			{
				waterTiles[x, y] = true;
			}
		}
		ApplyMapOverride("Mines\\Volcano_DwarfShop", (Microsoft.Xna.Framework.Rectangle?)null, (Microsoft.Xna.Framework.Rectangle?)new Microsoft.Xna.Framework.Rectangle(34, 29, 5, 4));
		setMapTile(36, 30, 77, "Buildings", "dungeon", "asedf");
		setMapTile(36, 29, 61, "Front", "dungeon");
		setMapTile(35, 31, 78, "Back", "dungeon");
		setMapTile(36, 31, 79, "Back", "dungeon");
		setMapTile(37, 31, 62, "Back", "dungeon");
		if (Game1.IsMasterGame)
		{
			objects.Add(new Vector2(34f, 29f), BreakableContainer.GetBarrelForVolcanoDungeon(new Vector2(34f, 29f)));
			objects.Add(new Vector2(26f, 32f), BreakableContainer.GetBarrelForVolcanoDungeon(new Vector2(26f, 32f)));
			objects.Add(new Vector2(38f, 33f), BreakableContainer.GetBarrelForVolcanoDungeon(new Vector2(38f, 33f)));
		}
	}

	public bool isMushroomLevel()
	{
		if (layoutIndex.Value >= 32)
		{
			return layoutIndex.Value <= 34;
		}
		return false;
	}

	public bool isMonsterLevel()
	{
		if (layoutIndex.Value >= 35)
		{
			return layoutIndex.Value <= 37;
		}
		return false;
	}

	/// <inheritdoc />
	public override void checkForMusic(GameTime time)
	{
		if (Game1.getMusicTrackName() == "none" || Game1.isMusicContextActiveButNotPlaying())
		{
			Game1.changeMusicTrack("Volcano_Ambient");
		}
		base.checkForMusic(time);
	}

	public virtual void UpdateShortcutOut()
	{
		if (this == Game1.currentLocation)
		{
			if (shortcutOutUnlocked.Value)
			{
				setMapTile(shortcutOutPosition.X, shortcutOutPosition.Y, 367, "Buildings", "dungeon");
				removeTile(shortcutOutPosition.X, shortcutOutPosition.Y - 1, "Front");
			}
			else
			{
				setMapTile(shortcutOutPosition.X, shortcutOutPosition.Y, 399, "Buildings", "dungeon");
				setMapTile(shortcutOutPosition.X, shortcutOutPosition.Y - 1, 383, "Front", "dungeon");
			}
		}
	}

	public virtual void UpdateBridge()
	{
		if (this != Game1.currentLocation)
		{
			return;
		}
		if (Game1.MasterPlayer.hasOrWillReceiveMail("reachedCaldera"))
		{
			setMapTile(27, 39, 399, "Buildings", "dungeon");
			setMapTile(27, 38, 383, "Front", "dungeon");
		}
		if (!bridgeUnlocked.Value)
		{
			return;
		}
		for (int x = 28; x <= 32; x++)
		{
			for (int y = 34; y <= 37; y++)
			{
				setMapTile(x, y, x switch
				{
					28 => y switch
					{
						34 => 189, 
						37 => 221, 
						_ => 205, 
					}, 
					32 => y switch
					{
						34 => 191, 
						37 => 223, 
						_ => 207, 
					}, 
					_ => y switch
					{
						34 => 190, 
						37 => 222, 
						_ => 206, 
					}, 
				}, "Buildings", "dungeon").Properties["Passable"] = "T";
				removeTileProperty(x, y, "Back", "Water");
				NPC n = isCharacterAtTile(new Vector2(x, y));
				if (n is Monster)
				{
					characters.Remove(n);
				}
				if (waterTiles != null && x != 28 && x != 32)
				{
					waterTiles[x, y] = false;
				}
				cooledLavaTiles.Remove(new Vector2(x, y));
			}
		}
	}

	protected override void resetLocalState()
	{
		if (!generated)
		{
			GenerateContents();
			generated = true;
		}
		foreach (Vector2 position in cooledLavaTiles.Keys)
		{
			UpdateLavaNeighbor((int)position.X, (int)position.Y);
		}
		if (level.Value == 0)
		{
			UpdateBridge();
		}
		if (level.Value == 5)
		{
			UpdateShortcutOut();
		}
		base.resetLocalState();
		Game1.ambientLight = Color.White;
		int player_tile_y = (int)(Game1.player.Position.Y / 64f);
		if (level.Value == 0 && Game1.player.previousLocationName == "Caldera")
		{
			Game1.player.Position = new Vector2(44f, 50f) * 64f;
		}
		else if (player_tile_y == 0 && endPosition.HasValue)
		{
			if (endPosition.HasValue)
			{
				Game1.player.Position = new Vector2(endPosition.Value.X, endPosition.Value.Y) * 64f;
			}
		}
		else if (player_tile_y == 1 && startPosition.HasValue)
		{
			Game1.player.Position = new Vector2(startPosition.Value.X, startPosition.Value.Y) * 64f;
		}
		TileSheet mainTileSheet = map.RequireTileSheet(0, "dungeon");
		mapBaseTilesheet = Game1.temporaryContent.Load<Texture2D>(mainTileSheet.ImageSource);
		foreach (DwarfGate dwarfGate in dwarfGates)
		{
			dwarfGate.ResetLocalState();
		}
		if (level.Value == 5)
		{
			AmbientLocationSounds.addSound(new Vector2(29f, 31f), 0);
		}
		if (level.Value != 0)
		{
			return;
		}
		if (Game1.player.hasOrWillReceiveMail("Saw_Flame_Sprite_Volcano"))
		{
			_sawFlameSprite = true;
		}
		if (!_sawFlameSprite)
		{
			temporarySprites.Add(new TemporaryAnimatedSprite("Characters\\Monsters\\Magma Sprite", new Microsoft.Xna.Framework.Rectangle(0, 32, 16, 16), new Vector2(30f, 38f) * 64f, flipped: false, 0f, Color.White)
			{
				id = 999,
				scale = 4f,
				totalNumberOfLoops = 99999,
				interval = 70f,
				lightId = "VolcanoDungeon_FlameSpirit",
				lightRadius = 1f,
				animationLength = 7,
				layerDepth = 1f,
				yPeriodic = true,
				yPeriodicRange = 12f,
				yPeriodicLoopTime = 1000f,
				xPeriodic = true,
				xPeriodicRange = 16f,
				xPeriodicLoopTime = 1800f
			});
			temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\shadow", new Microsoft.Xna.Framework.Rectangle(0, 0, 12, 7), new Vector2(30.2f, 39.4f) * 64f, flipped: false, 0f, Color.White)
			{
				id = 998,
				scale = 4f,
				totalNumberOfLoops = 99999,
				interval = 1000f,
				animationLength = 1,
				layerDepth = 0.001f,
				yPeriodic = true,
				yPeriodicRange = 1f,
				yPeriodicLoopTime = 1000f,
				xPeriodic = true,
				xPeriodicRange = 16f,
				xPeriodicLoopTime = 1800f
			});
		}
		ApplyMapOverride("Mines\\Volcano_Well", (Microsoft.Xna.Framework.Rectangle?)null, (Microsoft.Xna.Framework.Rectangle?)new Microsoft.Xna.Framework.Rectangle(22, 43, 6, 4));
		for (int x = 24; x < 28; x++)
		{
			for (int y = 43; y < 47; y++)
			{
				waterTiles[x, y] = true;
			}
		}
	}

	public override string GetLocationSpecificMusic()
	{
		if (level.Value == 5)
		{
			return "Volcano_Ambient";
		}
		if (Game1.getMusicTrackName() == "VolcanoMines")
		{
			return "VolcanoMines";
		}
		if (level.Value == 1 || ((Game1.random.NextDouble() < 0.25 || level.Value == 6) && (Game1.getMusicTrackName() == "none" || Game1.isMusicContextActiveButNotPlaying() || Game1.getMusicTrackName().EndsWith("_Ambient"))))
		{
			return "VolcanoMines";
		}
		return "Volcano_Ambient";
	}

	protected override void resetSharedState()
	{
		base.resetSharedState();
		if (level.Value != 5)
		{
			waterColor.Value = Color.White;
		}
	}

	public override bool CanRefillWateringCanOnTile(int tileX, int tileY)
	{
		if (level.Value == 5 && new Microsoft.Xna.Framework.Rectangle(27, 29, 4, 4).Contains(tileX, tileY))
		{
			return true;
		}
		if (level.Value == 0 && tileX > 23 && tileX < 28 && tileY > 42 && tileY < 47)
		{
			return true;
		}
		return false;
	}

	public virtual void GenerateLevel(bool use_level_level_as_layout = false)
	{
		generationRandom = Utility.CreateRandom(generationSeed.Value);
		generationRandom.Next();
		mapPath.Value = "Maps\\Mines\\VolcanoTemplate";
		updateMap();
		loadedMapPath = mapPath.Value;
		Texture2D layout_texture = Game1.temporaryContent.Load<Texture2D>("VolcanoLayouts\\Layouts");
		mapWidth = 64;
		mapHeight = 64;
		waterTiles = new WaterTiles(mapWidth, mapHeight);
		for (int i = 0; i < map.Layers.Count; i++)
		{
			Layer template_layer = map.Layers[i];
			map.RemoveLayer(template_layer);
			map.InsertLayer(new Layer(template_layer.Id, map, new Size(mapWidth, mapHeight), template_layer.TileSize), i);
		}
		backLayer = map.RequireLayer("Back");
		buildingsLayer = map.RequireLayer("Buildings");
		frontLayer = map.RequireLayer("Front");
		alwaysFrontLayer = map.RequireLayer("AlwaysFront");
		TileSheet tileSheet = map.RequireTileSheet(0, "dungeon");
		tileSheet.TileIndexProperties[1].Add("Type", "Stone");
		tileSheet.TileIndexProperties[2].Add("Type", "Stone");
		tileSheet.TileIndexProperties[3].Add("Type", "Stone");
		tileSheet.TileIndexProperties[17].Add("Type", "Stone");
		tileSheet.TileIndexProperties[18].Add("Type", "Stone");
		tileSheet.TileIndexProperties[19].Add("Type", "Stone");
		tileSheet.TileIndexProperties[528].Add("Type", "Stone");
		tileSheet.TileIndexProperties[544].Add("Type", "Stone");
		tileSheet.TileIndexProperties[560].Add("Type", "Stone");
		tileSheet.TileIndexProperties[545].Add("Type", "Stone");
		tileSheet.TileIndexProperties[561].Add("Type", "Stone");
		tileSheet.TileIndexProperties[564].Add("Type", "Stone");
		tileSheet.TileIndexProperties[565].Add("Type", "Stone");
		tileSheet.TileIndexProperties[555].Add("Type", "Stone");
		tileSheet.TileIndexProperties[571].Add("Type", "Stone");
		pixelMap = new Color[mapWidth * mapHeight];
		heightMap = new int[mapWidth * mapHeight];
		int columns = layout_texture.Width / 64;
		int value = layoutIndex.Value;
		int layout_offset_x = value % columns * 64;
		int layout_offset_y = value / columns * 64;
		bool flip_x = generationRandom.Next(2) == 1;
		if (layoutIndex.Value == 0 || layoutIndex.Value == 31)
		{
			flip_x = false;
		}
		ApplyPixels("VolcanoLayouts\\Layouts", layout_offset_x, layout_offset_y, mapWidth, mapHeight, 0, 0, flip_x);
		for (int j = 0; j < mapWidth; j++)
		{
			for (int k = 0; k < mapHeight; k++)
			{
				PlaceGroundTile(j, k);
			}
		}
		ApplyToColor(new Color(0, 255, 0), delegate(int x, int y)
		{
			if (!startPosition.HasValue)
			{
				startPosition = new Point(x, y);
			}
			if (level.Value == 0)
			{
				warps.Add(new Warp(x, y + 2, "IslandNorth", 40, 24, flipFarmer: false));
			}
			else
			{
				warps.Add(new Warp(x, y + 2, GetLevelName(level.Value - 1), x - startPosition.Value.X, 0, flipFarmer: false));
			}
		});
		ApplyToColor(new Color(255, 0, 0), delegate(int x, int y)
		{
			if (!endPosition.HasValue)
			{
				endPosition = new Point(x, y);
			}
			if (level.Value == 9)
			{
				warps.Add(new Warp(x, y - 2, "Caldera", 21, 39, flipFarmer: false));
			}
			else
			{
				warps.Add(new Warp(x, y - 2, GetLevelName(level.Value + 1), x - endPosition.Value.X, 1, flipFarmer: false));
			}
		});
		setPieceAreas.Clear();
		Color set_piece_color = new Color(255, 255, 0);
		ApplyToColor(set_piece_color, delegate(int x, int y)
		{
			int l;
			for (l = 0; l < 32 && !(GetPixel(x + l, y, Color.Black) != set_piece_color) && !(GetPixel(x, y + l, Color.Black) != set_piece_color); l++)
			{
			}
			setPieceAreas.Add(new Microsoft.Xna.Framework.Rectangle(x, y, l, l));
			for (int m = 0; m < l; m++)
			{
				for (int n = 0; n < l; n++)
				{
					SetPixelMap(x + m, y + n, Color.White);
				}
			}
		});
		possibleSwitchPositions = new Dictionary<int, List<Point>>();
		possibleGatePositions = new Dictionary<int, List<Point>>();
		ApplyToColor(new Color(128, 128, 128), delegate(int x, int y)
		{
			AddPossibleSwitchLocation(0, x, y);
		});
		ApplySetPieces();
		GenerateWalls(Color.Black, 0, 4, 4, 4, start_in_wall: true, delegate(int x, int y)
		{
			SetPixelMap(x, y, Color.Chartreuse);
		}, use_corner_hack: true);
		GenerateWalls(Color.Chartreuse, 0, 13, 1);
		ApplyToColor(Color.Blue, delegate(int x, int y)
		{
			waterTiles[x, y] = true;
			SetTile(backLayer, x, y, 4);
			setTileProperty(x, y, "Back", "Water", "T");
			if (generationRandom.NextDouble() < 0.1)
			{
				sharedLights.AddLight(new LightSource($"VolcanoDungeon_{level.Value}_Lava_{x}_{y}", 4, new Vector2(x, y) * 64f, 2f, new Color(0, 50, 50), LightSource.LightContext.None, 0L, base.NameOrUniqueName));
			}
		});
		GenerateBlobs(Color.Blue, 0, 16, fill_center: true, is_lava_pool: true);
		if (startPosition.HasValue)
		{
			CreateEntrance(startPosition.Value);
		}
		if (endPosition.HasValue)
		{
			CreateExit(endPosition);
		}
		if (level.Value != 0)
		{
			GenerateDirtTiles();
		}
		if ((level.Value == 9 || generationRandom.NextDouble() < (isMonsterLevel() ? 1.0 : 0.2)) && possibleSwitchPositions.TryGetValue(0, out var endSwitchPositions) && endSwitchPositions.Count > 0)
		{
			AddPossibleGateLocation(0, endPosition.Value.X, endPosition.Value.Y);
		}
		foreach (int index in possibleGatePositions.Keys)
		{
			if (possibleGatePositions[index].Count > 0 && possibleSwitchPositions.TryGetValue(index, out var dwarfSwitchPositions) && dwarfSwitchPositions.Count > 0)
			{
				Point gate_point = generationRandom.ChooseFrom(possibleGatePositions[index]);
				CreateDwarfGate(index, gate_point);
			}
		}
		if (level.Value == 0)
		{
			CreateExit(new Point(40, 48), draw_stairs: false);
			removeTile(40, 46, "Buildings");
			removeTile(40, 45, "Buildings");
			removeTile(40, 44, "Buildings");
			setMapTile(40, 45, 266, "AlwaysFront", "dungeon");
			setMapTile(40, 44, 76, "AlwaysFront", "dungeon");
			setMapTile(39, 44, 76, "AlwaysFront", "dungeon");
			setMapTile(41, 44, 76, "AlwaysFront", "dungeon");
			removeTile(40, 43, "Front");
			setMapTile(40, 43, 70, "AlwaysFront", "dungeon");
			removeTile(39, 43, "Front");
			setMapTile(39, 43, 69, "AlwaysFront", "dungeon");
			removeTile(41, 43, "Front");
			setMapTile(41, 43, 69, "AlwaysFront", "dungeon");
			setMapTile(39, 45, 265, "AlwaysFront", "dungeon");
			setMapTile(41, 45, 267, "AlwaysFront", "dungeon");
			setMapTile(40, 45, 60, "Back", "dungeon");
			setMapTile(40, 46, 60, "Back", "dungeon");
			setMapTile(40, 47, 60, "Back", "dungeon");
			setMapTile(40, 48, 555, "Back", "dungeon");
			AddPossibleSwitchLocation(-1, 40, 51);
			CreateDwarfGate(-1, new Point(40, 48));
			setMapTile(34, 30, 90, "Buildings", "dungeon");
			setMapTile(34, 29, 148, "Buildings", "dungeon");
			setMapTile(34, 31, 180, "Buildings", "dungeon");
			setMapTile(34, 32, 196, "Buildings", "dungeon");
			CoolLava(34, 34, playSound: false);
			if (Game1.MasterPlayer.hasOrWillReceiveMail("volcanoShortcutUnlocked"))
			{
				foreach (DwarfGate gate in dwarfGates)
				{
					if (gate.gateIndex.Value != -1)
					{
						continue;
					}
					gate.opened.Value = true;
					gate.triggeredOpen = true;
					foreach (Point point in gate.switches.Keys)
					{
						gate.switches[point] = true;
					}
				}
			}
			CreateExit(new Point(44, 50));
			warps.Add(new Warp(44, 48, "Caldera", 11, 36, flipFarmer: false));
			CreateEntrance(new Point(6, 48));
			warps.Add(new Warp(6, 50, "IslandNorth", 12, 31, flipFarmer: false));
		}
		if (Game1.IsMasterGame)
		{
			GenerateEntities();
		}
		pixelMap = null;
		SortLayers();
	}

	public virtual void GenerateDirtTiles()
	{
		if (level.Value == 5)
		{
			return;
		}
		for (int i = 0; i < 8; i++)
		{
			int center_x = generationRandom.Next(0, 64);
			int center_y = generationRandom.Next(0, 64);
			int travel_distance = generationRandom.Next(2, 8);
			int radius = generationRandom.Next(1, 3);
			int direction_x = ((generationRandom.Next(2) != 0) ? 1 : (-1));
			int direction_y = ((generationRandom.Next(2) != 0) ? 1 : (-1));
			bool x_oriented = generationRandom.Next(2) == 0;
			for (int j = 0; j < travel_distance; j++)
			{
				for (int x = center_x - radius; x <= center_x + radius; x++)
				{
					for (int y = center_y - radius; y <= center_y + radius; y++)
					{
						if (!(GetPixel(x, y, Color.Black) != Color.White))
						{
							dirtTiles.Add(new Point(x, y));
						}
					}
				}
				if (x_oriented)
				{
					direction_y += ((generationRandom.Next(2) != 0) ? 1 : (-1));
				}
				else
				{
					direction_x += ((generationRandom.Next(2) != 0) ? 1 : (-1));
				}
				center_x += direction_x;
				center_y += direction_y;
				radius += ((generationRandom.Next(2) != 0) ? 1 : (-1));
				if (radius < 1)
				{
					radius = 1;
				}
				if (radius > 4)
				{
					radius = 4;
				}
			}
		}
		for (int k = 0; k < 2; k++)
		{
			ErodeInvalidDirtTiles();
		}
		HashSet<Point> visited_neighbors = new HashSet<Point>();
		Point[] neighboring_tiles = new Point[8]
		{
			new Point(-1, -1),
			new Point(0, -1),
			new Point(1, -1),
			new Point(-1, 0),
			new Point(1, 0),
			new Point(-1, 1),
			new Point(0, 1),
			new Point(1, 1)
		};
		foreach (Point point in dirtTiles)
		{
			SetTile(backLayer, point.X, point.Y, GetTileIndex(9, 1));
			if (generationRandom.NextDouble() < 0.015)
			{
				characters.Add(new Duggy(Utility.PointToVector2(point) * 64f, magmaDuggy: true));
			}
			Point[] array = neighboring_tiles;
			for (int l = 0; l < array.Length; l++)
			{
				Point offset = array[l];
				Point neighbor = new Point(point.X + offset.X, point.Y + offset.Y);
				if (!dirtTiles.Contains(neighbor) && !visited_neighbors.Contains(neighbor))
				{
					visited_neighbors.Add(neighbor);
					Point? neighbor_tile_offset = GetDirtNeighborTile(neighbor.X, neighbor.Y);
					if (neighbor_tile_offset.HasValue)
					{
						SetTile(backLayer, neighbor.X, neighbor.Y, GetTileIndex(8 + neighbor_tile_offset.Value.X, neighbor_tile_offset.Value.Y));
					}
				}
			}
		}
	}

	public virtual void CreateEntrance(Point? position)
	{
		for (int x = -1; x <= 1; x++)
		{
			for (int y = 0; y <= 3; y++)
			{
				if (isTileOnMap(new Vector2(position.Value.X + x, position.Value.Y + y)))
				{
					removeTile(position.Value.X + x, position.Value.Y + y, "Back");
					removeTile(position.Value.X + x, position.Value.Y + y, "Buildings");
					removeTile(position.Value.X + x, position.Value.Y + y, "Front");
				}
			}
		}
		if (hasTileAt(position.Value.X - 1, position.Value.Y - 1, "Front"))
		{
			SetTile(frontLayer, position.Value.X - 1, position.Value.Y - 1, GetTileIndex(13, 16));
		}
		removeTile(position.Value.X, position.Value.Y - 1, "Front");
		SetTile(buildingsLayer, position.Value.X - 1, position.Value.Y, GetTileIndex(13, 17));
		SetTile(buildingsLayer, position.Value.X - 1, position.Value.Y + 1, GetTileIndex(13, 18));
		SetTile(buildingsLayer, position.Value.X - 1, position.Value.Y + 2, GetTileIndex(13, 19));
		if (hasTileAt(position.Value.X + 1, position.Value.Y - 1, "Front"))
		{
			SetTile(frontLayer, position.Value.X + 1, position.Value.Y - 1, GetTileIndex(15, 16));
		}
		SetTile(buildingsLayer, position.Value.X + 1, position.Value.Y, GetTileIndex(15, 17));
		SetTile(buildingsLayer, position.Value.X + 1, position.Value.Y + 1, GetTileIndex(15, 18));
		SetTile(buildingsLayer, position.Value.X + 1, position.Value.Y + 2, GetTileIndex(15, 19));
		SetTile(backLayer, position.Value.X, position.Value.Y, GetTileIndex(14, 17));
		SetTile(backLayer, position.Value.X, position.Value.Y + 1, GetTileIndex(14, 18));
		SetTile(frontLayer, position.Value.X, position.Value.Y + 2, GetTileIndex(14, 19));
		SetTile(buildingsLayer, position.Value.X - 1, position.Value.Y + 3, GetTileIndex(12, 4));
		SetTile(buildingsLayer, position.Value.X, position.Value.Y + 3, GetTileIndex(12, 4));
		SetTile(buildingsLayer, position.Value.X + 1, position.Value.Y + 3, GetTileIndex(12, 4));
	}

	private void CreateExit(Point? position, bool draw_stairs = true)
	{
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -4; y <= 0; y++)
			{
				if (isTileOnMap(new Vector2(position.Value.X + x, position.Value.Y + y)))
				{
					if (draw_stairs)
					{
						removeTile(position.Value.X + x, position.Value.Y + y, "Back");
					}
					removeTile(position.Value.X + x, position.Value.Y + y, "Buildings");
					removeTile(position.Value.X + x, position.Value.Y + y, "Front");
				}
			}
		}
		SetTile(buildingsLayer, position.Value.X - 1, position.Value.Y, GetTileIndex(9, 19));
		SetTile(buildingsLayer, position.Value.X - 1, position.Value.Y - 1, GetTileIndex(9, 18));
		SetTile(buildingsLayer, position.Value.X - 1, position.Value.Y - 2, GetTileIndex(9, 17));
		SetTile(buildingsLayer, position.Value.X - 1, position.Value.Y - 3, GetTileIndex(9, 16));
		SetTile(alwaysFrontLayer, position.Value.X - 1, position.Value.Y - 4, GetTileIndex(12, 4));
		SetTile(alwaysFrontLayer, position.Value.X, position.Value.Y - 4, GetTileIndex(12, 4));
		SetTile(alwaysFrontLayer, position.Value.X + 1, position.Value.Y - 4, GetTileIndex(12, 4));
		SetTile(buildingsLayer, position.Value.X, position.Value.Y - 3, GetTileIndex(10, 16));
		SetTile(buildingsLayer, position.Value.X + 1, position.Value.Y, GetTileIndex(11, 19));
		SetTile(buildingsLayer, position.Value.X + 1, position.Value.Y - 1, GetTileIndex(11, 18));
		SetTile(buildingsLayer, position.Value.X + 1, position.Value.Y - 2, GetTileIndex(11, 17));
		SetTile(buildingsLayer, position.Value.X + 1, position.Value.Y - 3, GetTileIndex(11, 16));
		if (draw_stairs)
		{
			SetTile(backLayer, position.Value.X, position.Value.Y, GetTileIndex(12, 19));
			SetTile(backLayer, position.Value.X, position.Value.Y - 1, GetTileIndex(12, 18));
			SetTile(backLayer, position.Value.X, position.Value.Y - 2, GetTileIndex(12, 17));
			SetTile(backLayer, position.Value.X, position.Value.Y - 3, GetTileIndex(12, 16));
		}
		SetTile(buildingsLayer, position.Value.X - 1, position.Value.Y - 4, GetTileIndex(12, 4));
		SetTile(buildingsLayer, position.Value.X, position.Value.Y - 4, GetTileIndex(12, 4));
		SetTile(buildingsLayer, position.Value.X + 1, position.Value.Y - 4, GetTileIndex(12, 4));
	}

	public virtual void ErodeInvalidDirtTiles()
	{
		Point[] neighboring_tiles = new Point[8]
		{
			new Point(-1, -1),
			new Point(0, -1),
			new Point(1, -1),
			new Point(-1, 0),
			new Point(1, 0),
			new Point(-1, 1),
			new Point(0, 1),
			new Point(1, 1)
		};
		Dictionary<Point, bool> visited_tiles = new Dictionary<Point, bool>();
		List<Point> dirt_to_remove = new List<Point>();
		foreach (Point dirt_tile in dirtTiles)
		{
			bool fail = false;
			foreach (Microsoft.Xna.Framework.Rectangle setPieceArea in setPieceAreas)
			{
				if (setPieceArea.Contains(dirt_tile))
				{
					fail = true;
					break;
				}
			}
			if (!fail && hasTileAt(dirt_tile, "Buildings"))
			{
				fail = true;
			}
			if (!fail)
			{
				Point[] array = neighboring_tiles;
				for (int i = 0; i < array.Length; i++)
				{
					Point offset = array[i];
					Point neighbor = new Point(dirt_tile.X + offset.X, dirt_tile.Y + offset.Y);
					if (visited_tiles.TryGetValue(neighbor, out var prevSucceeded))
					{
						if (!prevSucceeded)
						{
							fail = true;
							break;
						}
					}
					else if (!dirtTiles.Contains(neighbor))
					{
						if (!GetDirtNeighborTile(neighbor.X, neighbor.Y).HasValue)
						{
							fail = true;
						}
						visited_tiles[neighbor] = !fail;
						if (fail)
						{
							break;
						}
					}
				}
			}
			if (fail)
			{
				dirt_to_remove.Add(dirt_tile);
			}
		}
		foreach (Point remove in dirt_to_remove)
		{
			dirtTiles.Remove(remove);
		}
	}

	public override void monsterDrop(Monster monster, int x, int y, Farmer who)
	{
		base.monsterDrop(monster, x, y, who);
		if (Game1.random.NextDouble() < 0.05)
		{
			Game1.player.team.RequestLimitedNutDrops("VolcanoMonsterDrop", this, x, y, 5);
		}
	}

	public Point? GetDirtNeighborTile(int tile_x, int tile_y)
	{
		if (GetPixel(tile_x, tile_y, Color.Black) != Color.White)
		{
			return null;
		}
		if (hasTileAt(new Point(tile_x, tile_y), "Buildings"))
		{
			return null;
		}
		if (dirtTiles.Contains(new Point(tile_x, tile_y - 1)) && dirtTiles.Contains(new Point(tile_x, tile_y + 1)))
		{
			return null;
		}
		if (dirtTiles.Contains(new Point(tile_x - 1, tile_y)) && dirtTiles.Contains(new Point(tile_x + 1, tile_y)))
		{
			return null;
		}
		if (dirtTiles.Contains(new Point(tile_x - 1, tile_y)) && !dirtTiles.Contains(new Point(tile_x + 1, tile_y)))
		{
			if (dirtTiles.Contains(new Point(tile_x, tile_y - 1)))
			{
				return new Point(3, 3);
			}
			if (dirtTiles.Contains(new Point(tile_x, tile_y + 1)))
			{
				return new Point(3, 1);
			}
			return new Point(2, 1);
		}
		if (dirtTiles.Contains(new Point(tile_x + 1, tile_y)) && !dirtTiles.Contains(new Point(tile_x - 1, tile_y)))
		{
			if (dirtTiles.Contains(new Point(tile_x, tile_y - 1)))
			{
				return new Point(3, 2);
			}
			if (dirtTiles.Contains(new Point(tile_x, tile_y + 1)))
			{
				return new Point(3, 0);
			}
			return new Point(0, 1);
		}
		if (dirtTiles.Contains(new Point(tile_x, tile_y - 1)) && !dirtTiles.Contains(new Point(tile_x, tile_y + 1)))
		{
			return new Point(1, 2);
		}
		if (dirtTiles.Contains(new Point(tile_x, tile_y + 1)) && !dirtTiles.Contains(new Point(tile_x, tile_y - 1)))
		{
			return new Point(1, 0);
		}
		if (dirtTiles.Contains(new Point(tile_x - 1, tile_y - 1)))
		{
			return new Point(2, 2);
		}
		if (dirtTiles.Contains(new Point(tile_x + 1, tile_y - 1)))
		{
			return new Point(0, 2);
		}
		if (dirtTiles.Contains(new Point(tile_x - 1, tile_y + 1)))
		{
			return new Point(0, 2);
		}
		if (dirtTiles.Contains(new Point(tile_x + 1, tile_y + 1)))
		{
			return new Point(2, 2);
		}
		return null;
	}

	public virtual void CreateDwarfGate(int gate_index, Point tile_position)
	{
		SetTile(backLayer, tile_position.X, tile_position.Y + 1, GetTileIndex(3, 34));
		SetTile(buildingsLayer, tile_position.X - 1, tile_position.Y + 1, GetTileIndex(2, 34));
		SetTile(buildingsLayer, tile_position.X + 1, tile_position.Y + 1, GetTileIndex(4, 34));
		SetTile(buildingsLayer, tile_position.X - 1, tile_position.Y, GetTileIndex(2, 33));
		SetTile(buildingsLayer, tile_position.X + 1, tile_position.Y, GetTileIndex(4, 33));
		SetTile(frontLayer, tile_position.X - 1, tile_position.Y - 1, GetTileIndex(2, 32));
		SetTile(frontLayer, tile_position.X + 1, tile_position.Y - 1, GetTileIndex(4, 32));
		SetTile(alwaysFrontLayer, tile_position.X - 1, tile_position.Y - 1, GetTileIndex(2, 32));
		SetTile(alwaysFrontLayer, tile_position.X, tile_position.Y - 1, GetTileIndex(3, 32));
		SetTile(alwaysFrontLayer, tile_position.X + 1, tile_position.Y - 1, GetTileIndex(4, 32));
		if (gate_index == 0)
		{
			SetTile(alwaysFrontLayer, tile_position.X - 1, tile_position.Y - 2, GetTileIndex(0, 32));
			SetTile(alwaysFrontLayer, tile_position.X + 1, tile_position.Y - 2, GetTileIndex(0, 32));
		}
		else
		{
			SetTile(alwaysFrontLayer, tile_position.X - 1, tile_position.Y - 2, GetTileIndex(9, 25));
			SetTile(alwaysFrontLayer, tile_position.X + 1, tile_position.Y - 2, GetTileIndex(10, 25));
		}
		int seed = generationRandom.Next();
		if (Game1.IsMasterGame)
		{
			DwarfGate gate = new DwarfGate(this, gate_index, tile_position.X, tile_position.Y, seed);
			dwarfGates.Add(gate);
		}
	}

	public virtual void AddPossibleSwitchLocation(int switch_index, int x, int y)
	{
		if (!possibleSwitchPositions.TryGetValue(switch_index, out var positions))
		{
			positions = (possibleSwitchPositions[switch_index] = new List<Point>());
		}
		positions.Add(new Point(x, y));
	}

	public virtual void AddPossibleGateLocation(int gate_index, int x, int y)
	{
		if (!possibleGatePositions.TryGetValue(gate_index, out var positions))
		{
			positions = (possibleGatePositions[gate_index] = new List<Point>());
		}
		positions.Add(new Point(x, y));
	}

	private void adjustLevelChances(ref double stoneChance, ref double monsterChance, ref double itemChance, ref double gemStoneChance)
	{
		if (level.Value == 0 || level.Value == 5)
		{
			monsterChance = 0.0;
			itemChance = 0.0;
			gemStoneChance = 0.0;
			stoneChance = 0.0;
		}
		if (isMushroomLevel())
		{
			monsterChance = 0.025;
			itemChance *= 35.0;
			stoneChance = 0.0;
		}
		else if (isMonsterLevel())
		{
			stoneChance = 0.0;
			itemChance = 0.0;
			monsterChance *= 2.0;
		}
		bool has_avoid_monsters_buff = false;
		bool has_spawn_monsters_buff = false;
		foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
		{
			if (onlineFarmer.hasBuff("23"))
			{
				has_avoid_monsters_buff = true;
			}
			if (onlineFarmer.hasBuff("24"))
			{
				has_spawn_monsters_buff = true;
			}
			if (has_spawn_monsters_buff && has_avoid_monsters_buff)
			{
				break;
			}
		}
		if (has_spawn_monsters_buff)
		{
			monsterChance *= 2.0;
		}
		gemStoneChance /= 2.0;
	}

	public bool isTileClearForMineObjects(Vector2 v, bool ignoreRuins = false)
	{
		if ((Math.Abs((float)startPosition.Value.X - v.X) <= 2f && Math.Abs((float)startPosition.Value.Y - v.Y) <= 2f) || (Math.Abs((float)endPosition.Value.X - v.X) <= 2f && Math.Abs((float)endPosition.Value.Y - v.Y) <= 2f))
		{
			return false;
		}
		if (GetPixel((int)v.X, (int)v.Y, Color.Black) == new Color(128, 128, 128))
		{
			return false;
		}
		if (!CanItemBePlacedHere(v, itemIsPassable: false, CollisionMask.All, CollisionMask.None))
		{
			return false;
		}
		string s = doesTileHaveProperty((int)v.X, (int)v.Y, "Type", "Back");
		if (s == null || !s.Equals("Stone"))
		{
			return false;
		}
		if (!isTileOnClearAndSolidGround(v))
		{
			return false;
		}
		if (objects.ContainsKey(v))
		{
			return false;
		}
		if (ignoreRuins)
		{
			int tileIndex = getTileIndexAt((int)v.X, (int)v.Y, "Back", "dungeon");
			if (tileIndex == -1 || tileIndex >= 384)
			{
				return false;
			}
		}
		return true;
	}

	public bool isTileOnClearAndSolidGround(Vector2 v)
	{
		if (map.RequireLayer("Back").Tiles[(int)v.X, (int)v.Y] == null)
		{
			return false;
		}
		if (map.RequireLayer("Front").Tiles[(int)v.X, (int)v.Y] != null || map.RequireLayer("Buildings").Tiles[(int)v.X, (int)v.Y] != null)
		{
			return false;
		}
		return true;
	}

	public virtual void GenerateEntities()
	{
		List<Point> spawn_points = new List<Point>();
		ApplyToColor(new Color(0, 255, 255), delegate(int x, int y)
		{
			spawn_points.Add(new Point(x, y));
		});
		List<Point> spiker_spawn_points = new List<Point>();
		ApplyToColor(new Color(0, 128, 255), delegate(int x, int y)
		{
			spiker_spawn_points.Add(new Point(x, y));
		});
		double stoneChance = (double)generationRandom.Next(11, 18) / 150.0;
		double monsterChance = 0.0008 + (double)generationRandom.Next(70) / 10000.0;
		double itemChance = 0.001;
		double gemStoneChance = 0.003;
		adjustLevelChances(ref stoneChance, ref monsterChance, ref itemChance, ref gemStoneChance);
		if (level.Value > 0 && level.Value != 5 && (generationRandom.NextBool() || isMushroomLevel()))
		{
			int numBarrels = generationRandom.Next(5) + (int)(Game1.player.team.AverageDailyLuck(Game1.currentLocation) * 20.0);
			if (isMushroomLevel())
			{
				numBarrels += 50;
			}
			for (int i = 0; i < numBarrels; i++)
			{
				Point p;
				Point motion;
				if (generationRandom.NextDouble() < 0.33)
				{
					p = new Point(generationRandom.Next(map.RequireLayer("Back").LayerWidth), 0);
					motion = new Point(0, 1);
				}
				else if (generationRandom.NextBool())
				{
					p = new Point(0, generationRandom.Next(map.RequireLayer("Back").LayerHeight));
					motion = new Point(1, 0);
				}
				else
				{
					p = new Point(map.RequireLayer("Back").LayerWidth - 1, generationRandom.Next(map.RequireLayer("Back").LayerHeight));
					motion = new Point(-1, 0);
				}
				while (isTileOnMap(p.X, p.Y))
				{
					p.X += motion.X;
					p.Y += motion.Y;
					if (isTileClearForMineObjects(new Vector2(p.X, p.Y)))
					{
						Vector2 objectPos = new Vector2(p.X, p.Y);
						if (isMushroomLevel())
						{
							terrainFeatures.Add(objectPos, new CosmeticPlant(6 + generationRandom.Next(3)));
						}
						else
						{
							objects.Add(objectPos, BreakableContainer.GetBarrelForVolcanoDungeon(objectPos));
						}
						break;
					}
				}
			}
		}
		if (level.Value != 5)
		{
			for (int j = 0; j < map.Layers[0].LayerWidth; j++)
			{
				for (int k = 0; k < map.Layers[0].LayerHeight; k++)
				{
					Vector2 objectPos2 = new Vector2(j, k);
					if ((Math.Abs((float)startPosition.Value.X - objectPos2.X) <= 5f && Math.Abs((float)startPosition.Value.Y - objectPos2.Y) <= 5f) || (Math.Abs((float)endPosition.Value.X - objectPos2.X) <= 5f && Math.Abs((float)endPosition.Value.Y - objectPos2.Y) <= 5f))
					{
						continue;
					}
					if (CanItemBePlacedHere(objectPos2) && generationRandom.NextDouble() < monsterChance)
					{
						if (getTileIndexAt((int)objectPos2.X, (int)objectPos2.Y, "Back", "dungeon") == 25)
						{
							if (!isMushroomLevel())
							{
								characters.Add(new Duggy(objectPos2 * 64f, magmaDuggy: true));
							}
						}
						else if (isMushroomLevel())
						{
							characters.Add(new RockCrab(objectPos2 * 64f, "False Magma Cap"));
						}
						else
						{
							characters.Add(new Bat(objectPos2 * 64f, (level.Value > 5 && generationRandom.NextBool()) ? (-556) : (-555)));
						}
					}
					else
					{
						if (!isTileClearForMineObjects(objectPos2, ignoreRuins: true))
						{
							continue;
						}
						double chance = stoneChance;
						if (chance > 0.0)
						{
							foreach (Vector2 v in Utility.getAdjacentTileLocations(objectPos2))
							{
								if (objects.ContainsKey(v))
								{
									chance += 0.1;
								}
							}
						}
						int stoneIndex = chooseStoneTypeIndexOnly(objectPos2);
						bool basicStone = stoneIndex >= 845 && stoneIndex <= 847;
						if (chance > 0.0 && (!basicStone || generationRandom.NextDouble() < chance))
						{
							Object stone = createStone(stoneIndex, objectPos2);
							if (stone != null)
							{
								base.Objects.Add(objectPos2, stone);
							}
						}
						else if (generationRandom.NextDouble() < itemChance)
						{
							base.Objects.Add(objectPos2, new Object("851", 1)
							{
								IsSpawnedObject = true,
								CanBeGrabbed = true
							});
						}
					}
				}
			}
			while (stoneChance != 0.0 && generationRandom.NextDouble() < 0.2)
			{
				tryToAddOreClumps();
			}
		}
		for (int l = 0; l < 7; l++)
		{
			if (spawn_points.Count == 0)
			{
				break;
			}
			int index = generationRandom.Next(0, spawn_points.Count);
			Point spawn_point = spawn_points[index];
			if (CanItemBePlacedHere(new Vector2(spawn_point.X, spawn_point.Y)))
			{
				Monster monster = null;
				if (generationRandom.NextDouble() <= 0.25)
				{
					for (int m = 0; m < 20; m++)
					{
						Point point = spawn_point;
						point.X += generationRandom.Next(-10, 11);
						point.Y += generationRandom.Next(-10, 11);
						bool fail = false;
						for (int check_x = -1; check_x <= 1; check_x++)
						{
							for (int check_y = -1; check_y <= 1; check_y++)
							{
								if (!LavaLurk.IsLavaTile(this, point.X + check_x, point.Y + check_y))
								{
									fail = true;
									break;
								}
							}
						}
						if (!fail)
						{
							monster = new LavaLurk(Utility.PointToVector2(point) * 64f);
							break;
						}
					}
				}
				if (monster == null && generationRandom.NextDouble() <= 0.20000000298023224)
				{
					monster = new HotHead(Utility.PointToVector2(spawn_point) * 64f);
				}
				if (monster == null)
				{
					GreenSlime greenSlime = new GreenSlime(Utility.PointToVector2(spawn_point) * 64f, 0);
					greenSlime.makeTigerSlime();
					monster = greenSlime;
				}
				if (monster != null)
				{
					characters.Add(monster);
				}
			}
			spawn_points.RemoveAt(index);
		}
		foreach (Point p2 in spiker_spawn_points)
		{
			if (CanSpawnCharacterHere(new Vector2(p2.X, p2.Y)))
			{
				int direction = 1;
				switch (getTileIndexAt(p2, "Back", "dungeon"))
				{
				case 537:
				case 538:
					direction = 2;
					break;
				case 552:
				case 569:
					direction = 3;
					break;
				case 553:
				case 570:
					direction = 0;
					break;
				}
				characters.Add(new Spiker(new Vector2(p2.X, p2.Y) * 64f, direction));
			}
		}
	}

	private Object createStone(int stone, Vector2 tile)
	{
		string whichStone = chooseStoneTypeIndexOnly(tile).ToString() ?? "";
		int stoneHealth = 1;
		switch (whichStone)
		{
		case "1095382":
			whichStone = (Game1.random.NextBool() ? "VolcanoCoalNode0" : "VolcanoCoalNode1");
			stoneHealth = 10;
			break;
		case "845":
		case "846":
		case "847":
			stoneHealth = 6;
			break;
		case "843":
		case "844":
			stoneHealth = 12;
			break;
		case "765":
			stoneHealth = 16;
			break;
		case "764":
			whichStone = "VolcanoGoldNode";
			stoneHealth = 8;
			break;
		case "290":
			stoneHealth = 8;
			break;
		case "751":
			stoneHealth = 8;
			break;
		case "819":
			stoneHealth = 8;
			break;
		}
		return new Object(whichStone, 1)
		{
			MinutesUntilReady = stoneHealth
		};
	}

	private int chooseStoneTypeIndexOnly(Vector2 tile)
	{
		int whichStone = generationRandom.Next(845, 848);
		float levelMod = 1f + (float)level.Value / 7f;
		float masterMultiplier = 0.8f;
		float luckMultiplier = 1f + (float)Game1.player.team.AverageLuckLevel() * 0.035f + (float)Game1.player.team.AverageDailyLuck() / 2f;
		double chance = 0.008 * (double)levelMod * (double)masterMultiplier * (double)luckMultiplier;
		foreach (Vector2 v in Utility.getAdjacentTileLocations(tile))
		{
			if (objects.TryGetValue(v, out var obj) && (obj.QualifiedItemId == "(O)843" || obj.QualifiedItemId == "(O)844"))
			{
				chance += 0.15;
			}
		}
		if (generationRandom.NextDouble() < chance)
		{
			whichStone = generationRandom.Next(843, 845);
		}
		else
		{
			chance = 0.0025 * (double)levelMod * (double)masterMultiplier * (double)luckMultiplier;
			foreach (Vector2 v2 in Utility.getAdjacentTileLocations(tile))
			{
				if (objects.TryGetValue(v2, out var obj2) && obj2.QualifiedItemId == "(O)765")
				{
					chance += 0.1;
				}
			}
			if (generationRandom.NextDouble() < chance)
			{
				whichStone = 765;
			}
			else
			{
				chance = 0.01 * (double)levelMod * (double)masterMultiplier;
				foreach (Vector2 v3 in Utility.getAdjacentTileLocations(tile))
				{
					if (objects.TryGetValue(v3, out var obj3) && obj3.QualifiedItemId == "(O)VolcanoGoldNode")
					{
						chance += 0.2;
					}
				}
				if (generationRandom.NextDouble() < chance)
				{
					whichStone = 764;
				}
				else
				{
					chance = 0.012 * (double)levelMod * (double)masterMultiplier;
					foreach (Vector2 v4 in Utility.getAdjacentTileLocations(tile))
					{
						if (objects.TryGetValue(v4, out var obj4) && obj4.QualifiedItemId.StartsWith("(O)VolcanoCoalNode"))
						{
							chance += 0.2;
						}
					}
					if (generationRandom.NextDouble() < chance)
					{
						whichStone = 1095382;
					}
					else
					{
						chance = 0.015 * (double)levelMod * (double)masterMultiplier;
						foreach (Vector2 v5 in Utility.getAdjacentTileLocations(tile))
						{
							if (objects.TryGetValue(v5, out var obj5) && obj5.QualifiedItemId == "(O)850")
							{
								chance += 0.25;
							}
						}
						if (generationRandom.NextDouble() < chance)
						{
							whichStone = 850;
						}
						else
						{
							chance = 0.018 * (double)levelMod * (double)masterMultiplier;
							foreach (Vector2 v6 in Utility.getAdjacentTileLocations(tile))
							{
								if (objects.TryGetValue(v6, out var obj6) && obj6.QualifiedItemId == "(O)849")
								{
									chance += 0.25;
								}
							}
							if (generationRandom.NextDouble() < chance)
							{
								whichStone = 849;
							}
						}
					}
				}
			}
		}
		if (generationRandom.NextDouble() < 0.0005)
		{
			whichStone = 819;
		}
		if (generationRandom.NextDouble() < 0.0007)
		{
			whichStone = 44;
		}
		if (level.Value > 2 && generationRandom.NextDouble() < 0.0002)
		{
			whichStone = 46;
		}
		return whichStone;
	}

	public void tryToAddOreClumps()
	{
		if (!(generationRandom.NextDouble() < 0.55 + Game1.player.team.AverageDailyLuck(Game1.currentLocation)))
		{
			return;
		}
		Vector2 endPoint = getRandomTile();
		for (int tries = 0; tries < 1 || generationRandom.NextDouble() < 0.25 + Game1.player.team.AverageDailyLuck(Game1.currentLocation); tries++)
		{
			if (CanItemBePlacedHere(endPoint, itemIsPassable: false, CollisionMask.All, CollisionMask.None) && isTileOnClearAndSolidGround(endPoint) && doesTileHaveProperty((int)endPoint.X, (int)endPoint.Y, "Diggable", "Back") == null)
			{
				Utility.recursiveObjectPlacement(new Object(generationRandom.Next(843, 845).ToString(), 1)
				{
					MinutesUntilReady = 12
				}, (int)endPoint.X, (int)endPoint.Y, 0.949999988079071, 0.30000001192092896, this, "Dirt", 0, 0.05000000074505806);
			}
			endPoint = getRandomTile();
		}
	}

	public virtual void ApplySetPieces()
	{
		for (int i = 0; i < setPieceAreas.Count; i++)
		{
			Microsoft.Xna.Framework.Rectangle rectangle = setPieceAreas[i];
			int size = 3;
			if (rectangle.Width >= 32)
			{
				size = 32;
			}
			else if (rectangle.Width >= 16)
			{
				size = 16;
			}
			else if (rectangle.Width >= 8)
			{
				size = 8;
			}
			else if (rectangle.Width >= 4)
			{
				size = 4;
			}
			Map override_map = Game1.game1.xTileContent.Load<Map>("Maps\\Mines\\Volcano_SetPieces_" + size);
			int cols = override_map.Layers[0].LayerWidth / size;
			int rows = override_map.Layers[0].LayerHeight / size;
			int selected_col = generationRandom.Next(0, cols);
			int selected_row = generationRandom.Next(0, rows);
			ApplyMapOverride(override_map, "area_" + i, new Microsoft.Xna.Framework.Rectangle(selected_col * size, selected_row * size, size, size), rectangle);
			Layer paths_layer = override_map.GetLayer("Paths");
			if (paths_layer == null)
			{
				continue;
			}
			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y <= size; y++)
				{
					int source_x = selected_col * size + x;
					int source_y = selected_row * size + y;
					int dest_x = rectangle.Left + x;
					int dest_y = rectangle.Top + y;
					if (!paths_layer.IsValidTileLocation(source_x, source_y))
					{
						continue;
					}
					Tile tile = paths_layer.Tiles[source_x, source_y];
					int path_index = tile?.TileIndex ?? (-1);
					if (path_index >= GetTileIndex(10, 14) && path_index <= GetTileIndex(15, 14))
					{
						int index = path_index - GetTileIndex(10, 14);
						if (index > 0)
						{
							index += i * 10;
						}
						double chance = 1.0;
						if (tile.Properties.TryGetValue("Chance", out var property) && !double.TryParse(property, out chance))
						{
							chance = 1.0;
						}
						if (generationRandom.NextDouble() < chance)
						{
							AddPossibleGateLocation(index, dest_x, dest_y);
						}
					}
					else if (path_index >= GetTileIndex(10, 15) && path_index <= GetTileIndex(15, 15))
					{
						int index2 = path_index - GetTileIndex(10, 15);
						if (index2 > 0)
						{
							index2 += i * 10;
						}
						AddPossibleSwitchLocation(index2, dest_x, dest_y);
					}
					else if (path_index == GetTileIndex(10, 20))
					{
						SetPixelMap(dest_x, dest_y, new Color(0, 255, 255));
					}
					else if (path_index == GetTileIndex(11, 20))
					{
						SetPixelMap(dest_x, dest_y, new Color(0, 0, 255));
					}
					else if (path_index == GetTileIndex(12, 20))
					{
						SpawnChest(dest_x, dest_y);
					}
					else if (path_index == GetTileIndex(13, 20))
					{
						SetPixelMap(dest_x, dest_y, new Color(0, 0, 0));
					}
					else if (path_index == GetTileIndex(14, 20) && generationRandom.NextBool())
					{
						if (Game1.IsMasterGame)
						{
							objects.Add(new Vector2(dest_x, dest_y), BreakableContainer.GetBarrelForVolcanoDungeon(new Vector2(dest_x, dest_y)));
						}
					}
					else if (path_index == GetTileIndex(15, 20) && generationRandom.NextBool())
					{
						if (Game1.IsMasterGame)
						{
							Vector2 objTile = new Vector2(dest_x, dest_y);
							objects.Add(objTile, new Object("852", 1)
							{
								IsSpawnedObject = true,
								CanBeGrabbed = true
							});
						}
					}
					else if (path_index == GetTileIndex(10, 21))
					{
						SetPixelMap(dest_x, dest_y, new Color(0, 128, 255));
					}
				}
			}
		}
	}

	public virtual void SpawnChest(int tile_x, int tile_y)
	{
		Random chest_random = Utility.CreateRandom(generationRandom.Next());
		float extraRare_luckboost = (float)Game1.player.team.AverageLuckLevel() * 0.035f + (float)Game1.player.team.AverageDailyLuck() / 2f;
		if (Game1.IsMasterGame)
		{
			Vector2 position = new Vector2(tile_x, tile_y);
			Chest chest = new Chest(playerChest: false, position);
			chest.dropContents.Value = true;
			chest.synchronized.Value = true;
			chest.type.Value = "interactive";
			if (chest_random.NextDouble() < (double)((level.Value == 9) ? (0.5f + extraRare_luckboost) : (0.1f + extraRare_luckboost)))
			{
				chest.SetBigCraftableSpriteIndex(227);
				PopulateChest(chest.Items, chest_random, 1);
			}
			else
			{
				chest.SetBigCraftableSpriteIndex(223);
				PopulateChest(chest.Items, chest_random, 0);
			}
			setObject(position, chest);
		}
	}

	protected override bool breakStone(string stoneId, int x, int y, Farmer who, Random r)
	{
		if (who != null)
		{
			switch (stoneId)
			{
			case "845":
			case "846":
			case "847":
				if (Game1.random.NextDouble() < 0.005)
				{
					Game1.createObjectDebris("(O)827", x, y, who.UniqueMultiplayerID, this);
				}
				break;
			}
		}
		if (who != null && r.NextDouble() < 0.03)
		{
			Game1.player.team.RequestLimitedNutDrops("VolcanoMining", this, x * 64, y * 64, 5);
		}
		return base.breakStone(stoneId, x, y, who, r);
	}

	public virtual void PopulateChest(IList<Item> items, Random chest_random, int chest_type)
	{
		switch (chest_type)
		{
		case 0:
		{
			int random_count2 = 7;
			int random2 = chest_random.Next(random_count2);
			if (!Game1.netWorldState.Value.GoldenCoconutCracked)
			{
				while (random2 == 1)
				{
					random2 = chest_random.Next(random_count2);
				}
			}
			if (Game1.random.NextBool() && Game1.player.team.SpecialOrderRuleActive("DROP_QI_BEANS"))
			{
				int num2 = chest_random.Next(2, 6);
				for (int l = 0; l < num2; l++)
				{
					items.Add(ItemRegistry.Create("(O)890"));
				}
			}
			switch (random2)
			{
			case 0:
			{
				for (int num3 = 0; num3 < 3; num3++)
				{
					items.Add(ItemRegistry.Create("(O)848"));
				}
				break;
			}
			case 1:
				items.Add(ItemRegistry.Create("(O)791"));
				break;
			case 2:
			{
				for (int n = 0; n < 8; n++)
				{
					items.Add(ItemRegistry.Create("(O)831"));
				}
				break;
			}
			case 3:
			{
				for (int m = 0; m < 5; m++)
				{
					items.Add(ItemRegistry.Create("(O)833"));
				}
				break;
			}
			case 4:
				items.Add(new Ring("861"));
				break;
			case 5:
				items.Add(new Ring("862"));
				break;
			default:
				items.Add(MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)" + chest_random.Next(54, 57)), chest_random));
				break;
			}
			break;
		}
		case 1:
		{
			int random_count = 9;
			int random = chest_random.Next(random_count);
			if (!Game1.netWorldState.Value.GoldenCoconutCracked)
			{
				while (random == 3)
				{
					random = chest_random.Next(random_count);
				}
			}
			if (Game1.random.NextDouble() <= 1.0 && Game1.player.team.SpecialOrderRuleActive("DROP_QI_BEANS"))
			{
				int num = chest_random.Next(4, 6);
				for (int i = 0; i < num; i++)
				{
					items.Add(ItemRegistry.Create("(O)890"));
				}
			}
			switch (random)
			{
			case 0:
			{
				for (int k = 0; k < 10; k++)
				{
					items.Add(ItemRegistry.Create("(O)848"));
				}
				break;
			}
			case 1:
				items.Add(ItemRegistry.Create("(B)854"));
				break;
			case 2:
				items.Add(ItemRegistry.Create("(B)855"));
				break;
			case 3:
			{
				for (int j = 0; j < 3; j++)
				{
					items.Add(ItemRegistry.Create<Object>("(O)791"));
				}
				break;
			}
			case 4:
				items.Add(new Ring("863"));
				break;
			case 5:
				items.Add(new Ring("860"));
				break;
			case 6:
				items.Add(MeleeWeapon.attemptAddRandomInnateEnchantment(ItemRegistry.Create("(W)" + chest_random.Next(57, 60)), chest_random));
				break;
			case 7:
				items.Add(ItemRegistry.Create("(H)76"));
				break;
			default:
				items.Add(ItemRegistry.Create("(O)289"));
				break;
			}
			break;
		}
		}
	}

	public virtual void ApplyToColor(Color match, Action<int, int> action)
	{
		for (int x = 0; x < mapWidth; x++)
		{
			for (int y = 0; y < mapHeight; y++)
			{
				if (GetPixel(x, y, match) == match)
				{
					action?.Invoke(x, y);
				}
			}
		}
	}

	public override bool sinkDebris(Debris debris, Vector2 chunkTile, Vector2 chunkPosition)
	{
		if (cooledLavaTiles.ContainsKey(chunkTile))
		{
			return false;
		}
		return base.sinkDebris(debris, chunkTile, chunkPosition);
	}

	public override bool performToolAction(Tool t, int tileX, int tileY)
	{
		if (level.Value != 5 && t is WateringCan && isTileOnMap(new Vector2(tileX, tileY)) && waterTiles[tileX, tileY] && !cooledLavaTiles.ContainsKey(new Vector2(tileX, tileY)))
		{
			coolLavaEvent.Fire(new Point(tileX, tileY));
		}
		return base.performToolAction(t, tileX, tileY);
	}

	public virtual void GenerateBlobs(Color match, int tile_x, int tile_y, bool fill_center = true, bool is_lava_pool = false)
	{
		for (int x = 0; x < mapWidth; x++)
		{
			for (int y = 0; y < mapHeight; y++)
			{
				if (!(GetPixel(x, y, match) == match))
				{
					continue;
				}
				int value = GetNeighborValue(x, y, match, is_lava_pool);
				if (fill_center || value != 15)
				{
					Dictionary<int, Point> blob_lookup = GetBlobLookup();
					if (is_lava_pool)
					{
						blob_lookup = GetLavaBlobLookup();
					}
					if (blob_lookup.TryGetValue(value, out var offset))
					{
						SetTile(buildingsLayer, x, y, GetTileIndex(tile_x + offset.X, tile_y + offset.Y));
					}
				}
			}
		}
	}

	public Dictionary<int, Point> GetBlobLookup()
	{
		if (_blobIndexLookup == null)
		{
			_blobIndexLookup = new Dictionary<int, Point>();
			_blobIndexLookup[0] = new Point(0, 0);
			_blobIndexLookup[6] = new Point(1, 0);
			_blobIndexLookup[14] = new Point(2, 0);
			_blobIndexLookup[10] = new Point(3, 0);
			_blobIndexLookup[7] = new Point(1, 1);
			_blobIndexLookup[11] = new Point(3, 1);
			_blobIndexLookup[5] = new Point(1, 2);
			_blobIndexLookup[13] = new Point(2, 2);
			_blobIndexLookup[9] = new Point(3, 2);
			_blobIndexLookup[2] = new Point(0, 1);
			_blobIndexLookup[3] = new Point(0, 2);
			_blobIndexLookup[1] = new Point(0, 3);
			_blobIndexLookup[4] = new Point(1, 3);
			_blobIndexLookup[12] = new Point(2, 3);
			_blobIndexLookup[8] = new Point(3, 3);
			_blobIndexLookup[15] = new Point(2, 1);
		}
		return _blobIndexLookup;
	}

	public override bool isCollidingPosition(Microsoft.Xna.Framework.Rectangle position, xTile.Dimensions.Rectangle viewport, bool isFarmer, int damagesFarmer, bool glider, Character character, bool pathfinding, bool projectile = false, bool ignoreCharacterRequirement = false, bool skipCollisionEffects = false)
	{
		if (isFarmer && !glider && (position.Left < 0 || position.Right > map.DisplayWidth || position.Top < 0 || position.Bottom > map.DisplayHeight))
		{
			return true;
		}
		return base.isCollidingPosition(position, viewport, isFarmer, damagesFarmer, glider, character, pathfinding, projectile, ignoreCharacterRequirement, skipCollisionEffects: false);
	}

	public Dictionary<int, Point> GetLavaBlobLookup()
	{
		if (_lavaBlobIndexLookup == null)
		{
			_lavaBlobIndexLookup = new Dictionary<int, Point>(GetBlobLookup());
			_lavaBlobIndexLookup[63] = new Point(2, 1);
			_lavaBlobIndexLookup[47] = new Point(4, 3);
			_lavaBlobIndexLookup[31] = new Point(4, 2);
			_lavaBlobIndexLookup[15] = new Point(4, 1);
		}
		return _lavaBlobIndexLookup;
	}

	public virtual void GenerateWalls(Color match, int source_x, int source_y, int wall_height = 4, int random_wall_variants = 1, bool start_in_wall = false, Action<int, int> on_insufficient_wall_height = null, bool use_corner_hack = false)
	{
		heightMap = new int[mapWidth * mapHeight];
		for (int i = 0; i < heightMap.Length; i++)
		{
			heightMap[i] = -1;
		}
		for (int pass = 0; pass < 2; pass++)
		{
			for (int x = 0; x < mapWidth; x++)
			{
				int last_y = -1;
				int clearance = 0;
				if (start_in_wall)
				{
					clearance = wall_height;
				}
				for (int current_y = 0; current_y <= mapHeight; current_y++)
				{
					if (GetPixel(x, current_y, match) != match || current_y >= mapHeight)
					{
						int current_height = 0;
						int wall_variant_index = 0;
						if (random_wall_variants > 1 && generationRandom.NextBool())
						{
							wall_variant_index = generationRandom.Next(1, random_wall_variants);
						}
						if (current_y >= mapHeight)
						{
							current_height = wall_height;
							clearance = wall_height;
						}
						for (int curr_y = current_y - 1; curr_y > last_y; curr_y--)
						{
							if (clearance < wall_height)
							{
								if (on_insufficient_wall_height != null)
								{
									on_insufficient_wall_height(x, curr_y);
								}
								else
								{
									SetPixelMap(x, curr_y, Color.White);
									PlaceSingleWall(x, curr_y);
								}
								current_height--;
							}
							else
							{
								switch (pass)
								{
								case 0:
									if (GetPixelClearance(x - 1, curr_y, wall_height, match) < wall_height && GetPixelClearance(x + 1, curr_y, wall_height, match) < wall_height)
									{
										if (on_insufficient_wall_height != null)
										{
											on_insufficient_wall_height(x, curr_y);
										}
										else
										{
											SetPixelMap(x, curr_y, Color.White);
											PlaceSingleWall(x, curr_y);
										}
										current_height--;
									}
									break;
								case 1:
									heightMap[x + curr_y * mapWidth] = current_height + 1;
									if (current_height < wall_height || wall_height == 0)
									{
										if (wall_height > 0)
										{
											SetTile(buildingsLayer, x, curr_y, GetTileIndex(source_x + random_wall_variants + wall_variant_index, source_y + 1 + random_wall_variants + wall_height - current_height - 1));
										}
									}
									else
									{
										SetTile(buildingsLayer, x, curr_y, GetTileIndex(source_x + random_wall_variants * 3, source_y));
									}
									break;
								}
							}
							if (current_height < wall_height)
							{
								current_height++;
							}
						}
						last_y = current_y;
						clearance = 0;
					}
					else
					{
						clearance++;
					}
				}
			}
		}
		List<Point> corner_tiles = new List<Point>();
		for (int y = 0; y < mapHeight; y++)
		{
			for (int j = 0; j < mapWidth; j++)
			{
				int height = GetHeight(j, y, wall_height);
				int left_height = GetHeight(j - 1, y, wall_height);
				int right_height = GetHeight(j + 1, y, wall_height);
				int top_height = GetHeight(j, y - 1, wall_height);
				int index = generationRandom.Next(0, random_wall_variants);
				if (right_height < height)
				{
					if (right_height == wall_height)
					{
						if (use_corner_hack)
						{
							corner_tiles.Add(new Point(j, y));
							SetTile(buildingsLayer, j, y, GetTileIndex(source_x + random_wall_variants * 3, source_y));
						}
						else
						{
							SetTile(buildingsLayer, j, y, GetTileIndex(source_x + random_wall_variants * 3, source_y + 1));
						}
					}
					else
					{
						Layer target_layer = buildingsLayer;
						if (right_height >= 0)
						{
							SetTile(buildingsLayer, j, y, GetTileIndex(source_x + random_wall_variants, source_y + 1 + random_wall_variants + wall_height - right_height));
							target_layer = frontLayer;
						}
						if (height > wall_height)
						{
							SetTile(target_layer, j, y, GetTileIndex(source_x + random_wall_variants * 3 - 1, source_y + 1 + index));
						}
						else
						{
							SetTile(target_layer, j, y, GetTileIndex(source_x + random_wall_variants * 2 + index, source_y + 1 + random_wall_variants * 2 + 1 - height - 1));
						}
						if (wall_height > 0 && y + 1 < mapHeight && right_height == -1 && GetHeight(j + 1, y + 1, wall_height) >= 0 && GetHeight(j, y + 1, wall_height) >= 0)
						{
							if (use_corner_hack)
							{
								corner_tiles.Add(new Point(j, y));
								SetTile(buildingsLayer, j, y, GetTileIndex(source_x + random_wall_variants * 3, source_y));
							}
							else
							{
								SetTile(frontLayer, j, y, GetTileIndex(source_x + random_wall_variants * 3, source_y + 2));
							}
						}
					}
				}
				else if (left_height < height)
				{
					if (left_height == wall_height)
					{
						if (use_corner_hack)
						{
							corner_tiles.Add(new Point(j, y));
							SetTile(buildingsLayer, j, y, GetTileIndex(source_x + random_wall_variants * 3, source_y));
						}
						else
						{
							SetTile(buildingsLayer, j, y, GetTileIndex(source_x + random_wall_variants * 3 + 1, source_y + 1));
						}
					}
					else
					{
						Layer target_layer2 = buildingsLayer;
						if (left_height >= 0)
						{
							SetTile(buildingsLayer, j, y, GetTileIndex(source_x + random_wall_variants, source_y + 1 + random_wall_variants + wall_height - left_height));
							target_layer2 = frontLayer;
						}
						if (height > wall_height)
						{
							SetTile(target_layer2, j, y, GetTileIndex(source_x, source_y + 1 + index));
						}
						else
						{
							SetTile(target_layer2, j, y, GetTileIndex(source_x + index, source_y + 1 + random_wall_variants * 2 + 1 - height - 1));
						}
						if (wall_height > 0 && y + 1 < mapHeight && left_height == -1 && GetHeight(j - 1, y + 1, wall_height) >= 0 && GetHeight(j, y + 1, wall_height) >= 0)
						{
							if (use_corner_hack)
							{
								corner_tiles.Add(new Point(j, y));
								SetTile(buildingsLayer, j, y, GetTileIndex(source_x + random_wall_variants * 3, source_y));
							}
							else
							{
								SetTile(frontLayer, j, y, GetTileIndex(source_x + random_wall_variants * 3 + 1, source_y + 2));
							}
						}
					}
				}
				if (height < 0 || top_height != -1)
				{
					continue;
				}
				if (wall_height > 0)
				{
					if (right_height == -1)
					{
						SetTile(frontLayer, j, y - 1, GetTileIndex(source_x + random_wall_variants * 2 + index, source_y));
					}
					else if (left_height == -1)
					{
						SetTile(frontLayer, j, y - 1, GetTileIndex(source_x + index, source_y));
					}
					else
					{
						SetTile(frontLayer, j, y - 1, GetTileIndex(source_x + random_wall_variants + index, source_y));
					}
				}
				else if (right_height == -1)
				{
					SetTile(buildingsLayer, j, y, GetTileIndex(source_x + random_wall_variants * 2 + index, source_y));
				}
				else if (left_height == -1)
				{
					SetTile(buildingsLayer, j, y, GetTileIndex(source_x + index, source_y));
				}
				else
				{
					SetTile(buildingsLayer, j, y, GetTileIndex(source_x + random_wall_variants + index, source_y));
				}
			}
		}
		if (use_corner_hack)
		{
			foreach (Point corner_tile in corner_tiles)
			{
				if (GetHeight(corner_tile.X - 1, corner_tile.Y, wall_height) == -1)
				{
					SetTile(frontLayer, corner_tile.X, corner_tile.Y, GetTileIndex(source_x + random_wall_variants * 3 + 1, source_y + 2));
				}
				else if (GetHeight(corner_tile.X + 1, corner_tile.Y, wall_height) == -1)
				{
					SetTile(frontLayer, corner_tile.X, corner_tile.Y, GetTileIndex(source_x + random_wall_variants * 3, source_y + 2));
				}
				if (GetHeight(corner_tile.X - 1, corner_tile.Y, wall_height) == wall_height)
				{
					SetTile(alwaysFrontLayer, corner_tile.X, corner_tile.Y, GetTileIndex(source_x + random_wall_variants * 3 + 1, source_y + 1));
				}
				else if (GetHeight(corner_tile.X + 1, corner_tile.Y, wall_height) == wall_height)
				{
					SetTile(alwaysFrontLayer, corner_tile.X, corner_tile.Y, GetTileIndex(source_x + random_wall_variants * 3, source_y + 1));
				}
			}
		}
		heightMap = null;
	}

	public int GetPixelClearance(int x, int y, int wall_height, Color match)
	{
		int current_height = 0;
		if (GetPixel(x, y, Color.White) == match)
		{
			current_height++;
			for (int i = 1; i < wall_height; i++)
			{
				if (current_height >= wall_height)
				{
					break;
				}
				if (y + i >= mapHeight)
				{
					return wall_height;
				}
				if (!(GetPixel(x, y + i, Color.White) == match))
				{
					break;
				}
				current_height++;
			}
			for (int j = 1; j < wall_height; j++)
			{
				if (current_height >= wall_height)
				{
					break;
				}
				if (y - j < 0)
				{
					return wall_height;
				}
				if (!(GetPixel(x, y - j, Color.White) == match))
				{
					break;
				}
				current_height++;
			}
			return current_height;
		}
		return 0;
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		base.UpdateWhenCurrentLocation(time);
		coolLavaEvent.Poll();
		lavaSoundsPlayedThisTick = 0;
		if (level.Value == 0 && Game1.currentLocation == this)
		{
			steamTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
			if (steamTimer < 0f)
			{
				steamTimer = 5000f;
				Game1.playSound("cavedrip", null);
				temporarySprites.Add(new TemporaryAnimatedSprite(null, new Microsoft.Xna.Framework.Rectangle(0, 0, 1, 1), new Vector2(34.5f, 30.75f) * 64f, flipped: false, 0f, Color.White)
				{
					texture = Game1.staminaRect,
					color = new Color(100, 150, 255),
					alpha = 0.75f,
					motion = new Vector2(0f, 1f),
					acceleration = new Vector2(0f, 0.1f),
					interval = 99999f,
					layerDepth = 1f,
					scale = 8f,
					id = 89898,
					yStopCoordinate = 2208,
					reachedStopCoordinate = delegate
					{
						removeTemporarySpritesWithID(89898);
						Game1.playSound("steam", null);
						for (int i = 0; i < 4; i++)
						{
							temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Vector2(33.75f, 33.5f) * 64f + new Vector2(Game1.random.Next(64), Game1.random.Next(64)), flipped: false, 0.007f, Color.White)
							{
								alpha = 0.75f,
								motion = new Vector2(0f, -1f),
								acceleration = new Vector2(0.002f, 0f),
								interval = 99999f,
								layerDepth = 1f,
								scale = 4f,
								scaleChange = 0.02f,
								rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f
							});
						}
					}
				});
			}
		}
		foreach (DwarfGate dwarfGate in dwarfGates)
		{
			dwarfGate.UpdateWhenCurrentLocation(time, this);
		}
		if (!_sawFlameSprite && Utility.isThereAFarmerWithinDistance(new Vector2(30f, 38f), 3, this) != null)
		{
			Game1.addMailForTomorrow("Saw_Flame_Sprite_Volcano", noLetter: true);
			TemporaryAnimatedSprite v = getTemporarySpriteByID(999);
			if (v != null)
			{
				v.yPeriodic = false;
				v.xPeriodic = false;
				v.sourceRect.Y = 0;
				v.sourceRectStartingPos.Y = 0f;
				v.motion = new Vector2(0f, -4f);
				v.acceleration = new Vector2(0f, -0.04f);
			}
			localSound("magma_sprite_spot", null, null);
			v = getTemporarySpriteByID(998);
			if (v != null)
			{
				v.yPeriodic = false;
				v.xPeriodic = false;
				v.motion = new Vector2(0f, -4f);
				v.acceleration = new Vector2(0f, -0.04f);
			}
			_sawFlameSprite = true;
		}
	}

	public virtual void PlaceGroundTile(int x, int y)
	{
		if (generationRandom.NextDouble() < 0.30000001192092896)
		{
			SetTile(backLayer, x, y, GetTileIndex(1 + generationRandom.Next(0, 3), generationRandom.Next(0, 2)));
		}
		else
		{
			SetTile(backLayer, x, y, GetTileIndex(1, 0));
		}
	}

	public override void drawFloorDecorations(SpriteBatch b)
	{
		base.drawFloorDecorations(b);
		for (int y = Game1.viewport.Y / 64 - 1; y < (Game1.viewport.Y + Game1.viewport.Height) / 64 + 1; y++)
		{
			for (int x = Game1.viewport.X / 64 - 1; x < (Game1.viewport.X + Game1.viewport.Width) / 64 + 1; x++)
			{
				Vector2 tile = new Vector2(x, y);
				if (localCooledLavaTiles.TryGetValue(tile, out var point))
				{
					point.X += 5;
					point.Y += 16;
					b.Draw(mapBaseTilesheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64)), new Microsoft.Xna.Framework.Rectangle(point.X * 16, point.Y * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.55f);
				}
			}
		}
	}

	public override void drawWaterTile(SpriteBatch b, int x, int y)
	{
		if (level.Value == 5)
		{
			base.drawWaterTile(b, x, y);
			return;
		}
		if (level.Value == 0 && x > 23 && x < 28 && y > 42 && y < 47)
		{
			drawWaterTile(b, x, y, Color.DeepSkyBlue * 0.8f);
			return;
		}
		bool num = y == map.Layers[0].LayerHeight - 1 || !waterTiles[x, y + 1];
		bool topY = y == 0 || !waterTiles[x, y - 1];
		int water_tile_upper_left_x = 0;
		int water_tile_upper_left_y = 320;
		b.Draw(mapBaseTilesheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - (int)((!topY) ? waterPosition : 0f))), new Microsoft.Xna.Framework.Rectangle(water_tile_upper_left_x + waterAnimationIndex * 16, water_tile_upper_left_y + (((x + y) % 2 != 0) ? ((!waterTileFlip) ? 32 : 0) : (waterTileFlip ? 32 : 0)) + (topY ? ((int)waterPosition / 4) : 0), 16, 16 + (topY ? ((int)(0f - waterPosition) / 4) : 0)), waterColor.Value, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.56f);
		if (num)
		{
			b.Draw(mapBaseTilesheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, (y + 1) * 64 - (int)waterPosition)), new Microsoft.Xna.Framework.Rectangle(water_tile_upper_left_x + waterAnimationIndex * 16, water_tile_upper_left_y + (((x + (y + 1)) % 2 != 0) ? ((!waterTileFlip) ? 32 : 0) : (waterTileFlip ? 32 : 0)), 16, 16 - (int)(16f - waterPosition / 4f) - 1), waterColor.Value, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.56f);
		}
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		foreach (DwarfGate dwarfGate in dwarfGates)
		{
			dwarfGate.Draw(b);
		}
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		base.drawAboveAlwaysFrontLayer(b);
		if (!Game1.game1.takingMapScreenshot && level.Value > 0)
		{
			Color col = SpriteText.color_Red;
			string txt = level.Value.ToString() ?? "";
			Microsoft.Xna.Framework.Rectangle tsarea = Game1.game1.GraphicsDevice.Viewport.GetTitleSafeArea();
			SpriteText.drawString(b, txt, tsarea.Left + 16, tsarea.Top + 16, 999999, -1, 999999, 1f, 1f, junimoText: false, 2, "", col);
		}
	}

	public override void performTenMinuteUpdate(int timeOfDay)
	{
		base.performTenMinuteUpdate(timeOfDay);
		if (!(Game1.random.NextDouble() < 0.1) || level.Value <= 0 || level.Value == 5)
		{
			return;
		}
		int numsprites = 0;
		foreach (NPC character in characters)
		{
			if (character is Bat)
			{
				numsprites++;
			}
		}
		if (numsprites < farmers.Count * 4)
		{
			spawnFlyingMonsterOffScreen();
		}
	}

	public void spawnFlyingMonsterOffScreen()
	{
		Vector2 spawnLocation = Vector2.Zero;
		switch (Game1.random.Next(4))
		{
		case 0:
			spawnLocation.X = Game1.random.Next(map.Layers[0].LayerWidth);
			break;
		case 3:
			spawnLocation.Y = Game1.random.Next(map.Layers[0].LayerHeight);
			break;
		case 1:
			spawnLocation.X = map.Layers[0].LayerWidth - 1;
			spawnLocation.Y = Game1.random.Next(map.Layers[0].LayerHeight);
			break;
		case 2:
			spawnLocation.Y = map.Layers[0].LayerHeight - 1;
			spawnLocation.X = Game1.random.Next(map.Layers[0].LayerWidth);
			break;
		}
		playSound("magma_sprite_spot", null, null);
		characters.Add(new Bat(spawnLocation, (level.Value > 5 && Game1.random.NextBool()) ? (-556) : (-555))
		{
			focusedOnFarmers = true
		});
	}

	public virtual void PlaceSingleWall(int x, int y)
	{
		int index = generationRandom.Next(0, 4);
		SetTile(frontLayer, x, y - 1, GetTileIndex(index, 2));
		SetTile(buildingsLayer, x, y, GetTileIndex(index, 3));
	}

	public virtual void ApplyPixels(string layout_texture_name, int source_x = 0, int source_y = 0, int width = 64, int height = 64, int x_offset = 0, int y_offset = 0, bool flip_x = false)
	{
		Texture2D texture2D = Game1.temporaryContent.Load<Texture2D>(layout_texture_name);
		Color[] pixels = new Color[width * height];
		texture2D.GetData(0, new Microsoft.Xna.Framework.Rectangle(source_x, source_y, width, height), pixels, 0, width * height);
		for (int base_x = 0; base_x < width; base_x++)
		{
			int x = base_x + x_offset;
			if (flip_x)
			{
				x = x_offset + width - 1 - base_x;
			}
			if (x < 0 || x >= mapWidth)
			{
				continue;
			}
			for (int base_y = 0; base_y < height; base_y++)
			{
				int y = base_y + y_offset;
				if (y >= 0 && y < mapHeight)
				{
					Color pixel_color = GetPixelColor(width, height, pixels, base_x, base_y);
					SetPixelMap(x, y, pixel_color);
				}
			}
		}
	}

	public int GetHeight(int x, int y, int max_height)
	{
		if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
		{
			return max_height + 1;
		}
		return heightMap[x + y * mapWidth];
	}

	public Color GetPixel(int x, int y, Color out_of_bounds_color)
	{
		if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
		{
			return out_of_bounds_color;
		}
		return pixelMap[x + y * mapWidth];
	}

	public void SetPixelMap(int x, int y, Color color)
	{
		if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
		{
			pixelMap[x + y * mapWidth] = color;
		}
	}

	public int GetNeighborValue(int x, int y, Color matched_color, bool is_lava_pool = false)
	{
		int neighbor_value = 0;
		if (GetPixel(x, y - 1, matched_color) == matched_color)
		{
			neighbor_value++;
		}
		if (GetPixel(x, y + 1, matched_color) == matched_color)
		{
			neighbor_value += 2;
		}
		if (GetPixel(x + 1, y, matched_color) == matched_color)
		{
			neighbor_value += 4;
		}
		if (GetPixel(x - 1, y, matched_color) == matched_color)
		{
			neighbor_value += 8;
		}
		if (is_lava_pool && neighbor_value == 15)
		{
			if (GetPixel(x - 1, y - 1, matched_color) == matched_color)
			{
				neighbor_value += 16;
			}
			if (GetPixel(x + 1, y - 1, matched_color) == matched_color)
			{
				neighbor_value += 32;
			}
		}
		return neighbor_value;
	}

	public Color GetPixelColor(int width, int height, Color[] pixels, int x, int y)
	{
		if (x < 0 || x >= width)
		{
			return Color.Black;
		}
		if (y < 0 || y >= height)
		{
			return Color.Black;
		}
		int index = x + y * width;
		return pixels[index];
	}

	public static int GetTileIndex(int x, int y)
	{
		return x + y * 16;
	}

	public void SetTile(Layer layer, int x, int y, int index)
	{
		if (x >= 0 && x < layer.LayerWidth && y >= 0 && y < layer.LayerHeight)
		{
			Location location = new Location(x, y);
			TileSheet mainTileSheet = map.RequireTileSheet(0, "dungeon");
			layer.Tiles[location] = new StaticTile(layer, mainTileSheet, BlendMode.Alpha, index);
		}
	}

	public int GetMaxRoomLayouts()
	{
		return 30;
	}

	public static VolcanoDungeon GetLevel(string name, bool use_level_level_as_layout = false)
	{
		foreach (VolcanoDungeon level in activeLevels)
		{
			if (level.Name.Equals(name))
			{
				return level;
			}
		}
		if (!IsGeneratedLevel(name, out var newLevelNumber))
		{
			Game1.log.Warn("Failed parsing Volcano Dungeon level from location name '" + name + "', defaulting to level 0.");
			newLevelNumber = 0;
		}
		VolcanoDungeon new_level = new VolcanoDungeon(newLevelNumber);
		activeLevels.Add(new_level);
		if (Game1.IsMasterGame)
		{
			new_level.GenerateContents(use_level_level_as_layout);
		}
		else
		{
			new_level.reloadMap();
		}
		return new_level;
	}

	/// <summary>Get the location name for a generated Volcano Dungeon level.</summary>
	/// <param name="level">The dungeon level.</param>
	public static string GetLevelName(int level)
	{
		return "VolcanoDungeon" + level;
	}

	/// <param name="locationName">The location name to check.</param>
	public static bool IsGeneratedLevel(string locationName)
	{
		int num;
		return IsGeneratedLevel(locationName, out num);
	}

	/// <summary>Get whether a location name is a generated Volcano Dungeon level.</summary>
	/// <param name="locationName">The location name to check.</param>
	/// <param name="level">The parsed dungeon level, if applicable.</param>
	public static bool IsGeneratedLevel(string locationName, out int level)
	{
		if (locationName == null || !locationName.StartsWithIgnoreCase("VolcanoDungeon"))
		{
			level = 0;
			return false;
		}
		return int.TryParse(locationName.Substring("VolcanoDungeon".Length), out level);
	}

	public static void UpdateLevels(GameTime time)
	{
		foreach (VolcanoDungeon level in activeLevels)
		{
			if (level.farmers.Count > 0)
			{
				level.UpdateWhenCurrentLocation(time);
			}
			level.updateEvenIfFarmerIsntHere(time);
		}
	}

	public static void UpdateLevels10Minutes(int timeOfDay)
	{
		if (Game1.IsClient)
		{
			return;
		}
		foreach (VolcanoDungeon level in activeLevels)
		{
			if (level.farmers.Count > 0)
			{
				level.performTenMinuteUpdate(timeOfDay);
			}
		}
	}

	public static void ClearAllLevels()
	{
		activeLevels.RemoveAll(delegate(VolcanoDungeon level)
		{
			level.OnRemoved();
			return true;
		});
	}

	/// <inheritdoc />
	public override void OnRemoved()
	{
		base.OnRemoved();
		if (Game1.IsMasterGame)
		{
			debris.RemoveWhere((Debris d) => d.isEssentialItem() && d.collect(Game1.player));
		}
		mapContent.Dispose();
	}

	public static void ForEach(Action<VolcanoDungeon> action)
	{
		foreach (VolcanoDungeon level in activeLevels)
		{
			action(level);
		}
	}

	/// <inheritdoc />
	public override bool ShouldExcludeFromNpcPathfinding()
	{
		return true;
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		switch (getTileIndexAt(tileLocation.X, tileLocation.Y, "Buildings", "dungeon"))
		{
		case 367:
			createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Volcano_ShortcutOut"), createYesNoResponses(), "LeaveVolcano");
			return true;
		case 77:
			if (Game1.player.canUnderstandDwarves)
			{
				Utility.TryOpenShopMenu("VolcanoShop", null, playOpenSound: true);
			}
			else
			{
				Game1.player.doEmote(8);
			}
			return true;
		default:
			return base.checkAction(tileLocation, viewport, who);
		}
	}

	/// <inheritdoc />
	public override void performTouchAction(string[] action, Vector2 playerStandingPosition)
	{
		if (IgnoreTouchActions())
		{
			return;
		}
		if (ArgUtility.Get(action, 0) == "DwarfSwitch")
		{
			Point tile_point = new Point((int)playerStandingPosition.X, (int)playerStandingPosition.Y);
			{
				foreach (DwarfGate gate in dwarfGates)
				{
					if (gate.switches.TryGetValue(tile_point, out var wasPressed) && !wasPressed)
					{
						gate.pressEvent.Fire(tile_point);
					}
				}
				return;
			}
		}
		base.performTouchAction(action, playerStandingPosition);
	}
}
