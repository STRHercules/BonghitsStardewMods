using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.GameData;

namespace StardewValley.Menus;

public class LanguageSelectionMenu : IClickableMenu
{
	/// <summary>A language which can be selected in this menu.</summary>
	public class LanguageEntry
	{
		/// <summary>The language code for this entry.</summary>
		public readonly LocalizedContentManager.LanguageCode LanguageCode;

		/// <summary>The data for this language in <c>Data/AdditionalLanguages</c>, if applicable.</summary>
		public readonly ModLanguage ModLanguage;

		/// <summary>The button texture to render.</summary>
		public readonly Texture2D Texture;

		/// <summary>The sprite index for the button in the <see cref="F:StardewValley.Menus.LanguageSelectionMenu.LanguageEntry.Texture" />.</summary>
		public readonly int SpriteIndex;

		/// <summary>Construct an instance.</summary>
		/// <param name="languageCode"><inheritdoc cref="F:StardewValley.Menus.LanguageSelectionMenu.LanguageEntry.LanguageCode" path="/summary" /></param>
		/// <param name="modLanguage"><inheritdoc cref="F:StardewValley.Menus.LanguageSelectionMenu.LanguageEntry.ModLanguage" path="/summary" /></param>
		/// <param name="texture"><inheritdoc cref="F:StardewValley.Menus.LanguageSelectionMenu.LanguageEntry.Texture" path="/summary" /></param>
		/// <param name="spriteIndex"><inheritdoc cref="F:StardewValley.Menus.LanguageSelectionMenu.LanguageEntry.SpriteIndex" path="/summary" /></param>
		public LanguageEntry(LocalizedContentManager.LanguageCode languageCode, ModLanguage modLanguage, Texture2D texture, int spriteIndex)
		{
			LanguageCode = languageCode;
			ModLanguage = modLanguage;
			Texture = texture;
			SpriteIndex = spriteIndex;
		}
	}

	public new static int width = 500;

	public new static int height = 728;

	protected int _currentPage;

	protected int _pageCount;

	public readonly Dictionary<string, LanguageEntry> languages;

	public readonly List<ClickableComponent> languageButtons = new List<ClickableComponent>();

	public ClickableTextureComponent nextPageButton;

	public ClickableTextureComponent previousPageButton;

	public LanguageSelectionMenu()
	{
		Texture2D texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\LanguageButtons");
		languages = new LanguageEntry[12]
		{
			new LanguageEntry(LocalizedContentManager.LanguageCode.en, null, texture, 0),
			new LanguageEntry(LocalizedContentManager.LanguageCode.ru, null, texture, 3),
			new LanguageEntry(LocalizedContentManager.LanguageCode.zh, null, texture, 4),
			new LanguageEntry(LocalizedContentManager.LanguageCode.de, null, texture, 6),
			new LanguageEntry(LocalizedContentManager.LanguageCode.pt, null, texture, 2),
			new LanguageEntry(LocalizedContentManager.LanguageCode.fr, null, texture, 7),
			new LanguageEntry(LocalizedContentManager.LanguageCode.es, null, texture, 1),
			new LanguageEntry(LocalizedContentManager.LanguageCode.ja, null, texture, 5),
			new LanguageEntry(LocalizedContentManager.LanguageCode.ko, null, texture, 8),
			new LanguageEntry(LocalizedContentManager.LanguageCode.it, null, texture, 10),
			new LanguageEntry(LocalizedContentManager.LanguageCode.tr, null, texture, 9),
			new LanguageEntry(LocalizedContentManager.LanguageCode.hu, null, texture, 11)
		}.ToDictionary((LanguageEntry p) => p.LanguageCode.ToString());
		foreach (ModLanguage modLanguage in DataLoader.AdditionalLanguages(Game1.content))
		{
			Texture2D customTexture = Game1.temporaryContent.Load<Texture2D>(modLanguage.ButtonTexture);
			languages["ModLanguage_" + modLanguage.Id] = new LanguageEntry(LocalizedContentManager.LanguageCode.mod, modLanguage, customTexture, 0);
		}
		_pageCount = (int)Math.Floor((float)(languages.Count - 1) / 12f) + 1;
		SetupButtons();
	}

