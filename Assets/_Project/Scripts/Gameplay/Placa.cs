using HookPrototype;
using UnityEngine;
using Vertigo.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vertigo.Gameplay
{
    // Placa lida com E quando o Marcos está no alcance (trigger) e parado no chão.
    [RequireComponent(typeof(Collider2D))]
    public sealed class Placa : MonoBehaviour
    {
        [SerializeField, TextArea(4, 8)] private string texto;
        [SerializeField] private CaixaDeTexto caixa;

        [Header("Aviso \"E\" (opcional)")]
        [SerializeField] private CanvasGroup aviso;
        [SerializeField] private Vector2 deslocamentoAviso = new Vector2(0f, 1.25f);
        [SerializeField] private float duracaoFadeAviso = 0.25f;

        private PlayerController jogador;
        private RectTransform avisoRect;
        private RectTransform avisoPai;
        private Canvas avisoCanvas;

        private void Awake()
        {
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
            bool podeLer = jogador != null && jogador.CanInteract && !jogador.InputLocked && !caixa.Aberta;

            if (podeLer && caixa.FrameFechada != Time.frameCount && ReadInteractPressed())
            {
                PlayerController leitor = jogador;
                leitor.InputLocked = true;
                caixa.Abrir(texto, () => leitor.InputLocked = false);
                podeLer = false;
            }

            if (aviso != null)
            {
                float alvo = podeLer ? 1f : 0f;
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

            // Segue a placa na tela, travado no grid de pixels do Canvas (640x360).
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
