using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Locations;

public class BathHousePool : GameLocation
{
	public const float steamZoom = 4f;

	public const float steamYMotionPerMillisecond = 0.1f;

	private Texture2D steamAnimation;

	private Texture2D swimShadow;

	private Vector2 steamPosition;

	private float steamYOffset;

	private int swimShadowTimer;

	private int swimShadowFrame;

	public BathHousePool()
	{
	}

	public BathHousePool(string mapPath, string name)
		: base(mapPath, name)
	{
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		steamPosition = new Vector2(-Game1.viewport.X, -Game1.viewport.Y);
		steamAnimation = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\steamAnimation");
		swimShadow = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\swimShadow");
	}

	public override void cleanupBeforePlayerExit()
	{
		base.cleanupBeforePlayerExit();
		if (Game1.player.swimming.Value)
		{
			Game1.player.swimming.Value = false;
		}
		if (Game1.locationRequest != null && !Game1.locationRequest.Name.Contains("BathHouse"))
		{
			Game1.player.bathingClothes.Value = false;
		}
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		if (currentEvent != null)
		{
			foreach (NPC n in currentEvent.actors)
			{
				if (n.swimming.Value)
				{
					b.Draw(swimShadow, Game1.GlobalToLocal(Game1.viewport, n.Position + new Vector2(0f, n.Sprite.SpriteHeight / 3 * 4 + 4)), new Rectangle(swimShadowFrame * 16, 0, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
				}
			}
			return;
		}
		foreach (NPC n2 in characters)
		{
			if (n2.swimming.Value)
			{
				b.Draw(swimShadow, Game1.GlobalToLocal(Game1.viewport, n2.Position + new Vector2(0f, n2.Sprite.SpriteHeight / 3 * 4 + 4)), new Rectangle(swimShadowFrame * 16, 0, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
			}
		}
		foreach (Farmer f in farmers)
		{
			if (f.swimming.Value)
			{
				b.Draw(swimShadow, Game1.GlobalToLocal(Game1.viewport, f.Position + new Vector2(0f, f.Sprite.SpriteHeight / 4 * 4)), new Rectangle(swimShadowFrame * 16, 0, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
			}
		}
	}

	public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
	{
		base.drawAboveAlwaysFrontLayer(b);
		for (float x = steamPosition.X; x < (float)Game1.graphics.GraphicsDevice.Viewport.Width + 256f; x += 256f)
		{
			for (float y = steamPosition.Y + steamYOffset; y < (float)(Game1.graphics.GraphicsDevice.Viewport.Height + 128); y += 256f)
			{
				b.Draw(steamAnimation, new Vector2(x, y), new Rectangle(0, 0, 64, 64), Color.White * 0.8f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
			}
		}
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		base.UpdateWhenCurrentLocation(time);
		steamYOffset -= (float)time.ElapsedGameTime.Milliseconds * 0.1f;
		steamYOffset %= -256f;
		steamPosition -= Game1.getMostRecentViewportMotion();
		swimShadowTimer -= time.ElapsedGameTime.Milliseconds;
		if (swimShadowTimer <= 0)
		{
			swimShadowTimer = 70;
			swimShadowFrame++;
			swimShadowFrame %= 10;
		}
	}
}
