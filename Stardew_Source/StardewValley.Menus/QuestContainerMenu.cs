using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Menus;

public class QuestContainerMenu : MenuWithInventory
{
	public enum ChangeType
	{
		None,
		Place,
		Grab
	}

	public InventoryMenu ItemsToGrabMenu;

	public Func<Item, int> stackCapacityCheck;

	public Action onItemChanged;

	public Action onConfirm;

	public QuestContainerMenu(IList<Item> inventory, int rows = 3, InventoryMenu.highlightThisItem highlight_method = null, Func<Item, int> stack_capacity_check = null, Action on_item_changed = null, Action on_confirm = null)
		: base(highlight_method, okButton: true)
	{
		onItemChanged = (Action)Delegate.Combine(onItemChanged, on_item_changed);
		onConfirm = (Action)Delegate.Combine(onConfirm, on_confirm);
		int capacity = inventory.Count;
		int containerWidth = 64 * (capacity / rows);
		ItemsToGrabMenu = new InventoryMenu(Game1.uiViewport.Width / 2 - containerWidth / 2, yPositionOnScreen + 64, playerInventory: false, inventory, null, capacity, rows);
		stackCapacityCheck = stack_capacity_check;
		for (int i = 0; i < ItemsToGrabMenu.actualInventory.Count; i++)
		{
			if (i >= ItemsToGrabMenu.actualInventory.Count - ItemsToGrabMenu.capacity / ItemsToGrabMenu.rows)
			{
				ItemsToGrabMenu.inventory[i].downNeighborID = i + 53910;
			}
		}
		for (int j = 0; j < base.inventory.inventory.Count; j++)
		{
			base.inventory.inventory[j].myID = j + 53910;
			if (base.inventory.inventory[j].downNeighborID != -1)
			{
				base.inventory.inventory[j].downNeighborID += 53910;
			}
			if (base.inventory.inventory[j].rightNeighborID != -1)
			{
				base.inventory.inventory[j].rightNeighborID += 53910;
			}
			if (base.inventory.inventory[j].leftNeighborID != -1)
			{
				base.inventory.inventory[j].leftNeighborID += 53910;
			}
			if (base.inventory.inventory[j].upNeighborID != -1)
			{
				base.inventory.inventory[j].upNeighborID += 53910;
			}
			if (j < 12)
			{
				base.inventory.inventory[j].upNeighborID = ItemsToGrabMenu.actualInventory.Count - ItemsToGrabMenu.capacity / ItemsToGrabMenu.rows;
			}
			foreach (ClickableComponent item in base.inventory.GetBorder(InventoryMenu.BorderSide.Right))
			{
				item.rightNeighborID = okButton.myID;
			}
		}
		dropItemInvisibleButton.myID = -500;
		ItemsToGrabMenu.dropItemInvisibleButton.myID = -500;
		populateClickableComponentList();
		if (Game1.options.SnappyMenus)
		{
			setCurrentlySnappedComponentTo(53910);
			snapCursorToCurrentSnappedComponent();
		}
	}

	public virtual int GetDonatableAmount(Item item)
	{
		if (item == null)
		{
			return 0;
		}
		int stack_capacity = item.Stack;
		if (stackCapacityCheck != null)
		{
			stack_capacity = Math.Min(stack_capacity, stackCapacityCheck(item));
		}
		return stack_capacity;
	}

	public virtual Item TryToGrab(Item item, int amount)
	{
		int grabbed_amount = Math.Min(amount, item.Stack);
		if (grabbed_amount == 0)
		{
			return item;
		}
		Item taken_stack = item.getOne();
		taken_stack.Stack = grabbed_amount;
		item.Stack -= grabbed_amount;
		InventoryMenu.highlightThisItem highlight_method = inventory.highlightMethod;
		inventory.highlightMethod = InventoryMenu.highlightAllItems;
		Item leftover_items = inventory.tryToAddItem(taken_stack);
		inventory.highlightMethod = highlight_method;
		if (leftover_items != null)
		{
			item.Stack += leftover_items.Stack;
		}
		onItemChanged?.Invoke();
		if (item.Stack <= 0)
		{
			return null;
		}
		return item;
	}

