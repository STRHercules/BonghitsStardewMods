using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using xTile.Layers;

namespace StardewValley.Events;

public class SoundInTheNightEvent : BaseFarmEvent
{
	public const int cropCircle = 0;

	public const int meteorite = 1;

	public const int dogs = 2;

	public const int owl = 3;

	public const int earthquake = 4;

	public const int raccoonStump = 5;

	private readonly NetInt behavior = new NetInt();

	private float timer;

	private float timeUntilText = 7000f;

	private string soundName;

	private string message;

	private bool playedSound;

	private bool showedMessage;

	private bool finished;

	private Vector2 targetLocation;

	private Building targetBuilding;

	public SoundInTheNightEvent()
		: this(0)
	{
	}

	public SoundInTheNightEvent(int which)
	{
		behavior.Value = which;
	}

	/// <inheritdoc />
	public override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(behavior, "behavior");
	}

	/// <inheritdoc />
	public override bool setUp()
	{
		Random r = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed);
		Farm f = Game1.getFarm();
		f.updateMap();
		timer = 0f;
		switch (behavior.Value)
		{
		case 5:
			soundName = "windstorm";
			message = Game1.content.LoadString("Strings\\1_6_Strings:windstorm");
			timeUntilText = 14000f;
			Game1.player.mailReceived.Add("raccoonTreeFallen");
			break;
		case 0:
		{
			soundName = "UFO";
			message = Game1.content.LoadString("Strings\\Events:SoundInTheNight_UFO");
			int attempts2 = 50;
			Layer backLayer2 = f.map.RequireLayer("Back");
			while (attempts2 > 0)
			{
				targetLocation = new Vector2(r.Next(5, backLayer2.LayerWidth - 4), r.Next(5, backLayer2.LayerHeight - 4));
				if (f.CanItemBePlacedHere(targetLocation))
				{
					break;
				}
				attempts2--;
			}
			if (attempts2 <= 0)
			{
				return true;
			}
			break;
		}
		case 1:
		{
			soundName = "Meteorite";
			message = Game1.content.LoadString("Strings\\Events:SoundInTheNight_Meteorite");
			Layer backLayer3 = f.map.RequireLayer("Back");
			targetLocation = new Vector2(r.Next(5, backLayer3.LayerWidth - 20), r.Next(5, backLayer3.LayerHeight - 4));
			for (int x = (int)targetLocation.X; (float)x <= targetLocation.X + 1f; x++)
			{
				for (int y = (int)targetLocation.Y; (float)y <= targetLocation.Y + 1f; y++)
				{
					Vector2 v = new Vector2(x, y);
					if (!f.isTileOpenBesidesTerrainFeatures(v) || !f.isTileOpenBesidesTerrainFeatures(new Vector2(v.X + 1f, v.Y)) || !f.isTileOpenBesidesTerrainFeatures(new Vector2(v.X + 1f, v.Y - 1f)) || !f.isTileOpenBesidesTerrainFeatures(new Vector2(v.X, v.Y - 1f)) || f.isWaterTile((int)v.X, (int)v.Y) || f.isWaterTile((int)v.X + 1, (int)v.Y))
					{
						return true;
					}
				}
			}
			break;
		}
		case 2:
			soundName = "dogs";
			if (r.NextBool())
			{
				return true;
			}
			foreach (Building b in f.buildings)
			{
				if (b.GetIndoors() is AnimalHouse animalHouse && !b.animalDoorOpen.Value && animalHouse.animalsThatLiveHere.Count > animalHouse.animals.Length && r.NextDouble() < (double)(1f / (float)f.buildings.Count))
				{
					targetBuilding = b;
					break;
				}
			}
			if (targetBuilding == null)
			{
				return true;
			}
			return false;
		case 3:
		{
			soundName = "owl";
			int attempts = 50;
			Layer backLayer = f.map.RequireLayer("Back");
			while (attempts > 0)
			{
				targetLocation = new Vector2(r.Next(5, backLayer.LayerWidth - 4), r.Next(5, backLayer.LayerHeight - 4));
				if (f.CanItemBePlacedHere(targetLocation))
				{
					break;
				}
				attempts--;
			}
			if (attempts <= 0)
			{
				return true;
			}
			break;
		}
		case 4:
			soundName = "thunder_small";
			message = Game1.content.LoadString("Strings\\Events:SoundInTheNight_Earthquake");
			break;
		}
		Game1.freezeControls = true;
		return false;
	}

	/// <inheritdoc />
	public override bool tickUpdate(GameTime time)
	{
		timer += (float)time.ElapsedGameTime.TotalMilliseconds;
		if (timer > 1500f && !playedSound)
		{
			if (!string.IsNullOrEmpty(soundName))
			{
				Game1.playSound(soundName, null);
				playedSound = true;
			}
			if (!playedSound && message != null)
			{
				Game1.drawObjectDialogue(message);
				Game1.globalFadeToClear();
				showedMessage = true;
				if (message == null)
				{
					finished = true;
				}
				else
				{
					Game1.afterDialogues = delegate
					{
						finished = true;
					};
				}
			}
		}
		if (timer > timeUntilText && !showedMessage)
		{
			Game1.pauseThenMessage(10, message);
			showedMessage = true;
			if (message == null)
			{
				finished = true;
			}
			else
			{
				Game1.afterDialogues = delegate
				{
					finished = true;
				};
			}
		}
		if (finished)
		{
			Game1.freezeControls = false;
			return true;
		}
		return false;
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.graphics.GraphicsDevice.Viewport.Width, Game1.graphics.GraphicsDevice.Viewport.Height), Color.Black);
		if (!showedMessage)
		{
			b.Draw(Game1.mouseCursors_1_6, new Vector2(12f, Game1.viewport.Height - 12 - 76), new Rectangle(256 + (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 600.0 / 100.0) * 19, 413, 19, 19), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
		}
	}

	/// <inheritdoc />
	public override void makeChangesToLocation()
	{
		if (!Game1.IsMasterGame)
		{
			return;
		}
		Farm f = Game1.getFarm();
		switch (behavior.Value)
		{
		case 0:
		{
			Object o = ItemRegistry.Create<Object>("(BC)96");
			o.MinutesUntilReady = 24000 - Game1.timeOfDay;
			f.objects.Add(targetLocation, o);
			break;
		}
		case 1:
			f.terrainFeatures.Remove(targetLocation);
			f.terrainFeatures.Remove(targetLocation + new Vector2(1f, 0f));
			f.terrainFeatures.Remove(targetLocation + new Vector2(1f, 1f));
			f.terrainFeatures.Remove(targetLocation + new Vector2(0f, 1f));
			f.resourceClumps.Add(new ResourceClump(622, 2, 2, targetLocation, null));
			break;
		case 2:
		{
			AnimalHouse indoors = (AnimalHouse)targetBuilding.GetIndoors();
			long idOfRemove = 0L;
			foreach (long a in indoors.animalsThatLiveHere)
			{
				if (!indoors.animals.ContainsKey(a))
				{
					idOfRemove = a;
					break;
				}
			}
			if (!Game1.getFarm().animals.Remove(idOfRemove))
			{
				break;
			}
			indoors.animalsThatLiveHere.Remove(idOfRemove);
			{
				foreach (KeyValuePair<long, FarmAnimal> pair in Game1.getFarm().animals.Pairs)
				{
					pair.Value.moodMessage.Value = 5;
				}
				break;
			}
		}
		case 3:
			f.objects.Add(targetLocation, ItemRegistry.Create<Object>("(BC)95"));
			break;
		}
	}
}
