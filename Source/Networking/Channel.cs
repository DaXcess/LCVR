using System;
using System.IO;

namespace LCVR.Networking;

public class Channel(NetworkSystem network, ChannelType type, ulong? instanceId) : IDisposable
{
    public ChannelType Type => type;
    public ulong? InstanceId => instanceId;

    public event Action<ushort, BinaryReader> OnPacketReceived;

    public void SendPacket(byte[] packet)
    {
        using var mem = new MemoryStream();
        using var bw = new BinaryWriter(mem);
        
        bw.Write((byte)type);

        if (instanceId.HasValue)
        {
            bw.Write(true);
            bw.Write(instanceId.Value);
        }
        else
            bw.Write(false);
        
        bw.Write(packet);

        network.BroadcastPacket(NetworkSystem.MessageType.Channel, mem.ToArray());
    }

    internal void ReceivedPacket(ushort sender, BinaryReader reader)
    {
        OnPacketReceived?.Invoke(sender, reader);
        
        // Reader was cloned, so dispose our copy
        reader.Dispose();
    }

    public void Dispose()
    {
        network.CloseChannel(this);
    }
}

public enum ChannelType : byte
{
    PlayerPrefs,
    Rig,
    SpectatorRig,
    ShipLever,
    ChargeStation,
    Muffle,
    VehicleSteeringWheel,
    VehicleGearStick,
}