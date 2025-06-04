using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Objects;

namespace StardewValley.Menus;

public class DyeMenu : MenuWithInventory
{
	protected int _timeUntilCraft;

	public List<ClickableTextureComponent> dyePots;

	public ClickableTextureComponent dyeButton;

	public const int DYE_POT_ID_OFFSET = 5000;

	public Texture2D dyeTexture;

	protected Dictionary<Item, int> _highlightDictionary;

	protected List<Vector2> _slotDrawPositions;

	protected int _hoveredPotIndex = -1;

	protected int[] _dyeDropAnimationFrames;

	public const int MILLISECONDS_PER_DROP_FRAME = 50;

	public const int TOTAL_DROP_FRAMES = 10;

	public string[][] validPotColors = new string[6][]
	{
		new string[4] { "color_red", "color_salmon", "color_dark_red", "color_pink" },
		new string[5] { "color_orange", "color_dark_orange", "color_dark_brown", "color_brown", "color_copper" },
		new string[4] { "color_yellow", "color_dark_yellow", "color_gold", "color_sand" },
		new string[5] { "color_green", "color_dark_green", "color_lime", "color_yellow_green", "color_jade" },
		new string[6] { "color_blue", "color_dark_blue", "color_dark_cyan", "color_light_cyan", "color_cyan", "color_aquamarine" },
		new string[6] { "color_purple", "color_dark_purple", "color_dark_pink", "color_pale_violet_red", "color_poppyseed", "color_iridium" }
	};

	protected string displayedDescription = "";

	public List<ClickableTextureComponent> dyedClothesDisplays;

	protected Vector2 _dyedClothesDisplayPosition;

	public DyeMenu()
		: base(null, okButton: true, trashCan: true, 12, 132)
	{
		if (yPositionOnScreen == IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder)
		{
			movePosition(0, -IClickableMenu.spaceToClearTopBorder);
		}
		Game1.playSound("bigSelect", null);
		inventory.highlightMethod = HighlightItems;
		dyeTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\dye_bench");
		dyedClothesDisplays = new List<ClickableTextureComponent>();
		_CreateButtons();
		if (trashCan != null)
		{
			trashCan.myID = 106;
		}
		if (okButton != null)
		{
			okButton.leftNeighborID = 11;
		}
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
		GenerateHighlightDictionary();
		_UpdateDescriptionText();
	}

