using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Pathfinding;

namespace StardewValley.Characters;

public class Junimo : NPC
{
	private readonly NetFloat alpha = new NetFloat(1f);

	private readonly NetFloat alphaChange = new NetFloat();

	public readonly NetInt whichArea = new NetInt();

	public readonly NetBool friendly = new NetBool();

	public readonly NetBool holdingStar = new NetBool();

	public readonly NetBool holdingBundle = new NetBool();

	public readonly NetBool temporaryJunimo = new NetBool();

	public readonly NetBool stayPut = new NetBool();

	private readonly NetVector2 motion = new NetVector2(Vector2.Zero);

	private new readonly NetRectangle nextPosition = new NetRectangle();

	private readonly NetColor color = new NetColor();

	private readonly NetColor bundleColor = new NetColor();

	private readonly NetBool sayingGoodbye = new NetBool();

	private readonly NetEvent0 setReturnToJunimoHutToFetchStarControllerEvent = new NetEvent0();

	private readonly NetEvent0 setBringBundleBackToHutControllerEvent = new NetEvent0();

	private readonly NetEvent0 setJunimoReachedHutToFetchStarControllerEvent = new NetEvent0();

	private readonly NetEvent0 starDoneSpinningEvent = new NetEvent0();

	private readonly NetEvent0 returnToJunimoHutToFetchFinalStarEvent = new NetEvent0();

	private int farmerCloseCheckTimer = 100;

	private static int soundTimer;

	/// <inheritdoc />
	[XmlIgnore]
	public override bool IsVillager => false;

	public Junimo()
	{
		forceUpdateTimer = 9999;
	}

	public Junimo(Vector2 position, int whichArea, bool temporary = false)
		: base(new AnimatedSprite("Characters\\Junimo", 0, 16, 16), position, 2, "Junimo")
	{
		this.whichArea.Value = whichArea;
		try
		{
			friendly.Value = Game1.RequireLocation<CommunityCenter>("CommunityCenter").areasComplete[whichArea];
		}
		catch (Exception)
		{
			friendly.Value = true;
		}
		if (whichArea == 6)
		{
			friendly.Value = false;
		}
		temporaryJunimo.Value = temporary;
		nextPosition.Value = GetBoundingBox();
		base.Breather = false;
		base.speed = 3;
		forceUpdateTimer = 9999;
		collidesWithOtherCharacters.Value = true;
		farmerPassesThrough = true;
		base.Scale = 0.75f;
		if (temporaryJunimo.Value)
		{
			if (Game1.random.NextDouble() < 0.01)
			{
				switch (Game1.random.Next(8))
				{
				case 0:
					color.Value = Color.Red;
					break;
				case 1:
					color.Value = Color.Goldenrod;
					break;
				case 2:
					color.Value = Color.Yellow;
					break;
				case 3:
					color.Value = Color.Lime;
					break;
				case 4:
					color.Value = new Color(0, 255, 180);
					break;
				case 5:
					color.Value = new Color(0, 100, 255);
					break;
				case 6:
					color.Value = Color.MediumPurple;
					break;
				case 7:
					color.Value = Color.Salmon;
					break;
				}
				if (Game1.random.NextDouble() < 0.01)
				{
					color.Value = Color.White;
				}
			}
			else
			{
				switch (Game1.random.Next(8))
				{
				case 0:
					color.Value = Color.LimeGreen;
					break;
				case 1:
					color.Value = Color.Orange;
					break;
				case 2:
					color.Value = Color.LightGreen;
					break;
				case 3:
					color.Value = Color.Tan;
					break;
				case 4:
					color.Value = Color.GreenYellow;
					break;
				case 5:
					color.Value = Color.LawnGreen;
					break;
				case 6:
					color.Value = Color.PaleGreen;
					break;
				case 7:
					color.Value = Color.Turquoise;
					break;
				}
			}
		}
		else
		{
			switch (whichArea)
			{
			case -1:
			case 0:
				color.Value = Color.LimeGreen;
				break;
			case 1:
				color.Value = Color.Orange;
				break;
			case 2:
				color.Value = Color.Turquoise;
				break;
			case 3:
				color.Value = Color.Tan;
				break;
			case 4:
				color.Value = Color.Gold;
				break;
			case 5:
				color.Value = Color.BlanchedAlmond;
				break;
			case 6:
				color.Value = new Color(160, 20, 220);
				break;
			}
		}
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(alpha, "alpha").AddField(alphaChange, "alphaChange").AddField(whichArea, "whichArea")
			.AddField(friendly, "friendly")
			.AddField(holdingStar, "holdingStar")
			.AddField(holdingBundle, "holdingBundle")
			.AddField(temporaryJunimo, "temporaryJunimo")
			.AddField(stayPut, "stayPut")
			.AddField(motion, "motion")
			.AddField(nextPosition, "nextPosition")
			.AddField(color, "color")
			.AddField(bundleColor, "bundleColor")
			.AddField(sayingGoodbye, "sayingGoodbye")
			.AddField(setReturnToJunimoHutToFetchStarControllerEvent, "setReturnToJunimoHutToFetchStarControllerEvent")
			.AddField(setBringBundleBackToHutControllerEvent, "setBringBundleBackToHutControllerEvent")
			.AddField(setJunimoReachedHutToFetchStarControllerEvent, "setJunimoReachedHutToFetchStarControllerEvent")
			.AddField(starDoneSpinningEvent, "starDoneSpinningEvent")
			.AddField(returnToJunimoHutToFetchFinalStarEvent, "returnToJunimoHutToFetchFinalStarEvent");
		setReturnToJunimoHutToFetchStarControllerEvent.onEvent += setReturnToJunimoHutToFetchStarController;
		setBringBundleBackToHutControllerEvent.onEvent += setBringBundleBackToHutController;
		setJunimoReachedHutToFetchStarControllerEvent.onEvent += setJunimoReachedHutToFetchStarController;
		starDoneSpinningEvent.onEvent += performStartDoneSpinning;
		returnToJunimoHutToFetchFinalStarEvent.onEvent += returnToJunimoHutToFetchFinalStar;
		position.Field.AxisAlignedMovement = false;
	}

