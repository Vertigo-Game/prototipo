using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HookPrototype
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float runSpeed = 6.5f;
        [SerializeField] private float groundAcceleration = 55f;
        [SerializeField] private float airAcceleration = 16f;
        [SerializeField] private float jumpSpeed = 10.5f;
        [SerializeField] private float fallLimit = -5f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 14f;
        [SerializeField] private float dashDuration = 0.14f;

        [Header("Hook")]
        [SerializeField] private float hookRange = 7.5f;
        [SerializeField] private float hookPullSpeed = 19f;
        [SerializeField] private float hookPullAcceleration = 110f;
        [SerializeField] private float targetAcquireRadius = 0.5f;
        [SerializeField] private float targetReachDistance = 0.72f;
        [SerializeField, Range(0.05f, 1f)] private float aimingTimeScale = 0.25f;
        [SerializeField] private float enemyBounceSpeed = 14f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private LineRenderer aimLine;
        private HookTarget currentTarget;
        private Vector2 aimDirection = Vector2.up;
        private float moveInput;
        private float dashTimer;
        private float baseGravityScale;
        private bool dashAvailable = true;
        private bool aiming;
        private bool pulling;
        private bool previousHookHeld;
        private bool completed;

        public bool DashAvailable => dashAvailable;
        public bool IsPulling => pulling;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            baseGravityScale = body.gravityScale;

            aimLine = gameObject.AddComponent<LineRenderer>();
            aimLine.positionCount = 2;
            aimLine.useWorldSpace = true;
            aimLine.startWidth = 0.075f;
            aimLine.endWidth = 0.035f;
            aimLine.numCapVertices = 4;
            aimLine.sortingOrder = 20;
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
            aimLine.enabled = false;
        }

        private void Update()
        {
            if (ReadRestartPressed())
            {
                RestartPrototype();
                return;
            }

            if (completed)
            {
                return;
            }

            moveInput = ReadHorizontal();
            bool hookHeld = ReadHookHeld();
            bool hookPressed = hookHeld && !previousHookHeld;
            bool hookReleased = !hookHeld && previousHookHeld;

            if (pulling)
            {
                if (hookPressed || ReadCancelPressed())
                {
                    CancelHook(true);
                }
            }
            else
            {
                if (hookPressed)
                {
                    BeginAiming();
                }

                if (aiming && hookHeld)
                {
                    UpdateAim();
                }

                if (aiming && hookReleased)
                {
                    LaunchHook();
                }
            }

            if (!pulling && !aiming && ReadJumpPressed() && IsGrounded())
            {
                Vector2 velocity = body.linearVelocity;
                velocity.y = jumpSpeed;
                body.linearVelocity = velocity;
            }

            if (!pulling && ReadDashPressed() && dashAvailable)
            {
                StartDash();
            }

            if (transform.position.y < fallLimit)
            {
                RestartPrototype();
                return;
            }

            previousHookHeld = hookHeld;
        }

        private void FixedUpdate()
        {
            if (completed)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            if (pulling)
            {
                PullTowardTarget();
                return;
            }

            if (dashTimer > 0f)
            {
                dashTimer -= Time.fixedDeltaTime;
                if (dashTimer <= 0f)
                {
                    body.gravityScale = baseGravityScale;
                }

                return;
            }

            bool grounded = IsGrounded();
            if (grounded)
            {
                dashAvailable = true;
            }

            float acceleration = grounded ? groundAcceleration : airAcceleration;
            Vector2 velocity = body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, moveInput * runSpeed, acceleration * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
        }

        private void BeginAiming()
        {
            aiming = true;
            currentTarget = null;
            aimLine.enabled = true;
            SetTimeScale(aimingTimeScale);
            UpdateAim();
        }

        private void UpdateAim()
        {
            Vector2 origin = body.position;
            Vector2 pointerWorld = ReadPointerWorld();
            Vector2 candidateDirection = pointerWorld - origin;
            if (candidateDirection.sqrMagnitude > 0.01f)
            {
                aimDirection = candidateDirection.normalized;
            }

            currentTarget = FindTarget(origin, aimDirection);
            Vector2 end = currentTarget != null
                ? (Vector2)currentTarget.transform.position
                : origin + aimDirection * hookRange;

            aimLine.SetPosition(0, origin);
            aimLine.SetPosition(1, end);
            Color lineColor = currentTarget != null ? new Color(0.3f, 1f, 0.65f, 1f) : new Color(1f, 1f, 1f, 0.55f);
            aimLine.startColor = lineColor;
            aimLine.endColor = lineColor;
        }

        private HookTarget FindTarget(Vector2 origin, Vector2 direction)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, targetAcquireRadius, direction, hookRange);
            HookTarget best = null;
            float bestDistance = float.MaxValue;

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider == bodyCollider)
                {
                    continue;
                }

                HookTarget target = hit.collider.GetComponent<HookTarget>();
                if (target == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(origin, target.transform.position);
                if (distance < bestDistance && HasLineOfSight(origin, target, distance))
                {
                    best = target;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private bool HasLineOfSight(Vector2 origin, HookTarget target, float targetDistance)
        {
            Vector2 direction = ((Vector2)target.transform.position - origin).normalized;
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, targetDistance);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider == bodyCollider)
                {
                    continue;
                }

                if (hit.collider.GetComponent<HookTarget>() == target)
                {
                    return true;
                }

                if (hit.collider.GetComponent<PrototypePlatform>() != null)
                {
                    return false;
                }
            }

            return true;
        }

        private void LaunchHook()
        {
            // Slow motion exists only while choosing a target.
            SetTimeScale(1f);
            aiming = false;

            if (currentTarget == null)
            {
                aimLine.enabled = false;
                return;
            }

            pulling = true;
            body.gravityScale = 0f;
            dashTimer = 0f;
            aimLine.enabled = true;
        }

        private void PullTowardTarget()
        {
            // A connected hook must always run at full game speed.
            if (!Mathf.Approximately(Time.timeScale, 1f))
            {
                SetTimeScale(1f);
            }

            if (currentTarget == null)
            {
                CancelHook(false);
                return;
            }

            Vector2 targetPosition = currentTarget.transform.position;
            Vector2 toTarget = targetPosition - body.position;
            float distance = toTarget.magnitude;

            aimLine.SetPosition(0, body.position);
            aimLine.SetPosition(1, targetPosition);

            if (distance <= targetReachDistance)
            {
                ReachTarget();
                return;
            }

            Vector2 desiredVelocity = toTarget.normalized * hookPullSpeed;
            Vector2 preservedVelocity = body.linearVelocity * 0.32f;
            body.linearVelocity = Vector2.MoveTowards(
                preservedVelocity,
                desiredVelocity,
                hookPullAcceleration * Time.fixedDeltaTime);
        }

        private void ReachTarget()
        {
            SetTimeScale(1f);

            HookTarget reachedTarget = currentTarget;
            Vector2 incomingVelocity = body.linearVelocity;
            bool enemy = reachedTarget.IsEnemy;

            pulling = false;
            currentTarget = null;
            aimLine.enabled = false;
            body.gravityScale = baseGravityScale;

            if (enemy)
            {
                dashAvailable = true;
                float steering = Mathf.Abs(moveInput) > 0.1f ? moveInput : Mathf.Sign(incomingVelocity.x);
                body.linearVelocity = new Vector2(
                    incomingVelocity.x * 0.32f + steering * 3.75f,
                    enemyBounceSpeed + Mathf.Max(0f, incomingVelocity.y * 0.18f));
                Destroy(reachedTarget.gameObject);
            }
            else
            {
                Vector2 releasedVelocity = incomingVelocity;
                releasedVelocity.y = Mathf.Max(releasedVelocity.y, 5.5f);
                body.linearVelocity = releasedVelocity;
            }
        }

        private void CancelHook(bool preserveVelocity)
        {
            aiming = false;
            pulling = false;
            currentTarget = null;
            aimLine.enabled = false;
            body.gravityScale = baseGravityScale;
            SetTimeScale(1f);

            if (!preserveVelocity)
            {
                body.linearVelocity *= 0.75f;
            }
        }

        private void StartDash()
        {
            if (aiming)
            {
                CancelHook(true);
            }

            Vector2 dashDirection = ReadDashDirection();
            if (dashDirection.sqrMagnitude < 0.01f)
            {
                dashDirection = aimDirection.sqrMagnitude > 0.01f
                    ? aimDirection
                    : new Vector2(Mathf.Approximately(moveInput, 0f) ? 1f : Mathf.Sign(moveInput), 0f);
            }

            dashAvailable = false;
            dashTimer = dashDuration;
            body.gravityScale = 0f;
            body.linearVelocity = dashDirection.normalized * dashSpeed;
        }

        private bool IsGrounded()
        {
            Bounds bounds = bodyCollider.bounds;
            Vector2 center = new Vector2(bounds.center.x, bounds.min.y - 0.06f);
            Vector2 size = new Vector2(bounds.size.x * 0.78f, 0.12f);
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(center, size, 0f);

            foreach (Collider2D overlap in overlaps)
            {
                if (overlap != bodyCollider && !overlap.isTrigger &&
                    overlap.GetComponent<PrototypePlatform>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        public void CompletePrototype()
        {
            completed = true;
            CancelHook(true);
            body.linearVelocity = Vector2.zero;
            body.gravityScale = 0f;
        }

        private void RestartPrototype()
        {
            SetTimeScale(1f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static void SetTimeScale(float scale)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = 0.02f * scale;
        }

        private void OnDisable()
        {
            SetTimeScale(1f);
        }

        private void OnGUI()
        {
            GUIStyle title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUIStyle text = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };
            GUIStyle centered = new GUIStyle(title)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28
            };

            GUI.Box(new Rect(12f, 12f, 410f, 112f), GUIContent.none);
            GUI.Label(new Rect(24f, 19f, 380f, 28f), "HOOK CLIMB — PROTÓTIPO", title);
            GUI.Label(new Rect(24f, 48f, 390f, 72f),
                "A/D: mover   Espaço: pular   Shift: dash (8 direções)\n" +
                "Segure botão direito/E: mirar em slow motion\n" +
                "Solte: lançar Hook   Pressione novamente: cancelar   R: reiniciar", text);

            string dashText = dashAvailable ? "DASH PRONTO" : "DASH GASTO";
            Color previousColor = GUI.color;
            GUI.color = dashAvailable ? new Color(0.3f, 1f, 0.7f) : new Color(1f, 0.45f, 0.45f);
            GUI.Box(new Rect(Screen.width - 150f, 16f, 132f, 34f), dashText);
            GUI.color = previousColor;

            if (completed)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 75f, 460f, 150f), GUIContent.none);
                GUI.Label(new Rect(Screen.width * 0.5f - 215f, Screen.height * 0.5f - 45f, 430f, 45f),
                    "TOPO ALCANÇADO!", centered);
                GUI.Label(new Rect(Screen.width * 0.5f - 215f, Screen.height * 0.5f + 15f, 430f, 30f),
                    "Pressione R para testar novamente.", new GUIStyle(text) { alignment = TextAnchor.MiddleCenter });
            }
        }

        private static float ReadHorizontal()
        {
#if ENABLE_INPUT_SYSTEM
            float value = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) value -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) value += 1f;
            }
            if (Gamepad.current != null && Mathf.Abs(Gamepad.current.leftStick.x.ReadValue()) > Mathf.Abs(value))
            {
                value = Gamepad.current.leftStick.x.ReadValue();
            }
            return Mathf.Clamp(value, -1f, 1f);
