using Dissonance;
using Dissonance.Integrations.Unity_NFGO;
using Dissonance.Networking;
using Dissonance.Networking.Client;
using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Patches;
using LCVR.Physics.Interactions;
using LCVR.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Networking;

// (Ab)using Dissonance Voice to communicate directly to players without the host needing to have mods installed
// Keep in mind that all of this code is and should be CLIENT side!

internal static class DNet
{
    private const ushort PROTOCOL_VERSION = 3;

    private static readonly NamedLogger logger = new("Networking");

    private static DissonanceComms dissonance;
    private static BaseClient<NfgoServer, NfgoClient, NfgoConn> networkClient;
    private static SlaveClientCollection<NfgoConn> peers;

    private static ushort? LocalId => networkClient._serverNegotiator.LocalId;

    /// List of known clients inside the Dissonance Voice session
    private static readonly Dictionary<ushort, ClientInfo<NfgoConn?>> clients = [];
    
    /// List of active VR players in the session
    private static readonly Dictionary<ushort, VRNetPlayer> players = [];
    
    /// List of cached peers by Dissonance name
    private static readonly Dictionary<string, ushort> cachedPeers = [];
    
    /// List of client IDs (from Dissonance Voice) which support DNet
    private static readonly HashSet<ushort> subscribers = [];
    
    private static readonly Dictionary<ChannelType, List<Channel>> channels = [];

    public static VRNetPlayer[] Players => players.Values.ToArray();

    public static IEnumerator Initialize()
    {
        dissonance = GameObject.Find("DissonanceSetup").GetComponent<DissonanceComms>();
        networkClient = dissonance.GetComponent<NfgoCommsNetwork>().Client;
        peers = networkClient._peers;

        // Wait for voicechat connection
        yield return new WaitUntil(() => LocalId.HasValue);

        logger.LogDebug("Connected to Dissonance server");

        dissonance.OnPlayerJoinedSession += OnPlayerJoinedSession;
        dissonance.OnPlayerLeftSession += OnPlayerLeftSession;

        foreach (var player in dissonance.Players)
            if (!player.IsLocalPlayer && peers.TryGetClientInfoByName(player.Name, out var client))
            {
                clients.Add(client.PlayerId, client);
                cachedPeers.Add(player.Name, client.PlayerId);
            }

        dissonance.StartCoroutine(SendHandshakeCoroutine());
    }

    public static void Shutdown()
    {
        dissonance.OnPlayerJoinedSession -= OnPlayerJoinedSession;
        dissonance.OnPlayerLeftSession -= OnPlayerLeftSession;

        dissonance = null;

        players.Clear();
        clients.Clear();
        cachedPeers.Clear();
        channels.Clear();

        muffledPlayers.Clear();
    }

    public static Channel CreateChannel(ChannelType type, ulong? instanceId = null)
    {
        var channel = new Channel(type, instanceId);

        channels.TryAdd(type, []);
        channels[type].Add(channel);

        return channel;
    }

    internal static void CloseChannel(ChannelType type, Channel channel)
    {
        if (!channels.TryGetValue(type, out var channelList))
            return;

        channelList.Remove(channel);
    }

    public static bool TryGetPlayer(ushort id, out VRNetPlayer player)
    {
        return players.TryGetValue(id, out player);
    }

    public static void BroadcastChannelPacket(ChannelType type, ulong? instanceId, byte[] packet)
    {
        using var mem = new MemoryStream();
        using var bw = new BinaryWriter(mem);
        
        bw.Write((byte)type);

        if (instanceId.HasValue)
        {
            bw.Write(true);
            bw.Write(instanceId.Value);
        } else
            bw.Write(false);
        
        bw.Write(packet);
        
        BroadcastPacket(MessageType.Channel, mem.ToArray());
    }
    
    public static void BroadcastRig(Rig rig)
    {
        BroadcastPacket(MessageType.RigData, Serialization.Serialize(rig));
    }

    public static void BroadcastSpectatorRig(SpectatorRig rig)
    {
        BroadcastPacket(MessageType.SpectatorRigData, Serialization.Serialize(rig));
    }

    public static void InteractWithLever(bool started)
    {
        BroadcastPacket(MessageType.Lever, [started ? (byte)1 : (byte)0]);
    }

    public static void CancelChargerAnimation()
    {
        BroadcastPacket(MessageType.CancelChargerAnim, []);
    }

    public static void SetMuffled(bool muffled)
    {
        BroadcastPacket(MessageType.Muffled, [muffled ? (byte)1 : (byte)0]);
    }

    private static void SendHandshakeResponse(ushort clientId)
    {
        if (!clients.TryGetValue(clientId, out var target))
        {
            logger.LogError($"Cannot send handshake response to {clientId}: Client info is missing!");
            return;
        }

        SendPacket(MessageType.HandshakeResponse, [VRSession.InVR ? (byte)1 : (byte)0], target);
    }

