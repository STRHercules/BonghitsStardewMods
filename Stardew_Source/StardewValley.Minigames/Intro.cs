using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Menus;

namespace StardewValley.Minigames;

[InstanceStatics]
public class Intro : IMinigame
{
	public class Balloon
	{
		public Vector2 position;

		public Color color;

		public Balloon(int screenWidth, int screenHeight)
		{
			int g = Game1.random.Next(255);
			int b = 255 - g;
			int r = Game1.random.Choose(255, 0);
			position = new Vector2(Game1.random.Next(screenWidth / 5, screenWidth), screenHeight);
			color = new Color(r, g, b);
		}

		public void update(float speed, GameTime time)
		{
			position.Y -= speed * (float)time.ElapsedGameTime.TotalMilliseconds / 16f;
			position.X -= speed * (float)time.ElapsedGameTime.TotalMilliseconds / 32f;
		}
	}

	public int pixelScale = 4;

	public const int skyLoopWidth = 112;

	public const int cloudLoopWidth = 170;

	public const int tilesBeyondViewportToSimulate = 6;

	public const int leftFence = 0;

	public const int centerFence = 1;

	public const int rightFence = 2;

	public const int busYRest = 240;

	public const int choosingCharacterState = 0;

	public const int panningDownFromCloudsState = 1;

	public const int panningDownToRoadState = 2;

	public const int drivingState = 3;

	public const int stardewInViewState = 4;

	public float speed = 0.1f;

	private float valleyPosition;

	private float skyPosition;

	private float roadPosition;

	private float bigCloudPosition;

	private float backCloudPosition;

	private float globalYPan;

	private float globalYPanDY;

	private float drivingTimer;

	private float fadeAlpha;

	private float treePosition;

	private int screenWidth;

	private int screenHeight;

	private int tileSize = 16;

	private Matrix transformMatrix;

	private Texture2D texture;

	private Texture2D roadsideTexture;

	private Texture2D cloudTexture;

	private Texture2D treeStripTexture;

	private List<Point> backClouds = new List<Point>();

	private List<int> road = new List<int>();

	private List<int> sky = new List<int>();

	private List<int> roadsideObjects = new List<int>();

	private List<int> roadsideFences = new List<int>();

	private Color skyColor;

	private Color roadColor;

	private Color carColor;

	private bool cameraCenteredOnBus = true;

	private bool addedSign;

	private Vector2 busPosition;

	private Vector2 carPosition;

	private Vector2 birdPosition = Vector2.Zero;

	private CharacterCustomization characterCreateMenu;

	private List<Balloon> balloons = new List<Balloon>();

	private int birdFrame;

	private float birdTimer;

	private float birdXTimer;

	public static ICue roadNoise;

	private int fenceBuildStatus = -1;

	private int currentState;

	private bool quit;

	private bool hasQuit;

	public Intro()
	{
		texture = Game1.content.Load<Texture2D>("Minigames\\Intro");
		roadsideTexture = Game1.content.Load<Texture2D>("Maps\\spring_outdoorsTileSheet");
		cloudTexture = Game1.content.Load<Texture2D>("Minigames\\Clouds");
		treeStripTexture = Game1.content.Load<Texture2D>("Minigames\\treestrip");
		transformMatrix = Matrix.CreateScale(pixelScale);
		skyColor = new Color(64, 136, 248);
		roadColor = new Color(130, 130, 130);
		createBeginningOfLevel();
		Game1.player.FarmerSprite.SourceRect = new Rectangle(0, 0, 16, 32);
		bigCloudPosition = cloudTexture.Width;
		roadNoise = Game1.soundBank.GetCue("roadnoise");
		currentState = 1;
		Game1.changeMusicTrack("spring_day_ambient");
		changeScreenSize();
	}

	public Intro(int startingGameMode)
	{
		texture = Game1.content.Load<Texture2D>("Minigames\\Intro");
		roadsideTexture = Game1.content.Load<Texture2D>("Maps\\spring_outdoorsTileSheet");
		cloudTexture = Game1.content.Load<Texture2D>("Minigames\\Clouds");
		transformMatrix = Matrix.CreateScale(pixelScale);
		skyColor = new Color(102, 181, 255);
		roadColor = new Color(130, 130, 130);
		createBeginningOfLevel();
		currentState = startingGameMode;
		if (currentState == 4)
		{
			fadeAlpha = 1f;
		}
		changeScreenSize();
	}

