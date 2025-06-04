using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;
using StardewValley.TokenizableStrings;
using xTile.Dimensions;

namespace StardewValley.Menus;

public class MuseumMenu : MenuWithInventory
{
	public const int startingState = 0;

	public const int placingInMuseumState = 1;

	public const int exitingState = 2;

	public int fadeTimer;

	public int state;

	public int menuPositionOffset;

	public bool fadeIntoBlack;

	public bool menuMovingDown;

	public float blackFadeAlpha;

	public SparklingText sparkleText;

	public Vector2 globalLocationOfSparklingArtifact;

	/// <summary>The museum for which the menu was opened.</summary>
	public LibraryMuseum Museum;

	private bool holdingMuseumPiece;

	public bool reOrganizing;

	public MuseumMenu(InventoryMenu.highlightThisItem highlighterMethod)
		: base(highlighterMethod, okButton: true)
	{
		fadeTimer = 800;
		fadeIntoBlack = true;
		movePosition(0, Game1.uiViewport.Height - yPositionOnScreen - height);
		Game1.player.forceCanMove();
		Museum = (Game1.currentLocation as LibraryMuseum) ?? throw new InvalidOperationException("The museum donation menu must be used from within the museum.");
		if (Game1.options.SnappyMenus)
		{
			if (okButton != null)
			{
				okButton.myID = 106;
			}
			populateClickableComponentList();
			currentlySnappedComponent = getComponentWithID(0);
			snapCursorToCurrentSnappedComponent();
		}
		Game1.displayHUD = false;
	}

