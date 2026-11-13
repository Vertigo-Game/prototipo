using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Vertigo.Gameplay;
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
        [SerializeField] private float fallLimit = -5f;

        [Header("Jump")]
        [Tooltip("Altura máxima do pulo segurando o espaço, em tiles. A velocidade é calculada a partir da gravidade.")]
        [SerializeField] private float jumpHeight = 2.4f;
        [Tooltip("Soltar o espaço durante a subida multiplica a velocidade vertical por este valor (pulo curto).")]
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
        [Tooltip("Coyote time: segundos depois de sair da beirada em que ainda dá para pular.")]
        [SerializeField] private float coyoteTime = 0.1f;
        [Tooltip("Jump buffer: apertar o pulo até estes segundos antes de tocar o chão ainda pula. 0 desliga.")]
        [SerializeField] private float jumpBufferTime = 0.1f;
        [Tooltip("Multiplicador da gravidade enquanto cai (sobe leve, desce firme).")]
        [SerializeField] private float fallGravityMultiplier = 1.6f;
        [SerializeField] private float maxFallSpeed = 20f;

        [Header("Collision Assists")]
        [Tooltip("Degrau mais alto (em tiles) que o Marcus desce andando colado no chão (e sobe, se Auto Step Up). 0 desliga.")]
        [SerializeField] private float stepHeight = 1.05f;
        [Tooltip("Subir degraus sozinho andando contra eles. Desligado: degrau sempre pede pulo.")]
        [SerializeField] private bool autoStepUp = false;
        [Tooltip("Duração da subida automática de um degrau (só com Auto Step Up).")]
        [SerializeField] private float stepUpDuration = 0.08f;
        [Tooltip("Correção de quina: quanto (em tiles) o pé ou a cabeça pode raspar numa quina e ainda passar. 0 desliga.")]
        [SerializeField] private float cornerCorrection = 0.2f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 17f;
        [SerializeField] private float dashDuration = 0.18f;

        [Header("Rope")]
        [SerializeField] private float climbSpeed = 5.5f;
        [Tooltip("Quão rápido chega na velocidade de subida/descida (e para quando solta).")]
        [SerializeField] private float climbAcceleration = 45f;
        [Tooltip("Velocidade com que o Marcus desliza até o centro da corda ao agarrar.")]
        [SerializeField] private float ropeSnapSpeed = 8f;
        [Tooltip("Impulso para cima ao chegar no topo da corda segurando cima, em tiles.")]
        [SerializeField] private float ropeTopHopHeight = 1.3f;
        [Tooltip("Tempo depois de soltar a corda antes de poder agarrar de novo.")]
        [SerializeField] private float ropeRegrabDelay = 0.25f;

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
        private SonsGancho sons;
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
        private float climbVelocity;
        private float ropeReleaseTime = -100f;
        private const float Never = float.MaxValue;
        private float timeSinceGrounded = Never;
        private float timeSinceJumpPressed = Never;
        private bool jumpCutArmed;
        private readonly List<RaycastHit2D> castHits = new List<RaycastHit2D>();
        private readonly List<Collider2D> overlapHits = new List<Collider2D>();
        private ContactFilter2D solidFilter;
        private ContactFilter2D groundContactFilter;
        private readonly List<ContactPoint2D> groundContacts = new List<ContactPoint2D>();
        private float stepRemaining;
        private float stepSpeed;
        private bool settleNextStep;
        private bool wasGroundedLastStep;
        private float ungroundedTime;
        // Tempo no ar contando todos os estados (dash, gancho, corda), para a animação.
        private float airTime;
        // Caindo (sem ter pulado), só mostra a pose de pulo depois disso no ar: degrau e desnível pequeno não piscam.
        private const float AirborneAnimationDelay = 0.1f;
        private const float SnapDownWindow = 0.15f;

        // Velocidade inicial que faz o pulo subir exatamente jumpHeight com a gravidade de subida.
        private float JumpSpeed => Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * baseGravityScale * jumpHeight);

        private bool dead;
        private bool corpseFrozen;

        public bool IsDead => dead;
        public bool CanGrabRope => !dead && !climbing && !aiming && !pulling && !coasting && !completed && dashTimer <= 0f &&
                                   Time.time - ropeReleaseTime >= ropeRegrabDelay;
        public Bounds BodyBounds => bodyCollider.bounds;
        public bool HasHook => startsWithHook || hookCollected;

        // Chamado pelo item do gancho. Vale só até reiniciar a fase (a cena é recarregada).
        public void CollectHook()
        {
            hookCollected = true;
        }

        // Coloca o Marcos parado num ponto (ex.: renascendo no checkpoint). Chamar antes da física começar a rodar.
        public void Teleportar(Vector2 posicao)
        {
            transform.position = new Vector3(posicao.x, posicao.y, transform.position.z);
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            body.position = posicao;
            body.linearVelocity = Vector2.zero;
        }

        private bool restarting;

        public bool DashAvailable => dashAvailable;
        public bool IsPulling => pulling;
        public bool CanInteract => !dead && !aiming && !pulling && !coasting && !completed && dashTimer <= 0f && IsGrounded();

        // Corda: o Marcos fica preso na coluna x e sobe/desce entre minY e maxY.
        // Agarra com E ou segurando cima. Sai com pulo (para o lado que estiver segurando), dash, E,
        // clique, descendo além da ponta de baixo ou subindo além do topo (ganha um impulso).
        public void BeginClimb(float x, float minY, float maxY)
        {
            climbing = true;
            climbStartFrame = Time.frameCount;
            climbInput = 0f;
            // Agarrar caindo amortece um pouco a queda em vez de parar seco.
            climbVelocity = Mathf.Clamp(body.linearVelocity.y * 0.25f, -climbSpeed, climbSpeed);
            ropeX = x;
            ropeMinY = minY;
            ropeMaxY = maxY;
            dashAvailable = true;
            jumpCutArmed = false;
            coasting = false;
            CancelStep();
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
        }

        private void EndClimb()
        {
            climbing = false;
            climbVelocity = 0f;
            ropeReleaseTime = Time.time;
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
                // Pula para o lado que estiver segurando; sem direção, sobe reto.
                body.linearVelocity = new Vector2(moveInput * runSpeed, 0f);
                Jump();
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
            solidFilter = new ContactFilter2D { useTriggers = false };
            groundContactFilter = new ContactFilter2D { useTriggers = false };
            // Normal entre 30° e 150° = superfície por baixo (inclui quina arredondada); parede fica de fora.
            groundContactFilter.SetNormalAngle(30f, 150f);
            animator = GetComponentInChildren<Animator>();
            visual = animator != null ? animator.GetComponent<SpriteRenderer>() : null;
            sons = GetComponent<SonsGancho>();

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

            bool jumpPressed = ReadJumpPressed();
            UpdateJumpTimers(jumpPressed);

            if (hanging && jumpPressed)
            {
                // Preso num ponto fixo: pular solta e já impulsiona para cima (a velocidade era zero).
                CancelHook(true);
                Jump();
            }
            else if (!pulling && !aiming &&
                     timeSinceJumpPressed <= jumpBufferTime && timeSinceGrounded <= coyoteTime)
            {
                Jump();
            }

            ApplyJumpCut();

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

            airTime = IsGrounded() ? 0f : airTime + Time.fixedDeltaTime;

            if (climbing)
            {
                wasGroundedLastStep = false;
                UpdateClimbPhysics();
                return;
            }
            // Sample here so input, dash and hook cannot overwrite an independent force callback.
            float windSpeed = WindArea.Sample(bodyCollider);

            if (pulling)
            {
                wasGroundedLastStep = false;
                PullTowardTarget(windSpeed);
                return;
            }

            if (dashTimer > 0f)
            {
                wasGroundedLastStep = false;
                Vector2 dashVelocity = body.linearVelocity;
                dashVelocity.x = dashDirection * dashSpeed + windSpeed;
                dashTimer -= Time.fixedDeltaTime;
                if (dashTimer <= 0f)
                {
                    body.gravityScale = baseGravityScale;
                    // Sai do dash na velocidade de corrida (na direção do input, ou na do dash
                    // sem input) e o controle normal desacelera dali: sem o deslize longo de antes,
                    // mas também sem parar seco.
                    float exitDirection = Mathf.Abs(moveInput) > 0.1f ? moveInput : dashDirection;
                    dashVelocity.x = exitDirection * runSpeed + windSpeed;
                }

                body.linearVelocity = dashVelocity;
                return;
            }

            if (coasting)
            {
                wasGroundedLastStep = false;
                Coast(windSpeed);
                return;
            }

            if (stepRemaining > 0f)
            {
                ContinueStepUp(moveInput * runSpeed + windSpeed);
                return;
            }

            bool grounded = IsGrounded();
            if (grounded)
            {
                dashAvailable = true;
                ungroundedTime = 0f;
            }
            else
            {
                ungroundedTime += Time.fixedDeltaTime;
            }

            float acceleration = grounded ? groundAcceleration : airAcceleration;
            Vector2 velocity = body.linearVelocity;
            if (settleNextStep)
            {
                // Depois de subir/descer um degrau, zera o vertical que sobrou do movimento.
                settleNextStep = false;
                if (grounded)
                {
                    velocity.y = 0f;
                }
            }

            // Mirando no chão o Marcus fica parado; no ar continua dando para controlar.
            float horizontalInput = aiming && grounded ? 0f : moveInput;
            velocity.x = Mathf.MoveTowards(velocity.x, horizontalInput * runSpeed + windSpeed, acceleration * Time.fixedDeltaTime);
            float moveDirection = Mathf.Abs(horizontalInput) > 0.1f ? Mathf.Sign(horizontalInput) : 0f;

            // Andando contra um degrau baixo: sobe sozinho em vez de travar.
            if (grounded && moveDirection != 0f && velocity.y <= 0.01f && TryBeginStepUp(moveDirection))
            {
                ContinueStepUp(velocity.x);
                return;
            }

            if (!grounded)
            {
                ApplyCornerCorrection(ref velocity, moveDirection);
            }

            // Gravidade maior só na descida, com limite de velocidade de queda.
            body.gravityScale = velocity.y < 0f ? baseGravityScale * fallGravityMultiplier : baseGravityScale;
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);

            // Andando para fora de um degrau baixo: gruda no degrau de baixo em vez de cair.
            // O sensor de chão é mais estreito que o corpo, então a quina do corpo ainda pode estar apoiada
            // por alguns frames depois de "sair do chão": tenta durante uma janela curta, enquanto não pulou.
            if (!grounded && wasGroundedLastStep && ungroundedTime <= SnapDownWindow &&
                moveDirection != 0f && velocity.y <= 0.01f && TrySnapDown(out float drop))
            {
                velocity.y = -drop / Time.fixedDeltaTime;
                settleNextStep = true;
                grounded = true;
                airTime = 0f;
            }

            body.linearVelocity = velocity;
            // Continua elegível para grudar enquanto estiver dentro da janela e sem subir.
            wasGroundedLastStep = grounded || (wasGroundedLastStep && ungroundedTime <= SnapDownWindow && velocity.y <= 0.01f);
        }

        // ---------- Degraus e quinas ----------

        private Vector2 BodyEdge(Vector2 direction)
        {
            return bodyCollider.ClosestPoint((Vector2)bodyCollider.bounds.center + direction * 10f);
        }

        private bool AreaFree(Vector2 center, Vector2 size)
        {
            overlapHits.Clear();
            Physics2D.OverlapBox(center, size, 0f, solidFilter, overlapHits);
            foreach (Collider2D hit in overlapHits)
            {
                if (hit != bodyCollider)
                {
                    return false;
                }
            }

            return true;
        }

        private bool HitsFace(Vector2 direction, float distance, float minDot)
        {
            castHits.Clear();
            bodyCollider.Cast(direction, solidFilter, castHits, distance);
            foreach (RaycastHit2D hit in castHits)
            {
                if (Vector2.Dot(hit.normal, -direction) > minDot)
                {
                    return true;
                }
            }

            return false;
        }

        // Topo do chão sólido logo abaixo de (x, fromY), ou NaN se não houver ou se o ponto já estiver dentro dele.
        private float SurfaceTopBelow(float x, float fromY, float distance)
        {
            castHits.Clear();
            Physics2D.Raycast(new Vector2(x, fromY), Vector2.down, solidFilter, castHits, distance);
            foreach (RaycastHit2D hit in castHits)
            {
                if (hit.collider == bodyCollider)
                {
                    continue;
                }

                return hit.distance <= 0.0001f ? float.NaN : hit.point.y;
            }

            return float.NaN;
        }

        private bool TryBeginStepUp(float direction)
        {
            if (!autoStepUp || stepHeight <= 0f || !HitsFace(new Vector2(direction, 0f), 0.08f, 0.7f))
            {
                return false;
            }

            Vector2 center = bodyCollider.bounds.center;
            float feet = BodyEdge(Vector2.down).y;
            float head = BodyEdge(Vector2.up).y;
            float halfWidth = Mathf.Abs(BodyEdge(new Vector2(direction, 0f)).x - center.x);

            float top = SurfaceTopBelow(center.x + direction * (halfWidth + 0.06f), feet + stepHeight + 0.02f, stepHeight + 0.04f);
            float rise = top - feet;
            if (float.IsNaN(top) || rise <= 0.01f || rise > stepHeight)
            {
                return false;
            }

            // Precisa de espaço para subir no lugar e para ficar em cima do degrau.
            Vector2 size = new Vector2(halfWidth * 2f - 0.02f, head - feet - 0.02f);
            if (!AreaFree(new Vector2(center.x, center.y + rise + 0.02f), size) ||
                !AreaFree(new Vector2(center.x + direction * 0.1f, center.y + rise + 0.02f), size))
            {
                return false;
            }

            stepRemaining = rise + 0.02f;
            stepSpeed = stepRemaining / Mathf.Max(0.01f, stepUpDuration);
            body.gravityScale = 0f;
            return true;
        }

        private void ContinueStepUp(float horizontalVelocity)
        {
            float dt = Time.fixedDeltaTime;
            float rise = Mathf.Min(stepRemaining, stepSpeed * dt);
            stepRemaining -= rise;
            body.linearVelocity = new Vector2(horizontalVelocity, rise / dt);
            wasGroundedLastStep = true;

            if (stepRemaining <= 0.0001f)
            {
                CancelStep();
                settleNextStep = true;
            }
        }

        private void CancelStep()
        {
            if (stepRemaining > 0f)
            {
                body.gravityScale = baseGravityScale;
            }

            stepRemaining = 0f;
        }

        private bool TrySnapDown(out float drop)
        {
            drop = 0f;
            if (stepHeight <= 0f)
            {
                return false;
            }

            castHits.Clear();
            bodyCollider.Cast(Vector2.down, solidFilter, castHits, stepHeight + 0.02f);
            float best = float.MaxValue;
            foreach (RaycastHit2D hit in castHits)
            {
                if (hit.distance < 0.02f)
                {
                    // Ainda apoiado na quina do degrau de cima: tenta de novo no próximo frame.
                    return false;
                }

                if (hit.normal.y > 0.7f)
                {
                    best = Mathf.Min(best, hit.distance);
                }
            }

            drop = best - 0.005f;
            return best < float.MaxValue && drop > 0.02f;
        }

        // No ar: se o pé raspa na quina de uma borda, sobe por cima; se a cabeça raspa na quina de um teto, desvia de lado.
        private void ApplyCornerCorrection(ref Vector2 velocity, float moveDirection)
        {
            if (cornerCorrection <= 0f)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            Vector2 center = bodyCollider.bounds.center;
            float feet = BodyEdge(Vector2.down).y;
            float head = BodyEdge(Vector2.up).y;
            float halfWidth = (BodyEdge(Vector2.right).x - BodyEdge(Vector2.left).x) * 0.5f;
            Vector2 size = new Vector2(halfWidth * 2f - 0.02f, head - feet - 0.02f);

            float direction = moveDirection != 0f ? moveDirection : Mathf.Abs(velocity.x) > 0.5f ? Mathf.Sign(velocity.x) : 0f;
            if (direction != 0f && HitsFace(new Vector2(direction, 0f), Mathf.Max(0.05f, Mathf.Abs(velocity.x) * dt + 0.02f), 0.7f))
            {
                float top = SurfaceTopBelow(center.x + direction * (halfWidth + 0.06f), feet + cornerCorrection + 0.02f, cornerCorrection + 0.04f);
                float lift = top - feet + 0.02f;
                if (!float.IsNaN(top) && lift > 0f && lift <= cornerCorrection + 0.02f &&
                    AreaFree(new Vector2(center.x + direction * 0.08f, center.y + lift), size))
                {
                    body.position += new Vector2(0f, lift);
                    velocity.y = Mathf.Max(velocity.y, 0f);
                    // Empurra para cima da borda já neste passo; chegando contra a parede quase parado,
                    // ele andaria tão pouco que a gravidade puxaria de volta e ele ficaria pairando na quina.
                    velocity.x = direction * Mathf.Max(Mathf.Abs(velocity.x), runSpeed * 0.5f);
                    return;
                }
            }

            if (velocity.y > 0f)
            {
                float up = velocity.y * dt + 0.02f;
                if (!HitsFace(Vector2.up, up, 0.7f))
                {
                    return;
                }

                float first = direction != 0f ? direction : 1f;
                for (float shift = 0.02f; shift <= cornerCorrection + 0.001f; shift += 0.02f)
                {
                    if (AreaFree(new Vector2(center.x + first * shift, center.y + up), size))
                    {
                        body.position += new Vector2(first * shift, 0f);
                        return;
                    }

                    if (AreaFree(new Vector2(center.x - first * shift, center.y + up), size))
                    {
                        body.position += new Vector2(-first * shift, 0f);
                        return;
                    }
                }
            }
        }

        private void UpdateClimbPhysics()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 position = body.position;

            climbVelocity = Mathf.MoveTowards(climbVelocity, climbInput * climbSpeed, climbAcceleration * dt);
            float targetY = position.y + climbVelocity * dt;

            if (targetY >= ropeMaxY && climbInput > 0.5f)
            {
                // Passou do topo segurando cima: sai da corda com um impulso para subir na borda.
                EndClimb();
                float hopSpeed = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * baseGravityScale * ropeTopHopHeight);
                body.linearVelocity = new Vector2(moveInput * runSpeed, hopSpeed);
                return;
            }

            if (climbInput < -0.5f &&
                ((targetY <= ropeMinY && position.y <= ropeMinY + 0.01f) || IsGrounded()))
            {
                // Na ponta de baixo (ou já pisando no chão) segurando baixo: solta.
                EndClimb();
                return;
            }

            targetY = Mathf.Clamp(targetY, ropeMinY, ropeMaxY);
            if (Mathf.Approximately(targetY, ropeMinY) || Mathf.Approximately(targetY, ropeMaxY))
            {
                climbVelocity = 0f;
            }

            // Move por velocidade (não teleporte) para a interpolação continuar suave;
            // a correção até a coluna da corda e para dentro do trecho é limitada pelo ropeSnapSpeed.
            float maxStep = Mathf.Max(ropeSnapSpeed, climbSpeed) * dt;
            float x = Mathf.MoveTowards(position.x, ropeX, ropeSnapSpeed * dt);
            float y = Mathf.MoveTowards(position.y, targetY, maxStep);
            body.linearVelocity = (new Vector2(x, y) - position) / dt;
        }

        private void UpdateJumpTimers(bool jumpPressed)
        {
            // Só conta como chão parado ou descendo: logo depois do pulo o check ainda encosta no chão.
            if (IsGrounded() && body.linearVelocity.y <= 0.01f)
            {
                timeSinceGrounded = 0f;
            }
            else if (timeSinceGrounded < Never)
            {
                timeSinceGrounded += Time.deltaTime;
            }

            if (jumpPressed)
            {
                timeSinceJumpPressed = 0f;
            }
            else if (timeSinceJumpPressed < Never)
            {
                timeSinceJumpPressed += Time.deltaTime;
            }
        }

        private void Jump()
        {
            CancelStep();
            settleNextStep = false;
            wasGroundedLastStep = false;
            Vector2 velocity = body.linearVelocity;
            velocity.y = JumpSpeed;
            body.linearVelocity = velocity;
            body.gravityScale = baseGravityScale;
            jumpCutArmed = true;
            // Consome o buffer e o coyote para o mesmo aperto não gerar um segundo pulo.
            timeSinceGrounded = Never;
            timeSinceJumpPressed = Never;
        }

        private void ApplyJumpCut()
        {
            if (!jumpCutArmed)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            if (velocity.y <= 0f || pulling || coasting || dashTimer > 0f)
            {
                jumpCutArmed = false;
                return;
            }

            if (!ReadJumpHeld())
            {
                velocity.y *= jumpCutMultiplier;
                body.linearVelocity = velocity;
                jumpCutArmed = false;
            }
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
            // Pose de pulo só quando está mesmo no ar: subindo (pulo, quique) aparece na hora; caindo de uma
            // borda, só depois de AirborneAnimationDelay. Assim descer degrau (1-2 frames no ar antes de grudar)
            // não pisca correndo -> pulo -> parado -> correndo.
            bool rising = body.linearVelocity.y > 0.5f && stepRemaining <= 0f;
            bool airborne = !IsGrounded() && (rising || airTime > AirborneAnimationDelay);
            animator.SetBool(GroundedHash, !airborne);
            animator.SetBool(HookModeHash, aiming || pulling);
        }

        private void BeginAiming()
        {
            aiming = true;
            currentTarget = null;
            aimLine.enabled = true;
            SetTimeScale(aimingTimeScale);
            if (sons != null)
            {
                sons.IniciarCameraLenta();
            }

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
            if (sons != null)
            {
                sons.PararCameraLenta();
            }

            if (currentTarget == null)
            {
                aimLine.enabled = false;
                return;
            }

            pulling = true;
            jumpCutArmed = false;
            CancelStep();
            body.gravityScale = 0f;
            dashTimer = 0f;
            aimLine.enabled = true;
            currentTarget.NotifyHooked();
            if (sons != null)
            {
                sons.TocarConexao();
            }
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
                if (sons != null)
                {
                    sons.TocarSoltura();
                }

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
            bool connected = pulling && currentTarget != null;
            if (connected)
            {
                currentTarget.NotifyReleased();
            }

            if (sons != null)
            {
                sons.PararCameraLenta();
                // Saindo do gancho (soltar, pular ou dar dash do ponto); morrer ou terminar a fase não conta.
                if (connected && !dead && !completed)
                {
                    sons.TocarSoltura();
                }
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

            // Linha reta horizontal: zera o vertical para o dash não render altura no meio do pulo.
            float direction = Mathf.Abs(moveInput) > 0.1f ? Mathf.Sign(moveInput) : facing;

            coasting = false;
            jumpCutArmed = false;
            CancelStep();
            dashAvailable = false;
            dashTimer = dashDuration;
            dashDirection = direction;
            body.gravityScale = 0f;
            body.linearVelocity = new Vector2(direction * dashSpeed, 0f);
        }

        private bool IsGrounded()
        {
            // Contatos reais da física com normal apontando para cima: vale em qualquer ponto da base do corpo,
            // inclusive quando só a quina do corpo está apoiada numa borda. Parede (normal lateral) e teto não contam.
            groundContacts.Clear();
            body.GetContacts(groundContactFilter, groundContacts);
            foreach (ContactPoint2D contact in groundContacts)
            {
                if (IsSolidSurface(contact.collider))
                {
                    return true;
                }
            }

            // Reserva: logo depois de um teleporte/reinício ainda não há contato; olha embaixo dos pés.
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
                if (dead)
                {
                    target.NotifyKilledPlayer();
                }
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

        // O collider em pé deixaria o corpo deitado flutuando (e preso em beiradas): vira um collider
        // largo e baixo, com a mesma base, para o corpo assentar no chão sem dar pulo.
        private void LieDownCollider()
        {
            Vector3 scale = transform.lossyScale;
            float scaleX = Mathf.Abs(scale.x) > 0.0001f ? Mathf.Abs(scale.x) : 1f;
            float scaleY = Mathf.Abs(scale.y) > 0.0001f ? Mathf.Abs(scale.y) : 1f;
            Vector2 size = new Vector2(corpseColliderSize.x / scaleX, corpseColliderSize.y / scaleY);

            if (bodyCollider is BoxCollider2D box)
            {
                float bottom = box.offset.y - box.size.y * 0.5f;
                box.size = size;
                box.offset = new Vector2(box.offset.x, bottom + size.y * 0.5f);
            }
            else if (bodyCollider is CapsuleCollider2D capsule)
            {
                float bottom = capsule.offset.y - capsule.size.y * 0.5f;
                capsule.direction = CapsuleDirection2D.Horizontal;
                capsule.size = size;
                capsule.offset = new Vector2(capsule.offset.x, bottom + size.y * 0.5f);
            }
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

        private static bool ReadJumpHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#else
            return Input.GetKey(KeyCode.Space);
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
