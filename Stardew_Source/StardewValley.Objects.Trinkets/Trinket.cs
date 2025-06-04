using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley.GameData;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.TokenizableStrings;

namespace StardewValley.Objects.Trinkets;

/// <summary>An item which can be equipped in the player's trinket slot which has special effects while equipped.</summary>
public class Trinket : Object
{
	protected string _description;

	protected TrinketData _data;

	protected TrinketEffect _trinketEffect;

	protected string _trinketEffectClassName;

	/// <summary>The parsed form of <see cref="F:StardewValley.Objects.Trinkets.Trinket.displayNameOverrideTemplate" /> used to build the display name for <see cref="M:StardewValley.Objects.Trinkets.Trinket.loadDisplayName" />.</summary>
	protected string displayNameOverride;

	/// <summary>The net-synced <see cref="T:StardewValley.TokenizableStrings.TokenParser">tokenized string</see> used to build the display name for <see cref="M:StardewValley.Objects.Trinkets.Trinket.loadDisplayName" />.</summary>
	public readonly NetString displayNameOverrideTemplate = new NetString();

	/// <summary>The net-synced <see cref="T:StardewValley.TokenizableStrings.TokenParser">tokenized strings</see> used to fill placeholders in <see cref="M:StardewValley.Objects.Trinkets.Trinket.getDescription" />.</summary>
	public readonly NetStringList descriptionSubstitutionTemplates = new NetStringList();

	public readonly NetStringDictionary<string, NetString> trinketMetadata = new NetStringDictionary<string, NetString>();

	[XmlElement("generationSeed")]
	public readonly NetInt generationSeed = new NetInt();

	/// <inheritdoc />
	public override string TypeDefinitionId { get; } = "(TR)";

	public Trinket()
	{
	}

	public Trinket(string itemId, int generationSeed)
		: this()
	{
		base.ItemId = itemId;
		base.name = itemId;
		this.generationSeed.Value = generationSeed;
		ParsedItemData data = ItemRegistry.GetDataOrErrorItem(itemId);
		base.ParentSheetIndex = data.SpriteIndex;
		Dictionary<string, string> fromModData = GetTrinketData()?.ModData;
		if (fromModData != null && fromModData.Count > 0)
		{
			foreach (KeyValuePair<string, string> pair in fromModData)
			{
				base.modData.Add(pair.Key, pair.Value);
			}
		}
		GetEffect()?.GenerateRandomStats(this);
	}

	public static bool CanSpawnTrinket(Farmer f)
	{
		return f.stats.Get("trinketSlots") != 0;
	}

	public static void SpawnTrinket(GameLocation location, Vector2 spawnPoint)
	{
		Trinket t = GetRandomTrinket();
		if (t != null)
		{
			Game1.createItemDebris(t, spawnPoint, Game1.random.Next(4), location);
		}
	}

	/// <summary>Re-roll the trinket stats if applicable.</summary>
	/// <param name="newSeed">The new trinket seed to set.</param>
	/// <remarks>Returns whether the trinket stats were re-rolled (regardless of whether they changed).</remarks>
	public bool RerollStats(int newSeed)
	{
		generationSeed.Value = newSeed;
		return GetEffect()?.GenerateRandomStats(this) ?? false;
	}

	/// <inheritdoc />
	public override bool canBeShipped()
	{
		return false;
	}

	/// <inheritdoc />
	public override int sellToStorePrice(long specificPlayerID = -1L)
	{
		return 1000;
	}

	public static void TrySpawnTrinket(GameLocation location, Monster monster, Vector2 spawnPosition, double chanceModifier = 1.0)
	{
		if (!CanSpawnTrinket(Game1.player))
		{
			return;
		}
		double baseChance = 0.004;
		if (monster != null)
		{
			baseChance += (double)monster.MaxHealth * 1E-05;
			if (monster.isGlider.Value && monster.MaxHealth >= 150)
			{
				baseChance += 0.002;
			}
			if (monster is Leaper)
			{
				baseChance -= 0.005;
			}
		}
		baseChance = Math.Min(0.025, baseChance);
		baseChance += Game1.player.DailyLuck / 25.0;
		baseChance += (double)((float)Game1.player.LuckLevel * 0.00133f);
		baseChance *= chanceModifier;
		if (Game1.random.NextDouble() < baseChance)
		{
			SpawnTrinket(location, spawnPosition);
		}
	}

	public static Trinket GetRandomTrinket()
	{
		Dictionary<string, TrinketData> data_sheet = DataLoader.Trinkets(Game1.content);
		Trinket t = null;
		while (t == null)
		{
			int which = Game1.random.Next(data_sheet.Count);
			int i = 0;
			foreach (KeyValuePair<string, TrinketData> pair in data_sheet)
			{
				if (which == i && pair.Value.DropsNaturally)
				{
					t = ItemRegistry.Create<Trinket>("(TR)" + pair.Key);
					break;
				}
				i++;
			}
		}
		return t;
	}

	/// <inheritdoc />
	public override bool canBeGivenAsGift()
	{
		return true;
	}