	public override bool shouldClampGamePadCursor()
	{
		return true;
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (fadeTimer > 0)
		{
			return;
		}
		if (Game1.options.doesInputListContain(Game1.options.menuButton, key) && !Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.menuButton) && readyToClose())
		{
			state = 2;
			fadeTimer = 500;
			fadeIntoBlack = true;
		}
		else if (Game1.options.doesInputListContain(Game1.options.menuButton, key) && !Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.menuButton) && !holdingMuseumPiece && menuMovingDown)
		{
			if (base.heldItem != null)
			{
				Game1.playSound("bigDeSelect", null);
				Utility.CollectOrDrop(base.heldItem);
				base.heldItem = null;
			}
			ReturnToDonatableItems();
		}
		else if (Game1.options.SnappyMenus && base.heldItem == null && !reOrganizing)
		{
			base.receiveKeyPress(key);
		}
		if (!Game1.options.SnappyMenus)
		{
			if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
			{
				Game1.panScreen(0, 4);
			}
			else if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
			{
				Game1.panScreen(4, 0);
			}
			else if (Game1.options.doesInputListContain(Game1.options.moveUpButton, key))
			{
				Game1.panScreen(0, -4);
			}
			else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
			{
				Game1.panScreen(-4, 0);
			}
		}
		else
		{
			if (base.heldItem == null && !reOrganizing)
			{
				return;
			}
			LibraryMuseum museum = Museum;
			Vector2 newCursorPositionTile = new Vector2((int)((Utility.ModifyCoordinateFromUIScale(Game1.getMouseX()) + (float)Game1.viewport.X) / 64f), (int)((Utility.ModifyCoordinateFromUIScale(Game1.getMouseY()) + (float)Game1.viewport.Y) / 64f));
			if (!museum.isTileSuitableForMuseumPiece((int)newCursorPositionTile.X, (int)newCursorPositionTile.Y) && (!reOrganizing || !LibraryMuseum.HasDonatedArtifactAt(newCursorPositionTile)))
			{
				newCursorPositionTile = museum.getFreeDonationSpot();
				Game1.setMousePosition((int)Utility.ModifyCoordinateForUIScale(newCursorPositionTile.X * 64f - (float)Game1.viewport.X + 32f), (int)Utility.ModifyCoordinateForUIScale(newCursorPositionTile.Y * 64f - (float)Game1.viewport.Y + 32f));
				return;
			}
			if (key == Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveUpButton))
			{
				newCursorPositionTile = museum.findMuseumPieceLocationInDirection(newCursorPositionTile, 0, 21, !reOrganizing);
			}
			else if (key == Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveRightButton))
			{
				newCursorPositionTile = museum.findMuseumPieceLocationInDirection(newCursorPositionTile, 1, 21, !reOrganizing);
			}
			else if (key == Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveDownButton))
			{
				newCursorPositionTile = museum.findMuseumPieceLocationInDirection(newCursorPositionTile, 2, 21, !reOrganizing);
			}
			else if (key == Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.moveLeftButton))
			{
				newCursorPositionTile = museum.findMuseumPieceLocationInDirection(newCursorPositionTile, 3, 21, !reOrganizing);
			}
			if (!Game1.viewport.Contains(new Location((int)(newCursorPositionTile.X * 64f + 32f), Game1.viewport.Y + 1)))
			{
				Game1.panScreen((int)(newCursorPositionTile.X * 64f - (float)Game1.viewport.X), 0);
			}
			else if (!Game1.viewport.Contains(new Location(Game1.viewport.X + 1, (int)(newCursorPositionTile.Y * 64f + 32f))))
			{
				Game1.panScreen(0, (int)(newCursorPositionTile.Y * 64f - (float)Game1.viewport.Y));
			}
			Game1.setMousePosition((int)Utility.ModifyCoordinateForUIScale((int)newCursorPositionTile.X * 64 - Game1.viewport.X + 32), (int)Utility.ModifyCoordinateForUIScale((int)newCursorPositionTile.Y * 64 - Game1.viewport.Y + 32));
		}
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
		if ((button == Buttons.DPadUp || button == Buttons.LeftThumbstickUp) && !menuMovingDown && Game1.options.SnappyMenus && currentlySnappedComponent != null && currentlySnappedComponent.myID < 12)
		{
			reOrganizing = true;
			menuMovingDown = true;
			receiveKeyPress(Game1.options.moveUpButton[0].key);
		}
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (fadeTimer > 0)
		{
			return;
		}
		if (okButton != null && okButton.containsPoint(x, y) && readyToClose())
		{
			if (fadeTimer <= 0)
			{
				Game1.playSound("bigDeSelect", null);
			}
			state = 2;
			fadeTimer = 800;
			fadeIntoBlack = true;
			return;
		}
		Item oldItem = base.heldItem;
		if (!holdingMuseumPiece)
		{
			if (base.heldItem == null)
			{
				int inventoryIndex = inventory.getInventoryPositionOfClick(x, y);
				Item inventoryItem = ((inventoryIndex >= 0 && inventoryIndex < inventory.actualInventory.Count) ? inventory.actualInventory[inventoryIndex] : null);
				if (inventoryItem != null && inventory.highlightMethod(inventoryItem))
				{
					base.heldItem = inventoryItem.getOne();
					inventory.actualInventory[inventoryIndex] = inventoryItem.ConsumeStack(1);
				}
			}
			else
			{
				base.heldItem = inventory.leftClick(x, y, base.heldItem);
			}
		}
		if (oldItem == null && base.heldItem != null && Game1.isAnyGamePadButtonBeingPressed())
		{
			receiveGamePadButton(Buttons.DPadUp);
		}
		if (oldItem != null && base.heldItem != null && (y < Game1.viewport.Height - (height - (IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 192)) || menuMovingDown))
		{
			Item item = base.heldItem;
			LibraryMuseum museum = Museum;
			int mapXTile = (int)(Utility.ModifyCoordinateFromUIScale(x) + (float)Game1.viewport.X) / 64;
			int mapYTile = (int)(Utility.ModifyCoordinateFromUIScale(y) + (float)Game1.viewport.Y) / 64;
			if (museum.isTileSuitableForMuseumPiece(mapXTile, mapYTile) && museum.isItemSuitableForDonation(item))
			{
				int rewardsCount = museum.getRewardsForPlayer(Game1.player).Count;
				museum.museumPieces.Add(new Vector2(mapXTile, mapYTile), item.ItemId);
				Game1.playSound("stoneStep", null);
				if (museum.getRewardsForPlayer(Game1.player).Count > rewardsCount && !holdingMuseumPiece)
				{
					sparkleText = new SparklingText(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:NewReward"), Color.MediumSpringGreen, Color.White);
					Game1.playSound("reward", null);
					globalLocationOfSparklingArtifact = new Vector2((float)(mapXTile * 64 + 32) - sparkleText.textWidth / 2f, mapYTile * 64 - 48);
				}
				else
				{
					Game1.playSound("newArtifact", null);
				}
				Game1.player.completeQuest("24");
				base.heldItem = item.ConsumeStack(1);
				int pieces = museum.museumPieces.Length;
				if (!holdingMuseumPiece)
				{
					Game1.stats.checkForArchaeologyAchievements();
					if (pieces == LibraryMuseum.totalArtifacts)
					{
						Game1.multiplayer.globalChatInfoMessage("MuseumComplete", Game1.player.farmName.Value);
					}
					else if (pieces == 40)
					{
						Game1.multiplayer.globalChatInfoMessage("Museum40", Game1.player.farmName.Value);
					}
					else
					{
						Game1.multiplayer.globalChatInfoMessage("donation", Game1.player.name.Value, TokenStringBuilder.ItemNameFor(item));
					}
				}
				ReturnToDonatableItems();
			}
		}
		else if (base.heldItem == null && !inventory.isWithinBounds(x, y))
		{
			int mapXTile2 = (int)(Utility.ModifyCoordinateFromUIScale(x) + (float)Game1.viewport.X) / 64;
			int mapYTile2 = (int)(Utility.ModifyCoordinateFromUIScale(y) + (float)Game1.viewport.Y) / 64;
			Vector2 v = new Vector2(mapXTile2, mapYTile2);
			LibraryMuseum location = Museum;
			if (location.museumPieces.TryGetValue(v, out var itemId))
			{
				base.heldItem = ItemRegistry.Create(itemId, 1, 0, allowNull: true);
				location.museumPieces.Remove(v);
				if (base.heldItem != null)
				{
					holdingMuseumPiece = !LibraryMuseum.HasDonatedArtifact(base.heldItem.QualifiedItemId);
				}
			}
		}
		if (base.heldItem != null && oldItem == null)
		{
			menuMovingDown = true;
			reOrganizing = false;
		}
	}

	public virtual void ReturnToDonatableItems()
	{
		menuMovingDown = false;
		holdingMuseumPiece = false;
		reOrganizing = false;
		if (Game1.options.SnappyMenus)
		{
			movePosition(0, -menuPositionOffset);
			menuPositionOffset = 0;
			base.snapCursorToCurrentSnappedComponent();
		}
	}

	public override void emergencyShutDown()
	{
		if (base.heldItem != null && holdingMuseumPiece)
		{
			Vector2 tile = Museum.getFreeDonationSpot();
			if (Museum.museumPieces.TryAdd(tile, base.heldItem.ItemId))
			{
				base.heldItem = null;
				holdingMuseumPiece = false;
			}
		}
		base.emergencyShutDown();
	}

	public override bool readyToClose()
	{
		if (!holdingMuseumPiece && base.heldItem == null)
		{
			return !menuMovingDown;
		}
		return false;
	}

	/// <inheritdoc />
	protected override void cleanupBeforeExit()
	{
		if (base.heldItem != null)
		{
			base.heldItem = Game1.player.addItemToInventory(base.heldItem);
			if (base.heldItem != null)
			{
				Game1.createItemDebris(base.heldItem, Game1.player.Position, -1);
				base.heldItem = null;
			}
		}
		Game1.displayHUD = true;
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		Item oldItem = base.heldItem;
		if (fadeTimer <= 0)
		{
			base.receiveRightClick(x, y, playSound: true);
		}
		if (base.heldItem != null && oldItem == null)
		{
			menuMovingDown = true;
		}
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		if (sparkleText != null && sparkleText.update(time))
		{
			sparkleText = null;
		}
		if (fadeTimer > 0)
		{
			fadeTimer -= time.ElapsedGameTime.Milliseconds;
			if (fadeIntoBlack)
			{
				blackFadeAlpha = 0f + (1500f - (float)fadeTimer) / 1500f;
			}
			else
			{
				blackFadeAlpha = 1f - (1500f - (float)fadeTimer) / 1500f;
			}
			if (fadeTimer <= 0)
			{
				switch (state)
				{
				case 0:
					state = 1;
					Game1.viewportFreeze = true;
					Game1.viewport.Location = new Location(1152, 128);
					Game1.clampViewportToGameMap();
					fadeTimer = 800;
					fadeIntoBlack = false;
					break;
				case 2:
					Game1.viewportFreeze = false;
					fadeIntoBlack = false;
					fadeTimer = 800;
					state = 3;
					break;
				case 3:
					exitThisMenuNoSound();
					break;
				}
			}
		}
		if (menuMovingDown && menuPositionOffset < height / 3)
		{
			menuPositionOffset += 8;
			movePosition(0, 8);
		}
		else if (!menuMovingDown && menuPositionOffset > 0)
		{
			menuPositionOffset -= 8;
			movePosition(0, -8);
		}
		int mouseX = Game1.getOldMouseX(ui_scale: false) + Game1.viewport.X;
		int mouseY = Game1.getOldMouseY(ui_scale: false) + Game1.viewport.Y;
		if ((!Game1.options.SnappyMenus && Game1.lastCursorMotionWasMouse && mouseX - Game1.viewport.X < 64) || Game1.input.GetGamePadState().ThumbSticks.Right.X < 0f)
		{
			Game1.panScreen(-4, 0);
			if (Game1.input.GetGamePadState().ThumbSticks.Right.X < 0f)
			{
				snapCursorToCurrentMuseumSpot();
			}
		}
		else if ((!Game1.options.SnappyMenus && Game1.lastCursorMotionWasMouse && mouseX - (Game1.viewport.X + Game1.viewport.Width) >= -64) || Game1.input.GetGamePadState().ThumbSticks.Right.X > 0f)
		{
			Game1.panScreen(4, 0);
			if (Game1.input.GetGamePadState().ThumbSticks.Right.X > 0f)
			{
				snapCursorToCurrentMuseumSpot();
			}
		}
		if ((!Game1.options.SnappyMenus && Game1.lastCursorMotionWasMouse && mouseY - Game1.viewport.Y < 64) || Game1.input.GetGamePadState().ThumbSticks.Right.Y > 0f)
		{
			Game1.panScreen(0, -4);
			if (Game1.input.GetGamePadState().ThumbSticks.Right.Y > 0f)
			{
				snapCursorToCurrentMuseumSpot();
			}
		}
		else if ((!Game1.options.SnappyMenus && Game1.lastCursorMotionWasMouse && mouseY - (Game1.viewport.Y + Game1.viewport.Height) >= -64) || Game1.input.GetGamePadState().ThumbSticks.Right.Y < 0f)
		{
			Game1.panScreen(0, 4);
			if (Game1.input.GetGamePadState().ThumbSticks.Right.Y < 0f)
			{
				snapCursorToCurrentMuseumSpot();
			}
		}
		Keys[] pressedKeys = Game1.oldKBState.GetPressedKeys();
		foreach (Keys key in pressedKeys)
		{
			receiveKeyPress(key);
		}
	}

	private void snapCursorToCurrentMuseumSpot()
	{
		if (menuMovingDown)
		{
			Vector2 newCursorPositionTile = new Vector2((Game1.getMouseX(ui_scale: false) + Game1.viewport.X) / 64, (Game1.getMouseY(ui_scale: false) + Game1.viewport.Y) / 64);
			Game1.setMousePosition((int)newCursorPositionTile.X * 64 - Game1.viewport.X + 32, (int)newCursorPositionTile.Y * 64 - Game1.viewport.Y + 32, ui_scale: false);
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Microsoft.Xna.Framework.Rectangle oldBounds, Microsoft.Xna.Framework.Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		movePosition(0, Game1.viewport.Height - yPositionOnScreen - height);
		Game1.player.forceCanMove();
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if ((fadeTimer <= 0 || !fadeIntoBlack) && state != 3)
		{
			if (base.heldItem != null)
			{
				Game1.StartWorldDrawInUI(b);
				for (int y = Game1.viewport.Y / 64 - 1; y < (Game1.viewport.Y + Game1.viewport.Height) / 64 + 2; y++)
				{
					for (int x = Game1.viewport.X / 64 - 1; x < (Game1.viewport.X + Game1.viewport.Width) / 64 + 1; x++)
					{
						if (Museum.isTileSuitableForMuseumPiece(x, y))
						{
							b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x, y) * 64f), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 29), Color.LightGreen);
						}
					}
				}
				Game1.EndWorldDrawInUI(b);
			}
			if (!holdingMuseumPiece)
			{
				base.draw(b, drawUpperPortion: false, drawDescriptionArea: false);
			}
			if (!hoverText.Equals(""))
			{
				IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont, 0, 0, -1, null, -1, null, null, 0, null, -1, -1, -1, 1f, null, null, null, null, null, null);
			}
			base.heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
			drawMouse(b);
			sparkleText?.draw(b, Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(Game1.viewport, globalLocationOfSparklingArtifact)));
		}
		b.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * blackFadeAlpha);
	}
}
