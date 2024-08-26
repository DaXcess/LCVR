using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dissonance;
using Dissonance.Integrations.Unity_NFGO;
using Dissonance.Networking;
using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Networking;

/// (Ab)using Dissonance Voice to communicate directly to players without the host needing to have mods installed.
/// All of this code is <b>CLIENT</b> side!
public class NetworkSystem : MonoBehaviour
{
    /// Protocol Version, increase this every time a change is made that is not compatible with older versions
    private const ushort PROTOCOL_VERSION = 6;

    private static NetworkSystem _instance;

    public static NetworkSystem Instance => _instance == null
        ? _instance = new GameObject("VR Network System").AddComponent<NetworkSystem>()
        : _instance;

    private DissonanceComms dissonance;
    private BaseClient<NfgoServer, NfgoClient, NfgoConn> network;

    /// <summary>
    /// List of active clients in the session
    /// </summary>
    private readonly Dictionary<ushort, ClientInfo<NfgoConn?>> clients = [];

    /// <summary>
    /// List of VR players
    /// </summary>
    private readonly Dictionary<ushort, VRNetPlayer> players = [];

    /// <summary>
    /// Player ID lookup table
    /// </summary>
    private readonly Dictionary<string, ushort> playerIdByName = [];

    /// <summary>
    /// List of client IDs (from Dissonance Voice) which support LCVR networking
    /// </summary>
    private static readonly HashSet<ushort> subscribers = [];

    /// <summary>
    /// List of active channels we have, which can be used to communicate data over
    /// </summary>
    private readonly Dictionary<ChannelType, List<Channel>> channels = [];

    private ushort? LocalId => network?._serverNegotiator.LocalId;

    public bool Initialized { get; private set; }
    public VRNetPlayer[] Players => players.Values.ToArray();

    private void Awake()
    {
        StartCoroutine(Initialize());
    }

    private void OnDestroy()
    {
        if (!dissonance)
            return;

        dissonance.OnPlayerJoinedSession -= OnPlayerJoinedSession;
        dissonance.OnPlayerLeftSession -= OnPlayerLeftSession;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator Initialize()
    {
        yield return new WaitUntil(() => StartOfRound.Instance != null);

        dissonance = FindObjectOfType<DissonanceComms>();
        network = FindObjectOfType<NfgoCommsNetwork>().Client;

        // Wait until Dissonance Voip has been set up
        yield return new WaitUntil(() => LocalId.HasValue);

        Logger.LogDebug("Connected to Dissonance Voip");

        dissonance.OnPlayerJoinedSession += OnPlayerJoinedSession;
        dissonance.OnPlayerLeftSession += OnPlayerLeftSession;

        foreach (var player in dissonance.Players)
            if (!player.IsLocalPlayer && network._peers.TryGetClientInfoByName(player.Name, out var client))
            {
                clients.Add(client.PlayerId, client);
                playerIdByName[player.Name] = client.PlayerId;
            }

        StartCoroutine(SendHandshakeRoutine());

        Initialized = true;
    }

    public bool TryGetPlayer(ushort playerId, out VRNetPlayer player)
    {
        return players.TryGetValue(playerId, out player);
    }

    public bool IsInVR(ushort playerId)
    {
        return players.ContainsKey(playerId);
    }

    private void OnPlayerJoinedSession(VoicePlayerState player)
    {
        if (!network._peers.TryGetClientInfoByName(player.Name, out var client))
            return;

        clients.Add(client.PlayerId, client);
        playerIdByName.Add(player.Name, client.PlayerId);
    }

    private void OnPlayerLeftSession(VoicePlayerState player)
    {
        if (!playerIdByName.TryGetValue(player.Name, out var id))
            return;

        if (players.TryGetValue(id, out var networkPlayer))
            Destroy(networkPlayer);

        subscribers.Remove(id);
        players.Remove(id);
        clients.Remove(id);
        playerIdByName.Remove(player.Name);
    }

    private IEnumerator SendHandshakeRoutine()
    {
        while (true)
        {
            // Create a list of clients to send handshakes to
            var targets = clients.Where(client => !subscribers.Contains(client.Key)).Select(client => client.Value);

            // Send handshake request
            SendPacket(MessageType.HandshakeRequest, BitConverter.GetBytes(PROTOCOL_VERSION), targets.ToArray());

            yield return new WaitForSeconds(StartOfRound.Instance.inShipPhase ? 1 : 10);
        }

        // ReSharper disable once IteratorNeverReturns
    }

    #region PACKET SENDING

    internal void SendPacket(MessageType type, byte[] payload, params ClientInfo<NfgoConn?>[] targets)
    {
        if (LocalId is not { } sender)
            return;

        network.SendReliableP2P([.. targets], ConstructPacket(type, sender, payload));
    }

    internal void BroadcastPacket(MessageType type, byte[] payload)
    {
        if (LocalId is not { } sender)
            return;

        var targets = subscribers.Where(key => clients.TryGetValue(key, out _)).Select(value => clients[value])
            .ToList();

        network.SendReliableP2P(targets, ConstructPacket(type, sender, payload));
    }

    private static byte[] ConstructPacket(MessageType type, ushort sender, byte[] payload)
    {
        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory);

        // Magic
        writer.Write((ushort)51083);

        // Message type
        writer.Write((byte)type);

        // Sender Id
        writer.Write(sender);

        // Rest of payload
        writer.Write(payload);

        return memory.ToArray();
    }

