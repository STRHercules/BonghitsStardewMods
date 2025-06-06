using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace StardewValley.BellsAndWhistles;

public class MoneyDial
{
	public const int digitHeight = 8;

	public int numDigits;

	public int currentValue;

	public int previousTargetValue;

	public TemporaryAnimatedSpriteList animations = new TemporaryAnimatedSpriteList();

	private int speed;

	private int soundTimer;

	private int moneyMadeAccumulator;

	private int moneyShineTimer;

	private bool playSounds = true;

	public Action<int> onPlaySound;

	/// <summary>Whether to shake the money display in <see cref="F:StardewValley.Game1.dayTimeMoneyBox" /> when the money amount changes.</summary>
	public bool ShouldShakeMainMoneyBox = true;

	public MoneyDial(int numDigits, bool playSound = true)
	{
		this.numDigits = numDigits;
		playSounds = playSound;
		currentValue = 0;
		if (Game1.player != null)
		{
			currentValue = Game1.player.Money;
		}
		onPlaySound = playDefaultSound;
	}

	public void playDefaultSound(int direction)
	{
		if (direction > 0)
		{
			Game1.playSound("moneyDial", null);
		}
	}

	public void draw(SpriteBatch b, Vector2 position, int target)
	{
		if (previousTargetValue != target)
		{
			speed = (target - currentValue) / 100;
			previousTargetValue = target;
			soundTimer = Math.Max(6, 100 / (Math.Abs(speed) + 1));
		}
		if (moneyShineTimer > 0 && currentValue == target)
		{
			moneyShineTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
		}
		if (moneyMadeAccumulator > 0)
		{
			moneyMadeAccumulator -= (Math.Abs(speed / 2) + 1) * ((animations.Count > 0) ? 1 : 100);
			if (moneyMadeAccumulator <= 0)
			{
				moneyShineTimer = numDigits * 60;
			}
		}
		if (ShouldShakeMainMoneyBox && moneyMadeAccumulator > 2000)
		{
			Game1.dayTimeMoneyBox.moneyShakeTimer = 100;
		}
		if (currentValue != target)
		{
			currentValue += speed + ((currentValue < target) ? 1 : (-1));
			if (currentValue < target)
			{
				moneyMadeAccumulator += Math.Abs(speed);
			}
			soundTimer--;
			if (Math.Abs(target - currentValue) <= speed + 1 || (speed != 0 && Math.Sign(target - currentValue) != Math.Sign(speed)))
			{
				currentValue = target;
			}
			if (soundTimer <= 0)
			{
				if (playSounds)
				{
					onPlaySound?.Invoke(Math.Sign(target - currentValue));
				}
				soundTimer = Math.Max(6, 100 / (Math.Abs(speed) + 1));
				if (Game1.random.NextDouble() < 0.4)
				{
					if (target > currentValue)
					{
						animations.Add(TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(Game1.random.Next(10, 12), position + new Vector2(Game1.random.Next(30, 190), Game1.random.Next(-32, 48)), Color.Gold));
					}
					else if (target < currentValue)
					{
						TemporaryAnimatedSprite sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(356, 449, 1, 1), 999999f, 1, 44, position + new Vector2(Game1.random.Next(160), Game1.random.Next(-32, 32)), flicker: false, flipped: false, 1f, 0.01f, Color.White, Game1.random.Next(1, 3) * 4, -0.001f, 0f, 0f);
						sprite.motion = new Vector2((float)Game1.random.Next(-30, 40) / 10f, (float)Game1.random.Next(-30, -5) / 10f);
						sprite.acceleration = new Vector2(0f, 0.25f);
						animations.Add(sprite);
					}
				}
			}
		}
		for (int i = animations.Count - 1; i >= 0; i--)
		{
			if (animations[i].update(Game1.currentGameTime))
			{
				animations.RemoveAt(i);
			}
			else
			{
				animations[i].draw(b, localPosition: true);
			}
		}
		int xPosition = 0;
		int digitStrip = (int)Math.Pow(10.0, numDigits - 1);
		bool significant = false;
		for (int j = 0; j < numDigits; j++)
		{
			int currentDigit = currentValue / digitStrip % 10;
			if (currentDigit > 0 || j == numDigits - 1)
			{
				significant = true;
			}
			if (significant)
			{
				b.Draw(Game1.mouseCursors, position + new Vector2(xPosition, (Game1.activeClickableMenu is ShippingMenu && currentValue >= 1000000) ? ((float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100.53096771240234 + (double)j) * (float)(currentValue / 1000000)) : 0f), new Rectangle(286, 502 - currentDigit * 8, 5, 8), Color.Maroon, 0f, Vector2.Zero, 4f + ((moneyShineTimer / 60 == numDigits - j) ? 0.3f : 0f), SpriteEffects.None, 1f);
			}
			xPosition += 24;
			digitStrip /= 10;
		}
	}
}
