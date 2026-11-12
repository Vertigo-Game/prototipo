using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Vertigo.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HookPrototype
{
    // O que matou o Marcos. Queda e Perigo (espinhos/inimigos) tocam marcus_death_1;
    // Congelamento ainda não tem animação própria.
    public enum CausaDaMorte
    {
        Queda,
        Perigo,
        Congelamento
    }

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

        [Header("Rope")]
        [SerializeField] private float climbSpeed = 4f;

        [Header("Hook")]
        [Tooltip("Desmarcado: o gancho só funciona depois de pegar o item (ItemGancho).")]
        [SerializeField] private bool startsWithHook = true;
        [SerializeField] private float hookRange = 7.5f;
        [SerializeField] private float hookPullSpeed = 19f;
        [SerializeField] private float hookPullAcceleration = 110f;
        [SerializeField] private float targetAcquireRadius = 0.5f;
        [SerializeField] private float targetReachDistance = 0.72f;
        [SerializeField, Range(0.05f, 1f)] private float aimingTimeScale = 0.25f;
        [SerializeField] private float enemyBounceSpeed = 14f;
        [SerializeField] private float momentumMinSpeed = 13f;
        [SerializeField] private float momentumMaxSpeed = 22f;
        [SerializeField] private float momentumDeceleration = 14f;
        [SerializeField] private float momentumEndSpeed = 6f;

        [Header("Death")]
        [Tooltip("Tamanho da colisão do corpo deitado, em unidades do mundo. A base continua na altura dos pés.")]
        [SerializeField] private Vector2 corpseColliderSize = new Vector2(1.3f, 0.6f);

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int HookModeHash = Animator.StringToHash("HookMode");
        private static readonly int InteractHash = Animator.StringToHash("Interact");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private static readonly int DeathStateHash = Animator.StringToHash("marcus_death_1");

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private Animator animator;
        private SpriteRenderer visual;
        private LineRenderer aimLine;
        private HookTarget currentTarget;
        private Vector2 aimDirection = Vector2.up;
        private float moveInput;
        private float dashTimer;
        private float dashDirection;
        private float baseGravityScale;
        private bool dashAvailable = true;
        private bool aiming;
        private bool pulling;
        private bool hanging;
        private bool coasting;
        private float facing = 1f;
        private bool previousHookHeld;
        private bool completed;
        private bool hookCollected;
        private bool climbing;
        private int climbStartFrame;
        private float climbInput;
        private float ropeX;
        private float ropeMinY;
        private float ropeMaxY;
        private bool dead;
        private bool corpseFrozen;

        public bool IsDead => dead;
        public bool CanGrabRope => !dead && !climbing && !aiming && !pulling && !coasting && !completed && dashTimer <= 0f;
        public Bounds BodyBounds => bodyCollider.bounds;
        public bool HasHook => startsWithHook || hookCollected;

        // Chamado pelo item do gancho. Vale só até reiniciar a fase (a cena é recarregada).
        public void CollectHook()
        {
            hookCollected = true;
        }
        private bool restarting;

        public bool DashAvailable => dashAvailable;
        public bool IsPulling => pulling;
        public bool CanInteract => !dead && !aiming && !pulling && !coasting && !completed && dashTimer <= 0f && IsGrounded();

        // Corda: o Marcos fica preso na coluna x e sobe/desce entre minY e maxY (posição dos pés).
        // Sai com pulo, dash, E ou clique, como nos pontos fixos do gancho.
        public void BeginClimb(float x, float minY, float maxY)
        {
            climbing = true;
            climbStartFrame = Time.frameCount;
            climbInput = 0f;
            ropeX = x;
            ropeMinY = minY;
            ropeMaxY = maxY;
            dashAvailable = true;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
        }

        private void EndClimb()
        {
            climbing = false;
            body.gravityScale = baseGravityScale;
            body.linearVelocity = Vector2.zero;
        }

        private void UpdateClimb(bool hookPressed)
        {
            climbInput = ReadVertical();

            // No frame em que agarrou, o mesmo E não pode soltar.
            if (Time.frameCount == climbStartFrame)
            {
                return;
            }

            if (ReadJumpPressed())
            {
                EndClimb();
                body.linearVelocity = new Vector2(0f, jumpSpeed);
            }
            else if (ReadDashPressed() && dashAvailable)
            {
                EndClimb();
                StartDash();
            }
            else if (hookPressed || ReadInteractPressed())
            {
                EndClimb();
            }
        }

        // Set while a sign is being read: the game is paused and gameplay input is ignored.
        public bool InputLocked { get; set; }

        // Interactions that should show Marcus's interact animation (e.g. lighting the campfire)
        // call this when they start. Signs don't, so reading one doesn't trigger it.
        public void BeginInteractAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger(InteractHash);
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            baseGravityScale = body.gravityScale;
            animator = GetComponentInChildren<Animator>();
            visual = animator != null ? animator.GetComponent<SpriteRenderer>() : null;

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

            if (dead)
            {
                // Corpo que caiu num buraco fica parado onde passou do limite, à vista da câmera.
                if (!corpseFrozen && transform.position.y < fallLimit)
                {
                    FreezeCorpse();
                }

                return;
            }

            if (completed)
            {
                return;
            }

            if (InputLocked)
            {
                moveInput = 0f;
                previousHookHeld = ReadHookHeld();
                return;
            }

            moveInput = ReadHorizontal();
            if (Mathf.Abs(moveInput) > 0.1f)
            {
                facing = Mathf.Sign(moveInput);
            }

            bool hookHeld = ReadHookHeld();
            bool hookPressed = hookHeld && !previousHookHeld;
            bool hookReleased = !hookHeld && previousHookHeld;

            if (climbing)
            {
                UpdateClimb(hookPressed);
                previousHookHeld = hookHeld;
                UpdateAnimation();
                return;
            }

            if (pulling)
            {
                if (hookPressed)
                {
                    ReleasePull();
                }
            }
            else if (aiming && hookPressed)
            {
                CancelHook(true);
            }
            else
            {
                if (hookPressed && HasHook)
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

            if (hanging && ReadJumpPressed())
            {
                // Preso num ponto fixo: pular solta e já impulsiona para cima (a velocidade era zero).
                CancelHook(true);
                body.linearVelocity = new Vector2(0f, jumpSpeed);
            }
            else if (!pulling && !aiming && ReadJumpPressed() && IsGrounded())
            {
                Vector2 velocity = body.linearVelocity;
                velocity.y = jumpSpeed;
                body.linearVelocity = velocity;
            }

            if ((!pulling || hanging) && ReadDashPressed() && dashAvailable)
            {
                StartDash();
            }

            if (transform.position.y < fallLimit)
            {
                Die(CausaDaMorte.Queda);
                return;
            }

            previousHookHeld = hookHeld;
            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            if (dead)
            {
                if (corpseFrozen)
                {
                    body.linearVelocity = Vector2.zero;
                    return;
                }

                // Sem controle: o corpo só cai e para de deslizar no chão.
                Vector2 corpseVelocity = body.linearVelocity;
                corpseVelocity.x = Mathf.MoveTowards(corpseVelocity.x, 0f, groundAcceleration * Time.fixedDeltaTime);
                body.linearVelocity = corpseVelocity;
                return;
            }

            if (completed)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            if (climbing)
            {
                float y = Mathf.Clamp(body.position.y + climbInput * climbSpeed * Time.fixedDeltaTime, ropeMinY, ropeMaxY);
                body.position = new Vector2(ropeX, y);
                body.linearVelocity = Vector2.zero;
                return;
            }
            // Sample here so input, dash and hook cannot overwrite an independent force callback.
            float windSpeed = WindArea.Sample(bodyCollider);

            if (pulling)
            {
                PullTowardTarget(windSpeed);
                return;
            }

            if (dashTimer > 0f)
            {
                Vector2 dashVelocity = body.linearVelocity;
                dashVelocity.x = dashDirection * dashSpeed + windSpeed;
                body.linearVelocity = dashVelocity;
                dashTimer -= Time.fixedDeltaTime;
                if (dashTimer <= 0f)
                {
                    body.gravityScale = baseGravityScale;
                }

                return;
            }

            if (coasting)
            {
                Coast(windSpeed);
                return;
            }

            bool grounded = IsGrounded();
            if (grounded)
            {
                dashAvailable = true;
            }

            float acceleration = grounded ? groundAcceleration : airAcceleration;
            Vector2 velocity = body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, moveInput * runSpeed + windSpeed, acceleration * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
        }

        private void UpdateAnimation()
        {
            if (animator == null)
            {
                return;
            }

            if (visual != null && Mathf.Abs(moveInput) > 0.1f)
            {
                visual.flipX = moveInput < 0f;
            }

            animator.SetFloat(SpeedHash, Mathf.Abs(body.linearVelocity.x));
            animator.SetBool(GroundedHash, IsGrounded());
            animator.SetBool(HookModeHash, aiming || pulling);
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

                if (IsSolidSurface(hit.collider))
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
            currentTarget.NotifyHooked();
        }

        private void PullTowardTarget(float windSpeed)
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

            if (hanging)
            {
                // Fixed points hold the player in place until the hook is cancelled.
                body.position = targetPosition;
                body.linearVelocity = Vector2.zero;
                aimLine.SetPosition(0, targetPosition);
                aimLine.SetPosition(1, targetPosition);
                return;
            }

            Vector2 toTarget = targetPosition - body.position;
            float distance = toTarget.magnitude;

            aimLine.SetPosition(0, body.position);
            aimLine.SetPosition(1, targetPosition);

            if (distance <= targetReachDistance)
            {
                ReachTarget();
                return;
            }

            Vector2 desiredVelocity = toTarget.normalized * hookPullSpeed + Vector2.right * windSpeed;
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

            if (enemy)
            {
                pulling = false;
                currentTarget = null;
                aimLine.enabled = false;
                body.gravityScale = baseGravityScale;
                dashAvailable = true;
                float steering = Mathf.Abs(moveInput) > 0.1f ? moveInput : Mathf.Sign(incomingVelocity.x);
                body.linearVelocity = new Vector2(
                    incomingVelocity.x * 0.32f + steering * 3.75f,
                    enemyBounceSpeed + Mathf.Max(0f, incomingVelocity.y * 0.18f));
                reachedTarget.Capture();
            }
            else
            {
                hanging = true;
                pulling = true;
                dashAvailable = true; // agarrar o ponto devolve o dash, para poder sair dele com dash
                currentTarget = reachedTarget;
                body.gravityScale = 0f;
                body.linearVelocity = Vector2.zero;
                aimLine.enabled = true;
            }
        }

        private void ReleasePull()
        {
            bool wasHanging = hanging;
            Vector2 velocity = body.linearVelocity;
            Vector2 toTarget = currentTarget != null
                ? (Vector2)currentTarget.transform.position - body.position
                : Vector2.zero;
            CancelHook(true);

            if (wasHanging)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            // Cancelled while still accelerating: keep going forward with an impulse (momentum).
            Vector2 direction = velocity.sqrMagnitude > 0.01f ? velocity.normalized : toTarget.normalized;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            float speed = Mathf.Clamp(velocity.magnitude, momentumMinSpeed, momentumMaxSpeed);
            coasting = true;
            body.gravityScale = 0f;
            body.linearVelocity = direction * speed;
        }

        private void Coast(float windSpeed)
        {
            Vector2 drift = Vector2.right * windSpeed;
            Vector2 velocity = body.linearVelocity - drift;
            float speed = velocity.magnitude;
            if (speed <= momentumEndSpeed)
            {
                EndCoast();
                return;
            }

            speed = Mathf.Max(momentumEndSpeed, speed - momentumDeceleration * Time.fixedDeltaTime);
            body.linearVelocity = velocity.normalized * speed + drift;
        }

        private void EndCoast()
        {
            coasting = false;
            body.gravityScale = baseGravityScale;
        }

        private void CancelHook(bool preserveVelocity)
        {
            // Only a connected hook counts as a release; a target that was merely aimed at was never hooked.
            if (pulling && currentTarget != null)
            {
                currentTarget.NotifyReleased();
            }

            aiming = false;
            pulling = false;
            hanging = false;
            coasting = false;
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
            if (aiming || hanging)
            {
                CancelHook(true);
            }

            // Horizontal only; the current vertical velocity is kept for the whole dash.
            float direction = Mathf.Abs(moveInput) > 0.1f ? Mathf.Sign(moveInput) : facing;

            coasting = false;
            dashAvailable = false;
            dashTimer = dashDuration;
            dashDirection = direction;
            body.gravityScale = 0f;
            body.linearVelocity = new Vector2(direction * dashSpeed, body.linearVelocity.y);
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
                    IsSolidSurface(overlap))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSolidSurface(Collider2D collider)
        {
            return collider.GetComponent<PrototypePlatform>() != null ||
                   collider is TilemapCollider2D ||
                   collider is CompositeCollider2D;
        }

        public void CompletePrototype()
        {
            completed = true;
            climbing = false;
            CancelHook(true);
            body.linearVelocity = Vector2.zero;
            body.gravityScale = 0f;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CheckSpikeHit(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckSpikeHit(other);
            CheckEnemyTouch(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // Also on stay: letting go of the hook while already touching the enemy still kills.
            CheckEnemyTouch(other);
        }

        private void CheckEnemyTouch(Collider2D other)
        {
            if (completed || dead)
            {
                return;
            }

            // Reaching the enemy the hook is pulling toward is a capture, not a hit.
            HookTarget target = other.GetComponent<HookTarget>();
            if (target != null && target.IsEnemy && !(pulling && target == currentTarget))
            {
                Die(CausaDaMorte.Perigo);
            }
        }

        private void CheckSpikeHit(Collider2D other)
        {
            if (completed || dead)
            {
                return;
            }

            for (Transform t = other.transform; t != null; t = t.parent)
            {
                if (t.name.StartsWith("spike_group"))
                {
                    Die(CausaDaMorte.Perigo);
                    return;
                }
            }
        }

        // Morte: o Marcos para de responder, toca a animação (exceto congelamento, que ainda não tem)
        // e a tela de Game Over aparece por cima do jogo, que continua rodando. R recarrega a cena.
        public void Die(CausaDaMorte causa)
        {
            if (dead || completed || restarting)
            {
                return;
            }

            dead = true;
            climbing = false;
            dashTimer = 0f;
            moveInput = 0f;
            CancelHook(true);
            body.gravityScale = baseGravityScale;
            body.linearVelocity = new Vector2(0f, Mathf.Min(0f, body.linearVelocity.y));

            if (causa == CausaDaMorte.Queda)
            {
                FreezeCorpse();
            }

            if (animator != null)
            {
                animator.SetBool(DeadHash, true);
                animator.SetBool(HookModeHash, false);
                if (causa == CausaDaMorte.Congelamento)
                {
                    // Provisório até existir a animação de congelamento: o Marcos trava na pose atual.
                    animator.speed = 0f;
                }
                else
                {
                    animator.Play(DeathStateHash, 0, 0f);
                }
            }

            if (causa != CausaDaMorte.Congelamento)
            {
                LieDownCollider();
            }

            TelaGameOver.Mostrar();
        }

        // A cápsula em pé deixaria o corpo deitado flutuando (e preso em beiradas): vira uma cápsula
        // horizontal e baixa, com a mesma base, para o corpo assentar no chão sem dar pulo.
        private void LieDownCollider()
        {
            if (!(bodyCollider is CapsuleCollider2D capsule))
            {
                return;
            }

            Vector3 scale = transform.lossyScale;
            float scaleX = Mathf.Abs(scale.x) > 0.0001f ? Mathf.Abs(scale.x) : 1f;
            float scaleY = Mathf.Abs(scale.y) > 0.0001f ? Mathf.Abs(scale.y) : 1f;

            float bottom = capsule.offset.y - capsule.size.y * 0.5f;
            Vector2 size = new Vector2(corpseColliderSize.x / scaleX, corpseColliderSize.y / scaleY);

            capsule.direction = CapsuleDirection2D.Horizontal;
            capsule.size = size;
            capsule.offset = new Vector2(capsule.offset.x, bottom + size.y * 0.5f);
        }

        private void FreezeCorpse()
        {
            corpseFrozen = true;
            body.linearVelocity = Vector2.zero;
            body.gravityScale = 0f;
        }

        // Reinicia o loop: recarrega a cena (fogueiras voltam apagadas, Marcus volta pra base).
        // Usado pelo R (inclusive no Game Over) e pela fogueira final.
        public void RestartPrototype()
        {
            // Several hazards can fire in the same frame; the scene must only be reloaded once.
            if (restarting)
            {
                return;
            }

            restarting = true;
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

            // No Game Over só a tela de Game Over aparece.
            if (dead)
            {
                return;
            }

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

        private static float ReadVertical()
        {
#if ENABLE_INPUT_SYSTEM
            float value = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) value -= 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) value += 1f;
            }
            if (Gamepad.current != null && Mathf.Abs(Gamepad.current.leftStick.y.ReadValue()) > Mathf.Abs(value))
            {
                value = Gamepad.current.leftStick.y.ReadValue();
            }
            return Mathf.Clamp(value, -1f, 1f);
#else
            return Input.GetAxisRaw("Vertical");
#endif
        }

        private static bool ReadInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.E);
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
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
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
                   (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.LeftShift);
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
