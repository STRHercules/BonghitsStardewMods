using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace StardewValley.Locations;

public class CommunityCenter : GameLocation
{
	public const int AREA_Pantry = 0;

	public const int AREA_FishTank = 2;

	public const int AREA_CraftsRoom = 1;

	public const int AREA_BoilerRoom = 3;

	public const int AREA_Vault = 4;

	public const int AREA_Bulletin = 5;

	public const int AREA_AbandonedJojaMart = 6;

	public const int AREA_Bulletin2 = 7;

	public const int AREA_JunimoHut = 8;

	[XmlElement("warehouse")]
	private readonly NetBool warehouse = new NetBool();

	[XmlIgnore]
	public List<NetMutex> bundleMutexes = new List<NetMutex>();

	public readonly NetArray<bool, NetBool> areasComplete = new NetArray<bool, NetBool>(6);

	[XmlElement("numberOfStarsOnPlaque")]
	public readonly NetInt numberOfStarsOnPlaque = new NetInt();

	[XmlIgnore]
	private readonly NetEvent0 newJunimoNoteCheckEvent = new NetEvent0();

	[XmlIgnore]
	private readonly NetEvent1Field<int, NetInt> restoreAreaCutsceneEvent = new NetEvent1Field<int, NetInt>();

	[XmlIgnore]
	private readonly NetEvent1Field<int, NetInt> areaCompleteRewardEvent = new NetEvent1Field<int, NetInt>();

	private float messageAlpha;

	private List<int> junimoNotesViewportTargets;

	private Dictionary<int, List<int>> areaToBundleDictionary;

	private Dictionary<int, int> bundleToAreaDictionary;

	private Dictionary<string, List<List<int>>> bundlesIngredientsInfo;

	private bool _isWatchingJunimoGoodbye;

	private Vector2 missedRewardsChestTile = new Vector2(22f, 10f);

	private const string missedRewardsTileSheetId = "indoors2";

	[XmlIgnore]
	public readonly NetRef<Chest> missedRewardsChest = new NetRef<Chest>(new Chest(playerChest: true));

	[XmlIgnore]
	public readonly NetBool missedRewardsChestVisible = new NetBool(value: false);

	[XmlIgnore]
	public readonly NetEvent1Field<bool, NetBool> showMissedRewardsChestEvent = new NetEvent1Field<bool, NetBool>();

	public const int PHASE_firstPause = 0;

	public const int PHASE_junimoAppear = 1;

	public const int PHASE_junimoDance = 2;

	public const int PHASE_restore = 3;

	private int restoreAreaTimer;

	private int restoreAreaPhase;

	private int restoreAreaIndex;

	private ICue buildUpSound;

	[XmlElement("bundles")]
	public NetBundles bundles => Game1.netWorldState.Value.Bundles;

	[XmlElement("bundleRewards")]
	public NetIntDictionary<bool, NetBool> bundleRewards => Game1.netWorldState.Value.BundleRewards;

	public CommunityCenter()
	{
		initAreaBundleConversions();
		refreshBundlesIngredientsInfo();
	}

	public CommunityCenter(string map_path, string name)
		: base(map_path, name)
	{
		initAreaBundleConversions();
		refreshBundlesIngredientsInfo();
	}

	public CommunityCenter(string name)
		: base("Maps\\CommunityCenter_Ruins", name)
	{
		initAreaBundleConversions();
		refreshBundlesIngredientsInfo();
	}

	public void refreshBundlesIngredientsInfo()
	{
		bundlesIngredientsInfo = new Dictionary<string, List<List<int>>>();
		Dictionary<string, string> bundleData = Game1.netWorldState.Value.BundleData;
		Dictionary<int, bool[]> bundlesD = bundlesDict();
		foreach (KeyValuePair<string, string> v in bundleData)
		{
			string[] array = v.Key.Split('/');
			int bundleIndex = Convert.ToInt32(array[1]);
			string areaName = array[0];
			string[] ingredientSplit = ArgUtility.SplitBySpace(v.Value.Split('/')[2]);
			if (!shouldNoteAppearInArea(getAreaNumberFromName(areaName)))
			{
				continue;
			}
			for (int i = 0; i < ingredientSplit.Length; i += 3)
			{
				if (bundlesD.ContainsKey(bundleIndex) && !bundlesD[bundleIndex][i / 3])
				{
					string key;
					if (int.TryParse(ingredientSplit[i], out var categoryOrId) && categoryOrId < 0)
					{
						key = categoryOrId.ToString();
					}
					else
					{
						ParsedItemData data = ItemRegistry.GetData(ingredientSplit[i]);
						key = ((data != null) ? data.QualifiedItemId : ("(O)" + ingredientSplit[i]));
					}
					int itemStack = Convert.ToInt32(ingredientSplit[i + 1]);
					int itemQuality = Convert.ToInt32(ingredientSplit[i + 2]);
					if (!bundlesIngredientsInfo.TryGetValue(key, out var ingredients))
					{
						ingredients = (bundlesIngredientsInfo[key] = new List<List<int>>());
					}
					ingredients.Add(new List<int> { bundleIndex, itemStack, itemQuality });
				}
			}
		}
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(warehouse, "warehouse").AddField(areasComplete, "areasComplete").AddField(numberOfStarsOnPlaque, "numberOfStarsOnPlaque")
			.AddField(newJunimoNoteCheckEvent, "newJunimoNoteCheckEvent")
			.AddField(restoreAreaCutsceneEvent, "restoreAreaCutsceneEvent")
			.AddField(areaCompleteRewardEvent, "areaCompleteRewardEvent")
			.AddField(missedRewardsChest, "missedRewardsChest")
			.AddField(showMissedRewardsChestEvent, "showMissedRewardsChestEvent")
			.AddField(missedRewardsChestVisible, "missedRewardsChestVisible");
		newJunimoNoteCheckEvent.onEvent += doCheckForNewJunimoNotes;
		restoreAreaCutsceneEvent.onEvent += doRestoreAreaCutscene;
		areaCompleteRewardEvent.onEvent += doAreaCompleteReward;
		showMissedRewardsChestEvent.onEvent += doShowMissedRewardsChest;
	}

