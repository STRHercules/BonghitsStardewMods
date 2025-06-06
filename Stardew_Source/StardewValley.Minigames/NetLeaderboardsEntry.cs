using Netcode;

namespace StardewValley.Minigames;

public class NetLeaderboardsEntry : INetObject<NetFields>
{
	public readonly NetString name = new NetString("");

	public readonly NetInt score = new NetInt(0);

	public NetFields NetFields { get; } = new NetFields("NetLeaderboardsEntry");

	public void InitNetFields()
	{
		NetFields.SetOwner(this).AddField(name, "name").AddField(score, "score");
	}

	public NetLeaderboardsEntry()
	{
		InitNetFields();
	}

	public NetLeaderboardsEntry(string new_name, int new_score)
	{
		InitNetFields();
		name.Value = new_name;
		score.Value = new_score;
	}
}