	public bool overrideFreeMouseMovement()
	{
		return Game1.options.SnappyMenus;
	}

	public void createBeginningOfLevel()
	{
		backClouds.Clear();
		road.Clear();
		sky.Clear();
		roadsideObjects.Clear();
		roadsideFences.Clear();
		for (int i = 0; i < screenWidth / tileSize + 6; i++)
		{
			road.Add((!(Game1.random.NextDouble() < 0.7)) ? Game1.random.Next(0, 3) : 0);
			roadsideObjects.Add(-1);
			roadsideFences.Add(-1);
		}
		for (int j = 0; j < screenWidth / 112 + 2; j++)
		{
			sky.Add(Game1.random.Choose(0, 1, 1));
		}
		for (int k = 0; k < screenWidth / 170 + 2; k++)
		{
			backClouds.Add(new Point(Game1.random.Next(3), Game1.random.Next(screenHeight / 2)));
		}
		roadsideObjects.Add(-1);
		roadsideObjects.Add(-1);
		roadsideObjects.Add(-1);
		busPosition = new Vector2(tileSize * 8, 240f);
	}

	public void updateRoad(GameTime time)
	{
		roadPosition += (float)time.ElapsedGameTime.TotalMilliseconds * speed;
		if (roadPosition >= (float)(tileSize * 3))
		{
			roadPosition -= tileSize * 3;
			for (int i = 0; i < 3; i++)
			{
				road.Add((!(Game1.random.NextDouble() < 0.7)) ? Game1.random.Next(0, 3) : 0);
			}
			road.RemoveRange(0, 3);
			if (fenceBuildStatus != -1 || (cameraCenteredOnBus && Game1.random.NextDouble() < 0.1))
			{
				for (int j = 0; j < 3; j++)
				{
					switch (fenceBuildStatus)
					{
					case -1:
						fenceBuildStatus = 0;
						roadsideFences.Add(0);
						break;
					case 0:
						fenceBuildStatus = 1;
						roadsideFences.Add(Game1.random.Next(3));
						break;
					case 1:
						if (Game1.random.NextDouble() < 0.1)
						{
							roadsideFences.Add(2);
							fenceBuildStatus = 2;
						}
						else
						{
							fenceBuildStatus = 1;
							roadsideFences.Add((Game1.random.NextDouble() < 0.1) ? 3 : Game1.random.Next(3));
						}
						break;
					case 2:
					{
						fenceBuildStatus = -1;
						for (int k = j; k < 3; k++)
						{
							roadsideFences.Add(-1);
						}
						break;
					}
					}
					if (fenceBuildStatus == -1)
					{
						break;
					}
				}
			}
			else
			{
				roadsideFences.Add(-1);
				roadsideFences.Add(-1);
				roadsideFences.Add(-1);
			}
			roadsideFences.RemoveRange(0, 3);
			if (cameraCenteredOnBus && !addedSign && Game1.random.NextDouble() < 0.25)
			{
				for (int l = 0; l < 3; l++)
				{
					if (l == 0 && Game1.random.NextDouble() < 0.3)
					{
						roadsideObjects.Add(Game1.random.Next(2));
						for (int m = l; m < 3; m++)
						{
							roadsideObjects.Add(-1);
						}
						break;
					}
					if (Game1.random.NextBool())
					{
						roadsideObjects.Add(Game1.random.Next(2, 5));
					}
					else
					{
						roadsideObjects.Add(-1);
					}
				}
			}
			else
			{
				roadsideObjects.Add(-1);
				roadsideObjects.Add(-1);
				roadsideObjects.Add(-1);
			}
			roadsideObjects.RemoveRange(0, 3);
		}
		skyPosition += (float)time.ElapsedGameTime.TotalMilliseconds * (speed / 12f);
		if (skyPosition >= 112f)
		{
			skyPosition -= 112f;
			sky.Add(Game1.random.Next(2));
			sky.RemoveAt(0);
		}
		treePosition += (float)time.ElapsedGameTime.TotalMilliseconds * (speed / 2f);
		if (treePosition >= 256f)
		{
			treePosition -= 256f;
		}
		valleyPosition += (float)time.ElapsedGameTime.TotalMilliseconds * (speed / 6f);
		if (carPosition.Equals(Vector2.Zero) && Game1.random.NextDouble() < 0.002 && !addedSign)
		{
			carPosition = new Vector2(screenWidth, 200f);
			carColor = new Color(Game1.random.Next(100, 255), Game1.random.Next(100, 255), Game1.random.Next(100, 255));
		}
		else if (!carPosition.Equals(Vector2.Zero))
		{
			carPosition.X -= 0.1f * (float)time.ElapsedGameTime.TotalMilliseconds * ((float)(int)carColor.G / 60f);
			if (carPosition.X < -200f)
			{
				carPosition = Vector2.Zero;
			}
		}
	}

