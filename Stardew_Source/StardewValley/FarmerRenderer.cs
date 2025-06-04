using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.Tools;

namespace StardewValley;

[InstanceStatics]
public class FarmerRenderer : INetObject<NetFields>
{
	public enum FarmerSpriteLayers
	{
		SlingshotUp,
		ToolUp,
		Base,
		Pants,
		FaceSkin,
		Eyes,
		Shirt,
		AccessoryUnderHair,
		ArmsUp,
		HatMaskUp,
		Hair,
		Accessory,
		Hat,
		Tool,
		Arms,
		ToolDown,
		Slingshot,
		PantsPassedOut,
		SwimWaterRing,
		MAX,
		TOOL_IN_USE_SIDE
	}

	public const int sleeveDarkestColorIndex = 256;

	public const int skinDarkestColorIndex = 260;

	public const int shoeDarkestColorIndex = 268;

	public const int eyeLightestColorIndex = 276;

	public const int accessoryDrawBelowHairThreshold = 8;

	public const int accessoryFacialHairThreshold = 6;

	protected bool _sickFrame;

	public static bool isDrawingForUI = false;

	public const int TransparentSkin = -12345;

	public const int pantsOffset = 288;

	public const int armOffset = 96;

	public const int shirtXOffset = 16;

	public const int shirtYOffset = 56;

	public static int[] featureYOffsetPerFrame = new int[126]
	{
		1, 2, 2, 0, 5, 6, 1, 2, 2, 1,
		0, 2, 0, 1, 1, 0, 2, 2, 3, 3,
		2, 2, 1, 1, 0, 0, 2, 2, 4, 4,
		0, 0, 1, 2, 1, 1, 1, 1, 0, 0,
		1, 1, 1, 0, 0, -2, -1, 1, 1, 0,
		-1, -2, -1, -1, 5, 4, 0, 0, 3, 2,
		-1, 0, 4, 2, 0, 0, 2, 1, 0, -1,
		1, -2, 0, 0, 1, 1, 1, 1, 1, 1,
		0, 0, 0, 0, 1, -1, -1, -1, -1, 1,
		1, 0, 0, 0, 0, 4, 1, 0, 1, 2,
		1, 0, 1, 0, 1, 2, -3, -4, -1, 0,
		0, 2, 1, -4, -1, 0, 0, -3, 0, 0,
		-1, 0, 0, 2, 1, 1
	};

