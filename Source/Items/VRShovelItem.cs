using LCVR.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCVR.Items;

// The holding is insanely scuffed but I really have no clue how to do it properly I'm not /that/ good of a developer
internal class VRShovelItem : VRItem<Shovel>
{
    private readonly Vector3 positionOffset = new(-0.09f, 0, 0.25f);

    private Transform interactTransform;
    private readonly Queue<Vector3> positions = new();
    private Vector3 lastPosition = Vector3.zero;

    private bool isHitting;
    private bool hasSwung;
    private float lastActionTime;
    private float timeNotReeledUp;

    private new void Awake()
    {
        base.Awake();

        CancelGameUpdate = true;

        if (!IsLocal)
            return;

        var @object = new GameObject("Shovel Interaction Point");

        interactTransform = @object.transform;
        interactTransform.SetParent(transform, false);
        interactTransform.localRotation = Quaternion.identity;
        interactTransform.localPosition = new Vector3(0, 0, 1.25f);
    }

    private void OnDestroy()
    {
        if (!IsLocal)
            return;

        positions.Clear();

        if (item.reelingUp)
            item.StartCoroutine(CancelShovelSwing());

        Destroy(interactTransform.gameObject);
    }

    protected override void OnUpdate()
    {
        // This part is my attempt to hold an item with two hands
        // Some numbers might not make sense but that is because they probably don't

        if (!IsLocal)
        {
            transform.position = networkPlayer.LeftItemHolder.position;
            transform.LookAt(networkPlayer.RightItemHolder.position);

            var rotation2 = transform.rotation;

            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 360 - networkPlayer.LeftItemHolder.eulerAngles.z);
            transform.position += rotation2 * positionOffset;

            return;
        }

        var player = VRSession.Instance.LocalPlayer;

        transform.position = player.leftItemHolder.position;
        transform.LookAt(player.rightItemHolder.position);

        var rotation = transform.rotation;

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 360 - player.leftItemHolder.eulerAngles.z);
        transform.position += rotation * positionOffset;

        // Good enough failsafe to detect teleports, which could cause the shovel to swing
        if (Vector3.Distance(interactTransform.position, lastPosition) > 5)
            positions.Clear();

        lastPosition = interactTransform.position;

        positions.Enqueue(interactTransform.position);
        if (positions.Count > 5)
            positions.Dequeue();

        var vector = interactTransform.position - VRSession.Instance.MainCamera.transform.position;
        var forward = VRSession.Instance.MainCamera.transform.forward;

        var dot = Vector3.Dot(vector, forward);

        if (dot < 0)
        {
            // Also check for speed to prevent looking around causing the shovel to be reeled up
            if (GetAverageSpeed() > 3 && !item.reelingUp)
                ReelUpShovel();
        }
        else
        {
            if (item.reelingUp)
            {
                if (!hasSwung && GetAverageSpeed() > 8)
                {
                    item.SwingShovel();
                    hasSwung = true;
                }

                timeNotReeledUp += Time.deltaTime;

                if (timeNotReeledUp > 0.4f)
                {
                    Logger.LogDebug("Not reeled up and took too long to swing, hit cancelled");

                    item.StartCoroutine(CancelShovelSwing());
                    lastActionTime = Time.realtimeSinceStartup;
                }
            }
        }

        if (dot > 0.8 && item.reelingUp && !isHitting)
        {
            var averageSpeed = GetAverageSpeed();

            if (GetAverageSpeed() > 8)
            {
                Logger.LogDebug($"Shovel Swing detected with average speed: {averageSpeed}");
                HitShovel();
            }
        }
    }

    private void ReelUpShovel()
    {
        if (!CanUseItem())
            return;

        if (Time.realtimeSinceStartup - lastActionTime < 0.7f)
            return;

        lastActionTime = Time.realtimeSinceStartup;
        timeNotReeledUp = 0;

        item.reelingUp = true;
        item.previousPlayerHeldBy = item.playerHeldBy;
        item.playerHeldBy.activatingItem = true;
        item.playerHeldBy.twoHanded = true;
        item.playerHeldBy.playerBodyAnimator.ResetTrigger("shovelHit");
        item.playerHeldBy.playerBodyAnimator.SetBool("reelingUp", true);
        item.shovelAudio.PlayOneShot(item.reelUp);
        item.ReelUpSFXServerRpc();
    }

    private void HitShovel()
    {
        if (!CanUseItem())
            return;

        isHitting = true;
        lastActionTime = Time.realtimeSinceStartup;

        item.StartCoroutine(ShovelSwing());
    }

    private IEnumerator ShovelSwing()
    {
        item.HitShovel();
        yield return new WaitForSeconds(0.3f);
        item.reelingUp = false;
        isHitting = false;
        hasSwung = false;
    }

    private IEnumerator CancelShovelSwing()
    {
        item.reelingUp = false;
        hasSwung = false;
        item.previousPlayerHeldBy.twoHanded = false;

        item.SwingShovel(true);
        yield return new WaitForSeconds(0.13f);
        item.HitShovel(true);
    }

    private float GetAverageSpeed()
    {
        var positions = this.positions.ToArray();
        var distance = 0f;

        for (var i = 1; i < positions.Length; i++)
        {
            distance += Vector3.Distance(positions[i - 1], positions[i]);
        }

        var totalTime = (positions.Length - 1) * Time.deltaTime;
        var averageSpeed = distance / totalTime;

        return averageSpeed;
    }
}
