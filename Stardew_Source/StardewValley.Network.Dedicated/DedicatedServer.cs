using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Menus;

namespace StardewValley.Network.Dedicated;

public class DedicatedServer
{
	public class FarmerWarp
	{
		public Farmer who;

		public string name;

		public int facingDirection;

		public short x;

		public short y;

		public bool isStructure;

		public bool warpingForForcedRemoteEvent;

		public FarmerWarp(Farmer who, short x, short y, string name, bool isStructure, int facingDirection, bool warpingForForcedRemoteEvent)
		{
			this.who = who;
			this.name = name;
			this.facingDirection = facingDirection;
			this.x = x;
			this.y = y;
			this.isStructure = isStructure;
			this.warpingForForcedRemoteEvent = warpingForForcedRemoteEvent;
		}
	}

	private const string BROADCAST_EVENT_KEY = "BroadcastEvent";

	private readonly ConcurrentQueue<FarmerWarp> farmerWarps = new ConcurrentQueue<FarmerWarp>();

	private readonly Dictionary<string, Dictionary<string, long>> eventLocks = new Dictionary<string, Dictionary<string, long>>();

	private readonly HashSet<long> onlineIds = new HashSet<long>();

	private readonly HashSet<string> broadcastEvents = new HashSet<string>();

	private readonly HashSet<string> notBroadcastEvents = new HashSet<string>();

	private bool fakeWarp;

	private bool warpingSleep;

	private bool warpingFestival;

	private bool warpingHostBroadcastEvent;

	private bool startedFestivalMainEvent;

	private bool startedFestivalEnd;

	private bool shouldJudgeGrange;

	public bool CheckedHostPrecondition;

	private long fakeFarmerId;

	public bool FakeWarp
	{
		get
		{
			if (Game1.IsDedicatedHost)
			{
				return fakeWarp;
			}
			return false;
		}
	}

	public Farmer FakeFarmer
	{
		get
		{
			if (!Game1.IsDedicatedHost)
			{
				return Game1.player;
			}
			Farmer farmer = Game1.getFarmer(fakeFarmerId);
			if (!Game1.multiplayer.isDisconnecting(farmer))
			{
				return farmer;
			}
			return Game1.player;
		}
	}

	public DedicatedServer()
	{
		Reset();
	}

	public void Reset()
	{
		fakeWarp = false;
		warpingSleep = false;
		warpingFestival = false;
		startedFestivalMainEvent = false;
		startedFestivalEnd = false;
		shouldJudgeGrange = false;
		warpingHostBroadcastEvent = false;
		broadcastEvents.Clear();
		eventLocks.Clear();
	}

	public void ResetForNewDay()
	{
		if (Game1.IsDedicatedHost)
		{
			fakeWarp = false;
			warpingSleep = false;
			warpingFestival = false;
			startedFestivalMainEvent = false;
			startedFestivalEnd = false;
			shouldJudgeGrange = false;
			warpingHostBroadcastEvent = false;
			eventLocks.Clear();
		}
	}

	private bool TryForceClientHostEvent(FarmerWarp warp, GameLocation location, string eventId)
	{
		if (Game1.server == null)
		{
			return false;
		}
		string key = (warp.isStructure ? "1" : "0") + location.NameOrUniqueName;
		if (!eventLocks.TryGetValue(key, out var locationEvents))
		{
			eventLocks[key] = new Dictionary<string, long>();
		}
		else if (locationEvents.ContainsKey(eventId))
		{
			return false;
		}
		eventLocks[key][eventId] = warp.who.UniqueMultiplayerID;
		object[] message = Game1.multiplayer.generateForceEventMessage(eventId, location, warp.x, warp.y, use_local_farmer: true, notify_when_done: true);
		Game1.server.sendMessage(warp.who.UniqueMultiplayerID, 4, Game1.player, message);
		return true;
	}

