using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using xTile.Dimensions;

namespace StardewValley.Locations;

public class IslandWestCave1 : IslandLocation
{
	public class CaveCrystal
	{
		public Vector2 tileLocation;

		public int id;

		public int pitch;

		public Color color;

		public Color currentColor;

		public float shakeTimer;

		public float glowTimer;

		public void update()
		{
			if (glowTimer > 0f)
			{
				glowTimer -= (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
				currentColor.R = (byte)Utility.Lerp((int)color.R, 255f, glowTimer / 1000f);
				currentColor.G = (byte)Utility.Lerp((int)color.G, 255f, glowTimer / 1000f);
				currentColor.B = (byte)Utility.Lerp((int)color.B, 255f, glowTimer / 1000f);
			}
			if (shakeTimer > 0f)
			{
				shakeTimer -= (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
			}
		}

		public void activate()
		{
			glowTimer = 1000f;
			shakeTimer = 100f;
			Game1.playSound("crystal", pitch);
			currentColor = color;
		}

		public void draw(SpriteBatch b)
		{
			b.Draw(Game1.mouseCursors2, Game1.GlobalToLocal(tileLocation * 64f + new Vector2(8f, 10f) * 4f), new Microsoft.Xna.Framework.Rectangle(188, 228, 52, 28), currentColor, 0f, new Vector2(52f, 28f) / 2f, 4f, SpriteEffects.None, (tileLocation.Y * 64f + 64f - 8f) / 10000f);
			b.Draw(Game1.mouseCursors2, Game1.GlobalToLocal(tileLocation * 64f + new Vector2(0f, -52f) + new Vector2((shakeTimer > 0f) ? Game1.random.Next(-1, 2) : 0, (shakeTimer > 0f) ? Game1.random.Next(-1, 2) : 0)), new Microsoft.Xna.Framework.Rectangle(240, 227, 16, 29), currentColor, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tileLocation.Y * 64f + 64f - 4f) / 10000f);
		}
	}

	public const string lightSourceId = "IslandWestCave1";

	[XmlIgnore]
	protected List<CaveCrystal> crystals = new List<CaveCrystal>();

	public const int PHASE_INTRO = 0;

	public const int PHASE_PLAY_SEQUENCE = 1;

	public const int PHASE_WAIT_FOR_PLAYER_INPUT = 2;

	public const int PHASE_NOTHING = 3;

	public const int PHASE_SUCCESSFUL_SEQUENCE = 4;

	public const int PHASE_OUTRO = 5;

	[XmlElement("completed")]
	public NetBool completed = new NetBool();

	[XmlIgnore]
	public NetBool isActivated = new NetBool(value: false);

	[XmlIgnore]
	public NetFloat netPhaseTimer = new NetFloat();

	[XmlIgnore]
	public float localPhaseTimer;

	[XmlIgnore]
	public float betweenNotesTimer;

	[XmlIgnore]
	public int localPhase;

	[XmlIgnore]
	public NetInt netPhase = new NetInt(3);

	[XmlIgnore]
	public NetInt currentDifficulty = new NetInt(2);

	[XmlIgnore]
	public NetInt currentCrystalSequenceIndex = new NetInt(0);

	[XmlIgnore]
	public int currentPlaybackCrystalSequenceIndex;

	[XmlIgnore]
	public NetInt timesFailed = new NetInt(0);

	[XmlIgnore]
	public NetList<int, NetInt> currentCrystalSequence = new NetList<int, NetInt>();

	[XmlIgnore]
	public NetEvent1Field<int, NetInt> enterValueEvent = new NetEvent1Field<int, NetInt>();

	public IslandWestCave1()
	{
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(netPhase, "netPhase").AddField(isActivated, "isActivated").AddField(currentDifficulty, "currentDifficulty")
			.AddField(currentCrystalSequenceIndex, "currentCrystalSequenceIndex")
			.AddField(currentCrystalSequence, "currentCrystalSequence")
			.AddField(enterValueEvent.NetFields, "enterValueEvent.NetFields")
			.AddField(netPhaseTimer, "netPhaseTimer")
			.AddField(completed, "completed")
			.AddField(timesFailed, "timesFailed");
		enterValueEvent.onEvent += enterValue;
		isActivated.fieldChangeVisibleEvent += onActivationChanged;
	}

	public IslandWestCave1(string map, string name)
		: base(map, name)
	{
	}

	public void onActivationChanged(NetBool field, bool old_value, bool new_value)
	{
		updateActivationVisuals();
	}

	protected override void resetSharedState()
	{
		base.resetSharedState();
		resetPuzzle();
	}

	public void resetPuzzle()
	{
		isActivated.Value = false;
		updateActivationVisuals();
		netPhase.Value = 3;
	}