	protected void _CreateButtons()
	{
		_slotDrawPositions = inventory.GetSlotDrawPositions();
		Dictionary<int, Item> old_items = new Dictionary<int, Item>();
		if (dyePots != null)
		{
			for (int i = 0; i < dyePots.Count; i++)
			{
				old_items[i] = dyePots[i].item;
			}
		}
		dyePots = new List<ClickableTextureComponent>();
		for (int j = 0; j < validPotColors.Length; j++)
		{
			ClickableTextureComponent dye_pot = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth / 2 - 4 + 68 + 18 * j * 4, yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 132, 64, 64), dyeTexture, new Rectangle(32 + 16 * j, 80, 16, 16), 4f)
			{
				myID = j + 5000,
				downNeighborID = -99998,
				leftNeighborID = -99998,
				rightNeighborID = -99998,
				upNeighborID = -99998,
				item = old_items.GetValueOrDefault(j)
			};
			dyePots.Add(dye_pot);
		}
		_dyeDropAnimationFrames = new int[dyePots.Count];
		for (int k = 0; k < _dyeDropAnimationFrames.Length; k++)
		{
			_dyeDropAnimationFrames[k] = -1;
		}
		dyeButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth / 2 + 4 + 448, yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 8 + 200, 96, 96), dyeTexture, new Rectangle(0, 80, 24, 24), 4f)
		{
			myID = 1000,
			downNeighborID = -99998,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			upNeighborID = -99998,
			item = ((dyeButton != null) ? dyeButton.item : null)
		};
		List<ClickableComponent> list = inventory.inventory;
		if (list != null && list.Count >= 12)
		{
			for (int l = 0; l < 12; l++)
			{
				if (inventory.inventory[l] != null)
				{
					inventory.inventory[l].upNeighborID = -99998;
				}
			}
		}
		dyedClothesDisplays.Clear();
		_dyedClothesDisplayPosition = new Vector2(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth / 2 + 4 + 692, yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 8 + 232);
		Vector2 dyed_items_position = _dyedClothesDisplayPosition;
		int drawn_items_count = 0;
		if (Game1.player.CanDyeShirt())
		{
			drawn_items_count++;
		}
		if (Game1.player.CanDyePants())
		{
			drawn_items_count++;
		}
		dyed_items_position.X -= drawn_items_count * 64 / 2;
		if (Game1.player.CanDyeShirt())
		{
			ClickableTextureComponent component = new ClickableTextureComponent(new Rectangle((int)dyed_items_position.X, (int)dyed_items_position.Y, 64, 64), null, new Rectangle(0, 0, 64, 64), 4f);
			component.item = Game1.player.shirtItem.Value;
			dyed_items_position.X += 64f;
			dyedClothesDisplays.Add(component);
		}
		if (Game1.player.CanDyePants())
		{
			ClickableTextureComponent component2 = new ClickableTextureComponent(new Rectangle((int)dyed_items_position.X, (int)dyed_items_position.Y, 64, 64), null, new Rectangle(0, 0, 64, 64), 4f);
			component2.item = Game1.player.pantsItem.Value;
			dyed_items_position.X += 64f;
			dyedClothesDisplays.Add(component2);
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(0);
		snapCursorToCurrentSnappedComponent();
	}

	public bool IsBusy()
	{
		return _timeUntilCraft > 0;
	}

	public override bool readyToClose()
	{
		if (base.readyToClose() && base.heldItem == null)
		{
			return !IsBusy();
		}
		return false;
	}

	public bool HighlightItems(Item i)
	{
		if (i == null)
		{
			return false;
		}
		if (i != null && !i.canBeTrashed())
		{
			return false;
		}
		if (_highlightDictionary == null)
		{
			GenerateHighlightDictionary();
		}
		if (!_highlightDictionary.ContainsKey(i))
		{
			_highlightDictionary = null;
			GenerateHighlightDictionary();
		}
		if (_hoveredPotIndex >= 0)
		{
			return _hoveredPotIndex == _highlightDictionary[i];
		}
		if (_highlightDictionary[i] >= 0)
		{
			return dyePots[_highlightDictionary[i]].item == null;
		}
		return false;
	}

	public void GenerateHighlightDictionary()
	{
		_highlightDictionary = new Dictionary<Item, int>();
		foreach (Item item in new List<Item>(inventory.actualInventory))
		{
			if (item != null)
			{
				_highlightDictionary[item] = GetPotIndex(item);
			}
		}
	}

	private void _DyePotClicked(ClickableTextureComponent dyePot)
	{
		Item oldItem = dyePot.item;
		int index = dyePots.IndexOf(dyePot);
		if (index < 0)
		{
			return;
		}
		if (base.heldItem == null || (base.heldItem.canBeTrashed() && GetPotIndex(base.heldItem) == index))
		{
			bool removeHeldItem = false;
			if (dyePot.item != null && base.heldItem != null && dyePot.item.canStackWith(base.heldItem))
			{
				base.heldItem.Stack++;
				dyePot.item = null;
				Game1.playSound("quickSlosh", null);
				return;
			}
			dyePot.item = base.heldItem?.getOne();
			if (base.heldItem != null && base.heldItem.ConsumeStack(1) == null)
			{
				removeHeldItem = true;
			}
			if (base.heldItem != null && removeHeldItem)
			{
				base.heldItem = oldItem;
			}
			else if (base.heldItem != null && oldItem != null)
			{
				Item i = Game1.player.addItemToInventory(base.heldItem);
				if (i != null)
				{
					Game1.createItemDebris(i, Game1.player.getStandingPosition(), Game1.player.FacingDirection);
				}
				base.heldItem = oldItem;
			}
			else if (oldItem != null)
			{
				base.heldItem = oldItem;
			}
			else if (base.heldItem != null && oldItem == null && Game1.GetKeyboardState().IsKeyDown(Keys.LeftShift))
			{
				Game1.player.addItemToInventory(base.heldItem);
				base.heldItem = null;
			}
			if (oldItem != dyePot.item)
			{
				_dyeDropAnimationFrames[index] = 0;
				Game1.playSound("quickSlosh", null);
				int count = 0;
				for (int j = 0; j < dyePots.Count; j++)
				{
					if (dyePots[j].item != null)
					{
						count++;
					}
				}
				if (count >= dyePots.Count)
				{
					DelayedAction.playSoundAfterDelay("newArtifact", 200, null, null);
				}
			}
			_highlightDictionary = null;
			GenerateHighlightDictionary();
		}
		_UpdateDescriptionText();
	}

	public Color GetColorForPot(int index)
	{
		return index switch
		{
			0 => new Color(220, 0, 0), 
			1 => new Color(255, 128, 0), 
			2 => new Color(255, 230, 0), 
			3 => new Color(10, 143, 0), 
			4 => new Color(46, 105, 203), 
			5 => new Color(115, 41, 181), 
			_ => Color.Black, 
		};
	}

	public int GetPotIndex(Item item)
	{
		for (int i = 0; i < validPotColors.Length; i++)
		{
			for (int j = 0; j < validPotColors[i].Length; j++)
			{
				if (item is ColoredObject colorObject && colorObject.preservedParentSheetIndex.Value != null && ItemContextTagManager.DoAnyTagsMatch(new List<string> { validPotColors[i][j] }, ItemContextTagManager.GetBaseContextTags(colorObject.preservedParentSheetIndex.Value)))
				{
					return i;
				}
				if (item.HasContextTag(validPotColors[i][j]))
				{
					return i;
				}
			}
		}
		return -1;
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (key == Keys.Delete)
		{
			if (base.heldItem != null && base.heldItem.canBeTrashed())
			{
				Utility.trashItem(base.heldItem);
				base.heldItem = null;
			}
		}
		else
		{
			base.receiveKeyPress(key);
		}
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		Item oldHeldItem = base.heldItem;
		base.receiveLeftClick(x, y, base.heldItem != null || !Game1.GetKeyboardState().IsKeyDown(Keys.LeftShift));
		if (Game1.GetKeyboardState().IsKeyDown(Keys.LeftShift) && oldHeldItem != base.heldItem && base.heldItem != null)
		{
			foreach (ClickableTextureComponent pot in dyePots)
			{
				if (pot.item == null)
				{
					_DyePotClicked(pot);
				}
				if (base.heldItem == null)
				{
					return;
				}
			}
		}
		if (IsBusy())
		{
			return;
		}
		bool wasHeldItem = base.heldItem != null;
		foreach (ClickableTextureComponent pot2 in dyePots)
		{
			if (pot2.containsPoint(x, y))
			{
				_DyePotClicked(pot2);
				if (!wasHeldItem && base.heldItem != null && Game1.GetKeyboardState().IsKeyDown(Keys.LeftShift))
				{
					base.heldItem = Game1.player.addItemToInventory(base.heldItem);
				}
				return;
			}
		}
		if (dyeButton.containsPoint(x, y))
		{
			if (base.heldItem == null && CanDye())
			{
				Game1.playSound("glug", null);
				foreach (ClickableTextureComponent dyePot in dyePots)
				{
					if (dyePot.item != null)
					{
						dyePot.item = dyePot.item.ConsumeStack(1);
					}
				}
				Game1.activeClickableMenu = new CharacterCustomization(CharacterCustomization.Source.DyePots);
				_UpdateDescriptionText();
			}
			else
			{
				Game1.playSound("sell", null);
			}
		}
		if (base.heldItem != null && !isWithinBounds(x, y) && base.heldItem.canBeTrashed())
		{
			Game1.playSound("throwDownITem", null);
			Game1.createItemDebris(base.heldItem, Game1.player.getStandingPosition(), Game1.player.FacingDirection);
			base.heldItem = null;
		}
	}

	public bool CanDye()
	{
		for (int i = 0; i < dyePots.Count; i++)
		{
			if (dyePots[i].item == null)
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsWearingDyeable()
	{
		if (!Game1.player.CanDyeShirt())
		{
			return Game1.player.CanDyePants();
		}
		return true;
	}

	protected void _UpdateDescriptionText()
	{
		if (!IsWearingDyeable())
		{
			displayedDescription = Game1.content.LoadString("Strings\\UI:DyePot_NoDyeable");
		}
		else if (CanDye())
		{
			displayedDescription = Game1.content.LoadString("Strings\\UI:DyePot_CanDye");
		}
		else
		{
			displayedDescription = Game1.content.LoadString("Strings\\UI:DyePot_Help");
		}
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (!IsBusy())
		{
			base.receiveRightClick(x, y, playSound: true);
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		if (x <= dyePots[0].bounds.X || x >= dyePots.Last().bounds.Right || y <= dyePots[0].bounds.Y || y >= dyePots[0].bounds.Bottom)
		{
			_hoveredPotIndex = -1;
		}
		if (IsBusy())
		{
			return;
		}
		hoveredItem = null;
		base.performHoverAction(x, y);
		hoverText = "";
		foreach (ClickableTextureComponent component in dyedClothesDisplays)
		{
			if (component.containsPoint(x, y))
			{
				hoveredItem = component.item;
			}
		}
		for (int i = 0; i < dyePots.Count; i++)
		{
			if (dyePots[i].containsPoint(x, y))
			{
				dyePots[i].tryHover(x, y, 0f);
				_hoveredPotIndex = i;
			}
		}
		if (CanDye())
		{
			dyeButton.tryHover(x, y, 0.2f);
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		int yPositionForInventory = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth + 192 - 16 + 128 + 4;
		inventory = new InventoryMenu(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth / 2 + 12, yPositionForInventory, playerInventory: false, null, inventory.highlightMethod);
		_CreateButtons();
	}

	public override void emergencyShutDown()
	{
		_OnCloseMenu();
		base.emergencyShutDown();
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		descriptionText = displayedDescription;
		if (CanDye())
		{
			dyeButton.sourceRect.Y = 180;
			dyeButton.sourceRect.X = (int)(time.TotalGameTime.TotalMilliseconds % 600.0 / 100.0) * 24;
		}
		else
		{
			dyeButton.sourceRect.Y = 80;
			dyeButton.sourceRect.X = 0;
		}
		for (int i = 0; i < dyePots.Count; i++)
		{
			if (_dyeDropAnimationFrames[i] >= 0)
			{
				_dyeDropAnimationFrames[i] += time.ElapsedGameTime.Milliseconds;
				if (_dyeDropAnimationFrames[i] >= 500)
				{
					_dyeDropAnimationFrames[i] = -1;
				}
			}
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (!Game1.options.showClearBackgrounds)
		{
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);
		}
		base.draw(b, drawUpperPortion: true, drawDescriptionArea: true, 50, 160, 255);
		b.Draw(dyeTexture, new Vector2(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth / 2 - 4, yPositionOnScreen + IClickableMenu.spaceToClearTopBorder), new Rectangle(0, 0, 142, 80), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.87f);
		for (int i = 0; i < _slotDrawPositions.Count; i++)
		{
			if (i < inventory.actualInventory.Count && inventory.actualInventory[i] != null && _highlightDictionary.TryGetValue(inventory.actualInventory[i], out var index) && index >= 0)
			{
				Color color = GetColorForPot(index);
				if (_hoveredPotIndex == -1 && HighlightItems(inventory.actualInventory[i]))
				{
					b.Draw(dyeTexture, _slotDrawPositions[i], new Rectangle(32, 96, 32, 32), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.1f);
				}
			}
		}
		dyeButton.draw(b, Color.White * (CanDye() ? 1f : 0.55f), 0.96f);
		dyeButton.drawItem(b, 16, 16);
		string make_result_text = Game1.content.LoadString("Strings\\UI:DyePot_WillDye");
		Vector2 dyed_items_position = _dyedClothesDisplayPosition;
		Utility.drawTextWithColoredShadow(position: new Vector2(dyed_items_position.X - Game1.smallFont.MeasureString(make_result_text).X / 2f, (float)(int)dyed_items_position.Y - Game1.smallFont.MeasureString(make_result_text).Y), b: b, text: make_result_text, font: Game1.smallFont, color: Game1.textColor * 0.75f, shadowColor: Color.Black * 0.2f);
		foreach (ClickableTextureComponent dyedClothesDisplay in dyedClothesDisplays)
		{
			dyedClothesDisplay.drawItem(b);
		}
		for (int j = 0; j < dyePots.Count; j++)
		{
			dyePots[j].drawItem(b, 0, -16);
			if (_dyeDropAnimationFrames[j] >= 0)
			{
				Color color2 = GetColorForPot(j);
				b.Draw(dyeTexture, new Vector2(dyePots[j].bounds.X, dyePots[j].bounds.Y - 12), new Rectangle(_dyeDropAnimationFrames[j] / 50 * 16, 128, 16, 16), color2, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.87f);
			}
			dyePots[j].draw(b);
		}
		if (!hoverText.Equals(""))
		{
			IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont, (base.heldItem != null) ? 32 : 0, (base.heldItem != null) ? 32 : 0, -1, null, -1, null, null, 0, null, -1, -1, -1, 1f, null, null, null, null, null, null);
		}
		else if (hoveredItem != null)
		{
			IClickableMenu.drawToolTip(b, hoveredItem.getDescription(), hoveredItem.DisplayName, hoveredItem, base.heldItem != null);
		}
		base.heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
		if (!Game1.options.hardwareCursor)
		{
			drawMouse(b);
		}
	}

	/// <inheritdoc />
	protected override void cleanupBeforeExit()
	{
		_OnCloseMenu();
	}

	protected void _OnCloseMenu()
	{
		Utility.CollectOrDrop(base.heldItem);
		for (int i = 0; i < dyePots.Count; i++)
		{
			if (dyePots[i].item != null)
			{
				Utility.CollectOrDrop(dyePots[i].item);
			}
		}
		base.heldItem = null;
		dyeButton.item = null;
	}
}
