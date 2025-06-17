using GameNetcodeStuff;
using LCVR.Managers;
using LCVR.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LCVR.UI.Spectating;

public class SpectatingPlayer : MonoBehaviour
{
    public RawImage playerIcon;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI playerAlive;
    public Image speakingIcon;

    public GameObject teleportButtonObject;
    
    public GameObject spectateButtonObject;
    public Image spectateImage;
    public Image stopSpectateImage;
    
    private PlayerControllerB playerController;

    private float speakingTimer;
    private bool speaking;

    public bool Alive => !playerController.isPlayerDead;

    private void Update()
    {
        speakingTimer += Time.deltaTime;

        if (speakingTimer > 0.2f)
        {
            UpdateSpeaking();

            speakingTimer = 0;
        }

        speakingIcon.transform.localScale = Vector3.Lerp(speakingIcon.transform.localScale,
            speaking ? Vector3.one : Vector3.zero, 0.6f);
    }

    private void UpdateSpeaking()
    {
        if (!StartOfRound.Instance.voiceChatModule)
            return;

        if (!playerController.isPlayerDead && !playerController.isPlayerControlled)
            return;

        if (playerController == GameNetworkManager.Instance.localPlayerController)
        {
            var voiceState =
                StartOfRound.Instance.voiceChatModule.FindPlayer(StartOfRound.Instance.voiceChatModule.LocalPlayerName);

            if (voiceState != null)
                speaking = voiceState.IsSpeaking && voiceState.Amplitude > 0.005f;
        }
        else if (playerController.voicePlayerState != null)
        {
            speaking = playerController.voicePlayerState.IsSpeaking &&
                       playerController.voicePlayerState.Amplitude > 0.005f &&
                       !playerController.voicePlayerState.IsLocallyMuted;
        }
    }

    public void Setup(PlayerControllerB player)
    {
        gameObject.SetActive(true);

        playerController = player;
        playerName.text = player.playerUsername;

        if (!GameNetworkManager.Instance.disableSteam)
            HUDManager.FillImageWithSteamProfile(playerIcon, player.playerSteamId);
    }

    public void UpdateState()
    {
        playerAlive.text = playerController.isPlayerDead ? "(Dead)" : "<color=#3AFF34>(Alive)";
        
        if (playerController == GameNetworkManager.Instance.localPlayerController ||
            (playerController.isPlayerDead && !NetworkSystem.Instance.IsInVR((ushort)playerController.playerClientId)))
            teleportButtonObject.SetActive(false);
        else
            teleportButtonObject.SetActive(true);
        
        spectateButtonObject.SetActive(!playerController.isPlayerDead);
        spectateImage.enabled = VRSession.Instance.SpectateManager.SpectatedPlayer != playerController;
        stopSpectateImage.enabled = !spectateImage.enabled;
    }

    public bool IsPlayer(PlayerControllerB player) => playerController == player;
    
    // Button events

    public void Teleport()
    {
        VRSession.Instance.SpectateManager.TeleportToPlayer(playerController);
    }

    public void Spectate()
    {
        VRSession.Instance.SpectateManager.SpectatePlayer(playerController);
        
        UpdateState();
    }
}