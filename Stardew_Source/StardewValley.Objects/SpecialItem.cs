using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;

namespace StardewValley.Objects;

public class SpecialItem : Item
{
	public const int skullKey = 4;

	public const int clubCard = 2;

	public const int specialCharm = 3;

	public const int backpack = 99;

	public const int magnifyingGlass = 5;

	public const int darkTalisman = 6;

	public const int magicInk = 7;

	[XmlElement("which")]
	public readonly NetInt which = new NetInt();

	/// <summary>The backing field for <see cref="P:StardewValley.Objects.SpecialItem.displayName" />.</summary>
	[XmlIgnore]
	private string _displayName;

	/// <inheritdoc />
	public override string TypeDefinitionId { get; } = "(O)";

	/// <summary>The cached value for <see cref="P:StardewValley.Objects.SpecialItem.DisplayName" />.</summary>
	[XmlIgnore]
	private string displayName
	{
		get
		{
			if (string.IsNullOrEmpty(_displayName))
			{
				switch (which.Value)
				{
				case 4:
					_displayName = Game1.content.LoadString("Strings\\StringsFromCSFiles:SpecialItem.cs.13088");
					break;
				case 2:
					_displayName = Game1.content.LoadString("Strings\\StringsFromCSFiles:SpecialItem.cs.13089");
					break;
				case 3:
					_displayName = Game1.content.LoadString("Strings\\Objects:SpecialCharm");
					break;
				case 6:
					_displayName = Game1.content.LoadString("Strings\\Objects:DarkTalisman");
					break;
				case 7:
					_displayName = Game1.content.LoadString("Strings\\Objects:MagicInk");
					break;
				case 5:
					_displayName = Game1.content.LoadString("Strings\\Objects:MagnifyingGlass");
					break;
				case 99:
					if (Game1.player.maxItems.Value == 36)
					{
						_displayName = Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8709");
					}
					else
					{
						_displayName = Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8708");
					}
					break;
				}
			}
			return _displayName;
		}
		set
		{
			if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(_displayName))
			{
				switch (which.Value)
				{
				case 4:
					_displayName = Game1.content.LoadString("Strings\\StringsFromCSFiles:SpecialItem.cs.13088");
					break;
				case 2:
					_displayName = Game1.content.LoadString("Strings\\StringsFromCSFiles:SpecialItem.cs.13089");
					break;
				case 3:
					_displayName = Game1.content.LoadString("Strings\\Objects:SpecialCharm");
					break;
				case 6:
					_displayName = Game1.content.LoadString("Strings\\Objects:DarkTalisman");
					break;
				case 5:
					_displayName = Game1.content.LoadString("Strings\\Objects:MagnifyingGlass");
					break;
				case 7:
					_displayName = Game1.content.LoadString("Strings\\Objects:MagicInk");
					break;
				case 99:
					if (Game1.player.maxItems.Value == 36)
					{
						_displayName = Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8709");
					}
					else
					{
						_displayName = Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8708");
					}
					break;
				}
			}
			else
			{
				_displayName = value;
			}
		}
	}

	/// <inheritdoc />
	[XmlIgnore]
	public override string DisplayName => displayName;

	/// <inheritdoc />
	[XmlIgnore]
	public override string Name
	{
		get
		{
			if (netName.Value.Length < 1 || netName.Value == "Error Item")
			{
				switch (which.Value)
				{
				case 4:
					return "Skull Key";
				case 2:
					return "Club Card";
				case 6:
					return Game1.content.LoadString("Strings\\Objects:DarkTalisman");
				case 7:
					return Game1.content.LoadString("Strings\\Objects:MagicInk");
				case 5:
					return Game1.content.LoadString("Strings\\Objects:MagnifyingGlass");
				case 3:
					return Game1.content.LoadString("Strings\\Objects:SpecialCharm");
				}
			}
			return netName.Value;
		}
		set
		{
			netName.Value = value;
		}
	}

	public SpecialItem()
	{
		which.Value = which.Value;
		if (netName.Value == "Error Item" || Name.Length < 1)
		{
			switch (which.Value)
			{
			case 4:
				displayName = Game1.content.LoadString("Strings\\StringsFromCSFiles:SpecialItem.cs.13088");
				break;
			case 2:
				displayName = Game1.content.LoadString("Strings\\StringsFromCSFiles:SpecialItem.cs.13089");
				break;
			case 6:
				displayName = Game1.content.LoadString("Strings\\Objects:DarkTalisman");
				break;
			case 7:
				displayName = Game1.content.LoadString("Strings\\Objects:MagicInk");
				break;
			case 5:
				displayName = Game1.content.LoadString("Strings\\Objects:MagnifyingGlass");
				break;
			case 3:
				displayName = Game1.content.LoadString("Strings\\Objects:SpecialCharm");
				break;
			}
		}
	}

	public SpecialItem(int which, string name = "")
		: this()
	{
		this.which.Value = which;
		Name = name;
		if (name.Length < 1)
		{
			switch (which)
			{
			case 4:
				Name = "Skull Key";
				break;
			case 2:
				Name = "Club Card";
				break;
			case 6:
				Name = Game1.content.LoadString("Strings\\Objects:DarkTalisman");
				break;
			case 7:
				Name = Game1.content.LoadString("Strings\\Objects:MagicInk");
				break;
			case 5:
				Name = Game1.content.LoadString("Strings\\Objects:MagnifyingGlass");
				break;
			case 3:
				Name = Game1.content.LoadString("Strings\\Objects:SpecialCharm");
				break;
			}
		}
	}

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(which, "which");
	}

	public void actionWhenReceived(Farmer who)
	{
		switch (which.Value)
		{
		case 4:
			who.hasSkullKey = true;
			who.addQuest("19");
			break;
		case 6:
			who.hasDarkTalisman = true;
			break;
		case 7:
			who.hasMagicInk = true;
			break;
		case 5:
			who.hasMagnifyingGlass = true;
			break;
		case 3:
			who.hasSpecialCharm = true;
			break;
		}
	}

	public TemporaryAnimatedSprite getTemporarySpriteForHoldingUp(Vector2 position)
	{
		if (which.Value == 99)
		{
			return new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle((Game1.player.maxItems.Value == 36) ? 268 : 257, 1436, (Game1.player.maxItems.Value == 36) ? 11 : 9, 13), position + new Vector2(16f, 0f), flipped: false, 0f, Color.White)
			{
				scale = 4f,
				layerDepth = 1f
			};
		}
		return new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(129 + 16 * which.Value, 320, 16, 16), position, flipped: false, 0f, Color.White)
		{
			layerDepth = 1f
		};
	}

	public override string checkForSpecialItemHoldUpMeessage()
	{
		switch (which.Value)
		{
		case 2:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:SpecialItem.cs.13090", displayName);
		case 4:
			return Game1.content.LoadString("Strings\\StringsFromCSFiles:SpecialItem.cs.13092", displayName);
		case 6:
			return Game1.content.LoadString("Strings\\Objects:DarkTalismanDescription", displayName);
		case 7:
			return Game1.content.LoadString("Strings\\Objects:MagicInkDescription", displayName);
		case 5:
			return Game1.content.LoadString("Strings\\Objects:MagnifyingGlassDescription", displayName);
		case 3:
			return Game1.content.LoadString("Strings\\Objects:SpecialCharmDescription", displayName);
		default:
			if (which.Value == 99)
			{
				return Game1.content.LoadString("Strings\\StringsFromCSFiles:SpecialItem.cs.13094", displayName, Game1.player.maxItems);
			}
			return base.checkForSpecialItemHoldUpMeessage();
		}
	}

	public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
	{
	}

	public override int maximumStackSize()
	{
		return 1;
	}

	public override string getDescription()
	{
		return null;
	}

	public override bool isPlaceable()
	{
		return false;
	}

	/// <inheritdoc />
	protected override Item GetOneNew()
	{
		throw new NotImplementedException();
	}
}
