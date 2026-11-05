using UnityEngine;

namespace HookPrototype
{
    /// <summary>A landing starts a one-way countdown, even if the player jumps away.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(PrototypePlatform))]
    public sealed class BreakablePlatform : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float breakDelay = 2f;
        [Tooltip("Ordered from intact to most cracked. Keep the same size and pivot in every sprite.")]
        [SerializeField] private Sprite[] damageStages = new Sprite[3];

        private SpriteRenderer visual;
        private BoxCollider2D surface;
        private float elapsed;
        private bool breaking;

        public bool IsBreaking => breaking;

        private void Awake()
        {
            visual = GetComponent<SpriteRenderer>();
            surface = GetComponent<BoxCollider2D>();
        }

        private void OnEnable()
        {
            elapsed = 0f;
            breaking = false;
            SetDamageStage(0);
        }

        private void Update()
        {
            if (!breaking)
            {
                return;
            }

            // Game time respects pause menus and the hook's aiming slow motion.
            elapsed += Time.deltaTime;
            if (elapsed >= breakDelay)
            {
                surface.enabled = false;
                visual.enabled = false;
                Destroy(gameObject);
                return;
            }

            if (damageStages != null && damageStages.Length > 0)
            {
                int stage = Mathf.Min((int)(elapsed / breakDelay * damageStages.Length), damageStages.Length - 1);
                SetDamageStage(stage);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryStartBreaking(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            // Also handles landing after initially touching a side of the platform.
            TryStartBreaking(collision);
        }

        private void TryStartBreaking(Collision2D collision)
        {
            if (breaking || collision.collider.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint2D contact = collision.GetContact(i);
                // On the platform, a player pressing down on its top has a downward normal.
                // Side contacts and hits from underneath must not start the countdown.
                if (contact.normal.y < -0.5f && contact.point.y > surface.bounds.center.y)
                {
                    breaking = true;
                    return;
                }
            }
        }

        private void SetDamageStage(int stage)
        {
            if (damageStages != null && stage < damageStages.Length && damageStages[stage] != null)
            {
                visual.sprite = damageStages[stage];
            }
        }

        private void OnValidate()
        {
            breakDelay = Mathf.Max(0.01f, breakDelay);
        }
    }
}
