using UnityEngine;

namespace LCVR.Player;

/// <summary>
/// Helper class for getting all the transforms for the player model and IK
/// </summary>
public class Bones(Transform player)
{
    public Transform Model => player.Find("ScavengerModel");
    public Transform Metarig => Model.Find("metarig");

    #region Local bones and rigs
    public Transform ModelArmsOnly => Metarig.Find("ScavengerModelArmsOnly");
    public Transform LocalMetarig => ModelArmsOnly.Find("metarig");
    public Transform LocalSpine => LocalMetarig.Find("spine.003");

    public Transform LocalLeftShoulder => LocalSpine.Find("shoulder.L");
    public Transform LocalRightShoulder => LocalSpine.Find("shoulder.R");

    public Transform LocalLeftUpperArm => LocalLeftShoulder.Find("arm.L_upper");
    public Transform LocalRightUpperArm => LocalRightShoulder.Find("arm.R_upper");

    public Transform LocalLeftLowerArm => LocalLeftUpperArm.Find("arm.L_lower");
    public Transform LocalRightLowerArm => LocalRightUpperArm.Find("arm.R_lower");

    public Transform LocalLeftHand => LocalLeftLowerArm.Find("hand.L");
    public Transform LocalRightHand => LocalRightLowerArm.Find("hand.R");

    public Transform LocalItemHolder => LocalRightHand.Find("LocalItemHolder");

    public Transform LocalArmsRig => LocalSpine.Find("RigArms");

    public Transform LocalLeftArmRig => LocalArmsRig.Find("LeftArm");
    public Transform LocalRightArmRig => LocalArmsRig.Find("RightArm");

    public Transform LocalLeftArmRigHint => LocalLeftArmRig.Find("LeftArm_hint");
    public Transform LocalRightArmRigHint => LocalRightArmRig.Find("RightArm_hint");

    public Transform LocalLeftArmRigTarget => LocalLeftArmRig.Find("ArmsLeftArm_target");
    public Transform LocalRightArmRigTarget => LocalRightArmRig.Find("ArmsRightArm_target");
    #endregion

    #region Full model bones and rigs
    public Transform Rig => Metarig.Find("Rig 1");
    public Transform Spine => Metarig.Find("spine/spine.001/spine.002/spine.003");

    public Transform LeftShoulder => Spine.Find("shoulder.L");
    public Transform RightShoulder => Spine.Find("shoulder.R");

    public Transform LeftUpperArm => LeftShoulder.Find("arm.L_upper");
    public Transform RightUpperArm => RightShoulder.Find("arm.R_upper");

    public Transform LeftLowerArm => LeftUpperArm.Find("arm.L_lower");
    public Transform RightLowerArm => RightUpperArm.Find("arm.R_lower");

    public Transform LeftHand => LeftLowerArm.Find("hand.L");
    public Transform RightHand => RightLowerArm.Find("hand.R");

    public Transform LeftArmRig => Rig.Find("LeftArm");
    public Transform RightArmRig => Rig.Find("RightArm");

    public Transform LeftArmRigHint => LeftArmRig.Find("LeftArm_hint");
    public Transform RightArmRigHint => RightArmRig.Find("RightArm_hint");

    public Transform LeftArmRigTarget => Spine.Find("LeftArm_target");
    public Transform RightArmRigTarget => Spine.Find("RightArm_target");
    #endregion

    public void ResetToPrefabPositions()
    {
        var prefab = StartOfRound.Instance.playerPrefab;
        var prefabMetarig = prefab.transform.Find("ScavengerModel/metarig");

        RecurseApplyTransforms(prefabMetarig, Metarig);
    }

    private void RecurseApplyTransforms(Transform from, Transform to)
    {
        to.transform.localPosition = from.transform.localPosition;
        to.transform.localRotation = from.transform.localRotation;
        to.transform.localScale = from.transform.localScale;

        foreach (var child in from.GetChildren())
        {
            var toChild = to.Find(child.name);

            if (toChild != null)
                RecurseApplyTransforms(child, toChild);
        }
    }
}
