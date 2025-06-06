using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Extensions;
using xTile.Dimensions;

namespace StardewValley.Locations;

public class Desert : GameLocation
{
	public const int busDefaultXTile = 17;

	public const int busDefaultYTile = 24;

	private TemporaryAnimatedSprite busDoor;

	private Vector2 busPosition;

	private Vector2 busMotion;

	public bool drivingOff;

	public bool drivingBack;

	public bool leaving;

	private int chimneyTimer = 500;

	private Microsoft.Xna.Framework.Rectangle desertMerchantBounds = new Microsoft.Xna.Framework.Rectangle(2112, 1280, 836, 280);

	public static bool warpedToDesert;

	private Microsoft.Xna.Framework.Rectangle busSource = new Microsoft.Xna.Framework.Rectangle(288, 1247, 128, 64);

	private Microsoft.Xna.Framework.Rectangle pamSource = new Microsoft.Xna.Framework.Rectangle(384, 1311, 15, 19);

	private Microsoft.Xna.Framework.Rectangle transparentWindowSource = new Microsoft.Xna.Framework.Rectangle(0, 0, 21, 41);

	private Vector2 pamOffset = new Vector2(0f, 29f);

	public Desert()
	{
	}

	public Desert(string mapPath, string name)
		: base(mapPath, name)
	{
	}

	public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
	{
		if (map.GetLayer("Buildings")?.Tiles[tileLocation] != null)
		{
			return base.checkAction(tileLocation, viewport, who);
		}
		if ((tileLocation.X == 41 || tileLocation.X == 42) && tileLocation.Y == 24)
		{
			OnDesertTrader();
			return true;
		}
		if (tileLocation.X >= 34 && tileLocation.X <= 38 && tileLocation.Y == 24)
		{
			OnCamel();
			return true;
		}
		return base.checkAction(tileLocation, viewport, who);
	}

	public virtual void OnDesertTrader()
	{
		Utility.TryOpenShopMenu("DesertTrade", this, null, null);
	}

	public virtual void OnCamel()
	{
		Game1.playSound("camel", null);
		ShowCamelAnimation();
		Game1.player.faceDirection(0);
		Game1.haltAfterCheck = false;
	}

	public virtual void ShowCamelAnimation()
	{
		if (getTemporarySpriteByID(999) == null)
		{
			temporarySprites.Add(new TemporaryAnimatedSprite
			{
				texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
				sourceRect = new Microsoft.Xna.Framework.Rectangle(208, 591, 65, 49),
				sourceRectStartingPos = new Vector2(208f, 591f),
				animationLength = 1,
				totalNumberOfLoops = 1,
				interval = 300f,
				scale = 4f,
				position = new Vector2(536f, 340f) * 4f,
				layerDepth = 0.1332f,
				id = 999
			});
		}
	}

