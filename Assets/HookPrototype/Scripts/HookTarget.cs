using UnityEngine;

namespace HookPrototype
{
    /// <summary>Marks the only objects that the prototype hook may attach to.</summary>
    public sealed class HookTarget : MonoBehaviour
    {
        [SerializeField] private bool isEnemy;

        public bool IsEnemy => isEnemy;

        public void Configure(bool enemy)
        {
            isEnemy = enemy;
        }
    }
}
