using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Extensions;
using StardewValley.Logging;

namespace StardewValley.Menus;

public class ChatBox : IClickableMenu
{
	public const int chatMessage = 0;

	public const int errorMessage = 1;

	public const int userNotificationMessage = 2;

	public const int privateMessage = 3;

	public const int defaultMaxMessages = 10;

	public const int timeToDisplayMessages = 600;

	public const int chatboxWidth = 896;

	public const int chatboxHeight = 56;

	public const int region_chatBox = 101;

	public const int region_emojiButton = 102;

	public ChatTextBox chatBox;

	public ClickableComponent chatBoxCC;

	/// <summary>A logger which copies messages to the chat box, used when entering commands through the chat.</summary>
	private readonly IGameLogger CheatCommandChatLogger;

	public List<ChatMessage> messages = new List<ChatMessage>();

	private KeyboardState oldKBState;

	private List<string> cheatHistory = new List<string>();

	private int cheatHistoryPosition = -1;

	public int maxMessages = 10;

	public static Texture2D emojiTexture;

	public ClickableTextureComponent emojiMenuIcon;

	public EmojiMenu emojiMenu;

	public bool choosingEmoji;

	private long lastReceivedPrivateMessagePlayerId;

	public ChatBox()
	{
		CheatCommandChatLogger = new CheatCommandChatLogger(this);
		Texture2D chatboxTexture = Game1.content.Load<Texture2D>("LooseSprites\\chatBox");
		chatBox = new ChatTextBox(chatboxTexture, null, Game1.smallFont, Color.White);
		chatBox.OnEnterPressed += textBoxEnter;
		chatBox.TitleText = "Chat";
		chatBoxCC = new ClickableComponent(new Rectangle(chatBox.X, chatBox.Y, chatBox.Width, chatBox.Height), "")
		{
			myID = 101
		};
		Game1.keyboardDispatcher.Subscriber = chatBox;
		emojiTexture = Game1.content.Load<Texture2D>("LooseSprites\\emojis");
		emojiMenuIcon = new ClickableTextureComponent(new Rectangle(0, 0, 40, 36), emojiTexture, new Rectangle(0, 0, 9, 9), 4f)
		{
			myID = 102,
			leftNeighborID = 101
		};
		emojiMenu = new EmojiMenu(this, emojiTexture, chatboxTexture);
		chatBoxCC.rightNeighborID = 102;
		updatePosition();
		chatBox.Selected = false;
	}

	public override void snapToDefaultClickableComponent()
	{
		currentlySnappedComponent = getComponentWithID(101);
		snapCursorToCurrentSnappedComponent();
	}

	private void updatePosition()
	{
		chatBox.Width = 896;
		chatBox.Height = 56;
		width = chatBox.Width;
		height = chatBox.Height;
		xPositionOnScreen = 0;
		yPositionOnScreen = Game1.uiViewport.Height - chatBox.Height;
		Utility.makeSafe(ref xPositionOnScreen, ref yPositionOnScreen, chatBox.Width, chatBox.Height);
		chatBox.X = xPositionOnScreen;
		chatBox.Y = yPositionOnScreen;
		chatBoxCC.bounds = new Rectangle(chatBox.X, chatBox.Y, chatBox.Width, chatBox.Height);
		emojiMenuIcon.bounds.Y = chatBox.Y + 8;
		emojiMenuIcon.bounds.X = chatBox.Width - emojiMenuIcon.bounds.Width - 8;
		if (emojiMenu != null)
		{
			emojiMenu.xPositionOnScreen = emojiMenuIcon.bounds.Center.X - 146;
			emojiMenu.yPositionOnScreen = emojiMenuIcon.bounds.Y - 248;
		}
	}

	public virtual void textBoxEnter(string text_to_send)
	{
		if (text_to_send.Length < 1)
		{
			return;
		}
		if (text_to_send[0] == '/')
		{
			string text = ArgUtility.SplitBySpaceAndGet(text_to_send, 0);
			if (text != null && text.Length > 1)
			{
				runCommand(text_to_send.Substring(1));
				return;
			}
		}
		text_to_send = Program.sdk.FilterDirtyWords(text_to_send);
		Game1.multiplayer.sendChatMessage(LocalizedContentManager.CurrentLanguageCode, text_to_send, Multiplayer.AllPlayers);
		receiveChatMessage(Game1.player.UniqueMultiplayerID, 0, LocalizedContentManager.CurrentLanguageCode, text_to_send);
	}