	public void updateUpperClouds(GameTime time)
	{
		bigCloudPosition += (float)time.ElapsedGameTime.TotalMilliseconds * (speed / 24f);
		if (bigCloudPosition >= (float)(cloudTexture.Width * 3))
		{
			bigCloudPosition -= cloudTexture.Width * 3;
		}
		backCloudPosition += (float)time.ElapsedGameTime.TotalMilliseconds * (speed / 36f);
		if (backCloudPosition > 170f)
		{
			backCloudPosition %= 170f;
			backClouds.Add(new Point(Game1.random.Next(3), Game1.random.Next(screenHeight / 2)));
			backClouds.RemoveAt(0);
		}
		if (Game1.random.NextDouble() < 0.0002)
		{
			balloons.Add(new Balloon(screenWidth, screenHeight));
			if (Game1.random.NextDouble() < 0.1)
			{
				Vector2 position = new Vector2(Game1.random.Next(screenWidth / 3, screenWidth), screenHeight);
				balloons.Add(new Balloon(screenWidth, screenHeight)
				{
					position = new Vector2(position.X + (float)Game1.random.Next(-16, 16), position.Y + (float)Game1.random.Next(8))
				});
				balloons.Add(new Balloon(screenWidth, screenHeight)
				{
					position = new Vector2(position.X + (float)Game1.random.Next(-16, 16), position.Y + (float)Game1.random.Next(8))
				});
				balloons.Add(new Balloon(screenWidth, screenHeight)
				{
					position = new Vector2(position.X + (float)Game1.random.Next(-16, 16), position.Y + (float)Game1.random.Next(8))
				});
				balloons.Add(new Balloon(screenWidth, screenHeight)
				{
					position = new Vector2(position.X + (float)Game1.random.Next(-16, 16), position.Y + (float)Game1.random.Next(8))
				});
			}
		}
		for (int i = balloons.Count - 1; i >= 0; i--)
		{
			balloons[i].update(speed, time);
			if (balloons[i].position.X < (float)(-tileSize) || balloons[i].position.Y < (float)(-tileSize))
			{
				balloons.RemoveAt(i);
			}
		}
	}

