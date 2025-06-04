using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace StardewValley;

public class DebugTimings
{
	private static readonly Vector2 DrawPos = Vector2.One * 12f;

	private readonly Stopwatch StopwatchDraw = new Stopwatch();

	private readonly Stopwatch StopwatchUpdate = new Stopwatch();

	private double LastTimingDraw;

	private double LastTimingUpdate;

	private float DrawTextWidth = -1f;

	private bool Active;

	public bool Toggle()
	{
		if ((!(Game1.game1?.IsMainInstance)) ?? true)
		{
			return false;
		}
		Active = !Active;
		return Active;
	}

	public void StartDrawTimer()
	{
		if (Active && (Game1.game1?.IsMainInstance ?? false))
		{
			StopwatchDraw.Restart();
		}
	}

	public void StopDrawTimer()
	{
		if (Active && (Game1.game1?.IsMainInstance ?? false))
		{
			StopwatchDraw.Stop();
			LastTimingDraw = StopwatchDraw.Elapsed.TotalMilliseconds;
		}
	}

	public void StartUpdateTimer()
	{
		if (Active && (Game1.game1?.IsMainInstance ?? false))
		{
			StopwatchUpdate.Restart();
		}
	}

	public void StopUpdateTimer()
	{
		if (Active && (Game1.game1?.IsMainInstance ?? false))
		{
			StopwatchUpdate.Stop();
			LastTimingUpdate = StopwatchUpdate.Elapsed.TotalMilliseconds;
		}
	}

	public void Draw()
	{
		if (!Active)
		{
			return;
		}
		bool? flag = Game1.game1?.IsMainInstance;
		if (flag.HasValue && flag == true && Game1.spriteBatch != null && Game1.dialogueFont != null)
		{
			if (DrawTextWidth <= 0f)
			{
				DrawTextWidth = Game1.dialogueFont.MeasureString($"Draw time: {0:00.00} ms  ").X;
			}
			Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, 64), Color.Black * 0.5f);
			Game1.spriteBatch.DrawString(Game1.dialogueFont, $"Draw time: {LastTimingDraw:00.00} ms  ", DrawPos, Color.White);
			Game1.spriteBatch.DrawString(Game1.dialogueFont, $"Update time: {LastTimingUpdate:00.00} ms", new Vector2(DrawPos.X + DrawTextWidth, DrawPos.Y), Color.White);
		}
	}
}
