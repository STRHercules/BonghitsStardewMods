using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;

namespace StardewValley.Locations;

public class Club : GameLocation
{
	public static int timesPlayedCalicoJack;

	public static int timesPlayedSlots;

	private string coinBuffer;

	public Club()
	{
	}

	public Club(string mapPath, string name)
		: base(mapPath, name)
	{
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		lightGlows.Clear();
		coinBuffer = ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.zh) ? "\u3000\u3000" : "  ");
	}

	/// <inheritdoc />
	public override void checkForMusic(GameTime time)
	{
		if (Game1.random.NextDouble() < 0.002)
		{
			localSound("boop", null, null);
		}
	}

	public override void drawOverlays(SpriteBatch b)
	{
		if (Game1.currentMinigame == null)
		{
			SpriteText.drawStringWithScrollBackground(b, coinBuffer + Game1.player.clubCoins, 64, 16, "", 1f, null);
			Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(68f, 20f), new Rectangle(211, 373, 9, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
		}
		base.drawOverlays(b);
	}
}
