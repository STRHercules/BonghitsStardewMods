using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Locations;

namespace StardewValley.Menus;

public class InventoryMenu : IClickableMenu
{
	public delegate bool highlightThisItem(Item i);

	public enum BorderSide
	{
		Top,
		Left,
		Right,
		Bottom
	}

	public const int region_inventorySlot0 = 0;

	public const int region_inventorySlot1 = 1;

	public const int region_inventorySlot2 = 2;

	public const int region_inventorySlot3 = 3;

	public const int region_inventorySlot4 = 4;

	public const int region_inventorySlot5 = 5;

	public const int region_inventorySlot6 = 6;

	public const int region_inventorySlot7 = 7;

	public const int region_dropButton = 107;

	public const int region_inventoryArea = 9000;

	public string hoverText = "";

	public string hoverTitle = "";

	public string descriptionTitle = "";

	public string descriptionText = "";

	public List<ClickableComponent> inventory = new List<ClickableComponent>();

	protected Dictionary<int, double> _iconShakeTimer = new Dictionary<int, double>();

	public IList<Item> actualInventory;

	public highlightThisItem highlightMethod;

	public ItemGrabMenu.behaviorOnItemSelect onAddItem;

	public bool playerInventory;

	public bool drawSlots;

	public bool showGrayedOutSlots;

	public int capacity;

	public int rows;

	public int horizontalGap;

	public int verticalGap;

	public ClickableComponent dropItemInvisibleButton;

	public string moveItemSound = "dwop";

	public InventoryMenu(int xPosition, int yPosition, bool playerInventory, IList<Item> actualInventory = null, highlightThisItem highlightMethod = null, int capacity = -1, int rows = 3, int horizontalGap = 0, int verticalGap = 0, bool drawSlots = true)
		: base(xPosition, yPosition, 64 * (((capacity == -1) ? 36 : capacity) / rows), 64 * rows + 16)
	{
		this.drawSlots = drawSlots;
		this.horizontalGap = horizontalGap;
		this.verticalGap = verticalGap;
		this.rows = rows;
		this.capacity = ((capacity == -1) ? 36 : capacity);
		this.playerInventory = playerInventory;
		this.actualInventory = actualInventory;
		if (actualInventory == null)
		{
			this.actualInventory = Game1.player.Items;
		}
		for (int i = 0; i < Game1.player.maxItems.Value; i++)
		{
			if (Game1.player.Items.Count <= i)
			{
				Game1.player.Items.Add(null);
			}
		}
		for (int j = 0; j < this.capacity; j++)
		{
			int downNeighbor = 0;
			downNeighbor = ((!playerInventory) ? ((j >= this.capacity - this.capacity / rows) ? (-99998) : (j + this.capacity / rows)) : ((j < this.actualInventory.Count - this.capacity / rows) ? (j + this.capacity / rows) : ((j < this.actualInventory.Count - 3 && this.actualInventory.Count >= 36) ? (-99998) : ((j % 12 < 2) ? 102 : 101))));
			inventory.Add(new ClickableComponent(new Rectangle(xPosition + j % (this.capacity / rows) * 64 + horizontalGap * (j % (this.capacity / rows)), yPositionOnScreen + j / (this.capacity / rows) * (64 + verticalGap) + (j / (this.capacity / rows) - 1) * 4 - ((j <= this.capacity / rows && playerInventory && verticalGap == 0) ? 12 : 0), 64, 64), j.ToString() ?? "")
			{
				myID = j,
				leftNeighborID = ((j % (this.capacity / rows) != 0) ? (j - 1) : 107),
				rightNeighborID = (((j + 1) % (this.capacity / rows) != 0) ? (j + 1) : 106),
				downNeighborID = downNeighbor,
				upNeighborID = ((j < this.capacity / rows) ? (12340 + j) : (j - this.capacity / rows)),
				region = 9000,
				upNeighborImmutable = true,
				downNeighborImmutable = true,
				leftNeighborImmutable = true,
				rightNeighborImmutable = true
			});
		}
		this.highlightMethod = highlightMethod;
		if (highlightMethod == null)
		{
			this.highlightMethod = highlightAllItems;
		}
		dropItemInvisibleButton = new ClickableComponent(new Rectangle(xPosition - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 128, yPositionOnScreen - 12, 64, 64), "")
		{
			myID = (playerInventory ? 107 : (-500)),
			rightNeighborID = 0
		};
		foreach (ClickableComponent item in GetBorder(BorderSide.Top))
		{
			item.upNeighborImmutable = false;
		}
		foreach (ClickableComponent item2 in GetBorder(BorderSide.Bottom))
		{
			item2.downNeighborImmutable = false;
		}
		foreach (ClickableComponent item3 in GetBorder(BorderSide.Left))
		{
			item3.leftNeighborImmutable = false;
		}
		foreach (ClickableComponent item4 in GetBorder(BorderSide.Right))
		{
			item4.rightNeighborImmutable = false;
		}
	}

