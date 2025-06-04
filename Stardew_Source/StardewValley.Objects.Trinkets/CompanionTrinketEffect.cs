using System;
using StardewValley.Companions;
using StardewValley.Extensions;
using StardewValley.TokenizableStrings;

namespace StardewValley.Objects.Trinkets;

/// <summary>Implements the special behavior for a <see cref="T:StardewValley.Objects.Trinkets.Trinket" /> which summons a hungry frog companion.</summary>
public class CompanionTrinketEffect : TrinketEffect
{
	/// <summary>The frog variant to spawn.</summary>
	public int Variant;

	/// <inheritdoc />
	public CompanionTrinketEffect(Trinket trinket)
		: base(trinket)
	{
	}

	/// <inheritdoc />
	public override bool GenerateRandomStats(Trinket trinket)
	{
		Random r = Utility.CreateRandom(trinket.generationSeed.Value);
		if (r.NextBool(0.2))
		{
			Variant = 0;
		}
		else if (r.NextBool(0.8))
		{
			Variant = r.Next(3);
		}
		else if (r.NextBool(0.8))
		{
			Variant = r.Next(3) + 3;
		}
		else
		{
			Variant = r.Next(2) + 6;
		}
		trinket.displayNameOverrideTemplate.Value = TokenStringBuilder.LocalizedText("Strings\\1_6_Strings:frog_variant_" + Variant);
		return true;
	}

	/// <inheritdoc />
	public override void Apply(Farmer farmer)
	{
		Companion = new HungryFrogCompanion(Variant);
		if (Game1.gameMode == 3)
		{
			farmer.AddCompanion(Companion);
		}
	}

	/// <inheritdoc />
	public override void Unapply(Farmer farmer)
	{
		farmer.RemoveCompanion(Companion);
	}
}