	public override bool canPassThroughActionTiles()
	{
		return false;
	}

	public override bool shouldCollideWithBuildingLayer(GameLocation location)
	{
		return true;
	}

	public override bool canTalk()
	{
		return false;
	}

	public override void ChooseAppearance(LocalizedContentManager content = null)
	{
	}

	public void fadeAway()
	{
		collidesWithOtherCharacters.Value = false;
		alphaChange.Value = (stayPut.Value ? (-0.005f) : (-0.015f));
	}

	public void setAlpha(float a)
	{
		alpha.Value = a;
	}

	public void fadeBack()
	{
		alpha.Value = 0f;
		alphaChange.Value = 0.02f;
		base.IsInvisible = false;
	}

	public void setMoving(int xSpeed, int ySpeed)
	{
		motion.X = xSpeed;
		motion.Y = ySpeed;
	}

	public void setMoving(Vector2 motion)
	{
		this.motion.Value = motion;
	}

	public override void Halt()
	{
		base.Halt();
		motion.Value = Vector2.Zero;
	}

	public void returnToJunimoHut(GameLocation location)
	{
		base.currentLocation = location;
		jump();
		collidesWithOtherCharacters.Value = false;
		controller = new PathFindController(this, location, new Point(25, 10), 0, junimoReachedHut);
		location.playSound("junimoMeep1", null, null);
	}

	public void stayStill()
	{
		stayPut.Value = true;
		motion.Value = Vector2.Zero;
	}

	public void allowToMoveAgain()
	{
		stayPut.Value = false;
	}

	private void returnToJunimoHutToFetchFinalStar()
	{
		if (base.currentLocation == Game1.currentLocation)
		{
			Game1.globalFadeToBlack(finalCutscene, 0.005f);
			Game1.freezeControls = true;
			Game1.flashAlpha = 1f;
		}
	}