	public bool tick(GameTime time)
	{
		if (hasQuit)
		{
			return true;
		}
		if (quit && !hasQuit)
		{
			Game1.warpFarmer("BusStop", 22, 11, flip: false);
			roadNoise?.Stop(AudioStopOptions.Immediate);
			Game1.exitActiveMenu();
			hasQuit = true;
			return true;
		}
		switch (currentState)
		{
		case 0:
			updateUpperClouds(time);
			break;
		case 1:
			globalYPanDY = Math.Min(4f, globalYPanDY + (float)time.ElapsedGameTime.TotalMilliseconds * (speed / 140f));
			globalYPan -= globalYPanDY;
			updateUpperClouds(time);
			if (globalYPan < -1f)
			{
				globalYPan = screenHeight * pixelScale;
				currentState = 2;
				transformMatrix = Matrix.CreateScale(pixelScale);
				transformMatrix.Translation = new Vector3(0f, globalYPan, 0f);
				if (roadNoise != null)
				{
					roadNoise.SetVariable("Volume", 0);
					roadNoise.Play();
				}
				Game1.game1.loadForNewGame();
			}
			break;
		case 2:
		{
			int startPanY = screenHeight * pixelScale;
			int endPanY = -Math.Max(0, 900 - Game1.graphics.GraphicsDevice.Viewport.Height);
			endPanY = -(int)(240f * (540f / (float)Game1.graphics.GraphicsDevice.Viewport.Height));
			globalYPanDY = Math.Max(1f, globalYPan / 100f);
			globalYPan -= globalYPanDY;
			if (globalYPan <= (float)endPanY)
			{
				globalYPan = endPanY;
			}
			transformMatrix = Matrix.CreateScale(pixelScale);
			transformMatrix.Translation = new Vector3(0f, globalYPan, 0f);
			updateRoad(time);
			if (roadNoise != null)
			{
				float vol = (globalYPan - (float)startPanY) / (float)(endPanY - startPanY) * 10f + 90f;
				roadNoise.SetVariable("Volume", vol);
			}
			if (globalYPan <= (float)endPanY)
			{
				currentState = 3;
			}
			break;
		}
		case 3:
			updateRoad(time);
			drivingTimer += (float)time.ElapsedGameTime.TotalMilliseconds;
			if (drivingTimer > 4700f)
			{
				drivingTimer = 0f;
				currentState = 4;
			}
			break;
		case 4:
			updateRoad(time);
			drivingTimer += (float)time.ElapsedGameTime.TotalMilliseconds;
			if (!(drivingTimer > 2000f))
			{
				break;
			}
			busPosition.X += (float)time.ElapsedGameTime.TotalMilliseconds / 8f;
			roadNoise?.SetVariable("Volume", Math.Max(0f, roadNoise.GetVariable("Volume") - 1f));
			speed = Math.Max(0f, speed - (float)time.ElapsedGameTime.TotalMilliseconds / 70000f);
			if (!addedSign)
			{
				addedSign = true;
				roadsideObjects.RemoveAt(roadsideObjects.Count - 1);
				roadsideObjects.Add(5);
				Game1.playSound("busDriveOff", null);
			}
			if (speed <= 0f && birdPosition.Equals(Vector2.Zero))
			{
				int position = 0;
				for (int i = 0; i < roadsideObjects.Count; i++)
				{
					if (roadsideObjects[i] == 5)
					{
						position = i;
						break;
					}
				}
				birdPosition = new Vector2((float)(position * 16) - roadPosition - 32f + 16f, -16f);
				Game1.playSound("SpringBirds", null);
				fadeAlpha = 0f;
			}
			if (!birdPosition.Equals(Vector2.Zero) && birdPosition.Y < 116f)
			{
				float dy = Math.Max(0.5f, (116f - birdPosition.Y) / 116f * 2f);
				birdPosition.Y += dy;
				birdPosition.X += (float)Math.Sin((double)birdXTimer / (Math.PI * 16.0)) * dy / 2f;
				birdTimer += (float)time.ElapsedGameTime.TotalMilliseconds;
				birdXTimer += (float)time.ElapsedGameTime.TotalMilliseconds;
				if (birdTimer >= 100f)
				{
					birdFrame = (birdFrame + 1) % 4;
					birdTimer = 0f;
				}
			}
			else if (!birdPosition.Equals(Vector2.Zero))
			{
				birdFrame = ((birdTimer > 1500f) ? 5 : 4);
				birdTimer += (float)time.ElapsedGameTime.TotalMilliseconds;
				if (birdTimer > 2400f || (birdTimer > 1800f && Game1.random.NextDouble() < 0.006))
				{
					birdTimer = 0f;
					if (Game1.random.NextBool())
					{
						Game1.playSound("SpringBirds", null);
						birdPosition.Y -= 4f;
					}
				}
			}
			if (drivingTimer > 14000f)
			{
				fadeAlpha += (float)time.ElapsedGameTime.TotalMilliseconds * 0.1f / 128f;
				if (fadeAlpha >= 1f)
				{
					Game1.warpFarmer("BusStop", 22, 11, flip: false);
					roadNoise?.Stop(AudioStopOptions.Immediate);
					Game1.exitActiveMenu();
					return true;
				}
			}
			break;
		}
		return false;
	}

