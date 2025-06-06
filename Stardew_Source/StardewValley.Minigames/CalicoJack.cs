using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Menus;

namespace StardewValley.Minigames;

public class CalicoJack : IMinigame
{
	public const int cardState_flipped = -1;

	public const int cardState_up = 0;

	public const int cardState_transitioning = 400;

	public const int bet = 100;

	public const int cardWidth = 96;

	public const int dealTime = 1000;

	public const int playingTo = 21;

	public const int passNumber = 18;

	public const int dealerTurnDelay = 1000;

	public List<int[]> playerCards;

	public List<int[]> dealerCards;

	private Random r;

	private int currentBet;

	private int startTimer;

	private int dealerTurnTimer = -1;

	private int bustTimer;

	private ClickableComponent hit;

	private ClickableComponent stand;

	private ClickableComponent doubleOrNothing;

	private ClickableComponent playAgain;

	private ClickableComponent quit;

	private ClickableComponent currentlySnappedComponent;

	private bool showingResultsScreen;

	private bool playerWon;

	private bool highStakes;

	private string endMessage = "";

	private string endTitle = "";

	private string coinBuffer;

	public CalicoJack(int toBet = -1, bool highStakes = false)
	{
		coinBuffer = ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru) ? "     " : ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.zh) ? "\u3000\u3000" : "  "));
		this.highStakes = highStakes;
		startTimer = 1000;
		playerCards = new List<int[]>();
		dealerCards = new List<int[]>();
		if (toBet == -1)
		{
			currentBet = (highStakes ? 1000 : 100);
		}
		else
		{
			currentBet = toBet;
		}
		Club.timesPlayedCalicoJack++;
		r = Utility.CreateRandom(Club.timesPlayedCalicoJack, Game1.stats.DaysPlayed, Game1.uniqueIDForThisGame);
		hit = new ClickableComponent(new Rectangle((int)((float)Game1.graphics.GraphicsDevice.Viewport.Width / Game1.options.zoomLevel - 128f - (float)SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11924"))), Game1.graphics.GraphicsDevice.Viewport.Height / 2 - 64, SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11924") + "  "), 64), "", " " + Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11924") + " ");
		stand = new ClickableComponent(new Rectangle((int)((float)Game1.graphics.GraphicsDevice.Viewport.Width / Game1.options.zoomLevel - 128f - (float)SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11927"))), Game1.graphics.GraphicsDevice.Viewport.Height / 2 + 32, SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11927") + "  "), 64), "", " " + Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11927") + " ");
		doubleOrNothing = new ClickableComponent(new Rectangle((int)((float)(Game1.graphics.GraphicsDevice.Viewport.Width / 2) / Game1.options.zoomLevel) - SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11930")) / 2, (int)((float)(Game1.graphics.GraphicsDevice.Viewport.Height / 2) / Game1.options.zoomLevel), SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11930")) + 64, 64), "", Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11930"));
		playAgain = new ClickableComponent(new Rectangle((int)((float)(Game1.graphics.GraphicsDevice.Viewport.Width / 2) / Game1.options.zoomLevel) - SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11933")) / 2, (int)((float)(Game1.graphics.GraphicsDevice.Viewport.Height / 2) / Game1.options.zoomLevel) + 64 + 16, SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11933")) + 64, 64), "", Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11933"));
		quit = new ClickableComponent(new Rectangle((int)((float)(Game1.graphics.GraphicsDevice.Viewport.Width / 2) / Game1.options.zoomLevel) - SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11936")) / 2, (int)((float)(Game1.graphics.GraphicsDevice.Viewport.Height / 2) / Game1.options.zoomLevel) + 64 + 96, SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11936")) + 64, 64), "", Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11936"));
		RepositionButtons();
		if (Game1.options.SnappyMenus)
		{
			currentlySnappedComponent = hit;
			currentlySnappedComponent.snapMouseCursorToCenter();
		}
	}

	public void RepositionButtons()
	{
		hit.bounds = new Rectangle((int)((float)Game1.game1.localMultiplayerWindow.Width / Game1.options.zoomLevel - 128f - (float)SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11924"))), Game1.viewport.Height / 2 - 64, SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11924") + "  "), 64);
		stand.bounds = new Rectangle((int)((float)Game1.game1.localMultiplayerWindow.Width / Game1.options.zoomLevel - 128f - (float)SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11927"))), Game1.viewport.Height / 2 + 32, SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11927") + "  "), 64);
		doubleOrNothing.bounds = new Rectangle((int)((float)(Game1.game1.localMultiplayerWindow.Width / 2) / Game1.options.zoomLevel) - (SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11930")) + 64) / 2, (int)((float)(Game1.game1.localMultiplayerWindow.Height / 2) / Game1.options.zoomLevel), SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11930")) + 64, 64);
		playAgain.bounds = new Rectangle((int)((float)(Game1.game1.localMultiplayerWindow.Width / 2) / Game1.options.zoomLevel) - (SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11933")) + 64) / 2, (int)((float)(Game1.game1.localMultiplayerWindow.Height / 2) / Game1.options.zoomLevel) + 64 + 16, SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11933")) + 64, 64);
		quit.bounds = new Rectangle((int)((float)(Game1.game1.localMultiplayerWindow.Width / 2) / Game1.options.zoomLevel) - (SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11936")) + 64) / 2, (int)((float)(Game1.game1.localMultiplayerWindow.Height / 2) / Game1.options.zoomLevel) + 64 + 96, SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11936")) + 64, 64);
	}

	public bool overrideFreeMouseMovement()
	{
		return Game1.options.SnappyMenus;
	}

	public bool playButtonsActive()
	{
		if (startTimer <= 0 && dealerTurnTimer < 0)
		{
			return !showingResultsScreen;
		}
		return false;
	}

	public bool tick(GameTime time)
	{
		for (int i = 0; i < playerCards.Count; i++)
		{
			if (playerCards[i][1] > 0)
			{
				playerCards[i][1] -= time.ElapsedGameTime.Milliseconds;
				if (playerCards[i][1] <= 0)
				{
					playerCards[i][1] = 0;
				}
			}
		}
		for (int j = 0; j < dealerCards.Count; j++)
		{
			if (dealerCards[j][1] > 0)
			{
				dealerCards[j][1] -= time.ElapsedGameTime.Milliseconds;
				if (dealerCards[j][1] <= 0)
				{
					dealerCards[j][1] = 0;
				}
			}
		}
		if (startTimer > 0)
		{
			int oldTimer = startTimer;
			startTimer -= time.ElapsedGameTime.Milliseconds;
			if (oldTimer % 250 < startTimer % 250)
			{
				switch (oldTimer / 250)
				{
				case 4:
					dealerCards.Add(new int[2]
					{
						r.Next(1, 12),
						-1
					});
					break;
				case 3:
					dealerCards.Add(new int[2]
					{
						r.Next(1, 10),
						400
					});
					break;
				case 2:
					playerCards.Add(new int[2]
					{
						r.Next(1, 12),
						400
					});
					break;
				case 1:
					playerCards.Add(new int[2]
					{
						r.Next(1, 10),
						400
					});
					break;
				}
				Game1.playSound("shwip", null);
			}
		}
		else if (bustTimer > 0)
		{
			bustTimer -= time.ElapsedGameTime.Milliseconds;
			if (bustTimer <= 0)
			{
				endGame();
			}
		}
		else if (dealerTurnTimer > 0 && !showingResultsScreen)
		{
			dealerTurnTimer -= time.ElapsedGameTime.Milliseconds;
			if (dealerTurnTimer <= 0)
			{
				int dealerTotal = 0;
				foreach (int[] i2 in dealerCards)
				{
					dealerTotal += i2[0];
				}
				int playertotal = 0;
				foreach (int[] i3 in playerCards)
				{
					playertotal += i3[0];
				}
				if (dealerCards[0][1] == -1)
				{
					dealerCards[0][1] = 400;
					Game1.playSound("shwip", null);
				}
				else if (dealerTotal < 18 || (dealerTotal < playertotal && playertotal <= 21))
				{
					int nextCard = r.Next(1, 10);
					int dealerDistance = 21 - dealerTotal;
					if (playertotal == 20 && r.NextBool())
					{
						nextCard = dealerDistance + r.Next(1, 4);
					}
					else if (playertotal == 19 && r.NextDouble() < 0.25)
					{
						nextCard = dealerDistance + r.Next(1, 4);
					}
					else if (playertotal == 18 && r.NextDouble() < 0.1)
					{
						nextCard = dealerDistance + r.Next(1, 4);
					}
					if (r.NextDouble() < Math.Max(0.0005, 0.001 + Game1.player.DailyLuck / 20.0 + (double)((float)Game1.player.LuckLevel * 0.002f)))
					{
						nextCard = 999;
						currentBet *= 3;
					}
					dealerCards.Add(new int[2] { nextCard, 400 });
					dealerTotal += dealerCards.Last()[0];
					Game1.playSound((nextCard == 999) ? "batScreech" : "shwip", null);
					if (dealerTotal > 21)
					{
						bustTimer = 2000;
					}
				}
				else
				{
					bustTimer = 50;
				}
				dealerTurnTimer = 1000;
			}
		}
		if (playButtonsActive())
		{
			hit.scale = (hit.bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? 1.25f : 1f);
			stand.scale = (stand.bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? 1.25f : 1f);
		}
		else if (showingResultsScreen)
		{
			doubleOrNothing.scale = (doubleOrNothing.bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? 1.25f : 1f);
			playAgain.scale = (playAgain.bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? 1.25f : 1f);
			quit.scale = (quit.bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()) ? 1.25f : 1f);
		}
		return false;
	}

	public void endGame()
	{
		if (Game1.options.SnappyMenus)
		{
			currentlySnappedComponent = quit;
			currentlySnappedComponent.snapMouseCursorToCenter();
		}
		showingResultsScreen = true;
		int playertotal = 0;
		foreach (int[] i in playerCards)
		{
			playertotal += i[0];
		}
		if (playertotal == 21)
		{
			Game1.playSound("reward", null);
			playerWon = true;
			endTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11943");
			endMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11944");
			Game1.player.clubCoins += currentBet;
			return;
		}
		if (playertotal > 21)
		{
			Game1.playSound("fishEscape", null);
			endTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11946");
			endMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11947");
			Game1.player.clubCoins -= currentBet;
			if (Game1.player.clubCoins < 0)
			{
				Game1.player.clubCoins = 0;
			}
			return;
		}
		int dealerTotal = 0;
		foreach (int[] i2 in dealerCards)
		{
			dealerTotal += i2[0];
		}
		if (dealerTotal > 21)
		{
			Game1.playSound("reward", null);
			playerWon = true;
			endTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11943");
			endMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11950");
			Game1.player.clubCoins += currentBet;
			return;
		}
		if (playertotal == dealerTotal)
		{
			endTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11951");
			endMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11952");
			return;
		}
		if (playertotal > dealerTotal)
		{
			Game1.playSound("reward", null);
			endTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11943");
			endMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11955", 21);
			Game1.player.clubCoins += currentBet;
			playerWon = true;
			return;
		}
		Game1.playSound("fishEscape", null);
		endTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11946");
		endMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11958", 21);
		Game1.player.clubCoins -= currentBet;
		if (Game1.player.clubCoins < 0)
		{
			Game1.player.clubCoins = 0;
		}
	}

	public void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (playButtonsActive() && bustTimer <= 0)
		{
			if (hit.bounds.Contains(x, y))
			{
				int playertotal = 0;
				foreach (int[] i in playerCards)
				{
					playertotal += i[0];
				}
				int nextCard = r.Next(1, 10);
				int distance = 21 - playertotal;
				if (distance > 1 && distance < 6 && r.NextDouble() < (double)(1f / (float)distance))
				{
					nextCard = r.Choose(distance, distance - 1);
				}
				playerCards.Add(new int[2] { nextCard, 400 });
				Game1.playSound("shwip", null);
				int total = 0;
				foreach (int[] i2 in playerCards)
				{
					total += i2[0];
				}
				if (total == 21)
				{
					bustTimer = 1000;
				}
				else if (total > 21)
				{
					bustTimer = 1000;
				}
			}
			if (stand.bounds.Contains(x, y))
			{
				dealerTurnTimer = 1000;
				Game1.playSound("coin", null);
			}
		}
		else if (showingResultsScreen)
		{
			if (playerWon && doubleOrNothing.containsPoint(x, y))
			{
				Game1.currentMinigame = new CalicoJack(currentBet * 2, highStakes);
				Game1.playSound("bigSelect", null);
			}
			if (Game1.player.clubCoins >= currentBet && playAgain.containsPoint(x, y))
			{
				Game1.currentMinigame = new CalicoJack(-1, highStakes);
				Game1.playSound("smallSelect", null);
			}
			if (quit.containsPoint(x, y))
			{
				Game1.currentMinigame = null;
				Game1.playSound("bigDeSelect", null);
			}
		}
	}

	public void leftClickHeld(int x, int y)
	{
	}

	public void receiveRightClick(int x, int y, bool playSound = true)
	{
	}

	public void releaseLeftClick(int x, int y)
	{
	}

	public void releaseRightClick(int x, int y)
	{
	}

	public void receiveKeyPress(Keys k)
	{
		if (!Game1.options.SnappyMenus || currentlySnappedComponent == null)
		{
			return;
		}
		if (Game1.options.doesInputListContain(Game1.options.moveUpButton, k))
		{
			if (currentlySnappedComponent.Equals(stand))
			{
				currentlySnappedComponent = hit;
			}
			else if (currentlySnappedComponent.Equals(playAgain) && playerWon)
			{
				currentlySnappedComponent = doubleOrNothing;
			}
			else if (currentlySnappedComponent.Equals(quit) && Game1.player.clubCoins >= currentBet)
			{
				currentlySnappedComponent = playAgain;
			}
		}
		else if (Game1.options.doesInputListContain(Game1.options.moveDownButton, k))
		{
			if (currentlySnappedComponent.Equals(hit))
			{
				currentlySnappedComponent = stand;
			}
			else if (currentlySnappedComponent.Equals(doubleOrNothing))
			{
				currentlySnappedComponent = playAgain;
			}
			else if (currentlySnappedComponent.Equals(playAgain))
			{
				currentlySnappedComponent = quit;
			}
		}
		currentlySnappedComponent.snapMouseCursorToCenter();
	}

	public void receiveKeyRelease(Keys k)
	{
	}

	public void draw(SpriteBatch b)
	{
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
		b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.graphics.GraphicsDevice.Viewport.Width, Game1.graphics.GraphicsDevice.Viewport.Height), highStakes ? new Color(130, 0, 82) : Color.DarkGreen);
		Vector2 coin_draw_pos = new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width - 192, 32f);
		SpriteText.drawStringWithScrollBackground(b, coinBuffer + Game1.player.clubCoins, (int)coin_draw_pos.X, (int)coin_draw_pos.Y, "", 1f, null);
		Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(coin_draw_pos.X + 4f, coin_draw_pos.Y + 4f), new Rectangle(211, 373, 9, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
		if (showingResultsScreen)
		{
			SpriteText.drawStringWithScrollCenteredAt(b, endMessage, Game1.graphics.GraphicsDevice.Viewport.Width / 2, 48, "", 1f, null);
			SpriteText.drawStringWithScrollCenteredAt(b, endTitle, Game1.graphics.GraphicsDevice.Viewport.Width / 2, 128, "", 1f, null);
			if (!endTitle.Equals(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11951")))
			{
				SpriteText.drawStringWithScrollCenteredAt(b, Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11965", (playerWon ? "" : "-") + currentBet + "   "), Game1.graphics.GraphicsDevice.Viewport.Width / 2, 256, "", 1f, null);
				Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width / 2 - 32 + SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11965", (playerWon ? "" : "-") + currentBet + "   ")) / 2, 260f) + new Vector2(8f, 0f), new Rectangle(211, 373, 9, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
			}
			if (playerWon)
			{
				IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 373, 9, 9), doubleOrNothing.bounds.X, doubleOrNothing.bounds.Y, doubleOrNothing.bounds.Width, doubleOrNothing.bounds.Height, Color.White, 4f * doubleOrNothing.scale);
				SpriteText.drawString(b, doubleOrNothing.label, doubleOrNothing.bounds.X + 32, doubleOrNothing.bounds.Y + 8, 999999, -1, 999999, 1f, 0.88f, junimoText: false, -1, "", null);
			}
			if (Game1.player.clubCoins >= currentBet)
			{
				IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 373, 9, 9), playAgain.bounds.X, playAgain.bounds.Y, playAgain.bounds.Width, playAgain.bounds.Height, Color.White, 4f * playAgain.scale);
				SpriteText.drawString(b, playAgain.label, playAgain.bounds.X + 32, playAgain.bounds.Y + 8, 999999, -1, 999999, 1f, 0.88f, junimoText: false, -1, "", null);
			}
			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 373, 9, 9), quit.bounds.X, quit.bounds.Y, quit.bounds.Width, quit.bounds.Height, Color.White, 4f * quit.scale);
			SpriteText.drawString(b, quit.label, quit.bounds.X + 32, quit.bounds.Y + 8, 999999, -1, 999999, 1f, 0.88f, junimoText: false, -1, "", null);
		}
		else
		{
			Vector2 start = new Vector2(128f, Game1.graphics.GraphicsDevice.Viewport.Height - 320);
			int total = 0;
			foreach (int[] i in playerCards)
			{
				int cardHeight = 144;
				if (i[1] > 0)
				{
					cardHeight = (int)(Math.Abs((float)i[1] - 200f) / 200f * 144f);
				}
				IClickableMenu.drawTextureBox(b, Game1.mouseCursors, (i[1] > 200 || i[1] == -1) ? new Rectangle(399, 396, 15, 15) : new Rectangle(384, 396, 15, 15), (int)start.X, (int)start.Y + 72 - cardHeight / 2, 96, cardHeight, Color.White, 4f);
				if (i[1] == 0)
				{
					SpriteText.drawStringHorizontallyCenteredAt(b, i[0].ToString() ?? "", (int)start.X + 48 - 8 + 4, (int)start.Y + 72 - 16, 999999, -1, 999999, 1f, 0.88f, junimoText: false, null);
				}
				start.X += 112f;
				if (i[1] == 0)
				{
					total += i[0];
				}
			}
			SpriteText.drawStringWithScrollBackground(b, Game1.player.Name + ": " + total, 160, (int)start.Y + 144 + 32, "", 1f, null);
			start.X = 128f;
			start.Y = 128f;
			total = 0;
			foreach (int[] i2 in dealerCards)
			{
				int cardHeight2 = 144;
				if (i2[1] > 0)
				{
					cardHeight2 = (int)(Math.Abs((float)i2[1] - 200f) / 200f * 144f);
				}
				IClickableMenu.drawTextureBox(b, Game1.mouseCursors, (i2[1] > 200 || i2[1] == -1) ? new Rectangle(399, 396, 15, 15) : new Rectangle(384, 396, 15, 15), (int)start.X, (int)start.Y + 72 - cardHeight2 / 2, 96, cardHeight2, Color.White, 4f);
				if (i2[1] == 0)
				{
					if (i2[0] == 999)
					{
						b.Draw(Game1.objectSpriteSheet, new Vector2(start.X + 48f - 32f, start.Y + 72f - 32f), new Rectangle(16, 592, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
					}
					else
					{
						SpriteText.drawStringHorizontallyCenteredAt(b, i2[0].ToString() ?? "", (int)start.X + 48 - 8 + 4, (int)start.Y + 72 - 16, 999999, -1, 999999, 1f, 0.88f, junimoText: false, null);
					}
				}
				start.X += 112f;
				switch (i2[1])
				{
				case 0:
					total += i2[0];
					break;
				case -1:
					total = -99999;
					break;
				}
			}
			SpriteText.drawStringWithScrollBackground(b, Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11970", (total >= 999) ? "!!!" : ((total > 0) ? (total.ToString() ?? "") : "?")), 160, 32, "", 1f, null);
			SpriteText.drawStringWithScrollBackground(b, Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11972", currentBet + coinBuffer), 160, Game1.graphics.GraphicsDevice.Viewport.Height / 2 - 48, "", 1f, null);
			Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2(172 + SpriteText.getWidthOfString(Game1.content.LoadString("Strings\\StringsFromCSFiles:CalicoJack.cs.11972", currentBet)), Game1.graphics.GraphicsDevice.Viewport.Height / 2 + 4 - 48), new Rectangle(211, 373, 9, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
			if (playButtonsActive())
			{
				IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 373, 9, 9), hit.bounds.X, hit.bounds.Y, hit.bounds.Width, hit.bounds.Height, Color.White, 4f * hit.scale);
				SpriteText.drawString(b, hit.label, hit.bounds.X + 8, hit.bounds.Y + 8, 999999, -1, 999999, 1f, 0.88f, junimoText: false, -1, "", null);
				IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 373, 9, 9), stand.bounds.X, stand.bounds.Y, stand.bounds.Width, stand.bounds.Height, Color.White, 4f * stand.scale);
				SpriteText.drawString(b, stand.label, stand.bounds.X + 8, stand.bounds.Y + 8, 999999, -1, 999999, 1f, 0.88f, junimoText: false, -1, "", null);
			}
		}
		if (Game1.IsMultiplayer)
		{
			Utility.drawTextWithColoredShadow(b, Game1.getTimeOfDayString(Game1.timeOfDay), Game1.dialogueFont, new Vector2((float)Game1.graphics.GraphicsDevice.Viewport.Width - Game1.dialogueFont.MeasureString(Game1.getTimeOfDayString(Game1.timeOfDay)).X - 16f, (float)Game1.graphics.GraphicsDevice.Viewport.Height - Game1.dialogueFont.MeasureString(Game1.getTimeOfDayString(Game1.timeOfDay)).Y - 10f), Color.White, Color.Black * 0.2f);
		}
		if (!Game1.options.hardwareCursor)
		{
			b.Draw(Game1.mouseCursors, new Vector2(Game1.getMouseX(), Game1.getMouseY()), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16), Color.White, 0f, Vector2.Zero, 4f + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
		}
		b.End();
	}

	public void changeScreenSize()
	{
		RepositionButtons();
	}

	public void unload()
	{
	}

	public void receiveEventPoke(int data)
	{
	}

	public string minigameId()
	{
		return "CalicoJack";
	}

	public bool doMainGameUpdates()
	{
		return false;
	}

	public bool forceQuit()
	{
		return true;
	}
}
