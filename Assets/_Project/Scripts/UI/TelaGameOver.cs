using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.UI
{
    // Tela de Game Over, montada em runtime quando o Marcos morre (PlayerController.Die).
    // Escurece a tela só um pouco (o jogo continua rodando atrás) e mostra "GAME OVER" em vermelho
    // com a dica de reiniciar. O R em si é tratado pelo PlayerController, que recarrega a cena
    // e com ela some esta tela.
    public sealed class TelaGameOver : MonoBehaviour
    {
        private const string Titulo = "GAME OVER";
        private const string Dica = "Aperte 'R' para tentar novamente";

        // A fonte pixel (PixelifySans 11px) fica nítida em múltiplos de 11.
        private const float TamanhoTitulo = 66f;
        private const float TamanhoDica = 22f;

        private const float OpacidadeEscurecer = 0.5f;
        private const float AtrasoTitulo = 0.35f;
        private const float AtrasoDica = 0.9f;
        private const float DuracaoFade = 0.6f;
        private const int OrdemCanvas = 100;

        private static readonly Color CorTitulo = new Color(0.86f, 0.12f, 0.12f, 1f);
        private static readonly Color CorDica = new Color32(0xEE, 0xF2, 0xF6, 0xFF);

        private static TelaGameOver instancia;

        private Image escurecer;
        private TMP_Text titulo;
        private TMP_Text dica;
        private float tempo;

        public static void Mostrar()
        {
            if (instancia != null)
            {
                return;
            }

            instancia = new GameObject("TelaGameOver", typeof(RectTransform)).AddComponent<TelaGameOver>();
        }

        private void Awake()
        {
            Montar();
            Atualizar();
        }

        private void OnDestroy()
        {
            if (instancia == this)
            {
                instancia = null;
            }
        }

        private void Update()
        {
            tempo += Time.unscaledDeltaTime;
            Atualizar();
        }

        private void Atualizar()
        {
            SetAlpha(escurecer, OpacidadeEscurecer * Progresso(0f));
            SetAlpha(titulo, Progresso(AtrasoTitulo));
            SetAlpha(dica, Progresso(AtrasoDica));
        }

        private float Progresso(float atraso)
        {
            return Mathf.Clamp01((tempo - atraso) / DuracaoFade);
        }

        private static void SetAlpha(Graphic grafico, float alpha)
        {
            Color c = grafico.color;
            c.a = alpha;
            grafico.color = c;
        }

        private void Montar()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OrdemCanvas;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.matchWidthOrHeight = 1f;

            escurecer = new GameObject("Escurecer", typeof(RectTransform)).AddComponent<Image>();
            Esticar(escurecer.rectTransform);
            escurecer.color = Color.black;
            escurecer.raycastTarget = false;

            titulo = CriarTexto("Titulo", Titulo, TamanhoTitulo, CorTitulo, new Vector2(0f, 18f));
            dica = CriarTexto("Dica", Dica, TamanhoDica, CorDica, new Vector2(0f, -36f));
        }

        private TMP_Text CriarTexto(string nome, string texto, float tamanho, Color cor, Vector2 posicao)
        {
            // Sem fonte definida, o TMP usa a padrão do projeto (TMP Settings), a mesma dos menus.
            TextMeshProUGUI t = new GameObject(nome, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            RectTransform rect = t.rectTransform;
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, tamanho * 1.5f);
            rect.anchoredPosition = posicao;

            t.text = texto;
            t.fontSize = tamanho;
            t.color = cor;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        private void Esticar(RectTransform rect)
        {
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
