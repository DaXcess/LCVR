using Dissonance;
using Dissonance.Integrations.Unity_NFGO;
using Dissonance.Networking;
using GameNetcodeStuff;
using HarmonyLib;
using JetBrains.Annotations;
using LCVR.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LCVR.Networking
{
    // (Ab)using Dissonance Voice to communicate directly to players without the host needing to have mods installed
    // Keep in mind that all of this code is and should be CLIENT side!

    public class DNet
    {
        private static DissonanceComms dissonance;
        private static NfgoCommsNetwork network;
        private static BaseClient<NfgoServer, NfgoClient, NfgoConn> client;
        private static Peers peers;

        private static ushort? LocalIdSafe
        {
            get
            {
                var session = AccessTools.Field(client.GetType(), "_serverNegotiator").GetValue(client);
                var localId = (ushort?)AccessTools.Property(session.GetType(), "LocalId").GetValue(session);

                return localId;
            }
        }

        private static ushort LocalId => LocalIdSafe.Value;

        // A list of all known VR clients
        private static readonly Dictionary<ushort, ClientInfo<NfgoConn?>> clients = [];
        private static readonly Dictionary<ushort, VRNetPlayer> players = [];
        private static readonly Dictionary<string, ushort> clientByName = [];
        private static readonly List<ushort> subscribers = [];

        public static IEnumerator Initialize()
        {
            dissonance = GameObject.Find("DissonanceSetup").GetComponent<DissonanceComms>();
            network = dissonance.GetComponent<NfgoCommsNetwork>();
            client = (BaseClient<NfgoServer, NfgoClient, NfgoConn>)typeof(NfgoCommsNetwork).GetProperty("Client", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(network);
            peers = new Peers(AccessTools.Field(client.GetType(), "_peers").GetValue(client));

            // Wait for voicechat connection
            yield return new WaitUntil(() => LocalIdSafe.HasValue);

            Logger.LogDebug("Connected to Dissonance server");

            dissonance.OnPlayerJoinedSession += OnPlayerJoinedSession;
            dissonance.OnPlayerLeftSession += OnPlayerLeftSession;

            foreach (var player in dissonance.Players)
                if (peers.TryGetClientInfoByName(player.Name, out var client)) {
                    clients.Add(client.PlayerId, client);
                    clientByName.Add(player.Name, client.PlayerId);
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
            clientByName.Clear();
        }

        public static void BroadcastRig(Rig rig)
        {
            BroadcastVRPacket(MessageType.RigData, rig.Serialize());
        }

        private static IEnumerator SendHandshakeCoroutine()
        {
            while (true)
            {
                BroadcastGlobalPacket(MessageType.VRHandshake, [Plugin.Flags.HasFlag(Flags.VR) ? (byte)1 : (byte)0]);
                
                yield return new WaitForSeconds(StartOfRound.Instance.inShipPhase ? 1 : 30);
            }
        }

        #region EVENT HANDLERS

        private static void OnPlayerJoinedSession(VoicePlayerState player)
        {
            Logger.LogDebug("Player joined, trying to resolve client info");

            if (!peers.TryGetClientInfoByName(player.Name, out var info))
            {
                Logger.LogError($"Failed to resolve client info for client '{player.Name}'");
                return;
            }

            Logger.LogDebug($"Resolved client info");
            Logger.LogDebug($"Player Name = {player.Name}");
            Logger.LogDebug($"Player Id = {info.PlayerId}");

            clients.Add(info.PlayerId, info);
            clientByName.Add(player.Name, info.PlayerId);
        }

        private static void OnPlayerLeftSession(VoicePlayerState player)
        {
            if (!clientByName.TryGetValue(player.Name, out var id))
                return;

            if (players.TryGetValue(id, out var networkPlayer))
                GameObject.Destroy(networkPlayer);

            subscribers.Remove(id);
            players.Remove(id);
            clients.Remove(id);
            clientByName.Remove(player.Name);

            Logger.LogDebug($"Player {player.Name} left the game");
            Logger.LogDebug($"subscribers = {subscribers.Count}, players = {players.Count}, clients = {clients.Count} ({string.Join(", ", clients.Keys)}), clientByNames = {clientByName.Count} ({string.Join(", ", clientByName.Keys)})");
        }

        #endregion

        #region PACKET SENDING

        private static void BroadcastGlobalPacket(MessageType type, byte[] payload)
        {
            var clients = peers.Clients.Values.ToList();
            clients.RemoveAt(clients.FindIndex(client => client.PlayerId == LocalId));

            client.SendReliableP2P(clients, ConstructPacket(type, payload));
        }

        private static void BroadcastVRPacket(MessageType type, byte[] payload)
        {
            var targets = subscribers.Where(key => clients.TryGetValue(key, out var value)).Select(value => clients[value]).ToList();

            client.SendReliableP2P(targets, ConstructPacket(type, payload));
        }

        private static void SendPacket(ClientInfo<NfgoConn?> target, MessageType type, byte[] payload)
        {
            client.SendReliableP2P([target], ConstructPacket(type, payload));
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
            writer.Write(LocalId);

            // Rest of payload
            writer.Write(payload);

            return memory.ToArray();
        }

        #endregion

        #region PACKET HANDLING

        public static void OnPacketReceived(MessageType messageType, ushort sender, byte[] data)
        {
            switch (messageType)
            {
                case MessageType.VRHandshake:
                    dissonance.StartCoroutine(HandleVRHandshake(sender, BitConverter.ToBoolean(data)));        
                    break;

                case MessageType.RigData:
                    HandleRigUpdate(sender, data);
                    break;
            }
        }

        // VR Handshakes get sent for one of two reasons:
        //  - A client joins the lobby and announces themselves as VR
        //  - Another client joins the lobby and all other VR players send a VR handshake to this client
        private static IEnumerator HandleVRHandshake(ushort sender, bool isInVR)
        {
            if (!isInVR)
            {
                // Ignore if player is already known to be subscribed
                if (subscribers.Contains(sender))
                    yield break;

                subscribers.Add(sender);
                yield break;
            }

            if (!subscribers.Contains(sender))
                subscribers.Add(sender);

            // Ignore if player is already known to be VR
            if (players.ContainsKey(sender))
                yield break;

            yield return new WaitUntil(() => peers.TryGetClientInfoById(sender, out var client));

            if (!peers.TryGetClientInfoById(sender, out var client))
            {
                Logger.LogError($"Failed to resolve client for Player Id {sender}. No VR movements will be synchronized.");

                yield break;
            }

            var player = dissonance.FindPlayer(client.PlayerName);
            if (player == null)
            {
                Logger.LogError($"Failed to resolve client for Player {player}. No VR movements will be synchronized.");
                yield break;
            }

            yield return new WaitUntil(() => player.Tracker != null);

            var playerObject = ((NfgoPlayer)player.Tracker).gameObject;
            var networkPlayer = playerObject.AddComponent<VRNetPlayer>();
            var playerController = playerObject.GetComponent<PlayerControllerB>();

            Logger.LogInfo($"Found VR player {player.Name}");

            players.Add(sender, networkPlayer);

            foreach (var item in playerController.ItemSlots.Where(val => val != null))
            {
                // Add or enable VR item script on item if there is one for this item
                if (Player.Items.items.TryGetValue(item.itemProperties.itemName, out var type))
                {
                    var component = (MonoBehaviour)item.GetComponent(type);
                    if (component == null)
                        item.gameObject.AddComponent(type);
                    else
                        component.enabled = true;
                }
            }
        }

        private static void HandleRigUpdate(ushort sender, byte[] packet)
        {
            if (!players.TryGetValue(sender, out var player))
                return;

            var rig = Rig.Deserialize(packet);
            player.UpdateTargetTransforms(rig);
        }

        #endregion

        #region SERIALIZABLE STRUCTS
        
        public struct Rig
        {
            public Vector3 rightHandPosition;
            public Vector3 rightHandEulers;

            public Vector3 leftHandPosition;
            public Vector3 leftHandEulers;

            public Vector3 cameraEulers;
            public Vector3 cameraPosAccounted;

            public bool isCrouching;
            public float rotationOffset;
            public float cameraFloorOffset;

            public readonly byte[] Serialize()
            {
                using var mem = new MemoryStream();
                using var bw = new BinaryWriter(mem);

                bw.Write(rightHandPosition.x);
                bw.Write(rightHandPosition.y);
                bw.Write(rightHandPosition.z);

                bw.Write(rightHandEulers.x);
                bw.Write(rightHandEulers.y);
                bw.Write(rightHandEulers.z);

                bw.Write(leftHandPosition.x);
                bw.Write(leftHandPosition.y);
                bw.Write(leftHandPosition.z);

                bw.Write(leftHandEulers.x);
                bw.Write(leftHandEulers.y);
                bw.Write(leftHandEulers.z);

                bw.Write(cameraEulers.x);
                bw.Write(cameraEulers.y);
                bw.Write(cameraEulers.z);

                bw.Write(cameraPosAccounted.x);
                bw.Write(cameraPosAccounted.z);

                bw.Write(isCrouching);
                bw.Write(rotationOffset);
                bw.Write(cameraFloorOffset);

                return mem.ToArray();
            }

            public static Rig Deserialize(byte[] raw)
            {
                using var mem = new MemoryStream(raw);
                using var br = new BinaryReader(mem);

                var rig = new Rig
                {
                    rightHandPosition = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    rightHandEulers = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    leftHandPosition = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    leftHandEulers = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    cameraEulers = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    cameraPosAccounted = new Vector3(br.ReadSingle(), 0, br.ReadSingle()),
                    isCrouching = br.ReadBoolean(),
                    rotationOffset = br.ReadSingle(),
                    cameraFloorOffset = br.ReadSingle(),
                };

                return rig;
            }
        }

        public enum MessageType : byte
        {
            VRHandshake = 0x0C,
            RigData,
        }

        #endregion
    }

    internal static class DissonanceExtensions
    {
        private static readonly MethodInfo sendReliableP2P;

        static DissonanceExtensions()
        {
            sendReliableP2P = AccessTools.Method(typeof(BaseClient<NfgoServer, NfgoClient, NfgoConn>), "SendReliableP2P");
        }

        public static void SendReliableP2P(this BaseClient<NfgoServer, NfgoClient, NfgoConn> client, [NotNull] List<ClientInfo<NfgoConn?>> destinations, ArraySegment<byte> packet)
        {
            sendReliableP2P.Invoke(client, [destinations, packet]);
        }
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
                using var stream = new MemoryStream(data.Array, data.Offset, data.Array.Length - data.Offset);
                using var reader = new BinaryReader(stream);

                // Check magic
                if (reader.ReadUInt16() != 51083)
                    return;

                var messageType = reader.ReadByte();

                // Ignore built in messages
                if (messageType < 12)
                    return;

                var type = (DNet.MessageType)messageType;
                var sender = reader.ReadUInt16();
                var payload = reader.ReadBytes(data.Array.Length - data.Offset - 5);

                DNet.OnPacketReceived(type, sender, payload);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                Logger.LogError(ex.StackTrace);

                return;
            }
        }
    }
}
