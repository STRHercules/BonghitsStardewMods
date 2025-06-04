using System.Xml.Serialization;
using Netcode;

namespace StardewValley.Quests;

public class GoSomewhereQuest : Quest
{
	[XmlElement("whereToGo")]
	public readonly NetString whereToGo = new NetString();

	public GoSomewhereQuest()
	{
	}

	public GoSomewhereQuest(string where)
	{
		whereToGo.Value = where;
	}

	/// <inheritdoc />
	protected override void initNetFields()
	{
		base.initNetFields();
		base.NetFields.AddField(whereToGo, "whereToGo");
	}

	/// <inheritdoc />
	public override bool OnWarped(GameLocation location, bool probe = false)
	{
		bool baseChanged = base.OnWarped(location, probe);
		if (location?.NameOrUniqueName == whereToGo.Value)
		{
			if (!probe)
			{
				questComplete();
			}
			return true;
		}
		return baseChanged;
	}
}