	public virtual void textBoxEnter(TextBox sender)
	{
		bool include_color_information;
		if (sender is ChatTextBox box)
		{
			if (box.finalText.Count > 0)
			{
				include_color_information = true;
				string message = box.finalText[0].message;
				if (message != null && message.StartsWith('/'))
				{
					string text = ArgUtility.SplitBySpaceAndGet(box.finalText[0].message, 0);
					if (text != null && text.Length > 1)
					{
						include_color_information = false;
					}
				}
				if (box.finalText.Count != 1)
				{
					goto IL_00c8;
				}
				if (box.finalText[0].message != null || box.finalText[0].emojiIndex != -1)
				{
					string message2 = box.finalText[0].message;
					if (message2 == null || message2.Trim().Length != 0)
					{
						goto IL_00c8;
					}
				}
			}
			goto IL_00dc;
		}
		goto IL_00e9;
		IL_00e9:
		sender.Text = "";
		clickAway();
		return;
		IL_00dc:
		box.reset();
		cheatHistoryPosition = -1;
		goto IL_00e9;
		IL_00c8:
		string textToSend = ChatMessage.makeMessagePlaintext(box.finalText, include_color_information);
		textBoxEnter(textToSend);
		goto IL_00dc;
	}

	public virtual void addInfoMessage(string message)
	{
		receiveChatMessage(0L, 2, LocalizedContentManager.CurrentLanguageCode, message);
	}

	public virtual void globalInfoMessage(string messageKey, params string[] args)
	{
		if (Game1.IsMultiplayer)
		{
			Game1.multiplayer.globalChatInfoMessage(messageKey, args);
		}
		else
		{
			addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_" + messageKey, args));
		}
	}

	public virtual void addErrorMessage(string message)
	{
		receiveChatMessage(0L, 1, LocalizedContentManager.CurrentLanguageCode, message);
	}

