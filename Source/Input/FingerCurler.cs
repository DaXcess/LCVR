using LCVR.Networking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.Input;

public class FingerCurler
{
    public class Finger(Transform root, bool isLeft, Vector3 firstRotation, Vector3 secondRotation)
    {
        private readonly Transform bone01 = root;
        private readonly Transform bone02 = root.GetChild(0);

        private readonly Quaternion bone01Rotation = Quaternion.Euler(firstRotation);
        private readonly Quaternion bone02Rotation = Quaternion.Euler(secondRotation);

        private readonly float sign = isLeft ? 1f : -1f;

        public float curl;

        public void Update()
        {
            var angle = Mathf.Lerp(0f, 80f, curl);

            bone01.localRotation = Quaternion.AngleAxis(-angle, Vector3.right) * bone01Rotation;
            bone02.localRotation = Quaternion.AngleAxis(angle * sign, Vector3.forward) * bone02Rotation;
        }
    }

    public class Thumb(Transform root, Vector3 firstRotation, Vector3 secondRotation)
    {
        private Transform bone01 = root;
        private Transform bone02 = root.GetChild(0);

        public float curl;

        private readonly Quaternion bone01Rotation =
            Quaternion.AngleAxis(-30f, Vector3.right) * Quaternion.Euler(firstRotation);

        private readonly Quaternion bone02Rotation = Quaternion.Euler(secondRotation);

        public void Update()
        {
            var angle = Mathf.Lerp(-80f, 80f, curl);

            bone01.localRotation = bone01Rotation;
            bone02.localRotation = Quaternion.AngleAxis(-angle, Vector3.right) * bone02Rotation;
        }
    }

    public readonly Thumb thumbFinger;
    public readonly Finger indexFinger;
    public readonly Finger middleFinger;
    public readonly Finger ringFinger;
    public readonly Finger pinkyFinger;

    protected bool forceFist;

    public FingerCurler(Transform hand, bool isLeft)
    {
        if (isLeft)
        {
            thumbFinger = new Thumb(hand.Find("finger1.L"), new Vector3(35f, -90f, 0f), new Vector3(-25f, 0f, 3f));
            indexFinger = new Finger(hand.Find("finger2.L"), true, new Vector3(5f, -90f, 0f),
                new Vector3(3f, -3f, 33f));
            middleFinger = new Finger(hand.Find("finger3.L"), true, new Vector3(-182f, 90f, -176f),
                new Vector3(1f, -5f, 22f));
            ringFinger = new Finger(hand.Find("finger4.L"), true, new Vector3(186f, 90f, 177f),
                new Vector3(6f, -3f, 29f));
            pinkyFinger = new Finger(hand.Find("finger5.L"), true, new Vector3(183f, 80f, 176f),
                new Vector3(-17f, 8f, 27f));
        }
        else
        {
            thumbFinger = new Thumb(hand.Find("finger1.R"), new Vector3(143f, -90f, -180f), new Vector3(-26f, 0f, -5f));
            indexFinger = new Finger(hand.Find("finger2.R"), false, new Vector3(-193f, -90f, 176f),
                new Vector3(-9f, 0f, -21f));
            middleFinger = new Finger(hand.Find("finger3.R"), false, new Vector3(-186f, -90f, 180f),
                new Vector3(-6f, 0f, -24f));
            ringFinger = new Finger(hand.Find("finger4.R"), false, new Vector3(180f, -90f, -177f),
                new Vector3(-9f, 0f, -25f));
            pinkyFinger = new Finger(hand.Find("finger5.R"), false, new Vector3(182f, -90f, -168f),
                new Vector3(-13f, 3f, -33f));
        }
    }

    internal Fingers GetCurls()
    {
        return new Fingers()
        {
            Thumb = (byte)(thumbFinger.curl * 255f),
            Index = (byte)(indexFinger.curl * 255f),
            Middle = (byte)(middleFinger.curl * 255f),
            Ring = (byte)(ringFinger.curl * 255f),
            Pinky = (byte)(pinkyFinger.curl * 255f),
        };
    }

    internal void SetCurls(Fingers fingers)
    {
        thumbFinger.curl = fingers.Thumb / 255f;
        indexFinger.curl = fingers.Index / 255f;
        middleFinger.curl = fingers.Middle / 255f;
        ringFinger.curl = fingers.Ring / 255f;
        pinkyFinger.curl = fingers.Pinky / 255f;
    }

    public virtual void Update()
    {
        if (forceFist)
        {
            thumbFinger.curl = 1;
            indexFinger.curl = Mathf.Lerp(indexFinger.curl, 1, 0.5f);
            middleFinger.curl = ringFinger.curl = pinkyFinger.curl = Mathf.Lerp(middleFinger.curl, 1, 0.5f);
        }
        
        thumbFinger.Update();
        indexFinger.Update();
        middleFinger.Update();
        ringFinger.Update();
        pinkyFinger.Update();
    }

    public void ForceFist(bool enabled)
    {
        forceFist = enabled;
    }
}

public class VRFingerCurler(Transform hand, bool isLeft) : FingerCurler(hand, isLeft)
{
    private const float THUMBS_UP_TRESHOLD = 0.8f;
    private const float THUMB_STATE_UP = 0f;
    private const float THUMB_STATE_DEFAULT = 0.5f;
    private const float THUMB_STATE_DOWN = 1f;

    private readonly string hand = isLeft ? "L" : "R";

    private InputAction ThumbAction => Actions.Instance[$"Thumb{hand}"];
    private InputAction IndexAction => Actions.Instance[$"Index{hand}"];
    private InputAction OthersAction => Actions.Instance[$"Others{hand}"];

    public bool IsFist => indexFinger.curl > 0.75f && middleFinger.curl > 0.75f;
    public bool IsPointer => indexFinger.curl <= 0.75f && middleFinger.curl > 0.75f;
    public bool IsHand => middleFinger.curl <= 0.75f;

    public bool Enabled { get; set; } = true;

    public override void Update()
    {
        if (!Enabled)
            return;
        
        UpdateCurls();

        base.Update();
    }

    private void UpdateCurls()
    {
        if (forceFist)
            return;
        
        // Thumb Up = 0f
        // Thumb Default = 0.5f
        // Thumb Down = 1f
        var thumb = ThumbAction.ReadValue<float>() != 0f ? THUMB_STATE_DOWN : THUMB_STATE_DEFAULT;
        var index = IndexAction.ReadValue<float>();
        var grip = OthersAction.ReadValue<float>();

        var thumbsUp = index > THUMBS_UP_TRESHOLD && grip > THUMBS_UP_TRESHOLD &&
                       Mathf.Approximately(thumb, THUMB_STATE_DEFAULT);

        thumbFinger.curl = Mathf.Lerp(thumbFinger.curl, thumbsUp ? THUMB_STATE_UP : thumb, 0.5f);
        indexFinger.curl = index;
        middleFinger.curl = grip;
        ringFinger.curl = grip;
        pinkyFinger.curl = grip;
    }
}