	public List<ClickableComponent> GetBorder(BorderSide side)
	{
		List<ClickableComponent> inventory_slots = new List<ClickableComponent>();
		int row_size = capacity / rows;
		switch (side)
		{
		case BorderSide.Bottom:
		{
			for (int l = 0; l < inventory.Count; l++)
			{
				if (l >= actualInventory.Count - row_size)
				{
					inventory_slots.Add(inventory[l]);
				}
			}
			break;
		}
		case BorderSide.Top:
		{
			for (int j = 0; j < inventory.Count; j++)
			{
				if (j < row_size)
				{
					inventory_slots.Add(inventory[j]);
				}
			}
			break;
		}
		case BorderSide.Left:
		{
			for (int k = 0; k < inventory.Count; k++)
			{
				if (k % row_size == 0)
				{
					inventory_slots.Add(inventory[k]);
				}
			}
			break;
		}
		case BorderSide.Right:
		{
			for (int i = 0; i < inventory.Count; i++)
			{
				if (i % row_size == row_size - 1)
				{
					inventory_slots.Add(inventory[i]);
				}
			}
			break;
		}
		}
		return inventory_slots;
	}

	public static bool highlightAllItems(Item i)
	{
		return true;
	}

	public static bool highlightNoItems(Item i)
	{
		return false;
	}

	public void SetPosition(int x, int y)
	{
		movePosition(-xPositionOnScreen, -yPositionOnScreen);
		movePosition(x, y);
	}

	public void movePosition(int x, int y)
	{
		xPositionOnScreen += x;
		yPositionOnScreen += y;
		foreach (ClickableComponent item in inventory)
		{
			item.bounds.X += x;
			item.bounds.Y += y;
		}
		dropItemInvisibleButton.bounds.X += x;
		dropItemInvisibleButton.bounds.Y += y;
	}

	public void ShakeItem(Item item)
	{
		ShakeItem(actualInventory.IndexOf(item));
	}

	public void ShakeItem(int index)
	{
		if (index >= 0 && index < inventory.Count)
		{
			_iconShakeTimer[index] = Game1.currentGameTime.TotalGameTime.TotalSeconds + 0.5;
		}
	}

	public Item tryToAddItem(Item toPlace, string sound = "coin")
	{
		if (toPlace == null)
		{
			return null;
		}
		int originalStack = toPlace.Stack;
		foreach (ClickableComponent item in inventory)
		{
			int slotNumber = Convert.ToInt32(item.name);
			Item slot = ((slotNumber < actualInventory.Count) ? actualInventory[slotNumber] : null);
			if (slot == null || !highlightMethod(slot) || !slot.canStackWith(toPlace))
			{
				continue;
			}
			int toRemove = toPlace.Stack - slot.addToStack(toPlace);
			if (toPlace.ConsumeStack(toRemove) == null)
			{
				try
				{
					Game1.playSound(sound, null);
					onAddItem?.Invoke(toPlace, playerInventory ? Game1.player : null);
				}
				catch (Exception)
				{
				}
				return null;
			}
		}
		foreach (ClickableComponent item2 in inventory)
		{
			int slotNumber2 = Convert.ToInt32(item2.name);
			Item slot2 = ((slotNumber2 < actualInventory.Count) ? actualInventory[slotNumber2] : null);
			if (slotNumber2 >= actualInventory.Count || slot2 != null)
			{
				continue;
			}
			if (!string.IsNullOrEmpty(sound))
			{
				try
				{
					Game1.playSound(sound, null);
				}
				catch (Exception)
				{
				}
			}
			return Utility.addItemToInventory(toPlace, slotNumber2, actualInventory, onAddItem);
		}
		if (toPlace.Stack < originalStack)
		{
			Game1.playSound(sound, null);
		}
		return toPlace;
	}

