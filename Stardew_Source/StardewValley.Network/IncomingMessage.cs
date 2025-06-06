using System;
using System.IO;
using Netcode;

namespace StardewValley.Network;

public class IncomingMessage : IDisposable
{
	private byte messageType;

	private long farmerID;

	private byte[] data;

	private MemoryStream stream;

	private BinaryReader reader;

	public byte MessageType => messageType;

	public long FarmerID => farmerID;

	public Farmer SourceFarmer => Game1.GetPlayer(farmerID) ?? Game1.MasterPlayer;

	public byte[] Data => data;

	public BinaryReader Reader => reader;

	public void Read(BinaryReader reader)
	{
		Dispose();
		messageType = reader.ReadByte();
		farmerID = reader.ReadInt64();
		data = reader.ReadSkippableBytes();
		stream = new MemoryStream(data);
		this.reader = new BinaryReader(stream);
	}

	public void Dispose()
	{
		reader?.Dispose();
		stream?.Dispose();
		stream = null;
		reader = null;
	}
}
