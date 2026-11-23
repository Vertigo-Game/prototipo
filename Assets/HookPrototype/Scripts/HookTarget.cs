using System;
using UnityEngine;

namespace HookPrototype
{
    /// <summary>Marks the only objects that the prototype hook may attach to.</summary>
    public sealed class HookTarget : MonoBehaviour
    {
        [SerializeField] private bool isEnemy;
        [SerializeField, Tooltip("Impede que o gancho se prenda a este alvo, mantendo o perigo por contato.")]
        private bool blockHook;
        [SerializeField]
        [Tooltip("Ligado (padrão): o jogador fica preso parado no ponto, pelas mãos. Desligado: fica pendurado numa corda e balança.")]
        private bool stopAtAnchor = true;
        [SerializeField, Min(0f)]
        [Tooltip("Velocidade vertical ao capturar este inimigo. Zero usa a velocidade normal do jogador.")]
        private float verticalCaptureBounceSpeed;
        [SerializeField]
        [Tooltip("Mantém o impulso lateral padrão quando a velocidade vertical é personalizada.")]
        private bool keepHorizontalCaptureBounce;

        public bool IsEnemy => isEnemy;
        public bool CanBeHooked => !blockHook;
        public bool StopsAtAnchor => !isEnemy && stopAtAnchor;
        public float VerticalCaptureBounceSpeed => verticalCaptureBounceSpeed;
        public bool KeepHorizontalCaptureBounce => keepHorizontalCaptureBounce;

        /// <summary>Raised when a launched hook connects to this target.</summary>
        public event Action Hooked;

        /// <summary>Raised when a connected hook lets go without capturing this target.</summary>
        public event Action Released;

        /// <summary>Raised when the player reaches this enemy through the hook. A listener takes over
        /// what happens next (e.g. hiding it until it respawns); without one the enemy is destroyed.</summary>
        public event Action Captured;

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