	public int getInventoryPositionOfClick(int x, int y)
	{
		for (int i = 0; i < inventory.Count; i++)
		{
			if (inventory[i] != null && inventory[i].bounds.Contains(x, y))
			{
				return Convert.ToInt32(inventory[i].name);
			}
		}
		return -1;
	}

	public Item leftClick(int x, int y, Item toPlace, bool playSound = true)
	{
		foreach (ClickableComponent c in inventory)
		{
			if (!c.containsPoint(x, y))
			{
				continue;
			}
			int slotNumber = Convert.ToInt32(c.name);
			if (slotNumber >= actualInventory.Count || (actualInventory[slotNumber] != null && !highlightMethod(actualInventory[slotNumber]) && !actualInventory[slotNumber].canStackWith(toPlace)))
			{
				continue;
			}
			if (actualInventory[slotNumber] != null)
			{
				if (toPlace != null)
				{
					if (playSound)
					{
						Game1.playSound("stoneStep", null);
					}
					return Utility.addItemToInventory(toPlace, slotNumber, actualInventory, onAddItem);
				}
				if (playSound)
				{
					Game1.playSound(moveItemSound, null);
				}
				return Utility.removeItemFromInventory(slotNumber, actualInventory);
			}
			if (toPlace != null)
			{
				if (playSound)
				{
					Game1.playSound("stoneStep", null);
				}
				return Utility.addItemToInventory(toPlace, slotNumber, actualInventory, onAddItem);
			}
		}
		return toPlace;
	}

	public Vector2 snapToClickableComponent(int x, int y)
	{
		foreach (ClickableComponent c in inventory)
		{
			if (c.containsPoint(x, y))
			{
				return new Vector2(c.bounds.X, c.bounds.Y);
			}
		}
		return new Vector2(x, y);
	}

	public Item getItemAt(int x, int y)
	{
		foreach (ClickableComponent c in inventory)
		{
			if (c.containsPoint(x, y))
			{
				return getItemFromClickableComponent(c);
			}
		}
		return null;
	}

	public Item getItemFromClickableComponent(ClickableComponent c)
	{
		if (c != null)
		{
			int slotNumber = Convert.ToInt32(c.name);
			if (slotNumber < actualInventory.Count)
			{
				return actualInventory[slotNumber];
			}
		}
		return null;
	}