	private void initAreaBundleConversions()
	{
		areaToBundleDictionary = new Dictionary<int, List<int>>();
		bundleToAreaDictionary = new Dictionary<int, int>();
		for (int i = 0; i < 7; i++)
		{
			areaToBundleDictionary.Add(i, new List<int>());
			NetMutex m = new NetMutex();
			bundleMutexes.Add(m);
			base.NetFields.AddField(m.NetFields, "m.NetFields");
		}
		foreach (KeyValuePair<string, string> v in Game1.netWorldState.Value.BundleData)
		{
			int bundleIndex = Convert.ToInt32(v.Key.Split('/')[1]);
			areaToBundleDictionary[getAreaNumberFromName(v.Key.Split('/')[0])].Add(bundleIndex);
			bundleToAreaDictionary.Add(bundleIndex, getAreaNumberFromName(v.Key.Split('/')[0]));
		}
	}

	public static int getAreaNumberFromName(string name)
	{
		switch (name)
		{
		case "Pantry":
			return 0;
		case "Crafts Room":
		case "CraftsRoom":
			return 1;
		case "FishTank":
		case "Fish Tank":
			return 2;
		case "Boiler Room":
		case "BoilerRoom":
			return 3;
		case "Vault":
			return 4;
		case "Bulletin":
		case "BulletinBoard":
		case "Bulletin Board":
			return 5;
		case "Abandoned Joja Mart":
			return 6;
		default:
			return -1;
		}
	}

	private Point getNotePosition(int area)
	{
		return area switch
		{
			0 => new Point(14, 5), 
			2 => new Point(40, 10), 
			1 => new Point(14, 23), 
			3 => new Point(63, 14), 
			4 => new Point(55, 6), 
			5 => new Point(46, 11), 
			_ => Point.Zero, 
		};
	}

	public void addJunimoNote(int area)
	{
		Point position = getNotePosition(area);
		if (!position.Equals(Vector2.Zero))
		{
			StaticTile[] tileFrames = getJunimoNoteTileFrames(area, map);
			string layer = ((area == 5) ? "Front" : "Buildings");
			map.RequireLayer(layer).Tiles[position.X, position.Y] = new AnimatedTile(map.RequireLayer(layer), tileFrames, 70L);
			Game1.currentLightSources.Add(new LightSource($"{"CommunityCenter"}_Area{area}", 4, new Vector2(position.X * 64, position.Y * 64), 1f, LightSource.LightContext.None, 0L, base.NameOrUniqueName));
			temporarySprites.Add(new TemporaryAnimatedSprite(6, new Vector2(position.X * 64, position.Y * 64), Color.White)
			{
				layerDepth = 1f,
				interval = 50f,
				motion = new Vector2(1f, 0f),
				acceleration = new Vector2(-0.005f, 0f)
			});
			temporarySprites.Add(new TemporaryAnimatedSprite(6, new Vector2(position.X * 64 - 12, position.Y * 64 - 12), Color.White)
			{
				scale = 0.75f,
				layerDepth = 1f,
				interval = 50f,
				motion = new Vector2(1f, 0f),
				acceleration = new Vector2(-0.005f, 0f),
				delayBeforeAnimationStart = 50
			});
			temporarySprites.Add(new TemporaryAnimatedSprite(6, new Vector2(position.X * 64 - 12, position.Y * 64 + 12), Color.White)
			{
				layerDepth = 1f,
				interval = 50f,
				motion = new Vector2(1f, 0f),
				acceleration = new Vector2(-0.005f, 0f),
				delayBeforeAnimationStart = 100
			});
			temporarySprites.Add(new TemporaryAnimatedSprite(6, new Vector2(position.X * 64, position.Y * 64), Color.White)
			{
				layerDepth = 1f,
				scale = 0.75f,
				interval = 50f,
				motion = new Vector2(1f, 0f),
				acceleration = new Vector2(-0.005f, 0f),
				delayBeforeAnimationStart = 150
			});
		}
	}

	public int numberOfCompleteBundles()
	{
		int number = 0;
		foreach (KeyValuePair<int, bool[]> v in bundles.Pairs)
		{
			number++;
			for (int i = 0; i < v.Value.Length; i++)
			{
				if (!v.Value[i])
				{
					number--;
					break;
				}
			}
		}
		return number;
	}

	public void addStarToPlaque()
	{
		numberOfStarsOnPlaque.Value++;
	}

