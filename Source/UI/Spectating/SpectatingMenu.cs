using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LCVR.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LCVR.UI.Spectating;

[SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
public class SpectatingMenu : MonoBehaviour
{
    private const int InteractionLayer = 25; // Terrain, idc

    private static readonly int Visible = Animator.StringToHash("Visible");

    [SerializeField] private Animator animator;
    [SerializeField] private Transform raySource;
    
    // Players
    [SerializeField] private Transform playersContainer;
    [SerializeField] private GameObject spectatingPlayerPrefab;

    // Voting
    [SerializeField] private Button voteButton;
    [SerializeField] private RectTransform voteProgressBar;
    [SerializeField] private TextMeshProUGUI voteText;
    
    // Global Audio
    [SerializeField] private TextMeshProUGUI globalAudioButtonText;
    
    // Lighting
    [SerializeField] private TextMeshProUGUI lightToggleButtonText;
    [SerializeField] private Button fogToggleButton;
    [SerializeField] private TextMeshProUGUI fogToggleButtonText;

    private SphereCollider rayCollider;

    private float gazeTimer;

    // Players
    private bool hasLoadedSpectateUI;
    private List<SpectatingPlayer> playerBoxes = [];
    
    // Voting
    private bool isVoting;
    private float voteProgress;

    private void Awake()
    {
        // This component is disabled by default
        enabled = false;
        animator.GetComponent<Canvas>().worldCamera = VRSession.Instance.MainCamera;

        CreateRayReceiver();
    }

    private void OnEnable()
    {
        UpdateButtons();
    }

    private void OnDisable()
    {
        animator.SetBool(Visible, false);
    }

    private void Update()
    {
        HandleGaze();
        HandleVoting();

        voteText.text = HUDManager.Instance.holdButtonToEndGameEarlyVotesText.text;
    }

    public void PressedVoteButton()
    {
        if (VRSession.Instance.SpectateManager.Voted)
            return;

        isVoting = true;
    }

    public void ReleasedVoteButton()
    {
        isVoting = false;
    }

    public void TeleportToMainEntrance()
    {
        VRSession.Instance.SpectateManager.TeleportToMainEntrance();
    }

    public void TeleportToShip()
    {
        VRSession.Instance.SpectateManager.TeleportToShip();
    }

    public void ToggleGlobalAudio()
    {
        VRSession.Instance.SpectateManager.ToggleGlobalAudio();
        
        UpdateButtons();
    }

    public void ToggleLights()
    {
        VRSession.Instance.SpectateManager.ToggleLights();
        
        UpdateButtons();
    }

    public void ToggleFog()
    {
        VRSession.Instance.SpectateManager.ToggleFog();
        
        UpdateButtons();
    }
    
    public void UpdateBoxes()
    {
        if (!hasLoadedSpectateUI)
        {
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!player.isPlayerDead && !player.isPlayerControlled)
                    continue;
                
                var box = Instantiate(spectatingPlayerPrefab, playersContainer).GetComponent<SpectatingPlayer>();
                box.Setup(player);
                
                playerBoxes.Add(box);
            }

            hasLoadedSpectateUI = true;
        }

        // Check for players that DC'd
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            var idx = playerBoxes.FindIndex(box => box.IsPlayer(player));
            
            if (player.isPlayerDead || player.isPlayerControlled || idx == -1)
                continue;

            var go = playerBoxes[idx].gameObject;
            playerBoxes.RemoveAt(idx);
            
            Destroy(go);
        }

        foreach (var box in playerBoxes)
            box.UpdateState();
    }
    
    private void HandleGaze()
    {
        if (UnityEngine.Physics.Raycast(new Ray(raySource.position - raySource.forward * 0.5f, raySource.forward),
                out var hit, 4, 1 << InteractionLayer) && hit.collider == rayCollider)
            gazeTimer = 0.25f;

        if (!animator.GetBool(Visible) && gazeTimer > 0)
            StartCoroutine(Utils.FixYuckyScrollThing(animator));

        animator.SetBool(Visible, gazeTimer > 0);
        gazeTimer = Mathf.Max(gazeTimer - Time.deltaTime, 0);
    }

    private void HandleVoting()
    {
        if (VRSession.Instance.SpectateManager.Voted)
            return;

        if (StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.currentLevel.planetHasTime)
            return;

        voteProgress += Time.deltaTime * (isVoting ? 0.2f : -0.4f);
        voteProgress = Mathf.Clamp01(voteProgress);

        voteProgressBar.localScale = new Vector3(Mathf.SmoothStep(0, 1, voteProgress), 1, 1);

        if (voteProgress < 1)
            return;

        VRSession.Instance.SpectateManager.CastVote();

        voteButton.GetComponentInChildren<TextMeshProUGUI>().text = "<color=#BBBBBB>VOTE CAST!";
        voteButton.interactable = false;

        isVoting = false;
    }

    private void CreateRayReceiver()
    {
        rayCollider = new GameObject("UI Ray Receiver")
        {
            transform =
            {
                parent = VRSession.Instance.MainCamera.transform,
                localScale = new Vector3(0.5f, 0.3f, 0.3f)
            },
            layer = InteractionLayer
        }.AddComponent<SphereCollider>();
        rayCollider.isTrigger = true;
    }

    private void UpdateButtons()
    {
        var spectateManager = VRSession.Instance.SpectateManager;
        
        
        voteButton.gameObject.SetActive(!StartOfRound.Instance.shipIsLeaving &&
                                        StartOfRound.Instance.currentLevel.planetHasTime);

        globalAudioButtonText.text = spectateManager.GlobalAudio ? "<b>Everyone</b>" : "<b>Spectators</b> Only";
        lightToggleButtonText.text = spectateManager.LightEnabled ? "Disable Lights" : "Enable Lights";
        fogToggleButtonText.text = spectateManager.FogDisabled ? "Enable Fog" : "Disable Fog";

        fogToggleButton.interactable = !Plugin.Config.DisableVolumetrics.Value;
    }
}