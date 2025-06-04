using System.Xml.Serialization;
using Netcode;

namespace StardewValley.Quests;

public class SecretLostItemQuest : Quest
{
	/// <summary>The internal name for the NPC who gave the quest.</summary>
	[XmlElement("npcName")]
	public readonly NetString npcName = new NetString();

	/// <summary>The friendship point reward for completing the quest.</summary>
	[XmlElement("friendshipReward")]
	public readonly NetInt friendshipReward = new NetInt();

	/// <summary>If set, the ID for another quest to remove when this quest is completed.</summary>
	[XmlElement("exclusiveQuestId")]
	public readonly NetString exclusiveQuestId = new NetString();

	/// <summary>The qualified item ID that must be collected.</summary>
	[XmlElement("itemIndex")]
	public readonly NetString ItemId = new NetString();

	/// <summary>Whether the player has found the lost item.</summary>
	[XmlElement("itemFound")]
	public readonly NetBool itemFound = new NetBool();

	/// <summary>Construct an instance.</summary>
	public SecretLostItemQuest()
	{
	}

	/// <summary>Construct an instance.</summary>
	/// <param name="npcName">The internal name for the NPC who gave the quest.</param>
	/// <param name="itemId">The qualified or unqualified item ID that must be collected.</param>
	/// <param name="friendshipReward">The friendship point reward for completing the quest.</param>
	/// <param name="exclusiveQuestId">If set, the ID for another quest to remove when this quest is completed.</param>
	public SecretLostItemQuest(string npcName, string itemId, int friendshipReward, string exclusiveQuestId)
	{
		this.npcName.Value = npcName;
		ItemId.Value = ItemRegistry.QualifyItemId(itemId) ?? itemId;
		this.friendshipReward.Value = friendshipReward;
		this.exclusiveQuestId.Value = exclusiveQuestId;
		questType.Value = 9;
	}

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(npcName, "npcName").AddField(friendshipReward, "friendshipReward").AddField(exclusiveQuestId, "exclusiveQuestId")
			.AddField(ItemId, "ItemId")
			.AddField(itemFound, "itemFound");
	}

	public override bool isSecretQuest()
	{
		return true;
	}

	/// <inheritdoc />
	public override bool OnItemReceived(Item item, int numberAdded, bool probe = false)
	{
		bool baseChanged = base.OnItemReceived(item, numberAdded, probe);
		if (!completed.Value && !itemFound.Value && item?.QualifiedItemId == ItemId.Value)
		{
			if (!probe)
			{
				itemFound.Value = true;
				Game1.playSound("jingle1", null);
			}
			return true;
		}
		return baseChanged;
	}

	/// <inheritdoc />
	public override bool OnNpcSocialized(NPC npc, bool probe = false)
	{
		bool baseChanged = base.OnNpcSocialized(npc, probe);
		if (!completed.Value && itemFound.Value && npc.IsVillager && npc.Name == npcName.Value && Game1.player.Items.ContainsId(ItemId.Value))
		{
			if (!probe)
			{
				questComplete();
				string[] fields = Quest.GetRawQuestFields(id.Value);
				Dialogue thankYou = new Dialogue(npc, null, ArgUtility.Get(fields, 9, "Data\\ExtraDialogue:LostItemQuest_DefaultThankYou", allowBlank: false));
				npc.setNewDialogue(thankYou);
				Game1.drawDialogue(npc);
				Game1.player.changeFriendship(friendshipReward.Value, npc);
				Game1.player.removeFirstOfThisItemFromInventory(ItemId.Value);
			}
			return true;
		}
		return baseChanged;
	}

	public override void questComplete()
	{
		if (completed.Value)
		{
			return;
		}
		completed.Value = true;
		Game1.player.questLog.Remove(this);
		foreach (Quest q in Game1.player.questLog)
		{
			if (q != null && q.id.Value == exclusiveQuestId.Value)
			{
				q.destroy.Value = true;
			}
		}
		Game1.playSound("questcomplete", null);
	}
}
