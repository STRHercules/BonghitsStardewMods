using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace StardewValley.Locations;

public class DecoratableLocation : GameLocation
{
	/// <summary>Obsolete.</summary>
	public readonly DecorationFacade wallPaper = new DecorationFacade();

	[XmlIgnore]
	public readonly NetStringList wallpaperIDs = new NetStringList();

	public readonly NetStringDictionary<string, NetString> appliedWallpaper = new NetStringDictionary<string, NetString>
	{
		InterpolationWait = false
	};

	[XmlIgnore]
	public readonly Dictionary<string, List<Vector3>> wallpaperTiles = new Dictionary<string, List<Vector3>>();

	/// <summary>Obsolete.</summary>
	public readonly DecorationFacade floor = new DecorationFacade();

	[XmlIgnore]
	public readonly NetStringList floorIDs = new NetStringList();

	public readonly NetStringDictionary<string, NetString> appliedFloor = new NetStringDictionary<string, NetString>
	{
		InterpolationWait = false
	};

	[XmlIgnore]
	public readonly Dictionary<string, List<Vector3>> floorTiles = new Dictionary<string, List<Vector3>>();

	protected Dictionary<string, TileSheet> _wallAndFloorTileSheets = new Dictionary<string, TileSheet>();

	protected Map _wallAndFloorTileSheetMap;

