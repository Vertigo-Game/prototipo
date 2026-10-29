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
    }
}
