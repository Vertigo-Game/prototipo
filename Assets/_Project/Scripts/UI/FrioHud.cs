using UnityEngine;
using UnityEngine.UI;
using Vertigo.Gameplay;

namespace Vertigo.UI
{
    // HUD da Barra de Frio, montado em runtime (arte provisória gerada por código):
    // - frasco vertical no canto superior esquerdo, enchendo de azul de baixo pra cima;
    // - geada nas bordas da tela que entra a partir de inicioVinheta.
    // O frasco aparece quando há frio, fica um tempo depois de zerar e some.
    [RequireComponent(typeof(BarraDeFrio))]
    public sealed class FrioHud : MonoBehaviour
    {
        // Tamanho de 1 pixel da textura da geada, em unidades do Canvas 640x360.
        private const float PixelGeada = 2f;
        private const int LarguraFrasco = 12;
        private const int AlturaFrasco = 56;
        // Linhas do frasco que recebem líquido (de baixo pra cima, em pixels da textura).
        private const int PrimeiraLinhaLiquido = 1;
        private const int UltimaLinhaLiquido = AlturaFrasco - 4;

        [Header("Liga/desliga")]
        [SerializeField] private bool mostrarFrasco = true;
        [SerializeField, Tooltip("Geada nas bordas e tingimento azul da tela.")]
        private bool mostrarGeada = true;

        [Header("Frasco")]
        [SerializeField] private Vector2 margem = new Vector2(12f, 12f);
        [SerializeField] private int escalaPixel = 2;
        [SerializeField] private float tempoVisivelAposZerar = 2f;
        [SerializeField] private float duracaoFade = 0.5f;
        [SerializeField] private Color corLiquido = new Color(0.33f, 0.62f, 1f, 1f);

        [Header("Geada nas bordas")]
        [SerializeField, Range(0f, 1f)] private float inicioVinheta = 0.3f;
        [SerializeField, Range(0f, 1f)] private float opacidadeMaxVinheta = 0.85f;
        [SerializeField, Tooltip("Quanto a geada começa fora da tela (unidades do Canvas 640x360).")]
        private float recuoInicialVinheta = 90f;
        [SerializeField] private Color corGeada = new Color(0.78f, 0.9f, 1f, 1f);

        [Header("Tela congelando")]
        [SerializeField, Range(0f, 1f)] private float opacidadeMaxTingimento = 0.3f;
        [SerializeField] private Color corTingimento = new Color(0.35f, 0.6f, 1f, 1f);

        [Header("Canvas")]
        [SerializeField] private int ordemCanvas = -10;

        private BarraDeFrio barra;
        private GameObject raiz;
        private CanvasGroup grupoFrasco;
        private Image liquido;
        private Image vinheta;
        private RectTransform vinhetaRect;
        private Image tingimento;
        private float tempoDesdeZerar = float.PositiveInfinity;
        private readonly System.Collections.Generic.List<Object> criados = new System.Collections.Generic.List<Object>();

        private void Awake()
        {
            barra = GetComponent<BarraDeFrio>();
            MontarHud();
        }

        private void OnDestroy()
        {
            if (raiz != null)
            {
                Destroy(raiz);
            }

            foreach (Object o in criados)
            {
                if (o != null)
                {
                    Destroy(o);
                }
            }
        }

