using StardewValley.Monsters;

namespace StardewValley.Enchantments;

public class SlimeSlayerEnchantment : BaseWeaponEnchantment
{
	public override bool IsSecondaryEnchantment()
	{
		return true;
	}

	public override bool IsForge()
	{
		return false;
	}

	/// <inheritdoc />
	public override void OnCalculateDamage(Monster monster, GameLocation location, Farmer who, bool fromBomb, ref int amount)
	{
		base.OnCalculateDamage(monster, location, who, fromBomb, ref amount);
		if (!fromBomb && monster is GreenSlime)
		{
			amount = (int)((float)amount * 1.33f + 1f);
		}
	}

	public override int GetMaximumLevel()
	{
		return 5;
	}

	public override string GetName()
	{
		return Game1.content.LoadString("Strings\\1_6_Strings:SlimeSlayerEnchantment");
	}
}
