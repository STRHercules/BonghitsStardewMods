using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Characters;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData.Crops;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Tools;

namespace StardewValley.TerrainFeatures;

public class HoeDirt : TerrainFeature
{
	private struct NeighborLoc
	{
		public readonly Vector2 Offset;

		public readonly byte Direction;

		public readonly byte InvDirection;

		public NeighborLoc(Vector2 a, byte b, byte c)
		{
			Offset = a;
			Direction = b;
			InvDirection = c;
		}
	}

	private struct Neighbor
	{
		public readonly HoeDirt feature;

		public readonly byte direction;

		public readonly byte invDirection;

		public Neighbor(HoeDirt a, byte b, byte c)
		{
			feature = a;
			direction = b;
			invDirection = c;
		}
	}

	public const float defaultShakeRate = (float)Math.PI / 80f;

	public const float maximumShake = (float)Math.PI / 8f;

	public const float shakeDecayRate = (float)Math.PI / 300f;

	public const byte N = 1;

	public const byte E = 2;

	public const byte S = 4;

	public const byte W = 8;

	public const byte Cardinals = 15;

	public static readonly Vector2 N_Offset = new Vector2(0f, -1f);

	public static readonly Vector2 E_Offset = new Vector2(1f, 0f);

	public static readonly Vector2 S_Offset = new Vector2(0f, 1f);

	public static readonly Vector2 W_Offset = new Vector2(-1f, 0f);

	public const float paddyGrowBonus = 0.25f;

	public const int dry = 0;

	public const int watered = 1;

	public const int invisible = 2;

	public const string fertilizerLowQualityID = "368";

	public const string fertilizerHighQualityID = "369";

	public const string waterRetentionSoilID = "370";

	public const string waterRetentionSoilQualityID = "371";

	public const string speedGroID = "465";

	public const string superSpeedGroID = "466";

	public const string hyperSpeedGroID = "918";

	public const string fertilizerDeluxeQualityID = "919";

	public const string waterRetentionSoilDeluxeID = "920";

	public const string fertilizerLowQualityQID = "(O)368";

	public const string fertilizerHighQualityQID = "(O)369";

	public const string waterRetentionSoilQID = "(O)370";

	public const string waterRetentionSoilQualityQID = "(O)371";

	public const string speedGroQID = "(O)465";

	public const string superSpeedGroQID = "(O)466";

	public const string hyperSpeedGroQID = "(O)918";

	public const string fertilizerDeluxeQualityQID = "(O)919";

	public const string waterRetentionSoilDeluxeQID = "(O)920";

	public static Texture2D lightTexture;

	public static Texture2D darkTexture;

	public static Texture2D snowTexture;

	private readonly NetRef<Crop> netCrop = new NetRef<Crop>();

	public static Dictionary<byte, int> drawGuide;

	[XmlElement("state")]
	public readonly NetInt state = new NetInt();

	/// <summary>The qualified or unqualified item ID of the fertilizer applied to this dirt, if any.</summary>
	/// <remarks>See also the helper methods like <see cref="M:StardewValley.TerrainFeatures.HoeDirt.HasFertilizer" />, <see cref="M:StardewValley.TerrainFeatures.HoeDirt.CanApplyFertilizer(System.String)" />, <see cref="M:StardewValley.TerrainFeatures.HoeDirt.GetFertilizerSpeedBoost" />, etc.</remarks>
	[XmlElement("fertilizer")]
	public readonly NetString fertilizer = new NetString();

	private bool shakeLeft;

	private float shakeRotation;

	private float maxShake;

	private float shakeRate;

	[XmlElement("c")]
	private readonly NetColor c = new NetColor(Color.White);

	private List<Action<GameLocation, Vector2>> queuedActions = new List<Action<GameLocation, Vector2>>();

	private byte neighborMask;

	private byte wateredNeighborMask;

	[XmlIgnore]
	public NetInt nearWaterForPaddy = new NetInt(-1);

	private byte drawSum;

	private int sourceRectPosition;

	private int wateredRectPosition;

	private Texture2D texture;

	private static readonly NeighborLoc[] _offsets = new NeighborLoc[4]
	{
		new NeighborLoc(N_Offset, 1, 4),
		new NeighborLoc(S_Offset, 4, 1),
		new NeighborLoc(E_Offset, 2, 8),
		new NeighborLoc(W_Offset, 8, 2)
	};

	private List<Neighbor> _neighbors = new List<Neighbor>();

	/// <inheritdoc />
	[XmlIgnore]
	public override GameLocation Location
	{
		get
		{
			return base.Location;
		}
		set
		{
			base.Location = value;
			if (netCrop.Value != null)
			{
				netCrop.Value.currentLocation = value;
			}
		}
	}

	/// <inheritdoc />
	public override Vector2 Tile
	{
		get
		{
			return base.Tile;
		}
		set
		{
			base.Tile = value;
			if (netCrop.Value != null)
			{
				netCrop.Value.tilePosition = value;
			}
		}
	}

	public Crop crop
	{
		get
		{
			return netCrop.Value;
		}
		set
		{
			netCrop.Value = value;
		}
	}

	/// <summary>The pot containing this dirt, if applicable.</summary>
	[XmlIgnore]
	public IndoorPot Pot { get; set; }

	public HoeDirt()
		: base(needsTick: true)
	{
		loadSprite();
		if (drawGuide == null)
		{
			populateDrawGuide();
		}
		initialize(Game1.currentLocation);
	}