        private void LateUpdate()
        {
            float frio = barra.Frio;

            // Frasco: nível travado nas linhas de pixel da textura.
            int linhas = Mathf.RoundToInt(frio * (UltimaLinhaLiquido - PrimeiraLinhaLiquido + 1));
            liquido.fillAmount = linhas == 0 ? 0f : (float)(PrimeiraLinhaLiquido + linhas) / AlturaFrasco;

            if (frio > 0f)
            {
                tempoDesdeZerar = 0f;
            }
            else
            {
                tempoDesdeZerar += Time.unscaledDeltaTime;
            }

            float alvo = mostrarFrasco && tempoDesdeZerar <= tempoVisivelAposZerar ? 1f : 0f;
            grupoFrasco.alpha = Mathf.MoveTowards(grupoFrasco.alpha, alvo, Time.unscaledDeltaTime / duracaoFade);

            // Geada: entra das bordas e fica mais forte entre inicioVinheta e 100%.
            float t = mostrarGeada ? Mathf.InverseLerp(inicioVinheta, 1f, frio) : 0f;
            Color c = corGeada;
            c.a = opacidadeMaxVinheta * t;
            vinheta.color = c;
            vinheta.enabled = t > 0f;

            // Tingimento azul da tela toda: cresce junto com a geada, mas suave (curva ease-in).
            Color ct = corTingimento;
            ct.a = opacidadeMaxTingimento * t * t;
            tingimento.color = ct;
            tingimento.enabled = ct.a > 0f;

            // Recuo em múltiplos do pixel da textura, pra geada andar sem tremer.
            float recuo = Mathf.Round(recuoInicialVinheta * (1f - t) / PixelGeada) * PixelGeada;
            vinhetaRect.offsetMin = new Vector2(-recuo, -recuo);
            vinhetaRect.offsetMax = new Vector2(recuo, recuo);
        }

        private void MontarHud()
        {
            raiz = new GameObject("FrioHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = raiz.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ordemCanvas;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = raiz.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.matchWidthOrHeight = 1f;

            // Tingimento azul no fundo, depois a geada, e por cima o frasco.
            tingimento = CriarImagem("Tingimento", raiz.transform, CriarSprite(CriarTexturaBranca(), FilterMode.Point));
            Esticar(tingimento.rectTransform);
            tingimento.enabled = false;

            vinheta = CriarImagem("Geada", raiz.transform, CriarSprite(CriarTexturaGeada(), FilterMode.Point));
            vinhetaRect = vinheta.rectTransform;
            vinhetaRect.anchorMin = Vector2.zero;
            vinhetaRect.anchorMax = Vector2.one;
            vinhetaRect.offsetMin = Vector2.zero;
            vinhetaRect.offsetMax = Vector2.zero;
            vinheta.enabled = false;

            GameObject frasco = new GameObject("Frasco", typeof(RectTransform), typeof(CanvasGroup));
            frasco.transform.SetParent(raiz.transform, false);
            RectTransform frascoRect = (RectTransform)frasco.transform;
            frascoRect.anchorMin = frascoRect.anchorMax = frascoRect.pivot = new Vector2(0f, 1f);
            frascoRect.anchoredPosition = new Vector2(margem.x, -margem.y);
            frascoRect.sizeDelta = new Vector2(LarguraFrasco, AlturaFrasco) * escalaPixel;
            grupoFrasco = frasco.GetComponent<CanvasGroup>();
            grupoFrasco.alpha = 0f;
            grupoFrasco.interactable = false;
            grupoFrasco.blocksRaycasts = false;

            Texture2D texVidro;
            Texture2D texLiquido;
            CriarTexturasFrasco(out texVidro, out texLiquido);

            liquido = CriarImagem("Liquido", frasco.transform, CriarSprite(texLiquido, FilterMode.Point));
            Esticar(liquido.rectTransform);
            liquido.type = Image.Type.Filled;
            liquido.fillMethod = Image.FillMethod.Vertical;
            liquido.fillOrigin = (int)Image.OriginVertical.Bottom;
            liquido.fillAmount = 0f;

            Image vidro = CriarImagem("Vidro", frasco.transform, CriarSprite(texVidro, FilterMode.Point));
            Esticar(vidro.rectTransform);
        }

        private static Image CriarImagem(string nome, Transform pai, Sprite sprite)
        {
            GameObject go = new GameObject(nome, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(pai, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private static void Esticar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Sprite CriarSprite(Texture2D tex, FilterMode filtro)
        {
            tex.filterMode = filtro;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1f);
            criados.Add(tex);
            criados.Add(s);
            return s;
        }

        // Tubo de ensaio 12x56: boca com aba em cima, fundo arredondado.
        private void CriarTexturasFrasco(out Texture2D vidro, out Texture2D liquidoTex)
        {
            const int w = LarguraFrasco;
            const int h = AlturaFrasco;
            Color contorno = new Color(0.86f, 0.94f, 1f, 1f);
            Color interiorVidro = new Color(0.7f, 0.85f, 1f, 0.16f);
            Color brilhoVidro = new Color(1f, 1f, 1f, 0.45f);
            Color brilhoLiquido = Color.Lerp(corLiquido, Color.white, 0.45f);
            Color sombraLiquido = Color.Lerp(corLiquido, Color.black, 0.25f);

            Color[] pv = new Color[w * h];
            Color[] pl = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                int bordaEsq, bordaDir; // colunas do contorno
                if (y == 0) { bordaEsq = 4; bordaDir = 7; }
                else if (y == 1) { bordaEsq = 3; bordaDir = 8; }
                else if (y >= h - 2) { bordaEsq = 1; bordaDir = 10; }
                else { bordaEsq = 2; bordaDir = 9; }

                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    pv[i] = Color.clear;
                    pl[i] = Color.clear;

                    if (x < bordaEsq || x > bordaDir)
                    {
                        continue;
                    }

                    bool contornoAqui = x == bordaEsq || x == bordaDir || y == 0;
                    // Aba da boca: linha que liga o tubo à aba.
                    if (y == h - 3 && (x <= 2 || x >= 9))
                    {
                        contornoAqui = true;
                    }

                    if (contornoAqui)
                    {
                        pv[i] = contorno;
                        continue;
                    }

                    pv[i] = x == bordaEsq + 1 && y > 2 && y < h - 4 ? brilhoVidro : interiorVidro;

                    if (y >= PrimeiraLinhaLiquido && y <= UltimaLinhaLiquido)
                    {
                        if (x == bordaEsq + 1) pl[i] = brilhoLiquido;
                        else if (x == bordaDir - 1) pl[i] = sombraLiquido;
                        else pl[i] = corLiquido;
                    }
                }
            }

            // Contorno da aba (y == h-3) também cobre as colunas 1 e 10.
            pv[(h - 3) * w + 1] = contorno;
            pv[(h - 3) * w + 10] = contorno;

            vidro = new Texture2D(w, h, TextureFormat.RGBA32, false);
            vidro.SetPixels(pv);
            liquidoTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            liquidoTex.SetPixels(pl);
        }

        private static Texture2D CriarTexturaBranca()
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            return tex;
        }

