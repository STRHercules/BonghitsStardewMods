using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Extensions;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace StardewValley.Buildings;

public class GreenhouseBuilding : Building
{
	protected Farm _farm;

	public GreenhouseBuilding(Vector2 tileLocation)
		: base("Greenhouse", tileLocation)
	{
	}

	public GreenhouseBuilding()
		: this(Vector2.Zero)
	{
	}

	public override void drawBackground(SpriteBatch b)
	{
		base.drawBackground(b);
		if (!base.isMoving)
		{
			DrawEntranceTiles(b);
			drawShadow(b);
		}
	}

	public Farm GetFarm()
	{
		if (_farm == null)
		{
			_farm = Game1.getFarm();
		}
		return _farm;
	}

	public override bool OnUseHumanDoor(Farmer who)
	{
		if (Game1.MasterPlayer.mailReceived.Contains("ccPantry"))
		{
			return true;
		}
		Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:Farm_GreenhouseRuins"));
		return false;
	}

	public override string isThereAnythingtoPreventConstruction(GameLocation location, Vector2 tile_position)
	{
		return null;
	}

	public override bool doesTileHaveProperty(int tile_x, int tile_y, string property_name, string layer_name, ref string property_value)
	{
		if (base.isMoving)
		{
			return false;
		}
		if (layer_name == "Back" && ((tile_x >= tileX.Value - 1 && tile_x <= tileX.Value + tilesWide.Value - 1 && tile_y <= tileY.Value + tilesHigh.Value && tile_y >= tileY.Value) || (CanDrawEntranceTiles() && tile_x >= tileX.Value + 1 && tile_x <= tileX.Value + tilesWide.Value - 2 && tile_y == tileY.Value + tilesHigh.Value + 1)))
		{
			if (CanDrawEntranceTiles() && tile_x >= tileX.Value + humanDoor.X - 1 && tile_x <= tileX.Value + humanDoor.X + 1 && tile_y <= tileY.Value + tilesHigh.Value + 1 && tile_y >= tileY.Value + humanDoor.Y + 1)
			{
				switch (property_name)
				{
				case "Type":
					property_value = "Stone";
					return true;
				case "NoSpawn":
					property_value = "All";
					return true;
				case "Buildable":
					property_value = null;
					return true;
				}
			}
			switch (property_name)
			{
			case "Buildable":
				property_value = "T";
				return true;
			case "NoSpawn":
				property_value = "Tree";
				return true;
			case "Diggable":
				property_value = null;
				return true;
			}
		}
		return base.doesTileHaveProperty(tile_x, tile_y, property_name, layer_name, ref property_value);
	}

	public virtual bool CanDrawEntranceTiles()
	{
		return true;
	}

	public virtual void DrawEntranceTiles(SpriteBatch b)
	{
		Map map = GetFarm().Map;
		Layer back_layer = map.RequireLayer("Back");
		TileSheet tilesheet = map.GetTileSheet("untitled tile sheet");
		if (tilesheet == null)
		{
			tilesheet = map.TileSheets[Math.Min(1, map.TileSheets.Count - 1)];
		}
		if (tilesheet != null)
		{
			StaticTile tile = new StaticTile(back_layer, tilesheet, BlendMode.Alpha, 812);
			if (CanDrawEntranceTiles())
			{
				float draw_layer = 0f;
				Vector2 vector_draw_position = Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value + humanDoor.Value.X - 1, tileY.Value + humanDoor.Value.Y + 1) * 64f);
				Location draw_location = new Location((int)vector_draw_position.X, (int)vector_draw_position.Y);
				Game1.mapDisplayDevice.DrawTile(tile, draw_location, draw_layer);
				draw_location.X += 64;
				Game1.mapDisplayDevice.DrawTile(tile, draw_location, draw_layer);
				draw_location.X += 64;
				Game1.mapDisplayDevice.DrawTile(tile, draw_location, draw_layer);
				tile = new StaticTile(back_layer, tilesheet, BlendMode.Alpha, 838);
				vector_draw_position = Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value + humanDoor.Value.X - 1, tileY.Value + humanDoor.Value.Y + 2) * 64f);
				draw_location.X = (int)vector_draw_position.X;
				draw_location.Y = (int)vector_draw_position.Y;
				Game1.mapDisplayDevice.DrawTile(tile, draw_location, draw_layer);
				draw_location.X += 64;
				Game1.mapDisplayDevice.DrawTile(tile, draw_location, draw_layer);
				draw_location.X += 64;
				Game1.mapDisplayDevice.DrawTile(tile, draw_location, draw_layer);
			}
		}
	}

	public override void drawShadow(SpriteBatch b, int localX = -1, int localY = -1)
	{
		Microsoft.Xna.Framework.Rectangle shadow_rectangle = new Microsoft.Xna.Framework.Rectangle(112, 0, 128, 144);
		if (CanDrawEntranceTiles())
		{
			shadow_rectangle.Y = 144;
		}
		b.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, new Vector2((tileX.Value - 1) * 64, tileY.Value * 64)), shadow_rectangle, Color.White * ((localX == -1) ? alpha : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
	}
}
