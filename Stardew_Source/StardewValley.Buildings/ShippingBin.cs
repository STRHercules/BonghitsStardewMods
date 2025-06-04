using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;

namespace StardewValley.Buildings;

public class ShippingBin : Building
{
	private TemporaryAnimatedSprite shippingBinLid;

	private Farm farm;

	private Rectangle shippingBinLidOpenArea;

	protected Vector2 _lidGenerationPosition;

	public ShippingBin(Vector2 tileLocation)
		: base("Shipping Bin", tileLocation)
	{
		initLid();
	}

	public ShippingBin()
		: this(Vector2.Zero)
	{
	}

	public void initLid()
	{
		shippingBinLid = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(134, 226, 30, 25), new Vector2(tileX.Value, tileY.Value - 1) * 64f + new Vector2(1f, -7f) * 4f, flipped: false, 0f, Color.White)
		{
			holdLastFrame = true,
			destroyable = false,
			interval = 20f,
			animationLength = 13,
			paused = true,
			scale = 4f,
			layerDepth = (float)((tileY.Value + 1) * 64) / 10000f + 0.0001f,
			pingPong = true,
			pingPongMotion = 0
		};
		shippingBinLidOpenArea = new Rectangle((tileX.Value - 1) * 64, (tileY.Value - 1) * 64, 256, 192);
		_lidGenerationPosition = new Vector2(tileX.Value, tileY.Value);
	}

	public override Rectangle? getSourceRectForMenu()
	{
		return new Rectangle(0, 0, texture.Value.Bounds.Width, texture.Value.Bounds.Height);
	}

	public override void resetLocalState()
	{
		base.resetLocalState();
		if (shippingBinLid != null)
		{
			_ = shippingBinLidOpenArea;
		}
		else
		{
			initLid();
		}
	}

	public override void Update(GameTime time)
	{
		base.Update(time);
		if (farm == null)
		{
			farm = Game1.getFarm();
		}
		if (shippingBinLid != null)
		{
			_ = shippingBinLidOpenArea;
			if (_lidGenerationPosition.X == (float)tileX.Value && _lidGenerationPosition.Y == (float)tileY.Value)
			{
				bool opening = false;
				foreach (Farmer farmer in GetParentLocation().farmers)
				{
					if (farmer.GetBoundingBox().Intersects(shippingBinLidOpenArea))
					{
						openShippingBinLid();
						opening = true;
					}
				}
				if (!opening)
				{
					closeShippingBinLid();
				}
				updateShippingBinLid(time);
				return;
			}
		}
		initLid();
	}

	/// <inheritdoc />
	public override void performActionOnBuildingPlacement()
	{
		base.performActionOnBuildingPlacement();
		initLid();
	}

	private void openShippingBinLid()
	{
		if (shippingBinLid != null)
		{
			if (shippingBinLid.pingPongMotion != 1 && IsInCurrentLocation())
			{
				Game1.currentLocation.localSound("doorCreak", null, null);
			}
			shippingBinLid.pingPongMotion = 1;
			shippingBinLid.paused = false;
		}
	}

	private void closeShippingBinLid()
	{
		TemporaryAnimatedSprite temporaryAnimatedSprite = shippingBinLid;
		if (temporaryAnimatedSprite != null && temporaryAnimatedSprite.currentParentTileIndex > 0)
		{
			if (shippingBinLid.pingPongMotion != -1 && IsInCurrentLocation())
			{
				Game1.currentLocation.localSound("doorCreakReverse", null, null);
			}
			shippingBinLid.pingPongMotion = -1;
			shippingBinLid.paused = false;
		}
	}

	private void updateShippingBinLid(GameTime time)
	{
		if (isShippingBinLidOpen(requiredToBeFullyOpen: true) && shippingBinLid.pingPongMotion == 1)
		{
			shippingBinLid.paused = true;
		}
		else if (shippingBinLid.currentParentTileIndex == 0 && shippingBinLid.pingPongMotion == -1)
		{
			if (!shippingBinLid.paused && IsInCurrentLocation())
			{
				Game1.currentLocation.localSound("woodyStep", null, null);
			}
			shippingBinLid.paused = true;
		}
		shippingBinLid.update(time);
	}

	private bool isShippingBinLidOpen(bool requiredToBeFullyOpen = false)
	{
		if (shippingBinLid != null && shippingBinLid.currentParentTileIndex >= ((!requiredToBeFullyOpen) ? 1 : (shippingBinLid.animationLength - 1)))
		{
			return true;
		}
		return false;
	}

	private void shipItem(Item i, Farmer who)
	{
		if (i != null)
		{
			who.removeItemFromInventory(i);
			farm?.getShippingBin(who).Add(i);
			showShipment(i, playThrowSound: false);
			farm.lastItemShipped = i;
			if (Game1.player.ActiveItem == null)
			{
				Game1.player.showNotCarrying();
				Game1.player.Halt();
			}
		}
	}

	public override bool CanLeftClick(int x, int y)
	{
		Rectangle hit_rect = new Rectangle(tileX.Value * 64, tileY.Value * 64, tilesWide.Value * 64, tilesHigh.Value * 64);
		hit_rect.Y -= 64;
		hit_rect.Height += 64;
		return hit_rect.Contains(x, y);
	}

	public override bool leftClicked()
	{
		Item item = Game1.player.ActiveItem;
		bool? flag = item?.canBeShipped();
		if (flag.HasValue && flag == true && farm != null && Vector2.Distance(Game1.player.Tile, new Vector2((float)tileX.Value + 0.5f, tileY.Value)) <= 2f)
		{
			Game1.player.ActiveItem = null;
			Game1.player.showNotCarrying();
			farm.getShippingBin(Game1.player).Add(item);
			farm.lastItemShipped = item;
			showShipment(item);
			return true;
		}
		return base.leftClicked();
	}

	public void showShipment(Item item, bool playThrowSound = true)
	{
		if (farm == null)
		{
			return;
		}
		GameLocation parentLocation = GetParentLocation();
		if (playThrowSound)
		{
			parentLocation.localSound("backpackIN", null, null);
		}
		DelayedAction.playSoundAfterDelay("Ship", playThrowSound ? 250 : 0, null, null);
		int id = Game1.random.Next();
		parentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(524, 218, 34, 22), new Vector2(tileX.Value, tileY.Value - 1) * 64f + new Vector2(-1f, 5f) * 4f, flipped: false, 0f, Color.White)
		{
			interval = 100f,
			totalNumberOfLoops = 1,
			animationLength = 3,
			pingPong = true,
			alpha = alpha,
			scale = 4f,
			layerDepth = (float)((tileY.Value + 1) * 64) / 10000f + 0.0002f,
			id = id,
			extraInfoForEndBehavior = id,
			endFunction = parentLocation.removeTemporarySpritesWithID
		});
		parentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(524, 230, 34, 10), new Vector2(tileX.Value, tileY.Value - 1) * 64f + new Vector2(-1f, 17f) * 4f, flipped: false, 0f, Color.White)
		{
			interval = 100f,
			totalNumberOfLoops = 1,
			animationLength = 3,
			pingPong = true,
			alpha = alpha,
			scale = 4f,
			layerDepth = (float)((tileY.Value + 1) * 64) / 10000f + 0.0003f,
			id = id,
			extraInfoForEndBehavior = id
		});
		ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
		ColoredObject coloredObj = item as ColoredObject;
		Vector2 initialPosition = new Vector2(tileX.Value, tileY.Value - 1) * 64f + new Vector2(7 + Game1.random.Next(6), 2f) * 4f;
		bool[] array = new bool[2] { false, true };
		foreach (bool isColorOverlay in array)
		{
			if (!isColorOverlay || (coloredObj != null && !coloredObj.ColorSameIndexAsParentSheetIndex))
			{
				parentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(itemData.TextureName, itemData.GetSourceRect(isColorOverlay ? 1 : 0, null), initialPosition, flipped: false, 0f, Color.White)
				{
					interval = 9999f,
					scale = 4f,
					alphaFade = 0.045f,
					layerDepth = (float)((tileY.Value + 1) * 64) / 10000f + 0.000225f,
					motion = new Vector2(0f, 0.3f),
					acceleration = new Vector2(0f, 0.2f),
					scaleChange = -0.05f,
					color = (coloredObj?.color.Value ?? Color.White)
				});
			}
		}
	}

	public override bool doAction(Vector2 tileLocation, Farmer who)
	{
		if (daysOfConstructionLeft.Value <= 0 && tileLocation.X >= (float)tileX.Value && tileLocation.X <= (float)(tileX.Value + 1) && tileLocation.Y == (float)tileY.Value)
		{
			if (!Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
			{
				return false;
			}
			ItemGrabMenu itemGrabMenu = new ItemGrabMenu(null, reverseGrab: true, showReceivingMenu: false, Utility.highlightShippableObjects, shipItem, "", null, snapToBottom: true, canBeExitedWithKey: true, playRightClickSound: false, allowRightClick: true, showOrganizeButton: false, 0, null, -1, this);
			itemGrabMenu.initializeUpperRightCloseButton();
			itemGrabMenu.setBackgroundTransparency(b: false);
			itemGrabMenu.setDestroyItemOnClick(b: true);
			itemGrabMenu.initializeShippingBin();
			Game1.activeClickableMenu = itemGrabMenu;
			if (who.IsLocalPlayer)
			{
				Game1.playSound("shwip", null);
			}
			if (Game1.player.FacingDirection == 1)
			{
				Game1.player.Halt();
			}
			Game1.player.showCarrying();
			return true;
		}
		return base.doAction(tileLocation, who);
	}

	public override void drawInMenu(SpriteBatch b, int x, int y)
	{
		base.drawInMenu(b, x, y);
		b.Draw(Game1.mouseCursors, new Vector2(x + 4, y - 20), new Rectangle(134, 226, 30, 25), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
	}

	public override void draw(SpriteBatch b)
	{
		if (!base.isMoving)
		{
			base.draw(b);
			if (shippingBinLid != null && daysOfConstructionLeft.Value <= 0)
			{
				shippingBinLid.color = color;
				shippingBinLid.draw(b, localPosition: false, 0, 0, alpha * ((newConstructionTimer.Value > 0) ? ((1000f - (float)newConstructionTimer.Value) / 1000f) : 1f));
			}
		}
	}
}
