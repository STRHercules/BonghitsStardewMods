using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.FishPonds;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Tools;

namespace StardewValley.Buildings;

public class FishPond : Building
{
	public const int MAXIMUM_OCCUPANCY = 10;

	public static readonly float FISHING_MILLISECONDS = 1000f;

	public static readonly int HARVEST_BASE_EXP = 10;

	public static readonly float HARVEST_OUTPUT_EXP_MULTIPLIER = 0.04f;

	public static readonly int QUEST_BASE_EXP = 20;

	public static readonly float QUEST_SPAWNRATE_EXP_MULTIPIER = 5f;

	public const int NUMBER_OF_NETTING_STYLE_TYPES = 4;

	[XmlArrayItem("int")]
	public readonly NetString fishType = new NetString();

	public readonly NetInt lastUnlockedPopulationGate = new NetInt(0);

	public readonly NetBool hasCompletedRequest = new NetBool(value: false);

	/// <summary>Whether a player has added a golden cracker to the fish pond.</summary>
	public readonly NetBool goldenAnimalCracker = new NetBool(value: false);

	/// <summary>Whether a player has added a golden cracker to the fish pond, but it hasn't landed yet.</summary>
	[XmlIgnore]
	public readonly NetBool isPlayingGoldenCrackerAnimation = new NetBool(value: false);

	public readonly NetRef<Object> sign = new NetRef<Object>();

	public readonly NetColor overrideWaterColor = new NetColor(Color.White);

	public readonly NetRef<Item> output = new NetRef<Item>();

	public readonly NetRef<Item> neededItem = new NetRef<Item>();

	public readonly NetIntDelta neededItemCount = new NetIntDelta(0);

	public readonly NetInt daysSinceSpawn = new NetInt(0);

	public readonly NetInt nettingStyle = new NetInt(0);

	public readonly NetInt seedOffset = new NetInt(0);

	public readonly NetBool hasSpawnedFish = new NetBool(value: false);

	[XmlIgnore]
	public readonly NetMutex needsMutex = new NetMutex();

	[XmlIgnore]
	protected bool _hasAnimatedSpawnedFish;

	[XmlIgnore]
	protected float _delayUntilFishSilhouetteAdded;

	[XmlIgnore]
	protected int _numberOfFishToJump;

	[XmlIgnore]
	protected float _timeUntilFishHop;

	[XmlIgnore]
	protected Object _fishObject;

	[XmlIgnore]
	public List<PondFishSilhouette> _fishSilhouettes = new List<PondFishSilhouette>();

	[XmlIgnore]
	public List<JumpingFish> _jumpingFish = new List<JumpingFish>();

	[XmlIgnore]
	private readonly NetEvent0 animateHappyFishEvent = new NetEvent0();

	[XmlIgnore]
	public TemporaryAnimatedSpriteList animations = new TemporaryAnimatedSpriteList();

	[XmlIgnore]
	protected FishPondData _fishPondData;

	public int FishCount => currentOccupants.Value;

	public FishPond(Vector2 tileLocation)
		: base("Fish Pond", tileLocation)
	{
		UpdateMaximumOccupancy();
		fadeWhenPlayerIsBehind.Value = false;
		Reseed();
	}

