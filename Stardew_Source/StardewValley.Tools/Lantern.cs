using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;

namespace StardewValley.Tools;

public class Lantern : Tool
{
	public const float baseRadius = 10f;

	public const int millisecondsPerFuelUnit = 6000;

	public const int maxFuel = 100;

	public int fuelLeft;

	private int fuelTimer;

	public bool on;

	[XmlIgnore]
	public string lightSourceId;

	public Lantern()
		: base("Lantern", 0, 74, 74, stackable: false)
	{
		base.InstantUse = true;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		return new Lantern();
	}

	public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
	{
		base.DoFunction(location, x, y, power, who);
		on = !on;
		base.CurrentParentTileIndex = base.IndexOfMenuItemView;
		Utility.removeLightSource(lightSourceId);
		if (on)
		{
			lightSourceId = GenerateLightSourceId(who);
			Game1.currentLightSources.Add(new LightSource(lightSourceId, 1, new Vector2(who.Position.X + 21f, who.Position.Y + 64f), 2.5f + (float)fuelLeft / 100f * 10f * 0.75f, new Color(0, 131, 255), LightSource.LightContext.None, 0L));
		}
	}

	public override void tickUpdate(GameTime time, Farmer who)
	{
		if (on && fuelLeft > 0 && Game1.drawLighting)
		{
			fuelTimer += time.ElapsedGameTime.Milliseconds;
			if (fuelTimer > 6000)
			{
				fuelLeft--;
				fuelTimer = 0;
			}
			Vector2 lightPosition = new Vector2(who.Position.X + 21f, who.Position.Y + 64f);
			if (Game1.currentLightSources.TryGetValue(lightSourceId, out var light))
			{
				light.position.Value = lightPosition;
			}
			else
			{
				lightSourceId = GenerateLightSourceId(who);
				Game1.currentLightSources.Add(new LightSource(lightSourceId, 1, lightPosition, 2.5f + (float)fuelLeft / 100f * 10f * 0.75f, new Color(0, 131, 255), LightSource.LightContext.None, 0L));
			}
		}
		if (on && fuelLeft <= 0)
		{
			Utility.removeLightSource(GenerateLightSourceId(who));
		}
	}
}