	public Item rightClick(int x, int y, Item toAddTo, bool playSound = true, bool onlyCheckToolAttachments = false)
	{
		foreach (ClickableComponent item in inventory)
		{
			int slotNumber = Convert.ToInt32(item.name);
			Item slot = ((slotNumber < actualInventory.Count) ? actualInventory[slotNumber] : null);
			if (!item.containsPoint(x, y) || slotNumber >= actualInventory.Count || (slot != null && !highlightMethod(slot)) || slot == null)
			{
				continue;
			}
			if (slot is Tool tool && (toAddTo == null || toAddTo is Object) && tool.canThisBeAttached((Object)toAddTo))
			{
				return tool.attach((Object)toAddTo);
			}
			if (onlyCheckToolAttachments)
			{
				return toAddTo;
			}
			if (toAddTo == null)
			{
				if (slot.maximumStackSize() != -1)
				{
					if (slotNumber == Game1.player.CurrentToolIndex && slot.Stack == 1)
					{
						slot.actionWhenStopBeingHeld(Game1.player);
					}
					Item newItem = slot.getOne();
					newItem.Stack = ((slot.Stack <= 1 || !Game1.isOneOfTheseKeysDown(Game1.oldKBState, new InputButton[1]
					{
						new InputButton(Keys.LeftShift)
					})) ? 1 : ((int)Math.Ceiling((double)slot.Stack / 2.0)));
					actualInventory[slotNumber] = slot.ConsumeStack(newItem.Stack);
					if (playSound)
					{
						Game1.playSound(moveItemSound, null);
					}
					return newItem;
				}
			}
			else if (slot.canStackWith(toAddTo) && toAddTo.Stack < toAddTo.maximumStackSize())
			{
				if (Game1.isOneOfTheseKeysDown(Game1.oldKBState, new InputButton[1]
				{
					new InputButton(Keys.LeftShift)
				}))
				{
					int amountToAdd = (int)Math.Ceiling((double)slot.Stack / 2.0);
					amountToAdd = Math.Min(toAddTo.maximumStackSize() - toAddTo.Stack, amountToAdd);
					toAddTo.Stack += amountToAdd;
					actualInventory[slotNumber] = slot.ConsumeStack(amountToAdd);
				}
				else
				{
					toAddTo.Stack++;
					actualInventory[slotNumber] = slot.ConsumeStack(1);
				}
				if (playSound)
				{
					Game1.playSound(moveItemSound, null);
				}
				if (actualInventory[slotNumber] == null && slotNumber == Game1.player.CurrentToolIndex)
				{
					slot.actionWhenStopBeingHeld(Game1.player);
				}
				return toAddTo;
			}
		}
		return toAddTo;
	}

	public Item hover(int x, int y, Item heldItem)
	{
		descriptionText = "";
		descriptionTitle = "";
		hoverText = "";
		hoverTitle = "";
		Item toReturn = null;
		foreach (ClickableComponent c in inventory)
		{
			int slotNumber = Convert.ToInt32(c.name);
			c.scale = Math.Max(1f, c.scale - 0.025f);
			if (c.containsPoint(x, y) && slotNumber < actualInventory.Count && (actualInventory[slotNumber] == null || highlightMethod(actualInventory[slotNumber])) && slotNumber < actualInventory.Count && actualInventory[slotNumber] != null)
			{
				descriptionTitle = actualInventory[slotNumber].DisplayName;
				descriptionText = Environment.NewLine + actualInventory[slotNumber].getDescription();
				c.scale = Math.Min(c.scale + 0.05f, 1.1f);
				string s = actualInventory[slotNumber].getHoverBoxText(heldItem);
				if (s != null)
				{
					hoverText = s;
					hoverTitle = actualInventory[slotNumber].DisplayName;
				}
				else
				{
					hoverText = actualInventory[slotNumber].getDescription();
					hoverTitle = actualInventory[slotNumber].DisplayName;
				}
				if (toReturn == null)
				{
					toReturn = actualInventory[slotNumber];
				}
			}
		}
		if (toReturn is Object returnObj && Game1.RequireLocation<CommunityCenter>("CommunityCenter").couldThisIngredienteBeUsedInABundle(returnObj))
		{
			GameMenu.bundleItemHovered = true;
		}
		return toReturn;
	}