	public override void MakeMapModifications(bool force = false)
	{
		base.MakeMapModifications(force);
		UpdateActivationTiles();
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		if (crystals.Count == 0)
		{
			crystals.Add(new CaveCrystal
			{
				tileLocation = new Vector2(3f, 4f),
				color = new Color(220, 0, 255),
				currentColor = new Color(220, 0, 255),
				id = 1,
				pitch = 0
			});
			crystals.Add(new CaveCrystal
			{
				tileLocation = new Vector2(4f, 6f),
				color = Color.Lime,
				currentColor = Color.Lime,
				id = 2,
				pitch = 700
			});
			crystals.Add(new CaveCrystal
			{
				tileLocation = new Vector2(6f, 7f),
				color = new Color(255, 50, 100),
				currentColor = new Color(255, 50, 100),
				id = 3,
				pitch = 1200
			});
			crystals.Add(new CaveCrystal
			{
				tileLocation = new Vector2(8f, 6f),
				color = new Color(0, 200, 255),
				currentColor = new Color(0, 200, 255),
				id = 4,
				pitch = 1400
			});
			crystals.Add(new CaveCrystal
			{
				tileLocation = new Vector2(9f, 4f),
				color = new Color(255, 180, 0),
				currentColor = new Color(255, 180, 0),
				id = 5,
				pitch = 1600
			});
		}
		updateActivationVisuals();
	}

	/// <inheritdoc />
	public override bool performAction(string[] action, Farmer who, Location tileLocation)
	{
		if (who.IsLocalPlayer)
		{
			string text = ArgUtility.Get(action, 0);
			if (!(text == "Crystal"))
			{
				if (text == "CrystalCaveActivate" && !isActivated.Value && !completed.Value)
				{
					isActivated.Value = true;
					Game1.playSound("openBox", null);
					updateActivationVisuals();
					netPhaseTimer.Value = 1200f;
					netPhase.Value = 0;
					currentDifficulty.Value = 2;
					return true;
				}
			}
			else
			{
				if (!ArgUtility.TryGetInt(action, 1, out var crystalId, out var error, "int crystalId"))
				{
					LogTileActionError(action, tileLocation.X, tileLocation.Y, error);
					return false;
				}
				if (netPhase.Value == 5 || netPhase.Value == 3 || netPhase.Value == 2)
				{
					enterValueEvent.Fire(crystalId);
					return true;
				}
			}
		}
		return base.performAction(action, who, tileLocation);
	}

	public virtual void updateActivationVisuals()
	{
		if (map != null && Game1.gameMode != 6 && Game1.currentLocation == this)
		{
			if (isActivated.Value || completed.Value)
			{
				Game1.currentLightSources.Add(new LightSource("IslandWestCave1", 1, new Vector2(6.5f, 1f) * 64f, 2f, Color.Black, LightSource.LightContext.None, 0L, base.NameOrUniqueName));
			}
			else
			{
				Utility.removeLightSource("IslandWestCave1");
			}
			UpdateActivationTiles();
			if (completed.Value)
			{
				addCompletionTorches();
			}
		}
	}

	public virtual void UpdateActivationTiles()
	{
		if (map != null && Game1.gameMode != 6 && Game1.currentLocation == this)
		{
			int headIndex = ((isActivated.Value || completed.Value) ? 33 : 31);
			setMapTile(6, 1, headIndex, "Buildings", "untitled tile sheet");
		}
	}

	public virtual void enterValue(int which)
	{
		if (netPhase.Value == 2 && Game1.IsMasterGame && currentCrystalSequence.Count > currentCrystalSequenceIndex.Value)
		{
			if (currentCrystalSequence[currentCrystalSequenceIndex.Value] != which - 1)
			{
				playSound("cancel", null, null);
				resetPuzzle();
				timesFailed.Value++;
				return;
			}
			currentCrystalSequenceIndex.Value++;
			if (currentCrystalSequenceIndex.Value >= currentCrystalSequence.Count)
			{
				DelayedAction.playSoundAfterDelay((currentDifficulty.Value == 7) ? "discoverMineral" : "newArtifact", 500, this, null);
				netPhaseTimer.Value = 2000f;
				netPhase.Value = 4;
			}
		}
		if (crystals.Count > which - 1)
		{
			crystals[which - 1].activate();
		}
	}

