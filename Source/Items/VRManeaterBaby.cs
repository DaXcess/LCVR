using System.Collections;
using LCVR.Input;
using LCVR.Managers;
using UnityEngine;

namespace LCVR.Items;

internal class VRManEaterBaby : VRItem<CaveDwellerPhysicsProp>
{
    private ShakeDetector shake;
    private ShakeDetector shakeBig;
    
    private Coroutine stopRockingCoroutine;
    private Coroutine rockHardCoroutine;
    
    private int rockHardTicks;
    private bool isRockHard;
    
    private new void Awake()
    {
        base.Awake();

        if (!IsLocal)
            return;

        var source = VRSession.Instance.LocalPlayer.PrimaryController.transform;
        
        shake = new ShakeDetector(source, 0.015f, true);
        shakeBig = new ShakeDetector(source, 0.05f, true);
        
        shake.onShake += OnShakeMotion;
        shakeBig.onShake += OnShakeBigMotion;
    }

    private void OnDestroy()
    {
        if (IsLocal)
            shake.onShake -= OnShakeMotion;
    }

    private void OnShakeMotion()
    {
        if (player.currentlyHeldObjectServer != item || !item.IsOwner)
            return;

        if (player.isGrabbingObjectAnimation || player.inTerminalMenu || player.inSpecialInteractAnimation)
            return;

        if (isRockHard)
            return;
        
        if (stopRockingCoroutine != null)
            StopCoroutine(stopRockingCoroutine);

        StartRocking(false);
    }

    private void OnShakeBigMotion()
    {
        if (player.currentlyHeldObjectServer != item || !item.IsOwner)
            return;

        if (player.isGrabbingObjectAnimation || player.inTerminalMenu || player.inSpecialInteractAnimation)
            return;

        // If already rocking hard, let every tick reset the stop timer
        if (isRockHard)
        {
            if (stopRockingCoroutine != null)
                StopCoroutine(stopRockingCoroutine);

            StartRocking(true);

            return;
        }
        
        rockHardTicks++;

        if (rockHardCoroutine != null)
            return;

        rockHardCoroutine = StartCoroutine(KeepRockingHard());
    }

    private void StartRocking(bool rockHard)
    {
        isRockHard = rockHard;

        item.caveDwellerScript.rockingBaby = rockHard ? 2 : 1;
        item.SetRockingBabyServerRpc(rockHard);

        stopRockingCoroutine = StartCoroutine(StopRockingBaby(rockHard ? 0.5f : 1f));
    }

    private IEnumerator StopRockingBaby(float timeout)
    {
        yield return new WaitForSeconds(timeout);

        if (player.currentlyHeldObjectServer != item)
            yield break;

        isRockHard = false;
        item.caveDwellerScript.rockingBaby = 0;
        item.StopRockingBabyServerRpc();
        stopRockingCoroutine = null;
    }

    private IEnumerator KeepRockingHard()
    {
        yield return new WaitForSeconds(0.75f);

        if (rockHardTicks > 5)
        {
            if (stopRockingCoroutine != null)
                StopCoroutine(stopRockingCoroutine);

            StartRocking(true);
        }
        
        rockHardTicks = 0;
        rockHardCoroutine = null;
    }

    protected override void OnUpdate()
    {
        if (!IsLocal)
            return;
        
        shake.Update();
        shakeBig.Update();
    }
}