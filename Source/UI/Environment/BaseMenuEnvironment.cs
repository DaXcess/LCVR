using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace LCVR.UI.Environment;

public class BaseMenuEnvironment : MonoBehaviour
{
    [SerializeField] protected MenuScene menuScene;
    [SerializeField] protected Transform primaryCanvasAnchor;
    [SerializeField] protected Camera mainCamera;
    [SerializeField] protected Transform menuPlayer;

    private MenuManager menuManager;
    
    private Transform primaryCanvas;

    protected void Awake()
    {
        menuManager = FindObjectOfType<MenuManager>();
        
        switch (menuScene)
        {
            case MenuScene.InitScene:
            case MenuScene.MainMenu:
                HijackCanvas(FindObjectOfType<MenuManager>().transform.parent.GetComponent<Canvas>(),
                    GameObject.Find("UICamera").GetComponent<Camera>());
                break;

            case MenuScene.PauseMenu:
                HijackCanvas(GameObject.Find("Systems/UI/Canvas").GetComponent<Canvas>(),
                    GameObject.Find("Systems/UI/UICamera").GetComponent<Camera>());
                break;
        }
    }

    private void LateUpdate()
    {
        primaryCanvas.transform.position = primaryCanvasAnchor.transform.position;
        primaryCanvas.transform.rotation = primaryCanvasAnchor.transform.rotation;
    }

    private void HijackCanvas(Canvas canvas, Camera uiCamera)
    {
        uiCamera.enabled = false;
        primaryCanvas = canvas.transform;
        
        // Will be replaced by XR-compatible input module
        FindObjectOfType<InputSystemUIInputModule>().enabled = false;

        Destroy(canvas.GetComponent<GraphicRaycaster>());
        canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;
        canvas.transform.localScale = Vector3.one * 0.008f;
        canvas.transform.position = primaryCanvasAnchor.transform.position;
        canvas.transform.rotation = primaryCanvasAnchor.transform.rotation;
        
        // Yucky workaround
        if (primaryCanvasAnchor.GetComponentInChildren<MeshRenderer>() is { } renderer)
            renderer.material.renderQueue -= 1;
    }
    
    public void PlayButtonPressSfx()
    {
        menuManager?.PlayConfirmSFX();
    }

    public void PlayCancelSfx()
    {
        menuManager?.PlayCancelSFX();
    }

    public void PlayHoverSfx()
    {
        menuManager?.MenuAudio.PlayOneShot(GameNetworkManager.Instance.buttonSelectSFX);
    }

    public void PlayChangeSfx()
    {
        menuManager?.MenuAudio.PlayOneShot(GameNetworkManager.Instance.buttonTuneSFX);
    }

    public enum MenuScene
    {
        InitScene,
        MainMenu,
        PauseMenu
    }
}