	public virtual Item TryToPlace(Item item, int amount)
	{
		int stack_capacity = Math.Min(amount, GetDonatableAmount(item));
		if (stack_capacity == 0)
		{
			return item;
		}
		Item donation_stack = item.getOne();
		donation_stack.Stack = stack_capacity;
		item.Stack -= stack_capacity;
		Item leftover_items = ItemsToGrabMenu.tryToAddItem(donation_stack, "Ship");
		if (leftover_items != null)
		{
			item.Stack += leftover_items.Stack;
		}
		onItemChanged?.Invoke();
		if (item.Stack <= 0)
		{
			return null;
		}
		return item;
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (base.isWithinBounds(x, y))
		{
			Item clicked_item = inventory.getItemAt(x, y);
			if (clicked_item != null)
			{
				int clicked_index = inventory.getInventoryPositionOfClick(x, y);
				inventory.actualInventory[clicked_index] = TryToPlace(clicked_item, clicked_item.Stack);
			}
		}
		if (ItemsToGrabMenu.isWithinBounds(x, y))
		{
			Item clicked_item2 = ItemsToGrabMenu.getItemAt(x, y);
			if (clicked_item2 != null)
			{
				int clicked_index2 = ItemsToGrabMenu.getInventoryPositionOfClick(x, y);
				ItemsToGrabMenu.actualInventory[clicked_index2] = TryToGrab(clicked_item2, clicked_item2.Stack);
			}
		}
		if (okButton.containsPoint(x, y) && readyToClose())
		{
			exitThisMenu();
		}
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (base.isWithinBounds(x, y))
		{
			Item clicked_item = inventory.getItemAt(x, y);
			if (clicked_item != null)
			{
				int clicked_index = inventory.getInventoryPositionOfClick(x, y);
				inventory.actualInventory[clicked_index] = TryToPlace(clicked_item, 1);
			}
		}
		if (ItemsToGrabMenu.isWithinBounds(x, y))
		{
			Item clicked_item2 = ItemsToGrabMenu.getItemAt(x, y);
			if (clicked_item2 != null)
			{
				int clicked_index2 = ItemsToGrabMenu.getInventoryPositionOfClick(x, y);
				ItemsToGrabMenu.actualInventory[clicked_index2] = TryToGrab(clicked_item2, 1);
			}
		}
	}

	/// <inheritdoc />
	protected override void cleanupBeforeExit()
	{
		onConfirm?.Invoke();
		base.cleanupBeforeExit();
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
		ItemsToGrabMenu.hover(x, y, base.heldItem);
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
		base.draw(b, drawUpperPortion: false, drawDescriptionArea: false);
		Game1.drawDialogueBox(ItemsToGrabMenu.xPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder, ItemsToGrabMenu.yPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder, ItemsToGrabMenu.width + IClickableMenu.borderWidth * 2 + IClickableMenu.spaceToClearSideBorder * 2, ItemsToGrabMenu.height + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth * 2, speaker: false, drawOnlyBox: true);
		ItemsToGrabMenu.draw(b);
		if (!hoverText.Equals(""))
		{
			IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont, 0, 0, -1, null, -1, null, null, 0, null, -1, -1, -1, 1f, null, null, null, null, null, null);
		}
		base.heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 16), 1f);
		drawMouse(b);
		string text = ItemsToGrabMenu.descriptionTitle;
		if (text != null && text.Length > 1)
		{
			IClickableMenu.drawHoverText(b, ItemsToGrabMenu.descriptionTitle, Game1.smallFont, 32 + ((base.heldItem != null) ? 16 : (-21)), 32 + ((base.heldItem != null) ? 16 : (-21)), -1, null, -1, null, null, 0, null, -1, -1, -1, 1f, null, null, null, null, null, null);
		}
	}
}