	public virtual void listPlayers(bool otherPlayersOnly = false, bool onlineOnly = true)
	{
		addInfoMessage(Game1.content.LoadString("Strings\\UI:ChatCommands_ListOnlinePlayers"));
		IEnumerable<Farmer> enumerable;
		if (!onlineOnly)
		{
			enumerable = Game1.getAllFarmers();
		}
		else
		{
			IEnumerable<Farmer> onlineFarmers = Game1.getOnlineFarmers();
			enumerable = onlineFarmers;
		}
		foreach (Farmer f in enumerable)
		{
			if (!otherPlayersOnly || f.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
			{
				addInfoMessage(Game1.content.LoadString("Strings\\UI:ChatCommands_ListOnlinePlayersEntry", formattedUserNameLong(f)));
			}
		}
	}

	protected virtual void runCommand(string commandText)
	{
		if (!ChatCommands.TryHandle(ArgUtility.SplitBySpace(commandText), this) && (ChatCommands.AllowCheats || Game1.isRunningMacro))
		{
			cheat(commandText);
		}
	}

	public virtual void cheat(string command, bool isDebug = false)
	{
		string fullCommand = (isDebug ? "debug " : "") + command;
		Game1.debugOutput = null;
		addInfoMessage("/" + fullCommand);
		if (!Game1.isRunningMacro)
		{
			cheatHistory.Insert(0, "/" + fullCommand);
		}
		if (Game1.game1.parseDebugInput(command, CheatCommandChatLogger))
		{
			if (!string.IsNullOrEmpty(Game1.debugOutput))
			{
				addInfoMessage(Game1.debugOutput);
			}
		}
		else if (!string.IsNullOrEmpty(Game1.debugOutput))
		{
			addErrorMessage(Game1.debugOutput);
		}
		else
		{
			addErrorMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:ChatBox.cs.10261") + " " + ArgUtility.SplitBySpaceAndGet(command, 0));
		}
	}

	public void replyPrivateMessage(string[] command)
	{
		if (!Game1.IsMultiplayer)
		{
			return;
		}
		Farmer lastPlayer;
		if (lastReceivedPrivateMessagePlayerId == 0L)
		{
			addErrorMessage(Game1.content.LoadString("Strings\\UI:ChatCommands_Reply_NoMessageFound"));
		}
		else if (!Game1.otherFarmers.TryGetValue(lastReceivedPrivateMessagePlayerId, out lastPlayer) || !lastPlayer.isActive())
		{
			addErrorMessage(Game1.content.LoadString("Strings\\UI:ChatCommands_Reply_Failed"));
		}
		else
		{
			if (command.Length <= 1)
			{
				return;
			}
			string message = "";
			for (int i = 1; i < command.Length; i++)
			{
				message += command[i];
				if (i < command.Length - 1)
				{
					message += " ";
				}
			}
			message = Program.sdk.FilterDirtyWords(message);
			Game1.multiplayer.sendChatMessage(LocalizedContentManager.CurrentLanguageCode, message, lastReceivedPrivateMessagePlayerId);
			receiveChatMessage(Game1.player.UniqueMultiplayerID, 3, LocalizedContentManager.CurrentLanguageCode, message);
		}
	}

	public Farmer findMatchingFarmer(string[] command, ref int matchingIndex, bool allowMatchingByUserName = false, bool onlineOnly = true)
	{
		Farmer matchingFarmer = null;
		IEnumerable<Farmer> enumerable;
		if (!onlineOnly)
		{
			enumerable = Game1.getAllFarmers();
		}
		else
		{
			IEnumerable<Farmer> values = Game1.otherFarmers.Values;
			enumerable = values;
		}
		foreach (Farmer farmer in enumerable)
		{
			string[] farmerNameSplit = ArgUtility.SplitBySpace(farmer.displayName);
			bool isMatch = true;
			int i;
			for (i = 0; i < farmerNameSplit.Length; i++)
			{
				if (command.Length > i + 1)
				{
					if (!command[i + 1].EqualsIgnoreCase(farmerNameSplit[i]))
					{
						isMatch = false;
						break;
					}
					continue;
				}
				isMatch = false;
				break;
			}
			if (isMatch)
			{
				matchingFarmer = farmer;
				matchingIndex = i;
				break;
			}
			if (!allowMatchingByUserName)
			{
				continue;
			}
			isMatch = true;
			string[] userNameSplit = ArgUtility.SplitBySpace(Game1.multiplayer.getUserName(farmer.UniqueMultiplayerID));
			if (userNameSplit.Length == 0)
			{
				continue;
			}
			for (i = 0; i < userNameSplit.Length; i++)
			{
				if (command.Length > i + 1)
				{
					if (!command[i + 1].EqualsIgnoreCase(userNameSplit[i]))
					{
						isMatch = false;
						break;
					}
					continue;
				}
				isMatch = false;
				break;
			}
			if (isMatch)
			{
				matchingFarmer = farmer;
				matchingIndex = i;
				break;
			}
		}
		return matchingFarmer;
	}

	public void sendPrivateMessage(string[] command)
	{
		if (!Game1.IsMultiplayer)
		{
			return;
		}
		int matchingIndex = 0;
		Farmer matchingFarmer = findMatchingFarmer(command, ref matchingIndex);
		if (matchingFarmer == null)
		{
			addErrorMessage(Game1.content.LoadString("Strings\\UI:ChatCommands_Error_NoSuchOnlinePlayer"));
			return;
		}
		string message = "";
		for (int i = matchingIndex + 1; i < command.Length; i++)
		{
			message += command[i];
			if (i < command.Length - 1)
			{
				message += " ";
			}
		}
		message = Program.sdk.FilterDirtyWords(message);
		Game1.multiplayer.sendChatMessage(LocalizedContentManager.CurrentLanguageCode, message, matchingFarmer.UniqueMultiplayerID);
		receiveChatMessage(Game1.player.UniqueMultiplayerID, 3, LocalizedContentManager.CurrentLanguageCode, message);
	}

	public bool isActive()
	{
		return chatBox.Selected;
	}

	public void activate()
	{
		chatBox.Selected = true;
		setText("");
	}

	public override void clickAway()
	{
		base.clickAway();
		if (!choosingEmoji || !emojiMenu.isWithinBounds(Game1.getMouseX(), Game1.getMouseY()) || Game1.input.GetKeyboardState().IsKeyDown(Keys.Escape))
		{
			bool selected = chatBox.Selected;
			chatBox.Selected = false;
			choosingEmoji = false;
			setText("");
			cheatHistoryPosition = -1;
			if (selected)
			{
				Game1.oldKBState = Game1.GetKeyboardState();
			}
		}
	}

	public override bool isWithinBounds(int x, int y)
	{
		if (x - xPositionOnScreen >= width || x - xPositionOnScreen < 0 || y - yPositionOnScreen >= height || y - yPositionOnScreen < -getOldMessagesBoxHeight())
		{
			if (choosingEmoji)
			{
				return emojiMenu.isWithinBounds(x, y);
			}
			return false;
		}
		return true;
	}

	public virtual void setText(string text)
	{
		chatBox.setText(text);
	}

	/// <inheritdoc />
	public override void receiveKeyPress(Keys key)
	{
		switch (key)
		{
		case Keys.Up:
			if (cheatHistoryPosition < cheatHistory.Count - 1)
			{
				cheatHistoryPosition++;
				string cheat2 = cheatHistory[cheatHistoryPosition];
				chatBox.setText(cheat2);
			}
			break;
		case Keys.Down:
			if (cheatHistoryPosition > 0)
			{
				cheatHistoryPosition--;
				string cheat = cheatHistory[cheatHistoryPosition];
				chatBox.setText(cheat);
			}
			break;
		}
		if (!Game1.options.doesInputListContain(Game1.options.moveUpButton, key) && !Game1.options.doesInputListContain(Game1.options.moveRightButton, key) && !Game1.options.doesInputListContain(Game1.options.moveDownButton, key) && !Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
		{
			base.receiveKeyPress(key);
		}
	}

	public override bool readyToClose()
	{
		return false;
	}

	/// <inheritdoc />
	public override void receiveGamePadButton(Buttons button)
	{
	}

	public bool isHoveringOverClickable(int x, int y)
	{
		if (emojiMenuIcon.containsPoint(x, y) || (choosingEmoji && emojiMenu.isWithinBounds(x, y)))
		{
			return true;
		}
		return false;
	}

	/// <inheritdoc />
	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (!chatBox.Selected)
		{
			return;
		}
		if (emojiMenuIcon.containsPoint(x, y))
		{
			choosingEmoji = !choosingEmoji;
			Game1.playSound("shwip", null);
			emojiMenuIcon.scale = 4f;
			return;
		}
		if (choosingEmoji && emojiMenu.isWithinBounds(x, y))
		{
			emojiMenu.leftClick(x, y, this);
			return;
		}
		chatBox.Update();
		if (choosingEmoji)
		{
			choosingEmoji = false;
			emojiMenuIcon.scale = 4f;
		}
		if (isWithinBounds(x, y))
		{
			chatBox.Selected = true;
		}
	}

	public static string formattedUserName(Farmer farmer)
	{
		string name = farmer.Name;
		if (string.IsNullOrWhiteSpace(name))
		{
			name = Game1.content.LoadString("Strings\\UI:Chat_PlayerJoinedNewName");
		}
		return Program.sdk.FilterDirtyWords(name);
	}

	public static string formattedUserNameLong(Farmer farmer)
	{
		string name = formattedUserName(farmer);
		string userName = Game1.multiplayer.getUserName(farmer.UniqueMultiplayerID);
		if (string.IsNullOrWhiteSpace(userName))
		{
			return name;
		}
		return Game1.content.LoadString("Strings\\UI:Chat_PlayerName", name, userName);
	}

	public string formatMessage(long sourceFarmer, int chatKind, string message)
	{
		string userName = Game1.content.LoadString("Strings\\UI:Chat_UnknownUserName");
		Farmer farmer;
		if (sourceFarmer == Game1.player.UniqueMultiplayerID)
		{
			farmer = Game1.player;
		}
		else if (!Game1.otherFarmers.TryGetValue(sourceFarmer, out farmer))
		{
			farmer = null;
		}
		if (farmer != null)
		{
			userName = formattedUserName(farmer);
		}
		return chatKind switch
		{
			0 => Game1.content.LoadString("Strings\\UI:Chat_ChatMessageFormat", userName, message), 
			2 => Game1.content.LoadString("Strings\\UI:Chat_UserNotificationMessageFormat", message), 
			3 => Game1.content.LoadString("Strings\\UI:Chat_PrivateMessageFormat", userName, message), 
			_ => Game1.content.LoadString("Strings\\UI:Chat_ErrorMessageFormat", message), 
		};
	}

	public virtual Color messageColor(int chatKind)
	{
		return chatKind switch
		{
			0 => chatBox.TextColor, 
			3 => Color.DarkCyan, 
			2 => Color.Yellow, 
			_ => Color.Red, 
		};
	}

	public virtual void receiveChatMessage(long sourceFarmer, int chatKind, LocalizedContentManager.LanguageCode language, string message)
	{
		string text = formatMessage(sourceFarmer, chatKind, message);
		ChatMessage c = new ChatMessage();
		string s = Game1.parseText(text, chatBox.Font, chatBox.Width - 16);
		c.timeLeftToDisplay = 600;
		c.verticalSize = (int)chatBox.Font.MeasureString(s).Y + 4;
		c.color = messageColor(chatKind);
		c.language = language;
		c.parseMessageForEmoji(s);
		messages.Add(c);
		if (messages.Count > maxMessages)
		{
			messages.RemoveAt(0);
		}
		if (chatKind == 3 && sourceFarmer != Game1.player.UniqueMultiplayerID)
		{
			lastReceivedPrivateMessagePlayerId = sourceFarmer;
		}
	}

	public virtual void addMessage(string message, Color color)
	{
		ChatMessage c = new ChatMessage();
		string s = Game1.parseText(message, chatBox.Font, chatBox.Width - 8);
		c.timeLeftToDisplay = 600;
		c.verticalSize = (int)chatBox.Font.MeasureString(s).Y + 4;
		c.color = color;
		c.language = LocalizedContentManager.CurrentLanguageCode;
		c.parseMessageForEmoji(s);
		messages.Add(c);
		if (messages.Count > maxMessages)
		{
			messages.RemoveAt(0);
		}
	}

	/// <summary>Add a "ConcernedApe: Nice try..." Easter egg message to the chat box.</summary>
	public void addNiceTryEasterEggMessage()
	{
		addMessage(Game1.content.LoadString("Strings\\UI:ChatCommands_Error_NiceTry"), new Color(104, 214, 255));
	}

	/// <inheritdoc />
	public override void performHoverAction(int x, int y)
	{
		emojiMenuIcon.tryHover(x, y, 1f);
		emojiMenuIcon.tryHover(x, y, 1f);
	}

	/// <inheritdoc />
	public override void update(GameTime time)
	{
		KeyboardState keyState = Game1.input.GetKeyboardState();
		Keys[] pressedKeys = keyState.GetPressedKeys();
		foreach (Keys key in pressedKeys)
		{
			if (!oldKBState.IsKeyDown(key))
			{
				receiveKeyPress(key);
			}
		}
		oldKBState = keyState;
		for (int j = 0; j < messages.Count; j++)
		{
			if (messages[j].timeLeftToDisplay > 0)
			{
				messages[j].timeLeftToDisplay--;
			}
			if (messages[j].timeLeftToDisplay < 75)
			{
				messages[j].alpha = (float)messages[j].timeLeftToDisplay / 75f;
			}
		}
		if (chatBox.Selected)
		{
			foreach (ChatMessage message in messages)
			{
				message.alpha = 1f;
			}
		}
		emojiMenuIcon.tryHover(0, 0, 1f);
	}

	/// <inheritdoc />
	public override void receiveScrollWheelAction(int direction)
	{
		if (choosingEmoji)
		{
			emojiMenu.receiveScrollWheelAction(direction);
		}
	}

	/// <inheritdoc />
	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		updatePosition();
	}

