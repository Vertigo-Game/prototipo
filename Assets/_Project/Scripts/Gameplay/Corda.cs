using HookPrototype;
using UnityEngine;
using UnityEngine.Tilemaps;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vertigo.Gameplay
{
    // Corda pintada em um Tilemap: com E, o Marcos agarra a corda onde estiver tocando.
    // Aí sobe com cima (W/seta) e desce com baixo (S/seta) até as pontas da corda.
    // Sai com pulo, dash, E ou clique (ver PlayerController.BeginClimb).
    [RequireComponent(typeof(Tilemap))]
    public sealed class Corda : MonoBehaviour
    {
        [Header("Aviso \"E\" (opcional)")]
        [SerializeField] private CanvasGroup aviso;
        [SerializeField] private Vector2 deslocamentoAviso = new Vector2(0f, 1.25f);
        [SerializeField] private float duracaoFadeAviso = 0.25f;

        private Tilemap tilemap;
        private PlayerController jogador;
        private RectTransform avisoRect;
        private RectTransform avisoPai;
        private Canvas avisoCanvas;
        private Vector3 avisoMundo;

        private void Awake()
        {
            tilemap = GetComponent<Tilemap>();

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
            jogador = FindFirstObjectByType<PlayerController>();
        }

        private void Update()
        {
            float x = 0f;
            float minY = 0f;
            float maxY = 0f;
            bool podeAgarrar = jogador != null && jogador.CanGrabRope && !jogador.InputLocked &&
                               TentarAchar(out x, out minY, out maxY);

            if (podeAgarrar)
            {
                avisoMundo = new Vector3(x, jogador.transform.position.y, 0f);

                if (ReadInteractPressed())
                {
                    jogador.BeginClimb(x, minY, maxY);
                    podeAgarrar = false;
                }
            }

            if (aviso != null)
            {
                float alvo = podeAgarrar ? 1f : 0f;
                aviso.alpha = Mathf.MoveTowards(aviso.alpha, alvo, Time.unscaledDeltaTime / duracaoFadeAviso);
            }
        }

        // Procura um tile de corda tocando o corpo do jogador e devolve a coluna e o trecho contínuo dessa corda.
        // minY/maxY são a base e o topo do trecho, na altura dos pés do Marcos.
        private bool TentarAchar(out float x, out float minY, out float maxY)
        {
            x = minY = maxY = 0f;
            Bounds corpo = jogador.BodyBounds;
            Vector3Int minCell = tilemap.WorldToCell(corpo.min);
            Vector3Int maxCell = tilemap.WorldToCell(corpo.max);

            for (int cx = minCell.x; cx <= maxCell.x; cx++)
            {
                for (int cy = minCell.y; cy <= maxCell.y; cy++)
                {
                    if (!tilemap.HasTile(new Vector3Int(cx, cy, 0)))
                    {
                        continue;
                    }

                    int baixo = cy;
                    while (tilemap.HasTile(new Vector3Int(cx, baixo - 1, 0)))
                    {
                        baixo--;
                    }

                    int cima = cy;
                    while (tilemap.HasTile(new Vector3Int(cx, cima + 1, 0)))
                    {
                        cima++;
                    }

                    x = tilemap.GetCellCenterWorld(new Vector3Int(cx, cy, 0)).x;
                    minY = tilemap.CellToWorld(new Vector3Int(cx, baixo, 0)).y;
                    maxY = tilemap.CellToWorld(new Vector3Int(cx, cima + 1, 0)).y;
                    return true;
                }
            }

            return false;
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

            // Segue o jogador junto da corda, travado no grid de pixels do Canvas (640x360).
            Vector3 mundo = avisoMundo + (Vector3)deslocamentoAviso;
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