    /// <summary>
    /// Continuously send handshake requests to clients that have not been negotiated with yet
    /// </summary>
    private static IEnumerator SendHandshakeCoroutine()
    {
        while (true)
        {
            // Grab a list of clients that are not subscribed
            var targets = clients.Where(client => !subscribers.Contains(client.Key)).Select(client => client.Value);

            // Send handshake request
            SendPacket(MessageType.HandshakeRequest, BitConverter.GetBytes(PROTOCOL_VERSION), targets.ToArray());

            yield return new WaitForSeconds(StartOfRound.Instance.inShipPhase ? 1 : 10);
        }
    }

    #region EVENT HANDLERS

    private static void OnPlayerJoinedSession(VoicePlayerState player)
    {
        logger.LogDebug("Player joined, trying to resolve client info");

        if (!peers.TryGetClientInfoByName(player.Name, out var info))
        {
            logger.LogError($"Failed to resolve client info for client '{player.Name}'");
            return;
        }

        logger.LogDebug($"Resolved client info");
        logger.LogDebug($"Player Name = {player.Name}");
        logger.LogDebug($"Player Id = {info.PlayerId}");

        clients.Add(info.PlayerId, info);
        cachedPeers.Add(player.Name, info.PlayerId);
    }

    private static void OnPlayerLeftSession(VoicePlayerState player)
    {
        if (!cachedPeers.TryGetValue(player.Name, out var id))
            return;

        if (players.TryGetValue(id, out var networkPlayer))
            Object.Destroy(networkPlayer);

        subscribers.Remove(id);
        players.Remove(id);
        clients.Remove(id);
        cachedPeers.Remove(player.Name);

        muffledPlayers.Remove(id);

        logger.LogDebug($"Player {player.Name} left the game");
        logger.LogDebug($"subscribers = {subscribers.Count}, players = {players.Count}, clients = {clients.Count} ({string.Join(", ", clients.Keys)}), clientByNames = {cachedPeers.Count} ({string.Join(", ", cachedPeers.Keys)})");
    }

    #endregion

    #region PACKET SENDING

    private static void BroadcastPacket(MessageType type, byte[] payload)
    {
        var targets = subscribers.Where(key => clients.TryGetValue(key, out _)).Select(value => clients[value]).ToList();

        networkClient.SendReliableP2P(targets, ConstructPacket(type, payload));
    }

    private static void SendPacket(MessageType type, byte[] payload, params ClientInfo<NfgoConn?>[] targets)
    {
        networkClient.SendReliableP2P([.. targets], ConstructPacket(type, payload));
    }

    private static byte[] ConstructPacket(MessageType type, byte[] payload)
    {
        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory);

        // Magic
        writer.Write((ushort)51083);

        // Message type
        writer.Write((byte)type);

        // Sender Id
        writer.Write(LocalId.Value);

        // Rest of payload
        writer.Write(payload);

