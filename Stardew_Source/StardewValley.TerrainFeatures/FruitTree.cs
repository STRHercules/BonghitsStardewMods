using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData.FruitTrees;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;

namespace StardewValley.TerrainFeatures;

public class FruitTree : TerrainFeature
{
	/// <summary>The asset name for the default fruit tree tilesheet.</summary>
	public const string DefaultTextureName = "TileSheets\\fruitTrees";

	public const float shakeRate = (float)Math.PI / 200f;

	public const float shakeDecayRate = 0.0030679617f;

	public const int minWoodDebrisForFallenTree = 12;

	public const int minWoodDebrisForStump = 5;

	public const int startingHealth = 10;

	public const int leafFallRate = 3;

	public const int DaysUntilMaturity = 28;

	public const int maxFruitsOnTrees = 3;

	public const int seedStage = 0;

	public const int sproutStage = 1;

	public const int saplingStage = 2;

	public const int bushStage = 3;

	public const int treeStage = 4;

	/// <summary>The texture from which to draw the tree sprites.</summary>
	[XmlIgnore]
	public Texture2D texture;

	[XmlElement("growthStage")]
	public readonly NetInt growthStage = new NetInt();

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="F:StardewValley.TerrainFeatures.FruitTree.treeId" /> instead.</summary>
	[XmlElement("treeType")]
	public string obsolete_treeType;

	/// <summary>The unique identifier for the underlying fruit tree data.</summary>
	[XmlElement("treeId")]
	public readonly NetString treeId = new NetString();

	/// <summary>The number of days until the fruit tree becomes full-grown.</summary>
	/// <remarks>The fruit tree is a seed at <see cref="F:StardewValley.TerrainFeatures.FruitTree.DaysUntilMaturity" /> and becomes full-grown at 0 or below.</remarks>
	[XmlElement("daysUntilMature")]
	public readonly NetInt daysUntilMature = new NetInt(28);

	/// <summary>Obsolete. This is only kept to preserve data from old save files. Use <see cref="F:StardewValley.TerrainFeatures.FruitTree.fruit" /> instead.</summary>
	[XmlElement("fruitsOnTree")]
	public int? obsolete_fruitsOnTree;

	[XmlElement("fruit")]
	public readonly NetList<Item, NetRef<Item>> fruit = new NetList<Item, NetRef<Item>>();

	[XmlElement("struckByLightningCountdown")]
	public readonly NetInt struckByLightningCountdown = new NetInt();

	[XmlElement("health")]
	public readonly NetFloat health = new NetFloat(10f);

	[XmlElement("flipped")]
	public readonly NetBool flipped = new NetBool();

	[XmlElement("stump")]
	public readonly NetBool stump = new NetBool();

	/// <summary>Whether the tree is planted on a stone tile in the greenhouse.</summary>
	[XmlElement("greenHouseTileTree")]
	public readonly NetBool greenHouseTileTree = new NetBool();

	[XmlIgnore]
	public readonly NetBool shakeLeft = new NetBool();

	[XmlIgnore]
	public readonly NetBool falling = new NetBool();

	[XmlIgnore]
	public bool destroy;

	[XmlIgnore]
	public float shakeRotation;

	[XmlIgnore]
	public float maxShake;

	[XmlIgnore]
	public float alpha = 1f;

	private List<Leaf> leaves = new List<Leaf>();

	[XmlIgnore]
	public readonly NetLong lastPlayerToHit = new NetLong();

	[XmlIgnore]
	public float shakeTimer;

	[XmlElement("growthRate")]
	public readonly NetInt growthRate = new NetInt(1);

	/// <summary>The asset name loaded for <see cref="F:StardewValley.TerrainFeatures.FruitTree.texture" />.</summary>
	[XmlIgnore]
	public string textureName { get; private set; }

	/// <inheritdoc cref="F:StardewValley.TerrainFeatures.FruitTree.greenHouseTileTree" />
	[XmlIgnore]
	public bool GreenHouseTileTree
	{
		get
		{
			return greenHouseTileTree.Value;
		}
		set
		{
			greenHouseTileTree.Value = value;
		}
	}

	public FruitTree()
		: this(null)
	{
	}

	public FruitTree(string id, int growthStage = 0)
		: base(needsTick: true)
	{
		treeId.Value = id;
		this.growthStage.Value = growthStage;
		daysUntilMature.Value = GrowthStageToDaysUntilMature(growthStage);
		flipped.Value = Game1.random.NextBool();
		loadSprite();
	}

