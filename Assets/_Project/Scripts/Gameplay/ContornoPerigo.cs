using UnityEngine;

namespace Vertigo.Gameplay
{
    // Quatro cópias em silhueta, deslocadas para cada lado e desenhadas atrás do sprite.
    // Acompanha a animação e o flip, e some enquanto o inimigo está capturado (carcaça).
    public sealed class ContornoPerigo : MonoBehaviour
    {
        private static readonly Vector2[] Direcoes = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        private static readonly int CorId = Shader.PropertyToID("_Color");

        [Tooltip("Cor do contorno fixo. No modo arco-íris, somente a transparência desta cor é usada.")]
        [SerializeField] private Color cor = new Color(0.92f, 0.16f, 0.12f, 1f);
        [SerializeField, Range(0f, 1f), Tooltip("Opacidade do contorno quando o inimigo está sem perfil visual.")]
        private float opacidade = 1f;
        [Tooltip("Material com o shader Vertigo/SilhuetaSprite (pinta o sprite inteiro com uma cor só).")]
        [SerializeField] private Material materialSilhueta;
        [SerializeField, Min(1)] private int espessuraPixels = 1;
        [Tooltip("Pixels por unidade da arte do jogo: o contorno tem a espessura de um pixel da tela, não do sprite.")]
        [SerializeField, Min(1f)] private float pixelsPorUnidade = 16f;
        [SerializeField] private bool arcoIris;
        [SerializeField, Min(0f), Tooltip("Ciclos de cor por segundo. Zero deixa as cores do arco-íris paradas.")]
        private float velocidadeArcoIris = 0.8f;

        private SpriteRenderer original;
        private SpriteRenderer[] copias;
        private RenascimentoInimigo renascimento;
        private CorDoCristal cristal;
        private MaterialPropertyBlock bloco;
        private Color corAplicada;
        private bool arcoIrisAplicado;
        private float faseArcoIris;
        private PerfilVisualInimigo Perfil => cristal != null && cristal.isActiveAndEnabled ? cristal.Perfil : null;

        private void Awake()
        {
            original = GetComponentInChildren<SpriteRenderer>();
            renascimento = GetComponent<RenascimentoInimigo>();
            cristal = GetComponent<CorDoCristal>();
            if (original == null || materialSilhueta == null)
            {
                enabled = false;
                return;
            }

            bloco = new MaterialPropertyBlock();
            corAplicada = CorComOpacidade(Perfil);
            bloco.SetColor(CorId, corAplicada);

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
            PerfilVisualInimigo perfil = Perfil;
            Color corBase = CorComOpacidade(perfil);
            bool arcoIrisManual = perfil == null && arcoIris;
            bool atualizarCor = arcoIrisManual || arcoIrisAplicado || corBase != corAplicada;
            if (arcoIrisManual)
            {
                faseArcoIris = Mathf.Repeat(faseArcoIris + Time.deltaTime * velocidadeArcoIris, 1f);
            }

            corAplicada = corBase;
            arcoIrisAplicado = arcoIrisManual;

            Sprite sprite = original.sprite;
            bool visivel = corBase.a > 0f && sprite != null && original.enabled && original.gameObject.activeInHierarchy &&
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
                if (atualizarCor)
                {
                    Color corContorno = corBase;
                    if (arcoIrisManual)
                    {
                        float matiz = Mathf.Repeat(faseArcoIris + (float)i / copias.Length, 1f);
                        corContorno = Color.HSVToRGB(matiz, 0.9f, 1f);
                        corContorno.a = corBase.a;
                    }

                    bloco.SetColor(CorId, corContorno);
                    sr.SetPropertyBlock(bloco);
                }

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

        private Color CorComOpacidade(PerfilVisualInimigo perfil)
        {
            if (perfil == null)
            {
                return new Color(cor.r, cor.g, cor.b, cor.a * opacidade);
            }

            Color corPerfil = perfil.CorNoTempo(Time.time);
            corPerfil.a = perfil.OpacidadeContorno;
            return corPerfil;
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
