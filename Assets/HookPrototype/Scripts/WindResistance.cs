using UnityEngine;

namespace HookPrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WindArea))]
    [DefaultExecutionOrder(100)] // Aplica o limite depois do movimento do jogador, antes da simulação física.
    public sealed class WindResistance : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField, Min(0f), Tooltip("Velocidade horizontal máxima contra o vento, em unidades por segundo.")]
        private float maxSpeedAgainstWind = 3.5f;

        private WindArea wind;
        private Rigidbody2D body;

        private void Awake()
        {
            wind = GetComponent<WindArea>();
            if (player != null)
            {
                body = player.GetComponent<Rigidbody2D>();
            }
        }

        private void FixedUpdate()
        {
            if (body == null || !body.simulated || !player.isActiveAndEnabled || player.IsDead ||
                !wind.isActiveAndEnabled || !wind.TryGetArea(out Rect area))
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            float windDirection = wind.Direction.x;
            float maxSpeed = Mathf.Max(0f, maxSpeedAgainstWind);
            if (-velocity.x * windDirection <= maxSpeed)
            {
                return;
            }

            Bounds bounds = player.BodyBounds;
            if (bounds.max.x > area.xMin && bounds.min.x < area.xMax &&
                bounds.max.y > area.yMin && bounds.min.y < area.yMax)
            {
                velocity.x = -windDirection * maxSpeed;
                body.linearVelocity = velocity;
            }
        }
    }
}
