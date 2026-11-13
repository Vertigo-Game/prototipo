using UnityEngine;

namespace HookPrototype
{
    // Roda antes dos outros LateUpdate: os avisos "E" leem a posição da câmera e precisam do valor deste frame.
    [DefaultExecutionOrder(-100)]
    public sealed class VerticalCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float verticalLookAhead = 2.2f;
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float minimumY = 1.4f;
        [SerializeField] private bool followHorizontally;
        [SerializeField] private float horizontalSmoothTime = 0.18f;

        private float verticalVelocity;
        private float horizontalVelocity;
        private PlayerController targetPlayer;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            targetPlayer = null;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (targetPlayer == null)
            {
                targetPlayer = target.GetComponent<PlayerController>();
            }

            // No Game Over a câmera desce além do mínimo para o corpo de quem caiu no buraco continuar à vista.
            float desiredY = target.position.y + verticalLookAhead;
            if (targetPlayer == null || !targetPlayer.IsDead)
            {
                desiredY = Mathf.Max(minimumY, desiredY);
            }
            float y = Mathf.SmoothDamp(transform.position.y, desiredY, ref verticalVelocity, smoothTime);
            float x = followHorizontally
                ? Mathf.SmoothDamp(transform.position.x, target.position.x, ref horizontalVelocity, horizontalSmoothTime)
                : 0f;
            transform.position = new Vector3(x, y, transform.position.z);
        }
    }
}