	public void returnToJunimoHutToFetchStar(GameLocation location)
	{
		base.currentLocation = location;
		friendly.Value = true;
		CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
		if (communityCenter.areAllAreasComplete())
		{
			returnToJunimoHutToFetchFinalStarEvent.Fire();
			collidesWithOtherCharacters.Value = false;
			farmerPassesThrough = false;
			stayStill();
			faceDirection(0);
			Game1.player.mailReceived.Add("ccIsComplete");
			if (Game1.currentLocation.Equals(communityCenter))
			{
				communityCenter.addStarToPlaque();
			}
		}
		else
		{
			DelayedAction.textAboveHeadAfterDelay(Game1.random.NextBool() ? Game1.content.LoadString("Strings\\Characters:JunimoTextAboveHead1") : Game1.content.LoadString("Strings\\Characters:JunimoTextAboveHead2"), this, Game1.random.Next(3000, 6000));
			setReturnToJunimoHutToFetchStarControllerEvent.Fire();
			location.playSound("junimoMeep1", null, null);
			collidesWithOtherCharacters.Value = false;
			farmerPassesThrough = false;
			holdingBundle.Value = true;
			base.speed = 3;
		}
	}

	private void setReturnToJunimoHutToFetchStarController()
	{
		if (Game1.IsMasterGame)
		{
			controller = new PathFindController(this, base.currentLocation, new Point(25, 10), 0, junimoReachedHutToFetchStar);
		}
	}

	private void finalCutscene()
	{
		collidesWithOtherCharacters.Value = false;
		farmerPassesThrough = false;
		Game1.RequireLocation<CommunityCenter>("CommunityCenter").prepareForJunimoDance();
		Game1.player.Position = new Vector2(29f, 11f) * 64f;
		Game1.player.completelyStopAnimatingOrDoingAction();
		Game1.player.faceDirection(3);
		Point playerPixel = Game1.player.StandingPixel;
		Game1.UpdateViewPort(overrideFreeze: true, playerPixel);
		Game1.viewport.X = playerPixel.X - Game1.viewport.Width / 2;
		Game1.viewport.Y = playerPixel.Y - Game1.viewport.Height / 2;
		Game1.viewportTarget = Vector2.Zero;
		Game1.viewportCenter = playerPixel;
		Game1.moveViewportTo(new Vector2(32.5f, 6f) * 64f, 2f, 999999);
		Game1.globalFadeToClear(goodbyeDance, 0.005f);
		Game1.pauseTime = 1000f;
		Game1.freezeControls = true;
	}

	public void bringBundleBackToHut(Color bundleColor, GameLocation location)
	{
		base.currentLocation = location;
		if (holdingBundle.Value)
		{
			return;
		}
		base.Position = Utility.getRandomAdjacentOpenTile(Game1.player.Tile, location) * 64f;
		int iter = 0;
		while (location.isCollidingPosition(GetBoundingBox(), Game1.viewport, this) && iter < 5)
		{
			base.Position = Utility.getRandomAdjacentOpenTile(Game1.player.Tile, location) * 64f;
			iter++;
		}
		if (iter < 5)
		{
			if (Game1.random.NextDouble() < 0.25)
			{
				DelayedAction.textAboveHeadAfterDelay(Game1.random.NextBool() ? Game1.content.LoadString("Strings\\Characters:JunimoThankYou1") : Game1.content.LoadString("Strings\\Characters:JunimoThankYou2"), this, Game1.random.Next(3000, 6000));
			}
			fadeBack();
			this.bundleColor.Value = bundleColor;
			setBringBundleBackToHutControllerEvent.Fire();
			collidesWithOtherCharacters.Value = false;
			farmerPassesThrough = false;
			holdingBundle.Value = true;
			base.speed = 1;
		}
	}

	private void setBringBundleBackToHutController()
	{
		if (Game1.IsMasterGame)
		{
			controller = new PathFindController(this, base.currentLocation, new Point(25, 10), 0, junimoReachedHutToReturnBundle);
		}
	}

	private void junimoReachedHutToReturnBundle(Character c, GameLocation l)
	{
		base.currentLocation = l;
		holdingBundle.Value = false;
		collidesWithOtherCharacters.Value = true;
		farmerPassesThrough = true;
		l.playSound("Ship", null, null);
	}

