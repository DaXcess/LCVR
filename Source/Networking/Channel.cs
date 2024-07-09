using System;
using System.IO;

namespace LCVR.Networking;

public class Channel(ChannelType type, ulong? instanceId) : IDisposable
{
    public ulong? InstanceId => instanceId;

    public event Action<ushort, BinaryReader> OnPacketReceived;

    public void SendPacket(byte[] packet)
    {
        DNet.BroadcastChannelPacket(type, InstanceId, packet);
    }

    internal void ReceivedPacket(ushort sender, BinaryReader reader)
    {
        OnPacketReceived?.Invoke(sender, reader);
    }

    public void Dispose()
    {
        DNet.CloseChannel(type, this);
    }
}

public enum ChannelType : byte
{
    PlayerPrefs,
    VehicleSteeringWheel,
    VehicleGearStick,
}