	public override void setUpForGamePadMode()
	{
		base.setUpForGamePadMode();
		List<ClickableComponent> list = inventory;
		if (list != null && list.Count > 0)
		{
			Game1.setMousePosition(inventory[0].bounds.Right - inventory[0].bounds.Width / 8, inventory[0].bounds.Bottom - inventory[0].bounds.Height / 8);
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		draw(b, -1, -1, -1);
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b, int red, int green, int blue)
	{
		for (int i = 0; i < inventory.Count; i++)
		{
			if (_iconShakeTimer.TryGetValue(i, out var endTime) && Game1.currentGameTime.TotalGameTime.TotalSeconds >= endTime)
			{
				_iconShakeTimer.Remove(i);
			}
		}
		Color tint = ((red == -1) ? Color.White : new Color((int)Utility.Lerp(red, Math.Min(255, red + 150), 0.65f), (int)Utility.Lerp(green, Math.Min(255, green + 150), 0.65f), (int)Utility.Lerp(blue, Math.Min(255, blue + 150), 0.65f)));
		Texture2D texture = ((red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture);
		if (drawSlots)
		{
			for (int j = 0; j < capacity; j++)
			{
				Vector2 toDraw = new Vector2(xPositionOnScreen + j % (capacity / rows) * 64 + horizontalGap * (j % (capacity / rows)), yPositionOnScreen + j / (capacity / rows) * (64 + verticalGap) + (j / (capacity / rows) - 1) * 4 - ((j < capacity / rows && playerInventory && verticalGap == 0) ? 12 : 0));
				b.Draw(texture, toDraw, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), tint, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);
				if ((playerInventory || showGrayedOutSlots) && j >= Game1.player.maxItems.Value)
				{
					b.Draw(texture, toDraw, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 57), tint * 0.5f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);
				}
				if (!Game1.options.gamepadControls && j < 12 && playerInventory)
				{
					string strToDraw = j switch
					{
						11 => "=", 
						10 => "-", 
						9 => "0", 
						_ => (j + 1).ToString() ?? "", 
					};
					Vector2 strSize = Game1.tinyFont.MeasureString(strToDraw);
					b.DrawString(Game1.tinyFont, strToDraw, toDraw + new Vector2(32f - strSize.X / 2f, 0f - strSize.Y), (j == Game1.player.CurrentToolIndex) ? Color.Red : Color.DimGray);
				}
			}
			for (int k = 0; k < capacity; k++)
			{
				Vector2 toDraw2 = new Vector2(xPositionOnScreen + k % (capacity / rows) * 64 + horizontalGap * (k % (capacity / rows)), yPositionOnScreen + k / (capacity / rows) * (64 + verticalGap) + (k / (capacity / rows) - 1) * 4 - ((k < capacity / rows && playerInventory && verticalGap == 0) ? 12 : 0));
				if (actualInventory.Count > k && actualInventory[k] != null)
				{
					bool highlight = highlightMethod(actualInventory[k]);
					if (_iconShakeTimer.ContainsKey(k))
					{
						toDraw2 += 1f * new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
					}
					actualInventory[k].drawInMenu(b, toDraw2, (inventory.Count > k) ? inventory[k].scale : 1f, (!highlightMethod(actualInventory[k])) ? 0.25f : 1f, 0.865f, StackDrawType.Draw, Color.White, highlight);
				}
			}
			return;
		}
		for (int l = 0; l < capacity; l++)
		{
			Vector2 toDraw3 = new Vector2(xPositionOnScreen + l % (capacity / rows) * 64 + horizontalGap * (l % (capacity / rows)), yPositionOnScreen + l / (capacity / rows) * (64 + verticalGap) + (l / (capacity / rows) - 1) * 4 - ((l < capacity / rows && playerInventory && verticalGap == 0) ? 12 : 0));
			if (actualInventory.Count > l && actualInventory[l] != null)
			{
				bool highlight2 = highlightMethod(actualInventory[l]);
				if (_iconShakeTimer.ContainsKey(l))
				{
					toDraw3 += 1f * new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
				}
				actualInventory[l].drawInMenu(b, toDraw3, (inventory.Count > l) ? inventory[l].scale : 1f, (!highlight2) ? 0.25f : 1f, 0.865f, StackDrawType.Draw, Color.White, highlight2);
			}
		}
	}

	public List<Vector2> GetSlotDrawPositions()
	{
		List<Vector2> slot_draw_positions = new List<Vector2>();
		for (int i = 0; i < capacity; i++)
		{
			slot_draw_positions.Add(new Vector2(xPositionOnScreen + i % (capacity / rows) * 64 + horizontalGap * (i % (capacity / rows)), yPositionOnScreen + i / (capacity / rows) * (64 + verticalGap) + (i / (capacity / rows) - 1) * 4 - ((i < capacity / rows && playerInventory && verticalGap == 0) ? 12 : 0)));
		}
		return slot_draw_positions;
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
	}
}