	private string getMessageForAreaCompletion()
	{
		int areasComplete = getNumberOfAreasComplete();
		if (areasComplete >= 1 && areasComplete <= 6)
		{
			return Game1.content.LoadString("Strings\\Locations:CommunityCenter_AreaCompletion" + areasComplete, Game1.player.Name);
		}
		return "";
	}

	private int getNumberOfAreasComplete()
	{
		int complete = 0;
		for (int i = 0; i < areasComplete.Count; i++)
		{
			if (areasComplete[i])
			{
				complete++;
			}
		}
		return complete;
	}

	public Dictionary<int, bool[]> bundlesDict()
	{
		return bundles.Pairs.Select((KeyValuePair<int, bool[]> kvp) => new KeyValuePair<int, bool[]>(kvp.Key, kvp.Value.ToArray())).ToDictionary((KeyValuePair<int, bool[]> x) => x.Key, (KeyValuePair<int, bool[]> y) => y.Value);
	}

	/// <inheritdoc />
	public override bool performAction(string[] action, Farmer who, Location tileLocation)
	{
		if (who.IsLocalPlayer && ArgUtility.Get(action, 0) == "MissedRewards")
		{
			missedRewardsChest.Value.mutex.RequestLock(delegate
			{
				Game1.activeClickableMenu = new ItemGrabMenu(missedRewardsChest.Value.Items, reverseGrab: false, showReceivingMenu: true, null, null, null, rewardGrabbed, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true, allowRightClick: true, showOrganizeButton: false, 0, null, -1, this);
				Game1.activeClickableMenu.exitFunction = delegate
				{
					missedRewardsChest.Value.mutex.ReleaseLock();
					checkForMissedRewards();
				};
			});
			return true;
		}
		return base.performAction(action, who, tileLocation);
	}

	private void rewardGrabbed(Item item, Farmer who)
	{
		bundleRewards[item.SpecialVariable] = false;
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		switch (getTileIndexAt(tileLocation, "Buildings", "indoors"))
		{
		case 1799:
			if (numberOfCompleteBundles() > 2)
			{
				checkBundle(5);
			}
			return true;
		case 1824:
		case 1825:
		case 1826:
		case 1827:
		case 1828:
		case 1829:
		case 1830:
		case 1831:
		case 1832:
		case 1833:
			checkBundle(getAreaNumberFromLocation(who.Tile));
			return true;
		default:
			return base.checkAction(tileLocation, viewport, who);
		}
	}

	public void checkBundle(int area)
	{
		bundleMutexes[area].RequestLock(delegate
		{
			Game1.activeClickableMenu = new JunimoNoteMenu(area, bundlesDict());
		});
	}

	public void addJunimoNoteViewportTarget(int area)
	{
		if (junimoNotesViewportTargets == null)
		{
			junimoNotesViewportTargets = new List<int>();
		}
		if (!junimoNotesViewportTargets.Contains(area))
		{
			junimoNotesViewportTargets.Add(area);
		}
	}

	public void checkForNewJunimoNotes()
	{
		newJunimoNoteCheckEvent.Fire();
	}

	private void doCheckForNewJunimoNotes()
	{
		if (Game1.currentLocation != this)
		{
			return;
		}
		for (int i = 0; i < areasComplete.Count; i++)
		{
			if (!isJunimoNoteAtArea(i) && shouldNoteAppearInArea(i))
			{
				addJunimoNoteViewportTarget(i);
			}
		}
	}

	public bool isJunimoNoteAtArea(int area)
	{
		Point p = getNotePosition(area);
		if (area == 5)
		{
			return map.RequireLayer("Front").Tiles[p.X, p.Y] != null;
		}
		return map.RequireLayer("Buildings").Tiles[p.X, p.Y] != null;
	}

	public bool shouldNoteAppearInArea(int area)
	{
		bool isAreaComplete = true;
		for (int i = 0; i < areaToBundleDictionary[area].Count; i++)
		{
			foreach (int bundleIndex in areaToBundleDictionary[area])
			{
				if (bundles.TryGetValue(bundleIndex, out var bundleEntries))
				{
					int bundleLength = bundleEntries.Length / 3;
					for (int j = 0; j < bundleLength; j++)
					{
						if (!bundleEntries[j])
						{
							isAreaComplete = false;
							break;
						}
					}
				}
				if (!isAreaComplete)
				{
					break;
				}
			}
		}
		if (area >= 0 && !isAreaComplete)
		{
			switch (area)
			{
			case 1:
				return true;
			case 0:
			case 2:
				if (numberOfCompleteBundles() > 0)
				{
					return true;
				}
				break;
			case 3:
				if (numberOfCompleteBundles() > 1)
				{
					return true;
				}
				break;
			case 5:
				if (numberOfCompleteBundles() > 2)
				{
					return true;
				}
				break;
			case 4:
				if (numberOfCompleteBundles() > 3)
				{
					return true;
				}
				break;
			case 6:
				if (Utility.HasAnyPlayerSeenEvent("191393"))
				{
					return true;
				}
				break;
			}
		}
		return false;
	}

	public override void updateMap()
	{
		if (Game1.MasterPlayer.mailReceived.Contains("JojaMember"))
		{
			warehouse.Value = true;
			mapPath.Value = "Maps\\CommunityCenter_Joja";
		}
		base.updateMap();
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		if (areAllAreasComplete())
		{
			mapPath.Value = "Maps\\CommunityCenter_Refurbished";
			updateMap();
		}
		base.TransferDataFromSavedLocation(l);
	}