	private void junimoReachedHutToFetchStar(Character c, GameLocation l)
	{
		base.currentLocation = l;
		holdingStar.Value = true;
		holdingBundle.Value = false;
		base.speed = 1;
		collidesWithOtherCharacters.Value = false;
		farmerPassesThrough = false;
		setJunimoReachedHutToFetchStarControllerEvent.Fire();
		l.playSound("dwop", null, null);
		farmerPassesThrough = false;
	}

	private void setJunimoReachedHutToFetchStarController()
	{
		if (Game1.IsMasterGame)
		{
			controller = new PathFindController(this, base.currentLocation, new Point(32, 9), 2, placeStar);
		}
	}

	private void placeStar(Character c, GameLocation l)
	{
		base.currentLocation = l;
		collidesWithOtherCharacters.Value = false;
		farmerPassesThrough = true;
		holdingStar.Value = false;
		l.playSound("tinyWhip", null, null);
		friendly.Value = true;
		base.speed = 3;
		Game1.multiplayer.broadcastSprites(l, new TemporaryAnimatedSprite(Sprite.textureName.Value, new Rectangle(0, 109, 16, 19), 40f, 8, 10, base.Position + new Vector2(0f, -64f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f * scale.Value, 0f, 0f, 0f)
		{
			endFunction = starDoneSpinning,
			motion = new Vector2(0.22f, -2f),
			acceleration = new Vector2(0f, 0.01f),
			id = 777
		});
	}

	public void sayGoodbye()
	{
		sayingGoodbye.Value = true;
		farmerPassesThrough = true;
	}

	private void goodbyeDance()
	{
		Game1.player.faceDirection(3);
		Game1.RequireLocation<CommunityCenter>("CommunityCenter").junimoGoodbyeDance();
	}

	private void starDoneSpinning(int extraInfo)
	{
		starDoneSpinningEvent.Fire();
		(base.currentLocation as CommunityCenter).addStarToPlaque();
	}

	private void performStartDoneSpinning()
	{
		if (Game1.currentLocation is CommunityCenter)
		{
			Game1.playSound("yoba", null);
			Game1.flashAlpha = 1f;
			Game1.playSound("yoba", null);
		}
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		if (textAboveHeadTimer > 0 && textAboveHead != null)
		{
			Point standingPixel = base.StandingPixel;
			Vector2 local = Game1.GlobalToLocal(new Vector2(standingPixel.X, (float)standingPixel.Y - 128f + (float)yJumpOffset));
			if (textAboveHeadStyle == 0)
			{
				local += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
			}
			SpriteText.drawStringWithScrollCenteredAt(b, textAboveHead, (int)local.X, (int)local.Y, "", textAboveHeadAlpha, textAboveHeadColor, 1, (float)(base.TilePoint.Y * 64) / 10000f + 0.001f + (float)base.TilePoint.X / 10000f, !sayingGoodbye.Value);
		}
	}

	public void junimoReachedHut(Character c, GameLocation l)
	{
		base.currentLocation = l;
		fadeAway();
		controller = null;
		motion.X = 0f;
		motion.Y = -1f;
	}

	protected override void updateSlaveAnimation(GameTime time)
	{
		if (sayingGoodbye.Value || temporaryJunimo.Value)
		{
			return;
		}
		if (holdingStar.Value || holdingBundle.Value)
		{
			Sprite.Animate(time, 44, 4, 200f);
		}
		else if (position.IsInterpolating())
		{
			switch (FacingDirection)
			{
			case 1:
				flip = false;
				Sprite.Animate(time, 16, 8, 50f);
				break;
			case 3:
				flip = true;
				Sprite.Animate(time, 16, 8, 50f);
				break;
			case 0:
				Sprite.Animate(time, 32, 8, 50f);
				break;
			default:
				Sprite.Animate(time, 0, 8, 50f);
				break;
			}
		}
		else
		{
			Sprite.Animate(time, 8, 4, 100f);
		}
	}

	public override void update(GameTime time, GameLocation location)
	{
		base.currentLocation = location;
		setReturnToJunimoHutToFetchStarControllerEvent.Poll();
		setBringBundleBackToHutControllerEvent.Poll();
		setJunimoReachedHutToFetchStarControllerEvent.Poll();
		starDoneSpinningEvent.Poll();
		returnToJunimoHutToFetchFinalStarEvent.Poll();
		base.update(time, location);
		forceUpdateTimer = 99999;
		if (sayingGoodbye.Value)
		{
			flip = false;
			if (whichArea.Value % 2 == 0)
			{
				Sprite.Animate(time, 16, 8, 50f);
			}
			else
			{
				Sprite.Animate(time, 28, 4, 80f);
			}
			if (!base.IsInvisible && Game1.random.NextDouble() < 0.009999999776482582 && yJumpOffset == 0)
			{
				jump();
				if (Game1.random.NextDouble() < 0.15 && Game1.player.Tile.X == 29f && Game1.player.Tile.Y == 11f)
				{
					showTextAboveHead(Game1.random.NextBool() ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Junimo.cs.6625") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Junimo.cs.6626"), null);
				}
			}
			alpha.Value += alphaChange.Value;
			if (alpha.Value > 1f)
			{
				alpha.Value = 1f;
				alphaChange.Value = 0f;
			}
			if (alpha.Value < 0f)
			{
				alpha.Value = 0f;
				base.IsInvisible = true;
				base.HideShadow = true;
			}
		}
		else if (temporaryJunimo.Value)
		{
			Sprite.Animate(time, 12, 4, 100f);
			if (Game1.random.NextDouble() < 0.001)
			{
				jumpWithoutSound();
				location.localSound("junimoMeep1", null, null);
			}
		}
		else
		{
			if (EventActor)
			{
				return;
			}
			alpha.Value += alphaChange.Value;
			if (alpha.Value > 1f)
			{
				alpha.Value = 1f;
				base.HideShadow = false;
			}
			else if (alpha.Value < 0f)
			{
				alpha.Value = 0f;
				base.IsInvisible = true;
				base.HideShadow = true;
			}
			soundTimer--;
			farmerCloseCheckTimer -= time.ElapsedGameTime.Milliseconds;
			if (sayingGoodbye.Value || temporaryJunimo.Value || !Game1.IsMasterGame)
			{
				return;
			}
			if (!base.IsInvisible && farmerCloseCheckTimer <= 0 && controller == null && alpha.Value >= 1f && !stayPut.Value && Game1.IsMasterGame)
			{
				farmerCloseCheckTimer = 100;
				if (holdingStar.Value)
				{
					setJunimoReachedHutToFetchStarController();
				}
				else
				{
					Farmer f = Utility.isThereAFarmerWithinDistance(base.Tile, 5, base.currentLocation);
					if (f != null)
					{
						if (friendly.Value && Vector2.Distance(base.Position, f.Position) > (float)(base.speed * 4))
						{
							if (motion.Equals(Vector2.Zero) && soundTimer <= 0)
							{
								jump();
								location.localSound("junimoMeep1", null, null);
								soundTimer = 400;
							}
							if (Game1.random.NextDouble() < 0.007)
							{
								jumpWithoutSound(Game1.random.Next(6, 9));
							}
							setMoving(Utility.getVelocityTowardPlayer(new Point((int)base.Position.X, (int)base.Position.Y), base.speed, f));
						}
						else if (!friendly.Value)
						{
							fadeAway();
							Vector2 v = Utility.getAwayFromPlayerTrajectory(GetBoundingBox(), f);
							v.Normalize();
							v.Y *= -1f;
							setMoving(v * base.speed);
						}
						else if (alpha.Value >= 1f)
						{
							motion.Value = Vector2.Zero;
						}
					}
					else if (alpha.Value >= 1f)
					{
						motion.Value = Vector2.Zero;
					}
				}
			}
			if (!base.IsInvisible && controller == null)
			{
				nextPosition.Value = GetBoundingBox();
				nextPosition.X += (int)motion.X;
				bool sparkle = false;
				if (!location.isCollidingPosition(nextPosition.Value, Game1.viewport, this))
				{
					position.X += (int)motion.X;
					sparkle = true;
				}
				nextPosition.X -= (int)motion.X;
				nextPosition.Y += (int)motion.Y;
				if (!location.isCollidingPosition(nextPosition.Value, Game1.viewport, this))
				{
					position.Y += (int)motion.Y;
					sparkle = true;
				}
				if (!motion.Equals(Vector2.Zero) && sparkle && Game1.random.NextDouble() < 0.005)
				{
					location.temporarySprites.Add(new TemporaryAnimatedSprite(Game1.random.Choose(10, 11), base.Position, color.Value)
					{
						motion = motion.Value / 4f,
						alphaFade = 0.01f,
						layerDepth = 0.8f,
						scale = 0.75f,
						alpha = 0.75f
					});
				}
			}
			if (controller != null || !motion.Equals(Vector2.Zero))
			{
				if (holdingStar.Value || holdingBundle.Value)
				{
					Sprite.Animate(time, 44, 4, 200f);
				}
				else if (moveRight || (Math.Abs(motion.X) > Math.Abs(motion.Y) && motion.X > 0f))
				{
					flip = false;
					Sprite.Animate(time, 16, 8, 50f);
				}
				else if (moveLeft || (Math.Abs(motion.X) > Math.Abs(motion.Y) && motion.X < 0f))
				{
					Sprite.Animate(time, 16, 8, 50f);
					flip = true;
				}
				else if (moveUp || (Math.Abs(motion.Y) > Math.Abs(motion.X) && motion.Y < 0f))
				{
					Sprite.Animate(time, 32, 8, 50f);
				}
				else
				{
					Sprite.Animate(time, 0, 8, 50f);
				}
			}
			else
			{
				Sprite.Animate(time, 8, 4, 100f);
			}
		}
	}