	public override void cleanupBeforePlayerExit()
	{
		crystals.Clear();
		base.cleanupBeforePlayerExit();
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		enterValueEvent.Poll();
		if ((localPhase != 1 || currentPlaybackCrystalSequenceIndex < 0 || currentPlaybackCrystalSequenceIndex >= currentCrystalSequence.Count) && localPhase != netPhase.Value)
		{
			localPhaseTimer = netPhaseTimer.Value;
			localPhase = netPhase.Value;
			if (localPhase != 1)
			{
				currentPlaybackCrystalSequenceIndex = -1;
			}
			else
			{
				currentPlaybackCrystalSequenceIndex = 0;
			}
		}
		base.UpdateWhenCurrentLocation(time);
		foreach (CaveCrystal crystal in crystals)
		{
			crystal.update();
		}
		if (localPhaseTimer > 0f)
		{
			localPhaseTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
			if (localPhaseTimer <= 0f)
			{
				switch (localPhase)
				{
				case 0:
				case 4:
					currentPlaybackCrystalSequenceIndex = 0;
					if (Game1.IsMasterGame)
					{
						currentDifficulty.Value++;
						currentCrystalSequence.Clear();
						currentCrystalSequenceIndex.Value = 0;
						if (currentDifficulty.Value > ((timesFailed.Value < 8) ? 7 : 6))
						{
							netPhaseTimer.Value = 10f;
							netPhase.Value = 5;
							break;
						}
						for (int i = 0; i < currentDifficulty.Value; i++)
						{
							currentCrystalSequence.Add(Game1.random.Next(5));
						}
						netPhase.Value = 1;
					}
					betweenNotesTimer = 600f;
					break;
				case 5:
					if (Game1.currentLocation == this)
					{
						Game1.playSound("fireball", null);
						Utility.addSmokePuff(this, new Vector2(5f, 1f) * 64f);
						Utility.addSmokePuff(this, new Vector2(7f, 1f) * 64f);
					}
					if (Game1.IsMasterGame)
					{
						Game1.player.team.MarkCollectedNut("IslandWestCavePuzzle");
						Game1.createObjectDebris("(O)73", 5, 1, this);
						Game1.createObjectDebris("(O)73", 7, 1, this);
						Game1.createObjectDebris("(O)73", 6, 1, this);
					}
					completed.Value = true;
					if (Game1.currentLocation == this)
					{
						addCompletionTorches();
					}
					break;
				}
			}
		}
		if (localPhase != 1)
		{
			return;
		}
		betweenNotesTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
		if (!(betweenNotesTimer <= 0f) || currentCrystalSequence.Count <= 0 || currentPlaybackCrystalSequenceIndex < 0)
		{
			return;
		}
		int which = currentCrystalSequence[currentPlaybackCrystalSequenceIndex];
		if (which < crystals.Count)
		{
			crystals[which].activate();
		}
		currentPlaybackCrystalSequenceIndex++;
		int betweenNotesDivisor = currentDifficulty.Value;
		if (currentDifficulty.Value > 5)
		{
			betweenNotesDivisor--;
			if (timesFailed.Value >= 4)
			{
				betweenNotesDivisor--;
			}
			if (timesFailed.Value >= 6)
			{
				betweenNotesDivisor--;
			}
			if (timesFailed.Value >= 8)
			{
				betweenNotesDivisor = 3;
			}
		}
		else if (timesFailed.Value >= 4 && currentDifficulty.Value > 4)
		{
			betweenNotesDivisor--;
		}
		betweenNotesTimer = 1500f / (float)betweenNotesDivisor;
		if (currentDifficulty.Value > ((timesFailed.Value < 8) ? 7 : 6))
		{
			betweenNotesTimer = 100f;
		}
		if (currentPlaybackCrystalSequenceIndex < currentCrystalSequence.Count)
		{
			return;
		}
		currentPlaybackCrystalSequenceIndex = -1;
		if (currentDifficulty.Value > ((timesFailed.Value < 8) ? 7 : 6))
		{
			if (Game1.IsMasterGame)
			{
				netPhaseTimer.Value = 1000f;
				netPhase.Value = 5;
			}
		}
		else if (Game1.IsMasterGame)
		{
			netPhase.Value = 2;
			currentCrystalSequenceIndex.Value = 0;
		}
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		base.TransferDataFromSavedLocation(l);
		if (l is IslandWestCave1 cave)
		{
			completed.Value = cave.completed.Value;
		}
	}

	public void addCompletionTorches()
	{
		if (completed.Value)
		{
			temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), new Vector2(5f, 1f) * 64f + new Vector2(0f, -20f), flipped: false, 0f, Color.White)
			{
				interval = 50f,
				totalNumberOfLoops = 99999,
				animationLength = 4,
				lightId = "IslandWestCave1_Torch_1",
				lightRadius = 2f,
				scale = 4f,
				layerDepth = 0.013439999f
			});
			temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), new Vector2(7f, 1f) * 64f + new Vector2(8f, -20f), flipped: false, 0f, Color.White)
			{
				interval = 50f,
				totalNumberOfLoops = 99999,
				animationLength = 4,
				lightId = "IslandWestCave1_Torch_2",
				lightRadius = 2f,
				scale = 4f,
				layerDepth = 0.013439999f
			});
		}
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		foreach (CaveCrystal crystal in crystals)
		{
			crystal.draw(b);
		}
	}
}