	public override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(growthStage, "this.growthStage").AddField(treeId, "treeId").AddField(daysUntilMature, "daysUntilMature")
			.AddField(fruit, "fruit")
			.AddField(struckByLightningCountdown, "struckByLightningCountdown")
			.AddField(health, "health")
			.AddField(flipped, "flipped")
			.AddField(stump, "stump")
			.AddField(greenHouseTileTree, "greenHouseTileTree")
			.AddField(shakeLeft, "shakeLeft")
			.AddField(falling, "falling")
			.AddField(lastPlayerToHit, "lastPlayerToHit")
			.AddField(growthRate, "growthRate");
		treeId.fieldChangeVisibleEvent += delegate
		{
			loadSprite();
		};
	}

	public int GetSpriteRowNumber()
	{
		return GetData()?.TextureSpriteRow ?? 0;
	}

	public override void loadSprite()
	{
		string assetName = GetData()?.Texture ?? "TileSheets\\fruitTrees";
		if (texture == null || textureName != assetName)
		{
			try
			{
				texture = Game1.content.Load<Texture2D>(assetName);
				textureName = assetName;
			}
			catch (Exception exception)
			{
				Game1.log.Error($"Fruit tree '{treeId.Value}' failed to load spritesheet '{assetName}'.", exception);
			}
		}
	}

	public override bool isActionable()
	{
		return true;
	}

	/// <summary>Get whether the tree is in a location which ignores seasons (like the greenhouse or Ginger Island).</summary>
	public bool IgnoresSeasonsHere()
	{
		return Location?.SeedsIgnoreSeasonsHere() ?? false;
	}

	public override Rectangle getBoundingBox()
	{
		Vector2 tileLocation = Tile;
		return new Rectangle((int)tileLocation.X * 64, (int)tileLocation.Y * 64, 64, 64);
	}

	public override Rectangle getRenderBounds()
	{
		Vector2 tileLocation = Tile;
		if (stump.Value || growthStage.Value < 4)
		{
			return new Rectangle((int)(tileLocation.X - 0f) * 64, (int)(tileLocation.Y - 1f) * 64, 64, 128);
		}
		return new Rectangle((int)(tileLocation.X - 1f) * 64, (int)(tileLocation.Y - 5f) * 64, 192, 448);
	}

	public override bool performUseAction(Vector2 tileLocation)
	{
		GameLocation location = Location;
		if (maxShake == 0f && !stump.Value && growthStage.Value >= 3 && !IsWinterTreeHere())
		{
			location.playSound("leafrustle", null, null);
		}
		shake(tileLocation, doEvenIfStillShaking: false);
		return true;
	}

	public override bool tickUpdate(GameTime time)
	{
		if (destroy)
		{
			return true;
		}
		GameLocation location = Location;
		Vector2 tileLocation = Tile;
		alpha = Math.Min(1f, alpha + 0.05f);
		if (shakeTimer > 0f)
		{
			shakeTimer -= time.ElapsedGameTime.Milliseconds;
		}
		if (growthStage.Value >= 4 && !falling.Value && !stump.Value && Game1.player.GetBoundingBox().Intersects(new Rectangle(64 * ((int)tileLocation.X - 1), 64 * ((int)tileLocation.Y - 4), 192, 224)))
		{
			alpha = Math.Max(0.4f, alpha - 0.09f);
		}
		if (!falling.Value)
		{
			if ((double)Math.Abs(shakeRotation) > Math.PI / 2.0 && leaves.Count <= 0 && health.Value <= 0f)
			{
				return true;
			}
			if (maxShake > 0f)
			{
				if (shakeLeft.Value)
				{
					shakeRotation -= ((growthStage.Value >= 4) ? 0.005235988f : ((float)Math.PI / 200f));
					if (shakeRotation <= 0f - maxShake)
					{
						shakeLeft.Value = false;
					}
				}
				else
				{
					shakeRotation += ((growthStage.Value >= 4) ? 0.005235988f : ((float)Math.PI / 200f));
					if (shakeRotation >= maxShake)
					{
						shakeLeft.Value = true;
					}
				}
			}
			if (maxShake > 0f)
			{
				maxShake = Math.Max(0f, maxShake - ((growthStage.Value >= 4) ? 0.0010226539f : 0.0030679617f));
			}
			if (struckByLightningCountdown.Value > 0 && Game1.random.NextDouble() < 0.01)
			{
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(372, 1956, 10, 10), new Vector2(tileLocation.X * 64f + (float)Game1.random.Next(-64, 96), tileLocation.Y * 64f - 192f + (float)Game1.random.Next(-64, 128)), flipped: false, 0.002f, Color.Gray)
				{
					alpha = 0.75f,
					motion = new Vector2(0f, -0.5f),
					interval = 99999f,
					layerDepth = 1f,
					scale = 2f,
					scaleChange = 0.01f
				});
			}
		}
		else
		{
			shakeRotation += (shakeLeft.Value ? (0f - maxShake * maxShake) : (maxShake * maxShake));
			maxShake += 0.0015339808f;
			if (Game1.random.NextDouble() < 0.01 && !IsWinterTreeHere())
			{
				location.localSound("leafrustle", null, null);
			}
			if ((double)Math.Abs(shakeRotation) > Math.PI / 2.0)
			{
				falling.Value = false;
				maxShake = 0f;
				location.localSound("treethud", null, null);
				int leavesToAdd = Game1.random.Next(90, 120);
				for (int i = 0; i < leavesToAdd; i++)
				{
					leaves.Add(new Leaf(new Vector2(Game1.random.Next((int)(tileLocation.X * 64f), (int)(tileLocation.X * 64f + 192f)) + (shakeLeft.Value ? (-320) : 256), tileLocation.Y * 64f - 64f), (float)Game1.random.Next(-10, 10) / 100f, Game1.random.Next(4), (float)Game1.random.Next(10, 40) / 10f));
				}
				Farmer lastHitBy = Game1.GetPlayer(lastPlayerToHit.Value) ?? Game1.MasterPlayer;
				Game1.createRadialDebris(location, 12, (int)tileLocation.X + (shakeLeft.Value ? (-4) : 4), (int)tileLocation.Y, (int)((lastHitBy.professions.Contains(12) ? 1.25 : 1.0) * 12.0), resource: true, -1, item: false, null);
				Game1.createRadialDebris(location, 12, (int)tileLocation.X + (shakeLeft.Value ? (-4) : 4), (int)tileLocation.Y, (int)((lastHitBy.professions.Contains(12) ? 1.25 : 1.0) * 12.0), resource: false, -1, item: false, null);
				if (Game1.IsMultiplayer)
				{
					Game1.recentMultiplayerRandom = Utility.CreateRandom((double)tileLocation.X * 1000.0, tileLocation.Y);
				}
				if (Game1.IsMultiplayer)
				{
					Game1.createMultipleObjectDebris("(O)92", (int)tileLocation.X + (shakeLeft.Value ? (-4) : 4), (int)tileLocation.Y, 10, lastPlayerToHit.Value, location);
				}
				else
				{
					Game1.createMultipleObjectDebris("(O)92", (int)tileLocation.X + (shakeLeft.Value ? (-4) : 4), (int)tileLocation.Y, 10, location);
				}
				if (health.Value <= 0f)
				{
					health.Value = -100f;
				}
			}
		}
		for (int i2 = leaves.Count - 1; i2 >= 0; i2--)
		{
			Leaf leaf = leaves[i2];
			leaf.position.Y -= leaf.yVelocity - 3f;
			leaf.yVelocity = Math.Max(0f, leaf.yVelocity - 0.01f);
			leaf.rotation += leaf.rotationRate;
			if (leaf.position.Y >= tileLocation.Y * 64f + 64f)
			{
				leaves.RemoveAt(i2);
			}
		}
		return false;
	}

	/// <summary>Get the quality of fruit currently produced by the tree, matching one of the constants like <see cref="F:StardewValley.Object.highQuality" />.</summary>
	public int GetQuality()
	{
		if (struckByLightningCountdown.Value > 0 || daysUntilMature.Value >= 0)
		{
			return 0;
		}
		return (daysUntilMature.Value / -112) switch
		{
			0 => 0, 
			1 => 1, 
			2 => 2, 
			_ => 4, 
		};
	}

	public virtual void shake(Vector2 tileLocation, bool doEvenIfStillShaking)
	{
		if ((maxShake == 0f || doEvenIfStillShaking) && growthStage.Value >= 3 && !stump.Value)
		{
			Vector2 playerPixel = Game1.player.getStandingPosition();
			shakeLeft.Value = playerPixel.X > (tileLocation.X + 0.5f) * 64f || (Game1.player.Tile.X == tileLocation.X && Game1.random.NextBool());
			maxShake = (float)((growthStage.Value >= 4) ? (Math.PI / 128.0) : (Math.PI / 64.0));
			if (growthStage.Value >= 4)
			{
				if (Game1.random.NextDouble() < 0.66 && !IsWinterTreeHere())
				{
					int numberOfLeaves = Game1.random.Next(1, 6);
					for (int i = 0; i < numberOfLeaves; i++)
					{
						leaves.Add(new Leaf(new Vector2(Game1.random.Next((int)(tileLocation.X * 64f - 64f), (int)(tileLocation.X * 64f + 128f)), Game1.random.Next((int)(tileLocation.Y * 64f - 256f), (int)(tileLocation.Y * 64f - 192f))), (float)Game1.random.Next(-10, 10) / 100f, Game1.random.Next(4), (float)Game1.random.Next(5) / 10f));
					}
				}
				int fruitQuality = GetQuality();
				if (!Location.terrainFeatures.TryGetValue(tileLocation, out var terrainFeature) || !terrainFeature.Equals(this))
				{
					return;
				}
				for (int j = 0; j < fruit.Count; j++)
				{
					Vector2 offset = new Vector2(0f, 0f);
					switch (j)
					{
					case 0:
						offset.X = -64f;
						break;
					case 1:
						offset.X = 64f;
						offset.Y = -32f;
						break;
					case 2:
						offset.Y = 32f;
						break;
					}
					Debris d;
					if (struckByLightningCountdown.Value <= 0)
					{
						Item item = fruit[j];
						fruit[j] = null;
						d = new Debris(item, new Vector2(tileLocation.X * 64f + 32f, (tileLocation.Y - 3f) * 64f + 32f) + offset, playerPixel)
						{
							itemQuality = fruitQuality
						};
					}
					else
					{
						d = new Debris(382.ToString(), new Vector2(tileLocation.X * 64f + 32f, (tileLocation.Y - 3f) * 64f + 32f) + offset, playerPixel)
						{
							itemQuality = fruitQuality
						};
					}
					d.Chunks[0].xVelocity.Value += (float)Game1.random.Next(-10, 11) / 10f;
					d.chunkFinalYLevel = (int)(tileLocation.Y * 64f + 64f);
					Location.debris.Add(d);
				}
				fruit.Clear();
			}
			else if (Game1.random.NextDouble() < 0.66 && !IsWinterTreeHere())
			{
				int numberOfLeaves2 = Game1.random.Next(1, 3);
				for (int k = 0; k < numberOfLeaves2; k++)
				{
					leaves.Add(new Leaf(new Vector2(Game1.random.Next((int)(tileLocation.X * 64f), (int)(tileLocation.X * 64f + 48f)), tileLocation.Y * 64f - 96f), (float)Game1.random.Next(-10, 10) / 100f, Game1.random.Next(4), (float)Game1.random.Next(30) / 10f));
				}
			}
		}
		else if (stump.Value)
		{
			shakeTimer = 100f;
		}
	}

	/// <inheritdoc />
	public override bool isPassable(Character c = null)
	{
		return health.Value <= -99f;
	}

	public static bool IsTooCloseToAnotherTree(Vector2 tileLocation, GameLocation environment, bool fruitTreesOnly = false)
	{
		Vector2 v = default(Vector2);
		for (int i = (int)tileLocation.X - 2; i <= (int)tileLocation.X + 2; i++)
		{
			for (int j = (int)tileLocation.Y - 2; j <= (int)tileLocation.Y + 2; j++)
			{
				v.X = i;
				v.Y = j;
				if (environment.terrainFeatures.TryGetValue(v, out var feature) && (feature is FruitTree || (!fruitTreesOnly && feature is Tree)))
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Get whether a fruit tree is unable to grow due to nearby objects, terrain features, etc.</summary>
	/// <param name="tileLocation">The tile position containing the fruit tree.</param>
	/// <param name="environment">The location containing the fruit tree.</param>
	public static bool IsGrowthBlocked(Vector2 tileLocation, GameLocation environment)
	{
		Vector2[] surroundingTileLocationsArray = Utility.getSurroundingTileLocationsArray(tileLocation);
		foreach (Vector2 v in surroundingTileLocationsArray)
		{
			if (environment.IsTileOccupiedBy(v, CollisionMask.Objects))
			{
				string text = environment.objects.GetValueOrDefault(v)?.QualifiedItemId;
				if (!(text == "(O)590") && !(text == "(O)SeedSpot"))
				{
					return true;
				}
			}
			if (environment.IsTileOccupiedBy(v, CollisionMask.TerrainFeatures))
			{
				TerrainFeature valueOrDefault = environment.terrainFeatures.GetValueOrDefault(v);
				if (!(valueOrDefault is HoeDirt dirt))
				{
					if (!(valueOrDefault is Grass))
					{
						return true;
					}
				}
				else if (dirt.crop != null)
				{
					return true;
				}
			}
			if (environment.IsTileOccupiedBy(v, CollisionMask.Buildings | CollisionMask.Flooring | CollisionMask.Furniture | CollisionMask.LocationSpecific))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Get the fruit tree's data from <see cref="F:StardewValley.Game1.fruitTreeData" />, if found.</summary>
	public FruitTreeData GetData()
	{
		if (!TryGetData(treeId.Value, out var data))
		{
			return null;
		}
		return data;
	}

	/// <summary>Try to get a fruit tree's data from <see cref="F:StardewValley.Game1.fruitTreeData" />.</summary>
	/// <param name="id">The tree type ID (i.e. the key in <see cref="F:StardewValley.Game1.fruitTreeData" />).</param>
	/// <param name="data">The fruit tree data, if found.</param>
	/// <returns>Returns whether the fruit tree data was found.</returns>
	public static bool TryGetData(string id, out FruitTreeData data)
	{
		if (id == null)
		{
			data = null;
			return false;
		}
		return Game1.fruitTreeData.TryGetValue(id, out data);
	}

	/// <summary>Get the translated display name for this tree, like 'Cherry' or 'Mango'.</summary>
	public string GetDisplayName()
	{
		return TokenParser.ParseText(GetData()?.DisplayName) ?? ItemRegistry.GetErrorItemName();
	}

	public override void dayUpdate()
	{
		GameLocation environment = Location;
		if (health.Value <= -99f)
		{
			destroy = true;
		}
		if (struckByLightningCountdown.Value > 0)
		{
			struckByLightningCountdown.Value--;
			if (struckByLightningCountdown.Value <= 0)
			{
				fruit.Clear();
			}
		}
		bool foundSomething = IsGrowthBlocked(Tile, environment);
		if (!foundSomething || daysUntilMature.Value <= 0)
		{
			if (daysUntilMature.Value > 28)
			{
				daysUntilMature.Value = 28;
			}
			if (growthRate.Value > 1)
			{
				_ = growthRate.Value;
			}
			daysUntilMature.Value -= growthRate.Value;
			growthStage.Value = DaysUntilMatureToGrowthStage(daysUntilMature.Value);
		}
		else if (foundSomething && growthStage.Value != 4)
		{
			string tokenizedDisplayName = GetData()?.DisplayName ?? GetDisplayName();
			Game1.multiplayer.broadcastGlobalMessage("Strings\\UI:FruitTree_Warning", true, null, tokenizedDisplayName);
		}
		if (stump.Value)
		{
			fruit.Clear();
		}
		else
		{
			TryAddFruit();
		}
	}

	/// <summary>Get the maximum <see cref="F:StardewValley.TerrainFeatures.FruitTree.daysUntilMature" /> value which would match a given growth stage.</summary>
	/// <param name="growthStage">The growth stage (matching a constant like <see cref="F:StardewValley.TerrainFeatures.FruitTree.treeStage" />).</param>
	public static int GrowthStageToDaysUntilMature(int growthStage)
	{
		if (growthStage > 4)
		{
			growthStage = 4;
		}
		return growthStage switch
		{
			4 => 0, 
			3 => 7, 
			2 => 14, 
			1 => 21, 
			_ => 28, 
		};
	}

	/// <summary>Get the growth stage (matching a constant like <see cref="F:StardewValley.TerrainFeatures.FruitTree.treeStage" />) for a given <see cref="F:StardewValley.TerrainFeatures.FruitTree.daysUntilMature" /> value.</summary>
	/// <param name="daysUntilMature">The <see cref="F:StardewValley.TerrainFeatures.FruitTree.daysUntilMature" /> value.</param>
	public static int DaysUntilMatureToGrowthStage(int daysUntilMature)
	{
		for (int stage = 4; stage >= 0; stage--)
		{
			if (daysUntilMature <= GrowthStageToDaysUntilMature(stage))
			{
				return stage;
			}
		}
		return 0;
	}

	/// <summary>Try to add a fruit to the tree.</summary>
	public bool TryAddFruit()
	{
		if (!stump.Value && growthStage.Value >= 4 && (IsInSeasonHere() || (struckByLightningCountdown.Value > 0 && !IsWinterTreeHere())) && fruit.Count < 3)
		{
			FruitTreeData data = GetData();
			if (data?.Fruit != null)
			{
				foreach (FruitTreeFruitData entry in data.Fruit)
				{
					Item item = TryCreateFruit(entry);
					if (item != null)
					{
						fruit.Add(item);
						return true;
					}
				}
			}
		}
		return false;
	}

	/// <summary>Create a fruit item if its fields match.</summary>
	/// <param name="drop">The fruit data.</param>
	/// <returns>Returns the produced item (if any), else <c>null</c>.</returns>
	private Item TryCreateFruit(FruitTreeFruitData drop)
	{
		if (!Game1.random.NextBool(drop.Chance))
		{
			return null;
		}
		if (drop.Condition != null && !GameStateQuery.CheckConditions(drop.Condition, Location, null, null, null, null, IgnoresSeasonsHere() ? GameStateQuery.SeasonQueryKeys : null))
		{
			return null;
		}
		if (drop.Season.HasValue && !IgnoresSeasonsHere() && drop.Season != Game1.GetSeasonForLocation(Location))
		{
			return null;
		}
		Item item = ItemQueryResolver.TryResolveRandomItem(drop, new ItemQueryContext(Location, null, null, $"fruit tree '{treeId.Value}' > fruit '{drop.Id}'"), avoidRepeat: false, null, null, null, delegate(string query, string error)
		{
			Game1.log.Error($"Fruit tree '{treeId.Value}' failed parsing item query '{query}' for fruit '{drop.Id}': {error}");
		});
		if (item != null)
		{
			item.Quality = GetQuality();
		}
		return item;
	}

	/// <summary>Get whether the fruit tree is in winter mode now (e.g. with no leaves).</summary>
	public virtual bool IsWinterTreeHere()
	{
		if (!IgnoresSeasonsHere())
		{
			return Game1.GetSeasonForLocation(Location) == Season.Winter;
		}
		return false;
	}

	/// <summary>Get whether the fruit tree can produce fruit now.</summary>
	public virtual bool IsInSeasonHere()
	{
		if (IgnoresSeasonsHere())
		{
			return true;
		}
		List<Season> growSeasons = GetData()?.Seasons;
		if (growSeasons != null && growSeasons.Count > 0)
		{
			Season curSeason = Game1.GetSeasonForLocation(Location);
			foreach (Season growSeason in growSeasons)
			{
				if (curSeason == growSeason)
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>Get the season for which to show a fruit tree sprite (which isn't necessarily the season for which it produces fruit).</summary>
	public virtual Season GetCosmeticSeason()
	{
		if (!IgnoresSeasonsHere())
		{
			return Location.GetSeason();
		}
		return Season.Summer;
	}

	/// <inheritdoc />
	public override bool seasonUpdate(bool onLoad)
	{
		if (!IsInSeasonHere() && !onLoad)
		{
			fruit.Clear();
		}
		return false;
	}

	public override bool performToolAction(Tool t, int explosion, Vector2 tileLocation)
	{
		if (health.Value <= -99f)
		{
			return false;
		}
		if (t is MeleeWeapon)
		{
			return false;
		}
		GameLocation location = Location;
		if (growthStage.Value >= 4)
		{
			if (t is Axe)
			{
				location.playSound("axchop", tileLocation, null);
				location.debris.Add(new Debris(12, Game1.random.Next(t.upgradeLevel.Value * 2, t.upgradeLevel.Value * 4), t.getLastFarmerToUse().GetToolLocation() + new Vector2(16f, 0f), t.getLastFarmerToUse().Position, 0, null));
				lastPlayerToHit.Value = t.getLastFarmerToUse().UniqueMultiplayerID;
				int fruitQuality = GetQuality();
				if (location.terrainFeatures.TryGetValue(tileLocation, out var terrainFeature) && terrainFeature.Equals(this))
				{
					for (int i = 0; i < fruit.Count; i++)
					{
						Vector2 offset = new Vector2(0f, 0f);
						switch (i)
						{
						case 0:
							offset.X = -64f;
							break;
						case 1:
							offset.X = 64f;
							offset.Y = -32f;
							break;
						case 2:
							offset.Y = 32f;
							break;
						}
						Debris d;
						if (struckByLightningCountdown.Value <= 0)
						{
							Item item = fruit[i];
							fruit[i] = null;
							d = new Debris(item, new Vector2(tileLocation.X * 64f + 32f, (tileLocation.Y - 3f) * 64f + 32f) + offset, Game1.player.getStandingPosition())
							{
								itemQuality = fruitQuality
							};
						}
						else
						{
							d = new Debris(382.ToString(), new Vector2(tileLocation.X * 64f + 32f, (tileLocation.Y - 3f) * 64f + 32f) + offset, Game1.player.getStandingPosition())
							{
								itemQuality = fruitQuality
							};
						}
						d.Chunks[0].xVelocity.Value += (float)Game1.random.Next(-10, 11) / 10f;
						d.chunkFinalYLevel = (int)(tileLocation.Y * 64f + 64f);
						location.debris.Add(d);
					}
					fruit.Clear();
				}
			}
			else if (explosion <= 0)
			{
				return false;
			}
			shake(tileLocation, doEvenIfStillShaking: true);
			float damage;
			if (explosion > 0)
			{
				damage = explosion;
			}
			else
			{
				if (t == null)
				{
					return false;
				}
				damage = t.upgradeLevel.Value switch
				{
					0 => 1f, 
					1 => 1.25f, 
					2 => 1.67f, 
					3 => 2.5f, 
					4 => 5f, 
					_ => t.upgradeLevel.Value + 1, 
				};
			}
			health.Value -= damage;
			if (t is Axe && t.hasEnchantmentOfType<ShavingEnchantment>() && Game1.random.NextDouble() <= (double)(damage / 5f))
			{
				Debris d2 = new Debris("388", new Vector2(tileLocation.X * 64f + 32f, (tileLocation.Y - 0.5f) * 64f + 32f), Game1.player.getStandingPosition());
				d2.Chunks[0].xVelocity.Value += (float)Game1.random.Next(-10, 11) / 10f;
				d2.chunkFinalYLevel = (int)(tileLocation.Y * 64f + 64f);
				location.debris.Add(d2);
			}
			if (health.Value <= 0f)
			{
				if (!stump.Value)
				{
					location.playSound("treecrack", tileLocation, null);
					stump.Value = true;
					health.Value = 5f;
					falling.Value = true;
					if (t?.getLastFarmerToUse() == null)
					{
						shakeLeft.Value = true;
					}
					else
					{
						shakeLeft.Value = (float)t.getLastFarmerToUse().StandingPixel.X > (tileLocation.X + 0.5f) * 64f;
					}
				}
				else
				{
					health.Value = -100f;
					Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(30, 40), resource: false, -1, item: false, null);
					if (Game1.IsMultiplayer)
					{
						Game1.recentMultiplayerRandom = Utility.CreateRandom((double)tileLocation.X * 2000.0, tileLocation.Y);
					}
					if (t?.getLastFarmerToUse() == null)
					{
						Game1.createMultipleObjectDebris("(O)92", (int)tileLocation.X, (int)tileLocation.Y, 2, location);
					}
					else
					{
						Farmer lastHitBy = Game1.GetPlayer(lastPlayerToHit.Value) ?? Game1.MasterPlayer;
						if (Game1.IsMultiplayer)
						{
							Game1.createMultipleObjectDebris("(O)92", (int)tileLocation.X, (int)tileLocation.Y, 1, lastPlayerToHit.Value, location);
							Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, lastHitBy.professions.Contains(12) ? 5 : 4, resource: true, -1, item: false, null);
						}
						else
						{
							Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, (int)((lastHitBy.professions.Contains(12) ? 1.25 : 1.0) * 5.0), resource: true, -1, item: false, null);
							Game1.createMultipleObjectDebris("(O)92", (int)tileLocation.X, (int)tileLocation.Y, 1, location);
						}
					}
					if (treeId.Value != null)
					{
						Game1.createItemDebris(ItemRegistry.Create("(O)" + treeId.Value, 1, GetQuality()), tileLocation * 64f, 2, location);
					}
				}
			}
		}
		else if (growthStage.Value >= 3)
		{
			if (t != null && t.Name.Contains("Ax"))
			{
				location.playSound("axchop", tileLocation, null);
				location.playSound("leafrustle", tileLocation, null);
				location.debris.Add(new Debris(12, Game1.random.Next(t.upgradeLevel.Value * 2, t.upgradeLevel.Value * 4), t.getLastFarmerToUse().GetToolLocation() + new Vector2(16f, 0f), t.getLastFarmerToUse().getStandingPosition(), 0, null));
			}
			else if (explosion <= 0)
			{
				return false;
			}
			shake(tileLocation, doEvenIfStillShaking: true);
			float damage2 = 1f;
			Random debrisRandom = ((!Game1.IsMultiplayer) ? Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)tileLocation.X * 7.0, (double)tileLocation.Y * 11.0, Game1.stats.DaysPlayed, health.Value) : Game1.recentMultiplayerRandom);
			if (explosion > 0)
			{
				damage2 = explosion;
			}
			else
			{
				switch (t.upgradeLevel.Value)
				{
				case 0:
					damage2 = 2f;
					break;
				case 1:
					damage2 = 2.5f;
					break;
				case 2:
					damage2 = 3.34f;
					break;
				case 3:
					damage2 = 5f;
					break;
				case 4:
					damage2 = 10f;
					break;
				}
			}
			int debris = 0;
			while (t != null && debrisRandom.NextDouble() < (double)damage2 * 0.08 + (double)((float)t.getLastFarmerToUse().ForagingLevel / 200f))
			{
				debris++;
			}
			health.Value -= damage2;
			if (debris > 0)
			{
				Game1.createDebris(12, (int)tileLocation.X, (int)tileLocation.Y, debris, location);
			}
			if (health.Value <= 0f)
			{
				if (treeId.Value != null)
				{
					Game1.createItemDebris(ItemRegistry.Create("(O)" + treeId.Value), tileLocation * 64f, 2, location);
				}
				Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(20, 30), resource: false, -1, item: false, null);
				return true;
			}
		}
		else if (growthStage.Value >= 1)
		{
			if (explosion > 0)
			{
				return true;
			}
			if (t != null && t.Name.Contains("Axe"))
			{
				location.playSound("axchop", tileLocation, null);
				Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(10, 20), resource: false, -1, item: false, null);
			}
			if (t is Axe || t is Pickaxe || t is Hoe || t is MeleeWeapon)
			{
				Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(10, 20), resource: false, -1, item: false, null);
				if (t.Name.Contains("Axe") && Game1.recentMultiplayerRandom.NextDouble() < (double)((float)t.getLastFarmerToUse().ForagingLevel / 10f))
				{
					Game1.createDebris(12, (int)tileLocation.X, (int)tileLocation.Y, 1, location);
				}
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(17, tileLocation * 64f, Color.White));
				if (treeId.Value != null)
				{
					Game1.createItemDebris(ItemRegistry.Create("(O)" + treeId.Value), tileLocation * 64f, 2, location);
				}
				return true;
			}
		}
		else
		{
			if (explosion > 0)
			{
				return true;
			}
			if (t.Name.Contains("Axe") || t.Name.Contains("Pick") || t.Name.Contains("Hoe"))
			{
				location.playSound("woodyHit", tileLocation, null);
				location.playSound("axchop", tileLocation, null);
				Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(17, tileLocation * 64f, Color.White));
				if (treeId.Value != null)
				{
					Game1.createItemDebris(ItemRegistry.Create("(O)" + treeId.Value), tileLocation * 64f, 2, location);
				}
				return true;
			}
		}
		return false;
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 positionOnScreen, Vector2 tileLocation, float scale, float layerDepth)
	{
		layerDepth += positionOnScreen.X / 100000f;
		if (growthStage.Value < 4)
		{
			Rectangle sourceRect = growthStage.Value switch
			{
				0 => new Rectangle(128, 512, 64, 64), 
				1 => new Rectangle(0, 512, 64, 64), 
				2 => new Rectangle(64, 512, 64, 64), 
				_ => new Rectangle(0, 384, 64, 128), 
			};
			spriteBatch.Draw(texture, positionOnScreen - new Vector2(0f, (float)sourceRect.Height * scale), sourceRect, Color.White, 0f, Vector2.Zero, scale, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth + (positionOnScreen.Y + (float)sourceRect.Height * scale) / 20000f);
			return;
		}
		if (!falling.Value)
		{
			spriteBatch.Draw(texture, positionOnScreen + new Vector2(0f, -64f * scale), new Rectangle(128, 384, 64, 128), Color.White, 0f, Vector2.Zero, scale, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth + (positionOnScreen.Y + 448f * scale - 1f) / 20000f);
		}
		if (!stump.Value || falling.Value)
		{
			spriteBatch.Draw(texture, positionOnScreen + new Vector2(-64f * scale, -320f * scale), new Rectangle(0, 0, 192, 384), Color.White, shakeRotation, Vector2.Zero, scale, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth + (positionOnScreen.Y + 448f * scale) / 20000f);
		}
	}

	public override void draw(SpriteBatch spriteBatch)
	{
		int seasonIndex = Game1.GetSeasonIndexForLocation(Location);
		int spriteRow = GetSpriteRowNumber();
		Vector2 tileLocation = Tile;
		Rectangle boundingBox = getBoundingBox();
		if (greenHouseTileTree.Value)
		{
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f, tileLocation.Y * 64f)), new Rectangle(669, 1957, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-08f);
		}
		if (growthStage.Value < 4)
		{
			Vector2 positionOffset = new Vector2((float)Math.Max(-8.0, Math.Min(64.0, Math.Sin((double)(tileLocation.X * 200f) / (Math.PI * 2.0)) * -16.0)), (float)Math.Max(-8.0, Math.Min(64.0, Math.Sin((double)(tileLocation.X * 200f) / (Math.PI * 2.0)) * -16.0))) / 2f;
			Rectangle sourceRect = growthStage.Value switch
			{
				0 => new Rectangle(0, spriteRow * 5 * 16, 48, 80), 
				1 => new Rectangle(48, spriteRow * 5 * 16, 48, 80), 
				2 => new Rectangle(96, spriteRow * 5 * 16, 48, 80), 
				_ => new Rectangle(144, spriteRow * 5 * 16, 48, 80), 
			};
			spriteBatch.Draw(this.texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + 32f + positionOffset.X, tileLocation.Y * 64f - (float)sourceRect.Height + 128f + positionOffset.Y)), sourceRect, Color.White, shakeRotation, new Vector2(24f, 80f), 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)boundingBox.Bottom / 10000f - tileLocation.X / 1000000f);
		}
		else
		{
			if (!stump.Value || falling.Value)
			{
				Season cosmeticSeason = GetCosmeticSeason();
				if (!falling.Value)
				{
					spriteBatch.Draw(this.texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f + 64f)), new Rectangle((12 + (int)cosmeticSeason * 3) * 16, spriteRow * 5 * 16 + 64, 48, 16), (struckByLightningCountdown.Value > 0) ? (Color.Gray * alpha) : (Color.White * alpha), 0f, new Vector2(24f, 16f), 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 1E-07f);
				}
				spriteBatch.Draw(this.texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f + 64f)), new Rectangle((12 + (int)cosmeticSeason * 3) * 16, spriteRow * 5 * 16, 48, 64), (struckByLightningCountdown.Value > 0) ? (Color.Gray * alpha) : (Color.White * alpha), shakeRotation, new Vector2(24f, 80f), 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)boundingBox.Bottom / 10000f + 0.001f - tileLocation.X / 1000000f);
			}
			if (health.Value >= 1f || (!falling.Value && health.Value > -99f))
			{
				spriteBatch.Draw(this.texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + 32f + ((shakeTimer > 0f) ? ((float)Math.Sin(Math.PI * 2.0 / (double)shakeTimer) * 2f) : 0f), tileLocation.Y * 64f + 64f)), new Rectangle(384, spriteRow * 5 * 16 + 48, 48, 32), (struckByLightningCountdown.Value > 0) ? (Color.Gray * alpha) : (Color.White * alpha), 0f, new Vector2(24f, 32f), 4f, flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (stump.Value && !falling.Value) ? ((float)boundingBox.Bottom / 10000f) : ((float)boundingBox.Bottom / 10000f - 0.001f - tileLocation.X / 1000000f));
			}
			for (int i = 0; i < fruit.Count; i++)
			{
				ParsedItemData obj = ((struckByLightningCountdown.Value > 0) ? ItemRegistry.GetDataOrErrorItem("(O)382") : ItemRegistry.GetDataOrErrorItem(fruit[i].QualifiedItemId));
				Texture2D texture = obj.GetTexture();
				Rectangle sourceRect2 = obj.GetSourceRect(0, null);
				switch (i)
				{
				case 0:
					spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f - 64f + tileLocation.X * 200f % 64f / 2f, tileLocation.Y * 64f - 192f - tileLocation.X % 64f / 3f)), sourceRect2, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)boundingBox.Bottom / 10000f + 0.002f - tileLocation.X / 1000000f);
					break;
				case 1:
					spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f - 256f + tileLocation.X * 232f % 64f / 3f)), sourceRect2, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)boundingBox.Bottom / 10000f + 0.002f - tileLocation.X / 1000000f);
					break;
				case 2:
					spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileLocation.X * 64f + tileLocation.X * 200f % 64f / 3f, tileLocation.Y * 64f - 160f + tileLocation.X * 200f % 64f / 3f)), sourceRect2, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, (float)boundingBox.Bottom / 10000f + 0.002f - tileLocation.X / 1000000f);
					break;
				}
			}
		}
		foreach (Leaf l in leaves)
		{
			spriteBatch.Draw(this.texture, Game1.GlobalToLocal(Game1.viewport, l.position), new Rectangle((24 + seasonIndex) * 16, spriteRow * 5 * 16, 8, 8), Color.White, l.rotation, Vector2.Zero, 4f, SpriteEffects.None, (float)boundingBox.Bottom / 10000f + 0.01f);
		}
	}
}
