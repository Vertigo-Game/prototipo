using UnityEngine;

namespace HookPrototype
{
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

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            float desiredY = Mathf.Max(minimumY, target.position.y + verticalLookAhead);
            float y = Mathf.SmoothDamp(transform.position.y, desiredY, ref verticalVelocity, smoothTime);
            float x = followHorizontally
                ? Mathf.SmoothDamp(transform.position.x, target.position.x, ref horizontalVelocity, horizontalSmoothTime)
                : 0f;
            transform.position = new Vector3(x, y, transform.position.z);
        }
    }
}
