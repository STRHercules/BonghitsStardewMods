using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Extensions;

namespace StardewValley.Menus;

public class ConfirmationDialog : IClickableMenu
{
	public delegate void behavior(Farmer who);

	public const int region_okButton = 101;

	public const int region_cancelButton = 102;

	protected string message;

	public ClickableTextureComponent okButton;

	public ClickableTextureComponent cancelButton;

	protected behavior onConfirm;

	protected behavior onCancel;

	private bool active = true;

	private int delayBeforeCancellable;

	public ConfirmationDialog(string message, behavior onConfirm, behavior onCancel = null)
		: base(Game1.uiViewport.Width / 2 - (int)Game1.dialogueFont.MeasureString(message).X / 2 - IClickableMenu.borderWidth, Game1.uiViewport.Height / 2 - (int)Game1.dialogueFont.MeasureString(message).Y / 2, (int)Game1.dialogueFont.MeasureString(message).X + IClickableMenu.borderWidth * 2, (int)Game1.dialogueFont.MeasureString(message).Y + IClickableMenu.borderWidth * 2 + 160)
	{
		if (onCancel == null)
		{
			onCancel = closeDialog;
		}
		else
		{
			this.onCancel = onCancel;
		}
		this.onConfirm = onConfirm;
		Rectangle titleSafeArea = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
		message = Game1.parseText(message, Game1.dialogueFont, Math.Min(titleSafeArea.Width - 64, width));
		this.message = message;
		okButton = new ClickableTextureComponent("OK", new Rectangle(xPositionOnScreen + width - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 128 - 4, yPositionOnScreen + height - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder + 21, 64, 64), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f)
		{
			myID = 101,
			rightNeighborID = 102
		};
		cancelButton = new ClickableTextureComponent("OK", new Rectangle(xPositionOnScreen + width - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 64, yPositionOnScreen + height - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder + 21, 64, 64), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47), 1f)
		{
			myID = 102,
			leftNeighborID = 101
		};
		if (Game1.options.SnappyMenus)
		{
			populateClickableComponentList();
			snapToDefaultClickableComponent();
			delayBeforeCancellable = 300;
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.gameWindowSizeChanged(oldBounds, newBounds);
		okButton.setPosition(xPositionOnScreen + width - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 128 - 4, yPositionOnScreen + height - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder + 21);
		cancelButton.setPosition(xPositionOnScreen + width - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 64, yPositionOnScreen + height - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder + 21);
	}

	public virtual void closeDialog(Farmer who)
	{
		if (Game1.activeClickableMenu is TitleMenu titleMenu)
		{
			titleMenu.backButtonPressed();
		}
		else
		{
			Game1.exitActiveMenu();
		}
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(102);
		snapCursorToCurrentSnappedComponent();
	}

	public void confirm()
	{
		if (active)
		{
			active = false;
			onConfirm?.Invoke(Game1.player);
			Game1.playSound("smallSelect", null);
		}
	}

	public void cancel()
	{
		if (onCancel != null)
		{
			onCancel(Game1.player);
		}
		else
		{
			closeDialog(Game1.player);
		}
		Game1.playSound("bigDeSelect", null);
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (active)
		{
			if (okButton.containsPoint(x, y))
			{
				confirm();
			}
			if (cancelButton.containsPoint(x, y) && delayBeforeCancellable <= 0)
			{
				cancel();
			}
		}
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		base.receiveKeyPress(key);
		if (active && Game1.activeClickableMenu == null && onCancel != null)
		{
			onCancel(Game1.player);
		}
		if (active)
		{
			switch (key)
			{
			case Keys.Y:
				confirm();
				break;
			case Keys.N:
				cancel();
				break;
			}
		}
	}

	public override void update(GameTime time)
	{
		base.update(time);
		if (delayBeforeCancellable > 0)
		{
			delayBeforeCancellable -= (int)time.ElapsedGameTime.TotalMilliseconds;
		}
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		if (okButton.containsPoint(x, y))
		{
			okButton.scale = Math.Min(okButton.scale + 0.02f, okButton.baseScale + 0.2f);
		}
		else
		{
			okButton.scale = Math.Max(okButton.scale - 0.02f, okButton.baseScale);
		}
		if (cancelButton.containsPoint(x, y))
		{
			cancelButton.scale = ((cancelButton.baseScale == 1f) ? Math.Min(cancelButton.scale + 0.02f, cancelButton.baseScale + 0.2f) : Math.Min(cancelButton.scale + 0.1f, cancelButton.baseScale + 0.75f));
		}
		else
		{
			cancelButton.scale = ((cancelButton.baseScale == 1f) ? Math.Max(cancelButton.scale - 0.02f, cancelButton.baseScale) : Math.Max(cancelButton.scale - 0.1f, cancelButton.baseScale));
		}
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		if (active)
		{
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
			Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, speaker: false, drawOnlyBox: true);
			b.DrawString(Game1.dialogueFont, message, new Vector2(xPositionOnScreen + IClickableMenu.borderWidth, yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth / 2), Game1.textColor);
			okButton.draw(b);
			cancelButton.draw(b);
			drawMouse(b);
		}
	}
}
