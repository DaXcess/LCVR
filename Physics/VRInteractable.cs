using LCVR.Player;
using System;

namespace LCVR.Physics
{
    public interface VRInteractable
    {
        /// <summary>
        /// Determines the usage flags of this interactable object
        /// </summary>
        InteractableFlags Flags { get; }

        /// <summary>
        /// When a controller enters the collision zone of this collider
        /// </summary>
        void OnColliderEnter(VRInteractor interactor);

        /// <summary>
        /// When a controller enters the collision zone of this collider
        /// </summary>
        void OnColliderExit(VRInteractor interactor);

        /// <summary>
        /// When a controller that is already in the collision zone of this collider presses the interact button
        /// </summary>
        /// <returns>Whether or not this button press was acknowledged</returns>
        bool OnButtonPress(VRInteractor interactor);

        /// <summary>
        /// When a controller that is already in the collision zone of this collider releases the interact button
        /// </summary>
        void OnButtonRelease(VRInteractor interactor);
    }

    [Flags]
    public enum InteractableFlags
    {
        LeftHand = 1 << 0,
        RightHand = 1 << 1,

        BothHands = LeftHand | RightHand
    }
}