	public override string checkForBuriedItem(int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
	{
		if (who.secretNotesSeen.Contains(18) && xLocation == 40 && yLocation == 55 && who.mailReceived.Add("SecretNote18_done"))
		{
			Game1.createObjectDebris("(O)127", xLocation, yLocation, who.UniqueMultiplayerID, this);
			return "";
		}
		return base.checkForBuriedItem(xLocation, yLocation, explosion, detectOnly, who);
	}

	private void playerReachedBusDoor(Character c, GameLocation l)
	{
		Game1.viewportFreeze = true;
		Game1.player.position.X = -10000f;
		Game1.freezeControls = true;
		Game1.player.CanMove = false;
		busDriveOff();
		playSound("stoneStep", null, null);
	}

	public override bool answerDialogue(Response answer)
	{
		if (lastQuestionKey != null && afterQuestion == null && ArgUtility.SplitBySpaceAndGet(lastQuestionKey, 0) + "_" + answer.responseKey == "DesertBus_Yes")
		{
			playerReachedBusDoor(Game1.player, this);
			return true;
		}
		return base.answerDialogue(answer);
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		leaving = false;
		Game1.ambientLight = Color.White;
		GameLocation previousLocation = Game1.getLocationFromName(Game1.player.previousLocationName);
		bool showingBusArrival = false;
		if (previousLocation == null || previousLocation.GetLocationContextId() != GetLocationContextId())
		{
			warpedToDesert = true;
			if (Game1.player.previousLocationName == "BusStop" && Game1.player.TilePoint.X == 16 && Game1.player.TilePoint.Y == 24)
			{
				warpedToDesert = false;
				showingBusArrival = true;
				Game1.changeMusicTrack("silence");
				busPosition = new Vector2(17f, 24f) * 64f;
				busDoor = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(368, 1311, 16, 38), busPosition + new Vector2(16f, 26f) * 4f, flipped: false, 0f, Color.White)
				{
					interval = 999999f,
					animationLength = 1,
					holdLastFrame = true,
					layerDepth = (busPosition.Y + 192f) / 10000f + 1E-05f,
					scale = 4f
				};
				Game1.displayFarmer = false;
				busDriveBack();
			}
		}
		if (!showingBusArrival)
		{
			drivingOff = false;
			drivingBack = false;
			busMotion = Vector2.Zero;
			busPosition = new Vector2(17f, 24f) * 64f;
			busDoor = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(288, 1311, 16, 38), busPosition + new Vector2(16f, 26f) * 4f, flipped: false, 0f, Color.White)
			{
				interval = 999999f,
				animationLength = 6,
				holdLastFrame = true,
				layerDepth = (busPosition.Y + 192f) / 10000f + 1E-05f,
				scale = 4f
			};
		}
		if (GetType() == typeof(DesertFestival))
		{
			temporarySprites.Add(new TemporaryAnimatedSprite
			{
				texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
				sourceRect = new Microsoft.Xna.Framework.Rectangle(208, 524, 65, 49),
				sourceRectStartingPos = new Vector2(208f, 524f),
				animationLength = 1,
				totalNumberOfLoops = 9999,
				interval = 99999f,
				scale = 4f,
				position = new Vector2(536f, 340f) * 4f,
				layerDepth = 0.1324f,
				id = 996
			});
		}
		else
		{
			temporarySprites.Add(new TemporaryAnimatedSprite
			{
				texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
				sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 513, 208, 101),
				sourceRectStartingPos = new Vector2(0f, 513f),
				animationLength = 1,
				totalNumberOfLoops = 9999,
				interval = 99999f,
				scale = 4f,
				position = new Vector2(528f, 298f) * 4f,
				layerDepth = 0.1324f,
				id = 996
			});
		}
		if (IsTravelingDesertMerchantHere())
		{
			temporarySprites.Add(new TemporaryAnimatedSprite
			{
				texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
				sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 614, 20, 26),
				sourceRectStartingPos = new Vector2(0f, 614f),
				animationLength = 1,
				totalNumberOfLoops = 999,
				interval = 99999f,
				scale = 4f,
				position = new Vector2(663f, 354f) * 4f,
				layerDepth = 0.1328f,
				id = 995
			});
		}
		if (Game1.timeOfDay >= Game1.getModeratelyDarkTime(this))
		{
			lightMerchantLamps();
		}
	}

	private bool IsTravelingDesertMerchantHere()
	{
		if (Game1.IsWinter && Game1.dayOfMonth >= 15)
		{
			return Game1.dayOfMonth > 17;
		}
		return true;
	}

	public override bool isCollidingPosition(Microsoft.Xna.Framework.Rectangle position, xTile.Dimensions.Rectangle viewport, bool isFarmer, int damagesFarmer, bool glider, Character character)
	{
		if (position.Intersects(desertMerchantBounds))
		{
			return true;
		}
		return base.isCollidingPosition(position, viewport, isFarmer, damagesFarmer, glider, character);
	}

	public override void performTenMinuteUpdate(int timeOfDay)
	{
		base.performTenMinuteUpdate(timeOfDay);
		if (Game1.currentLocation != this)
		{
			return;
		}
		if (IsTravelingDesertMerchantHere())
		{
			if (Game1.random.NextDouble() < 0.33)
			{
				temporarySprites.Add(new TemporaryAnimatedSprite
				{
					texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
					sourceRect = new Microsoft.Xna.Framework.Rectangle(40, 614, 20, 26),
					sourceRectStartingPos = new Vector2(40f, 614f),
					animationLength = 6,
					totalNumberOfLoops = 1,
					interval = 100f,
					scale = 4f,
					position = new Vector2(663f, 354f) * 4f,
					layerDepth = 0.1336f,
					id = 997,
					pingPong = true
				});
			}
			else
			{
				temporarySprites.Add(new TemporaryAnimatedSprite
				{
					texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
					sourceRect = new Microsoft.Xna.Framework.Rectangle(20, 614, 20, 26),
					sourceRectStartingPos = new Vector2(20f, 614f),
					animationLength = 1,
					totalNumberOfLoops = 1,
					interval = Game1.random.Next(100, 800),
					scale = 4f,
					position = new Vector2(663f, 354f) * 4f,
					layerDepth = 0.1332f,
					id = 998
				});
			}
		}
		ShowCamelAnimation();
		if (timeOfDay == Game1.getModeratelyDarkTime(this) && Game1.currentLocation == this)
		{
			lightMerchantLamps();
		}
	}

	public void lightMerchantLamps()
	{
		if (getTemporarySpriteByID(1000) == null)
		{
			temporarySprites.Add(new TemporaryAnimatedSprite
			{
				texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
				sourceRect = new Microsoft.Xna.Framework.Rectangle(181, 633, 7, 6),
				sourceRectStartingPos = new Vector2(181f, 633f),
				animationLength = 1,
				totalNumberOfLoops = 9999,
				interval = 99999f,
				scale = 4f,
				position = new Vector2(545f, 309f) * 4f,
				layerDepth = 0.134f,
				id = 1000,
				lightId = "Desert_MerchantLamp_1",
				lightRadius = 1f,
				lightcolor = Color.Black
			});
			temporarySprites.Add(new TemporaryAnimatedSprite
			{
				texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
				sourceRect = new Microsoft.Xna.Framework.Rectangle(181, 633, 7, 6),
				sourceRectStartingPos = new Vector2(181f, 633f),
				animationLength = 1,
				totalNumberOfLoops = 9999,
				interval = 99999f,
				scale = 4f,
				position = new Vector2(644f, 360f) * 4f,
				layerDepth = 0.134f,
				id = 1000,
				lightId = "Desert_MerchantLamp_2",
				lightRadius = 1f,
				lightcolor = Color.Black
			});
			temporarySprites.Add(new TemporaryAnimatedSprite
			{
				texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\temporary_sprites_1"),
				sourceRect = new Microsoft.Xna.Framework.Rectangle(181, 633, 7, 6),
				sourceRectStartingPos = new Vector2(181f, 633f),
				animationLength = 1,
				totalNumberOfLoops = 9999,
				interval = 99999f,
				scale = 4f,
				position = new Vector2(717f, 309f) * 4f,
				layerDepth = 0.134f,
				id = 1000,
				lightId = "Desert_MerchantLamp_3",
				lightRadius = 1f,
				lightcolor = Color.Black
			});
		}
	}

	public override void cleanupBeforePlayerExit()
	{
		base.cleanupBeforePlayerExit();
		if (farmers.Count <= 1)
		{
			busDoor = null;
		}
	}

	public void busDriveOff()
	{
		busDoor = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(288, 1311, 16, 38), busPosition + new Vector2(16f, 26f) * 4f, flipped: false, 0f, Color.White)
		{
			interval = 999999f,
			animationLength = 6,
			holdLastFrame = true,
			layerDepth = (busPosition.Y + 192f) / 10000f + 1E-05f,
			scale = 4f
		};
		busDoor.timer = 0f;
		busDoor.interval = 70f;
		busDoor.endFunction = busStartMovingOff;
		localSound("trashcanlid", null, null);
		drivingBack = false;
		busDoor.paused = false;
	}

	public void busDriveBack()
	{
		busPosition.X = map.RequireLayer("Back").DisplayWidth;
		busDoor.Position = busPosition + new Vector2(16f, 26f) * 4f;
		drivingBack = true;
		drivingOff = false;
		localSound("busDriveOff", null, null);
		busMotion = new Vector2(-6f, 0f);
	}

	private void busStartMovingOff(int extraInfo)
	{
		Game1.globalFadeToBlack(delegate
		{
			Game1.globalFadeToClear();
			localSound("batFlap", null, null);
			drivingOff = true;
			localSound("busDriveOff", null, null);
			Game1.changeMusicTrack("silence");
		});
	}

	/// <inheritdoc />
	public override bool IgnoreTouchActions()
	{
		if (!base.IgnoreTouchActions() && !drivingBack)
		{
			return drivingOff;
		}
		return true;
	}

	/// <inheritdoc />
	public override void performTouchAction(string[] action, Vector2 playerStandingPosition)
	{
		if (!IgnoreTouchActions())
		{
			if (ArgUtility.Get(action, 0) == "DesertBus")
			{
				Response[] returnOptions = new Response[2]
				{
					new Response("Yes", Game1.content.LoadString("Strings\\Locations:Desert_Return_Yes")),
					new Response("Not", Game1.content.LoadString("Strings\\Locations:Desert_Return_No"))
				};
				createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Desert_Return_Question"), returnOptions, "DesertBus");
			}
			else
			{
				base.performTouchAction(action, playerStandingPosition);
			}
		}
	}

	private void doorOpenAfterReturn(int extraInfo)
	{
		localSound("batFlap", null, null);
		busDoor = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(288, 1311, 16, 38), busPosition + new Vector2(16f, 26f) * 4f, flipped: false, 0f, Color.White)
		{
			interval = 999999f,
			animationLength = 6,
			holdLastFrame = true,
			layerDepth = (busPosition.Y + 192f) / 10000f + 1E-05f,
			scale = 4f
		};
		Game1.player.Position = new Vector2(18f, 27f) * 64f;
		lastTouchActionLocation = Game1.player.Tile;
		Game1.displayFarmer = true;
		Game1.player.forceCanMove();
		Game1.player.faceDirection(2);
		Game1.changeMusicTrack("none", track_interruptable: true);
		GameLocation.HandleMusicChange(null, this);
	}

	private void busLeftToValley()
	{
		Game1.viewport.Y = -100000;
		Game1.viewportFreeze = true;
		Game1.warpFarmer("BusStop", 22, 10, flip: true);
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		base.UpdateWhenCurrentLocation(time);
		if (drivingBack || drivingOff)
		{
			if (Game1.player.currentLocation == this)
			{
				Game1.player.CanMove = false;
			}
			else
			{
				drivingBack = false;
				drivingOff = false;
			}
		}
		if (drivingOff && !leaving)
		{
			busMotion.X -= 0.075f;
			if (busPosition.X + 512f < 0f)
			{
				leaving = true;
				Game1.globalFadeToBlack(busLeftToValley, 0.01f);
			}
		}
		if (drivingBack && busMotion != Vector2.Zero)
		{
			Game1.player.Position = busDoor.position;
			Game1.player.freezePause = 100;
			if (busPosition.X - 1088f < 256f)
			{
				busMotion.X = Math.Min(-1f, busMotion.X * 0.98f);
			}
			if (Math.Abs(busPosition.X - 1088f) <= Math.Abs(busMotion.X * 1.5f))
			{
				busPosition.X = 1088f;
				busMotion = Vector2.Zero;
				Game1.globalFadeToBlack(delegate
				{
					drivingBack = false;
					busDoor.Position = busPosition + new Vector2(16f, 26f) * 4f;
					busDoor.pingPong = true;
					busDoor.interval = 70f;
					busDoor.currentParentTileIndex = 5;
					busDoor.endFunction = doorOpenAfterReturn;
					localSound("trashcanlid", null, null);
					Game1.globalFadeToClear();
				});
			}
		}
		if (!busMotion.Equals(Vector2.Zero))
		{
			busPosition += busMotion;
			if (busDoor != null)
			{
				busDoor.Position += busMotion;
			}
		}
		busDoor?.update(time);
		if (IsTravelingDesertMerchantHere())
		{
			chimneyTimer -= time.ElapsedGameTime.Milliseconds;
			if (chimneyTimer <= 0)
			{
				chimneyTimer = 500;
				Vector2 smokeSpot = new Vector2(670f, 308f) * 4f;
				temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), smokeSpot, flipped: false, 0.002f, new Color(255, 222, 198))
				{
					alpha = 0.05f,
					alphaFade = -0.01f,
					alphaFadeFade = -8E-05f,
					motion = new Vector2(0f, -0.5f),
					acceleration = new Vector2(0.002f, 0f),
					interval = 99999f,
					layerDepth = 1f,
					scale = 3f,
					scaleChange = 0.01f,
					rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f,
					drawAboveAlwaysFront = (this is DesertFestival)
				});
			}
		}
	}

	public override void DayUpdate(int dayOfMonth)
	{
		base.DayUpdate(dayOfMonth);
		removeObjectsAndSpawned(33, 20, 13, 6);
	}

	public override bool isTilePlaceable(Vector2 v, bool itemIsPassable = false)
	{
		if (v.X >= 33f && v.X < 46f && v.Y >= 20f && v.Y < 25f)
		{
			return false;
		}
		return base.isTilePlaceable(v, itemIsPassable);
	}

	public override bool shouldHideCharacters()
	{
		if (!drivingOff)
		{
			return drivingBack;
		}
		return true;
	}

	public override void draw(SpriteBatch spriteBatch)
	{
		base.draw(spriteBatch);
		spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2((int)busPosition.X, (int)busPosition.Y)), busSource, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (busPosition.Y + 192f) / 10000f);
		busDoor?.draw(spriteBatch);
		if (drivingOff || drivingBack)
		{
			if (Game1.netWorldState.Value.canDriveYourselfToday.Value || (drivingOff && warpedToDesert))
			{
				Game1.player.faceDirection(3);
				Game1.player.blinkTimer = -1000;
				Game1.player.FarmerRenderer.draw(spriteBatch, new FarmerSprite.AnimationFrame(117, 99999, 0, secondaryArm: false, flip: true), 117, new Microsoft.Xna.Framework.Rectangle(48, 608, 16, 32), Game1.GlobalToLocal(new Vector2((int)(busPosition.X + 4f), (int)(busPosition.Y - 8f)) + pamOffset * 4f), Vector2.Zero, (busPosition.Y + 192f + 4f) / 10000f, Color.White, 0f, 1f, Game1.player);
				spriteBatch.Draw(Game1.mouseCursors2, Game1.GlobalToLocal(Game1.viewport, new Vector2((int)busPosition.X, (int)busPosition.Y - 40) + pamOffset * 4f), transparentWindowSource, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (busPosition.Y + 192f + 8f) / 10000f);
			}
			else
			{
				spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2((int)busPosition.X, (int)busPosition.Y) + pamOffset * 4f), pamSource, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (busPosition.Y + 192f + 4f) / 10000f);
			}
		}
	}
}