	private void CheckForWarpEvents(FarmerWarp warp)
	{
		if (warp.warpingForForcedRemoteEvent || Game1.eventUp || Game1.farmEvent != null || IsWarping())
		{
			return;
		}
		GameLocation location = Game1.getLocationFromName(warp.name, warp.isStructure);
		Dictionary<string, string> events;
		try
		{
			if (!location.TryGetLocationEvents(out var _, out events) || events == null)
			{
				return;
			}
		}
		catch
		{
			return;
		}
		int xLocationAfterWarp = Game1.xLocationAfterWarp;
		int yLocationAfterWarp = Game1.yLocationAfterWarp;
		Game1.xLocationAfterWarp = warp.x;
		Game1.yLocationAfterWarp = warp.y;
		fakeWarp = true;
		EventCommandDelegate broadcastEventCommand = null;
		foreach (string eventKey in events.Keys)
		{
			CheckedHostPrecondition = false;
			string eventId = location.checkEventPrecondition(eventKey);
			if (!CheckedHostPrecondition || eventId == "-1" || string.IsNullOrEmpty(eventId) || !GameLocation.IsValidLocationEvent(eventKey, events[eventKey]) || (broadcastEventCommand == null && !Event.TryGetEventCommandHandler("BroadcastEvent", out broadcastEventCommand)))
			{
				continue;
			}
			if (notBroadcastEvents.Contains(eventId))
			{
				if (TryForceClientHostEvent(warp, location, eventId))
				{
					break;
				}
				continue;
			}
			if (broadcastEvents.Contains(eventId))
			{
				fakeFarmerId = warp.who.UniqueMultiplayerID;
				warpingHostBroadcastEvent = true;
				break;
			}
			string[] array = Event.ParseCommands(events[eventKey]);
			for (int i = 0; i < array.Length; i++)
			{
				string commandName = ArgUtility.Get(ArgUtility.SplitBySpaceQuoteAware(array[i]), 0);
				bool? flag = commandName?.StartsWith("--");
				if (flag.HasValue && flag != true && Event.TryGetEventCommandHandler(commandName, out var eventCommandHandler) && (object)eventCommandHandler == broadcastEventCommand)
				{
					fakeFarmerId = warp.who.UniqueMultiplayerID;
					warpingHostBroadcastEvent = true;
					broadcastEvents.Add(eventId);
					break;
				}
			}
			if (!warpingHostBroadcastEvent)
			{
				notBroadcastEvents.Add(eventId);
				if (TryForceClientHostEvent(warp, location, eventId))
				{
					break;
				}
			}
		}
		fakeWarp = false;
		Game1.xLocationAfterWarp = xLocationAfterWarp;
		Game1.yLocationAfterWarp = yLocationAfterWarp;
		if (warpingHostBroadcastEvent)
		{
			LocationRequest locationRequest = Game1.getLocationRequest(warp.name, warp.isStructure);
			locationRequest.OnWarp += delegate
			{
				warpingHostBroadcastEvent = false;
			};
			Game1.warpFarmer(locationRequest, warp.x, warp.y, warp.facingDirection);
		}
	}

	private bool IsWarping()
	{
		if (!Game1.isWarping && !warpingHostBroadcastEvent && !warpingSleep)
		{
			return warpingFestival;
		}
		return true;
	}

