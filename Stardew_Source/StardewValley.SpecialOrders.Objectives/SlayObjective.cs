using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Netcode;
using StardewValley.Monsters;

namespace StardewValley.SpecialOrders.Objectives;

public class SlayObjective : OrderObjective
{
	[XmlElement("targetNames")]
	public NetStringList targetNames = new NetStringList();

	/// <summary>Whether to ignore monsters killed on the farm.</summary>
	[XmlElement("ignoreFarmMonsters")]
	public NetBool ignoreFarmMonsters = new NetBool(value: true);

	public override void InitializeNetFields()
	{
		base.InitializeNetFields();
		base.NetFields.AddField(targetNames, "targetNames").AddField(ignoreFarmMonsters, "ignoreFarmMonsters");
	}

	public override void Load(SpecialOrder order, Dictionary<string, string> data)
	{
		base.Load(order, data);
		if (data.TryGetValue("TargetName", out var rawValue))
		{
			string[] array = order.Parse(rawValue).Split(',');
			foreach (string target in array)
			{
				targetNames.Add(target.Trim());
			}
		}
		if (data.TryGetValue("IgnoreFarmMonsters", out var rawIgnoreFarmMonsters))
		{
			if (bool.TryParse(rawIgnoreFarmMonsters, out var parsedIgnoreFarmMonsters))
			{
				ignoreFarmMonsters.Value = parsedIgnoreFarmMonsters;
			}
			else
			{
				Game1.log.Warn("Special order slay objective can't parse IgnoreFarmMonsters value '" + rawIgnoreFarmMonsters + "' as a boolean.");
			}
		}
	}

	protected override void _Register()
	{
		base._Register();
		SpecialOrder order = _order;
		order.onMonsterSlain = (Action<Farmer, Monster>)Delegate.Combine(order.onMonsterSlain, new Action<Farmer, Monster>(OnMonsterSlain));
	}

	protected override void _Unregister()
	{
		base._Unregister();
		SpecialOrder order = _order;
		order.onMonsterSlain = (Action<Farmer, Monster>)Delegate.Remove(order.onMonsterSlain, new Action<Farmer, Monster>(OnMonsterSlain));
	}

	public virtual void OnMonsterSlain(Farmer farmer, Monster monster)
	{
		if (ignoreFarmMonsters.Value && monster.currentLocation?.Name == "Farm")
		{
			return;
		}
		foreach (string target in targetNames)
		{
			if (monster.Name.Contains(target))
			{
				IncrementCount(1);
				break;
			}
		}
	}
}
