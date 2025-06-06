using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Buildings;
using StardewValley.Extensions;
using xTile.Dimensions;

namespace StardewValley.Menus;

public class AnimalQueryMenu : IClickableMenu
{
	public const int region_okButton = 101;

	public const int region_love = 102;

	public const int region_sellButton = 103;

	public const int region_moveHomeButton = 104;

	public const int region_noButton = 105;

	public const int region_allowReproductionButton = 106;

	public const int region_loveHover = 109;

	public const int region_textBoxCC = 110;

	public new static int width = 384;

	public new static int height = 512;

	public FarmAnimal animal;

	public TextBox textBox;

	public ClickableTextureComponent okButton;

	public ClickableTextureComponent love;

	public ClickableTextureComponent sellButton;

	public ClickableTextureComponent moveHomeButton;

	public ClickableTextureComponent yesButton;

	public ClickableTextureComponent noButton;

	public ClickableTextureComponent allowReproductionButton;

	public ClickableComponent loveHover;

	public ClickableComponent textBoxCC;

	public double loveLevel;

	public bool confirmingSell;

	public bool movingAnimal;

	public string hoverText = "";

	public string parentName;

	public AnimalQueryMenu(FarmAnimal animal)
		: base(Game1.uiViewport.Width / 2 - width / 2, Game1.uiViewport.Height / 2 - height / 2, width, height)
	{
		Game1.player.Halt();
		Game1.player.faceGeneralDirection(animal.Position, 0, opposite: false, useTileCalculations: false);
		width = 384;
		if (Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.ru)
		{
			width += 32;
		}
		height = 512;
		this.animal = animal;
		textBox = new TextBox(null, null, Game1.dialogueFont, Game1.textColor);
		textBox.X = Game1.uiViewport.Width / 2 - 128 - 12;
		textBox.Y = yPositionOnScreen - 4 + 128;
		textBox.Width = 256;
		textBox.Height = 192;
		textBoxCC = new ClickableComponent(new Microsoft.Xna.Framework.Rectangle(textBox.X, textBox.Y, textBox.Width, 64), "")
		{
			myID = 110,
			downNeighborID = 104
		};
		textBox.Text = animal.displayName;
		Game1.keyboardDispatcher.Subscriber = textBox;
		textBox.Selected = false;
		if (animal.parentId.Value != -1)
		{
			FarmAnimal parent = Utility.getAnimal(animal.parentId.Value);
			if (parent != null)
			{
				parentName = parent.displayName;
			}
		}
		animal.makeSound();
		okButton = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(xPositionOnScreen + width + 4, yPositionOnScreen + height - 64 - IClickableMenu.borderWidth, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f)
		{
			myID = 101,
			upNeighborID = -99998
		};
		sellButton = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(xPositionOnScreen + width + 4, yPositionOnScreen + height - 192 - IClickableMenu.borderWidth, 64, 64), Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(0, 384, 16, 16), 4f)
		{
			myID = 103,
			downNeighborID = -99998,
			upNeighborID = 104
		};
		moveHomeButton = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(xPositionOnScreen + width + 4, yPositionOnScreen + height - 256 - IClickableMenu.borderWidth, 64, 64), Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(16, 384, 16, 16), 4f)
		{
			myID = 104,
			downNeighborID = 103,
			upNeighborID = 110
		};
		if (!animal.isBaby() && animal.CanHavePregnancy())
		{
			allowReproductionButton = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + height - 128 - IClickableMenu.borderWidth + 8, 36, 36), Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(animal.allowReproduction.Value ? 128 : 137, 393, 9, 9), 4f)
			{
				myID = 106,
				downNeighborID = 101,
				upNeighborID = 103
			};
		}
		love = new ClickableTextureComponent(Math.Round((double)animal.friendshipTowardFarmer.Value, 0) / 10.0 + "<", new Microsoft.Xna.Framework.Rectangle(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + 32 + 16, yPositionOnScreen - 32 + IClickableMenu.spaceToClearTopBorder + 256 - 32, width - 128, 64), null, "Friendship", Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(172, 512, 16, 16), 4f)
		{
			myID = 102
		};
		loveHover = new ClickableComponent(new Microsoft.Xna.Framework.Rectangle(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder, yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 192 - 32, width, 64), "Friendship")
		{
			myID = 109
		};
		if (animal.homeInterior == null)
		{
			Utility.fixAllAnimals();
		}
		loveLevel = (float)animal.friendshipTowardFarmer.Value / 1000f;
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
		}
	}

	public override bool shouldClampGamePadCursor()
	{
		return movingAnimal;
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(101);
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		if (Game1.globalFade)
		{
			return;
		}
		if (Game1.options.menuButton.Contains(new InputButton(key)) && (textBox == null || !textBox.Selected))
		{
			Game1.playSound("smallSelect", null);
			if (readyToClose())
			{
				Game1.exitActiveMenu();
				if (textBox.Text.Length > 0 && !Utility.areThereAnyOtherAnimalsWithThisName(textBox.Text))
				{
					animal.displayName = textBox.Text;
					animal.Name = textBox.Text;
				}
			}
			else if (movingAnimal)
			{
				Game1.globalFadeToBlack(prepareForReturnFromPlacement);
			}
		}
		else if (Game1.options.SnappyMenus && (!Game1.options.menuButton.Contains(new InputButton(key)) || textBox == null || !textBox.Selected))
		{
			base.receiveKeyPress(key);
		}
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		base.update(time);
		if (movingAnimal)
		{
			int mouseX = Game1.getOldMouseX(ui_scale: false) + Game1.viewport.X;
			int mouseY = Game1.getOldMouseY(ui_scale: false) + Game1.viewport.Y;
			if (mouseX - Game1.viewport.X < 64)
			{
				Game1.panScreen(-8, 0);
			}
			else if (mouseX - (Game1.viewport.X + Game1.viewport.Width) >= -64)
			{
				Game1.panScreen(8, 0);
			}
			if (mouseY - Game1.viewport.Y < 64)
			{
				Game1.panScreen(0, -8);
			}
			else if (mouseY - (Game1.viewport.Y + Game1.viewport.Height) >= -64)
			{
				Game1.panScreen(0, 8);
			}
			Keys[] pressedKeys = Game1.oldKBState.GetPressedKeys();
			foreach (Keys key in pressedKeys)
			{
				receiveKeyPress(key);
			}
		}
	}

	public void finishedPlacingAnimal()
	{
		Game1.exitActiveMenu();
		Game1.currentLocation.cleanupBeforePlayerExit();
		Game1.currentLocation = Game1.player.currentLocation;
		Game1.currentLocation.resetForPlayerEntry();
		Game1.globalFadeToClear();
		Game1.displayHUD = true;
		Game1.viewportFreeze = false;
		Game1.displayFarmer = true;
		Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\UI:AnimalQuery_Moving_HomeChanged")));
		Game1.player.viewingLocation.Value = null;
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (Game1.globalFade)
		{
			return;
		}
		if (movingAnimal)
		{
			if (okButton != null && okButton.containsPoint(x, y))
			{
				Game1.globalFadeToBlack(prepareForReturnFromPlacement);
				Game1.playSound("smallSelect", null);
			}
			Vector2 clickTile = new Vector2((Game1.viewport.X + Game1.getOldMouseX(ui_scale: false)) / 64, (Game1.viewport.Y + Game1.getOldMouseY(ui_scale: false)) / 64);
			Farm f = Game1.getFarm();
			Building selection = f.getBuildingAt(clickTile);
			if (selection == null)
			{
				return;
			}
			if (animal.CanLiveIn(selection))
			{
				AnimalHouse selectedHome = (AnimalHouse)selection.GetIndoors();
				if (selectedHome.isFull())
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:AnimalQuery_Moving_BuildingFull"));
					return;
				}
				if (selection.Equals(animal.home))
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:AnimalQuery_Moving_AlreadyHome"));
					return;
				}
				AnimalHouse oldHome = (AnimalHouse)animal.homeInterior;
				if (oldHome.animals.Remove(animal.myID.Value) || f.animals.Remove(animal.myID.Value))
				{
					oldHome.animalsThatLiveHere.Remove(animal.myID.Value);
					selectedHome.adoptAnimal(animal);
				}
				animal.makeSound();
				Game1.globalFadeToBlack(finishedPlacingAnimal);
			}
			else
			{
				Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:AnimalQuery_Moving_CantLiveThere", animal.shortDisplayType()));
			}
			return;
		}
		if (confirmingSell)
		{
			if (yesButton.containsPoint(x, y))
			{
				Game1.player.Money += animal.getSellPrice();
				((AnimalHouse)animal.homeInterior).animalsThatLiveHere.Remove(animal.myID.Value);
				animal.health.Value = -1;
				if (animal.foundGrass != null && FarmAnimal.reservedGrass.Contains(animal.foundGrass))
				{
					FarmAnimal.reservedGrass.Remove(animal.foundGrass);
				}
				int numClouds = animal.Sprite.getWidth() / 2;
				for (int i = 0; i < numClouds; i++)
				{
					int nonRedness = Game1.random.Next(25, 200);
					Game1.multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(5, animal.Position + new Vector2(Game1.random.Next(-32, animal.Sprite.getWidth() * 3), Game1.random.Next(-32, animal.GetBoundingBox().Height * 3)), new Color(255 - nonRedness, 255, 255 - nonRedness), 8, flipped: false, Game1.random.NextBool() ? 50 : Game1.random.Next(30, 200), 0, 64, -1f, 64, (!Game1.random.NextBool()) ? Game1.random.Next(0, 600) : 0)
					{
						scale = (float)Game1.random.Next(2, 5) * 0.25f,
						alpha = (float)Game1.random.Next(2, 5) * 0.25f,
						motion = new Vector2(0f, (float)(0.0 - Game1.random.NextDouble()))
					});
				}
				Game1.playSound("newRecipe", null);
				Game1.playSound("money", null);
				Game1.exitActiveMenu();
			}
			else if (noButton.containsPoint(x, y))
			{
				confirmingSell = false;
				Game1.playSound("smallSelect", null);
				if (Game1.options.SnappyMenus)
				{
					currentlySnappedComponent = getComponentWithID(103);
					snapCursorToCurrentSnappedComponent();
				}
			}
			return;
		}
		if (okButton != null && okButton.containsPoint(x, y) && readyToClose())
		{
			Game1.exitActiveMenu();
			if (textBox.Text.Length > 0 && !Utility.areThereAnyOtherAnimalsWithThisName(textBox.Text))
			{
				animal.displayName = textBox.Text;
				animal.Name = textBox.Text;
			}
			Game1.playSound("smallSelect", null);
		}
		if (sellButton.containsPoint(x, y))
		{
			confirmingSell = true;
			yesButton = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(Game1.uiViewport.Width / 2 - 64 - 4, Game1.uiViewport.Height / 2 - 32, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f)
			{
				myID = 111,
				rightNeighborID = 105
			};
			noButton = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(Game1.uiViewport.Width / 2 + 4, Game1.uiViewport.Height / 2 - 32, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47), 1f)
			{
				myID = 105,
				leftNeighborID = 111
			};
			Game1.playSound("smallSelect", null);
			if (Game1.options.SnappyMenus)
			{
				populateClickableComponentList();
				currentlySnappedComponent = noButton;
				snapCursorToCurrentSnappedComponent();
			}
			return;
		}
		if (moveHomeButton.containsPoint(x, y))
		{
			Game1.playSound("smallSelect", null);
			Game1.globalFadeToBlack(prepareForAnimalPlacement);
		}
		if (allowReproductionButton != null && allowReproductionButton.containsPoint(x, y))
		{
			Game1.playSound("drumkit6", null);
			animal.allowReproduction.Value = !animal.allowReproduction.Value;
			if (animal.allowReproduction.Value)
			{
				allowReproductionButton.sourceRect.X = 128;
			}
			else
			{
				allowReproductionButton.sourceRect.X = 137;
			}
		}
		textBox.Update();
	}

	/// <inheritdoc />
	public override bool overrideSnappyMenuCursorMovementBan()
	{
		return movingAnimal;
	}

	public void prepareForAnimalPlacement()
	{
		movingAnimal = true;
		Game1.currentLocation.cleanupBeforePlayerExit();
		Game1.currentLocation = Game1.getFarm();
		Game1.player.viewingLocation.Value = Game1.currentLocation.NameOrUniqueName;
		Game1.globalFadeToClear();
		okButton.bounds.X = Game1.uiViewport.Width - 128;
		okButton.bounds.Y = Game1.uiViewport.Height - 128;
		Game1.displayHUD = false;
		Game1.viewportFreeze = true;
		Game1.viewport.Location = new Location(3136, 320);
		Game1.panScreen(0, 0);
		Game1.currentLocation.resetForPlayerEntry();
		Game1.displayFarmer = false;
	}

	public void prepareForReturnFromPlacement()
	{
		Game1.currentLocation.cleanupBeforePlayerExit();
		Game1.currentLocation = Game1.player.currentLocation;
		Game1.currentLocation.resetForPlayerEntry();
		Game1.globalFadeToClear();
		okButton.bounds.X = xPositionOnScreen + width + 4;
		okButton.bounds.Y = yPositionOnScreen + height - 64 - IClickableMenu.borderWidth;
		Game1.displayHUD = true;
		Game1.viewportFreeze = false;
		Game1.displayFarmer = true;
		movingAnimal = false;
		Game1.player.viewingLocation.Value = null;
	}

	public override bool readyToClose()
	{
		textBox.Selected = false;
		if (base.readyToClose() && !movingAnimal)
		{
			return !Game1.globalFade;
		}
		return false;
	}

	/// <inheritdoc />
	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (Game1.globalFade)
		{
			return;
		}
		if (readyToClose())
		{
			Game1.exitActiveMenu();
			if (textBox.Text.Length > 0 && !Utility.areThereAnyOtherAnimalsWithThisName(textBox.Text))
			{
				animal.displayName = textBox.Text;
				animal.Name = textBox.Text;
			}
			Game1.playSound("smallSelect", null);
		}
		else if (movingAnimal)
		{
			Game1.globalFadeToBlack(prepareForReturnFromPlacement);
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		hoverText = "";
		if (movingAnimal)
		{
			Vector2 clickTile = new Vector2((Game1.viewport.X + Game1.getOldMouseX(ui_scale: false)) / 64, (Game1.viewport.Y + Game1.getOldMouseY(ui_scale: false)) / 64);
			Farm f = Game1.getFarm();
			foreach (Building building in f.buildings)
			{
				building.color = Color.White;
			}
			Building selection = f.getBuildingAt(clickTile);
			if (selection != null)
			{
				if (animal.CanLiveIn(selection) && !((AnimalHouse)selection.GetIndoors()).isFull() && !selection.Equals(animal.home))
				{
					selection.color = Color.LightGreen * 0.8f;
				}
				else
				{
					selection.color = Color.Red * 0.8f;
				}
			}
		}
		if (okButton != null)
		{
			if (okButton.containsPoint(x, y))
			{
				okButton.scale = Math.Min(1.1f, okButton.scale + 0.05f);
			}
			else
			{
				okButton.scale = Math.Max(1f, okButton.scale - 0.05f);
			}
		}
		if (sellButton != null)
		{
			if (sellButton.containsPoint(x, y))
			{
				sellButton.scale = Math.Min(4.1f, sellButton.scale + 0.05f);
				hoverText = Game1.content.LoadString("Strings\\UI:AnimalQuery_Sell", animal.getSellPrice());
			}
			else
			{
				sellButton.scale = Math.Max(4f, sellButton.scale - 0.05f);
			}
		}
		if (moveHomeButton != null)
		{
			if (moveHomeButton.containsPoint(x, y))
			{
				moveHomeButton.scale = Math.Min(4.1f, moveHomeButton.scale + 0.05f);
				hoverText = Game1.content.LoadString("Strings\\UI:AnimalQuery_Move");
			}
			else
			{
				moveHomeButton.scale = Math.Max(4f, moveHomeButton.scale - 0.05f);
			}
		}
		if (allowReproductionButton != null)
		{
			if (allowReproductionButton.containsPoint(x, y))
			{
				allowReproductionButton.scale = Math.Min(4.1f, allowReproductionButton.scale + 0.05f);
				hoverText = Game1.content.LoadString("Strings\\UI:AnimalQuery_AllowReproduction");
			}
			else
			{
				allowReproductionButton.scale = Math.Max(4f, allowReproductionButton.scale - 0.05f);
			}
		}
		if (yesButton != null)
		{
			if (yesButton.containsPoint(x, y))
			{
				yesButton.scale = Math.Min(1.1f, yesButton.scale + 0.05f);
			}
			else
			{
				yesButton.scale = Math.Max(1f, yesButton.scale - 0.05f);
			}
		}
		if (noButton != null)
		{
			if (noButton.containsPoint(x, y))
			{
				noButton.scale = Math.Min(1.1f, noButton.scale + 0.05f);
			}
			else
			{
				noButton.scale = Math.Max(1f, noButton.scale - 0.05f);
			}
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (!movingAnimal && !Game1.globalFade)
		{
			if (!Game1.options.showClearBackgrounds)
			{
				b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
			}
			Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen + 128, width, height - 128, speaker: false, drawOnlyBox: true);
			textBox.Draw(b);
			int age = (animal.GetDaysOwned() + 1) / 28 + 1;
			string ageText = ((age <= 1) ? Game1.content.LoadString("Strings\\UI:AnimalQuery_Age1") : Game1.content.LoadString("Strings\\UI:AnimalQuery_AgeN", age));
			if (animal.isBaby())
			{
				ageText += Game1.content.LoadString("Strings\\UI:AnimalQuery_AgeBaby");
			}
			Utility.drawTextWithShadow(b, ageText, Game1.smallFont, new Vector2(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + 32, yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 16 + 128), Game1.textColor);
			int yOffset = 0;
			if (parentName != null)
			{
				yOffset = 21;
				Utility.drawTextWithShadow(b, Game1.content.LoadString("Strings\\UI:AnimalQuery_Parent", parentName), Game1.smallFont, new Vector2(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + 32, 32 + yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + 16 + 128), Game1.textColor);
			}
			int halfHeart = (int)((loveLevel * 1000.0 % 200.0 >= 100.0) ? (loveLevel * 1000.0 / 200.0) : (-100.0));
			for (int i = 0; i < 5; i++)
			{
				b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen + 96 + 32 * i, yOffset + yPositionOnScreen - 32 + 320), new Microsoft.Xna.Framework.Rectangle(211 + ((loveLevel * 1000.0 <= (double)((i + 1) * 195)) ? 7 : 0), 428, 7, 6), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.89f);
				if (halfHeart == i)
				{
					b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen + 96 + 32 * i, yOffset + yPositionOnScreen - 32 + 320), new Microsoft.Xna.Framework.Rectangle(211, 428, 4, 6), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.891f);
				}
			}
			Utility.drawTextWithShadow(b, Game1.parseText(animal.getMoodMessage(), Game1.smallFont, width - IClickableMenu.spaceToClearSideBorder * 2 - 64), Game1.smallFont, new Vector2(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + 32, yOffset + yPositionOnScreen + 384 - 64 + 4), Game1.textColor);
			okButton.draw(b);
			sellButton.draw(b);
			moveHomeButton.draw(b);
			allowReproductionButton?.draw(b);
			if (animal != null && animal.hasEatenAnimalCracker.Value && Game1.objectSpriteSheet_2 != null)
			{
				Utility.drawWithShadow(b, Game1.objectSpriteSheet_2, new Vector2((float)(xPositionOnScreen + width) - 105.6f, (float)yPositionOnScreen + 224f), new Microsoft.Xna.Framework.Rectangle(16, 240, 16, 16), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 0.89f);
			}
			if (confirmingSell)
			{
				if (!Game1.options.showClearBackgrounds)
				{
					b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
				}
				Game1.drawDialogueBox(Game1.uiViewport.Width / 2 - 160, Game1.uiViewport.Height / 2 - 192, 320, 256, speaker: false, drawOnlyBox: true);
				string confirmText = Game1.content.LoadString("Strings\\UI:AnimalQuery_ConfirmSell");
				b.DrawString(Game1.dialogueFont, confirmText, new Vector2((float)(Game1.uiViewport.Width / 2) - Game1.dialogueFont.MeasureString(confirmText).X / 2f, Game1.uiViewport.Height / 2 - 96 + 8), Game1.textColor);
				yesButton.draw(b);
				noButton.draw(b);
			}
			else
			{
				string text = hoverText;
				if (text != null && text.Length > 0)
				{
					IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont, 0, 0, -1, null, -1, null, null, 0, null, -1, -1, -1, 1f, null, null, null, null, null, null);
				}
			}
		}
		else if (!Game1.globalFade)
		{
			string s = Game1.content.LoadString("Strings\\UI:AnimalQuery_ChooseBuilding", animal.displayHouse, animal.displayType);
			Game1.drawDialogueBox(32, -64, (int)Game1.dialogueFont.MeasureString(s).X + IClickableMenu.borderWidth * 2 + 16, 128 + IClickableMenu.borderWidth * 2, speaker: false, drawOnlyBox: true);
			b.DrawString(Game1.dialogueFont, s, new Vector2(32 + IClickableMenu.spaceToClearSideBorder * 2 + 8, 44f), Game1.textColor);
			okButton.draw(b);
		}
		drawMouse(b);
	}
}