        return memory.ToArray();
    }

    #endregion

    #region PACKET HANDLING

    public static void OnPacketReceived(MessageType messageType, ushort sender, BinaryReader reader)
    {
        switch (messageType)
        {
            case MessageType.HandshakeRequest:
                HandleHandshakeRequest(sender, reader.ReadUInt16());
                break;

            case MessageType.HandshakeResponse:
                dissonance.StartCoroutine(HandleHandshakeResponse(sender, reader.ReadBoolean()));
                break;

            case MessageType.RigData:
                HandleRigUpdate(sender, reader);
                break;
            
            case MessageType.SpectatorRigData:
                HandleSpectatorRigUpdate(sender, reader);
                break;

            case MessageType.Lever:
                HandleInteractWithLever(sender, reader.ReadBoolean());
                break;

            case MessageType.CancelChargerAnim:
                VRSession.Instance.ChargeStation.CancelChargingAnimation();
                break;

            case MessageType.Muffled:
                HandleSetMuffled(sender, reader.ReadBoolean());
                break;
            
            case MessageType.Channel:
                HandleChannelMessage(sender, reader);
                break;
        }
    }

    private static void HandleHandshakeRequest(ushort sender, ushort protocol)
    {
        if (protocol != PROTOCOL_VERSION)
            return;

        logger.LogDebug($"Player {sender} has initiated a handshake");

        SendHandshakeResponse(sender);
    }

    private static IEnumerator HandleHandshakeResponse(ushort sender, bool isInVR)
    {
        subscribers.Add(sender);

        if (!isInVR)
            yield break;

        // Wait until client is a part of the peers list
        yield return new WaitUntil(() => peers.TryGetClientInfoById(sender, out _));

        if (!peers.TryGetClientInfoById(sender, out var client))
        {
            logger.LogError($"Failed to resolve client for Player Id {sender}. No VR movements will be synchronized.");

            yield break;
        }

        var player = dissonance.FindPlayer(client.PlayerName);
        if (player == null)
        {
            logger.LogError($"Failed to resolve client for Player {client.PlayerName}. No VR movements will be synchronized.");
            yield break;
        }

        yield return new WaitUntil(() => player.Tracker != null);

        // Ignore players that have already been registered
        if (players.TryGetValue(sender, out var networkPlayer))
            yield break;

        var playerObject = ((NfgoPlayer)player.Tracker!).gameObject;
        var playerController = playerObject.GetComponent<PlayerControllerB>();
        networkPlayer = playerObject.AddComponent<VRNetPlayer>();

        logger.LogInfo($"Found VR player {playerController.playerUsername}");

        if (!players.TryAdd(sender, networkPlayer))
        {
            Logger.LogError("VR player already exists? Emergency nuking the network connection for this player!");
            
            Object.Destroy(networkPlayer);
            OnPlayerLeftSession(player);

            yield break;
        }

        foreach (var item in playerController.ItemSlots.Where(val => val != null))
        {
            // Add or enable VR item script on item if there is one for this item
            if (!Player.Items.items.TryGetValue(item.itemProperties.itemName, out var type))
                continue;

            var component = (MonoBehaviour)item.GetComponent(type);
            if (component == null)
                item.gameObject.AddComponent(type);
            else
                component.enabled = true;
        }
    }

    private static void HandleRigUpdate(ushort sender, BinaryReader reader)
    {
        if (!players.TryGetValue(sender, out var player))
            return;

        var rig = Serialization.Deserialize<Rig>(reader);
        player.UpdateTargetTransforms(rig);
    }

    private static void HandleSpectatorRigUpdate(ushort sender, BinaryReader reader)
    {
        if (!players.TryGetValue(sender, out var player))
            return;

        var rig = Serialization.Deserialize<SpectatorRig>(reader);
        player.UpdateSpectatorTransforms(rig);
    }
    
    private static void HandleInteractWithLever(ushort sender, bool started)
    {
        if (!players.TryGetValue(sender, out var player))
            return;

        var lever = VRSession.Instance.ShipLever;

        if (started && lever.CurrentActor == ShipLever.Actor.None)
            lever.StartInteracting(player.Bones.RightHand, ShipLever.Actor.Other);
        else if (!started && lever.CurrentActor == ShipLever.Actor.Other)
            lever.StopInteracting();
    }

    private static readonly HashSet<ushort> muffledPlayers = [];

    public static bool IsPlayerMuffled(int playerId)
    {
        return muffledPlayers.Any(player => player == playerId);
    }

    private static void HandleSetMuffled(ushort sender, bool muffled)
    {
        if (!players.TryGetValue(sender, out var player))
            return;

        logger.Log($"{player.PlayerController.playerUsername} muffled: {muffled}");

        if (muffled)
        {
            var occlude = player.PlayerController.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
            occlude.overridingLowPass = true;
            occlude.lowPassOverride = 1000f;

            muffledPlayers.Add(sender);
        }
        else
        {
            muffledPlayers.Remove(sender);

            StartOfRound.Instance.UpdatePlayerVoiceEffects();
        }
    }

    private static void HandleChannelMessage(ushort sender, BinaryReader reader)
    {
        var type = (ChannelType)reader.ReadByte();
        ulong? instanceId = null;

        if (reader.ReadBoolean())
            instanceId = reader.ReadUInt64();

        if (!channels.TryGetValue(type, out var channelList))
            return;

        if (instanceId.HasValue)
            channelList.Where(channel => channel.InstanceId == instanceId.Value)
                .Do(channel => channel.ReceivedPacket(sender, reader));
        else
            channelList.Do(channel => channel.ReceivedPacket(sender, reader));
    }
    
    #endregion

    #region SERIALIZABLE STRUCTS

    [Serialize]
    public struct Rig
    {
        public Vector3 rightHandPosition;
        public Vector3 rightHandEulers;
        public Fingers rightHandFingers;

        public Vector3 leftHandPosition;
        public Vector3 leftHandEulers;
        public Fingers leftHandFingers;

        public Vector3 cameraEulers;
        public Vector3 cameraPosAccounted;
        public Vector3 modelOffset;

        public CrouchState crouchState;
        public float rotationOffset;
        public float cameraFloorOffset;

        public enum CrouchState : byte
        {
            None,
            Roomscale,
            Button
        }
    }

    [Serialize]
    public struct SpectatorRig
    {
        public Vector3 headPosition;
        public Vector3 headRotation;

        public Vector3 leftHandPosition;
        public Vector3 leftHandRotation;

        public Vector3 rightHandPosition;
        public Vector3 rightHandRotation;

        public bool parentedToShip;
    }
    
    [Serialize]
    public struct Fingers
    {
        public byte thumb;
        public byte index;
        public byte middle;
        public byte ring;
        public byte pinky;
    }

    public enum MessageType : byte
    {
        HandshakeRequest = 16,
        HandshakeResponse,
        RigData,
        SpectatorRigData,
        Lever,
        CancelChargerAnim,
        Muffled,
        Channel
    }

    #endregion
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class DissonancePatches
{
    [HarmonyPatch(typeof(BaseClient<NfgoServer, NfgoClient, NfgoConn>), "ProcessReceivedPacket")]
    [HarmonyPostfix]
    private static void ProcessReceivedPacket(ref ArraySegment<byte> data)
    {
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

            var type = (DNet.MessageType)messageType;
            var sender = reader.ReadUInt16();

            DNet.OnPacketReceived(type, sender, reader);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);
        }
    }
}
