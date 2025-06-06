using System.Xml.Serialization;
using Netcode;

namespace StardewValley;

public class Warp : INetObject<NetFields>
{
	[XmlElement("x")]
	private readonly NetInt x = new NetInt();

	[XmlElement("y")]
	private readonly NetInt y = new NetInt();

	[XmlElement("targetX")]
	private readonly NetInt targetX = new NetInt();

	[XmlElement("targetY")]
	private readonly NetInt targetY = new NetInt();

	[XmlElement("flipFarmer")]
	public readonly NetBool flipFarmer = new NetBool();

	[XmlElement("targetName")]
	private readonly NetString targetName = new NetString();

	[XmlElement("npcOnly")]
	public readonly NetBool npcOnly = new NetBool();

	[XmlIgnore]
	public NetFields NetFields { get; } = new NetFields("Warp");

	public int X => x.Value;

	public int Y => y.Value;

	public int TargetX
	{
		get
		{
			return targetX.Value;
		}
		set
		{
			targetX.Value = value;
		}
	}

	public int TargetY
	{
		get
		{
			return targetY.Value;
		}
		set
		{
			targetY.Value = value;
		}
	}

	public string TargetName
	{
		get
		{
			return targetName.Value;
		}
		set
		{
			targetName.Value = value;
		}
	}

	public Warp()
	{
		NetFields.SetOwner(this).AddField(x, "this.x").AddField(y, "this.y")
			.AddField(targetX, "this.targetX")
			.AddField(targetY, "this.targetY")
			.AddField(targetName, "this.targetName")
			.AddField(flipFarmer, "this.flipFarmer")
			.AddField(npcOnly, "this.npcOnly");
	}

	public Warp(int x, int y, string targetName, int targetX, int targetY, bool flipFarmer, bool npcOnly = false)
		: this()
	{
		this.x.Value = x;
		this.y.Value = y;
		this.targetX.Value = targetX;
		this.targetY.Value = targetY;
		this.targetName.Value = targetName;
		this.flipFarmer.Value = flipFarmer;
		this.npcOnly.Value = npcOnly;
	}
}