	/// <summary>Whether to log troubleshooting warnings for wallpaper and flooring issues.</summary>
	public static bool LogTroubleshootingInfo;

	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(appliedWallpaper, "appliedWallpaper").AddField(appliedFloor, "appliedFloor").AddField(floorIDs, "floorIDs")
			.AddField(wallpaperIDs, "wallpaperIDs");
		appliedWallpaper.OnValueAdded += delegate(string key, string value)
		{
			UpdateWallpaper(key);
		};
		appliedWallpaper.OnConflictResolve += delegate(string key, NetString rejected, NetString accepted)
		{
			UpdateWallpaper(key);
		};
		appliedWallpaper.OnValueTargetUpdated += delegate(string key, string old_value, string new_value)
		{
			if (appliedWallpaper.FieldDict.TryGetValue(key, out var value2))
			{
				value2.CancelInterpolation();
			}
			UpdateWallpaper(key);
		};
		appliedFloor.OnValueAdded += delegate(string key, string value)
		{
			UpdateFloor(key);
		};
		appliedFloor.OnConflictResolve += delegate(string key, NetString rejected, NetString accepted)
		{
			UpdateFloor(key);
		};
		appliedFloor.OnValueTargetUpdated += delegate(string key, string old_value, string new_value)
		{
			if (appliedFloor.FieldDict.TryGetValue(key, out var value3))
			{
				value3.CancelInterpolation();
			}
			UpdateFloor(key);
		};
	}

	public DecoratableLocation()
	{
	}

	public DecoratableLocation(string mapPath, string name)
		: base(mapPath, name)
	{
	}

	public override void updateLayout()
	{
		base.updateLayout();
		if (Game1.IsMasterGame)
		{
			setWallpapers();
			setFloors();
		}
	}

	public virtual void ReadWallpaperAndFloorTileData()
	{
		updateMap();
		wallpaperTiles.Clear();
		floorTiles.Clear();
		wallpaperIDs.Clear();
		floorIDs.Clear();
		string defaultWallpaper = "0";
		string defaultFlooring = "0";
		if (this is FarmHouse { upgradeLevel: <3 })
		{
			Farm farm = Game1.getLocationFromName("Farm", isStructure: false) as Farm;
			defaultWallpaper = FarmHouse.GetStarterWallpaper(farm) ?? "0";
			defaultFlooring = FarmHouse.GetStarterFlooring(farm) ?? "0";
		}
		Dictionary<string, string> initial_values = new Dictionary<string, string>();
		if (TryGetMapProperty("WallIDs", out var wallProperty))
		{
			string[] array = wallProperty.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				string[] data_split = ArgUtility.SplitBySpace(array[i]);
				if (data_split.Length >= 1)
				{
					wallpaperIDs.Add(data_split[0]);
				}
				if (data_split.Length >= 2)
				{
					initial_values[data_split[0]] = data_split[1];
				}
			}
		}
		if (wallpaperIDs.Count == 0)
		{
			List<Microsoft.Xna.Framework.Rectangle> walls = getWalls();
			for (int j = 0; j < walls.Count; j++)
			{
				string id = "Wall_" + j;
				wallpaperIDs.Add(id);
				Microsoft.Xna.Framework.Rectangle rect = walls[j];
				if (!wallpaperTiles.ContainsKey(j.ToString()))
				{
					wallpaperTiles[id] = new List<Vector3>();
				}
				foreach (Point tile in rect.GetPoints())
				{
					wallpaperTiles[id].Add(new Vector3(tile.X, tile.Y, tile.Y - rect.Top));
				}
			}
		}
		else
		{
			for (int x = 0; x < map.Layers[0].LayerWidth; x++)
			{
				for (int y = 0; y < map.Layers[0].LayerHeight; y++)
				{
					string tile_property = doesTileHaveProperty(x, y, "WallID", "Back");
					if (tile_property == null)
					{
						continue;
					}
					if (!wallpaperIDs.Contains(tile_property))
					{
						wallpaperIDs.Add(tile_property);
					}
					if (appliedWallpaper.TryAdd(tile_property, defaultWallpaper) && initial_values.TryGetValue(tile_property, out var initial_value))
					{
						if (appliedWallpaper.TryGetValue(initial_value, out var newValue))
						{
							appliedWallpaper[tile_property] = newValue;
						}
						else if (GetWallpaperSource(initial_value).Value >= 0)
						{
							appliedWallpaper[tile_property] = initial_value;
						}
					}
					if (!wallpaperTiles.TryGetValue(tile_property, out var areas))
					{
						areas = (wallpaperTiles[tile_property] = new List<Vector3>());
					}
					areas.Add(new Vector3(x, y, 0f));
					if (IsFloorableOrWallpaperableTile(x, y + 1, "Back"))
					{
						areas.Add(new Vector3(x, y + 1, 1f));
					}
					if (IsFloorableOrWallpaperableTile(x, y + 2, "Buildings"))
					{
						areas.Add(new Vector3(x, y + 2, 2f));
					}
					else if (IsFloorableOrWallpaperableTile(x, y + 2, "Back") && !IsFloorableTile(x, y + 2, "Back"))
					{
						areas.Add(new Vector3(x, y + 2, 2f));
					}
				}
			}
		}
		initial_values.Clear();
		if (TryGetMapProperty("FloorIDs", out var floorProperty))
		{
			string[] array = floorProperty.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				string[] data_split2 = ArgUtility.SplitBySpace(array[i]);
				if (data_split2.Length >= 1)
				{
					floorIDs.Add(data_split2[0]);
				}
				if (data_split2.Length >= 2)
				{
					initial_values[data_split2[0]] = data_split2[1];
				}
			}
		}
		if (floorIDs.Count == 0)
		{
			List<Microsoft.Xna.Framework.Rectangle> floors = getFloors();
			for (int k = 0; k < floors.Count; k++)
			{
				string id2 = "Floor_" + k;
				floorIDs.Add(id2);
				Microsoft.Xna.Framework.Rectangle rect2 = floors[k];
				if (!floorTiles.ContainsKey(k.ToString()))
				{
					floorTiles[id2] = new List<Vector3>();
				}
				foreach (Point tile2 in rect2.GetPoints())
				{
					floorTiles[id2].Add(new Vector3(tile2.X, tile2.Y, 0f));
				}
			}
		}
		else
		{
			for (int l = 0; l < map.Layers[0].LayerWidth; l++)
			{
				for (int m = 0; m < map.Layers[0].LayerHeight; m++)
				{
					string tile_property2 = doesTileHaveProperty(l, m, "FloorID", "Back");
					if (tile_property2 == null)
					{
						continue;
					}
					if (!floorIDs.Contains(tile_property2))
					{
						floorIDs.Add(tile_property2);
					}
					if (appliedFloor.TryAdd(tile_property2, defaultFlooring) && initial_values.TryGetValue(tile_property2, out var initial_value2))
					{
						if (appliedFloor.TryGetValue(initial_value2, out var newValue2))
						{
							appliedFloor[tile_property2] = newValue2;
						}
						else if (GetFloorSource(initial_value2).Value >= 0)
						{
							appliedFloor[tile_property2] = initial_value2;
						}
					}
					if (!floorTiles.TryGetValue(tile_property2, out var areas2))
					{
						areas2 = (floorTiles[tile_property2] = new List<Vector3>());
					}
					areas2.Add(new Vector3(l, m, 0f));
				}
			}
		}
		setFloors();
		setWallpapers();
	}

	public virtual TileSheet GetWallAndFloorTilesheet(string id)
	{
		if (map != _wallAndFloorTileSheetMap)
		{
			_wallAndFloorTileSheets.Clear();
			_wallAndFloorTileSheetMap = map;
		}
		if (_wallAndFloorTileSheets.TryGetValue(id, out var wallAndFloorTilesheet))
		{
			return wallAndFloorTilesheet;
		}
		try
		{
			foreach (ModWallpaperOrFlooring entry in DataLoader.AdditionalWallpaperFlooring(Game1.content))
			{
				if (!(entry.Id != id))
				{
					Texture2D texture = Game1.content.Load<Texture2D>(entry.Texture);
					if (texture.Width != 256)
					{
						Game1.log.Warn($"The tilesheet for wallpaper/floor '{entry.Id}' is {texture.Width} pixels wide, but it must be exactly {256} pixels wide.");
					}
					TileSheet tilesheet = new TileSheet("x_WallsAndFloors_" + id, map, entry.Texture, new Size(texture.Width / 16, texture.Height / 16), new Size(16, 16));
					map.AddTileSheet(tilesheet);
					map.LoadTileSheets(Game1.mapDisplayDevice);
					_wallAndFloorTileSheets[id] = tilesheet;
					return tilesheet;
				}
			}
			Game1.log.Error("The tilesheet for wallpaper/floor '" + id + "' could not be loaded: no such ID found in Data/AdditionalWallpaperFlooring.");
			_wallAndFloorTileSheets[id] = null;
			return null;
		}
		catch (Exception exception)
		{
			Game1.log.Error("The tilesheet for wallpaper/floor '" + id + "' could not be loaded.", exception);
			_wallAndFloorTileSheets[id] = null;
			return null;
		}
	}

	public virtual KeyValuePair<string, int> GetFloorSource(string pattern_id)
	{
		int pattern_index;
		if (pattern_id.Contains(':'))
		{
			string[] pattern_split = pattern_id.Split(':');
			TileSheet tilesheet = GetWallAndFloorTilesheet(pattern_split[0]);
			if (int.TryParse(pattern_split[1], out pattern_index) && tilesheet != null)
			{
				return new KeyValuePair<string, int>(tilesheet.Id, pattern_index);
			}
		}
		if (int.TryParse(pattern_id, out pattern_index))
		{
			return new KeyValuePair<string, int>("walls_and_floors", pattern_index);
		}
		return new KeyValuePair<string, int>(null, -1);
	}

	public virtual KeyValuePair<string, int> GetWallpaperSource(string pattern_id)
	{
		int pattern_index;
		if (pattern_id.Contains(':'))
		{
			string[] pattern_split = pattern_id.Split(':');
			TileSheet tilesheet = GetWallAndFloorTilesheet(pattern_split[0]);
			if (int.TryParse(pattern_split[1], out pattern_index) && tilesheet != null)
			{
				return new KeyValuePair<string, int>(tilesheet.Id, pattern_index);
			}
		}
		if (int.TryParse(pattern_id, out pattern_index))
		{
			return new KeyValuePair<string, int>("walls_and_floors", pattern_index);
		}
		return new KeyValuePair<string, int>(null, -1);
	}

	public virtual void UpdateFloor(string floorId)
	{
		updateMap();
		if (!appliedFloor.TryGetValue(floorId, out var patternId) || !floorTiles.TryGetValue(floorId, out var tiles))
		{
			return;
		}
		bool appliedAny = false;
		HashSet<string> errors = null;
		foreach (Vector3 item in tiles)
		{
			int x = (int)item.X;
			int y = (int)item.Y;
			KeyValuePair<string, int> source = GetFloorSource(patternId);
			if (source.Value < 0)
			{
				if (LogTroubleshootingInfo)
				{
					errors = errors ?? new HashSet<string>();
					errors.Add("floor pattern '" + patternId + "' doesn't match any known floor set");
				}
				continue;
			}
			string tilesheetId = source.Key;
			int spriteIndex = source.Value;
			int tilesWide = map.RequireTileSheet(tilesheetId).SheetWidth;
			spriteIndex = spriteIndex * 2 + spriteIndex / (tilesWide / 2) * tilesWide;
			if (tilesheetId == "walls_and_floors")
			{
				spriteIndex += GetFirstFlooringTile();
			}
			if (!IsFloorableOrWallpaperableTile(x, y, "Back", out var reason))
			{
				if (LogTroubleshootingInfo)
				{
					errors = errors ?? new HashSet<string>();
					errors.Add(reason);
				}
			}
			else
			{
				setMapTile(x, y, GetFlooringIndex(spriteIndex, x, y), "Back", tilesheetId);
				appliedAny = true;
			}
		}
		if (!appliedAny && errors != null && errors.Count > 0)
		{
			Game1.log.Warn($"Couldn't apply floors for area ID '{floorId}' ({string.Join("; ", errors)})");
		}
	}

	public virtual void UpdateWallpaper(string wallpaperId)
	{
		updateMap();
		if (!appliedWallpaper.TryGetValue(wallpaperId, out var patternId) || !wallpaperTiles.TryGetValue(wallpaperId, out var tiles))
		{
			return;
		}
		bool appliedAny = false;
		HashSet<string> errors = null;
		foreach (Vector3 item in tiles)
		{
			int x = (int)item.X;
			int y = (int)item.Y;
			int type = (int)item.Z;
			KeyValuePair<string, int> source = GetWallpaperSource(patternId);
			if (source.Value < 0)
			{
				if (LogTroubleshootingInfo)
				{
					errors = errors ?? new HashSet<string>();
					errors.Add("wallpaper pattern '" + patternId + "' doesn't match any known wallpaper set");
				}
				continue;
			}
			string tileSheetId = source.Key;
			int spriteIndex = source.Value;
			TileSheet tilesheet = map.RequireTileSheet(tileSheetId);
			int tilesWide = tilesheet.SheetWidth;
			string reasonInvalid;
			string layer = ((type == 2 && IsFloorableOrWallpaperableTile(x, y, "Buildings", out reasonInvalid)) ? "Buildings" : "Back");
			if (!IsFloorableOrWallpaperableTile(x, y, layer, out var reason))
			{
				if (LogTroubleshootingInfo)
				{
					errors = errors ?? new HashSet<string>();
					errors.Add(reason);
				}
			}
			else
			{
				setMapTile(x, y, spriteIndex / tilesWide * tilesWide * 3 + spriteIndex % tilesWide + type * tilesWide, layer, tilesheet.Id);
				appliedAny = true;
			}
		}
		if (!appliedAny && errors != null && errors.Count > 0)
		{
			Game1.log.Warn($"Couldn't apply wallpaper for area ID '{wallpaperId}' ({string.Join("; ", errors)})");
		}
	}

	public override void UpdateWhenCurrentLocation(GameTime time)
	{
		if (!wasUpdated)
		{
			base.UpdateWhenCurrentLocation(time);
		}
	}

	public override void MakeMapModifications(bool force = false)
	{
		base.MakeMapModifications(force);
		if (!(this is FarmHouse))
		{
			ReadWallpaperAndFloorTileData();
			setWallpapers();
			setFloors();
		}
		if (hasTileAt(Game1.player.TilePoint, "Buildings"))
		{
			Game1.player.position.Y += 64f;
		}
	}

	protected override void resetLocalState()
	{
		base.resetLocalState();
		if (Game1.player.mailReceived.Add("button_tut_1"))
		{
			Game1.onScreenMenus.Add(new ButtonTutorialMenu(0));
		}
	}

	public override bool CanFreePlaceFurniture()
	{
		return true;
	}

	public virtual bool isTileOnWall(int x, int y)
	{
		foreach (string id in wallpaperTiles.Keys)
		{
			foreach (Vector3 tile_data in wallpaperTiles[id])
			{
				if ((int)tile_data.X == x && (int)tile_data.Y == y)
				{
					return true;
				}
			}
		}
		return false;
	}

	public int GetWallTopY(int x, int y)
	{
		foreach (string id in wallpaperTiles.Keys)
		{
			foreach (Vector3 tile_data in wallpaperTiles[id])
			{
				if ((int)tile_data.X == x && (int)tile_data.Y == y)
				{
					return y - (int)tile_data.Z;
				}
			}
		}
		return -1;
	}

	public virtual void setFloors()
	{
		foreach (KeyValuePair<string, string> pair in appliedFloor.Pairs)
		{
			UpdateFloor(pair.Key);
		}
	}

	public virtual void setWallpapers()
	{
		foreach (KeyValuePair<string, string> pair in appliedWallpaper.Pairs)
		{
			UpdateWallpaper(pair.Key);
		}
	}

	public void SetFloor(string which, string which_room)
	{
		if (which_room == null)
		{
			foreach (string key in floorIDs)
			{
				appliedFloor[key] = which;
			}
			return;
		}
		appliedFloor[which_room] = which;
	}

	public void SetWallpaper(string which, string which_room)
	{
		if (which_room == null)
		{
			foreach (string key in wallpaperIDs)
			{
				appliedWallpaper[key] = which;
			}
			return;
		}
		appliedWallpaper[which_room] = which;
	}

	public void OverrideSpecificWallpaper(string which, string which_room, string wallpaperStyleToOverride)
	{
		if (which_room == null)
		{
			foreach (string key in wallpaperIDs)
			{
				if (appliedWallpaper.TryGetValue(key, out var prevStyle) && prevStyle == wallpaperStyleToOverride)
				{
					appliedWallpaper[key] = which;
				}
			}
			return;
		}
		if (appliedWallpaper[which_room] == wallpaperStyleToOverride)
		{
			appliedWallpaper[which_room] = which;
		}
	}

	public void OverrideSpecificFlooring(string which, string which_room, string flooringStyleToOverride)
	{
		if (which_room == null)
		{
			foreach (string key in floorIDs)
			{
				if (appliedFloor.TryGetValue(key, out var prevStyle) && prevStyle == flooringStyleToOverride)
				{
					appliedFloor[key] = which;
				}
			}
			return;
		}
		if (appliedFloor[which_room] == flooringStyleToOverride)
		{
			appliedFloor[which_room] = which;
		}
	}

	public string GetFloorID(int x, int y)
	{
		foreach (string id in floorTiles.Keys)
		{
			foreach (Vector3 tile_data in floorTiles[id])
			{
				if ((int)tile_data.X == x && (int)tile_data.Y == y)
				{
					return id;
				}
			}
		}
		return null;
	}

	public string GetWallpaperID(int x, int y)
	{
		foreach (string id in wallpaperTiles.Keys)
		{
			foreach (Vector3 tile_data in wallpaperTiles[id])
			{
				if ((int)tile_data.X == x && (int)tile_data.Y == y)
				{
					return id;
				}
			}
		}
		return null;
	}

	protected bool IsFloorableTile(int x, int y, string layer_name)
	{
		int tileIndex = getTileIndexAt(x, y, "Buildings", "untitled tile sheet");
		if (tileIndex >= 197 && tileIndex <= 199)
		{
			return false;
		}
		return IsFloorableOrWallpaperableTile(x, y, layer_name);
	}

	public bool IsWallAndFloorTilesheet(string tilesheet_id)
	{
		if (!(tilesheet_id == "walls_and_floors") && !tilesheet_id.Contains("walls_and_floors"))
		{
			return tilesheet_id.StartsWith("x_WallsAndFloors_");
		}
		return true;
	}

	protected bool IsFloorableOrWallpaperableTile(int x, int y, string layerName)
	{
		string reasonInvalid;
		return IsFloorableOrWallpaperableTile(x, y, layerName, out reasonInvalid);
	}

	protected bool IsFloorableOrWallpaperableTile(int x, int y, string layerName, out string reasonInvalid)
	{
		Layer layer = map.GetLayer(layerName);
		if (layer == null)
		{
			reasonInvalid = "layer '" + layerName + "' not found";
			return false;
		}
		if (x < 0 || x >= layer.LayerWidth || y < 0 || y >= layer.LayerHeight)
		{
			reasonInvalid = $"tile ({x}, {y}) is out of bounds for the layer";
			return false;
		}
		Tile tile = layer.Tiles[x, y];
		if (tile == null)
		{
			reasonInvalid = $"tile ({x}, {y}) not found";
			return false;
		}
		TileSheet tilesheet = tile.TileSheet;
		if (tilesheet == null)
		{
			reasonInvalid = $"tile ({x}, {y}) has unknown tilesheet";
			return false;
		}
		if (!IsWallAndFloorTilesheet(tilesheet.Id))
		{
			reasonInvalid = "tilesheet '" + tilesheet.Id + "' isn't a wall and floor tilesheet, expected tilesheet ID containing 'walls_and_floors' or starting with 'x_WallsAndFloors_'";
			return false;
		}
		reasonInvalid = null;
		return true;
	}

	public override void TransferDataFromSavedLocation(GameLocation l)
	{
		if (l is DecoratableLocation decoratable_location)
		{
			if (!decoratable_location.appliedWallpaper.Keys.Any() && !decoratable_location.appliedFloor.Keys.Any())
			{
				ReadWallpaperAndFloorTileData();
				for (int i = 0; i < decoratable_location.wallPaper.Count; i++)
				{
					try
					{
						string key = wallpaperIDs[i];
						string value = decoratable_location.wallPaper[i].ToString();
						appliedWallpaper[key] = value;
					}
					catch (Exception)
					{
					}
				}
				for (int j = 0; j < decoratable_location.floor.Count; j++)
				{
					try
					{
						string key2 = floorIDs[j];
						string value2 = decoratable_location.floor[j].ToString();
						appliedFloor[key2] = value2;
					}
					catch (Exception)
					{
					}
				}
			}
			else
			{
				foreach (string key3 in decoratable_location.appliedWallpaper.Keys)
				{
					appliedWallpaper[key3] = decoratable_location.appliedWallpaper[key3];
				}
				foreach (string key4 in decoratable_location.appliedFloor.Keys)
				{
					appliedFloor[key4] = decoratable_location.appliedFloor[key4];
				}
			}
		}
		setWallpapers();
		setFloors();
		base.TransferDataFromSavedLocation(l);
	}

	public Furniture getRandomFurniture(Random r)
	{
		return r.ChooseFrom(furniture);
	}

	public virtual string getFloorRoomIdAt(Point p)
	{
		foreach (string key in floorTiles.Keys)
		{
			foreach (Vector3 tile_data in floorTiles[key])
			{
				if ((int)tile_data.X == p.X && (int)tile_data.Y == p.Y)
				{
					return key;
				}
			}
		}
		return null;
	}

	public virtual int GetFirstFlooringTile()
	{
		return 336;
	}

	public virtual int GetFlooringIndex(int base_tile_sheet, int tile_x, int tile_y)
	{
		if (!hasTileAt(tile_x, tile_y, "Back"))
		{
			return 0;
		}
		string tilesheet_name = getTileSheetIDAt(tile_x, tile_y, "Back");
		TileSheet tilesheet = map.GetTileSheet(tilesheet_name);
		int tiles_wide = 16;
		if (tilesheet != null)
		{
			tiles_wide = tilesheet.SheetWidth;
		}
		int x_offset = tile_x % 2;
		int y_offset = tile_y % 2;
		return base_tile_sheet + x_offset + tiles_wide * y_offset;
	}

	public virtual List<Microsoft.Xna.Framework.Rectangle> getFloors()
	{
		return new List<Microsoft.Xna.Framework.Rectangle>();
	}
}
