using Dissonance;
using Dissonance.Integrations.Unity_NFGO;
using Dissonance.Networking;
using GameNetcodeStuff;
using LCVR.Player;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LCVR.Networking
{
    // (Ab)using Dissonance Voice to communicate directly to players without the host needing to have mods installed
    // Keep in mind that all of this code is and should be CLIENT side!

    public static class DNet
    {
        private static readonly Dictionary<string, VRNetPlayer> vrPlayers = [];
        private static DissonanceComms dissonance;
        private static RoomMembership room;

        public static void SetupDissonanceNetworking()
        {
            dissonance = GameObject.Find("DissonanceSetup").GetComponent<DissonanceComms>();

            dissonance.OnPlayerEnteredRoom += OnPlayerJoin;
            dissonance.OnPlayerLeftSession += OnPlayerLeave;
            dissonance.Text.MessageReceived += OnPacketReceived;

            room = dissonance.Rooms.Join("LCVR");

            if (Plugin.Flags.HasFlag(Flags.VR))
                SendHandshake();

            Logger.Log("Joined 'LCVR' network, ready for other VR players!");
        }

        public static void DestroyDissonanceNetworking()
        {
            dissonance.OnPlayerEnteredRoom -= OnPlayerJoin;
            dissonance.OnPlayerLeftSession -= OnPlayerLeave;
            dissonance.Text.MessageReceived -= OnPacketReceived;

            dissonance.Rooms.Leave(room);
            dissonance = null;

            vrPlayers.Clear();

            Logger.Log("Left 'LCVR' network, goodbye for now!");
        }

        public static void BroadcastRig(Rig rig)
        {
            dissonance.Text.Send("LCVR", "UPRIG" + Convert.ToBase64String(rig.Serialize()));
        }

        public static void BroadcastFloorOffset(float cameraFloorOffset)
        {
            dissonance.Text.Send("LCVR", $"CAMFL{cameraFloorOffset}");
        }

        private static void SendHandshake(string player = null)
        {
            var offset = StartOfRound.Instance.localPlayerController.GetComponent<VRPlayer>().cameraFloorOffset;

            if (string.IsNullOrEmpty(player))
            {
                dissonance.Text.Send("LCVR", "HELLO");
                dissonance.Text.Send("LCVR", $"CAMFL{offset}");
            } else
            {
                dissonance.Text.Whisper(player, "HELLO");
                dissonance.Text.Whisper(player, $"CAMFL{offset}");
            }
        }

        private static void OnPlayerJoin(VoicePlayerState player, string room)
        {
            if (room != "LCVR")
                return;

            if (Plugin.Flags.HasFlag(Flags.VR))
                // Tell the player that joined that we are in VR
                SendHandshake(player.Name);
        }

        private static void OnPlayerLeave(VoicePlayerState player)
        {
            if (vrPlayers.TryGetValue(player.Name, out var networkPlayer))
                GameObject.Destroy(networkPlayer);

            vrPlayers.Remove(player.Name);
        }

        private static void OnPacketReceived(TextMessage packet)
        {
            var header = packet.Message[..5];

            switch (header)
            {
                case "HELLO":
                    HandleVRAnnouncement(packet.Sender);
                    break;

                case "UPRIG":
                    HandleRigUpdate(packet.Sender, packet.Message[5..]);
                    break;

                case "CAMFL":
                    if (!float.TryParse(packet.Message[5..], out var offset))
                        return;

                    HandleCameraFloorOffset(packet.Sender, offset);
                    break;
            }
        }

        // VR Announcements get sent for one of two reasons:
        //  - A client joins the lobby and announces themselves as VR
        //  - Another client joins the lobby and all other VR players announce to them that they're VR players
        private static void HandleVRAnnouncement(string sender)
        {
            // Ignore if player is already known to be VR
            if (vrPlayers.ContainsKey(sender))
                return;

            var player = dissonance.FindPlayer(sender);

            if (player == null)
                return;

            var playerObject = ((NfgoPlayer)player.Tracker).gameObject;
            var networkPlayer = playerObject.AddComponent<VRNetPlayer>();
            var playerController = playerObject.GetComponent<PlayerControllerB>();

            Logger.LogInfo($"Found VR player {player.Name}");

            vrPlayers.Add(player.Name, networkPlayer);

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

        private static void HandleRigUpdate(string sender, string packet)
        {
            if (!vrPlayers.TryGetValue(sender, out var player))
                return;

            var rig = Rig.Deserialize(Convert.FromBase64String(packet));
            player.UpdateTargetTransforms(rig);
        }

        private static void HandleCameraFloorOffset(string sender, float offset)
        {
            if (!vrPlayers.TryGetValue(sender, out var player))
                return;

            player.UpdateCameraFloorOffset(offset);
        }

        public struct Rig
        {
            public Vector3 rightHandPosition;
            public Vector3 rightHandEulers;

            public Vector3 leftHandPosition;
            public Vector3 leftHandEulers;

            public Vector3 cameraEulers;

            public bool isCrouching;
            public float rotationOffset;

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

                bw.Write(isCrouching);
                bw.Write(rotationOffset);

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
                    isCrouching = br.ReadBoolean(),
                    rotationOffset = br.ReadSingle(),
                };

                return rig;
            }
        }
    }
}