	public override void draw(SpriteBatch b, float alpha = 1f)
	{
		if (!base.IsInvisible)
		{
			Sprite.UpdateSourceRect();
			b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(Sprite.SpriteWidth * 4 / 2, (float)Sprite.SpriteHeight * 3f / 4f * 4f / (float)Math.Pow(Sprite.SpriteHeight / 16, 2.0) + (float)yJumpOffset - 8f) + ((shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), Sprite.SourceRect, color.Value * this.alpha.Value, rotation, new Vector2(Sprite.SpriteWidth * 4 / 2, (float)(Sprite.SpriteHeight * 4) * 3f / 4f) / 4f, Math.Max(0.2f, scale.Value) * 4f, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.991f : ((float)base.StandingPixel.Y / 10000f)));
			if (holdingStar.Value)
			{
				b.Draw(Sprite.Texture, Game1.GlobalToLocal(Game1.viewport, base.Position + new Vector2(8f, -64f * scale.Value + 4f + (float)yJumpOffset)), new Rectangle(0, 109, 16, 19), Color.White * this.alpha.Value, 0f, Vector2.Zero, 4f * scale.Value, SpriteEffects.None, base.Position.Y / 10000f + 0.0001f);
			}
			else if (holdingBundle.Value)
			{
				b.Draw(Sprite.Texture, Game1.GlobalToLocal(Game1.viewport, base.Position + new Vector2(8f, -64f * scale.Value + 20f + (float)yJumpOffset)), new Rectangle(0, 96, 16, 13), bundleColor.Value * this.alpha.Value, 0f, Vector2.Zero, 4f * scale.Value, SpriteEffects.None, base.Position.Y / 10000f + 0.0001f);
			}
		}
	}

	/// <inheritdoc />
	public override void DrawShadow(SpriteBatch b)
	{
		b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, base.Position + new Vector2((float)(Sprite.SpriteWidth * 4) / 2f, 44f)), Game1.shadowTexture.Bounds, color.Value * alpha.Value, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), (4f + (float)yJumpOffset / 40f) * scale.Value, SpriteEffects.None, Math.Max(0f, (float)base.StandingPixel.Y / 10000f) - 1E-06f);
	}
}