	public void doneCreatingCharacter()
	{
		characterCreateMenu = null;
		currentState = 1;
		Game1.changeMusicTrack("spring_day_ambient");
	}

	public void receiveLeftClick(int x, int y, bool playSound = true)
	{
		characterCreateMenu?.receiveLeftClick(x, y);
		for (int i = balloons.Count - 1; i >= 0; i--)
		{
			if (new Rectangle((int)balloons[i].position.X * 4 + 16, (int)balloons[i].position.Y * 4 + 16, 32, 32).Contains(x, y))
			{
				balloons.RemoveAt(i);
				Game1.playSound("coin", null);
			}
		}
	}

	public void receiveRightClick(int x, int y, bool playSound = true)
	{
		characterCreateMenu?.receiveRightClick(x, y);
	}

	public void releaseLeftClick(int x, int y)
	{
		characterCreateMenu?.releaseLeftClick(x, y);
	}

	public void leftClickHeld(int x, int y)
	{
		characterCreateMenu?.leftClickHeld(x, y);
	}

	public void releaseRightClick(int x, int y)
	{
	}

	public void receiveKeyPress(Keys k)
	{
		if (k == Keys.Escape && currentState != 1)
		{
			if (!quit)
			{
				Game1.playSound("bigDeSelect", null);
			}
			quit = true;
		}
	}

	public void receiveKeyRelease(Keys k)
	{
	}