	public void DoHostAction(string action, params object[] data)
	{
		object[] messageData = new object[data.Length + 2];
		messageData[0] = (byte)1;
		messageData[1] = action;
		Array.Copy(data, 0, messageData, 2, data.Length);
		OutgoingMessage message = new OutgoingMessage(33, Game1.player, messageData);
		if (Game1.IsMasterGame)
		{
			IncomingMessage fakeMessage = new IncomingMessage();
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using BinaryWriter writer = new BinaryWriter(memoryStream);
				message.Write(writer);
				memoryStream.Seek(0L, SeekOrigin.Begin);
				using BinaryReader reader = new BinaryReader(memoryStream);
				fakeMessage.Read(reader);
			}
			Game1.multiplayer.processIncomingMessage(fakeMessage);
		}
		else if (Game1.HasDedicatedHost)
		{
			if (Game1.client != null)
			{
				Game1.client.sendMessage(message);
			}
		}
		else
		{
			Game1.log.Error("Tried to execute a host-only action '" + action + "' as a client on a non-dedicated server.");
		}
	}

	public void Tick()
	{
		if (!Game1.IsDedicatedHost)
		{
			return;
		}
		onlineIds.Clear();
		foreach (Farmer farmer in Game1.getOnlineFarmers())
		{
			if (!Game1.multiplayer.isDisconnecting(farmer) && farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
			{
				onlineIds.Add(farmer.UniqueMultiplayerID);
			}
		}
		if (onlineIds.Count == 0)
		{
			farmerWarps.Clear();
			eventLocks.Clear();
			if (Game1.CurrentEvent?.isFestival ?? false)
			{
				if (Game1.netWorldState.Value.IsPaused)
				{
					Game1.netWorldState.Value.IsPaused = false;
				}
				if (!startedFestivalEnd)
				{
					Game1.CurrentEvent.TryStartEndFestivalDialogue(Game1.player);
					startedFestivalEnd = true;
				}
			}
			else if (!Game1.netWorldState.Value.IsPaused)
			{
				Game1.netWorldState.Value.IsPaused = true;
			}
			return;
		}
		if (Game1.netWorldState.Value.IsPaused)
		{
			Game1.netWorldState.Value.IsPaused = false;
		}
		if (Game1.player.Stamina < (float)Game1.player.MaxStamina)
		{
			Game1.player.Stamina = Game1.player.MaxStamina;
		}
		if (Game1.player.health < Game1.player.maxHealth)
		{
			Game1.player.health = Game1.player.maxHealth;
		}
		if (eventLocks.Count > 0)
		{
			List<string> removeLocations = new List<string>();
			List<string> removeEvents = new List<string>();
			foreach (KeyValuePair<string, Dictionary<string, long>> locationEntry in eventLocks)
			{
				removeEvents.Clear();
				foreach (KeyValuePair<string, long> eventEntry in locationEntry.Value)
				{
					if (!onlineIds.Contains(eventEntry.Value))
					{
						removeEvents.Add(eventEntry.Key);
					}
				}
				if (locationEntry.Value.Count - removeEvents.Count <= 0)
				{
					removeLocations.Add(locationEntry.Key);
					continue;
				}
				foreach (string eventToRemove in removeEvents)
				{
					locationEntry.Value.Remove(eventToRemove);
				}
			}
			foreach (string locationToRemove in removeLocations)
			{
				eventLocks.Remove(locationToRemove);
			}
		}
		FarmerWarp warp;
		while (farmerWarps.TryDequeue(out warp))
		{
			if (warp.who != null && onlineIds.Contains(warp.who.UniqueMultiplayerID))
			{
				CheckForWarpEvents(warp);
			}
		}
		if (IsWarping())
		{
			return;
		}
		if (Game1.activeClickableMenu is DialogueBox dialogueBox)
		{
			if (dialogueBox.isQuestion)
			{
				dialogueBox.selectedResponse = 0;
			}
			dialogueBox.receiveLeftClick(0, 0);
		}
		if (Game1.CurrentEvent != null)
		{
			if (!Game1.CurrentEvent.skipped && Game1.CurrentEvent.skippable)
			{
				Game1.CurrentEvent.skipped = true;
				Game1.CurrentEvent.skipEvent();
				Game1.freezeControls = false;
			}
			if (Game1.CurrentEvent.isFestival)
			{
				NPC festivalHost = Game1.CurrentEvent.festivalHost;
				if (festivalHost != null && !startedFestivalMainEvent && CheckOthersReady("MainEvent_" + Game1.CurrentEvent.id))
				{
					Game1.CurrentEvent.answerDialogueQuestion(festivalHost, "yes");
					startedFestivalMainEvent = true;
				}
			}
			if (!startedFestivalEnd && Game1.CurrentEvent.isFestival && CheckOthersReady("festivalEnd"))
			{
				Game1.CurrentEvent.TryStartEndFestivalDialogue(Game1.player);
				startedFestivalEnd = true;
			}
			return;
		}
		if (!warpingSleep && CheckOthersReady("sleep"))
		{
			if (Game1.currentLocation.NameOrUniqueName.EqualsIgnoreCase(Game1.player.homeLocation.Value))
			{
				HostSleepInBed();
			}
			else
			{
				warpingSleep = true;
				LocationRequest locationRequest = Game1.getLocationRequest(Game1.player.homeLocation.Value);
				locationRequest.OnWarp += delegate
				{
					HostSleepInBed();
				};
				Game1.warpFarmer(locationRequest, 5, 9, Game1.player.FacingDirection);
			}
		}
		if (!warpingFestival && Game1.whereIsTodaysFest != null && CheckOthersReady("festivalStart"))
		{
			warpingFestival = true;
			LocationRequest locationRequest2 = Game1.getLocationRequest(Game1.whereIsTodaysFest);
			locationRequest2.OnWarp += delegate
			{
				warpingFestival = false;
			};
			int tileX = -1;
			int tileY = -1;
			Utility.getDefaultWarpLocation(Game1.whereIsTodaysFest, ref tileX, ref tileY);
			Game1.warpFarmer(locationRequest2, tileX, tileY, 2);
		}
	}

	internal void HandleFarmerWarp(FarmerWarp warp)
	{
		if (Game1.IsDedicatedHost && warp.who != null)
		{
			farmerWarps.Enqueue(warp);
		}
	}

	private bool CheckOthersReady(string readyCheck)
	{
		if (readyCheck == "MainEvent_festival_fall16")
		{
			return shouldJudgeGrange;
		}
		int ready = Game1.netReady.GetNumberReady(readyCheck);
		if (ready <= 0)
		{
			return false;
		}
		if (!Game1.netReady.IsReady(readyCheck))
		{
			return ready >= Game1.netReady.GetNumberRequired(readyCheck) - 1;
		}
		return false;
	}

	private void HostSleepInBed()
	{
		if (Game1.currentLocation is FarmHouse farmHouse)
		{
			Game1.player.position.Set(Utility.PointToVector2(farmHouse.GetPlayerBedSpot()) * 64f);
			farmHouse.answerDialogueAction("Sleep_Yes", null);
		}
		warpingSleep = false;
	}

	private void ProcessEventDone(IncomingMessage message)
	{
		if (message.SourceFarmer == null)
		{
			return;
		}
		string name = message.Reader.ReadString();
		bool locationIsStructure = message.Reader.ReadByte() != 0;
		string eventId = message.Reader.ReadString();
		GameLocation location = Game1.getLocationFromName(name, locationIsStructure);
		if (location != null)
		{
			string key = (locationIsStructure ? "1" : "0") + location.NameOrUniqueName;
			if (eventLocks.TryGetValue(key, out var locationEvents) && locationEvents.TryGetValue(eventId, out var lockOwner) && lockOwner == message.SourceFarmer.UniqueMultiplayerID)
			{
				Game1.player.eventsSeen.Add(eventId);
				locationEvents.Remove(eventId);
			}
		}
	}

	private void ProcessHostAction(IncomingMessage message)
	{
		switch (message.Reader.ReadString())
		{
		case "ChooseCave":
			Event.hostActionChooseCave(message.SourceFarmer, message.Reader);
			break;
		case "NamePet":
			Event.hostActionNamePet(message.SourceFarmer, message.Reader);
			break;
		case "JudgeGrange":
			shouldJudgeGrange = true;
			break;
		}
	}

	public void ProcessMessage(IncomingMessage message)
	{
		switch ((DedicatedServerMessageType)message.Reader.ReadByte())
		{
		case DedicatedServerMessageType.EventDone:
			ProcessEventDone(message);
			break;
		case DedicatedServerMessageType.HostAction:
			ProcessHostAction(message);
			break;
		}
	}
}
