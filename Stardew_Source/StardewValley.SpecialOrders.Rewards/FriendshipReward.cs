using System.Collections.Generic;
using System.Xml.Serialization;
using Netcode;

namespace StardewValley.SpecialOrders.Rewards;

public class FriendshipReward : OrderReward
{
	[XmlElement("targetName")]
	public NetString targetName = new NetString();

	[XmlElement("amount")]
	public NetInt amount = new NetInt();

	public override void InitializeNetFields()
	{
		base.InitializeNetFields();
		base.NetFields.AddField(targetName, "targetName").AddField(amount, "amount");
	}

	public override void Load(SpecialOrder order, Dictionary<string, string> data)
	{
		if (!data.TryGetValue("TargetName", out var target_name))
		{
			target_name = order.requester.Value;
		}
		target_name = order.Parse(target_name);
		targetName.Value = target_name;
		string amountString = data.GetValueOrDefault("Amount", "250");
		amountString = order.Parse(amountString);
		amount.Value = int.Parse(amountString);
	}

	public override void Grant()
	{
		NPC n = Game1.getCharacterFromName(targetName.Value);
		if (n != null)
		{
			Game1.player.changeFriendship(amount.Value, n);
		}
	}
}