	public static int[] featureXOffsetPerFrame = new int[126]
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, -1, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, -1,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, -1, -1, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 4, 0, 0, 0, 0, -1, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, -1, 0, 0,
		0, 0, 0, 0, 0, 0
	};

	public static int[] hairstyleHatOffset = new int[16]
	{
		0, 0, 0, 4, 0, 0, 3, 0, 4, 0,
		0, 0, 0, 0, 0, 0
	};

	public static Texture2D hairStylesTexture;

	public static Texture2D shirtsTexture;

	public static Texture2D hatsTexture;

	public static Texture2D accessoriesTexture;

	public static Texture2D pantsTexture;

	public static Dictionary<string, Dictionary<int, List<int>>> recolorOffsets;

	[XmlElement("textureName")]
	public readonly NetString textureName = new NetString();

	[XmlIgnore]
	private LocalizedContentManager farmerTextureManager;

	[XmlIgnore]
	internal Texture2D baseTexture;

	[XmlElement("heightOffset")]
	public readonly NetInt heightOffset = new NetInt(0);

	[XmlIgnore]
	public readonly NetColor eyes = new NetColor();

	[XmlIgnore]
	public readonly NetInt skin = new NetInt();

	[XmlIgnore]
	public readonly NetString shoes = new NetString();

	[XmlIgnore]
	public readonly NetString shirt = new NetString();

	[XmlIgnore]
	public readonly NetString pants = new NetString();

	protected bool _spriteDirty;

	protected bool _baseTextureDirty;

	protected bool _eyesDirty;

	protected bool _skinDirty;

	protected bool _shoesDirty;

	protected bool _shirtDirty;

	protected bool _pantsDirty;

	public Rectangle shirtSourceRect;

	public Rectangle hairstyleSourceRect;

	public Rectangle hatSourceRect;

	public Rectangle accessorySourceRect;

	public Vector2 rotationAdjustment;

	public Vector2 positionOffset;

	[XmlIgnore]
	public NetFields NetFields { get; } = new NetFields("FarmerRenderer");

	public FarmerRenderer()
	{
		NetFields.SetOwner(this).AddField(textureName, "textureName").AddField(heightOffset, "heightOffset")
			.AddField(eyes, "eyes")
			.AddField(skin, "skin")
			.AddField(shoes, "shoes")
			.AddField(shirt, "shirt")
			.AddField(pants, "pants");
		farmerTextureManager = Game1.content.CreateTemporary();
		textureName.fieldChangeVisibleEvent += delegate
		{
			_spriteDirty = true;
			_baseTextureDirty = true;
		};
		eyes.fieldChangeVisibleEvent += delegate
		{
			_spriteDirty = true;
			_eyesDirty = true;
		};
		skin.fieldChangeVisibleEvent += delegate
		{
			_spriteDirty = true;
			_skinDirty = true;
			_shirtDirty = true;
		};
		shoes.fieldChangeVisibleEvent += delegate
		{
			_spriteDirty = true;
			_shoesDirty = true;
		};
		shirt.fieldChangeVisibleEvent += delegate
		{
			_spriteDirty = true;
			_shirtDirty = true;
		};
		pants.fieldChangeVisibleEvent += delegate
		{
			_spriteDirty = true;
			_pantsDirty = true;
		};
		_spriteDirty = true;
		_baseTextureDirty = true;
	}

	public FarmerRenderer(string textureName, Farmer farmer)
		: this()
	{
		eyes.Set(farmer.newEyeColor.Value);
		this.textureName.Set(textureName);
		_spriteDirty = true;
		_baseTextureDirty = true;
	}

	public bool isAccessoryFacialHair(int which)
	{
		if (which >= 6)
		{
			if (which >= 19)
			{
				return which <= 22;
			}
			return false;
		}
		return true;
	}

	public bool drawAccessoryBelowHair(int which)
	{
		if (which >= 8)
		{
			return isAccessoryFacialHair(which);
		}
		return true;
	}

	private void executeRecolorActions(Farmer farmer)
	{
		if (_spriteDirty)
		{
			_spriteDirty = false;
			if (_baseTextureDirty)
			{
				_baseTextureDirty = false;
				textureChanged();
				_eyesDirty = true;
				_shoesDirty = true;
				_pantsDirty = true;
				_skinDirty = true;
				_shirtDirty = true;
			}
			if (recolorOffsets == null)
			{
				recolorOffsets = new Dictionary<string, Dictionary<int, List<int>>>();
			}
			if (!recolorOffsets.ContainsKey(textureName.Value))
			{
				recolorOffsets[textureName.Value] = new Dictionary<int, List<int>>();
				Texture2D sourceTexture = farmerTextureManager.Load<Texture2D>(textureName.Value);
				Color[] sourcePixelData = new Color[sourceTexture.Width * sourceTexture.Height];
				sourceTexture.GetData(sourcePixelData);
				_GeneratePixelIndices(256, textureName.Value, sourcePixelData);
				_GeneratePixelIndices(257, textureName.Value, sourcePixelData);
				_GeneratePixelIndices(258, textureName.Value, sourcePixelData);
				_GeneratePixelIndices(268, textureName.Value, sourcePixelData);
				_GeneratePixelIndices(269, textureName.Value, sourcePixelData);
				_GeneratePixelIndices(270, textureName.Value, sourcePixelData);
				_GeneratePixelIndices(271, textureName.Value, sourcePixelData);
				_GeneratePixelIndices(260, textureName.Value, sourcePixelData);
				_GeneratePixelIndices(261, textureName.Value, sourcePixelData);
				_GeneratePixelIndices(262, textureName.Value, sourcePixelData);
				_GeneratePixelIndices(276, textureName.Value, sourcePixelData);
				_GeneratePixelIndices(277, textureName.Value, sourcePixelData);
			}
			Color[] pixelData = new Color[baseTexture.Width * baseTexture.Height];
			baseTexture.GetData(pixelData);
			if (_eyesDirty)
			{
				_eyesDirty = false;
				ApplyEyeColor(textureName.Value, pixelData);
			}
			if (_skinDirty)
			{
				_skinDirty = false;
				ApplySkinColor(textureName.Value, pixelData);
			}
			if (_shoesDirty)
			{
				_shoesDirty = false;
				ApplyShoeColor(textureName.Value, pixelData);
			}
			if (_shirtDirty)
			{
				_shirtDirty = false;
				ApplySleeveColor(textureName.Value, pixelData, farmer);
			}
			if (_pantsDirty)
			{
				_pantsDirty = false;
			}
			baseTexture.SetData(pixelData);
		}
	}

	protected void _GeneratePixelIndices(int source_color_index, string texture_name, Color[] pixels)
	{
		Color sourceColor = pixels[source_color_index];
		List<int> pixelIndices = new List<int>();
		for (int i = 0; i < pixels.Length; i++)
		{
			if (pixels[i].PackedValue == sourceColor.PackedValue)
			{
				pixelIndices.Add(i);
			}
		}
		recolorOffsets[texture_name][source_color_index] = pixelIndices;
	}

	public void unload()
	{
		farmerTextureManager.Unload();
		farmerTextureManager.Dispose();
	}

	public void textureChanged()
	{
		if (baseTexture != null)
		{
			baseTexture.Dispose();
			baseTexture = null;
		}
		Texture2D sourceTexture = farmerTextureManager.Load<Texture2D>(textureName.Value);
		baseTexture = new Texture2D(Game1.graphics.GraphicsDevice, sourceTexture.GetActualWidth(), sourceTexture.GetActualHeight())
		{
			Name = "@FarmerRenderer.baseTexture"
		};
		Color[] data = new Color[sourceTexture.GetElementCount()];
		sourceTexture.GetData(data, 0, data.Length);
		baseTexture.SetData(data);
	}

	public void recolorEyes(Color lightestColor)
	{
		eyes.Set(lightestColor);
	}

	public void ApplyEyeColor(string texture_name, Color[] pixels)
	{
		Color lightestColor = eyes.Value;
		Color darkerColor = changeBrightness(lightestColor, -75);
		if (lightestColor.Equals(darkerColor))
		{
			lightestColor.B += 10;
		}
		_SwapColor(texture_name, pixels, 276, lightestColor);
		_SwapColor(texture_name, pixels, 277, darkerColor);
	}

	private void _SwapColor(string texture_name, Color[] pixels, int color_index, Color color)
	{
		foreach (int pixelOffset in recolorOffsets[texture_name][color_index])
		{
			pixels[pixelOffset] = color;
		}
	}

	public void recolorShoes(string which)
	{
		shoes.Set(which);
	}

	private void ApplyShoeColor(string texture_name, Color[] pixels)
	{
		int which = 12;
		Texture2D texture = null;
		int splitIndex = shoes.Value.LastIndexOf(':');
		if (splitIndex > -1)
		{
			string texturePath = shoes.Value.Substring(0, splitIndex);
			string index = shoes.Value.Substring(splitIndex + 1);
			try
			{
				texture = farmerTextureManager.Load<Texture2D>(texturePath);
				if (!int.TryParse(index, out which))
				{
					which = 12;
				}
			}
			catch (Exception)
			{
				texture = farmerTextureManager.Load<Texture2D>("Characters\\Farmer\\shoeColors");
			}
		}
		else if (!int.TryParse(shoes.Value, out which))
		{
			which = 12;
		}
		if (texture == null)
		{
			texture = farmerTextureManager.Load<Texture2D>("Characters\\Farmer\\shoeColors");
		}
		Texture2D shoeColors = texture;
		if (which >= shoeColors.Height)
		{
			which = shoeColors.Height - 1;
		}
		if (shoeColors.Width >= 4)
		{
			Color[] shoeColorsData = new Color[shoeColors.Width * shoeColors.Height];
			shoeColors.GetData(shoeColorsData);
			Color darkest = shoeColorsData[which * 4 % (shoeColors.Height * 4)];
			Color medium = shoeColorsData[which * 4 % (shoeColors.Height * 4) + 1];
			Color lightest = shoeColorsData[which * 4 % (shoeColors.Height * 4) + 2];
			Color lightest2 = shoeColorsData[which * 4 % (shoeColors.Height * 4) + 3];
			_SwapColor(texture_name, pixels, 268, darkest);
			_SwapColor(texture_name, pixels, 269, medium);
			_SwapColor(texture_name, pixels, 270, lightest);
			_SwapColor(texture_name, pixels, 271, lightest2);
		}
	}

	public int recolorSkin(int which, bool force = false)
	{
		if (force)
		{
			skin.Value = -1;
		}
		skin.Set(which);
		return which;
	}

	private void ApplySkinColor(string texture_name, Color[] pixels)
	{
		int which = skin.Value;
		Texture2D skinColors = farmerTextureManager.Load<Texture2D>("Characters\\Farmer\\skinColors");
		Color[] skinColorsData = new Color[skinColors.Width * skinColors.Height];
		if (which < 0)
		{
			which = skinColors.Height - 1;
		}
		if (which > skinColors.Height - 1)
		{
			which = 0;
		}
		skinColors.GetData(skinColorsData);
		Color darkest = skinColorsData[which * 3 % (skinColors.Height * 3)];
		Color medium = skinColorsData[which * 3 % (skinColors.Height * 3) + 1];
		Color lightest = skinColorsData[which * 3 % (skinColors.Height * 3) + 2];
		if (skin.Value == -12345)
		{
			darkest = (medium = (lightest = Color.Transparent));
		}
		_SwapColor(texture_name, pixels, 260, darkest);
		_SwapColor(texture_name, pixels, 261, medium);
		_SwapColor(texture_name, pixels, 262, lightest);
	}

	public void changeShirt(string whichShirt)
	{
		shirt.Set(whichShirt);
	}

	public void changePants(string whichPants)
	{
		pants.Set(whichPants);
	}

	public void MarkSpriteDirty()
	{
		_spriteDirty = true;
		_shirtDirty = true;
		_pantsDirty = true;
		_eyesDirty = true;
		_shoesDirty = true;
		_baseTextureDirty = true;
	}

	public void ApplySleeveColor(string texture_name, Color[] pixels, Farmer who)
	{
		who.GetDisplayShirt(out var shirtTexture, out var shirtIndex);
		Color[] shirtData = new Color[shirtTexture.Bounds.Width * shirtTexture.Bounds.Height];
		shirtTexture.GetData(shirtData);
		int index = shirtIndex * 8 / 128 * 32 * shirtTexture.Bounds.Width + shirtIndex * 8 % 128 + shirtTexture.Width * 4;
		int dyeIndex = index + 128;
		if (!who.ShirtHasSleeves() || index >= shirtData.Length || (skin.Value == -12345 && who.shirtItem.Value == null))
		{
			Texture2D skinColors = farmerTextureManager.Load<Texture2D>("Characters\\Farmer\\skinColors");
			Color[] skinColorsData = new Color[skinColors.Width * skinColors.Height];
			int skinIndex = skin.Value;
			if (skinIndex < 0)
			{
				skinIndex = skinColors.Height - 1;
			}
			if (skinIndex > skinColors.Height - 1)
			{
				skinIndex = 0;
			}
			skinColors.GetData(skinColorsData);
			Color darkest = skinColorsData[skinIndex * 3 % (skinColors.Height * 3)];
			Color medium = skinColorsData[skinIndex * 3 % (skinColors.Height * 3) + 1];
			Color lightest = skinColorsData[skinIndex * 3 % (skinColors.Height * 3) + 2];
			if (skin.Value == -12345)
			{
				darkest = pixels[260 + baseTexture.Width * 2];
				medium = pixels[261 + baseTexture.Width * 2];
				lightest = pixels[262 + baseTexture.Width * 2];
			}
			if (_sickFrame)
			{
				darkest = pixels[260 + baseTexture.Width];
				medium = pixels[261 + baseTexture.Width];
				lightest = pixels[262 + baseTexture.Width];
			}
			_SwapColor(texture_name, pixels, 256, darkest);
			_SwapColor(texture_name, pixels, 257, medium);
			_SwapColor(texture_name, pixels, 258, lightest);
		}
		else
		{
			Color color = Utility.MakeCompletelyOpaque(who.GetShirtColor());
			Color shirtSleeveColor = shirtData[dyeIndex];
			Color clothesColor = color;
			if (shirtSleeveColor.A < byte.MaxValue)
			{
				shirtSleeveColor = shirtData[index];
				clothesColor = Color.White;
			}
			shirtSleeveColor = Utility.MultiplyColor(shirtSleeveColor, clothesColor);
			_SwapColor(texture_name, pixels, 256, shirtSleeveColor);
			shirtSleeveColor = shirtData[dyeIndex - shirtTexture.Width];
			if (shirtSleeveColor.A < byte.MaxValue)
			{
				shirtSleeveColor = shirtData[index - shirtTexture.Width];
				clothesColor = Color.White;
			}
			shirtSleeveColor = Utility.MultiplyColor(shirtSleeveColor, clothesColor);
			_SwapColor(texture_name, pixels, 257, shirtSleeveColor);
			shirtSleeveColor = shirtData[dyeIndex - shirtTexture.Width * 2];
			if (shirtSleeveColor.A < byte.MaxValue)
			{
				shirtSleeveColor = shirtData[index - shirtTexture.Width * 2];
				clothesColor = Color.White;
			}
			shirtSleeveColor = Utility.MultiplyColor(shirtSleeveColor, clothesColor);
			_SwapColor(texture_name, pixels, 258, shirtSleeveColor);
		}
	}

	public static Color changeBrightness(Color c, int brightness)
	{
		c.R = (byte)Math.Min(255, Math.Max(0, c.R + brightness));
		c.G = (byte)Math.Min(255, Math.Max(0, c.G + brightness));
		c.B = (byte)Math.Min(255, Math.Max(0, c.B + ((brightness > 0) ? (brightness * 5 / 6) : (brightness * 8 / 7))));
		return c;
	}

	public void draw(SpriteBatch b, Farmer who, int whichFrame, Vector2 position, float layerDepth = 1f, bool flip = false)
	{
		who.FarmerSprite.setCurrentSingleFrame(whichFrame, 32000, secondaryArm: false, flip);
		draw(b, who.FarmerSprite, who.FarmerSprite.SourceRect, position, Vector2.Zero, layerDepth, Color.White, 0f, who);
	}

	public void draw(SpriteBatch b, FarmerSprite farmerSprite, Rectangle sourceRect, Vector2 position, Vector2 origin, float layerDepth, Color overrideColor, float rotation, Farmer who)
	{
		draw(b, farmerSprite.CurrentAnimationFrame, farmerSprite.CurrentFrame, sourceRect, position, origin, layerDepth, overrideColor, rotation, 1f, who);
	}

	public void drawMiniPortrat(SpriteBatch b, Vector2 position, float layerDepth, float scale, int facingDirection, Farmer who, float alpha = 1f)
	{
		int hairStyle = who.getHair(ignore_hat: true);
		executeRecolorActions(who);
		facingDirection = 2;
		bool flip = false;
		int yOffset = 0;
		int featureYOffset = 0;
		HairStyleMetadata hairMetadata = Farmer.GetHairStyleMetadata(who.hair.Value);
		Texture2D hairTexture = hairMetadata?.texture ?? hairStylesTexture;
		hairstyleSourceRect = ((hairMetadata != null) ? new Rectangle(hairMetadata.tileX * 16, hairMetadata.tileY * 16, 16, 15) : new Rectangle(hairStyle * 16 % hairStylesTexture.Width, hairStyle * 16 / hairStylesTexture.Width * 96, 16, 15));
		if (facingDirection == 2)
		{
			yOffset = 0;
			hairstyleSourceRect.Offset(0, 0);
			featureYOffset = featureYOffsetPerFrame[0];
		}
		b.Draw(baseTexture, position, new Rectangle(0, yOffset, 16, who.IsMale ? 15 : 16), Color.White * alpha, 0f, Vector2.Zero, scale, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Base));
		Color hairColor = (who.prismaticHair.Value ? Utility.GetPrismaticColor() : who.hairstyleColor.Value);
		b.Draw(hairTexture, position + new Vector2(0f, featureYOffset * 4 + ((who.IsMale && who.hair.Value >= 16) ? (-4) : ((!who.IsMale && who.hair.Value < 16) ? 4 : 0))) * scale / 4f, hairstyleSourceRect, hairColor * alpha, 0f, Vector2.Zero, scale, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hair));
	}

	public void draw(SpriteBatch b, FarmerSprite.AnimationFrame animationFrame, int currentFrame, Rectangle sourceRect, Vector2 position, Vector2 origin, float layerDepth, Color overrideColor, float rotation, float scale, Farmer who)
	{
		draw(b, animationFrame, currentFrame, sourceRect, position, origin, layerDepth, who.FacingDirection, overrideColor, rotation, scale, who);
	}

	public void drawHairAndAccesories(SpriteBatch b, int facingDirection, Farmer who, Vector2 position, Vector2 origin, float scale, int currentFrame, float rotation, Color overrideColor, float layerDepth)
	{
		int hairStyle = who.getHair();
		float scaledPixelZoom = 4f * scale;
		int frameXOffset = featureXOffsetPerFrame[currentFrame];
		int frameYOffset = featureYOffsetPerFrame[currentFrame];
		HairStyleMetadata hairMetadata = Farmer.GetHairStyleMetadata(hairStyle);
		Hat value = who.hat.Value;
		if (value != null && value.hairDrawType.Value == 1 && hairMetadata != null && hairMetadata.coveredIndex != -1)
		{
			hairStyle = hairMetadata.coveredIndex;
			hairMetadata = Farmer.GetHairStyleMetadata(hairStyle);
		}
		executeRecolorActions(who);
		who.GetDisplayShirt(out var shirtTexture, out var shirtIndex);
		Color hairColor = (who.prismaticHair.Value ? Utility.GetPrismaticColor() : who.hairstyleColor.Value);
		shirtSourceRect = new Rectangle(shirtIndex * 8 % 128, shirtIndex * 8 / 128 * 32, 8, 8);
		Texture2D hairTexture = hairMetadata?.texture ?? hairStylesTexture;
		hairstyleSourceRect = ((hairMetadata != null) ? new Rectangle(hairMetadata.tileX * 16, hairMetadata.tileY * 16, 16, 32) : new Rectangle(hairStyle * 16 % hairStylesTexture.Width, hairStyle * 16 / hairStylesTexture.Width * 96, 16, 32));
		if (who.accessory.Value >= 0)
		{
			accessorySourceRect = new Rectangle(who.accessory.Value * 16 % accessoriesTexture.Width, who.accessory.Value * 16 / accessoriesTexture.Width * 32, 16, 16);
		}
		Texture2D hatTexture = hatsTexture;
		bool isErrorHat = false;
		if (who.hat.Value != null)
		{
			ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(who.hat.Value.QualifiedItemId);
			int spriteIndex = itemData.SpriteIndex;
			hatTexture = itemData.GetTexture();
			hatSourceRect = new Rectangle(20 * spriteIndex % hatTexture.Width, 20 * spriteIndex / hatTexture.Width * 20 * 4, 20, 20);
			if (itemData.IsErrorItem)
			{
				hatSourceRect = itemData.GetSourceRect(0, null);
				isErrorHat = true;
			}
		}
		FarmerSpriteLayers accessoryLayer = FarmerSpriteLayers.Accessory;
		if (who.accessory.Value >= 0 && drawAccessoryBelowHair(who.accessory.Value))
		{
			accessoryLayer = FarmerSpriteLayers.AccessoryUnderHair;
		}
		switch (facingDirection)
		{
		case 0:
		{
			shirtSourceRect.Offset(0, 24);
			hairstyleSourceRect.Offset(0, 64);
			Rectangle dyedShirtSourceRect = shirtSourceRect;
			dyedShirtSourceRect.Offset(128, 0);
			if (!isErrorHat && who.hat.Value != null)
			{
				hatSourceRect.Offset(0, 60);
			}
			if (!who.bathingClothes.Value && (skin.Value != -12345 || who.shirtItem.Value != null))
			{
				Vector2 shirtPosition3 = position + origin + positionOffset + new Vector2(16f * scale + (float)(frameXOffset * 4), (float)(56 + frameYOffset * 4) + (float)heightOffset.Value * scale);
				b.Draw(shirtTexture, shirtPosition3, shirtSourceRect, overrideColor.Equals(Color.White) ? Color.White : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt));
				b.Draw(shirtTexture, shirtPosition3, dyedShirtSourceRect, overrideColor.Equals(Color.White) ? Utility.MakeCompletelyOpaque(who.GetShirtColor()) : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt, dyeLayer: true));
			}
			b.Draw(hairTexture, position + origin + positionOffset + new Vector2(frameXOffset * 4, frameYOffset * 4 + 4 + ((who.IsMale && hairStyle >= 16) ? (-4) : ((!who.IsMale && hairStyle < 16) ? 4 : 0))), hairstyleSourceRect, overrideColor.Equals(Color.White) ? hairColor : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hair));
			break;
		}
		case 1:
		{
			shirtSourceRect.Offset(0, 8);
			hairstyleSourceRect.Offset(0, 32);
			Rectangle dyedShirtSourceRect = shirtSourceRect;
			dyedShirtSourceRect.Offset(128, 0);
			if (!isErrorHat && who.hat.Value != null)
			{
				hatSourceRect.Offset(0, 20);
			}
			if (rotation != -(float)Math.PI / 32f)
			{
				if (rotation == (float)Math.PI / 32f)
				{
					rotationAdjustment.X = -6f;
					rotationAdjustment.Y = 1f;
				}
			}
			else
			{
				rotationAdjustment.X = 6f;
				rotationAdjustment.Y = -2f;
			}
			if (!who.bathingClothes.Value && (skin.Value != -12345 || who.shirtItem.Value != null))
			{
				Vector2 shirtPosition4 = position + origin + positionOffset + rotationAdjustment + new Vector2(16f * scale + (float)(frameXOffset * 4), 56f * scale + (float)(frameYOffset * 4) + (float)heightOffset.Value * scale);
				b.Draw(shirtTexture, shirtPosition4, shirtSourceRect, overrideColor.Equals(Color.White) ? Color.White : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt));
				b.Draw(shirtTexture, shirtPosition4, dyedShirtSourceRect, overrideColor.Equals(Color.White) ? Utility.MakeCompletelyOpaque(who.GetShirtColor()) : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt, dyeLayer: true));
			}
			if (who.accessory.Value >= 0)
			{
				accessorySourceRect.Offset(0, 16);
				b.Draw(accessoriesTexture, position + origin + positionOffset + rotationAdjustment + new Vector2(frameXOffset * 4, 4 + frameYOffset * 4 + heightOffset.Value), accessorySourceRect, (overrideColor.Equals(Color.White) && isAccessoryFacialHair(who.accessory.Value)) ? hairColor : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, accessoryLayer));
			}
			b.Draw(hairTexture, position + origin + positionOffset + new Vector2(frameXOffset * 4, frameYOffset * 4 + ((who.IsMale && who.hair.Value >= 16) ? (-4) : ((!who.IsMale && who.hair.Value < 16) ? 4 : 0))), hairstyleSourceRect, overrideColor.Equals(Color.White) ? hairColor : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hair));
			break;
		}
		case 2:
		{
			Rectangle dyedShirtSourceRect = shirtSourceRect;
			dyedShirtSourceRect.Offset(128, 0);
			if (!who.bathingClothes.Value && (skin.Value != -12345 || who.shirtItem.Value != null))
			{
				Vector2 shirtPosition2 = position + origin + positionOffset + new Vector2(16 + frameXOffset * 4, (float)(56 + frameYOffset * 4) + (float)heightOffset.Value * scale);
				b.Draw(shirtTexture, shirtPosition2, shirtSourceRect, overrideColor.Equals(Color.White) ? Color.White : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt));
				b.Draw(shirtTexture, shirtPosition2, dyedShirtSourceRect, overrideColor.Equals(Color.White) ? Utility.MakeCompletelyOpaque(who.GetShirtColor()) : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt, dyeLayer: true));
			}
			if (who.accessory.Value >= 0)
			{
				if (who.accessory.Value == 26)
				{
					switch (currentFrame)
					{
					case 24:
					case 25:
					case 26:
					case 70:
						positionOffset.Y += 4f;
						break;
					}
				}
				b.Draw(accessoriesTexture, position + origin + positionOffset + rotationAdjustment + new Vector2(frameXOffset * 4, 8 + frameYOffset * 4 + heightOffset.Value - 4), accessorySourceRect, (overrideColor.Equals(Color.White) && isAccessoryFacialHair(who.accessory.Value)) ? hairColor : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, accessoryLayer));
			}
			b.Draw(hairTexture, position + origin + positionOffset + new Vector2(frameXOffset * 4, frameYOffset * 4 + ((who.IsMale && who.hair.Value >= 16) ? (-4) : ((!who.IsMale && who.hair.Value < 16) ? 4 : 0))), hairstyleSourceRect, overrideColor.Equals(Color.White) ? hairColor : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hair));
			break;
		}
		case 3:
		{
			bool flip = true;
			shirtSourceRect.Offset(0, 16);
			Rectangle dyedShirtSourceRect = shirtSourceRect;
			dyedShirtSourceRect.Offset(128, 0);
			if (hairMetadata != null && hairMetadata.usesUniqueLeftSprite)
			{
				flip = false;
				hairstyleSourceRect.Offset(0, 96);
			}
			else
			{
				hairstyleSourceRect.Offset(0, 32);
			}
			if (!isErrorHat && who.hat.Value != null)
			{
				hatSourceRect.Offset(0, 40);
			}
			if (rotation != -(float)Math.PI / 32f)
			{
				if (rotation == (float)Math.PI / 32f)
				{
					rotationAdjustment.X = -5f;
					rotationAdjustment.Y = 1f;
				}
			}
			else
			{
				rotationAdjustment.X = 6f;
				rotationAdjustment.Y = -2f;
			}
			if (!who.bathingClothes.Value && (skin.Value != -12345 || who.shirtItem.Value != null))
			{
				Vector2 shirtPosition = position + origin + positionOffset + rotationAdjustment + new Vector2(16f * scale - (float)(frameXOffset * 4), 56f * scale + (float)(frameYOffset * 4) + (float)heightOffset.Value * scale);
				b.Draw(shirtTexture, shirtPosition, shirtSourceRect, overrideColor.Equals(Color.White) ? Color.White : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt));
				b.Draw(shirtTexture, shirtPosition, dyedShirtSourceRect, overrideColor.Equals(Color.White) ? Utility.MakeCompletelyOpaque(who.GetShirtColor()) : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt, dyeLayer: true));
			}
			if (who.accessory.Value >= 0)
			{
				accessorySourceRect.Offset(0, 16);
				b.Draw(accessoriesTexture, position + origin + positionOffset + rotationAdjustment + new Vector2(-frameXOffset * 4, 4 + frameYOffset * 4 + heightOffset.Value), accessorySourceRect, (overrideColor.Equals(Color.White) && isAccessoryFacialHair(who.accessory.Value)) ? hairColor : overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.FlipHorizontally, GetLayerDepth(layerDepth, accessoryLayer));
			}
			b.Draw(hairTexture, position + origin + positionOffset + new Vector2(-frameXOffset * 4, frameYOffset * 4 + ((who.IsMale && who.hair.Value >= 16) ? (-4) : ((!who.IsMale && who.hair.Value < 16) ? 4 : 0))), hairstyleSourceRect, overrideColor.Equals(Color.White) ? hairColor : overrideColor, rotation, origin, scaledPixelZoom, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hair));
			break;
		}
		}
		if (who.hat.Value != null && !who.bathingClothes.Value)
		{
			bool flip2 = who.FarmerSprite.CurrentAnimationFrame.flip;
			int hatOffset = ((!who.hat.Value.ignoreHairstyleOffset.Value) ? hairstyleHatOffset[who.hair.Value % 16] : 0);
			Vector2 hatPosition = position + origin + positionOffset + new Vector2((0f - scaledPixelZoom) * 2f + (float)(((!flip2) ? 1 : (-1)) * frameXOffset) * scaledPixelZoom, (0f - scaledPixelZoom) * 4f + (float)(frameYOffset * 4) + (float)hatOffset + 4f + (float)heightOffset.Value);
			Color hatColor = (who.hat.Value.isPrismatic.Value ? Utility.GetPrismaticColor() : overrideColor);
			if (!isErrorHat && who.hat.Value.isMask && facingDirection == 0)
			{
				Rectangle maskDrawRect = hatSourceRect;
				maskDrawRect.Height -= 11;
				maskDrawRect.Y += 11;
				b.Draw(hatTexture, position + origin + positionOffset + new Vector2(0f, 11f * scaledPixelZoom) + new Vector2((0f - scaledPixelZoom) * 2f + (float)(((!flip2) ? 1 : (-1)) * frameXOffset * 4), -16 + frameYOffset * 4 + hatOffset + 4 + heightOffset.Value), maskDrawRect, overrideColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hat));
				maskDrawRect = hatSourceRect;
				maskDrawRect.Height = 11;
				b.Draw(hatTexture, hatPosition, maskDrawRect, hatColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.HatMaskUp));
			}
			else
			{
				b.Draw(hatTexture, hatPosition, hatSourceRect, hatColor, rotation, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hat));
			}
		}
	}

	public static float GetLayerDepth(float baseLayerDepth, FarmerSpriteLayers layer, bool dyeLayer = false)
	{
		if (layer == FarmerSpriteLayers.TOOL_IN_USE_SIDE)
		{
			return baseLayerDepth + 0.0032f;
		}
		int sortDirection = ((!Game1.isUsingBackToFrontSorting) ? 1 : (-1));
		if (dyeLayer)
		{
			baseLayerDepth += 1E-07f * (float)sortDirection;
		}
		return baseLayerDepth + (float)layer * 1E-06f * (float)sortDirection;
	}

	public void draw(SpriteBatch b, FarmerSprite.AnimationFrame animationFrame, int currentFrame, Rectangle sourceRect, Vector2 position, Vector2 origin, float layerDepth, int facingDirection, Color overrideColor, float rotation, float scale, Farmer who)
	{
		float scaledPixelZoom = 4f * scale;
		int frameXOffset = featureXOffsetPerFrame[currentFrame];
		int frameYOffset = featureYOffsetPerFrame[currentFrame];
		bool sickFrame = currentFrame == 104 || currentFrame == 105;
		if (_sickFrame != sickFrame)
		{
			_sickFrame = sickFrame;
			_shirtDirty = true;
			_spriteDirty = true;
		}
		executeRecolorActions(who);
		position = new Vector2((float)Math.Floor(position.X), (float)Math.Floor(position.Y));
		rotationAdjustment = Vector2.Zero;
		positionOffset.Y = animationFrame.positionOffset * 4;
		positionOffset.X = animationFrame.xOffset * 4;
		if (!isDrawingForUI && who.swimming.Value)
		{
			sourceRect.Height /= 2;
			sourceRect.Height -= (int)who.yOffset / 4;
			position.Y += 64f;
		}
		if (facingDirection == 3 || facingDirection == 1)
		{
			facingDirection = ((!animationFrame.flip) ? 1 : 3);
		}
		b.Draw(baseTexture, position + origin + positionOffset, sourceRect, overrideColor, rotation, origin, scaledPixelZoom, animationFrame.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Base));
		if (!isDrawingForUI && who.swimming.Value)
		{
			if (who.currentEyes != 0 && who.FacingDirection != 0 && (Game1.timeOfDay < 2600 || (who.isInBed.Value && who.timeWentToBed.Value != 0)) && ((!who.FarmerSprite.PauseForSingleAnimation && !who.UsingTool) || (who.UsingTool && who.CurrentTool is FishingRod)))
			{
				Vector2 eyePosition = position + origin + positionOffset + new Vector2(frameXOffset * 4 + 20 + ((who.FacingDirection == 1) ? 12 : ((who.FacingDirection == 3) ? 4 : 0)), frameYOffset * 4 + 40);
				b.Draw(baseTexture, eyePosition, new Rectangle(5, 16, (who.FacingDirection == 2) ? 6 : 2, 2), overrideColor, 0f, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.FaceSkin));
				b.Draw(baseTexture, eyePosition, new Rectangle(264 + ((who.FacingDirection == 3) ? 4 : 0), 2 + (who.currentEyes - 1) * 2, (who.FacingDirection == 2) ? 6 : 2, 2), overrideColor, 0f, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Eyes));
			}
			drawHairAndAccesories(b, facingDirection, who, position, origin, scale, currentFrame, rotation, overrideColor, layerDepth);
			b.Draw(Game1.staminaRect, new Rectangle((int)position.X + (int)who.yOffset + 8, (int)position.Y - 128 + sourceRect.Height * 4 + (int)origin.Y - (int)who.yOffset, sourceRect.Width * 4 - (int)who.yOffset * 2 - 16, 4), Game1.staminaRect.Bounds, Color.White * 0.75f, 0f, Vector2.Zero, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.SwimWaterRing));
			return;
		}
		who.GetDisplayPants(out var texture, out var pantsIndex);
		Rectangle pantsRect = new Rectangle(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height);
		pantsRect.X += pantsIndex % 10 * 192;
		pantsRect.Y += pantsIndex / 10 * 688;
		if (!who.IsMale)
		{
			pantsRect.X += 96;
		}
		if (skin.Value != -12345 || who.pantsItem.Value != null)
		{
			b.Draw(texture, position + origin + positionOffset, pantsRect, (overrideColor == Color.White) ? Utility.MakeCompletelyOpaque(who.GetPantsColor()) : overrideColor, rotation, origin, scaledPixelZoom, animationFrame.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth(layerDepth, (who.FarmerSprite.CurrentAnimationFrame.frame == 5) ? FarmerSpriteLayers.PantsPassedOut : FarmerSpriteLayers.Pants));
		}
		sourceRect.Offset(288, 0);
		if (who.currentEyes != 0 && facingDirection != 0 && (Game1.timeOfDay < 2600 || (who.isInBed.Value && who.timeWentToBed.Value != 0)) && ((!who.FarmerSprite.PauseForSingleAnimation && !who.UsingTool) || (who.UsingTool && who.CurrentTool is FishingRod)) && (!who.UsingTool || !(who.CurrentTool is FishingRod { isFishing: false })))
		{
			int xAdjustment = 5;
			xAdjustment = (animationFrame.flip ? (xAdjustment - frameXOffset) : (xAdjustment + frameXOffset));
			switch (facingDirection)
			{
			case 1:
				xAdjustment += 3;
				break;
			case 3:
				xAdjustment++;
				break;
			}
			xAdjustment *= 4;
			b.Draw(baseTexture, position + origin + positionOffset + new Vector2(xAdjustment, frameYOffset * 4 + ((who.IsMale && who.FacingDirection != 2) ? 36 : 40)), new Rectangle(5, 16, (facingDirection == 2) ? 6 : 2, 2), overrideColor, 0f, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.FaceSkin));
			b.Draw(baseTexture, position + origin + positionOffset + new Vector2(xAdjustment, frameYOffset * 4 + ((who.FacingDirection == 1 || who.FacingDirection == 3) ? 40 : 44)), new Rectangle(264 + ((facingDirection == 3) ? 4 : 0), 2 + (who.currentEyes - 1) * 2, (facingDirection == 2) ? 6 : 2, 2), overrideColor, 0f, origin, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Eyes));
		}
		drawHairAndAccesories(b, facingDirection, who, position, origin, scale, currentFrame, rotation, overrideColor, layerDepth);
		FarmerSpriteLayers armLayer = FarmerSpriteLayers.Arms;
		if (facingDirection == 0)
		{
			armLayer = FarmerSpriteLayers.ArmsUp;
		}
		if (animationFrame.armOffset > 0)
		{
			sourceRect.Offset(-288 + animationFrame.armOffset * 16, 0);
			b.Draw(baseTexture, position + origin + positionOffset + who.armOffset, sourceRect, overrideColor, rotation, origin, scaledPixelZoom, animationFrame.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth(layerDepth, armLayer));
		}
		if (!who.usingSlingshot || !(who.CurrentTool is Slingshot slingshot))
		{
			return;
		}
		Point point = Utility.Vector2ToPoint(slingshot.AdjustForHeight(Utility.PointToVector2(slingshot.aimPos.Value)));
		int mouseX = point.X;
		int y = point.Y;
		int backArmDistance = slingshot.GetBackArmDistance(who);
		Vector2 shootOrigin = slingshot.GetShootOrigin(who);
		float frontArmRotation = (float)Math.Atan2((float)y - shootOrigin.Y, (float)mouseX - shootOrigin.X) + (float)Math.PI;
		if (!Game1.options.useLegacySlingshotFiring)
		{
			frontArmRotation -= (float)Math.PI;
			if (frontArmRotation < 0f)
			{
				frontArmRotation += (float)Math.PI * 2f;
			}
		}
		switch (facingDirection)
		{
		case 0:
			b.Draw(baseTexture, position + new Vector2(4f + frontArmRotation * 8f, -44f), new Rectangle(173, 238, 9, 14), Color.White, 0f, new Vector2(4f, 11f), scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
			break;
		case 1:
		{
			b.Draw(baseTexture, position + new Vector2(52 - backArmDistance, -32f), new Rectangle(147, 237, 10, 4), Color.White, 0f, new Vector2(8f, 3f), scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
			b.Draw(baseTexture, position + new Vector2(36f, -44f), new Rectangle(156, 244, 9, 10), Color.White, frontArmRotation, new Vector2(0f, 3f), scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
			int slingshotAttachX = (int)(Math.Cos(frontArmRotation + (float)Math.PI / 2f) * (double)(20 - backArmDistance - 8) - Math.Sin(frontArmRotation + (float)Math.PI / 2f) * -68.0);
			int slingshotAttachY = (int)(Math.Sin(frontArmRotation + (float)Math.PI / 2f) * (double)(20 - backArmDistance - 8) + Math.Cos(frontArmRotation + (float)Math.PI / 2f) * -68.0);
			Utility.drawLineWithScreenCoordinates((int)(position.X + 52f - (float)backArmDistance), (int)(position.Y - 32f - 4f), (int)(position.X + 32f + (float)(slingshotAttachX / 2)), (int)(position.Y - 32f - 12f + (float)(slingshotAttachY / 2)), b, Color.White);
			break;
		}
		case 3:
		{
			b.Draw(baseTexture, position + new Vector2(40 + backArmDistance, -32f), new Rectangle(147, 237, 10, 4), Color.White, 0f, new Vector2(9f, 4f), scaledPixelZoom, SpriteEffects.FlipHorizontally, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
			b.Draw(baseTexture, position + new Vector2(24f, -40f), new Rectangle(156, 244, 9, 10), Color.White, frontArmRotation + (float)Math.PI, new Vector2(8f, 3f), scaledPixelZoom, SpriteEffects.FlipHorizontally, GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
			int slingshotAttachX = (int)(Math.Cos(frontArmRotation + (float)Math.PI * 2f / 5f) * (double)(20 + backArmDistance - 8) - Math.Sin(frontArmRotation + (float)Math.PI * 2f / 5f) * -68.0);
			int slingshotAttachY = (int)(Math.Sin(frontArmRotation + (float)Math.PI * 2f / 5f) * (double)(20 + backArmDistance - 8) + Math.Cos(frontArmRotation + (float)Math.PI * 2f / 5f) * -68.0);
			Utility.drawLineWithScreenCoordinates((int)(position.X + 4f + (float)backArmDistance), (int)(position.Y - 32f - 8f), (int)(position.X + 26f + (float)slingshotAttachX * 4f / 10f), (int)(position.Y - 32f - 8f + (float)slingshotAttachY * 4f / 10f), b, Color.White);
			break;
		}
		case 2:
			b.Draw(baseTexture, position + new Vector2(4f, -32 - backArmDistance / 2), new Rectangle(148, 244, 4, 4), Color.White, 0f, Vector2.Zero, scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Arms));
			Utility.drawLineWithScreenCoordinates((int)(position.X + 16f), (int)(position.Y - 28f - (float)(backArmDistance / 2)), (int)(position.X + 44f - frontArmRotation * 10f), (int)(position.Y - 16f - 8f), b, Color.White);
			Utility.drawLineWithScreenCoordinates((int)(position.X + 16f), (int)(position.Y - 28f - (float)(backArmDistance / 2)), (int)(position.X + 56f - frontArmRotation * 10f), (int)(position.Y - 16f - 8f), b, Color.White);
			b.Draw(baseTexture, position + new Vector2(44f - frontArmRotation * 10f, -16f), new Rectangle(167, 235, 7, 9), Color.White, 0f, new Vector2(3f, 5f), scaledPixelZoom, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot, dyeLayer: true));
			break;
		}
	}
}
