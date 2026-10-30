using HookPrototype;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vertigo.Gameplay
{
    // Fogueira acende com E quando o Marcos está no alcance (trigger) e parado no chão.
    // A sequência apagada -> acendendo -> acesa em loop fica toda no Animator.
    [RequireComponent(typeof(Collider2D), typeof(Animator))]
    public sealed class Fogueira : MonoBehaviour
    {
        private static readonly int AcenderHash = Animator.StringToHash("Acender");

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

        private void Awake()
        {
            animator = GetComponent<Animator>();

            if (aviso != null)
            {
                aviso.alpha = 0f;
                avisoRect = (RectTransform)aviso.transform;
                avisoPai = (RectTransform)avisoRect.parent;
                avisoCanvas = aviso.GetComponentInParent<Canvas>();
            }
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
            bool podeAcender = !acesa && jogador != null && jogador.CanInteract && !jogador.InputLocked;

            if (podeAcender && ReadInteractPressed())
            {
                acesa = true;
                podeAcender = false;
                animator.SetTrigger(AcenderHash);
                jogador.BeginInteractAnimation();
            }

            if (aviso != null)
            {
                float alvo = podeAcender ? 1f : 0f;
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

        private static bool ReadInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }
    }
}
