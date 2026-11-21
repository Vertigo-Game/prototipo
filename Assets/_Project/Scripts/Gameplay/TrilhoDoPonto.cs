using UnityEngine;

namespace Vertigo.Gameplay
{
    // Desenha (se estiver ligado) o trilho por onde o InimigoMovel anda: uma linha cinza de 3 pixels com o pixel do meio
    // alternando para um tom escuro. Cobre a rota inteira, de ponta a ponta, e fica parado na cena
    // (não acompanha o ponto). Os pixels têm o mesmo tamanho dos da arte do jogo.
    [RequireComponent(typeof(InimigoMovel))]
    public sealed class TrilhoDoPonto : MonoBehaviour
    {
        [Tooltip("Liga ou desliga o trilho. O ponto móvel usa ligado; o inimigo móvel, desligado.")]
        [SerializeField] private bool trilhoLigado = true;
        [SerializeField] private Color corTrilho = (Color)new Color32(0x8d, 0x8e, 0x8f, 255);
        [SerializeField] private Color corPixelAlternado = (Color)new Color32(0x36, 0x36, 0x36, 255);
        [Tooltip("Pixels por unidade da arte do jogo: o trilho usa pixels desse tamanho.")]
        [SerializeField, Min(1f)] private float pixelsPorUnidade = 16f;
        [Tooltip("Ordem de desenho do trilho em relação ao visual do ponto (negativo = atrás).")]
        [SerializeField] private int ordemRelativaAoPonto = -1;

        private const int LarguraPadrao = 2; // pixels de um ciclo claro/escuro, ao longo do trilho
        private const int Espessura = 3;     // pixels, de lado a lado

        private GameObject raiz;
        private Texture2D textura;
        private Sprite sprite;

        private void Start()
        {
            if (!trilhoLigado)
            {
                return;
            }

            InimigoMovel movel = GetComponent<InimigoMovel>();
            Vector2[] pontos = movel.PontosDaRotaNoMundo();
            if (pontos.Length < 2)
            {
                return;
            }

            SpriteRenderer referencia = VisualMaisAoFundo();
            sprite = CriarSprite();

            // Fora da hierarquia do ponto: o trilho não anda junto com ele.
            raiz = new GameObject($"Trilho de {name}");
            raiz.transform.SetParent(transform.parent, true);

            int trechos = movel.EmLoop ? pontos.Length : pontos.Length - 1;
            for (int i = 0; i < trechos; i++)
            {
                CriarTrecho(pontos[i], pontos[(i + 1) % pontos.Length], referencia);
            }
        }

        // O visual que aparece de verdade com a menor ordem de desenho. Renderers desligados (como o do
        // objeto raiz do prefab) não contam, senão o trilho herdaria a ordem deles e ficaria por cima.
        private SpriteRenderer VisualMaisAoFundo()
        {
            SpriteRenderer menor = null;
            foreach (SpriteRenderer visual in GetComponentsInChildren<SpriteRenderer>())
            {
                if (visual.enabled && (menor == null || visual.sortingOrder < menor.sortingOrder))
                {
                    menor = visual;
                }
            }

            return menor;
        }

        private void OnDestroy()
        {
            if (raiz != null)
            {
                Destroy(raiz);
            }

            if (sprite != null)
            {
                Destroy(sprite);
            }

            if (textura != null)
            {
                Destroy(textura);
            }
        }

        private void CriarTrecho(Vector2 inicio, Vector2 fim, SpriteRenderer referencia)
        {
            Vector2 direcao = fim - inicio;
            float comprimento = direcao.magnitude;
            if (comprimento <= 0f)
            {
                return;
            }

            GameObject trecho = new GameObject("Trecho");
            trecho.transform.SetParent(raiz.transform, false);
            trecho.transform.position = new Vector3((inicio.x + fim.x) * 0.5f, (inicio.y + fim.y) * 0.5f, transform.position.z);
            trecho.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg);

            SpriteRenderer sr = trecho.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(comprimento, Espessura / pixelsPorUnidade);
            if (referencia != null)
            {
                sr.sharedMaterial = referencia.sharedMaterial;
                sr.sortingLayerID = referencia.sortingLayerID;
                sr.sortingOrder = referencia.sortingOrder + ordemRelativaAoPonto;
            }
        }

        // Textura de 2x3 pixels: linhas de cima e de baixo cinza; a do meio alterna cinza e escuro.
        private Sprite CriarSprite()
        {
            textura = new Texture2D(LarguraPadrao, Espessura, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (int y = 0; y < Espessura; y++)
            {
                for (int x = 0; x < LarguraPadrao; x++)
                {
                    bool alternado = y == Espessura / 2 && x == LarguraPadrao - 1;
                    textura.SetPixel(x, y, alternado ? corPixelAlternado : corTrilho);
                }
            }

            textura.Apply(false, true);
            return Sprite.Create(
                textura,
                new Rect(0f, 0f, LarguraPadrao, Espessura),
                new Vector2(0.5f, 0.5f),
                pixelsPorUnidade,
                0,
                SpriteMeshType.FullRect);
        }
    }
}
