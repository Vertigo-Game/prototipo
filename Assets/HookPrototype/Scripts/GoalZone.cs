using UnityEngine;
using Vertigo.Gameplay;

namespace HookPrototype
{
    public sealed class GoalZone : MonoBehaviour
    {
        [SerializeField] private string proximaCena;

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(proximaCena))
            {
                player.CompletePrototype();
                return;
            }

            TransicaoDeFase.Iniciar(player, proximaCena);
        }
    }
}
