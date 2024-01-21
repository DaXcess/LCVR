using LCVR.Networking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.Input
{
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
                float angle = Mathf.Lerp(0f, 80f, curl);

                bone01.localRotation = Quaternion.AngleAxis(-angle, Vector3.right) * bone01Rotation;
                bone02.localRotation = Quaternion.AngleAxis(angle * sign, Vector3.forward) * bone02Rotation;
            }
        }

        public class Thumb
        {
            public Transform bone01;
            public Transform bone02;

            public float curl;

            private readonly Quaternion bone01Rotation;
            private readonly Quaternion bone02Rotation;

            public Thumb(Transform root, Vector3 firstRotation, Vector3 secondRotation)
            {
                bone01 = root;
                bone02 = root.GetChild(0);

                var offset = Quaternion.AngleAxis(-30f, Vector3.right);

                bone01Rotation = offset * Quaternion.Euler(firstRotation);
                bone02Rotation = Quaternion.Euler(secondRotation);
            }

            public void Update()
            {
                float angle = Mathf.Lerp(-80f, 80f, curl);

                bone01.localRotation = bone01Rotation;
                bone02.localRotation = Quaternion.AngleAxis(-angle, Vector3.right) * bone02Rotation;
            }
        }

        public Thumb thumbFinger;
        public Finger indexFinger;
        public Finger middleFinger;
        public Finger ringFinger;
        public Finger pinkyFinger;

        public FingerCurler(Transform hand, bool isLeft)
        {
            if (isLeft)
            {
                thumbFinger = new Thumb(hand.Find($"finger1.L"), new Vector3(35f, -90f, 0f), new Vector3(-25f, 0f, 3f));
                indexFinger = new Finger(hand.Find($"finger2.L"), isLeft, new Vector3(5f, -90f, 0f), new Vector3(3f, -3f, 33f));
                middleFinger = new Finger(hand.Find($"finger3.L"), isLeft, new Vector3(-182f, 90f, -176f), new Vector3(1f, -5f, 22f));
                ringFinger = new Finger(hand.Find($"finger4.L"), isLeft, new Vector3(186f, 90f, 177f), new Vector3(6f, -3f, 29f));
                pinkyFinger = new Finger(hand.Find($"finger5.L"), isLeft, new Vector3(183f, 80f, 176f), new Vector3(-17f, 8f, 27f));
            }
            else
            {
                thumbFinger = new Thumb(hand.Find($"finger1.R"), new Vector3(143f, -90f, -180f), new Vector3(-26f, 0f, -5f));
                indexFinger = new Finger(hand.Find($"finger2.R"), isLeft, new Vector3(-193f, -90f, 176f), new Vector3(-9f, 0f, -21f));
                middleFinger = new Finger(hand.Find($"finger3.R"), isLeft, new Vector3(-186f, -90f, 180f), new Vector3(-6f, 0f, -24f));
                ringFinger = new Finger(hand.Find($"finger4.R"), isLeft, new Vector3(180f, -90f, -177f), new Vector3(-9f, 0f, -25f));
                pinkyFinger = new Finger(hand.Find($"finger5.R"), isLeft, new Vector3(182f, -90f, -168f), new Vector3(-13f, 3f, -33f));
            }
        }

        public DNet.Fingers GetCurls()
        {
            return new DNet.Fingers()
            {
                thumb = thumbFinger.curl,
                index = indexFinger.curl,
                middle = middleFinger.curl,
                ring = ringFinger.curl,
                pinky = pinkyFinger.curl,
            };
        }

        public void SetCurls(DNet.Fingers fingers)
        {
            thumbFinger.curl = fingers.thumb;
            indexFinger.curl = fingers.index;
            middleFinger.curl = fingers.middle;
            ringFinger.curl = fingers.ring;
            pinkyFinger.curl = fingers.pinky;
        }

        public virtual void Update()
        {
            thumbFinger.Update();
            indexFinger.Update();
            middleFinger.Update();
            ringFinger.Update();
            pinkyFinger.Update();
        }
    }

    public class VRFingerCurler : FingerCurler
    {
        private const float THUMBS_UP_TRESHOLD = 0.8f;
        private const float THUMB_STATE_UP = 0f;
        private const float THUMB_STATE_DEFAULT = 0.5f;
        private const float THUMB_STATE_DOWN = 1f;

        private readonly InputAction thumbAction;
        private readonly InputAction indexAction;
        private readonly InputAction othersAction;

        public VRFingerCurler(Transform hand, bool isLeft) : base(hand, isLeft)
        {
            var map = isLeft ? "Left Hand Fingers" : "Right Hand Fingers";

            thumbAction = Actions.FindAction($"{map}/Thumb");
            indexAction = Actions.FindAction($"{map}/Index");
            othersAction = Actions.FindAction($"{map}/Others");
        }

        public override void Update()
        {
            UpdateCurls();

            base.Update();
        }

        private void UpdateCurls()
        {
            // Thumb Up = 0f
            // Thumb Default = 0.5f
            // Thumb Down = 1f
            var thumb = thumbAction.ReadValue<float>() != 0f ? THUMB_STATE_DOWN : THUMB_STATE_DEFAULT;
            var index = indexAction.ReadValue<float>();
            var grip = othersAction.ReadValue<float>();

            var thumbsUp = index > THUMBS_UP_TRESHOLD && grip > THUMBS_UP_TRESHOLD && thumb == THUMB_STATE_DEFAULT;

            thumbFinger.curl = Mathf.Lerp(thumbFinger.curl, thumbsUp ? THUMB_STATE_UP : thumb, 0.5f);
            indexFinger.curl = index;
            middleFinger.curl = grip;
            ringFinger.curl = grip;
            pinkyFinger.curl = grip;
        }
    }
}
