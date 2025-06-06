using System;
using System.Xml.Serialization;
using Netcode;
using StardewValley.Extensions;

namespace StardewValley.Quests;

public class ResourceCollectionQuest : Quest
{
	/// <summary>The internal name for the NPC who gave the quest.</summary>
	[XmlElement("target")]
	public readonly NetString target = new NetString();

	/// <summary>The translated NPC dialogue shown when the quest is completed.</summary>
	[XmlElement("targetMessage")]
	public readonly NetString targetMessage = new NetString();

	/// <summary>The number of items collected so far.</summary>
	[XmlElement("numberCollected")]
	public readonly NetInt numberCollected = new NetInt();

	/// <summary>The number of items which must be collected.</summary>
	[XmlElement("number")]
	public readonly NetInt number = new NetInt();

	/// <summary>The gold reward for finishing the quest.</summary>
	[XmlElement("reward")]
	public readonly NetInt reward = new NetInt();

	/// <summary>The qualified item ID that must be collected.</summary>
	[XmlElement("resource")]
	public readonly NetString ItemId = new NetString();

	/// <summary>The translatable text segments for the quest description shown in the quest log.</summary>
	public readonly NetDescriptionElementList parts = new NetDescriptionElementList();

	/// <summary>The translatable text segments for the <see cref="F:StardewValley.Quests.ResourceCollectionQuest.targetMessage" />.</summary>
	public readonly NetDescriptionElementList dialogueparts = new NetDescriptionElementList();

	/// <summary>The translatable text segments for the objective shown in the quest log (like "0/5 caught").</summary>
	[XmlElement("objective")]
	public readonly NetDescriptionElementRef objective = new NetDescriptionElementRef();