	public override void reloadSprite()
	{
		base.reloadSprite();
		GetEffect()?.GenerateRandomStats(this);
	}

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(trinketMetadata, "trinketMetadata").AddField(generationSeed, "generationSeed").AddField(displayNameOverrideTemplate, "displayNameOverrideTemplate")
			.AddField(descriptionSubstitutionTemplates, "descriptionSubstitutionTemplates");
		displayNameOverrideTemplate.fieldChangeVisibleEvent += delegate(NetString field, string oldValue, string newValue)
		{
			displayNameOverride = TokenParser.ParseText(newValue);
		};
		descriptionSubstitutionTemplates.OnElementChanged += delegate
		{
			_description = null;
		};
		descriptionSubstitutionTemplates.OnArrayReplaced += delegate
		{
			_description = null;
		};
	}

	public TrinketData GetTrinketData()
	{
		if (_data == null)
		{
			_data = DataLoader.Trinkets(Game1.content).GetValueOrDefault(base.ItemId);
		}
		return _data;
	}

	public virtual TrinketEffect GetEffect()
	{
		if (_trinketEffect == null)
		{
			TrinketData data = GetTrinketData();
			if (data != null && _trinketEffectClassName != data.TrinketEffectClass)
			{
				_trinketEffectClassName = data.TrinketEffectClass;
				if (data.TrinketEffectClass != null)
				{
					Type trinketEffectType = System.Type.GetType(data.TrinketEffectClass);
					if (trinketEffectType != null)
					{
						_trinketEffect = (TrinketEffect)Activator.CreateInstance(trinketEffectType, this);
					}
					else
					{
						Game1.log.Warn($"Failed loading effects for trinket {base.QualifiedItemId}: invalid class type '{data.TrinketEffectClass}'.");
					}
				}
			}
		}
		return _trinketEffect;
	}

	/// <inheritdoc />
	protected override string loadDisplayName()
	{
		ParsedItemData data = ItemRegistry.GetDataOrErrorItem(base.ItemId);
		return displayNameOverride ?? data.DisplayName;
	}

	public override int maximumStackSize()
	{
		return 1;
	}

	public override string getDescription()
	{
		if (_description == null)
		{
			string description = TokenParser.ParseText(ItemRegistry.GetDataOrErrorItem(base.ItemId).Description);
			if (descriptionSubstitutionTemplates.Count > 0)
			{
				object[] tokens = new object[descriptionSubstitutionTemplates.Count];
				for (int i = 0; i < descriptionSubstitutionTemplates.Count; i++)
				{
					tokens[i] = TokenParser.ParseText(descriptionSubstitutionTemplates[i]);
				}
				description = string.Format(description, tokens);
			}
			_description = Game1.parseText(description, Game1.smallFont, getDescriptionWidth());
		}
		return _description;
	}

	/// <inheritdoc />
	public override string getCategoryName()
	{
		return Game1.content.LoadString("Strings\\1_6_Strings:Trinket");
	}

	/// <inheritdoc />
	public override Color getCategoryColor()
	{
		return new Color(96, 81, 255);
	}

	public override bool isPlaceable()
	{
		return false;
	}

	public override bool performUseAction(GameLocation location)
	{
		GetEffect().OnUse(Game1.player);
		return false;
	}

	public override bool performToolAction(Tool t)
	{
		return false;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Trinket(base.ItemId, generationSeed.Value);
	}

	/// <inheritdoc />
	protected override void GetOneCopyFrom(Item source)
	{
		base.GetOneCopyFrom(source);
		if (source is Trinket other)
		{
			displayNameOverrideTemplate.Value = other.displayNameOverrideTemplate.Value;
			descriptionSubstitutionTemplates.Set(other.descriptionSubstitutionTemplates);
			trinketMetadata.Set(other.trinketMetadata.Pairs);
			generationSeed.Value = other.generationSeed.Value;
		}
	}

	public override bool IsHeldOverHead()
	{
		return false;
	}

	/// <summary>Handle the trinket being equipped.</summary>
	/// <param name="farmer">The player equipping the trinket.</param>
	public virtual void Apply(Farmer farmer)
	{
		GetEffect()?.Apply(farmer);
	}

	/// <summary>Handle the trinket being unequipped.</summary>
	/// <param name="farmer">The player unequipping the trinket.</param>
	public virtual void Unapply(Farmer farmer)
	{
		GetEffect()?.Unapply(farmer);
	}

	/// <summary>Update the trinket.</summary>
	/// <param name="farmer">The player with the trinket equipped.</param>
	/// <param name="time">The elapsed game time.</param>
	/// <param name="location">The player's current location.</param>
	public virtual void Update(Farmer farmer, GameTime time, GameLocation location)
	{
		GetEffect()?.Update(farmer, time, location);
	}

	/// <summary>Handle the player having taken a step.</summary>
	/// <param name="farmer">The player with the trinket equipped.</param>
	public virtual void OnFootstep(Farmer farmer)
	{
		GetEffect()?.OnFootstep(farmer);
	}

	/// <summary>Handle the player having received damage.</summary>
	/// <param name="farmer">The player with the trinket equipped.</param>
	/// <param name="damageAmount">The amount of damage that was taken.</param>
	public virtual void OnReceiveDamage(Farmer farmer, int damageAmount)
	{
		GetEffect()?.OnReceiveDamage(farmer, damageAmount);
	}

	/// <summary>Handle the player dealing damage to a monster.</summary>
	/// <param name="farmer">The player with the trinket equipped.</param>
	/// <param name="monster">The monster which was damaged.</param>
	/// <param name="damageAmount">The amount of damage that was dealt.</param>
	/// <param name="isBomb">Whether the damage is from a bomb.</param>
	/// <param name="isCriticalHit">Whether the attack which caused the damage was a critical hit.</param>
	public virtual void OnDamageMonster(Farmer farmer, Monster monster, int damageAmount, bool isBomb, bool isCriticalHit)
	{
		GetEffect()?.OnDamageMonster(farmer, monster, damageAmount, isBomb, isCriticalHit);
	}
}
