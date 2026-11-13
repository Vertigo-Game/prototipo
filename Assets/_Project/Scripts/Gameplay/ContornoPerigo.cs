using UnityEngine;

namespace Vertigo.Gameplay
{
    // Contorno fixo de 1 pixel em volta do sprite, para avisar que encostar mata (inimigos).
    // São 4 cópias do sprite em silhueta, deslocadas 1 pixel para cada lado, desenhadas atrás dele.
    // Acompanha a animação e o flip, e some enquanto o inimigo está capturado (carcaça).
    public sealed class ContornoPerigo : MonoBehaviour
    {
        private static readonly Vector2[] Direcoes = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        private static readonly int CorId = Shader.PropertyToID("_Color");

        [SerializeField] private Color cor = new Color(0.92f, 0.16f, 0.12f, 1f);
        [Tooltip("Material com o shader Vertigo/SilhuetaSprite (pinta o sprite inteiro com uma cor só).")]
        [SerializeField] private Material materialSilhueta;
        [SerializeField, Min(1)] private int espessuraPixels = 1;
        [Tooltip("Pixels por unidade da arte do jogo: o contorno tem a espessura de um pixel da tela, não do sprite.")]
        [SerializeField, Min(1f)] private float pixelsPorUnidade = 16f;

        private SpriteRenderer original;
        private SpriteRenderer[] copias;
        private RenascimentoInimigo renascimento;

        private void Awake()
        {
            original = GetComponentInChildren<SpriteRenderer>();
            renascimento = GetComponent<RenascimentoInimigo>();
            if (original == null || materialSilhueta == null)
            {
                enabled = false;
                return;
            }

            MaterialPropertyBlock bloco = new MaterialPropertyBlock();
            bloco.SetColor(CorId, cor);

            copias = new SpriteRenderer[Direcoes.Length];
            for (int i = 0; i < Direcoes.Length; i++)
            {
                GameObject copia = new GameObject("Contorno");
                copia.transform.SetParent(original.transform, false);
                SpriteRenderer sr = copia.AddComponent<SpriteRenderer>();
                sr.sharedMaterial = materialSilhueta;
                sr.sortingLayerID = original.sortingLayerID;
                sr.sortingOrder = original.sortingOrder - 1;
                sr.SetPropertyBlock(bloco);
                copias[i] = sr;
            }
        }

        private void LateUpdate()
        {
            Sprite sprite = original.sprite;
            bool visivel = sprite != null && original.enabled && original.gameObject.activeInHierarchy &&
                           (renascimento == null || !renascimento.Capturado);

            // Espessura em unidades do mundo, convertida para o espaço local (o sprite pode estar escalado).
            float passoMundo = espessuraPixels / pixelsPorUnidade;
            Vector3 escala = original.transform.lossyScale;
            Vector2 passo = new Vector2(
                passoMundo / Mathf.Max(Mathf.Abs(escala.x), 0.0001f),
                passoMundo / Mathf.Max(Mathf.Abs(escala.y), 0.0001f));
            for (int i = 0; i < copias.Length; i++)
            {
                SpriteRenderer sr = copias[i];
                sr.enabled = visivel;
                if (!visivel)
                {
                    continue;
                }

                sr.sprite = sprite;
                sr.flipX = original.flipX;
                sr.flipY = original.flipY;
                sr.transform.localPosition = (Vector3)Vector2.Scale(Direcoes[i], passo);
            }
        }

        private void OnDisable()
        {
            if (copias == null)
            {
                return;
            }

            foreach (SpriteRenderer sr in copias)
            {
                if (sr != null)
                {
                    sr.enabled = false;
                }
            }
        }
    }
}