        // Moldura de geada 320x180 (1 px = PixelGeada unidades do Canvas), borda irregular em degraus.
        // Alpha em vários degraus; a franja externa é mais branca e o miolo mais azul.
        private static Texture2D CriarTexturaGeada()
        {
            const int w = 320;
            const int h = 180;
            const int degraus = 6;
            Color franja = Color.white;
            Color miolo = new Color(0.72f, 0.88f, 1f, 1f);
            Color[] p = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                    float v = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                    // Distância "quadradona" pra geada abraçar as bordas.
                    float d = Mathf.Pow(Mathf.Pow(u, 6f) + Mathf.Pow(v, 6f), 1f / 6f);
                    // Ruído em 3 escalas: grão fino, cristais médios e manchas grandes.
                    float ruido = Hash(x, y) * 0.07f + Hash(x / 2, y / 2) * 0.07f + Hash(x / 6, y / 6) * 0.09f;
                    float a = Mathf.InverseLerp(0.68f, 1f, d + ruido - 0.1f);
                    a = Mathf.Clamp01(a);
                    int nivel = Mathf.FloorToInt(a * degraus);
                    float aq = nivel / (float)(degraus - 1);
                    Color c = Color.Lerp(franja, miolo, aq);
                    c.a = Mathf.Clamp01(aq);
                    p[y * w + x] = c;
                }
            }

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(p);
            return tex;
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                uint n = (uint)(x * 374761393 + y * 668265263);
                n = (n ^ (n >> 13)) * 1274126177u;
                return ((n ^ (n >> 16)) & 0xFFFF) / 65535f;
            }
        }
    }
}
