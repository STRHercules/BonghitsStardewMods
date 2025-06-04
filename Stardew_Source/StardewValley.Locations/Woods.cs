using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Network;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace StardewValley.Locations;

public class Woods : GameLocation
{
	public const int numBaubles = 25;

	private List<Vector2> baubles;

	private List<WeatherDebris> weatherDebris;

	[XmlElement("hasUnlockedStatue")]
	public readonly NetBool hasUnlockedStatue = new NetBool();

	[XmlElement("addedSlimesToday")]
	private readonly NetBool addedSlimesToday = new NetBool();

	[XmlIgnore]
	private readonly NetEvent0 statueAnimationEvent = new NetEvent0();

	protected Color _ambientLightColor = Color.White;

	private int statueTimer;

	public Woods()
	{
	}

	public Woods(string map, string name)
		: base(map, name)
	{
		isOutdoors.Value = true;
		ignoreDebrisWeather.Value = true;
		ignoreOutdoorLighting.Value = true;
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(addedSlimesToday, "addedSlimesToday").AddField(statueAnimationEvent, "statueAnimationEvent").AddField(hasUnlockedStatue, "hasUnlockedStatue");
		statueAnimationEvent.onEvent += doStatueAnimation;
	}

	/// <summary>Reset the crow shop which contains lost unique items.</summary>
	public static void ResetLostItemsShop()
	{
		IInventory shopItems = GetLostItemsShopInventory();
		shopItems.Clear();
		Dictionary<string, int> itemsInSave = new Dictionary<string, int>();
		Utility.ForEachItem(delegate(Item item)
		{
			itemsInSave[item.QualifiedItemId] = itemsInSave.GetValueOrDefault(item.QualifiedItemId) + item.Stack;
			return true;
		});
		Dictionary<string, int> eventsSeen = new Dictionary<string, int>();
		Dictionary<string, int> mailFlags = new Dictionary<string, int>();
		foreach (Farmer player in Game1.getAllFarmers())
		{
			foreach (string eventSeen in player.eventsSeen)
			{
				eventsSeen[eventSeen] = eventsSeen.GetValueOrDefault(eventSeen) + 1;
			}
			foreach (string mailFlag in player.mailReceived)
			{
				mailFlags[mailFlag] = mailFlags.GetValueOrDefault(mailFlag) + 1;
			}
		}
		foreach (LostItem entry in DataLoader.LostItemsShop(Game1.content))
		{
			int unlocked;
			if (entry.RequireMailReceived != null)
			{
				unlocked = mailFlags.GetValueOrDefault(entry.RequireMailReceived);
			}
			else
			{
				if (entry.RequireEventSeen == null)
				{
					continue;
				}
				unlocked = eventsSeen.GetValueOrDefault(entry.RequireEventSeen);
			}
			int existInWorld = itemsInSave.GetValueOrDefault(entry.ItemId);
			int missing = unlocked - existInWorld;
			if (missing > 0)
			{
				for (int i = 0; i < missing; i++)
				{
					shopItems.Add(ItemRegistry.Create(entry.ItemId));
				}
			}
		}
	}

	public bool localPlayerHasFoundStardrop()
	{
		return Game1.player.hasOrWillReceiveMail("CF_Statue");
	}

	public void statueAnimation(Farmer who)
	{
		if (!hasUnlockedStatue.Value)
		{
			who.reduceActiveItemByOne();
			hasUnlockedStatue.Value = true;
			statueAnimationEvent.Fire();
		}
	}

