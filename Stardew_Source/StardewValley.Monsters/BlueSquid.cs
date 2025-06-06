using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.Projectiles;

namespace StardewValley.Monsters;

public class BlueSquid : Monster
{
	public float nextFire;

	public int squidYOffset;

	public float canMoveTimer;

	public NetFloat projectileIntroTimer = new NetFloat();

	public NetFloat projectileOutroTimer = new NetFloat();

	public NetBool nearFarmer = new NetBool();

	public NetFloat lastRotation = new NetFloat();

	[XmlIgnore]
	public bool justThrust;

	public BlueSquid()
	{
	}

	public BlueSquid(Vector2 position)
		: base("Blue Squid", position)
	{
		Sprite.SpriteHeight = 24;
		Sprite.SpriteWidth = 24;
		base.IsWalkingTowardPlayer = true;
		reloadSprite();
		Sprite.UpdateSourceRect();
		base.HideShadow = true;
		slipperiness.Value = Game1.random.Next(6, 9);
		canMoveTimer = Game1.random.Next(500);
		isHardModeMonster.Value = true;
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(projectileIntroTimer, "projectileIntroTimer").AddField(projectileOutroTimer, "projectileOutroTimer").AddField(lastRotation, "lastRotation")
			.AddField(nearFarmer, "nearFarmer");
		lastRotation.Interpolated(interpolate: false, wait: false);
		projectileIntroTimer.Interpolated(interpolate: false, wait: false);
		projectileOutroTimer.Interpolated(interpolate: false, wait: false);
	}

	/// <inheritdoc />
	public override void reloadSprite(bool onlyAppearance = false)
	{
		Sprite = new AnimatedSprite("Characters\\Monsters\\Blue Squid", 0, 24, 24);
	}

	public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
	{
		int actualDamage = Math.Max(1, damage - resilience.Value);
		if (Game1.random.NextDouble() < missChance.Value - missChance.Value * addedPrecision)
		{
			actualDamage = -1;
		}
		else
		{
			base.Health -= actualDamage;
			projectileOutroTimer.Value = 0f;
			projectileIntroTimer.Value = 0f;
			shakeTimer = 250;
			setTrajectory(xTrajectory, yTrajectory);
			lastRotation.Value = (float)Math.Atan2(0f - yVelocity, xVelocity) + (float)Math.PI / 2f;
			DelayedAction.playSoundAfterDelay("squid_hit", 80, base.currentLocation, null);
			base.currentLocation.playSound("slimeHit", null, null);
			if (base.Health <= 0)
			{
				deathAnimation();
			}
		}
		return actualDamage;
	}

	protected override void sharedDeathAnimation()
	{
		base.currentLocation.localSound("slimedead", null, null);
		if (Sprite.Texture.Height > Sprite.getHeight() * 4)
		{
			Point standingPixel = base.StandingPixel;
			Game1.createRadialDebris(base.currentLocation, Sprite.textureName.Value, new Rectangle(0, 48, 16, 16), 8, standingPixel.X, standingPixel.Y, 6, base.TilePoint.Y, Color.White, 4f * scale.Value);
		}
	}