	protected override void resetSharedState()
	{
		base.resetSharedState();
		if (areAllAreasComplete())
		{
			mapPath.Value = "Maps\\CommunityCenter_Refurbished";
			addFishTank();
		}
		_isWatchingJunimoGoodbye = false;
		if (!Game1.MasterPlayer.mailReceived.Contains("JojaMember") && !areAllAreasComplete())
		{
			for (int i = 0; i < areasComplete.Count; i++)
			{
				if (shouldNoteAppearInArea(i))
				{
					characters.Add(new Junimo(new Vector2(getNotePosition(i).X, getNotePosition(i).Y + 2) * 64f, i));
				}
			}
		}
		numberOfStarsOnPlaque.Value = 0;
		for (int j = 0; j < areasComplete.Count; j++)
		{
			if (areasComplete[j])
			{
				numberOfStarsOnPlaque.Value++;
			}
		}
		checkForMissedRewards();
	}

	private void doShowMissedRewardsChest(bool isVisible)
	{
		int tileX = (int)missedRewardsChestTile.X;
		int tileY = (int)missedRewardsChestTile.Y;
		removeMapTile(tileX, tileY, "Buildings");
		if (isVisible)
		{
			setMapTile(tileX, tileY, 5, "Buildings", "indoors2", "MissedRewards");
		}
	}

