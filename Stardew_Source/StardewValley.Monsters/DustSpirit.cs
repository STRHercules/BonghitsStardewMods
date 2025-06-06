using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;

namespace StardewValley.Monsters;

public class DustSpirit : Monster
{
	[XmlIgnore]
	public bool seenFarmer;

	[XmlIgnore]
	public bool runningAwayFromFarmer;

	[XmlIgnore]
	public bool chargingFarmer;

	public byte voice;

	[XmlIgnore]
	public ICue meep;

	public DustSpirit()
	{
	}

	public DustSpirit(Vector2 position)
		: base("Dust Spirit", position)
	{
		base.IsWalkingTowardPlayer = false;
		Sprite.interval = 45f;
		base.Scale = (float)Game1.random.Next(75, 101) / 100f;
		voice = (byte)Game1.random.Next(1, 24);
		base.HideShadow = true;
	}

	public DustSpirit(Vector2 position, bool chargingTowardFarmer)
		: base("Dust Spirit", position)
	{
		base.IsWalkingTowardPlayer = false;
		if (chargingTowardFarmer)
		{
			chargingFarmer = true;
			seenFarmer = true;
		}
		Sprite.interval = 45f;
		base.Scale = (float)Game1.random.Next(75, 101) / 100f;
		base.HideShadow = true;
	}

	public override void draw(SpriteBatch b)
	{
		if (!base.IsInvisible && Utility.isOnScreen(base.Position, 128))
		{
			int standingY = base.StandingPixel.Y;
			b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), 64 + yJumpOffset), Sprite.SourceRect, Color.White, rotation, new Vector2(8f, 16f), new Vector2(scale.Value + (float)Math.Max(-0.1, (double)(yJumpOffset + 32) / 128.0), scale.Value - Math.Max(-0.1f, (float)yJumpOffset / 256f)) * 4f, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.991f : ((float)standingY / 10000f)));
			if (isGlowing)
			{
				b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(32f, 64 + yJumpOffset), Sprite.SourceRect, glowingColor * glowingTransparency, rotation, new Vector2(8f, 16f), Math.Max(0.2f, scale.Value) * 4f, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.99f : ((float)standingY / 10000f + 0.001f)));
			}
			b.Draw(Game1.shadowTexture, getLocalPosition(Game1.viewport) + new Vector2(32f, 80f), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f + (float)yJumpOffset / 64f, SpriteEffects.None, (float)(standingY - 1) / 10000f);
		}
	}

	protected override void sharedDeathAnimation()
	{
	}

	protected override void localDeathAnimation()
	{
		base.currentLocation.localSound("dustMeep", null, null);
		base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(44, base.Position, new Color(50, 50, 80), 10));
		base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(44, base.Position + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32)), new Color(50, 50, 80), 10)
		{
			delayBeforeAnimationStart = 150,
			scale = 0.5f
		});
		base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(44, base.Position + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32)), new Color(50, 50, 80), 10)
		{
			delayBeforeAnimationStart = 300,
			scale = 0.5f
		});
		base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(44, base.Position + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32)), new Color(50, 50, 80), 10)
		{
			delayBeforeAnimationStart = 450,
			scale = 0.5f
		});
	}

	public override void shedChunks(int number, float scale)
	{
		Point standingPixel = base.StandingPixel;
		Game1.createRadialDebris(base.currentLocation, Sprite.textureName.Value, new Rectangle(0, 16, 16, 16), 8, standingPixel.X, standingPixel.Y, number, base.TilePoint.Y, Color.White, (base.Health <= 0) ? 4f : 2f);
	}

	public void offScreenBehavior(Character c, GameLocation l)
	{
	}

	public virtual bool CaughtInWeb()
	{
		if (base.currentLocation != null && base.currentLocation.terrainFeatures.TryGetValue(base.Tile, out var terrainFeature) && terrainFeature is Grass grass)
		{
			return grass.grassType.Value == 6;
		}
		return false;
	}

	protected override void updateAnimation(GameTime time)
	{
		if (yJumpOffset == 0)
		{
			if (isHardModeMonster.Value && CaughtInWeb())
			{
				Sprite.Animate(time, 5, 3, 200f);
				return;
			}
			jumpWithoutSound();
			yJumpVelocity = (float)Game1.random.Next(50, 70) / 10f;
			if (Game1.random.NextDouble() < 0.1 && (meep == null || !meep.IsPlaying) && Utility.isOnScreen(base.Position, 64) && Game1.currentLocation == base.currentLocation)
			{
				Game1.playSound("dustMeep", voice * 100 + Game1.random.Next(-100, 100), out meep);
			}
		}
		Sprite.AnimateDown(time);
		resetAnimationSpeed();
	}

	public override void behaviorAtGameTick(GameTime time)
	{
		base.behaviorAtGameTick(time);
		if (yJumpOffset == 0)
		{
			if (Game1.random.NextDouble() < 0.01)
			{
				Vector2 standingPixel = getStandingPosition();
				Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 128, 64, 64), 40f, 4, 0, standingPixel + new Vector2(-21f, 0f), flicker: false, flipped: false)
				{
					layerDepth = (standingPixel.Y - 10f) / 10000f
				});
				foreach (Vector2 v in Utility.getAdjacentTileLocations(base.Tile))
				{
					if (base.currentLocation.objects.TryGetValue(v, out var obj) && (obj.IsBreakableStone() || obj.IsTwig()))
					{
						base.currentLocation.destroyObject(v, null);
					}
				}
				yJumpVelocity *= 2f;
			}
			if (!chargingFarmer)
			{
				xVelocity = (float)Game1.random.Next(-20, 21) / 5f;
			}
		}
		if (chargingFarmer)
		{
			base.Slipperiness = 10;
			Vector2 v2 = Utility.getAwayFromPlayerTrajectory(GetBoundingBox(), base.Player);
			xVelocity += (0f - v2.X) / 150f + ((Game1.random.NextDouble() < 0.01) ? ((float)Game1.random.Next(-50, 50) / 10f) : 0f);
			if (Math.Abs(xVelocity) > 5f)
			{
				xVelocity = Math.Sign(xVelocity) * 5;
			}
			yVelocity += (0f - v2.Y) / 150f + ((Game1.random.NextDouble() < 0.01) ? ((float)Game1.random.Next(-50, 50) / 10f) : 0f);
			if (Math.Abs(yVelocity) > 5f)
			{
				yVelocity = Math.Sign(yVelocity) * 5;
			}
			if (Game1.random.NextDouble() < 0.0001)
			{
				controller = new PathFindController(this, base.currentLocation, base.Player.TilePoint, Game1.random.Next(4), null, 300);
				chargingFarmer = false;
			}
			if (isHardModeMonster.Value && CaughtInWeb())
			{
				xVelocity = 0f;
				yVelocity = 0f;
				if (shakeTimer <= 0 && Game1.random.NextDouble() < 0.05)
				{
					shakeTimer = 200;
				}
			}
		}
		else if (!seenFarmer && Utility.doesPointHaveLineOfSightInMine(base.currentLocation, getStandingPosition() / 64f, base.Player.getStandingPosition() / 64f, 8))
		{
			seenFarmer = true;
		}
		else if (seenFarmer && controller == null && !runningAwayFromFarmer)
		{
			addedSpeed = 2f;
			controller = new PathFindController(this, base.currentLocation, Utility.isOffScreenEndFunction, -1, offScreenBehavior, 350, Point.Zero);
			runningAwayFromFarmer = true;
		}
		else if (controller == null && runningAwayFromFarmer)
		{
			chargingFarmer = true;
		}
	}
}
