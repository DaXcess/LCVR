using System.Linq;
using LCVR.Assets;
using LCVR.Managers;
using UnityEngine;

namespace LCVR.Items;

public class VRKnife : VRItem<KnifeItem>
{
    private GameObject interactionTarget;
    private GameObject knifeCollider;

    private Vector3 previous;
    private float attackTimer;

    public float Speed { get; private set; }
    private Vector3 Position => VRSession.Instance.LocalPlayer.transform.InverseTransformPoint(transform.position);

    private new void Awake()
    {
        base.Awake();

        if (!IsLocal)
            return;

        interactionTarget = Instantiate(AssetManager.Interactable, VRSession.Instance.MainCamera.transform);
        interactionTarget.transform.localPosition = new Vector3(0, 0, 0.5f);
        interactionTarget.transform.localScale = Vector3.one * 0.3f;
        interactionTarget.AddComponent<KnifeInteractor>();
        interactionTarget.AddComponent<Rigidbody>().isKinematic = true;

        knifeCollider = Instantiate(AssetManager.Interactable, transform);
        knifeCollider.transform.localPosition = new Vector3(0, 0, 7.25f);
        knifeCollider.transform.localScale = new Vector3(1.2f, 3, 12.9f);

        previous = Position;
    }

    protected override void OnUpdate()
    {
        if (!IsLocal)
            return;

        Speed = (Position - previous).magnitude / Time.deltaTime;
        previous = Position;
    }

    private void OnDestroy()
    {
        Destroy(interactionTarget);
        Destroy(knifeCollider);
    }

    internal void Attack()
    {
        if (Time.realtimeSinceStartup < attackTimer)
            return;

        attackTimer = Time.realtimeSinceStartup + 0.15f;
        item.ItemActivate(true);
    }

    internal static RaycastHit[] GetKnifeHits(KnifeItem knife)
    {
        var tf = knife.transform;

        var forwardHits = UnityEngine.Physics.SphereCastAll(tf.position, 0.3f, tf.forward, 0.75f, knife.knifeMask,
            QueryTriggerInteraction.Collide);
        var upHits = UnityEngine.Physics.SphereCastAll(tf.position, 0.3f, -tf.up, 0.75f, knife.knifeMask,
            QueryTriggerInteraction.Collide);

        RaycastHit[] allHits = [..forwardHits, ..upHits];

        return allHits.GroupBy(x => x.collider).Select(x => x.First()).ToArray();
    }
}

public class KnifeInteractor : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var knife = other.GetComponentInParent<VRKnife>();
        if (knife?.Speed > 6)
            knife.Attack();
    }
}