	/// <summary>Construct an instance.</summary>
	public ResourceCollectionQuest()
	{
		questType.Value = 10;
	}

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(parts, "parts").AddField(dialogueparts, "dialogueparts").AddField(objective, "objective")
			.AddField(target, "target")
			.AddField(targetMessage, "targetMessage")
			.AddField(numberCollected, "numberCollected")
			.AddField(number, "number")
			.AddField(reward, "reward")
			.AddField(ItemId, "ItemId");
	}

	public void loadQuestInfo()
	{
		if (target.Value != null || Game1.gameMode == 6)
		{
			return;
		}
		Random random = CreateInitializationRandom();
		base.questTitle = Game1.content.LoadString("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13640");
		int randomResource = random.Next(6) * 2;
		for (int i = 0; i < random.Next(1, 100); i++)
		{
			random.Next();
		}
		int highest_mining_level = 0;
		int highest_foraging_level = 0;
		foreach (Farmer farmer in Game1.getAllFarmers())
		{
			highest_mining_level = Math.Max(highest_mining_level, farmer.MiningLevel);
		}
		foreach (Farmer farmer2 in Game1.getAllFarmers())
		{
			highest_foraging_level = Math.Max(highest_foraging_level, farmer2.ForagingLevel);
		}
		switch (randomResource)
		{
		case 0:
			ItemId.Value = "(O)378";
			number.Value = 20 + highest_mining_level * 2 + random.Next(-2, 4) * 2;
			reward.Value = number.Value * 10;
			number.Value -= number.Value % 5;
			target.Value = "Clint";
			break;
		case 2:
			ItemId.Value = "(O)380";
			number.Value = 15 + highest_mining_level + random.Next(-1, 3) * 2;
			reward.Value = number.Value * 15;
			number.Value = (int)((float)number.Value * 0.75f);
			number.Value -= number.Value % 5;
			target.Value = "Clint";
			break;
		case 4:
			ItemId.Value = "(O)382";
			number.Value = 10 + highest_mining_level + random.Next(-1, 3) * 2;
			reward.Value = number.Value * 25;
			number.Value = (int)((float)number.Value * 0.75f);
			number.Value -= number.Value % 5;
			target.Value = "Clint";
			break;
		case 6:
			ItemId.Value = ((Utility.GetAllPlayerDeepestMineLevel() > 40) ? "(O)384" : "(O)378");
			number.Value = 8 + highest_mining_level / 2 + random.Next(-1, 1) * 2;
			reward.Value = number.Value * 30;
			number.Value = (int)((float)number.Value * 0.75f);
			number.Value -= number.Value % 2;
			target.Value = "Clint";
			break;
		case 8:
			ItemId.Value = "(O)388";
			number.Value = 25 + highest_foraging_level + random.Next(-3, 3) * 2;
			number.Value -= number.Value % 5;
			reward.Value = number.Value * 8;
			target.Value = "Robin";
			break;
		default:
			ItemId.Value = "(O)390";
			number.Value = 25 + highest_mining_level + random.Next(-3, 3) * 2;
			number.Value -= number.Value % 5;
			reward.Value = number.Value * 8;
			target.Value = "Robin";
			break;
		}
		if (target.Value == null)
		{
			return;
		}
		Item item = ItemRegistry.Create(ItemId.Value);
		if (ItemId.Value != "(O)388" && ItemId.Value != "(O)390")
		{
			parts.Clear();
			int rand = random.Next(4);
			parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13647", number.Value, item, new DescriptionElement("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs." + (new string[4] { "13649", "13650", "13651", "13652" })[rand])));
			if (rand == 3)
			{
				dialogueparts.Clear();
				dialogueparts.Add("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13655");
				dialogueparts.Add("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs." + random.Choose("13656", "13657", "13658"));
				dialogueparts.Add("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13659");
			}
			else
			{
				dialogueparts.Clear();
				dialogueparts.Add("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13662");
				dialogueparts.Add("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs." + random.Choose("13656", "13657", "13658"));
				dialogueparts.Add(random.NextBool() ? new DescriptionElement("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13667", new DescriptionElement("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs." + random.Choose("13668", "13669", "13670"))) : new DescriptionElement("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13672"));
				dialogueparts.Add("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13673");
			}
		}
		else
		{
			parts.Clear();
			parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13674", number.Value, item));
			dialogueparts.Clear();
			dialogueparts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13677", (ItemId.Value == "(O)388") ? new DescriptionElement("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13678") : new DescriptionElement("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13679")));
			dialogueparts.Add("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs." + random.Choose("13681", "13682", "13683"));
		}
		parts.Add(new DescriptionElement("Strings\\StringsFromCSFiles:ItemDeliveryQuest.cs.13607", reward.Value));
		parts.Add(target.Value.Equals("Clint") ? "Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13688" : "");
		objective.Value = new DescriptionElement("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13691", "0", number.Value, item);
	}

	public override void reloadDescription()
	{
		if (_questDescription == "")
		{
			loadQuestInfo();
		}
		if (parts.Count == 0 || parts == null || dialogueparts.Count == 0 || dialogueparts == null)
		{
			return;
		}
		string descriptionBuilder = "";
		string messageBuilder = "";
		foreach (DescriptionElement a in parts)
		{
			descriptionBuilder += a.loadDescriptionElement();
		}
		foreach (DescriptionElement b in dialogueparts)
		{
			messageBuilder += b.loadDescriptionElement();
		}
		base.questDescription = descriptionBuilder;
		targetMessage.Value = messageBuilder;
	}

	public override void reloadObjective()
	{
		if (numberCollected.Value < number.Value)
		{
			Item item = ItemRegistry.Create(ItemId.Value);
			objective.Value = new DescriptionElement("Strings\\StringsFromCSFiles:ResourceCollectionQuest.cs.13691", numberCollected.Value, number.Value, item);
		}
		if (objective.Value != null)
		{
			base.currentObjective = objective.Value.loadDescriptionElement();
		}
	}

	/// <inheritdoc />
	public override bool OnItemReceived(Item item, int numberAdded, bool probe = false)
	{
		bool baseChanged = base.OnItemReceived(item, numberAdded, probe);
		if (!completed.Value && item?.QualifiedItemId == ItemId.Value && numberAdded != -1 && numberCollected.Value < number.Value)
		{
			if (!probe)
			{
				numberCollected.Value = Math.Min(number.Value, numberCollected.Value + numberAdded);
				Game1.dayTimeMoneyBox.pingQuest(this);
				if (numberCollected.Value >= number.Value)
				{
					NPC actualTarget = Game1.getCharacterFromName(target.Value);
					objective.Value = new DescriptionElement("Strings\\StringsFromCSFiles:FishingQuest.cs.13277", actualTarget);
					Game1.playSound("jingle1", null);
				}
			}
			return true;
		}
		return baseChanged;
	}

	/// <inheritdoc />
	public override bool OnNpcSocialized(NPC npc, bool probe = false)
	{
		bool baseChanged = base.OnNpcSocialized(npc, probe);
		if (!completed.Value && npc.IsVillager && npc.Name == target.Value && numberCollected.Value >= number.Value)
		{
			if (!probe)
			{
				npc.CurrentDialogue.Push(new Dialogue(npc, null, targetMessage.Value));
				moneyReward.Value = reward.Value;
				questComplete();
				Game1.drawDialogue(npc);
			}
			return true;
		}
		return baseChanged;
	}
}
