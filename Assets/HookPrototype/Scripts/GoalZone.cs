using UnityEngine;

namespace HookPrototype
{
    public sealed class GoalZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.CompletePrototype();
            }
        }
    }
}
