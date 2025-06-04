using System;
using Microsoft.Xna.Framework;
using StardewValley.Characters;
using StardewValley.Util;

namespace StardewValley.Buildings;

public class Stable : Building
{
	public Guid HorseId
	{
		get
		{
			return id.Value;
		}
		set
		{
			id.Value = value;
		}
	}

	public Stable()
		: this(Vector2.Zero)
	{
	}

	public Stable(Vector2 tileLocation)
		: this(tileLocation, GuidHelper.NewGuid())
	{
	}

	public Stable(Vector2 tileLocation, Guid horseId)
		: base("Stable", tileLocation)
	{
		HorseId = horseId;
	}

	public override Rectangle? getSourceRectForMenu()
	{
		return new Rectangle(0, 0, texture.Value.Bounds.Width, texture.Value.Bounds.Height);
	}

	public Horse getStableHorse()
	{
		return Utility.findHorse(HorseId);
	}

	/// <summary>Get the default tile position for a horse in this stable.</summary>
	public Point GetDefaultHorseTile()
	{
		return new Point(tileX.Value + 1, tileY.Value + 1);
	}

	public virtual void grabHorse()
	{
		if (daysOfConstructionLeft.Value <= 0)
		{
			Horse horse = Utility.findHorse(HorseId);
			Point defaultTile = GetDefaultHorseTile();
			if (horse == null)
			{
				horse = new Horse(HorseId, defaultTile.X, defaultTile.Y);
				GetParentLocation().characters.Add(horse);
			}
			else
			{
				Game1.warpCharacter(horse, parentLocationName.Value, defaultTile);
			}
			horse.ownerId.Value = owner.Value;
		}
	}

	public virtual void updateHorseOwnership()
	{
		if (daysOfConstructionLeft.Value > 0)
		{
			return;
		}
		Horse horse = Utility.findHorse(HorseId);
		if (horse == null)
		{
			return;
		}
		horse.ownerId.Value = owner.Value;
		if (horse.getOwner() != null)
		{
			if (horse.getOwner().horseName.Value != null)
			{
				horse.name.Value = horse.getOwner().horseName.Value;
				horse.displayName = horse.getOwner().horseName.Value;
			}
			else
			{
				horse.name.Value = "";
				horse.displayName = "";
			}
		}
	}

	public override void dayUpdate(int dayOfMonth)
	{
		base.dayUpdate(dayOfMonth);
		grabHorse();
	}

	/// <inheritdoc />
	public override void performActionOnDemolition(GameLocation location)
	{
		base.performActionOnDemolition(location);
		Horse horse = getStableHorse();
		horse?.currentLocation?.characters.Remove(horse);
	}
}