	public HoeDirt(int startingState, GameLocation location = null)
		: this()
	{
		state.Value = startingState;
		Location = location ?? Game1.currentLocation;
		if (location != null)
		{
			initialize(location);
		}
	}

	public HoeDirt(int startingState, Crop crop)
		: this()
	{
		state.Value = startingState;
		this.crop = crop;
	}

	public override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(netCrop, "netCrop").AddField(state, "state").AddField(fertilizer, "fertilizer")
			.AddField(c, "c")
			.AddField(nearWaterForPaddy, "nearWaterForPaddy");
		state.fieldChangeVisibleEvent += delegate
		{
			OnAdded(Location, Tile);
		};
		netCrop.fieldChangeVisibleEvent += delegate
		{
			nearWaterForPaddy.Value = -1;
			updateNeighbors();
			if (netCrop.Value != null)
			{
				netCrop.Value.Dirt = this;
				netCrop.Value.currentLocation = Location;
				netCrop.Value.updateDrawMath(Tile);
			}
		};
		nearWaterForPaddy.Interpolated(interpolate: false, wait: false);
		netCrop.Interpolated(interpolate: false, wait: false);
		netCrop.OnConflictResolve += delegate(Crop rejected, Crop accepted)
		{
			if (Game1.IsMasterGame && rejected != null && rejected.netSeedIndex.Value != null)
			{
				queuedActions.Add(delegate(GameLocation gLocation, Vector2 tileLocation)
				{
					Vector2 vector = tileLocation * 64f;
					gLocation.debris.Add(new Debris(rejected.netSeedIndex.Value, vector, vector));
				});
				base.NeedsUpdate = true;
			}
		};
	}

	private void initialize(GameLocation location)
	{
		if (location == null)
		{
			location = Game1.currentLocation;
		}
		if (location == null)
		{
			return;
		}
		if (location is MineShaft mine)
		{
			int mineArea = mine.getMineArea();
			if (mine.GetAdditionalDifficulty() > 0)
			{
				if (mineArea == 0 || mineArea == 10)
				{
					c.Value = new Color(80, 100, 140) * 0.5f;
				}
			}
			else if (mineArea == 80)
			{
				c.Value = Color.MediumPurple * 0.4f;
			}
		}
		else if (location.GetSeason() == Season.Fall && location.IsOutdoors && !(location is Beach))
		{
			c.Value = new Color(250, 210, 240);
		}
		else if (location is VolcanoDungeon)
		{
			c.Value = Color.MediumPurple * 0.7f;
		}
	}

	public float getShakeRotation()
	{
		return shakeRotation;
	}

	public float getMaxShake()
	{
		return maxShake;
	}

	public override Rectangle getBoundingBox()
	{
		Vector2 tileLocation = Tile;
		return new Rectangle((int)(tileLocation.X * 64f), (int)(tileLocation.Y * 64f), 64, 64);
	}

	public override void doCollisionAction(Rectangle positionOfCollider, int speedOfCollision, Vector2 tileLocation, Character who)
	{
		if (crop != null && crop.currentPhase.Value != 0 && speedOfCollision > 0 && maxShake == 0f && positionOfCollider.Intersects(getBoundingBox()) && Utility.isOnScreen(Utility.Vector2ToPoint(tileLocation), 64, Location))
		{
			if (!(who is FarmAnimal))
			{
				Grass.PlayGrassSound();
			}
			shake((float)Math.PI / 8f / Math.Min(1f, 5f / (float)speedOfCollision) - ((speedOfCollision > 2) ? ((float)crop.currentPhase.Value * (float)Math.PI / 64f) : 0f), (float)Math.PI / 80f / Math.Min(1f, 5f / (float)speedOfCollision), (float)positionOfCollider.Center.X > tileLocation.X * 64f + 32f);
		}
		if (crop != null && crop.currentPhase.Value != 0 && who is Farmer { running: not false } player)
		{
			if (player.stats.Get("Book_Grass") != 0)
			{
				player.temporarySpeedBuff = -0.33f;
			}
			else
			{
				player.temporarySpeedBuff = -1f;
			}
		}
	}

	public void shake(float shake, float rate, bool left)
	{
		if (crop != null)
		{
			maxShake = shake * (crop.raisedSeeds.Value ? 0.6f : 1.5f);
			shakeRate = rate * 0.5f;
			shakeRotation = 0f;
			shakeLeft = left;
		}
		base.NeedsUpdate = true;
	}

	/// <summary>Whether this dirt contains a crop which needs water to grow further. To check whether it is watered, see <see cref="M:StardewValley.TerrainFeatures.HoeDirt.isWatered" />.</summary>
	public bool needsWatering()
	{
		if (crop != null && (!readyForHarvest() || crop.RegrowsAfterHarvest()))
		{
			return crop.GetData()?.NeedsWatering ?? true;
		}
		return false;
	}

	/// <summary>Whether this dirt is watered.</summary>
	/// <remarks>See also <see cref="M:StardewValley.TerrainFeatures.HoeDirt.needsWatering" />.</remarks>
	public bool isWatered()
	{
		return state.Value == 1;
	}

	public static void populateDrawGuide()
	{
		drawGuide = new Dictionary<byte, int>
		{
			[0] = 0,
			[8] = 15,
			[2] = 13,
			[1] = 12,
			[4] = 4,
			[9] = 11,
			[3] = 9,
			[5] = 8,
			[6] = 1,
			[12] = 3,
			[10] = 14,
			[7] = 5,
			[15] = 6,
			[13] = 7,
			[11] = 10,
			[14] = 2
		};
	}

	public override void loadSprite()
	{
		if (lightTexture == null)
		{
			try
			{
				lightTexture = Game1.content.Load<Texture2D>("TerrainFeatures\\hoeDirt");
			}
			catch (Exception)
			{
			}
		}
		if (darkTexture == null)
		{
			try
			{
				darkTexture = Game1.content.Load<Texture2D>("TerrainFeatures\\hoeDirtDark");
			}
			catch (Exception)
			{
			}
		}
		if (snowTexture == null)
		{
			try
			{
				snowTexture = Game1.content.Load<Texture2D>("TerrainFeatures\\hoeDirtSnow");
			}
			catch (Exception)
			{
			}
		}
		nearWaterForPaddy.Value = -1;
		crop?.updateDrawMath(Tile);
	}

	/// <inheritdoc />
	public override bool isPassable(Character c = null)
	{
		if (crop != null && crop.raisedSeeds.Value)
		{
			return c is JunimoHarvester;
		}
		return true;
	}

	public bool readyForHarvest()
	{
		if (crop != null && (!crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0) && crop.currentPhase.Value >= crop.phaseDays.Count - 1 && !crop.dead.Value)
		{
			if (crop.forageCrop.Value)
			{
				return crop.whichForageCrop.Value != "2";
			}
			return true;
		}
		return false;
	}

	public override bool performUseAction(Vector2 tileLocation)
	{
		if (crop != null)
		{
			bool harvestable = crop.currentPhase.Value >= crop.phaseDays.Count - 1 && (!crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0);
			HarvestMethod harvestMethod = crop.GetHarvestMethod();
			if (Game1.player.CurrentTool != null && Game1.player.CurrentTool.isScythe() && Game1.player.CurrentTool.ItemId == "66")
			{
				harvestMethod = HarvestMethod.Scythe;
			}
			switch (harvestMethod)
			{
			case HarvestMethod.Grab:
				if (crop.harvest((int)tileLocation.X, (int)tileLocation.Y, this))
				{
					GameLocation location = Location;
					if (location is IslandLocation && Game1.random.NextDouble() < 0.05)
					{
						Game1.player.team.RequestLimitedNutDrops("IslandFarming", location, (int)tileLocation.X * 64, (int)tileLocation.Y * 64, 5);
					}
					destroyCrop(showAnimation: false);
					return true;
				}
				break;
			case HarvestMethod.Scythe:
			{
				if (!readyForHarvest())
				{
					break;
				}
				Tool currentTool = Game1.player.CurrentTool;
				if (currentTool != null && currentTool.isScythe())
				{
					Game1.player.CanMove = false;
					Game1.player.UsingTool = true;
					Game1.player.canReleaseTool = true;
					Game1.player.Halt();
					try
					{
						Game1.player.CurrentTool.beginUsing(Game1.currentLocation, (int)Game1.player.lastClick.X, (int)Game1.player.lastClick.Y, Game1.player);
					}
					catch (Exception)
					{
					}
					((MeleeWeapon)Game1.player.CurrentTool).setFarmerAnimating(Game1.player);
				}
				else if (Game1.didPlayerJustClickAtAll(ignoreNonMouseHeldInput: true))
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HoeDirt.cs.13915"));
				}
				break;
			}
			}
			return harvestable;
		}
		return false;
	}

	public bool plant(string itemId, Farmer who, bool isFertilizer)
	{
		GameLocation location = Location;
		if (isFertilizer)
		{
			if (!CanApplyFertilizer(itemId))
			{
				return false;
			}
			fertilizer.Value = ItemRegistry.QualifyItemId(itemId) ?? itemId;
			applySpeedIncreases(who);
			location.playSound("dirtyHit", null, null);
			return true;
		}
		Season season = location.GetSeason();
		Point tilePos = Utility.Vector2ToPoint(Tile);
		itemId = Crop.ResolveSeedId(itemId, location);
		if (!Crop.TryGetData(itemId, out var cropData) || cropData.Seasons.Count == 0)
		{
			return false;
		}
		Object obj;
		bool isGardenPot = location.objects.TryGetValue(Tile, out obj) && obj is IndoorPot;
		bool isIndoorPot = isGardenPot && !location.IsOutdoors;
		if (!who.currentLocation.CheckItemPlantRules(itemId, isGardenPot, isIndoorPot || (location.GetData()?.CanPlantHere ?? location.IsFarm), out var deniedMessage))
		{
			if (Game1.didPlayerJustClickAtAll(ignoreNonMouseHeldInput: true))
			{
				if (deniedMessage == null && location.NameOrUniqueName != "Farm")
				{
					Farm farm = Game1.getFarm();
					if (farm.CheckItemPlantRules(itemId, isGardenPot, farm.GetData()?.CanPlantHere ?? true, out var _))
					{
						deniedMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:HoeDirt.cs.13919");
					}
				}
				if (deniedMessage == null)
				{
					deniedMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:HoeDirt.cs.13925");
				}
				Game1.showRedMessage(deniedMessage);
			}
			return false;
		}
		if (!isIndoorPot && !who.currentLocation.CanPlantSeedsHere(itemId, tilePos.X, tilePos.Y, isGardenPot, out deniedMessage))
		{
			if (Game1.didPlayerJustClickAtAll(ignoreNonMouseHeldInput: true))
			{
				if (deniedMessage == null)
				{
					deniedMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:HoeDirt.cs.13925");
				}
				Game1.showRedMessage(deniedMessage);
			}
			return false;
		}
		if (isIndoorPot || location.SeedsIgnoreSeasonsHere() || !((!(cropData.Seasons?.Contains(season))) ?? true))
		{
			crop = new Crop(itemId, tilePos.X, tilePos.Y, Location);
			if (crop.raisedSeeds.Value)
			{
				location.playSound("stoneStep", null, null);
			}
			location.playSound("dirtyHit", null, null);
			Game1.stats.SeedsSown++;
			applySpeedIncreases(who);
			nearWaterForPaddy.Value = -1;
			if (hasPaddyCrop() && paddyWaterCheck())
			{
				state.Value = 1;
				updateNeighbors();
			}
			return true;
		}
		if (Game1.didPlayerJustClickAtAll(ignoreNonMouseHeldInput: true))
		{
			string errorKey = (((!(cropData.Seasons?.Contains(season))) ?? false) ? "Strings\\StringsFromCSFiles:HoeDirt.cs.13924" : "Strings\\StringsFromCSFiles:HoeDirt.cs.13925");
			Game1.showRedMessage(Game1.content.LoadString(errorKey));
		}
		return false;
	}

	public void applySpeedIncreases(Farmer who)
	{
		if (crop == null)
		{
			return;
		}
		bool paddy_bonus = Location != null && paddyWaterCheck();
		float fertilizerSpeedBoost = GetFertilizerSpeedBoost();
		if (!(fertilizerSpeedBoost != 0f || who.professions.Contains(5) || paddy_bonus))
		{
			return;
		}
		crop.ResetPhaseDays();
		int totalDaysOfCropGrowth = 0;
		for (int i = 0; i < crop.phaseDays.Count - 1; i++)
		{
			totalDaysOfCropGrowth += crop.phaseDays[i];
		}
		float speedIncrease = fertilizerSpeedBoost;
		if (paddy_bonus)
		{
			speedIncrease += 0.25f;
		}
		if (who.professions.Contains(5))
		{
			speedIncrease += 0.1f;
		}
		int daysToRemove = (int)Math.Ceiling((float)totalDaysOfCropGrowth * speedIncrease);
		int tries = 0;
		while (daysToRemove > 0 && tries < 3)
		{
			for (int j = 0; j < crop.phaseDays.Count; j++)
			{
				if ((j > 0 || crop.phaseDays[j] > 1) && crop.phaseDays[j] != 99999 && crop.phaseDays[j] > 0)
				{
					crop.phaseDays[j]--;
					daysToRemove--;
				}
				if (daysToRemove <= 0)
				{
					break;
				}
			}
			tries++;
		}
	}

	public void destroyCrop(bool showAnimation)
	{
		GameLocation location = Location;
		if (crop != null && showAnimation && location != null)
		{
			Vector2 tileLocation = Tile;
			if (crop.currentPhase.Value < 1 && !crop.dead.Value)
			{
				Game1.multiplayer.broadcastSprites(Game1.player.currentLocation, new TemporaryAnimatedSprite(12, tileLocation * 64f, Color.White));
				location.playSound("dirtyHit", tileLocation, null);
			}
			else
			{
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(50, tileLocation * 64f, crop.dead.Value ? new Color(207, 193, 43) : Color.ForestGreen));
			}
		}
		crop = null;
		nearWaterForPaddy.Value = -1;
		if (location != null)
		{
			updateNeighbors();
		}
	}

	public override bool performToolAction(Tool t, int damage, Vector2 tileLocation)
	{
		GameLocation location = Location;
		if (t != null)
		{
			if (!(t is Hoe))
			{
				if (!(t is Pickaxe))
				{
					if (t is WateringCan)
					{
						if (crop == null || !crop.forageCrop.Value || crop.whichForageCrop.Value != "2")
						{
							state.Value = 1;
						}
						goto IL_0379;
					}
				}
				else if (crop == null)
				{
					return true;
				}
				if (t.isScythe())
				{
					Crop obj = crop;
					if ((obj != null && obj.GetHarvestMethod() == HarvestMethod.Scythe) || (crop != null && t.ItemId == "66"))
					{
						if (crop.indexOfHarvest.Value == "771" && t.hasEnchantmentOfType<HaymakerEnchantment>())
						{
							for (int i = 0; i < 2; i++)
							{
								Game1.createItemDebris(ItemRegistry.Create("(O)771"), new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f + 32f), -1);
							}
						}
						if (crop.harvest((int)tileLocation.X, (int)tileLocation.Y, this, null, isForcedScytheHarvest: true))
						{
							if (location is IslandLocation && Game1.random.NextDouble() < 0.05)
							{
								Game1.player.team.RequestLimitedNutDrops("IslandFarming", location, (int)tileLocation.X * 64, (int)tileLocation.Y * 64, 5);
							}
							destroyCrop(showAnimation: true);
						}
					}
					if (crop != null && crop.dead.Value)
					{
						destroyCrop(showAnimation: true);
					}
					if (crop == null && t.ItemId == "66" && location.objects.TryGetValue(tileLocation, out var tileObj) && tileObj.isForage())
					{
						Farmer player = t.getLastFarmerToUse() ?? Game1.player;
						tileObj.Quality = location.GetHarvestSpawnedObjectQuality(player, tileObj.isForage(), tileObj.TileLocation);
						Vector2 spawnPosition = new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f + 32f);
						Game1.createItemDebris(tileObj, spawnPosition, -1);
						location.OnHarvestedForage(player, tileObj);
						location.objects.Remove(tileLocation);
						if (player.professions.Contains(13) && Game1.random.NextDouble() < 0.2)
						{
							Object extraDrop = (Object)tileObj.getOne();
							extraDrop.Quality = location.GetHarvestSpawnedObjectQuality(player, extraDrop.isForage(), extraDrop.TileLocation);
							Game1.createItemDebris(extraDrop, spawnPosition, -1);
							location.OnHarvestedForage(player, extraDrop);
						}
					}
				}
				else if (t.isHeavyHitter() && !(t is MeleeWeapon) && crop != null)
				{
					destroyCrop(showAnimation: true);
				}
			}
			else if (crop != null && crop.hitWithHoe((int)tileLocation.X, (int)tileLocation.Y, location, this))
			{
				if (crop.forageCrop.Value && crop.whichForageCrop.Value == "2" && t.getLastFarmerToUse() != null)
				{
					t.getLastFarmerToUse().gainExperience(2, 7);
				}
				destroyCrop(showAnimation: true);
			}
			goto IL_0379;
		}
		if (damage > 0 && crop != null)
		{
			if (damage == 50)
			{
				crop.Kill();
			}
			else
			{
				destroyCrop(showAnimation: true);
			}
		}
		goto IL_03cd;
		IL_0379:
		shake((float)Math.PI / 32f, (float)Math.PI / 40f, tileLocation.X * 64f < Game1.player.Position.X);
		goto IL_03cd;
		IL_03cd:
		return false;
	}

	public bool canPlantThisSeedHere(string itemId, bool isFertilizer = false)
	{
		if (isFertilizer)
		{
			return CanApplyFertilizer(itemId);
		}
		if (crop == null)
		{
			Season season = Location.GetSeason();
			itemId = Crop.ResolveSeedId(itemId, Location);
			if (Crop.TryGetData(itemId, out var cropData))
			{
				if (cropData.Seasons.Count == 0)
				{
					return false;
				}
				if (!Game1.currentLocation.IsOutdoors || Game1.currentLocation.SeedsIgnoreSeasonsHere() || cropData.Seasons.Contains(season))
				{
					if (cropData.IsRaised && Utility.doesRectangleIntersectTile(Game1.player.GetBoundingBox(), (int)Tile.X, (int)Tile.Y))
					{
						return false;
					}
					return true;
				}
				switch (itemId)
				{
				case "309":
				case "310":
				case "311":
					return true;
				}
				if (Game1.didPlayerJustClickAtAll() && !Game1.doesHUDMessageExist(Game1.content.LoadString("Strings\\StringsFromCSFiles:HoeDirt.cs.13924")))
				{
					Game1.playSound("cancel", null);
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HoeDirt.cs.13924"));
				}
			}
		}
		return false;
	}

	public override void performPlayerEntryAction()
	{
		base.performPlayerEntryAction();
		crop?.updateDrawMath(Tile);
	}

	public override bool tickUpdate(GameTime time)
	{
		foreach (Action<GameLocation, Vector2> queuedAction in queuedActions)
		{
			queuedAction(Location, Tile);
		}
		queuedActions.Clear();
		if (maxShake > 0f)
		{
			if (shakeLeft)
			{
				shakeRotation -= shakeRate;
				if (Math.Abs(shakeRotation) >= maxShake)
				{
					shakeLeft = false;
				}
			}
			else
			{
				shakeRotation += shakeRate;
				if (shakeRotation >= maxShake)
				{
					shakeLeft = true;
					shakeRotation -= shakeRate;
				}
			}
			maxShake = Math.Max(0f, maxShake - (float)Math.PI / 300f);
		}
		else
		{
			shakeRotation /= 2f;
			if (shakeRotation <= 0.01f)
			{
				base.NeedsUpdate = false;
				shakeRotation = 0f;
			}
		}
		if (state.Value == 2)
		{
			return crop == null;
		}
		return false;
	}

	/// <summary>Get whether this dirt contains a crop which should be planted near water.</summary>
	public bool hasPaddyCrop()
	{
		if (crop != null)
		{
			return crop.isPaddyCrop();
		}
		return false;
	}

	/// <summary>Get whether this is a paddy crop planted near water, so it should be watered automatically.</summary>
	/// <param name="forceUpdate">Whether to recheck the surrounding map area instead of using the cached value.</param>
	public bool paddyWaterCheck(bool forceUpdate = false)
	{
		if (!forceUpdate && nearWaterForPaddy.Value >= 0)
		{
			return nearWaterForPaddy.Value == 1;
		}
		if (!hasPaddyCrop())
		{
			nearWaterForPaddy.Value = 0;
			return false;
		}
		Vector2 tile_location = Tile;
		if (Location.getObjectAtTile((int)tile_location.X, (int)tile_location.Y) is IndoorPot)
		{
			nearWaterForPaddy.Value = 0;
			return false;
		}
		int range = 3;
		for (int x_offset = -range; x_offset <= range; x_offset++)
		{
			for (int y_offset = -range; y_offset <= range; y_offset++)
			{
				if (Location.isWaterTile((int)(tile_location.X + (float)x_offset), (int)(tile_location.Y + (float)y_offset)))
				{
					nearWaterForPaddy.Value = 1;
					return true;
				}
			}
		}
		nearWaterForPaddy.Value = 0;
		return false;
	}

	public override void dayUpdate()
	{
		GameLocation environment = Location;
		int num;
		if (hasPaddyCrop())
		{
			num = (paddyWaterCheck(forceUpdate: true) ? 1 : 0);
			if (num != 0 && state.Value == 0)
			{
				state.Value = 1;
			}
		}
		else
		{
			num = 0;
		}
		if (crop != null)
		{
			crop.newDay(state.Value);
			if (environment.isOutdoors.Value && environment.GetSeason() == Season.Winter && crop != null && !crop.isWildSeedCrop() && !crop.IsInSeason(environment))
			{
				destroyCrop(showAnimation: false);
			}
		}
		if (num == 0 && !Game1.random.NextBool(GetFertilizerWaterRetentionChance()))
		{
			state.Value = 0;
		}
		if (environment.IsGreenhouse)
		{
			c.Value = Color.White;
		}
	}

	/// <inheritdoc />
	public override bool seasonUpdate(bool onLoad)
	{
		GameLocation location = Location;
		if (!onLoad && !location.SeedsIgnoreSeasonsHere() && (crop == null || crop.dead.Value || !crop.IsInSeason(location)))
		{
			fertilizer.Value = null;
		}
		if (location.GetSeason() == Season.Fall && !location.IsGreenhouse)
		{
			c.Value = new Color(250, 210, 240);
		}
		else
		{
			c.Value = Color.White;
		}
		texture = null;
		return false;
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 positionOnScreen, Vector2 tileLocation, float scale, float layerDepth)
	{
		byte drawSum = 0;
		Vector2 surroundingLocations = tileLocation;
		surroundingLocations.X += 1f;
		Farm farm = Game1.getFarm();
		if (farm.terrainFeatures.TryGetValue(surroundingLocations, out var rightFeature) && rightFeature is HoeDirt)
		{
			drawSum += 2;
		}
		surroundingLocations.X -= 2f;
		if (farm.terrainFeatures.TryGetValue(surroundingLocations, out var leftFeature) && leftFeature is HoeDirt)
		{
			drawSum += 8;
		}
		surroundingLocations.X += 1f;
		surroundingLocations.Y += 1f;
		if (Game1.currentLocation.terrainFeatures.TryGetValue(surroundingLocations, out var downFeature) && downFeature is HoeDirt)
		{
			drawSum += 4;
		}
		surroundingLocations.Y -= 2f;
		if (farm.terrainFeatures.TryGetValue(surroundingLocations, out var upFeature) && upFeature is HoeDirt)
		{
			drawSum++;
		}
		int sourceRectPosition = drawGuide[drawSum];
		spriteBatch.Draw(lightTexture, positionOnScreen, new Rectangle(sourceRectPosition % 4 * 64, sourceRectPosition / 4 * 64, 64, 64), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth + positionOnScreen.Y / 20000f);
		crop?.drawInMenu(spriteBatch, positionOnScreen + new Vector2(64f * scale, 64f * scale), Color.White, 0f, scale, layerDepth + (positionOnScreen.Y + 64f * scale) / 20000f);
	}

	public override void draw(SpriteBatch spriteBatch)
	{
		DrawOptimized(spriteBatch, spriteBatch, spriteBatch);
	}

	public void DrawOptimized(SpriteBatch dirt_batch, SpriteBatch fert_batch, SpriteBatch crop_batch)
	{
		int state = this.state.Value;
		Vector2 tileLocation = Tile;
		if (state != 2 && (dirt_batch != null || fert_batch != null))
		{
			if (dirt_batch != null && texture == null)
			{
				texture = ((Game1.currentLocation.Name.Equals("Mountain") || Game1.currentLocation.Name.Equals("Mine") || (Game1.currentLocation is MineShaft mine && mine.shouldShowDarkHoeDirt()) || Game1.currentLocation is VolcanoDungeon) ? darkTexture : lightTexture);
				if ((Game1.currentLocation.GetSeason() == Season.Winter && !Game1.currentLocation.SeedsIgnoreSeasonsHere() && !(Game1.currentLocation is MineShaft)) || (Game1.currentLocation is MineShaft shaft && shaft.shouldUseSnowTextureHoeDirt()))
				{
					texture = snowTexture;
				}
			}
			Vector2 drawPos = Game1.GlobalToLocal(Game1.viewport, tileLocation * 64f);
			if (dirt_batch != null)
			{
				dirt_batch.Draw(texture, drawPos, new Rectangle(sourceRectPosition % 4 * 16, sourceRectPosition / 4 * 16, 16, 16), c.Value, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-08f);
				if (state == 1)
				{
					dirt_batch.Draw(texture, drawPos, new Rectangle(wateredRectPosition % 4 * 16 + (paddyWaterCheck() ? 128 : 64), wateredRectPosition / 4 * 16, 16, 16), c.Value, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1.2E-08f);
				}
			}
			if (fert_batch != null && HasFertilizer())
			{
				fert_batch.Draw(Game1.mouseCursors, drawPos, GetFertilizerSourceRect(), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1.9E-08f);
			}
		}
		if (crop != null && crop_batch != null)
		{
			crop.draw(crop_batch, tileLocation, (state == 1 && crop.currentPhase.Value == 0 && crop.shouldDrawDarkWhenWatered()) ? (new Color(180, 100, 200) * 1f) : Color.White, shakeRotation);
		}
	}

	/// <summary>Get whether the dirt has any fertilizer applied.</summary>
	public virtual bool HasFertilizer()
	{
		if (fertilizer.Value != null)
		{
			return fertilizer.Value != "0";
		}
		return false;
	}

	/// <summary>Get whether a player can apply the given fertilizer to this dirt.</summary>
	/// <param name="fertilizerId">The fertilizer item ID.</param>
	public virtual bool CanApplyFertilizer(string fertilizerId)
	{
		return CheckApplyFertilizerRules(fertilizerId) == HoeDirtFertilizerApplyStatus.Okay;
	}

	/// <summary>Get a status which indicates whether fertilizer can be applied to this dirt, and the reason it can't if applicable.</summary>
	/// <param name="fertilizerId">The fertilizer item ID.</param>
	public virtual HoeDirtFertilizerApplyStatus CheckApplyFertilizerRules(string fertilizerId)
	{
		if (HasFertilizer())
		{
			fertilizerId = ItemRegistry.QualifyItemId(fertilizerId);
			if (!(fertilizerId == ItemRegistry.QualifyItemId(fertilizer.Value)))
			{
				return HoeDirtFertilizerApplyStatus.HasAnotherFertilizer;
			}
			return HoeDirtFertilizerApplyStatus.HasThisFertilizer;
		}
		if (crop != null && crop.currentPhase.Value != 0 && (fertilizerId == "(O)368" || fertilizerId == "(O)369"))
		{
			return HoeDirtFertilizerApplyStatus.CropAlreadySprouted;
		}
		return HoeDirtFertilizerApplyStatus.Okay;
	}

	/// <summary>Get the crop growth speed boost from fertilizers applied to this dirt.</summary>
	public virtual float GetFertilizerSpeedBoost()
	{
		switch (fertilizer.Value)
		{
		case "465":
		case "(O)465":
			return 0.1f;
		case "466":
		case "(O)466":
			return 0.25f;
		case "918":
		case "(O)918":
			return 0.33f;
		default:
			return 0f;
		}
	}

	/// <summary>Get the water retention chance from fertilizers applied to this dirt, as a value between 0 (no change) and 1 (100% chance of staying watered).</summary>
	public virtual float GetFertilizerWaterRetentionChance()
	{
		switch (fertilizer.Value)
		{
		case "370":
		case "(O)370":
			return 0.33f;
		case "371":
		case "(O)371":
			return 0.66f;
		case "920":
		case "(O)920":
			return 1f;
		default:
			return 0f;
		}
	}

	/// <summary>Get the quality boost level from fertilizers applied to this dirt, which influences the chance of producing a higher-quality crop.</summary>
	/// <remarks>See <see cref="M:StardewValley.Crop.harvest(System.Int32,System.Int32,StardewValley.TerrainFeatures.HoeDirt,StardewValley.Characters.JunimoHarvester,System.Boolean)" /> for the quality boost logic.</remarks>
	public virtual int GetFertilizerQualityBoostLevel()
	{
		switch (fertilizer.Value)
		{
		case "368":
		case "(O)368":
			return 1;
		case "369":
		case "(O)369":
			return 2;
		case "919":
		case "(O)919":
			return 3;
		default:
			return 0;
		}
	}

	/// <summary>Get the pixel area within the dirt spritesheet to draw for any fertilizer applied to this dirt.</summary>
	public virtual Rectangle GetFertilizerSourceRect()
	{
		string value = fertilizer.Value;
		if (value != null)
		{
			int length = value.Length;
			if (length != 3)
			{
				if (length == 6)
				{
					switch (value[5])
					{
					case '9':
						break;
					case '0':
						goto IL_015c;
					case '1':
						goto IL_0178;
					case '5':
						goto IL_0187;
					case '6':
						goto IL_0196;
					case '8':
						goto IL_01a5;
					default:
						goto IL_01d4;
					}
					if (value == "(O)369")
					{
						goto IL_01b4;
					}
					if (value == "(O)919")
					{
						goto IL_01d0;
					}
				}
			}
			else
			{
				switch (value[2])
				{
				case '9':
					break;
				case '0':
					goto IL_00c7;
				case '1':
					goto IL_00ec;
				case '5':
					goto IL_0101;
				case '6':
					goto IL_0116;
				case '8':
					goto IL_012b;
				default:
					goto IL_01d4;
				}
				if (value == "369")
				{
					goto IL_01b4;
				}
				if (value == "919")
				{
					goto IL_01d0;
				}
			}
		}
		goto IL_01d4;
		IL_0196:
		if (value == "(O)466")
		{
			goto IL_01c8;
		}
		goto IL_01d4;
		IL_01a5:
		if (value == "(O)918")
		{
			goto IL_01cc;
		}
		goto IL_01d4;
		IL_00c7:
		if (value == "370")
		{
			goto IL_01b8;
		}
		if (value == "920")
		{
			goto IL_01c0;
		}
		goto IL_01d4;
		IL_01d6:
		int fertilizerIndex;
		return new Rectangle(173 + fertilizerIndex / 3 * 16, 462 + fertilizerIndex % 3 * 16, 16, 16);
		IL_00ec:
		if (value == "371")
		{
			goto IL_01bc;
		}
		goto IL_01d4;
		IL_0101:
		if (value == "465")
		{
			goto IL_01c4;
		}
		goto IL_01d4;
		IL_0116:
		if (value == "466")
		{
			goto IL_01c8;
		}
		goto IL_01d4;
		IL_012b:
		if (value == "918")
		{
			goto IL_01cc;
		}
		goto IL_01d4;
		IL_01d0:
		fertilizerIndex = 2;
		goto IL_01d6;
		IL_01d4:
		fertilizerIndex = 0;
		goto IL_01d6;
		IL_01c8:
		fertilizerIndex = 7;
		goto IL_01d6;
		IL_01cc:
		fertilizerIndex = 8;
		goto IL_01d6;
		IL_015c:
		if (value == "(O)370")
		{
			goto IL_01b8;
		}
		if (value == "(O)920")
		{
			goto IL_01c0;
		}
		goto IL_01d4;
		IL_01b8:
		fertilizerIndex = 3;
		goto IL_01d6;
		IL_01b4:
		fertilizerIndex = 1;
		goto IL_01d6;
		IL_01c0:
		fertilizerIndex = 5;
		goto IL_01d6;
		IL_0178:
		if (value == "(O)371")
		{
			goto IL_01bc;
		}
		goto IL_01d4;
		IL_01bc:
		fertilizerIndex = 4;
		goto IL_01d6;
		IL_0187:
		if (value == "(O)465")
		{
			goto IL_01c4;
		}
		goto IL_01d4;
		IL_01c4:
		fertilizerIndex = 6;
		goto IL_01d6;
	}

	private List<Neighbor> gatherNeighbors()
	{
		List<Neighbor> results = _neighbors;
		results.Clear();
		if (Pot == null)
		{
			GameLocation location = Location;
			Vector2 tilePos = Tile;
			NetVector2Dictionary<TerrainFeature, NetRef<TerrainFeature>> terrainFeatures = location.terrainFeatures;
			NeighborLoc[] offsets = _offsets;
			for (int i = 0; i < offsets.Length; i++)
			{
				NeighborLoc item = offsets[i];
				Vector2 tile = tilePos + item.Offset;
				if (terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt dirt && dirt.state.Value != 2)
				{
					Neighbor n = new Neighbor(dirt, item.Direction, item.InvDirection);
					results.Add(n);
				}
			}
		}
		return results;
	}

	public void updateNeighbors()
	{
		if (Location == null)
		{
			return;
		}
		List<Neighbor> list = gatherNeighbors();
		neighborMask = 0;
		wateredNeighborMask = 0;
		foreach (Neighbor n in list)
		{
			neighborMask |= n.direction;
			if (state.Value != 2)
			{
				n.feature.OnNeighborAdded(n.invDirection, state.Value);
			}
			if (isWatered() && n.feature.isWatered())
			{
				if (n.feature.paddyWaterCheck() == paddyWaterCheck())
				{
					wateredNeighborMask |= n.direction;
					n.feature.wateredNeighborMask |= n.invDirection;
				}
				else
				{
					n.feature.wateredNeighborMask = (byte)(n.feature.wateredNeighborMask & ~n.invDirection);
				}
			}
			n.feature.UpdateDrawSums();
		}
		UpdateDrawSums();
	}

	public void OnAdded(GameLocation loc, Vector2 tilePos)
	{
		Location = loc;
		Tile = tilePos;
		updateNeighbors();
	}

	public void OnRemoved()
	{
		if (Location == null)
		{
			return;
		}
		List<Neighbor> list = gatherNeighbors();
		neighborMask = 0;
		wateredNeighborMask = 0;
		foreach (Neighbor n in list)
		{
			n.feature.OnNeighborRemoved(n.invDirection);
			if (isWatered())
			{
				n.feature.wateredNeighborMask = (byte)(n.feature.wateredNeighborMask & ~n.invDirection);
			}
			n.feature.UpdateDrawSums();
		}
		UpdateDrawSums();
	}

	public virtual void UpdateDrawSums()
	{
		drawSum = (byte)(neighborMask & 0xF);
		sourceRectPosition = drawGuide[drawSum];
		wateredRectPosition = drawGuide[wateredNeighborMask];
	}

	/// <summary>Called when a neighbor is added or changed.</summary>
	/// <param name="direction">The direction from this dirt to the one which changed.</param>
	/// <param name="neighborState">The water state for the neighbor which changed.</param>
	public void OnNeighborAdded(byte direction, int neighborState)
	{
		neighborMask |= direction;
		if (neighborState == 1)
		{
			wateredNeighborMask |= direction;
		}
		else
		{
			wateredNeighborMask = (byte)(wateredNeighborMask & ~direction);
		}
	}

	/// <summary>Called when a neighbor is removed.</summary>
	/// <param name="direction">The direction from this dirt to the one which was removed.</param>
	public void OnNeighborRemoved(byte direction)
	{
		neighborMask = (byte)(neighborMask & ~direction);
		wateredNeighborMask = (byte)(wateredNeighborMask & ~direction);
	}
}