	private void checkForMissedRewards()
	{
		HashSet<int> visited_areas = new HashSet<int>();
		bool hasUnclaimedRewards = false;
		missedRewardsChest.Value.Items.Clear();
		List<Item> rewards = new List<Item>();
		foreach (int key in bundleRewards.Keys)
		{
			int area = bundleToAreaDictionary[key];
			if (!bundleRewards[key] || areasComplete.Count <= area || !areasComplete[area] || visited_areas.Contains(area))
			{
				continue;
			}
			visited_areas.Add(area);
			hasUnclaimedRewards = true;
			rewards.Clear();
			JunimoNoteMenu.GetBundleRewards(area, rewards);
			foreach (Item item in rewards)
			{
				missedRewardsChest.Value.addItem(item);
			}
		}
		if (hasUnclaimedRewards != missedRewardsChestVisible.Value)
		{
			showMissedRewardsChestEvent.Fire(hasUnclaimedRewards);
			Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(Game1.random.Choose(5, 46), missedRewardsChestTile * 64f + new Vector2(16f, 16f), Color.White)
			{
				layerDepth = 1f
			});
			missedRewardsChestVisible.Value = hasUnclaimedRewards;
		}
	}

	public override void MakeMapModifications(bool force = false)
	{
		base.MakeMapModifications(force);
		if (!Game1.MasterPlayer.mailReceived.Contains("JojaMember") && !areAllAreasComplete())
		{
			for (int i = 0; i < areasComplete.Count; i++)
			{
				if (areasComplete[i])
				{
					loadArea(i, showEffects: false);
				}
				else if (shouldNoteAppearInArea(i))
				{
					addJunimoNote(i);
				}
			}
		}
		doShowMissedRewardsChest(missedRewardsChestVisible.Value);
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		if (!Game1.eventUp && !areAllAreasComplete())
		{
			Game1.changeMusicTrack("communityCenter");
		}
	}

	private int getAreaNumberFromLocation(Vector2 tileLocation)
	{
		for (int i = 0; i < areasComplete.Count; i++)
		{
			if (getAreaBounds(i).Contains((int)tileLocation.X, (int)tileLocation.Y))
			{
				return i;
			}
		}
		return -1;
	}

	private Microsoft.Xna.Framework.Rectangle getAreaBounds(int area)
	{
		return area switch
		{
			1 => new Microsoft.Xna.Framework.Rectangle(0, 12, 21, 17), 
			0 => new Microsoft.Xna.Framework.Rectangle(0, 0, 22, 11), 
			2 => new Microsoft.Xna.Framework.Rectangle(35, 4, 9, 9), 
			3 => new Microsoft.Xna.Framework.Rectangle(52, 9, 16, 12), 
			5 => new Microsoft.Xna.Framework.Rectangle(22, 13, 28, 9), 
			4 => new Microsoft.Xna.Framework.Rectangle(45, 0, 15, 9), 
			7 => new Microsoft.Xna.Framework.Rectangle(44, 10, 6, 3), 
			8 => new Microsoft.Xna.Framework.Rectangle(22, 4, 13, 9), 
			_ => Microsoft.Xna.Framework.Rectangle.Empty, 
		};
	}

	protected void removeJunimo()
	{
		characters.RemoveWhere((NPC npc) => npc is Junimo);
	}

	public override void cleanupBeforeSave()
	{
		removeJunimo();
	}

	public override void cleanupBeforePlayerExit()
	{
		base.cleanupBeforePlayerExit();
		if (farmers.Count <= 1)
		{
			removeJunimo();
		}
	}

	public bool isBundleComplete(int bundleIndex)
	{
		for (int i = 0; i < bundles[bundleIndex].Length; i++)
		{
			if (!bundles[bundleIndex][i])
			{
				return false;
			}
		}
		return true;
	}

	public bool couldThisIngredienteBeUsedInABundle(Object o)
	{
		if (!o.bigCraftable.Value)
		{
			if (bundlesIngredientsInfo.TryGetValue(o.QualifiedItemId, out var ingredientsById))
			{
				foreach (List<int> l in ingredientsById)
				{
					if (o.Quality >= l[2])
					{
						return true;
					}
				}
			}
			if (o.Category < 0 && bundlesIngredientsInfo.TryGetValue(o.Category.ToString(), out var ingredientsByCategory))
			{
				foreach (List<int> l2 in ingredientsByCategory)
				{
					if (o.Quality >= l2[2])
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public void areaCompleteReward(int whichArea)
	{
		areaCompleteRewardEvent.Fire(whichArea);
	}

	private void doAreaCompleteReward(int whichArea)
	{
		string mailReceivedID = "";
		switch (whichArea)
		{
		case 3:
			mailReceivedID = "ccBoilerRoom";
			break;
		case 0:
			mailReceivedID = "ccPantry";
			break;
		case 2:
			mailReceivedID = "ccFishTank";
			break;
		case 4:
			mailReceivedID = "ccVault";
			break;
		case 5:
			mailReceivedID = "ccBulletin";
			Game1.addMailForTomorrow("ccBulletinThankYou");
			break;
		case 1:
			mailReceivedID = "ccCraftsRoom";
			break;
		}
		if (mailReceivedID.Length > 0 && !Game1.player.mailReceived.Contains(mailReceivedID))
		{
			Game1.player.mailForTomorrow.Add(mailReceivedID + "%&NL&%");
		}
	}

	public void loadArea(int area, bool showEffects = true)
	{
		Microsoft.Xna.Framework.Rectangle areaToRefurbish = getAreaBounds(area);
		Map refurbishedMap = Game1.game1.xTileContent.Load<Map>("Maps\\CommunityCenter_Refurbished");
		ApplyMapOverride(refurbishedMap, $"CommunityCenter_Refurbished{area}", areaToRefurbish, areaToRefurbish);
		Layer refurbishedBuildingsLayer = refurbishedMap.RequireLayer("Buildings");
		Layer refurbishedFrontLayer = refurbishedMap.RequireLayer("Front");
		Layer refurbishedPathsLayer = refurbishedMap.RequireLayer("Paths");
		foreach (Point tile in areaToRefurbish.GetPoints())
		{
			int x = tile.X;
			int y = tile.Y;
			Tile fromTile = refurbishedBuildingsLayer.Tiles[x, y];
			if (fromTile != null)
			{
				adjustMapLightPropertiesForLamp(fromTile.TileIndex, x, y, "Buildings");
				if (Game1.player.currentLocation == this && Game1.player.TilePoint.X == x && Game1.player.TilePoint.Y == y)
				{
					Game1.player.Position = new Vector2(2080f, 576f);
				}
			}
			fromTile = refurbishedFrontLayer.Tiles[x, y];
			if (fromTile != null)
			{
				adjustMapLightPropertiesForLamp(fromTile.TileIndex, x, y, "Front");
			}
			fromTile = refurbishedPathsLayer.Tiles[x, y];
			if (fromTile != null && fromTile.TileIndex == 8)
			{
				Game1.currentLightSources.Add(new LightSource($"{"CommunityCenter"}_Area{area}_{tile.X}_{tile.Y}", 4, new Vector2(x * 64, y * 64), 2f, LightSource.LightContext.None, 0L, base.NameOrUniqueName));
			}
			if (showEffects && Game1.random.NextDouble() < 0.58 && refurbishedBuildingsLayer.Tiles[x, y] == null)
			{
				temporarySprites.Add(new TemporaryAnimatedSprite(6, new Vector2(x * 64, y * 64), Color.White)
				{
					layerDepth = 1f,
					interval = 50f,
					motion = new Vector2((float)Game1.random.Next(17) / 10f, 0f),
					acceleration = new Vector2(-0.005f, 0f),
					delayBeforeAnimationStart = Game1.random.Next(500)
				});
			}
		}
		if ((area == 5 || area == 8) && missedRewardsChestVisible.Value)
		{
			doShowMissedRewardsChest(isVisible: true);
		}
		switch (area)
		{
		case 5:
			loadArea(7);
			break;
		case 2:
			addFishTank();
			break;
		}
		addLightGlows();
	}

	public void addFishTank()
	{
		bool found = false;
		foreach (Furniture f in furniture)
		{
			if (f.QualifiedItemId == "(F)CCFishTank")
			{
				f.AllowLocalRemoval = false;
				found = true;
				break;
			}
		}
		if (!found)
		{
			FishTankFurniture f2 = new FishTankFurniture("CCFishTank", new Vector2(38f, 9f));
			f2.CanBeGrabbed = false;
			f2.AllowLocalRemoval = false;
			f2.Fragility = 2;
			f2.heldItems.Add(ItemRegistry.Create("(O)143"));
			f2.heldItems.Add(ItemRegistry.Create("(O)145"));
			f2.heldItems.Add(ItemRegistry.Create("(O)721"));
			furniture.Add(f2);
		}
	}

	public void restoreAreaCutscene(int whichArea)
	{
		restoreAreaCutsceneEvent.Fire(whichArea);
	}

	public void markAreaAsComplete(int area)
	{
		if (Game1.currentLocation == this)
		{
			areasComplete[area] = true;
		}
		if (areAllAreasComplete() && Game1.currentLocation == this)
		{
			_isWatchingJunimoGoodbye = true;
		}
	}

	private void doRestoreAreaCutscene(int whichArea)
	{
		markAreaAsComplete(whichArea);
		restoreAreaIndex = whichArea;
		restoreAreaPhase = 0;
		restoreAreaTimer = 1000;
		if (Game1.player.currentLocation == this)
		{
			Game1.freezeControls = true;
			Game1.changeMusicTrack("none");
		}
		checkForMissedRewards();
	}

	public override void updateEvenIfFarmerIsntHere(GameTime time, bool ignoreWasUpdatedFlush = false)
	{
		base.updateEvenIfFarmerIsntHere(time, ignoreWasUpdatedFlush);
		restoreAreaCutsceneEvent.Poll();
		newJunimoNoteCheckEvent.Poll();
		areaCompleteRewardEvent.Poll();
		showMissedRewardsChestEvent.Poll();
		foreach (NetMutex m in bundleMutexes)
		{
			m.Update(this);
			if (m.IsLockHeld() && Game1.activeClickableMenu == null)
			{
				m.ReleaseLock();
			}
		}
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		base.UpdateWhenCurrentLocation(time);
		missedRewardsChest.Value.updateWhenCurrentLocation(time);
		if (restoreAreaTimer > 0)
		{
			int old = restoreAreaTimer;
			restoreAreaTimer -= time.ElapsedGameTime.Milliseconds;
			switch (restoreAreaPhase)
			{
			case 0:
				if (restoreAreaTimer <= 0)
				{
					restoreAreaTimer = 3000;
					restoreAreaPhase = 1;
					if (Game1.player.currentLocation == this)
					{
						Game1.player.faceDirection(2);
						Game1.player.jump();
						Game1.player.jitterStrength = 1f;
						Game1.player.showFrame(94);
					}
				}
				break;
			case 1:
				if (Game1.IsMasterGame && Game1.random.NextDouble() < 0.4)
				{
					Vector2 v = Utility.getRandomPositionInThisRectangle(getAreaBounds(restoreAreaIndex), Game1.random);
					Junimo j2 = new Junimo(v * 64f, restoreAreaIndex, temporary: true);
					if (!isCollidingPosition(j2.GetBoundingBox(), Game1.viewport, j2))
					{
						characters.Add(j2);
						Game1.multiplayer.broadcastSprites(this, new TemporaryAnimatedSprite(Game1.random.Choose(5, 46), v * 64f + new Vector2(16f, 16f), Color.White)
						{
							layerDepth = 1f
						});
						localSound("tinyWhip", null, null);
					}
				}
				if (restoreAreaTimer <= 0)
				{
					restoreAreaTimer = 999999;
					restoreAreaPhase = 2;
					if (Game1.player.currentLocation != this)
					{
						break;
					}
					Game1.screenGlowOnce(Color.White, hold: true, 0.005f, 1f);
					Game1.playSound("wind", out buildUpSound);
					buildUpSound.SetVariable("Volume", 0f);
					buildUpSound.SetVariable("Frequency", 0f);
					Game1.player.jitterStrength = 2f;
					Game1.player.stopShowingFrame();
				}
				Game1.drawLighting = false;
				break;
			case 2:
				if (buildUpSound != null)
				{
					buildUpSound.SetVariable("Volume", Game1.screenGlowAlpha * 150f);
					buildUpSound.SetVariable("Frequency", Game1.screenGlowAlpha * 150f);
				}
				if (Game1.screenGlowAlpha >= Game1.screenGlowMax)
				{
					messageAlpha += 0.008f;
					messageAlpha = Math.Min(messageAlpha, 1f);
				}
				if ((Game1.screenGlowAlpha == Game1.screenGlowMax || Game1.currentLocation != this) && restoreAreaTimer > 5200)
				{
					restoreAreaTimer = 5200;
				}
				if (restoreAreaTimer < 5200 && Game1.random.NextDouble() < (double)((float)(5200 - restoreAreaTimer) / 10000f))
				{
					localSound(Game1.random.Choose("dustMeep", "junimoMeep1"), null, null);
				}
				if (restoreAreaTimer > 0)
				{
					break;
				}
				restoreAreaTimer = 2000;
				messageAlpha = 0f;
				restoreAreaPhase = 3;
				if (Game1.IsMasterGame)
				{
					characters.RemoveWhere((NPC npc) => npc is Junimo junimo && junimo.temporaryJunimo.Value);
				}
				if (Game1.player.currentLocation != this)
				{
					if (Game1.IsMasterGame)
					{
						loadArea(restoreAreaIndex);
						_mapSeatsDirty = true;
					}
					break;
				}
				Game1.screenGlowHold = false;
				loadArea(restoreAreaIndex);
				if (Game1.IsMasterGame)
				{
					_mapSeatsDirty = true;
				}
				buildUpSound?.Stop(AudioStopOptions.Immediate);
				localSound("wand", null, null);
				Game1.changeMusicTrack("junimoStarSong");
				localSound("woodyHit", null, null);
				Game1.flashAlpha = 1f;
				Game1.player.stopJittering();
				Game1.drawLighting = true;
				break;
			case 3:
				if (old > 1000 && restoreAreaTimer <= 1000)
				{
					Junimo j = getJunimoForArea(restoreAreaIndex);
					if (j != null && Game1.IsMasterGame)
					{
						if (!j.holdingBundle.Value)
						{
							j.Position = Utility.getRandomAdjacentOpenTile(Utility.PointToVector2(getNotePosition(restoreAreaIndex)), this) * 64f;
							int iter = 0;
							while (isCollidingPosition(j.GetBoundingBox(), Game1.viewport, j) && iter < 20)
							{
								Microsoft.Xna.Framework.Rectangle area_bounds = getAreaBounds(restoreAreaIndex);
								if (restoreAreaIndex == 5)
								{
									area_bounds = new Microsoft.Xna.Framework.Rectangle(44, 13, 6, 2);
								}
								j.Position = Utility.getRandomPositionInThisRectangle(area_bounds, Game1.random) * 64f;
								iter++;
							}
							if (iter < 20)
							{
								j.fadeBack();
							}
						}
						j.returnToJunimoHutToFetchStar(this);
					}
				}
				if (restoreAreaTimer <= 0 && !_isWatchingJunimoGoodbye)
				{
					Game1.freezeControls = false;
				}
				break;
			}
		}
		else if (Game1.activeClickableMenu == null)
		{
			List<int> list = junimoNotesViewportTargets;
			if (list != null && list.Count > 0 && !Game1.isViewportOnCustomPath())
			{
				setViewportToNextJunimoNoteTarget();
			}
		}
	}

	private void setViewportToNextJunimoNoteTarget()
	{
		if (junimoNotesViewportTargets.Count > 0)
		{
			Game1.freezeControls = true;
			int area = junimoNotesViewportTargets[0];
			Point p = getNotePosition(area);
			Game1.moveViewportTo(new Vector2(p.X, p.Y) * 64f, 5f, 2000, afterViewportGetsToJunimoNotePosition, setViewportToNextJunimoNoteTarget);
		}
		else
		{
			Game1.viewportFreeze = true;
			Game1.viewportHold = 10000;
			Game1.globalFadeToBlack(Game1.afterFadeReturnViewportToPlayer);
			Game1.freezeControls = false;
			Game1.afterViewport = null;
		}
	}

	private void afterViewportGetsToJunimoNotePosition()
	{
		int area = junimoNotesViewportTargets[0];
		junimoNotesViewportTargets.RemoveAt(0);
		addJunimoNote(area);
		localSound("reward", null, null);
	}

	public Junimo getJunimoForArea(int whichArea)
	{
		foreach (NPC character in characters)
		{
			if (character is Junimo junimo && junimo.whichArea.Value == whichArea)
			{
				return junimo;
			}
		}
		Junimo j = new Junimo(Vector2.Zero, whichArea);
		addCharacter(j);
		return j;
	}

	public bool areAllAreasComplete()
	{
		foreach (bool item in areasComplete)
		{
			if (!item)
			{
				return false;
			}
		}
		return true;
	}

	public void junimoGoodbyeDance()
	{
		getJunimoForArea(0).Position = new Vector2(23f, 11f) * 64f;
		getJunimoForArea(1).Position = new Vector2(27f, 11f) * 64f;
		getJunimoForArea(2).Position = new Vector2(24f, 12f) * 64f;
		getJunimoForArea(4).Position = new Vector2(26f, 12f) * 64f;
		getJunimoForArea(3).Position = new Vector2(28f, 12f) * 64f;
		getJunimoForArea(5).Position = new Vector2(25f, 11f) * 64f;
		for (int i = 0; i < areasComplete.Count; i++)
		{
			getJunimoForArea(i).stayStill();
			getJunimoForArea(i).faceDirection(1);
			getJunimoForArea(i).fadeBack();
			getJunimoForArea(i).IsInvisible = false;
			getJunimoForArea(i).setAlpha(1f);
		}
		Point playerPixel = Game1.player.StandingPixel;
		Game1.moveViewportTo(new Vector2(playerPixel.X, playerPixel.Y), 2f, 5000, startGoodbyeDance, endGoodbyeDance);
		Game1.viewportFreeze = false;
		Game1.freezeControls = true;
	}

	public void prepareForJunimoDance()
	{
		for (int i = 0; i < areasComplete.Count; i++)
		{
			Junimo junimoForArea = getJunimoForArea(i);
			junimoForArea.holdingBundle.Value = false;
			junimoForArea.holdingStar.Value = false;
			junimoForArea.controller = null;
			junimoForArea.Halt();
			junimoForArea.IsInvisible = true;
		}
		numberOfStarsOnPlaque.Value = 0;
		for (int j = 0; j < areasComplete.Count; j++)
		{
			if (areasComplete[j])
			{
				numberOfStarsOnPlaque.Value++;
			}
		}
	}

	private void startGoodbyeDance()
	{
		Game1.freezeControls = true;
		getJunimoForArea(0).Position = new Vector2(23f, 11f) * 64f;
		getJunimoForArea(1).Position = new Vector2(27f, 11f) * 64f;
		getJunimoForArea(2).Position = new Vector2(24f, 12f) * 64f;
		getJunimoForArea(4).Position = new Vector2(26f, 12f) * 64f;
		getJunimoForArea(3).Position = new Vector2(28f, 12f) * 64f;
		getJunimoForArea(5).Position = new Vector2(25f, 11f) * 64f;
		for (int i = 0; i < areasComplete.Count; i++)
		{
			getJunimoForArea(i).stayStill();
			getJunimoForArea(i).faceDirection(1);
			getJunimoForArea(i).fadeBack();
			getJunimoForArea(i).IsInvisible = false;
			getJunimoForArea(i).setAlpha(1f);
			getJunimoForArea(i).sayGoodbye();
		}
	}

	private void endGoodbyeDance()
	{
		for (int i = 0; i < areasComplete.Count; i++)
		{
			getJunimoForArea(i).fadeAway();
		}
		Game1.pauseThenDoFunction(3600, loadJunimoHut);
		Game1.freezeControls = true;
	}

	private void loadJunimoHut()
	{
		for (int i = 0; i < areasComplete.Count; i++)
		{
			getJunimoForArea(i).clearTextAboveHead();
		}
		loadArea(8);
		Game1.flashAlpha = 1f;
		localSound("wand", null, null);
		Game1.freezeControls = false;
		Game1.showGlobalMessage(Game1.content.LoadString("Strings\\Locations:CommunityCenter_JunimosReturned"));
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		for (int i = 0; i < numberOfStarsOnPlaque.Value; i++)
		{
			switch (i)
			{
			case 0:
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(2136f, 324f)), new Microsoft.Xna.Framework.Rectangle(354, 401, 7, 7), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.8f);
				break;
			case 1:
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(2136f, 364f)), new Microsoft.Xna.Framework.Rectangle(354, 401, 7, 7), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.8f);
				break;
			case 2:
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(2096f, 384f)), new Microsoft.Xna.Framework.Rectangle(354, 401, 7, 7), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.8f);
				break;
			case 3:
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(2056f, 364f)), new Microsoft.Xna.Framework.Rectangle(354, 401, 7, 7), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.8f);
				break;
			case 4:
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(2056f, 324f)), new Microsoft.Xna.Framework.Rectangle(354, 401, 7, 7), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.8f);
				break;
			case 5:
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(2096f, 308f)), new Microsoft.Xna.Framework.Rectangle(354, 401, 7, 7), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.8f);
				break;
			}
		}
		if (!Game1.eventUp)
		{
			return;
		}
		Furniture.isDrawingLocationFurniture = true;
		foreach (Furniture f in furniture)
		{
			if (f.QualifiedItemId == "(F)CCFishTank")
			{
				f.draw(b, -1, -1);
			}
		}
		Furniture.isDrawingLocationFurniture = false;
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		base.drawAboveAlwaysFrontLayer(b);
		if (messageAlpha > 0f)
		{
			Junimo j = getJunimoForArea(0);
			if (j != null)
			{
				b.Draw(j.Sprite.Texture, new Vector2(Game1.viewport.Width / 2 - 32, (float)(Game1.viewport.Height * 2) / 3f - 64f), new Microsoft.Xna.Framework.Rectangle((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 800.0) / 100 * 16, 0, 16, 16), Color.Lime * messageAlpha, 0f, new Vector2(j.Sprite.SpriteWidth * 4 / 2, (float)(j.Sprite.SpriteHeight * 4) * 3f / 4f) / 4f, Math.Max(0.2f, 1f) * 4f, j.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 1f);
			}
			b.DrawString(Game1.dialogueFont, "\"" + Game1.parseText(getMessageForAreaCompletion() + "\"", Game1.dialogueFont, 640), new Vector2(Game1.viewport.Width / 2 - 320, (float)(Game1.viewport.Height * 2) / 3f), Game1.textColor * messageAlpha * 0.6f);
		}
	}

	public static string getAreaNameFromNumber(int areaNumber)
	{
		return areaNumber switch
		{
			3 => "Boiler Room", 
			5 => "Bulletin Board", 
			1 => "Crafts Room", 
			2 => "Fish Tank", 
			0 => "Pantry", 
			4 => "Vault", 
			6 => "Abandoned Joja Mart", 
			_ => "", 
		};
	}

	public static string getAreaEnglishDisplayNameFromNumber(int areaNumber)
	{
		return Game1.content.LoadBaseString("Strings\\Locations:CommunityCenter_AreaName_" + getAreaNameFromNumber(areaNumber).Replace(" ", ""));
	}

	public static string getAreaDisplayNameFromNumber(int areaNumber)
	{
		return Game1.content.LoadString("Strings\\Locations:CommunityCenter_AreaName_" + getAreaNameFromNumber(areaNumber).Replace(" ", ""));
	}

	public static StaticTile[] getJunimoNoteTileFrames(int area, Map map)
	{
		TileSheet tileSheet = map.GetTileSheet("indoor") ?? map.RequireTileSheet(0, "indoors");
		if (area == 5)
		{
			Layer layer = map.RequireLayer("Front");
			return new StaticTile[13]
			{
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1741),
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1741),
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1741),
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1741),
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1741),
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1741),
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1741),
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1741),
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1741),
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1773),
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1805),
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1805),
				new StaticTile(layer, tileSheet, BlendMode.Alpha, 1773)
			};
		}
		Layer layer2 = map.RequireLayer("Buildings");
		return new StaticTile[20]
		{
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1833),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1833),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1833),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1833),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1833),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1833),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1833),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1833),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1833),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1832),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1824),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1825),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1826),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1827),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1828),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1829),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1830),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1831),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1832),
			new StaticTile(layer2, tileSheet, BlendMode.Alpha, 1833)
		};
	}
}
