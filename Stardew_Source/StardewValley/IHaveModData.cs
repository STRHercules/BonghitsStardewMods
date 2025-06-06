using StardewValley.Mods;

namespace StardewValley;

/// <summary>An instance with a <see cref="T:StardewValley.Mods.ModDataDictionary" /> field for custom mod data.</summary>
public interface IHaveModData
{
	/// <summary>Custom metadata for this instance, synchronized in multiplayer and persisted in the save file.</summary>
	ModDataDictionary modData { get; }

	/// <summary>The <see cref="P:StardewValley.IHaveModData.modData" /> adjusted for save file serialization. This returns null during save if it's empty. Most code should use <see cref="P:StardewValley.IHaveModData.modData" /> instead.</summary>
	ModDataDictionary modDataForSerialization { get; set; }
}