    #endregion

    #region PACKET HANDLING

    public void OnPacketReceived(MessageType messageType, ushort sender, BinaryReader reader)
    {
        switch (messageType)
        {
            case MessageType.HandshakeRequest:
                HandleHandshakeRequest(sender, reader.ReadUInt16());
                break;

            case MessageType.HandshakeResponse:
                StartCoroutine(HandleHandshakeResponse(sender, reader.ReadBoolean()));
                break;

            case MessageType.Channel:
                HandleChannelMessage(sender, reader);
                break;
        }
    }

    private void HandleHandshakeRequest(ushort sender, ushort protocol)
    {
        if (protocol != PROTOCOL_VERSION)
        {
            if (StartOfRound.Instance.allPlayerScripts.FirstOrDefault(p => p.playerClientId == sender) is { } player)
                Logger.LogWarning(
                    $"{player.playerUsername} protocol version is {protocol}, expected {PROTOCOL_VERSION}");

            return;
        }

        if (!clients.TryGetValue(sender, out var target))
        {
            Logger.LogError($"Cannot send handshake response to {sender}: Client info is missing!");
            return;
        }

        Logger.LogDebug($"Received handshake request from {sender}");

        SendPacket(MessageType.HandshakeResponse, [VRSession.InVR ? (byte)1 : (byte)0], target);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator HandleHandshakeResponse(ushort sender, bool inVR)
    {
        Logger.LogDebug($"Received handshake response from {sender}");

        subscribers.Add(sender);

        if (!inVR)
            yield break;

        // Wait until client is a part of the peers list
        yield return new WaitUntilTimeout(10, () => network._peers.TryGetClientInfoById(sender, out _));

        if (!network._peers.TryGetClientInfoById(sender, out var client))
        {
            Logger.LogError(
                $"Failed to resolve client for Player Id {sender} after 10s. No VR movements will be synchronized.");

            yield break;
        }

        var player = dissonance.FindPlayer(client.PlayerName);
        if (player == null)
        {
            Logger.LogError(
                $"Failed to resolve client for Player {client.PlayerName}. No VR movements will be synchronized.");
            yield break;
        }

        yield return new WaitUntil(() => player.Tracker != null);

        // Ignore players that have already been registered
        if (players.ContainsKey(sender))
            yield break;

        var playerObject = ((NfgoPlayer)player.Tracker!).gameObject;
        var playerController = playerObject.GetComponent<PlayerControllerB>();
        var networkPlayer = playerObject.AddComponent<VRNetPlayer>();

        Logger.LogInfo($"Found VR player {playerController.playerUsername}");

        if (players.TryAdd(sender, networkPlayer))
            yield break;

        Logger.LogError(
            "VR player already exists? Destroying VR player script! Player will look like a vanilla player.");

        Destroy(networkPlayer);
        OnPlayerLeftSession(player);
    }

    private void HandleChannelMessage(ushort sender, BinaryReader reader)
    {
        var type = (ChannelType)reader.ReadByte();
        ulong? instanceId = null;

        if (reader.ReadBoolean())
            instanceId = reader.ReadUInt64();

        if (!channels.TryGetValue(type, out var channelList))
            return;

        if (instanceId.HasValue)
            channelList.Where(channel => channel.InstanceId == instanceId.Value)
                .Do(channel => channel.ReceivedPacket(sender, reader.Clone()));
        else
            channelList.Do(channel => channel.ReceivedPacket(sender, reader.Clone()));
    }

    #endregion

    #region CHANNELS

    public Channel CreateChannel(ChannelType type, ulong? instanceId = null)
    {
        var channel = new Channel(this, type, instanceId);

        channels.TryAdd(type, []);
        channels[type].Add(channel);

        return channel;
    }

    internal void CloseChannel(Channel channel)
    {
        if (!channels.TryGetValue(channel.Type, out var channelList))
            return;

        channelList.Remove(channel);
    }

    #endregion

    public enum MessageType : byte
    {
        HandshakeRequest = 16,
        HandshakeResponse,
        Channel
    }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class NetworkSystemPatches
{
    [HarmonyPatch(typeof(BaseClient<NfgoServer, NfgoClient, NfgoConn>), "ProcessReceivedPacket")]
    [HarmonyPostfix]
    private static void ProcessReceivedPacket(ref ArraySegment<byte> data)
    {
        if (!NetworkSystem.Instance.Initialized)
            return;
        
        try
        {
            using var stream = new MemoryStream(data.Array!, data.Offset, data.Array!.Length - data.Offset);
            using var reader = new BinaryReader(stream);

            // Check magic
            if (reader.ReadUInt16() != 51083)
                return;

            var messageType = reader.ReadByte();

            // Ignore built in messages
            if (messageType < 16)
                return;

            var type = (NetworkSystem.MessageType)messageType;
            var sender = reader.ReadUInt16();

            NetworkSystem.Instance.OnPacketReceived(type, sender, reader);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);
        }
    }
}

internal class WaitUntilTimeout(float timeout, Func<bool> predicate) : CustomYieldInstruction
{
    private readonly float startTime = Time.realtimeSinceStartup;

    public override bool keepWaiting => Time.realtimeSinceStartup - startTime < timeout && !predicate();
}