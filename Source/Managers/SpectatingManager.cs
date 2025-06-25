using System.Collections;
using GameNetcodeStuff;
using LCVR.Assets;
using LCVR.Compatibility.MoreCompany;
using LCVR.Input;
using LCVR.Networking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

namespace LCVR.Managers;

public class SpectatingManager : MonoBehaviour
{
    private static readonly int GameOver = Animator.StringToHash("gameOver");
    private static readonly int Revive = Animator.StringToHash("revive");

    private PlayerControllerB localPlayer;

    private bool isSpectating;

    private bool wasInElevator;
    private bool wasInHangarShipRoom;
    private bool wasInsideFactory;

    private int lastSpectatedPlayer = 0;

    private GameObject spectatorLight;

    public bool Voted { get; private set; }
    public bool GlobalAudio { get; private set; }
    public bool LightEnabled { get; private set; }
    public bool FogDisabled { get; private set; }
    public PlayerControllerB SpectatedPlayer { get; private set; }

    private void Awake()
    {
        spectatorLight = Instantiate(AssetManager.SpectatorLight, transform);
        spectatorLight.SetActive(false);

        // Prevents CullFactory from culling the light
        spectatorLight.hideFlags |= HideFlags.DontSave;
    }

    private void Start()
    {
        localPlayer = GameNetworkManager.Instance.localPlayerController;
    }

    private void OnEnable()
    {
        Actions.Instance["SpectateNext"].performed += OnSpectateNext;
        Actions.Instance["SpectatePrevious"].performed += OnSpectatePrevious;
    }

    private void OnDisable()
    {
        Actions.Instance["SpectateNext"].performed -= OnSpectateNext;
        Actions.Instance["SpectatePrevious"].performed -= OnSpectatePrevious;
    }

    private void Update()
    {
        if (!localPlayer.isPlayerControlled || !localPlayer.isPlayerDead)
            return;

        ResetStamina();

        if (SpectatedPlayer)
            UpdateSpectatePlayer();
    }

    private void UpdateSpectatePlayer()
    {
        var movement = Actions.Instance["Move"].ReadValue<Vector2>().sqrMagnitude;

        if (SpectatedPlayer.isPlayerDead || !SpectatedPlayer.isPlayerControlled || movement > 0.01f)
        {
            StopSpectatingPlayer();

            return;
        }

        localPlayer.health = SpectatedPlayer.health;
        localPlayer.isCrouching = SpectatedPlayer.IsCrouching();

        if (localPlayer.transform.parent != SpectatedPlayer.transform.parent)
            localPlayer.transform.SetParent(SpectatedPlayer.transform.parent, true);

        TeleportLocalPlayer(SpectatedPlayer.transform.position, SpectatedPlayer.isInsideFactory,
            SpectatedPlayer.isInElevator, SpectatedPlayer.isInHangarShipRoom);
    }

    internal void PlayerDeathInit()
    {
        wasInElevator = localPlayer.isInElevator;
        wasInHangarShipRoom = localPlayer.isInHangarShipRoom;
        wasInsideFactory = localPlayer.isInsideFactory;
    }

    internal void PlayerDeath()
    {
        if (isSpectating || !localPlayer.AllowPlayerDeath())
            return;

        isSpectating = true;

        // Keep using FPV camera after death
        localPlayer.ChangeAudioListenerToObject(localPlayer.gameplayCamera.gameObject);
        StartOfRound.Instance.SwitchCamera(localPlayer.gameplayCamera);

        // Set up player for free roam spectating
        localPlayer.isPlayerControlled = true;
        localPlayer.thisPlayerModelArms.enabled = true;
        localPlayer.spectatedPlayerScript = localPlayer;

        // Apply environmental effects based on where we died
        localPlayer.isInElevator = wasInElevator;
        localPlayer.isInHangarShipRoom = wasInHangarShipRoom;
        localPlayer.isInsideFactory = wasInsideFactory;

        var audioPreset = (wasInsideFactory, wasInHangarShipRoom) switch
        {
            (true, _) => 2,
            (_, true) => 3,
            (false, false) => 1,
        };

        var presets = FindObjectOfType<AudioReverbPresets>();
        if (presets)
            presets.audioPresets[audioPreset].ChangeAudioReverbForPlayer(localPlayer);

        EnableLargeDoorCollisions(false);
        EnableDoorCollisions(false);
        EnableShipDoorCollisions(false);
        SpecialFixEnemies();

        // Clear spectator text
        HUDManager.Instance.spectatingPlayerText.text = "";

        // Clear fear effect
        StartOfRound.Instance.fearLevel = 0;

        // Disable interactors
        EnableInteractions(false);

        VRSession.Instance.VolumeManager.Death();
        VRSession.Instance.StartCoroutine(ShowDeathScreen());
    }