	protected override void localDeathAnimation()
	{
		Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(44, base.Position, Color.HotPink * 0.86f, 10)
		{
			interval = 70f,
			holdLastFrame = true,
			alphaFade = 0.01f
		});
		Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(44, base.Position + new Vector2(-16f, 0f), Color.HotPink * 0.86f, 10)
		{
			interval = 70f,
			delayBeforeAnimationStart = 0,
			holdLastFrame = true,
			alphaFade = 0.01f
		});
		Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(44, base.Position + new Vector2(0f, -16f), Color.HotPink * 0.86f, 10)
		{
			interval = 70f,
			delayBeforeAnimationStart = 100,
			holdLastFrame = true,
			alphaFade = 0.01f
		});
		Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite(44, base.Position + new Vector2(16f, 0f), Color.HotPink * 0.86f, 10)
		{
			interval = 70f,
			delayBeforeAnimationStart = 200,
			holdLastFrame = true,
			alphaFade = 0.01f
		});
	}

	public override Rectangle GetBoundingBox()
	{
		if (Sprite == null)
		{
			return Rectangle.Empty;
		}
		Vector2 position = base.Position;
		int width = GetSpriteWidthForPositioning() * 4 * 3 / 4;
		return new Rectangle((int)position.X, (int)position.Y + 16, width, 64);
	}

	public override void drawAboveAllLayers(SpriteBatch b)
	{
		int standingY = base.StandingPixel.Y;
		b.Draw(Game1.shadowTexture, getLocalPosition(Game1.viewport) + new Vector2(32f, 96f), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), Math.Min(4f, 4f + (float)squidYOffset / 20f), SpriteEffects.None, (float)(standingY - 32) / 10000f);
		b.Draw(Sprite.Texture, getLocalPosition(Game1.viewport) + new Vector2(32f, 21 + squidYOffset) + new Vector2((shakeTimer > 0) ? Game1.random.Next(-2, 3) : 0, (shakeTimer > 0) ? Game1.random.Next(-2, 3) : 0), Sprite.SourceRect, Color.White, lastRotation.Value, new Vector2(12f, 12f), Math.Max(0.2f, scale.Value) * 4f, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Math.Max(0f, drawOnTop ? 0.991f : ((float)standingY / 10000f)));
	}

	protected override void updateAnimation(GameTime time)
	{
		if (Sprite.CurrentFrame != 2)
		{
			justThrust = false;
		}
		if (projectileIntroTimer.Value > 0f)
		{
			shakeTimer = 10;
			Sprite.CurrentFrame = 6;
			squidYOffset--;
			if (squidYOffset < 0)
			{
				squidYOffset = 0;
			}
		}
		else if (projectileOutroTimer.Value > 0f)
		{
			Sprite.CurrentFrame = 5;
			squidYOffset += 2;
		}
		else
		{
			squidYOffset = (int)(Math.Sin((double)((float)time.TotalGameTime.TotalMilliseconds / 2000f) * Math.PI * 2.0) * 30.0);
			Sprite.currentFrame = Math.Abs(squidYOffset - 24) / 12;
			if (squidYOffset < 0)
			{
				Sprite.CurrentFrame = 2;
			}
		}
		Sprite.UpdateSourceRect();
	}

	public override void noMovementProgressNearPlayerBehavior()
	{
	}

	public override void behaviorAtGameTick(GameTime time)
	{
		base.behaviorAtGameTick(time);
		nearFarmer.Value = withinPlayerThreshold(10) || base.focusedOnFarmers;
		if (projectileIntroTimer.Value <= 0f && projectileOutroTimer.Value <= 0f)
		{
			if (Math.Abs(xVelocity) <= 1f && Math.Abs(yVelocity) <= 1f && nearFarmer.Value)
			{
				Vector2 trajFinder = Utility.getVelocityTowardPoint(findPlayer().position.Value, position.Value, Game1.random.Next(25, 50));
				trajFinder.X *= -1f;
				if (canMoveTimer > 0f)
				{
					canMoveTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
				}
				if (!justThrust && Sprite.CurrentFrame == 2 && canMoveTimer <= 0f)
				{
					justThrust = true;
					Vector2 traj = Utility.getVelocityTowardPoint(findPlayer().position.Value, position.Value + new Vector2(Game1.random.Next(-64, 64)), Game1.random.Next(25, 50));
					traj.X *= -1f;
					setTrajectory(traj);
					lastRotation.Value = (float)Math.Atan2(0f - yVelocity, xVelocity) + (float)Math.PI / 2f;
					base.currentLocation.playSound("squid_move", null, null);
					canMoveTimer = 500f;
				}
			}
			else if (!nearFarmer.Value)
			{
				lastRotation.Value = 0f;
			}
		}
		if ((Math.Abs(xVelocity) >= 10f || Math.Abs(yVelocity) >= 10f) && Game1.random.NextDouble() < 0.25)
		{
			Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Rectangle(Game1.random.Choose(135, 140), 234, 5, 5), base.Position + new Vector2(32f, 32 + Game1.random.Next(-8, 8)), flipped: false, 0.01f, Color.White)
			{
				interval = 9999f,
				holdLastFrame = true,
				alphaFade = 0.01f,
				motion = new Vector2(0f, -1f),
				xPeriodic = true,
				xPeriodicLoopTime = Game1.random.Next(800, 1200),
				xPeriodicRange = Game1.random.Next(8, 20),
				scale = 4f,
				drawAboveAlwaysFront = true
			});
		}
		if (projectileIntroTimer.Value > 0f)
		{
			projectileIntroTimer.Value -= (float)time.ElapsedGameTime.TotalMilliseconds;
			shakeTimer = 10;
			if (Game1.random.NextDouble() < 0.25)
			{
				Game1.multiplayer.broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Rectangle(Game1.random.Choose(135, 140), 234, 5, 5), base.Position + new Vector2(21 + Game1.random.Next(-21, 21), squidYOffset / 2 + 32 + Game1.random.Next(-32, 32)), flipped: false, 0.01f, Color.White)
				{
					interval = 9999f,
					holdLastFrame = true,
					alphaFade = 0.01f,
					motion = new Vector2(0f, -1f),
					xPeriodic = true,
					xPeriodicLoopTime = Game1.random.Next(800, 1200),
					xPeriodicRange = Game1.random.Next(8, 20),
					scale = 4f,
					drawAboveAlwaysFront = true
				});
			}
			if (projectileIntroTimer.Value < 0f)
			{
				projectileOutroTimer.Value = 500f;
				base.IsWalkingTowardPlayer = false;
				Halt();
				Point standingPixel = base.StandingPixel;
				Vector2 trajectory = Utility.getVelocityTowardPlayer(standingPixel, 8f, base.Player);
				DebuffingProjectile projectile = new DebuffingProjectile("27", 8, 3, 4, 0f, trajectory.X, trajectory.Y, Utility.PointToVector2(standingPixel) - new Vector2(32f, -squidYOffset), base.currentLocation, this);
				projectile.height.Value = 48f;
				base.currentLocation.projectiles.Add(projectile);
				base.currentLocation.playSound("debuffSpell", null, null);
				nextFire = Game1.random.Next(1200, 3500);
			}
		}
		else if (projectileOutroTimer.Value > 0f)
		{
			projectileOutroTimer.Value -= (float)time.ElapsedGameTime.TotalMilliseconds;
		}
		nextFire = Math.Max(0f, nextFire - (float)time.ElapsedGameTime.Milliseconds);
		if (withinPlayerThreshold(6) && nextFire == 0f && projectileIntroTimer.Value <= 0f && Math.Abs(xVelocity) < 1f && Math.Abs(yVelocity) < 1f && Game1.random.NextDouble() < 0.003 && canMoveTimer <= 0f && base.currentLocation.hasTileAt(base.TilePoint.X, base.TilePoint.Y, "Back") && !base.currentLocation.hasTileAt(base.TilePoint.X, base.TilePoint.Y, "Buildings") && !base.currentLocation.hasTileAt(base.TilePoint.X, base.TilePoint.Y, "Front"))
		{
			projectileIntroTimer.Value = 1000f;
			lastRotation.Value = 0f;
			base.currentLocation.playSound("squid_bubble", null, null);
		}
	}
}