	public static SpriteFont messageFont(LocalizedContentManager.LanguageCode language)
	{
		return Game1.content.Load<SpriteFont>("Fonts\\SmallFont", language);
	}

	public int getOldMessagesBoxHeight()
	{
		int heightSoFar = 20;
		for (int i = messages.Count - 1; i >= 0; i--)
		{
			ChatMessage message = messages[i];
			if (chatBox.Selected || message.alpha > 0.01f)
			{
				heightSoFar += message.verticalSize;
			}
		}
		return heightSoFar;
	}

	/// <inheritdoc />
	public override void draw(SpriteBatch b)
	{
		int heightSoFar = 0;
		bool drawBG = false;
		for (int i = messages.Count - 1; i >= 0; i--)
		{
			ChatMessage message = messages[i];
			if (chatBox.Selected || message.alpha > 0.01f)
			{
				heightSoFar += message.verticalSize;
				drawBG = true;
			}
		}
		if (drawBG)
		{
			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(301, 288, 15, 15), xPositionOnScreen, yPositionOnScreen - heightSoFar - 20 + ((!chatBox.Selected) ? chatBox.Height : 0), chatBox.Width, heightSoFar + 20, Color.White, 4f, drawShadow: false);
		}
		heightSoFar = 0;
		for (int i2 = messages.Count - 1; i2 >= 0; i2--)
		{
			ChatMessage message2 = messages[i2];
			heightSoFar += message2.verticalSize;
			message2.draw(b, xPositionOnScreen + 12, yPositionOnScreen - heightSoFar - 8 + ((!chatBox.Selected) ? chatBox.Height : 0));
		}
		if (chatBox.Selected)
		{
			chatBox.Draw(b, drawShadow: false);
			emojiMenuIcon.draw(b, Color.White, 0.99f);
			if (choosingEmoji)
			{
				emojiMenu.draw(b);
			}
			if (isWithinBounds(Game1.getMouseX(), Game1.getMouseY()) && !Game1.options.hardwareCursor)
			{
				Game1.mouseCursor = (Game1.options.gamepadControls ? Game1.cursor_gamepad_pointer : Game1.cursor_default);
			}
		}
	}
}
