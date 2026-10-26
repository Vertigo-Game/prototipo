using UnityEngine;

namespace HookPrototype
{
    public sealed class VerticalCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float verticalLookAhead = 2.2f;
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float minimumY = 1.4f;

        private float verticalVelocity;

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
            transform.position = new Vector3(0f, y, transform.position.z);
        }
    }
}
