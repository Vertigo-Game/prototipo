using System;
using UnityEngine;

namespace HookPrototype
{
    /// <summary>Marks the only objects that the prototype hook may attach to.</summary>
    public sealed class HookTarget : MonoBehaviour
    {
        [SerializeField] private bool isEnemy;

        public bool IsEnemy => isEnemy;

        /// <summary>Raised when a launched hook connects to this target.</summary>
        public event Action Hooked;

        /// <summary>Raised when a connected hook lets go without capturing this target.</summary>
        public event Action Released;

        /// <summary>Raised when the player reaches this enemy through the hook. A listener takes over
        /// what happens next (e.g. hiding it until it respawns); without one the enemy is destroyed.</summary>
        public event Action Captured;

        /// <summary>Raised when touching this enemy kills the player.</summary>
        public event Action KilledPlayer;

        public void Configure(bool enemy)
        {
            isEnemy = enemy;
        }

        public void NotifyHooked()
        {
            Hooked?.Invoke();
        }

        public void NotifyReleased()
        {
            Released?.Invoke();
        }

        public void NotifyKilledPlayer()
        {
            KilledPlayer?.Invoke();
        }

        public void Capture()
        {
            if (Captured != null)
            {
                Captured.Invoke();
                return;
            }

            // Deactivate right away: Destroy only happens at the end of the frame, and the
            // player's touch check must not see this enemy again in the meantime.
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