	public void draw(SpriteBatch b)
	{
		switch (currentState)
		{
		case 1:
		{
			b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
			b.GraphicsDevice.Clear(skyColor);
			int x = 64;
			int y = Game1.graphics.GraphicsDevice.Viewport.Height - 64;
			int w = 0;
			int h = 64;
			Utility.makeSafe(ref x, ref y, w, h);
			SpriteText.drawString(b, Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3689"), x, y, 999, -1, 999, 1f, 1f, junimoText: false, 0, "", null);
			b.End();
			break;
		}
		case 2:
		case 3:
		case 4:
			drawRoadArea(b);
			break;
		case 0:
			break;
		}
	}

	public void drawRoadArea(SpriteBatch b)
	{
		b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, transformMatrix);
		b.GraphicsDevice.Clear(roadColor);
		b.Draw(Game1.staminaRect, new Rectangle(0, -screenHeight * 2, screenWidth, screenHeight * 8), skyColor);
		b.Draw(Game1.staminaRect, new Rectangle(0, screenHeight / 2 + 80 - 100, screenWidth, screenHeight * 4), roadColor);
		for (int i = 0; i < screenWidth / 112 + 2; i++)
		{
			if (sky[i] == 0)
			{
				b.Draw(texture, new Vector2(0f - skyPosition + (float)(i * 112) - (float)(i * 2), -16f), new Rectangle(129, 0, 110, 96), Color.White);
			}
			else
			{
				b.Draw(sourceRectangle: new Rectangle(128, 0, 1, 96), texture: texture, destinationRectangle: new Rectangle((int)(0f - skyPosition) - 1 + i * 112 - i * 2, -16, 114, 96), color: Color.White);
			}
		}
		for (int j = 0; j < 12; j++)
		{
			b.Draw(Game1.mouseCursors, new Vector2(-10f + (0f - valleyPosition) / 2f + (float)(j * 639) - (float)(j * 2), 70f), new Rectangle(0, 886, 639, 148), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.08f);
			b.Draw(Game1.mouseCursors, new Vector2(0f - valleyPosition + (float)(j * 639) - (float)(j * 2), 80f), new Rectangle(0, 737, 639, 120), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.08f);
		}
		for (int k = 0; k < 8; k++)
		{
			b.Draw(treeStripTexture, new Vector2((float)(k * 256) - treePosition, 110f), new Rectangle(0, 0, 256, 64), Color.White);
		}
		for (int l = 0; l < road.Count; l++)
		{
			if (l % 3 == 0)
			{
				b.Draw(texture, new Vector2((float)(l * 16) - roadPosition, 160f), new Rectangle(0, 176, 48, 48), Color.White);
				b.Draw(texture, new Vector2((float)(l * 16 + tileSize) - roadPosition, 272f), new Rectangle(0, 64, 16, 16), Color.White);
			}
			b.Draw(texture, new Vector2((float)(l * 16) - roadPosition, 208f), new Rectangle(road[l] * 16, 240, 16, 16), Color.White);
		}
		for (int m = 0; m < roadsideObjects.Count; m++)
		{
			switch (roadsideObjects[m])
			{
			case 0:
				b.Draw(roadsideTexture, new Vector2((float)(m * 16) - roadPosition - 32f, 96f), new Rectangle(48, 0, 48, 96), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
				break;
			case 1:
				b.Draw(roadsideTexture, new Vector2((float)(m * 16) - roadPosition - 32f, 96f), new Rectangle(0, 0, 48, 64), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
				b.Draw(roadsideTexture, new Vector2((float)(m * 16) - roadPosition - 16f, 160f), new Rectangle(16, 64, 16, 32), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
				break;
			case 2:
				b.Draw(roadsideTexture, new Vector2((float)(m * 16) - roadPosition - 32f, 176f), new Rectangle(112, 144, 16, 16), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
				break;
			case 3:
				b.Draw(roadsideTexture, new Vector2((float)(m * 16) - roadPosition - 32f, 176f), new Rectangle(112, 160, 16, 16), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
				break;
			case 5:
				b.Draw(texture, new Vector2((float)(m * 16) - roadPosition - 32f, 128f), new Rectangle(48, 176, 64, 64), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
				break;
			}
		}
		for (int n = 0; n < roadsideFences.Count; n++)
		{
			if (roadsideFences[n] != -1)
			{
				if (roadsideFences[n] == 3)
				{
					b.Draw(roadsideTexture, new Vector2((float)(n * 16) - roadPosition, 176f), new Rectangle(144, 256, 16, 32), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
				}
				else
				{
					b.Draw(roadsideTexture, new Vector2((float)(n * 16) - roadPosition, 176f), new Rectangle(128 + roadsideFences[n] * 16, 224, 16, 32), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
				}
			}
		}
		if (!carPosition.Equals(Vector2.Zero))
		{
			b.Draw(texture, carPosition, new Rectangle(160, 112, 80, 64), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			b.Draw(texture, carPosition, new Rectangle(160, 176, 80, 64), carColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
		}
		b.Draw(texture, busPosition, new Rectangle(0, 0, 128, 64), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
		b.Draw(texture, busPosition + new Vector2(23.5f, 56.5f) * 1f, new Rectangle(21, 54, 5, 5), Color.White, (float)((double)(roadPosition / 3f / 16f) * Math.PI * 2.0), new Vector2(2.5f, 2.5f), 1f, SpriteEffects.None, 0f);
		b.Draw(texture, busPosition + new Vector2(87.5f, 56.5f) * 1f, new Rectangle(21, 54, 5, 5), Color.White, (float)((double)((roadPosition + 4f) / 3f / 16f) * Math.PI * 2.0), new Vector2(2.5f, 2.5f), 1f, SpriteEffects.None, 0f);
		if (!birdPosition.Equals(Vector2.Zero))
		{
			b.Draw(texture, birdPosition, new Rectangle(16 + birdFrame * 16, 64, 16, 16), Color.White);
		}
		if (fadeAlpha > 0f)
		{
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, screenWidth + 2, screenHeight * 2), Color.Black * fadeAlpha);
		}
		b.End();
	}

	public void changeScreenSize()
	{
		if (Game1.graphics.GraphicsDevice.Viewport.Height < 1000)
		{
			pixelScale = 3;
		}
		else if (Game1.graphics.GraphicsDevice.Viewport.Width > 2600)
		{
			pixelScale = 5;
		}
		else
		{
			pixelScale = 4;
		}
		transformMatrix = Matrix.CreateScale(pixelScale);
		screenWidth = Game1.graphics.GraphicsDevice.Viewport.Width / pixelScale;
		screenHeight = Game1.graphics.GraphicsDevice.Viewport.Height / pixelScale;
		createBeginningOfLevel();
	}

	public void unload()
	{
	}

	public void receiveEventPoke(int data)
	{
		throw new NotImplementedException();
	}

	public string minigameId()
	{
		return null;
	}

	public bool doMainGameUpdates()
	{
		return false;
	}

	public bool forceQuit()
	{
		return false;
	}
}