    internal void PlayerRevive()
    {
        isSpectating = false;

        if (!localPlayer.isPlayerDead)
            return;

        VRSession.Instance.VolumeManager.Saturation = 0;
        VRSession.Instance.VolumeManager.VignetteIntensity = 0;

        // Set up params to allow the game to perform the normal revive sequence
        localPlayer.thisPlayerModelArms.enabled = false;
        localPlayer.isPlayerControlled = false;
        localPlayer.takingFallDamage = false;
        localPlayer.isCameraDisabled = true;

        EnableInteractions(true);
        EnableShipDoorCollisions(true);
        EnableDoorCollisions(true);
        EnableLargeDoorCollisions(true);

        ToggleGlobalAudio(false);
        ToggleFog(true);
        ToggleLights(false);
        StopSpectatingPlayer();

        VRSession.Instance.HUD.SpectatingMenu.enabled = false;
    }

    internal void CastVote()
    {
        Voted = true;

        TimeOfDay.Instance.VoteShipToLeaveEarly();
    }

    internal void SpectatePlayer(PlayerControllerB targetPlayer)
    {
        if (targetPlayer.isPlayerDead || !targetPlayer.isPlayerControlled)
            return;
        
        StopSpectatingPlayer();

        if (targetPlayer == SpectatedPlayer)
            return;

        localPlayer.thisController.enabled = false;
        localPlayer.enabled = false;
        SpectatedPlayer = targetPlayer;
        SpectatedPlayer.DisablePlayerModel(SpectatedPlayer.gameObject);
        SpectatedPlayer.playerBetaBadgeMesh.enabled = false;
        SpectatedPlayer.playerBadgeMesh.GetComponent<MeshRenderer>().enabled = false;
        localPlayer.isCrouching = SpectatedPlayer.IsCrouching();
        
        if (Compat.IsLoaded(Compat.MoreCompany))
            MoreCompanyCompatibility.EnablePlayerCosmetics(SpectatedPlayer, false);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void StopSpectatingPlayer()
    {
        if (!SpectatedPlayer)
            return;

        SpectatedPlayer.DisablePlayerModel(SpectatedPlayer.gameObject, !SpectatedPlayer.isPlayerDead, true);
        SpectatedPlayer.playerBetaBadgeMesh.enabled = true;
        SpectatedPlayer.playerBadgeMesh.GetComponent<MeshRenderer>().enabled = true;
        localPlayer.thisController.enabled = true;
        localPlayer.enabled = true;

        if (Compat.IsLoaded(Compat.MoreCompany))
            MoreCompanyCompatibility.EnablePlayerCosmetics(SpectatedPlayer, true);

        SpectatedPlayer = null;
        
        VRSession.Instance.HUD.SpectatingMenu.UpdateBoxes();
    }

    internal void TeleportToPlayer(PlayerControllerB targetPlayer)
    {
        if (!localPlayer.isPlayerDead)
            return;

        if (targetPlayer == localPlayer)
            return;

        if (targetPlayer.isPlayerDead)
        {
            if (!NetworkSystem.Instance.TryGetPlayer((ushort)targetPlayer.playerClientId, out var networkPlayer))
                return;

            if (networkPlayer.GetSpectatorGhost() is not { } ghost)
                return;

            TeleportLocalPlayer(ghost.transform.parent.TransformPoint(ghost.PlayerPosition), ghost.InInterior,
                ghost.ParentedToShip, ghost.InHangarShipRoom);

            return;
        }

        if (!targetPlayer.isPlayerControlled)
            return;

        TeleportLocalPlayer(targetPlayer.transform.position, targetPlayer.isInsideFactory, targetPlayer.isInElevator,
            targetPlayer.isInHangarShipRoom);
    }

    internal void TeleportToMainEntrance()
    {
        if (!localPlayer.isPlayerDead)
            return;

        StopSpectatingPlayer();
        localPlayer.TeleportPlayer(RoundManager.FindMainEntrancePosition(true, !localPlayer.isInsideFactory));
    }

    internal void TeleportToShip()
    {
        if (!localPlayer.isPlayerDead)
            return;

        StopSpectatingPlayer();
        TeleportLocalPlayer(StartOfRound.Instance.GetPlayerSpawnPosition((int)localPlayer.playerClientId), false, true,
            true);
    }

    internal void ToggleGlobalAudio(bool? enable = null)
    {
        if (enable.HasValue)
            GlobalAudio = enable.Value;
        else
            GlobalAudio = !GlobalAudio;

        StartOfRound.Instance.UpdatePlayerVoiceEffects();
    }

    internal void ToggleLights(bool? enable = null)
    {
        if (enable.HasValue)
            LightEnabled = enable.Value;
        else
            LightEnabled = !LightEnabled;

        spectatorLight.SetActive(LightEnabled);
    }

    internal void ToggleFog(bool? enable = null)
    {
        if (Plugin.Config.DisableVolumetrics.Value)
            return;

        if (enable.HasValue)
            FogDisabled = !enable.Value;
        else
            FogDisabled = !FogDisabled;

        var hdCamera = VRSession.Instance.MainCamera.GetComponent<HDAdditionalCameraData>();

        if (FogDisabled)
            hdCamera.DisableQualitySetting(FrameSettingsField.Volumetrics);
        else
            hdCamera.EnableQualitySetting(FrameSettingsField.Volumetrics);
    }

    private void TeleportLocalPlayer(Vector3 position, bool inFactory, bool inElevator, bool inHangar)
    {
        localPlayer.TeleportPlayer(position, enableController: localPlayer.thisController.enabled);
        localPlayer.isInsideFactory = inFactory;
        localPlayer.isInElevator = inElevator;
        localPlayer.isInHangarShipRoom = inHangar;

        var audioPreset = (inFactory, inHangar) switch
        {
            (true, _) => 2,
            (_, true) => 3,
            (false, false) => 1,
        };

        var presets = FindObjectOfType<AudioReverbPresets>();
        if (presets)
            presets.audioPresets[audioPreset].ChangeAudioReverbForPlayer(localPlayer);
    }

    private IEnumerator ShowDeathScreen()
    {
        HUDManager.Instance.gameOverAnimator.ResetTrigger(GameOver);
        HUDManager.Instance.gameOverAnimator.ResetTrigger(Revive);

        yield return new WaitForSeconds(2.5f);

        HUDManager.Instance.gameOverAnimator.SetTrigger(GameOver);

        yield return new WaitForSeconds(4.3f);

        VRSession.Instance.HUD.SpectatingMenu.enabled = true;

        // Fixes an issue where if you pick up an item while dying it stays in your inventory
        localPlayer.DropAllHeldItems(false);
    }

    private void ResetStamina()
    {
        localPlayer.sprintMeter = 1;
        localPlayer.isExhausted = false;
        localPlayer.criticallyInjured = false;
        localPlayer.takingFallDamage = false;
    }

    /// <summary>
    /// Whether to enable the VR interactors
    /// </summary>
    private void EnableInteractions(bool enable)
    {
        VRSession.Instance.LocalPlayer.LeftHandInteractor.enabled = enable;
        VRSession.Instance.LocalPlayer.RightHandInteractor.enabled = enable;
    }

    /// <summary>
    /// Whether to enable collisions on the large ship-controlled doors
    /// </summary>
    private void EnableLargeDoorCollisions(bool enable)
    {
        var powerDoors = FindObjectsOfType<PowerSwitchable>();
        foreach (var powerDoor in powerDoors)
        {
            var leftDoor = powerDoor.transform.Find("BigDoorLeft")?.GetComponent<Collider>();
            var rightDoor = powerDoor.transform.Find("BigDoorRight")?.GetComponent<Collider>();

            if (!leftDoor || !rightDoor)
                continue;

            leftDoor.isTrigger = !enable;
            rightDoor.isTrigger = !enable;
        }
    }

    /// <summary>
    /// Whether to enable collisions on normal doors
    /// </summary>
    private void EnableDoorCollisions(bool enable)
    {
        var doors = FindObjectsOfType<DoorLock>();
        foreach (var door in doors)
            door.GetComponent<Collider>().isTrigger = !enable;
    }

    /// <summary>
    /// Whether to enable collisions on the ship door
    /// </summary>
    private void EnableShipDoorCollisions(bool enable)
    {
        var shipDoorContainer = FindObjectOfType<HangarShipDoor>().transform;
        var shipDoorLeft = shipDoorContainer.Find("HangarDoorLeft (1)");
        var shipDoorRight = shipDoorContainer.Find("HangarDoorRight (1)");
        var shipDoorWall = shipDoorContainer.Find("Cube");

        shipDoorLeft.GetComponent<BoxCollider>().isTrigger = !enable;
        shipDoorRight.GetComponent<BoxCollider>().isTrigger = !enable;
        shipDoorWall.GetComponent<BoxCollider>().isTrigger = !enable;
    }

    /// <summary>
    /// Some special fixes for specific enemies
    /// </summary>
    private void SpecialFixEnemies()
    {
        // Force nutcrackers to lose aggro
        var nutcrackers = FindObjectsOfType<NutcrackerEnemyAI>();
        foreach (var nutcracker in nutcrackers)
            if (nutcracker.lastPlayerSeenMoving == (int)localPlayer.playerClientId)
                nutcracker.lastPlayerSeenMoving = -1;
    }

    private void OnSpectateNext(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !localPlayer.isPlayerDead || VRSession.Instance.HUD.SpectatingMenu.IsOpen)
            return;
        
        for (var i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
        {
            var nextIndex = (lastSpectatedPlayer + i + 1) % StartOfRound.Instance.allPlayerScripts.Length;
            var player = StartOfRound.Instance.allPlayerScripts[nextIndex];
            
            if (player == localPlayer || player.isPlayerDead || !player.isPlayerControlled)
                continue;

            lastSpectatedPlayer = nextIndex;
            
            if (SpectatedPlayer)
            {
                // If we are already spectating this player, just break, no others players left to spectate
                if (SpectatedPlayer == player)
                    return;
                
                SpectatePlayer(player);
            }
            else
                TeleportToPlayer(player);

            return;
        }
    }

    private void OnSpectatePrevious(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !localPlayer.isPlayerDead || VRSession.Instance.HUD.SpectatingMenu.IsOpen)
            return;

        for (var i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
        {
            var prevIndex = (lastSpectatedPlayer - i - 1 + StartOfRound.Instance.allPlayerScripts.Length) %
                            StartOfRound.Instance.allPlayerScripts.Length;
            var player = StartOfRound.Instance.allPlayerScripts[prevIndex];

            if (player == localPlayer || player.isPlayerDead || !player.isPlayerControlled)
                continue;

            lastSpectatedPlayer = prevIndex;

            if (SpectatedPlayer)
            {
                // If we are already spectating this player, just break, no others players left to spectate
                if (SpectatedPlayer == player)
                    return;
                
                SpectatePlayer(player);
            }
            else
                TeleportToPlayer(player);

            return;
        }
    }
}