	private void SetupButtons()
	{
		Vector2 topLeft = Utility.getTopLeftPositionForCenteringOnScreen((int)((float)width * 2.5f), height);
		languageButtons.Clear();
		int buttonWidth = width - 128;
		int buttonHeight = 83;
		int minIndex = 12 * _currentPage;
		int maxIndex = minIndex + 11;
		int index = 0;
		int row = 0;
		int column = 0;
		foreach (KeyValuePair<string, LanguageEntry> pair in languages)
		{
			if (index < minIndex)
			{
				index++;
				continue;
			}
			if (index > maxIndex)
			{
				break;
			}
			languageButtons.Add(new ClickableComponent(new Rectangle((int)topLeft.X + 64 + column * 6 * 64, (int)topLeft.Y + height - 30 - buttonHeight * (6 - row) - 16, buttonWidth, buttonHeight), pair.Key, null)
			{
				myID = index - minIndex,
				downNeighborID = -99998,
				leftNeighborID = -99998,
				rightNeighborID = -99998,
				upNeighborID = -99998
			});
			index++;
			column++;
			if (column > 2)
			{
				row++;
				column = 0;
			}
		}
		previousPageButton = new ClickableTextureComponent(new Rectangle((int)topLeft.X + 4, (int)topLeft.Y + height / 2 - 25, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
		{
			myID = 554,
			downNeighborID = -99998,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			upNeighborID = -99998,
			visible = (_currentPage > 0)
		};
		nextPageButton = new ClickableTextureComponent(new Rectangle((int)(topLeft.X + (float)width * 2.5f) - 32, (int)topLeft.Y + height / 2 - 25, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
		{
			myID = 555,
			downNeighborID = -99998,
			leftNeighborID = -99998,
			rightNeighborID = -99998,
			upNeighborID = -99998,
			visible = (_currentPage < _pageCount - 1)
		};
		if (Game1.options.SnappyMenus)
		{
			int id = currentlySnappedComponent?.myID ?? 0;
			populateClickableComponentList();
			currentlySnappedComponent = getComponentWithID(id);
			snapCursorToCurrentSnappedComponent();
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(0);
		snapCursorToCurrentSnappedComponent();
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, playSound);
		if (nextPageButton.visible && nextPageButton.containsPoint(x, y))
		{
			Game1.playSound("shwip", null);
			_currentPage++;
			SetupButtons();
			return;
		}
		if (previousPageButton.visible && previousPageButton.containsPoint(x, y))
		{
			Game1.playSound("shwip", null);
			_currentPage--;
			SetupButtons();
			return;
		}
		foreach (ClickableComponent component in languageButtons)
		{
			if (!component.containsPoint(x, y))
			{
				continue;
			}
			Game1.playSound("select", null);
			LanguageEntry entry = languages.GetValueOrDefault(component.name);
			if (entry == null)
			{
				Game1.log.Error("Received click on unknown language button '" + component.name + "'.");
				continue;
			}
			if (Game1.options.SnappyMenus)
			{
				Game1.activeClickableMenu.setCurrentlySnappedComponentTo(81118);
				Game1.activeClickableMenu.snapCursorToCurrentSnappedComponent();
			}
			ApplyLanguage(entry);
			exitThisMenu();
			break;
		}
	}

	public virtual void ApplyLanguage(LanguageEntry entry)
	{
		if (entry.ModLanguage != null)
		{
			LocalizedContentManager.SetModLanguage(entry.ModLanguage);
		}
		else
		{
			LocalizedContentManager.CurrentLanguageCode = entry.LanguageCode;
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
		foreach (ClickableComponent component in languageButtons)
		{
			if (component.containsPoint(x, y))
			{
				if (component.label == null)
				{
					Game1.playSound("Cowboy_Footstep", null);
					component.label = "hovered";
				}
			}
			else
			{
				component.label = null;
			}
		}
		previousPageButton.tryHover(x, y);
		nextPageButton.tryHover(x, y);
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		Vector2 topLeft = Utility.getTopLeftPositionForCenteringOnScreen((int)((float)width * 2.5f), height);
		if (!Game1.options.showClearBackgrounds)
		{
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);
		}
		IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(473, 36, 24, 24), (int)topLeft.X + 32, (int)topLeft.Y + 156, (int)((float)width * 2.55f) - 64, height / 2 + 25, Color.White, 4f);
		foreach (ClickableComponent c in languageButtons)
		{
			LanguageEntry entry = languages.GetValueOrDefault(c.name);
			if (entry != null)
			{
				int buttonSourceY = ((entry.SpriteIndex <= 6) ? (entry.SpriteIndex * 78) : ((entry.SpriteIndex - 7) * 78));
				buttonSourceY += ((c.label != null) ? 39 : 0);
				int buttonSourceX = ((entry.SpriteIndex > 6) ? 174 : 0);
				b.Draw(entry.Texture, c.bounds, new Rectangle(buttonSourceX, buttonSourceY, 174, 40), Color.White, 0f, new Vector2(0f, 0f), SpriteEffects.None, 0f);
			}
		}
		previousPageButton.draw(b);
		nextPageButton.draw(b);
		if (Game1.activeClickableMenu == this)
		{
			drawMouse(b);
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		SetupButtons();
	}
}
