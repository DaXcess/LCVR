using LCVR.Assets;
using LCVR.Physics;
using LCVR.Player;
using UnityEngine;

namespace LCVR.UI;

public class BeltBagUI : MonoBehaviour
{
    private const float UI_SCALE_GROUNDED = 0.004f;
    private const float UI_SCALE_IN_HAND = 0.003f;
    
    private BeltBagInventoryUI inventoryUI;
    private BeltBagItem currentBeltBag;
    private Canvas canvas;

    private void Awake()
    {
        inventoryUI = FindObjectOfType<BeltBagInventoryUI>();

        canvas = new GameObject("VR Belt Bag UI")
        {
            transform =
            {
                localScale = Vector3.one * UI_SCALE_GROUNDED
            }
        }.AddComponent<Canvas>();
        canvas.worldCamera = VRSession.Instance.MainCamera;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1;

        var beltUI = GameObject.Find("BeltBagUI").transform; // Fingers crossed
        beltUI.SetParent(canvas.transform, false);
        beltUI.localPosition = Vector3.zero;
        beltUI.localRotation = Quaternion.identity;
        
        // Remove X button
        beltUI.transform.Find("Buttons/ExitButton").gameObject.SetActive(false);
        
        // Set up interactables
        for (var i = 0; i < inventoryUI.inventorySlots.Length; i++)
        {
            var interactableSlot = Instantiate(AssetManager.Interactable, inventoryUI.inventorySlots[i].transform)
                .AddComponent<BeltBagInventorySlot>();

            interactableSlot.transform.localScale = new Vector3(30, 30, 1);
            interactableSlot.inventory = inventoryUI;
            interactableSlot.slot = i;
        }
    }

    private void Update()
    {
        if (inventoryUI.currentBeltBag is null)
            return;

        if (inventoryUI.currentBeltBag != currentBeltBag)
            UpdateCanvas();

        currentBeltBag = inventoryUI.currentBeltBag;

        if (currentBeltBag.playerHeldBy is null)
            UpdateGrounded();
        else if (currentBeltBag.playerHeldBy == StartOfRound.Instance.localPlayerController)
            UpdateHand();
    }

    private void UpdateCanvas()
    {
        var itemTransform = inventoryUI.currentBeltBag.transform;
        var canvasTransform = canvas.transform;

        canvasTransform.SetParent(itemTransform);
        canvasTransform.localEulerAngles = new Vector3(290, 180, 0);
        canvasTransform.localPosition = new Vector3(0, 0.2f, 1);
    }

    private void UpdateGrounded()
    {
        if (currentBeltBag.currentPlayerChecking != StartOfRound.Instance.localPlayerController)
            return;
        
        canvas.transform.localScale = Vector3.one * UI_SCALE_GROUNDED;

        var vpPos = VRSession.Instance.MainCamera.WorldToViewportPoint(currentBeltBag.transform.position);
        if (vpPos.x is >= 0 and <= 1 && vpPos.y is >= 0 and <= 1f && vpPos.z > 0f)
            return;

        StartOfRound.Instance.localPlayerController.SetInSpecialMenu(false);
    }

    private void UpdateHand()
    {
        canvas.transform.localScale = Vector3.one * UI_SCALE_IN_HAND;
    }
}

public class BeltBagInventorySlot : MonoBehaviour, VRInteractable
{
    public BeltBagInventoryUI inventory;
    public int slot;
    
    public InteractableFlags Flags => InteractableFlags.BothHands;
    
    public void OnColliderEnter(VRInteractor interactor)
    {
    }

    public void OnColliderExit(VRInteractor interactor)
    {
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        if (inventory.currentBeltBag is not { } beltBag || beltBag.objectsInBag.Count < slot ||
            beltBag.objectsInBag[slot] == null)
            return true;
        
        inventory.RemoveItemFromUI(slot);
        return true;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
    }
}