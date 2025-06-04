using System.Collections.Generic;
using Netcode;
using Netcode.Validation;

namespace StardewValley.SpecialOrders.Rewards;

public class ObjectReward : OrderReward
{
	public readonly NetString itemKey = new NetString("");

	public readonly NetInt amount = new NetInt(0);

	[NotNetField]
	private Object _objectInstance;

	/// <summary>The item stack to be drawn on the special orders board.</summary>
	public Object objectInstance
	{
		get
		{
			if (_objectInstance == null && !string.IsNullOrEmpty(itemKey.Value) && amount.Value > 0)
			{
				_objectInstance = new Object(itemKey.Value, amount.Value);
			}
			return _objectInstance;
		}
	}

	public override void InitializeNetFields()
	{
		base.InitializeNetFields();
		base.NetFields.AddField(itemKey, "itemKey").AddField(amount, "amount");
	}

	public override void Load(SpecialOrder order, Dictionary<string, string> data)
	{
		itemKey.Value = order.Parse(data["Item"]);
		amount.Value = int.Parse(order.Parse(data["Amount"]));
		_objectInstance = new Object(itemKey.Value, amount.Value);
	}

	public override void Grant()
	{
		Object i = new Object(itemKey.Value, amount.Value);
		Game1.player.addItemByMenuIfNecessary(i);
	}
}