	public FishPond()
		: this(Vector2.Zero)
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(fishType, "fishType").AddField(output, "output").AddField(daysSinceSpawn, "daysSinceSpawn")
			.AddField(lastUnlockedPopulationGate, "lastUnlockedPopulationGate")
			.AddField(animateHappyFishEvent, "animateHappyFishEvent")
			.AddField(hasCompletedRequest, "hasCompletedRequest")
			.AddField(goldenAnimalCracker, "goldenAnimalCracker")
			.AddField(isPlayingGoldenCrackerAnimation, "isPlayingGoldenCrackerAnimation")
			.AddField(neededItem, "neededItem")
			.AddField(seedOffset, "seedOffset")
			.AddField(hasSpawnedFish, "hasSpawnedFish")
			.AddField(needsMutex.NetFields, "needsMutex.NetFields")
			.AddField(neededItemCount, "neededItemCount")
			.AddField(overrideWaterColor, "overrideWaterColor")
			.AddField(sign, "sign")
			.AddField(nettingStyle, "nettingStyle");
		animateHappyFishEvent.onEvent += AnimateHappyFish;
		fishType.fieldChangeVisibleEvent += OnFishTypeChanged;
	}

	public virtual void OnFishTypeChanged(NetString field, string old_value, string new_value)
	{
		_fishSilhouettes.Clear();
		_jumpingFish.Clear();
		_fishObject = null;
	}

	public virtual void Reseed()
	{
		seedOffset.Value = DateTime.UtcNow.Millisecond;
	}

	public List<PondFishSilhouette> GetFishSilhouettes()
	{
		return _fishSilhouettes;
	}

	public void UpdateMaximumOccupancy()
	{
		GetFishPondData();
		if (_fishPondData == null)
		{
			return;
		}
		if (_fishPondData.MaxPopulation > 0)
		{
			maxOccupants.Value = _fishPondData.MaxPopulation;
			return;
		}
		for (int i = 1; i <= 10; i++)
		{
			if (i <= lastUnlockedPopulationGate.Value)
			{
				maxOccupants.Set(i);
				continue;
			}
			if (!(_fishPondData.PopulationGates?.ContainsKey(i) ?? false))
			{
				maxOccupants.Set(i);
				continue;
			}
			break;
		}
	}

	public FishPondData GetFishPondData()
	{
		FishPondData data_entry = GetRawData(fishType.Value);
		if (data_entry == null)
		{
			return null;
		}
		_fishPondData = data_entry;
		if (_fishPondData.SpawnTime == -1)
		{
			int value = GetFishObject().Price;
			if (value <= 30)
			{
				_fishPondData.SpawnTime = 1;
			}
			else if (value <= 80)
			{
				_fishPondData.SpawnTime = 2;
			}
			else if (value <= 120)
			{
				_fishPondData.SpawnTime = 3;
			}
			else if (value <= 250)
			{
				_fishPondData.SpawnTime = 4;
			}
			else
			{
				_fishPondData.SpawnTime = 5;
			}
		}
		return _fishPondData;
	}

	/// <summary>Get the data entry matching a fish item ID.</summary>
	/// <param name="itemId">The unqualified fish item ID.</param>
	public static FishPondData GetRawData(string itemId)
	{
		if (itemId == null)
		{
			return null;
		}
		HashSet<string> contextTags = ItemContextTagManager.GetBaseContextTags(itemId);
		if (contextTags.Contains("fish_pond_ignore"))
		{
			return null;
		}
		FishPondData selected = null;
		foreach (FishPondData data in DataLoader.FishPondData(Game1.content))
		{
			if (!(selected?.Precedence <= data.Precedence) && ItemContextTagManager.DoAllTagsMatch(data.RequiredTags, contextTags))
			{
				selected = data;
			}
		}
		return selected;
	}

	public Item GetFishProduce(Random random = null)
	{
		if (random == null)
		{
			random = Game1.random;
		}
		FishPondData data = GetFishPondData();
		if (data == null)
		{
			return null;
		}
		GameLocation location = GetParentLocation();
		Object fish = GetFishObject();
		FishPondReward selectedOutput = null;
		foreach (FishPondReward itemData in data.ProducedItems)
		{
			if (!(selectedOutput?.Precedence <= itemData.Precedence) && currentOccupants.Value >= itemData.RequiredPopulation && random.NextBool(itemData.Chance) && GameStateQuery.CheckConditions(itemData.Condition, location, null, null, fish))
			{
				selectedOutput = itemData;
			}
		}
		Item item = null;
		if (selectedOutput != null)
		{
			item = ItemQueryResolver.TryResolveRandomItem(selectedOutput, new ItemQueryContext(location, null, null, $"fish pond data '{fishType.Value}' > reward '{selectedOutput.Id}'"), avoidRepeat: false, null, (string id) => (!(ItemRegistry.QualifyItemId(selectedOutput.ItemId) == "(O)812")) ? id : ("FLAVORED_ITEM Roe " + fish.QualifiedItemId), fish);
		}
		if (item != null)
		{
			if (item.Name.Contains("Roe"))
			{
				while (random.NextDouble() < 0.2)
				{
					item.Stack++;
				}
			}
			if (goldenAnimalCracker.Value)
			{
				item.Stack *= 2;
			}
		}
		return item;
	}

	private Item CreateFishInstance()
	{
		return new Object(fishType.Value, 1);
	}

	public override bool doAction(Vector2 tileLocation, Farmer who)
	{
		if (daysOfConstructionLeft.Value <= 0 && occupiesTile(tileLocation))
		{
			if (who.isMoving())
			{
				Game1.haltAfterCheck = false;
			}
			if (who.ActiveObject != null && performActiveObjectDropInAction(who, probe: false))
			{
				return true;
			}
			if (output.Value != null)
			{
				Item item = output.Value;
				output.Value = null;
				if (who.addItemToInventoryBool(item))
				{
					Game1.playSound("coin", null);
					int bonusExperience = 0;
					if (item is Object obj)
					{
						bonusExperience = (int)((float)obj.sellToStorePrice(-1L) * HARVEST_OUTPUT_EXP_MULTIPLIER);
					}
					who.gainExperience(1, bonusExperience + HARVEST_BASE_EXP);
				}
				else
				{
					output.Value = item;
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
				}
				return true;
			}
			if (who.ActiveObject != null && HasUnresolvedNeeds() && who.ActiveObject.QualifiedItemId == neededItem.Value.QualifiedItemId)
			{
				if (neededItemCount.Value == 1)
				{
					showObjectThrownIntoPondAnimation(who, who.ActiveObject, delegate
					{
						if (neededItemCount.Value <= 0)
						{
							Game1.playSound("jingle1", null);
						}
					});
				}
				else
				{
					showObjectThrownIntoPondAnimation(who, who.ActiveObject);
				}
				who.reduceActiveItemByOne();
				if (who == Game1.player)
				{
					neededItemCount.Value--;
					if (neededItemCount.Value <= 0)
					{
						needsMutex.RequestLock(delegate
						{
							needsMutex.ReleaseLock();
							ResolveNeeds(who);
						});
						neededItemCount.Value = -1;
					}
				}
				if (neededItemCount.Value <= 0)
				{
					animateHappyFishEvent.Fire();
				}
				return true;
			}
			if (who.ActiveObject != null && (who.ActiveObject.Category == -4 || who.ActiveObject.QualifiedItemId == "(O)393" || who.ActiveObject.QualifiedItemId == "(O)397"))
			{
				if (fishType.Value != null)
				{
					if (!isLegalFishForPonds(fishType.Value))
					{
						string heldFishName = who.ActiveObject.DisplayName;
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:CantPutInPonds", heldFishName.ToLower()));
						return true;
					}
					if (who.ActiveObject.ItemId != fishType.Value)
					{
						string heldFishName2 = who.ActiveObject.DisplayName;
						if (who.ActiveObject.QualifiedItemId == "(O)393" || who.ActiveObject.QualifiedItemId == "(O)397")
						{
							Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:WrongFishTypeCoral", heldFishName2));
						}
						else
						{
							string displayName = ItemRegistry.GetDataOrErrorItem(fishType.Value).DisplayName;
							if (Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.de)
							{
								Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:WrongFishType", heldFishName2, displayName));
							}
							else
							{
								Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:WrongFishType", heldFishName2.ToLower(), displayName.ToLower()));
							}
						}
						return true;
					}
					if (currentOccupants.Value >= maxOccupants.Value)
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:PondFull"));
						return true;
					}
					return addFishToPond(who, who.ActiveObject);
				}
				if (!isLegalFishForPonds(who.ActiveObject.ItemId))
				{
					string heldFishName3 = who.ActiveObject.DisplayName;
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:CantPutInPonds", heldFishName3));
					return true;
				}
				return addFishToPond(who, who.ActiveObject);
			}
			if (fishType.Value != null)
			{
				if (Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
				{
					Game1.playSound("bigSelect", null);
					Game1.activeClickableMenu = new PondQueryMenu(this);
					return true;
				}
			}
			else if (Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Buildings:NoFish"));
				return true;
			}
		}
		return base.doAction(tileLocation, who);
	}

	public void AnimateHappyFish()
	{
		_numberOfFishToJump = currentOccupants.Value;
		_timeUntilFishHop = 1f;
	}

	public Vector2 GetItemBucketTile()
	{
		return new Vector2(tileX.Value + 4, tileY.Value + 4);
	}

	public Vector2 GetRequestTile()
	{
		return new Vector2(tileX.Value + 2, tileY.Value + 2);
	}

	public Vector2 GetCenterTile()
	{
		return new Vector2(tileX.Value + 2, tileY.Value + 2);
	}

	public void ResolveNeeds(Farmer who)
	{
		Reseed();
		hasCompletedRequest.Value = true;
		lastUnlockedPopulationGate.Value = maxOccupants.Value + 1;
		UpdateMaximumOccupancy();
		daysSinceSpawn.Value = 0;
		int bonusExperience = 0;
		FishPondData fishData = GetFishPondData();
		if (fishData != null)
		{
			bonusExperience = (int)((float)fishData.SpawnTime * QUEST_SPAWNRATE_EXP_MULTIPIER);
		}
		who.gainExperience(1, bonusExperience + QUEST_BASE_EXP);
		Random r = Utility.CreateDaySaveRandom(seedOffset.Value);
		Game1.showGlobalMessage(PondQueryMenu.getCompletedRequestString(this, GetFishObject(), r));
	}

	public override void resetLocalState()
	{
		base.resetLocalState();
		_jumpingFish.Clear();
		while (_fishSilhouettes.Count < currentOccupants.Value)
		{
			PondFishSilhouette silhouette = new PondFishSilhouette(this);
			_fishSilhouettes.Add(silhouette);
			silhouette.position = (GetCenterTile() + new Vector2(Utility.Lerp(-0.5f, 0.5f, (float)Game1.random.NextDouble()) * (float)(tilesWide.Value - 2), Utility.Lerp(-0.5f, 0.5f, (float)Game1.random.NextDouble()) * (float)(tilesHigh.Value - 2))) * 64f;
		}
	}

	private bool isLegalFishForPonds(string itemId)
	{
		return GetRawData(itemId) != null;
	}

	private void showObjectThrownIntoPondAnimation(Farmer who, Object whichObject, Action callback = null)
	{
		who.faceGeneralDirection(GetCenterTile() * 64f + new Vector2(32f, 32f));
		if (who.FacingDirection == 1 || who.FacingDirection == 3)
		{
			float distance = Vector2.Distance(who.Position, GetCenterTile() * 64f);
			float verticalDistance = GetCenterTile().Y * 64f + 32f - who.position.Y;
			distance -= 8f;
			float gravity = 0.0025f;
			float velocity = (float)((double)distance * Math.Sqrt(gravity / (2f * (distance + 96f))));
			float t = 2f * (velocity / gravity) + (float)((Math.Sqrt(velocity * velocity + 2f * gravity * 96f) - (double)velocity) / (double)gravity);
			t += verticalDistance;
			float xVelocityReduction = 0f;
			if (verticalDistance > 0f)
			{
				xVelocityReduction = verticalDistance / 832f;
				t += xVelocityReduction * 200f;
			}
			Game1.playSound("throwDownITem", null);
			TemporaryAnimatedSpriteList fishTossSprites = new TemporaryAnimatedSpriteList();
			ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(whichObject.QualifiedItemId);
			fishTossSprites.Add(new TemporaryAnimatedSprite(itemData.GetTextureName(), itemData.GetSourceRect(0, null), who.Position + new Vector2(0f, -64f), flipped: false, 0f, Color.White)
			{
				scale = 4f,
				layerDepth = 1f,
				totalNumberOfLoops = 1,
				interval = t,
				motion = new Vector2((float)((who.FacingDirection != 3) ? 1 : (-1)) * (velocity - xVelocityReduction), (0f - velocity) * 3f / 2f),
				acceleration = new Vector2(0f, gravity),
				timeBasedMotion = true
			});
			fishTossSprites.Add(new TemporaryAnimatedSprite(28, 100f, 2, 1, GetCenterTile() * 64f, flicker: false, flipped: false)
			{
				delayBeforeAnimationStart = (int)t,
				layerDepth = (((float)tileY.Value + 0.5f) * 64f + 2f) / 10000f
			});
			fishTossSprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 55f, 8, 0, GetCenterTile() * 64f, flicker: false, Game1.random.NextBool(), (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f)
			{
				delayBeforeAnimationStart = (int)t
			});
			fishTossSprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 65f, 8, 0, GetCenterTile() * 64f + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-16, 32)), flicker: false, Game1.random.NextBool(), (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f)
			{
				delayBeforeAnimationStart = (int)t
			});
			fishTossSprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 75f, 8, 0, GetCenterTile() * 64f + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-16, 32)), flicker: false, Game1.random.NextBool(), (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f)
			{
				delayBeforeAnimationStart = (int)t
			});
			if (who.IsLocalPlayer)
			{
				DelayedAction.playSoundAfterDelay("waterSlosh", (int)t, who.currentLocation, null);
				if (callback != null)
				{
					DelayedAction.functionAfterDelay(callback, (int)t);
				}
			}
			if (fishType.Value != null && whichObject.ItemId == fishType.Value)
			{
				_delayUntilFishSilhouetteAdded = t / 1000f;
			}
			Game1.multiplayer.broadcastSprites(who.currentLocation, fishTossSprites);
			return;
		}
		float distance2 = Vector2.Distance(who.Position, GetCenterTile() * 64f);
		float height = Math.Abs(distance2);
		if (who.FacingDirection == 0)
		{
			distance2 = 0f - distance2;
			height += 64f;
		}
		float horizontalDistance = GetCenterTile().X * 64f - who.position.X;
		float gravity2 = 0.0025f;
		float velocity2 = (float)Math.Sqrt(2f * gravity2 * height);
		float t2 = (float)(Math.Sqrt(2f * (height - distance2) / gravity2) + (double)(velocity2 / gravity2));
		t2 *= 1.05f;
		t2 = ((who.FacingDirection != 0) ? (t2 * 2.5f) : (t2 * 0.7f));
		t2 -= Math.Abs(horizontalDistance) / ((who.FacingDirection == 0) ? 100f : 2f);
		Game1.playSound("throwDownITem", null);
		TemporaryAnimatedSpriteList fishTossSprites2 = new TemporaryAnimatedSpriteList();
		ParsedItemData itemData2 = ItemRegistry.GetDataOrErrorItem(whichObject.QualifiedItemId);
		fishTossSprites2.Add(new TemporaryAnimatedSprite(itemData2.GetTextureName(), itemData2.GetSourceRect(0, null), who.Position + new Vector2(0f, -64f), flipped: false, 0f, Color.White)
		{
			scale = 4f,
			layerDepth = 1f,
			totalNumberOfLoops = 1,
			interval = t2,
			motion = new Vector2(horizontalDistance / ((who.FacingDirection == 0) ? 900f : 1000f), 0f - velocity2),
			acceleration = new Vector2(0f, gravity2),
			timeBasedMotion = true
		});
		fishTossSprites2.Add(new TemporaryAnimatedSprite(28, 100f, 2, 1, GetCenterTile() * 64f, flicker: false, flipped: false)
		{
			delayBeforeAnimationStart = (int)t2,
			layerDepth = (((float)tileY.Value + 0.5f) * 64f + 2f) / 10000f
		});
		fishTossSprites2.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 55f, 8, 0, GetCenterTile() * 64f, flicker: false, Game1.random.NextBool(), (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f)
		{
			delayBeforeAnimationStart = (int)t2
		});
		fishTossSprites2.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 65f, 8, 0, GetCenterTile() * 64f + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-16, 32)), flicker: false, Game1.random.NextBool(), (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f)
		{
			delayBeforeAnimationStart = (int)t2
		});
		fishTossSprites2.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 0, 64, 64), 75f, 8, 0, GetCenterTile() * 64f + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-16, 32)), flicker: false, Game1.random.NextBool(), (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f)
		{
			delayBeforeAnimationStart = (int)t2
		});
		if (who.IsLocalPlayer)
		{
			DelayedAction.playSoundAfterDelay("waterSlosh", (int)t2, who.currentLocation, null);
			if (callback != null)
			{
				DelayedAction.functionAfterDelay(callback, (int)t2);
			}
		}
		if (fishType.Value != null && whichObject.ItemId == fishType.Value)
		{
			_delayUntilFishSilhouetteAdded = t2 / 1000f;
		}
		Game1.multiplayer.broadcastSprites(who.currentLocation, fishTossSprites2);
	}

	private bool addFishToPond(Farmer who, Object fish)
	{
		who.reduceActiveItemByOne();
		currentOccupants.Value++;
		if (currentOccupants.Value == 1)
		{
			fishType.Value = fish.ItemId;
			_fishPondData = null;
			UpdateMaximumOccupancy();
		}
		showObjectThrownIntoPondAnimation(who, fish);
		return true;
	}

	public override void dayUpdate(int dayOfMonth)
	{
		hasSpawnedFish.Value = false;
		_hasAnimatedSpawnedFish = false;
		if (hasCompletedRequest.Value)
		{
			neededItem.Value = null;
			neededItemCount.Set(-1);
			hasCompletedRequest.Value = false;
		}
		FishPondData data = GetFishPondData();
		if (currentOccupants.Value > 0 && data != null)
		{
			Random r = Utility.CreateDaySaveRandom(tileX.Value * 1000, tileY.Value * 2000);
			if ((data.BaseMinProduceChance >= data.BaseMaxProduceChance) ? r.NextBool(data.BaseMinProduceChance) : (r.NextDouble() < (double)Utility.Lerp(data.BaseMinProduceChance, data.BaseMaxProduceChance, (float)currentOccupants.Value / 10f)))
			{
				output.Value = GetFishProduce(r);
			}
			daysSinceSpawn.Value++;
			if (daysSinceSpawn.Value > data.SpawnTime)
			{
				daysSinceSpawn.Value = data.SpawnTime;
			}
			if (daysSinceSpawn.Value >= data.SpawnTime)
			{
				if (TryGetNeededItemData(out var itemId, out var count))
				{
					if (currentOccupants.Value >= maxOccupants.Value && neededItem.Value == null)
					{
						neededItem.Value = ItemRegistry.Create(itemId);
						neededItemCount.Value = count;
					}
				}
				else
				{
					SpawnFish();
				}
			}
			if (currentOccupants.Value == 10 && fishType.Value == "717")
			{
				foreach (Farmer f in Game1.getAllFarmers())
				{
					if (f.mailReceived.Add("FullCrabPond"))
					{
						f.activeDialogueEvents["FullCrabPond"] = 14;
					}
				}
			}
			doFishSpecificWaterColoring();
		}
		base.dayUpdate(dayOfMonth);
	}

	private void doFishSpecificWaterColoring()
	{
		FishPondData data = GetFishPondData();
		Color? customColor = null;
		if (data != null && data.WaterColor?.Count > 0)
		{
			foreach (FishPondWaterColor entry in data.WaterColor)
			{
				if (currentOccupants.Value >= entry.MinPopulation && lastUnlockedPopulationGate.Value >= entry.MinUnlockedPopulationGate && (entry.Condition == null || GameStateQuery.CheckConditions(entry.Condition, GetParentLocation(), null, null, GetFishObject())))
				{
					if (entry.Color.EqualsIgnoreCase("CopyFromInput"))
					{
						Object fish = GetFishObject();
						ColoredObject obj = fish as ColoredObject;
						customColor = ((obj != null) ? new Color?(obj.color.Value) : ItemContextTagManager.GetColorFromTags(fish));
					}
					else
					{
						customColor = Utility.StringToColor(entry.Color);
					}
					break;
				}
			}
		}
		overrideWaterColor.Value = customColor ?? Color.White;
	}

	/// <inheritdoc />
	public override Color? GetWaterColor(Vector2 tile)
	{
		if (!(overrideWaterColor.Value != Color.White))
		{
			return null;
		}
		return overrideWaterColor.Value;
	}

	public bool JumpFish()
	{
		if (_fishSilhouettes.Count == 0)
		{
			return false;
		}
		PondFishSilhouette fish_silhouette = Game1.random.ChooseFrom(_fishSilhouettes);
		_fishSilhouettes.Remove(fish_silhouette);
		_jumpingFish.Add(new JumpingFish(this, fish_silhouette.position, (GetCenterTile() + new Vector2(0.5f, 0.5f)) * 64f));
		return true;
	}

	public void SpawnFish()
	{
		if (currentOccupants.Value < maxOccupants.Value && currentOccupants.Value > 0)
		{
			hasSpawnedFish.Value = true;
			daysSinceSpawn.Value = 0;
			currentOccupants.Value += 1;
			if (currentOccupants.Value > maxOccupants.Value)
			{
				currentOccupants.Value = maxOccupants.Value;
			}
		}
	}

	public override bool performActiveObjectDropInAction(Farmer who, bool probe)
	{
		Object heldObj = who.ActiveObject;
		if (IsValidSignItem(heldObj) && (sign.Value == null || heldObj.QualifiedItemId != sign.Value.QualifiedItemId))
		{
			if (probe)
			{
				return true;
			}
			Object oldSign = sign.Value;
			sign.Value = (Object)heldObj.getOne();
			who.reduceActiveItemByOne();
			if (oldSign != null)
			{
				Game1.createItemDebris(oldSign, new Vector2((float)tileX.Value + 0.5f, tileY.Value + tilesHigh.Value) * 64f, 3, who.currentLocation);
			}
			who.currentLocation.playSound("axe", null, null);
			return true;
		}
		if (heldObj?.QualifiedItemId == "(O)GoldenAnimalCracker" && !goldenAnimalCracker.Value && currentOccupants.Value > 0)
		{
			if (probe)
			{
				return true;
			}
			who.reduceActiveItemByOne();
			goldenAnimalCracker.Value = true;
			isPlayingGoldenCrackerAnimation.Value = true;
			showObjectThrownIntoPondAnimation(who, heldObj, delegate
			{
				isPlayingGoldenCrackerAnimation.Value = false;
			});
			return true;
		}
		return base.performActiveObjectDropInAction(who, probe);
	}

	public override void performToolAction(Tool t, int tileX, int tileY)
	{
		if ((t is Axe || t is Pickaxe) && sign.Value != null)
		{
			if (t.getLastFarmerToUse() != null)
			{
				Game1.createItemDebris(sign.Value, new Vector2((float)base.tileX.Value + 0.5f, base.tileY.Value + tilesHigh.Value) * 64f, 3, t.getLastFarmerToUse().currentLocation);
			}
			sign.Value = null;
			t.getLastFarmerToUse().currentLocation.playSound("hammer", new Vector2(tileX, tileY), null);
		}
		base.performToolAction(t, tileX, tileY);
	}

	/// <inheritdoc />
	public override void performActionOnConstruction(GameLocation location, Farmer who)
	{
		base.performActionOnConstruction(location, who);
		nettingStyle.Value = (tileX.Value / 3 + tileY.Value / 3) % 3;
	}

	/// <inheritdoc />
	public override void performActionOnBuildingPlacement()
	{
		base.performActionOnBuildingPlacement();
		nettingStyle.Value = (tileX.Value / 3 + tileY.Value / 3) % 3;
	}

	public bool HasUnresolvedNeeds()
	{
		if (neededItem.Value != null && TryGetNeededItemData(out var _, out var _))
		{
			return !hasCompletedRequest.Value;
		}
		return false;
	}

	private bool TryGetNeededItemData(out string itemId, out int count)
	{
		itemId = null;
		count = 1;
		if (currentOccupants.Value < maxOccupants.Value)
		{
			return false;
		}
		GetFishPondData();
		if (_fishPondData?.PopulationGates != null)
		{
			if (maxOccupants.Value + 1 <= lastUnlockedPopulationGate.Value)
			{
				return false;
			}
			if (_fishPondData.PopulationGates.TryGetValue(maxOccupants.Value + 1, out var gate))
			{
				Random r = Utility.CreateDaySaveRandom(Utility.CreateRandomSeed(tileX.Value * 1000, tileY.Value * 2000));
				string[] split_data = ArgUtility.SplitBySpace(r.ChooseFrom(gate));
				if (split_data.Length >= 1)
				{
					itemId = split_data[0];
				}
				if (split_data.Length >= 3)
				{
					count = r.Next(Convert.ToInt32(split_data[1]), Convert.ToInt32(split_data[2]) + 1);
				}
				else if (split_data.Length >= 2)
				{
					count = Convert.ToInt32(split_data[1]);
				}
				return true;
			}
		}
		return false;
	}

	public void ClearPond()
	{
		Rectangle r = GetBoundingBox();
		for (int i = 0; i < currentOccupants.Value; i++)
		{
			Vector2 pos = Utility.PointToVector2(r.Center);
			int direction = Game1.random.Next(4);
			switch (direction)
			{
			case 0:
				pos = new Vector2(Game1.random.Next(r.Left, r.Right), r.Top);
				break;
			case 1:
				pos = new Vector2(r.Right, Game1.random.Next(r.Top, r.Bottom));
				break;
			case 2:
				pos = new Vector2(Game1.random.Next(r.Left, r.Right), r.Bottom);
				break;
			case 3:
				pos = new Vector2(r.Left, Game1.random.Next(r.Top, r.Bottom));
				break;
			}
			Game1.createItemDebris(CreateFishInstance(), pos, direction, Game1.currentLocation, -1, flopFish: true);
		}
		_hasAnimatedSpawnedFish = false;
		hasSpawnedFish.Value = false;
		_fishSilhouettes.Clear();
		_jumpingFish.Clear();
		goldenAnimalCracker.Value = false;
		isPlayingGoldenCrackerAnimation.Value = false;
		_fishObject = null;
		currentOccupants.Value = 0;
		daysSinceSpawn.Value = 0;
		neededItem.Value = null;
		neededItemCount.Value = -1;
		lastUnlockedPopulationGate.Value = 0;
		fishType.Value = null;
		Reseed();
		overrideWaterColor.Value = Color.White;
	}

	public Object CatchFish()
	{
		if (currentOccupants.Value == 0)
		{
			return null;
		}
		currentOccupants.Value--;
		return (Object)CreateFishInstance();
	}

	public Object GetFishObject()
	{
		if (_fishObject == null)
		{
			_fishObject = new Object(fishType.Value, 1);
		}
		return _fishObject;
	}

	public override void Update(GameTime time)
	{
		needsMutex.Update(GetParentLocation());
		animateHappyFishEvent.Poll();
		if (!_hasAnimatedSpawnedFish && hasSpawnedFish.Value && _numberOfFishToJump <= 0 && Utility.isOnScreen((GetCenterTile() + new Vector2(0.5f, 0.5f)) * 64f, 64))
		{
			_hasAnimatedSpawnedFish = true;
			if (fishType.Value != "393" && fishType.Value != "397")
			{
				_numberOfFishToJump = 1;
				_timeUntilFishHop = Utility.RandomFloat(2f, 5f);
			}
		}
		if (_delayUntilFishSilhouetteAdded > 0f)
		{
			_delayUntilFishSilhouetteAdded -= (float)time.ElapsedGameTime.TotalSeconds;
			if (_delayUntilFishSilhouetteAdded < 0f)
			{
				_delayUntilFishSilhouetteAdded = 0f;
			}
		}
		if (_numberOfFishToJump > 0 && _timeUntilFishHop > 0f)
		{
			_timeUntilFishHop -= (float)time.ElapsedGameTime.TotalSeconds;
			if (_timeUntilFishHop <= 0f && JumpFish())
			{
				_numberOfFishToJump--;
				_timeUntilFishHop = Utility.RandomFloat(0.15f, 0.25f);
			}
		}
		while (_fishSilhouettes.Count > currentOccupants.Value - _jumpingFish.Count)
		{
			_fishSilhouettes.RemoveAt(0);
		}
		if (_delayUntilFishSilhouetteAdded <= 0f)
		{
			while (_fishSilhouettes.Count < currentOccupants.Value - _jumpingFish.Count)
			{
				_fishSilhouettes.Add(new PondFishSilhouette(this));
			}
		}
		for (int i = 0; i < _fishSilhouettes.Count; i++)
		{
			_fishSilhouettes[i].Update((float)time.ElapsedGameTime.TotalSeconds);
		}
		for (int j = 0; j < _jumpingFish.Count; j++)
		{
			if (_jumpingFish[j].Update((float)time.ElapsedGameTime.TotalSeconds))
			{
				PondFishSilhouette new_silhouette = new PondFishSilhouette(this);
				new_silhouette.position = _jumpingFish[j].position;
				_fishSilhouettes.Add(new_silhouette);
				_jumpingFish.RemoveAt(j);
				j--;
			}
		}
		base.Update(time);
	}

	public override bool isTileFishable(Vector2 tile)
	{
		if (daysOfConstructionLeft.Value > 0)
		{
			return false;
		}
		if (tile.X > (float)tileX.Value && tile.X < (float)(tileX.Value + tilesWide.Value - 1) && tile.Y > (float)tileY.Value)
		{
			return tile.Y < (float)(tileY.Value + tilesHigh.Value - 1);
		}
		return false;
	}

	public override bool CanRefillWateringCan()
	{
		return daysOfConstructionLeft.Value <= 0;
	}

	public override Rectangle? getSourceRectForMenu()
	{
		return new Rectangle(0, 0, 80, 80);
	}

	public override void drawInMenu(SpriteBatch b, int x, int y)
	{
		BuildingData data = GetData();
		y += 32;
		if (ShouldDrawShadow(data))
		{
			drawShadow(b, x, y);
		}
		b.Draw(texture.Value, new Vector2(x, y), new Rectangle(0, 80, 80, 80), new Color(60, 126, 150) * alpha, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, 0.75f);
		for (int yWater = tileY.Value; yWater < tileY.Value + 5; yWater++)
		{
			for (int xWater = tileX.Value; xWater < tileX.Value + 4; xWater++)
			{
				bool num = yWater == tileY.Value + 4;
				bool topY = yWater == tileY.Value;
				if (num)
				{
					b.Draw(Game1.mouseCursors, new Vector2(x + xWater * 64 + 32, y + (yWater + 1) * 64 - (int)Game1.currentLocation.waterPosition - 32), new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((xWater + yWater) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)), 64, 32 + (int)Game1.currentLocation.waterPosition - 5), Game1.currentLocation.waterColor.Value, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.8f);
				}
				else
				{
					b.Draw(Game1.mouseCursors, new Vector2(x + xWater * 64 + 32, y + yWater * 64 + 32 - (int)((!topY) ? Game1.currentLocation.waterPosition : 0f)), new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((xWater + yWater) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)) + (topY ? ((int)Game1.currentLocation.waterPosition) : 0), 64, 64 + (topY ? ((int)(0f - Game1.currentLocation.waterPosition)) : 0)), Game1.currentLocation.waterColor.Value, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.8f);
				}
			}
		}
		b.Draw(texture.Value, new Vector2(x, y), new Rectangle(0, 0, 80, 80), color * alpha, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, 0.9f);
		b.Draw(texture.Value, new Vector2(x + 64, y + 44 + ((Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2500.0 < 1250.0) ? 4 : 0)), new Rectangle(16, 160, 48, 7), color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.95f);
		b.Draw(texture.Value, new Vector2(x, y - 128), new Rectangle(80, 0, 80, 48), color * alpha, 0f, new Vector2(0f, 0f), 4f, SpriteEffects.None, 1f);
	}

	public override void OnEndMove()
	{
		foreach (PondFishSilhouette fishSilhouette in _fishSilhouettes)
		{
			fishSilhouette.position = (GetCenterTile() + new Vector2(Utility.Lerp(-0.5f, 0.5f, (float)Game1.random.NextDouble()) * (float)(tilesWide.Value - 2), Utility.Lerp(-0.5f, 0.5f, (float)Game1.random.NextDouble()) * (float)(tilesHigh.Value - 2))) * 64f;
		}
	}

	public override void draw(SpriteBatch b)
	{
		if (base.isMoving)
		{
			return;
		}
		if (daysOfConstructionLeft.Value > 0)
		{
			drawInConstruction(b);
			return;
		}
		BuildingData data = GetData();
		for (int i = animations.Count - 1; i >= 0; i--)
		{
			animations[i].draw(b);
		}
		if (ShouldDrawShadow(data))
		{
			drawShadow(b);
		}
		b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64)), new Rectangle(0, 80, 80, 80), ((overrideWaterColor.Value == Color.White) ? new Color(60, 126, 150) : overrideWaterColor.Value) * alpha, 0f, new Vector2(0f, 80f), 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f - 3f) / 10000f);
		for (int y = tileY.Value; y < tileY.Value + 5; y++)
		{
			for (int x = tileX.Value; x < tileX.Value + 4; x++)
			{
				bool num = y == tileY.Value + 4;
				bool topY = y == tileY.Value;
				if (num)
				{
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32, (y + 1) * 64 - (int)Game1.currentLocation.waterPosition - 32)), new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((x + y) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)), 64, 32 + (int)Game1.currentLocation.waterPosition - 5), overrideWaterColor.Equals(Color.White) ? Game1.currentLocation.waterColor.Value : (overrideWaterColor.Value * 0.5f), 0f, Vector2.Zero, 1f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f - 2f) / 10000f);
				}
				else
				{
					b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32, y * 64 + 32 - (int)((!topY) ? Game1.currentLocation.waterPosition : 0f))), new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((x + y) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)) + (topY ? ((int)Game1.currentLocation.waterPosition) : 0), 64, 64 + (topY ? ((int)(0f - Game1.currentLocation.waterPosition)) : 0)), (overrideWaterColor.Value == Color.White) ? Game1.currentLocation.waterColor.Value : (overrideWaterColor.Value * 0.5f), 0f, Vector2.Zero, 1f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f - 2f) / 10000f);
				}
			}
		}
		if (overrideWaterColor.Value.Equals(Color.White))
		{
			b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 64, tileY.Value * 64 + 44 + ((Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2500.0 < 1250.0) ? 4 : 0))), new Rectangle(16, 160, 48, 7), color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f);
		}
		b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64)), new Rectangle(0, 0, 80, 80), color * alpha, 0f, new Vector2(0f, 80f), 4f, SpriteEffects.None, ((float)tileY.Value + 0.5f) * 64f / 10000f);
		if (nettingStyle.Value < 3)
		{
			b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64 + tilesHigh.Value * 64 - 128)), new Rectangle(80, nettingStyle.Value * 48, 80, 48), color * alpha, 0f, new Vector2(0f, 80f), 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 2f) / 10000f);
		}
		if (sign.Value != null)
		{
			ParsedItemData signDraw = ItemRegistry.GetDataOrErrorItem(sign.Value.QualifiedItemId);
			b.Draw(signDraw.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 8, tileY.Value * 64 + tilesHigh.Value * 64 - 128 - 32)), signDraw.GetSourceRect(0, null), color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 2f) / 10000f);
			if (fishType.Value != null)
			{
				ParsedItemData fishDraw = ItemRegistry.GetData(fishType.Value);
				if (fishDraw != null)
				{
					Texture2D fishTexture = fishDraw.GetTexture();
					Rectangle fishSourceRect = fishDraw.GetSourceRect(0, null);
					float yOffset = ((maxOccupants.Value == 1) ? 6f : 0f);
					b.Draw(fishTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 8 + 8 - 4, (float)(tileY.Value * 64 + tilesHigh.Value * 64 - 128 - 8 + 4) + yOffset)), fishSourceRect, Color.Black * 0.4f * alpha, 0f, Vector2.Zero, 3f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 3f) / 10000f);
					b.Draw(fishTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 8 + 8 - 1, (float)(tileY.Value * 64 + tilesHigh.Value * 64 - 128 - 8 + 1) + yOffset)), fishSourceRect, color * alpha, 0f, Vector2.Zero, 3f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 4f) / 10000f);
					if (maxOccupants.Value > 1)
					{
						Utility.drawTinyDigits(currentOccupants.Value, b, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 32 + 8 + ((currentOccupants.Value < 10) ? 8 : 0), tileY.Value * 64 + tilesHigh.Value * 64 - 96)), 3f, (((float)tileY.Value + 0.5f) * 64f + 5f) / 10000f, Color.LightYellow * alpha);
					}
				}
			}
		}
		if (_fishObject != null && (_fishObject.QualifiedItemId == "(O)393" || _fishObject.QualifiedItemId == "(O)397"))
		{
			for (int j = 0; j < currentOccupants.Value; j++)
			{
				Vector2 drawOffset = Vector2.Zero;
				int drawI = (j + seedOffset.Value) % 10;
				switch (drawI)
				{
				case 0:
					drawOffset = new Vector2(0f, 0f);
					break;
				case 1:
					drawOffset = new Vector2(48f, 32f);
					break;
				case 2:
					drawOffset = new Vector2(80f, 72f);
					break;
				case 3:
					drawOffset = new Vector2(140f, 28f);
					break;
				case 4:
					drawOffset = new Vector2(96f, 0f);
					break;
				case 5:
					drawOffset = new Vector2(0f, 96f);
					break;
				case 6:
					drawOffset = new Vector2(140f, 80f);
					break;
				case 7:
					drawOffset = new Vector2(64f, 120f);
					break;
				case 8:
					drawOffset = new Vector2(140f, 140f);
					break;
				case 9:
					drawOffset = new Vector2(0f, 150f);
					break;
				}
				b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 64 + 7, tileY.Value * 64 + 64 + 32) + drawOffset), Game1.shadowTexture.Bounds, color * alpha, 0f, Vector2.Zero, 3f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f - 2f) / 10000f - 1.1E-05f);
				ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem("(O)" + fishType.Value);
				Texture2D sprite = dataOrErrorItem.GetTexture();
				Rectangle sourceRect = dataOrErrorItem.GetSourceRect(0, null);
				b.Draw(sprite, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 64, tileY.Value * 64 + 64) + drawOffset), sourceRect, color * alpha * 0.75f, 0f, Vector2.Zero, 3f, (drawI % 3 == 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f - 2f) / 10000f - 1E-05f);
			}
		}
		else
		{
			for (int k = 0; k < _fishSilhouettes.Count; k++)
			{
				_fishSilhouettes[k].Draw(b);
			}
		}
		for (int l = 0; l < _jumpingFish.Count; l++)
		{
			_jumpingFish[l].Draw(b);
		}
		if (HasUnresolvedNeeds())
		{
			Vector2 drawn_position = GetRequestTile() * 64f;
			drawn_position += 64f * new Vector2(0.5f, 0.5f);
			float y_offset = 3f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
			float bubble_layer_depth = (drawn_position.Y + 160f) / 10000f + 1E-06f;
			drawn_position.Y += y_offset - 32f;
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, drawn_position), new Rectangle(403, 496, 5, 14), Color.White * 0.75f, 0f, new Vector2(2f, 14f), 4f, SpriteEffects.None, bubble_layer_depth);
		}
		bool showGoldenCracker = goldenAnimalCracker.Value && !isPlayingGoldenCrackerAnimation.Value;
		if (showGoldenCracker)
		{
			b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64) + new Vector2(65f, 59f) * 4f), new Rectangle(130, 160, 15, 16), color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 2f) / 10000f);
		}
		if (output.Value != null)
		{
			b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64) + new Vector2(65f, 59f) * 4f), new Rectangle(0, 160, 15, 16), color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 1f) / 10000f);
			if (showGoldenCracker)
			{
				b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64) + new Vector2(65f, 59f) * 4f), new Rectangle(145, 160, 15, 16), color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (((float)tileY.Value + 0.5f) * 64f + 3f) / 10000f);
			}
			Vector2 vector = GetItemBucketTile() * 64f;
			float y_offset2 = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
			Vector2 bubble_draw_position = vector + new Vector2(0f, -2f) * 64f + new Vector2(0f, y_offset2);
			Vector2 item_relative_to_bubble = new Vector2(40f, 36f);
			float bubble_layer_depth2 = (vector.Y + 64f) / 10000f + 1E-06f;
			float item_layer_depth = (vector.Y + 64f) / 10000f + 1E-05f;
			b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, bubble_draw_position), new Rectangle(141, 465, 20, 24), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, bubble_layer_depth2);
			ParsedItemData outputDraw = ItemRegistry.GetDataOrErrorItem(output.Value.QualifiedItemId);
			Texture2D outputTexture = outputDraw.GetTexture();
			b.Draw(outputTexture, Game1.GlobalToLocal(Game1.viewport, bubble_draw_position + item_relative_to_bubble), outputDraw.GetSourceRect(0, null), Color.White * 0.75f, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, item_layer_depth);
			if (output.Value is ColoredObject coloredObj)
			{
				Rectangle colored_source_rect = ItemRegistry.GetDataOrErrorItem(output.Value.QualifiedItemId).GetSourceRect(1, null);
				b.Draw(outputTexture, Game1.GlobalToLocal(Game1.viewport, bubble_draw_position + item_relative_to_bubble), colored_source_rect, coloredObj.color.Value * 0.75f, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, item_layer_depth + 1E-05f);
			}
			if (output.Value.Stack > 1)
			{
				Utility.drawTinyDigits(output.Value.Stack, b, Game1.GlobalToLocal(Game1.viewport, bubble_draw_position + item_relative_to_bubble + new Vector2(16f, 12f)), 3f, item_layer_depth + 2E-05f, Color.LightYellow * alpha);
			}
		}
	}

	/// <summary>Get whether an item can be placed on the fish pond as a sign.</summary>
	/// <param name="item">The item to check.</param>
	public bool IsValidSignItem(Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (!item.HasContextTag("sign_item"))
		{
			return item.QualifiedItemId == "(BC)34";
		}
		return true;
	}
}