	private void doStatueAnimation()
	{
		temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(8f, 7f) * 64f, Color.White, 9, flipped: false, 50f));
		temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(9f, 7f) * 64f, Color.Orange, 9, flipped: false, 70f));
		temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(8f, 6f) * 64f, Color.White, 9, flipped: false, 60f));
		temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(9f, 6f) * 64f, Color.OrangeRed, 9, flipped: false, 120f));
		temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(8f, 5f) * 64f, Color.Red, 9));
		temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(9f, 5f) * 64f, Color.White, 9, flipped: false, 170f));
		temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(544f, 464f), Color.Orange, 9, flipped: false, 40f));
		temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(608f, 464f), Color.White, 9, flipped: false, 90f));
		temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(544f, 400f), Color.OrangeRed, 9, flipped: false, 190f));
		temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(608f, 400f), Color.White, 9, flipped: false, 80f));
		temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(544f, 336f), Color.Red, 9, flipped: false, 69f));
		temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(608f, 336f), Color.OrangeRed, 9, flipped: false, 130f));
		temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(480f, 464f), Color.Orange, 9, flipped: false, 40f));
		temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(672f, 368f), Color.White, 9, flipped: false, 90f));
		temporarySprites.Add(new TemporaryAnimatedSprite(10, new Vector2(480f, 464f), Color.Red, 9, flipped: false, 30f));
		temporarySprites.Add(new TemporaryAnimatedSprite(11, new Vector2(672f, 368f), Color.White, 9, flipped: false, 180f));
		localSound("secret1", null, null);
		updateStatueEyes();
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		if (who.IsLocalPlayer)
		{
			int tileIndexAt = getTileIndexAt(tileLocation, "Buildings", "untitled tile sheet");
			if ((uint)(tileIndexAt - 1140) <= 1u)
			{
				if (!hasUnlockedStatue.Value)
				{
					if (who.ActiveObject?.QualifiedItemId == "(O)417")
					{
						statueTimer = 1000;
						who.freezePause = 1000;
						Game1.changeMusicTrack("none");
						playSound("newArtifact", null, null);
					}
					else
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Woods_Statue").Replace('\n', '^'));
					}
				}
				if (hasUnlockedStatue.Value && !localPlayerHasFoundStardrop() && who.freeSpotsInInventory() > 0)
				{
					who.addItemByMenuIfNecessaryElseHoldUp(ItemRegistry.Create("(O)434"));
					Game1.player.mailReceived.Add("CF_Statue");
				}
				return true;
			}
		}
		return base.checkAction(tileLocation, viewport, who);
	}

	public override void DayUpdate(int dayOfMonth)
	{
		base.DayUpdate(dayOfMonth);
		GetLostItemShopMutex().ReleaseLock();
		characters.RemoveWhere((NPC npc) => npc is Monster);
		addedSlimesToday.Value = false;
	}

	public override void cleanupBeforePlayerExit()
	{
		base.cleanupBeforePlayerExit();
		baubles?.Clear();
		weatherDebris?.Clear();
	}

	protected override void resetSharedState()
	{
		if (!addedSlimesToday.Value)
		{
			addedSlimesToday.Value = true;
			Random rand = Utility.CreateRandom(Game1.stats.DaysPlayed, Game1.uniqueIDForThisGame, 12.0);
			for (int tries = 50; tries > 0; tries--)
			{
				Vector2 tile = getRandomTile();
				if (rand.NextDouble() < 0.25 && CanItemBePlacedHere(tile))
				{
					switch (GetSeason())
					{
					case Season.Spring:
						characters.Add(new GreenSlime(tile * 64f, 0));
						break;
					case Season.Summer:
						characters.Add(new GreenSlime(tile * 64f, 0));
						break;
					case Season.Fall:
						characters.Add(new GreenSlime(tile * 64f, rand.Choose(0, 40)));
						break;
					case Season.Winter:
						characters.Add(new GreenSlime(tile * 64f, 40));
						break;
					}
				}
			}
		}
		base.resetSharedState();
	}

	protected void _updateWoodsLighting()
	{
		if (Game1.currentLocation != this)
		{
			return;
		}
		int fade_start_time = Utility.ConvertTimeToMinutes(Game1.getStartingToGetDarkTime(this));
		int fade_end_time = Utility.ConvertTimeToMinutes(Game1.getModeratelyDarkTime(this));
		int light_fade_start_time = Utility.ConvertTimeToMinutes(Game1.getModeratelyDarkTime(this));
		int light_fade_end_time = Utility.ConvertTimeToMinutes(Game1.getTrulyDarkTime(this));
		float num = (float)Utility.ConvertTimeToMinutes(Game1.timeOfDay) + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameMinute;
		float lerp = Utility.Clamp((num - (float)fade_start_time) / (float)(fade_end_time - fade_start_time), 0f, 1f);
		float light_lerp = Utility.Clamp((num - (float)light_fade_start_time) / (float)(light_fade_end_time - light_fade_start_time), 0f, 1f);
		Game1.ambientLight.R = (byte)Utility.Lerp((int)_ambientLightColor.R, (int)Math.Max(_ambientLightColor.R, Game1.isRaining ? Game1.ambientLight.R : Game1.outdoorLight.R), lerp);
		Game1.ambientLight.G = (byte)Utility.Lerp((int)_ambientLightColor.G, (int)Math.Max(_ambientLightColor.G, Game1.isRaining ? Game1.ambientLight.G : Game1.outdoorLight.G), lerp);
		Game1.ambientLight.B = (byte)Utility.Lerp((int)_ambientLightColor.B, (int)Math.Max(_ambientLightColor.B, Game1.isRaining ? Game1.ambientLight.B : Game1.outdoorLight.B), lerp);
		Game1.ambientLight.A = (byte)Utility.Lerp((int)_ambientLightColor.A, (int)Math.Max(_ambientLightColor.A, Game1.isRaining ? Game1.ambientLight.A : Game1.outdoorLight.A), lerp);
		Color light_color = Color.Black;
		light_color.A = (byte)Utility.Lerp(255f, 0f, light_lerp);
		foreach (LightSource light in Game1.currentLightSources.Values)
		{
			if (light.lightContext.Value == LightSource.LightContext.MapLight)
			{
				light.color.Value = light_color;
			}
		}
	}

	public override void MakeMapModifications(bool force = false)
	{
		base.MakeMapModifications(force);
		UpdateLostItemsShopTile();
		updateStatueEyes();
	}

	protected override void resetLocalState()
	{
		_ambientLightColor = new Color(150, 120, 50);
		ignoreOutdoorLighting.Value = false;
		Game1.player.mailReceived.Add("beenToWoods");
		base.resetLocalState();
		_updateWoodsLighting();
		Random r = Utility.CreateDaySaveRandom();
		int numberOfBaubles = 25 + r.Next(0, 75);
		if (!IsRainingHere())
		{
			baubles = new List<Vector2>();
			for (int i = 0; i < numberOfBaubles; i++)
			{
				baubles.Add(new Vector2(Game1.random.Next(0, map.DisplayWidth), Game1.random.Next(0, map.DisplayHeight)));
			}
			Season season = GetSeason();
			if (season != Season.Winter)
			{
				weatherDebris = new List<WeatherDebris>();
				int spacing = 192;
				int leafType = 1;
				if (season == Season.Fall)
				{
					leafType = 2;
				}
				for (int j = 0; j < numberOfBaubles; j++)
				{
					weatherDebris.Add(new WeatherDebris(new Vector2(j * spacing % Game1.graphics.GraphicsDevice.Viewport.Width + Game1.random.Next(spacing), j * spacing / Game1.graphics.GraphicsDevice.Viewport.Width * spacing % Game1.graphics.GraphicsDevice.Viewport.Height + Game1.random.Next(spacing)), leafType, (float)Game1.random.Next(15) / 500f, (float)Game1.random.Next(-10, 0) / 50f, (float)Game1.random.Next(10) / 50f));
				}
			}
		}
		if (Game1.timeOfDay < 1200)
		{
			return;
		}
		Random asdfTime = Utility.CreateDaySaveRandom(15.0);
		int time = Utility.ModifyTime(1920, asdfTime.Next(390));
		int delayBeforeStart = Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, time) * Game1.realMilliSecondsPerGameMinute;
		if (delayBeforeStart <= 0)
		{
			return;
		}
		temporarySprites.Add(new TemporaryAnimatedSprite("Characters\\asldkfjsquaskutanfsldk", new Microsoft.Xna.Framework.Rectangle(0, 0, 32, 48), new Vector2(0f, 0f), flipped: false, 0f, Color.White)
		{
			animationLength = 1,
			totalNumberOfLoops = 1,
			interval = delayBeforeStart,
			endFunction = delegate
			{
				bool flag = true;
				foreach (Farmer current in farmers)
				{
					if (current.position.X < 640f || current.position.Y > 1280f)
					{
						flag = false;
					}
				}
				if (flag)
				{
					foreach (LightSource current2 in sharedLights.Values)
					{
						if (current2.position.X < 1600f && current2.position.Y > 1184f)
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						temporarySprites.Add(new TemporaryAnimatedSprite("Characters\\asldkfjsquaskutanfsldk", new Microsoft.Xna.Framework.Rectangle(0, 0, 32, 48), new Vector2(22f, 24.3f) * 64f, flipped: true, 0f, Color.White)
						{
							animationLength = 8,
							totalNumberOfLoops = 88,
							interval = 90f,
							motion = new Vector2(-7f, 0f),
							scale = 5.5f,
							layerDepth = 0.176f
						});
					}
				}
			}
		});
	}

	/// <summary>Add or remove the crow shop as needed.</summary>
	private void UpdateLostItemsShopTile()
	{
		IInventory lostItemsShopInventory = GetLostItemsShopInventory();
		lostItemsShopInventory.RemoveWhere((Item item) => item == null || item.Stack <= 0);
		if (lostItemsShopInventory.HasAny())
		{
			if (base.Map.GetTileSheet("lostItemsShop") == null)
			{
				Texture2D texture = Game1.content.Load<Texture2D>("Characters\\Crow");
				map.AddTileSheet(new TileSheet("lostItemsShop", map, "Characters\\Crow", new Size(texture.Width / 16, texture.Height / 16), new Size(16)));
			}
			setAnimatedMapTile(12, 4, Enumerable.Range(0, 32).ToArray(), 100L, "Front", "lostItemsShop");
			setAnimatedMapTile(12, 5, Enumerable.Range(32, 32).ToArray(), 100L, "Buildings", "lostItemsShop", "LostItemsShop");
			for (int i = 0; i < 3; i++)
			{
				setTileProperty(11 + i, 6, "Buildings", "Action", "LostItemsShop");
			}
			setMapTile(10, 4, 0, "Buildings", "untitled tile sheet");
			setMapTile(14, 5, 0, "Buildings", "untitled tile sheet");
		}
		else
		{
			removeMapTile(12, 4, "Front");
			removeMapTile(12, 5, "Buildings");
			for (int j = 0; j < 3; j++)
			{
				removeTileProperty(11, 6 + j, "Buildings", "Action");
			}
			removeMapTile(10, 4, "Buildings");
			removeMapTile(14, 5, "Buildings");
		}
	}

	private void updateStatueEyes()
	{
		Layer frontLayer = map.RequireLayer("Front");
		if (hasUnlockedStatue.Value && !localPlayerHasFoundStardrop())
		{
			frontLayer.Tiles[8, 6].TileIndex = 1117;
			frontLayer.Tiles[9, 6].TileIndex = 1118;
		}
		else
		{
			frontLayer.Tiles[8, 6].TileIndex = 1115;
			frontLayer.Tiles[9, 6].TileIndex = 1116;
		}
	}

	public override void updateEvenIfFarmerIsntHere(GameTime time, bool skipWasUpdatedFlush = false)
	{
		base.updateEvenIfFarmerIsntHere(time, skipWasUpdatedFlush);
		statueAnimationEvent.Poll();
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		base.UpdateWhenCurrentLocation(time);
		_updateWoodsLighting();
		if (statueTimer > 0)
		{
			statueTimer -= time.ElapsedGameTime.Milliseconds;
			if (statueTimer <= 0)
			{
				statueAnimation(Game1.player);
			}
		}
		if (baubles != null)
		{
			for (int i = 0; i < baubles.Count; i++)
			{
				Vector2 v = default(Vector2);
				v.X = baubles[i].X - Math.Max(0.4f, Math.Min(1f, (float)i * 0.01f)) - (float)((double)((float)i * 0.01f) * Math.Sin(Math.PI * 2.0 * (double)time.TotalGameTime.Milliseconds / 8000.0));
				v.Y = baubles[i].Y + Math.Max(0.5f, Math.Min(1.2f, (float)i * 0.02f));
				if (v.Y > (float)map.DisplayHeight || v.X < 0f)
				{
					v.X = Game1.random.Next(0, map.DisplayWidth);
					v.Y = -64f;
				}
				baubles[i] = v;
			}
		}
		if (weatherDebris == null)
		{
			return;
		}
		foreach (WeatherDebris weatherDebri in weatherDebris)
		{
			weatherDebri.update();
		}
		Game1.updateDebrisWeatherForMovement(weatherDebris);
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		base.drawAboveAlwaysFrontLayer(b);
		if (baubles != null)
		{
			for (int i = 0; i < baubles.Count; i++)
			{
				b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, baubles[i]), new Microsoft.Xna.Framework.Rectangle(346 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(i * 25)) % 600.0) / 150 * 5, 1971, 5, 5), Color.White, (float)i * ((float)Math.PI / 8f), Vector2.Zero, 4f, SpriteEffects.None, 1f);
			}
		}
		if (weatherDebris == null || currentEvent != null)
		{
			return;
		}
		foreach (WeatherDebris weatherDebri in weatherDebris)
		{
			weatherDebri.draw(b);
		}
	}

	/// <inheritdoc />
	public override bool performAction(string[] action, Farmer who, Location tileLocation)
	{
		if (ArgUtility.Get(action, 0) == "LostItemsShop")
		{
			GetLostItemShopMutex().RequestLock(delegate
			{
				if (Utility.TryOpenShopMenu("LostItems", null, playOpenSound: true) && Game1.activeClickableMenu is ShopMenu shopMenu)
				{
					shopMenu.behaviorBeforeCleanup = OnLostItemsShopClosed;
				}
			});
			return true;
		}
		return base.performAction(action, who, tileLocation);
	}

	/// <summary>Get the items sold in the lost items shop.</summary>
	public static IInventory GetLostItemsShopInventory()
	{
		return Game1.player.team.GetOrCreateGlobalInventory("LostItemsShop");
	}

	/// <summary>Get the mutex which locks access to the lost items shop.</summary>
	public static NetMutex GetLostItemShopMutex()
	{
		return Game1.player.team.GetOrCreateGlobalInventoryMutex("LostItemsShop");
	}

	/// <summary>Handle the player closing the lost items shop.</summary>
	/// <param name="shopMenu">The shop menu.</param>
	private void OnLostItemsShopClosed(IClickableMenu shopMenu)
	{
		GetLostItemShopMutex().ReleaseLock();
	}
}