#else
            return Input.GetAxisRaw("Horizontal");
#endif
        }

        private static Vector2 ReadDashDirection()
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 direction = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) direction.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) direction.x += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) direction.y -= 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) direction.y += 1f;
            }
            if (Gamepad.current != null && Gamepad.current.leftStick.ReadValue().sqrMagnitude > direction.sqrMagnitude)
            {
                direction = Gamepad.current.leftStick.ReadValue();
            }
            return direction.normalized;
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
#endif
        }

        private static Vector2 ReadPointerWorld()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return Vector2.up;
            }
#if ENABLE_INPUT_SYSTEM
            Vector2 screenPosition = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.75f);
#else
            Vector2 screenPosition = Input.mousePosition;
#endif
            Vector3 worldPosition = camera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -camera.transform.position.z));
            return worldPosition;
        }

        private static bool ReadHookHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return (Mouse.current != null && Mouse.current.rightButton.isPressed) ||
                   (Keyboard.current != null && Keyboard.current.eKey.isPressed);
#else
            return Input.GetMouseButton(1) || Input.GetKey(KeyCode.E);
#endif
        }

        private static bool ReadJumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        private static bool ReadDashPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.LeftShift);
#endif
        }

        private static bool ReadCancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private static bool ReadRestartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }
    }
}
