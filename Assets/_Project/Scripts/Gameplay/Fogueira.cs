using System.Collections.Generic;
using HookPrototype;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vertigo.Gameplay
{
    // Fogueira acende com E quando o Marcos está no alcance (trigger) e parado no chão.
    // A sequência apagada -> acendendo -> acesa em loop fica toda no Animator.
    // Acesa, aquece quem estiver dentro do raio de calor (ver BarraDeFrio).
    // No modo final (fogueira do topo), sentar nela inicia o final: aurora boreal e tela de finalização (AuroraFinal).
    // Marcada como checkpoint, acendê-la guarda o lugar: morrendo (ou com R) o Marcos renasce ao lado dela, já acesa.
    [RequireComponent(typeof(Collider2D), typeof(Animator))]
    public sealed class Fogueira : MonoBehaviour
    {
        private static readonly int AcenderHash = Animator.StringToHash("Acender");
        private static readonly int EstadoAcesaHash = Animator.StringToHash("campfire");
        private static readonly List<Fogueira> ativas = new List<Fogueira>();

        [Header("Estado inicial")]
        [Tooltip("Marcado: a fogueira já começa acesa (aquece e não precisa ser acendida). Desmarcado: começa apagada.")]
        [SerializeField] private bool comecaAcesa;

        [Header("Calor")]
        [SerializeField] private float raioCalor = 3f;

        [Header("Fogueira final (topo)")]
        [Tooltip("Fim do jogo: ao sentar, a aurora boreal cobre a tela e aparece a tela de finalização.")]
        [SerializeField] private bool final;
        [Tooltip("Música do final: toca ao sentar, com fade in, enquanto a que estava tocando some com fade out.")]
        [SerializeField] private AudioClip musicaFinal;
        [Tooltip("Sons de brilho da aurora do final (um é sorteado a cada vez). Vazio: sininhos gerados por código.")]
        [SerializeField] private AudioClip[] sonsBrilhinhos;

        [Header("Checkpoint")]
        [Tooltip("Acesa, guarda o lugar: ao morrer ou apertar R o Marcos renasce aqui, com a fogueira já acesa.")]
        [SerializeField] private bool checkpoint;
        [Tooltip("Onde o Marcos renasce, relativo à fogueira (posição do pivô dele).")]
        [SerializeField] private Vector2 pontoRenascer = new Vector2(1.2f, 0.5f);

        [Header("Aviso \"E\" (opcional)")]
        [SerializeField] private CanvasGroup aviso;
        [SerializeField] private Vector2 deslocamentoAviso = new Vector2(0f, 1.25f);
        [SerializeField] private float duracaoFadeAviso = 0.25f;

        private Animator animator;
        private PlayerController jogador;
        private RectTransform avisoRect;
        private RectTransform avisoPai;
        private Canvas avisoCanvas;
        private bool acesa;
        private bool renasceAqui;

        public bool Acesa => acesa;

        // True se alguma fogueira acesa alcança essa posição com o calor.
        public static bool AlgumaAquece(Vector2 posicao)
        {
            for (int i = 0; i < ativas.Count; i++)
            {
                if (ativas[i].Aquece(posicao))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Aquece(Vector2 posicao)
        {
            return acesa && ((Vector2)transform.position - posicao).sqrMagnitude <= raioCalor * raioCalor;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            renasceAqui = checkpoint && Checkpoint.Eh(this);

            if (comecaAcesa || renasceAqui)
            {
                // Pula direto para o estado em loop, sem passar pela animação de acender.
                acesa = true;
                animator.Play(EstadoAcesaHash);
            }

            if (aviso != null)
            {
                aviso.alpha = 0f;
                avisoRect = (RectTransform)aviso.transform;
                avisoPai = (RectTransform)avisoRect.parent;
                avisoCanvas = aviso.GetComponentInParent<Canvas>();
            }
        }

        private void Start()
        {
            if (!renasceAqui)
            {
                return;
            }

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                return;
            }

            player.Teleportar((Vector2)transform.position + pontoRenascer);
            if (Checkpoint.ComGancho)
            {
                player.CollectHook();
            }

            VerticalCameraFollow seguidor = FindAnyObjectByType<VerticalCameraFollow>();
            if (seguidor != null)
            {
                seguidor.Encaixar();
            }
        }

        public AudioClip MusicaFinal => musicaFinal;
        public AudioClip[] SonsBrilhinhos => sonsBrilhinhos;

        private void OnEnable()
        {
            ativas.Add(this);
        }

        private void OnDisable()
        {
            ativas.Remove(this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                jogador = player;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (jogador != null && other.GetComponentInParent<PlayerController>() == jogador)
            {
                jogador = null;
            }
        }

        private void Update()
        {
            bool jogadorPronto = jogador != null && jogador.CanInteract && !jogador.InputLocked;
            // Com o final em andamento a fogueira não responde mais: a aurora cuida do resto.
            bool podeInteragir = jogadorPronto && (!acesa || final) && !AuroraFinal.Ativa;

            if (podeInteragir && ReadInteractPressed())
            {
                if (final)
                {
                    // Fim do jogo: o Marcos se senta (acende, se ainda estiver apagada), a aurora o envolve e cobre
                    // a tela, e aparece a tela de finalização. R recomeça do zero, sem checkpoint.
                    if (!acesa)
                    {
                        acesa = true;
                        animator.SetTrigger(AcenderHash);
                    }

                    jogador.BeginSitAnimation();
                    Checkpoint.Limpar();
                    AuroraFinal.Iniciar(jogador, musicaFinal, sonsBrilhinhos);
                }
                else if (!acesa)
                {
                    acesa = true;
                    animator.SetTrigger(AcenderHash);
                    jogador.BeginInteractAnimation();
                    if (checkpoint)
                    {
                        Checkpoint.Salvar(this, jogador.HasHook);
                    }
                }

                podeInteragir = false;
            }

            if (aviso != null)
            {
                float alvo = podeInteragir ? 1f : 0f;
                aviso.alpha = Mathf.MoveTowards(aviso.alpha, alvo, Time.unscaledDeltaTime / duracaoFadeAviso);
            }
        }

        private void LateUpdate()
        {
            if (aviso == null || aviso.alpha <= 0f)
            {
                return;
            }

            Camera cameraMundo = Camera.main;
            if (cameraMundo == null)
            {
                return;
            }

            // Segue a fogueira na tela, travado no grid de pixels do Canvas (640x360).
            Vector3 mundo = transform.position + (Vector3)deslocamentoAviso;
            Vector2 tela = cameraMundo.WorldToScreenPoint(mundo);
            Camera cameraUi = avisoCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : avisoCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(avisoPai, tela, cameraUi, out Vector2 local))
            {
                avisoRect.anchoredPosition = new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, raioCalor);

            if (checkpoint)
            {
                Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.9f);
                Gizmos.DrawWireCube((Vector2)transform.position + pontoRenascer, new Vector3(0.7f, 1f, 0f));
            }
        }

        private static bool ReadInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Vertigo.Gameplay.ControlesJogo.Interagir();
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }
    }
}
