using System.Collections;
using HookPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Vertigo.UI
{
    // Esc/Start permanece disponível depois de remapear os controles do jogo.
    [DefaultExecutionOrder(-100)]
    public sealed class MenuPausa : MonoBehaviour, IMenuVertical
    {
        [SerializeField] private GameObject painel;
        [SerializeField] private TMP_Text[] itens;
        [SerializeField] private TMP_Text marcador;
        [SerializeField] private float distanciaMarcador = 16f;
        [SerializeField] private Color corNormal = new Color32(0xEE, 0xF2, 0xF6, 0xFF);
        [SerializeField] private Color corSelecionada = new Color32(0xF2, 0x99, 0x4A, 0xFF);
        [SerializeField] private float intervaloPiscarMarcador = 0.5f;

        [Header("Controles")]
        [SerializeField] private GameObject painelControles;
        [SerializeField] private TMP_Text[] itensControles;
        [SerializeField] private TMP_Text marcadorControles;
        [SerializeField] private TMP_Text mensagemControles;

        [Header("Transição")]
        [SerializeField] private Image fade;
        [SerializeField] private float duracaoFade = 0.5f;
        [SerializeField] private string cenaTelaInicial = "TelaInicial";

        [Header("Referências")]
        [SerializeField] private PlayerController jogador;
        [SerializeField] private CaixaDeTexto caixa;

        private float escalaDeTempoAnterior = 1f;
        private bool entradaBloqueadaAnterior;
        private bool cursorVisivelAnterior;
        private CursorLockMode cursorAnterior;
        private int selecionado;
        private bool saindo;
        private bool controles;
        private RemapeamentoControles remapeamento;
        private float tempoPiscar;
        private int frameAcao = -1;

        public bool Aberto { get; private set; }
        private bool Remapeando => controles && remapeamento.AguardandoTecla;
        private TMP_Text[] ItensAtuais => controles ? itensControles : itens;
        private TMP_Text MarcadorAtual => controles ? marcadorControles : marcador;

        private void Awake()
        {
            painel.SetActive(false);
            if (painelControles != null)
            {
                painelControles.SetActive(false);
                remapeamento = new RemapeamentoControles(itensControles, mensagemControles);
            }
            if (fade == null) return;
            fade.color = Color.clear;
            fade.raycastTarget = false;
        }

        private void Update()
        {
            if (saindo) return;
            if (!Aberto)
            {
                bool caixaOcupada = caixa != null && (caixa.Aberta || caixa.FrameFechada == Time.frameCount);
                if (!caixaOcupada && jogador != null && !jogador.IsDead && MenuInput.PausePressed()) Abrir();
                return;
            }
            if (frameAcao == Time.frameCount) return;
            if (MenuInput.PausePressed())
            {
                if (Remapeando) remapeamento.Cancelar();
                else if (controles) MostrarMenu();
                else Fechar();
                return;
            }
            if (Remapeando)
            {
                remapeamento.LerEntrada();
                return;
            }
            if (MenuInput.UpPressed()) Selecionar((selecionado - 1 + ItensAtuais.Length) % ItensAtuais.Length);
            else if (MenuInput.DownPressed()) Selecionar((selecionado + 1) % ItensAtuais.Length);
            else if (MenuInput.ConfirmPressed()) { Confirmar(selecionado); return; }

            tempoPiscar += Time.unscaledDeltaTime;
            if (tempoPiscar < intervaloPiscarMarcador) return;
            tempoPiscar = 0f;
            MarcadorAtual.enabled = !MarcadorAtual.enabled;
        }

        public void Abrir()
        {
            if (Aberto || saindo) return;
            Aberto = true;
            escalaDeTempoAnterior = Time.timeScale;
            entradaBloqueadaAnterior = jogador != null && jogador.InputLocked;
            cursorVisivelAnterior = Cursor.visible;
            cursorAnterior = Cursor.lockState;
            Time.timeScale = 0f;
            if (jogador != null) jogador.InputLocked = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            MostrarMenu();
        }

        public void Fechar()
        {
            if (!Aberto || saindo) return;
            Aberto = false;
            controles = false;
            painel.SetActive(false);
            if (painelControles != null) painelControles.SetActive(false);
            Time.timeScale = escalaDeTempoAnterior;
            if (jogador != null) jogador.InputLocked = entradaBloqueadaAnterior;
            Cursor.lockState = cursorAnterior;
            Cursor.visible = cursorVisivelAnterior;
        }

        private void OnDisable()
        {
            if (Aberto && !saindo) Fechar();
        }

        private void MostrarMenu()
        {
            controles = false;
            if (painelControles != null) painelControles.SetActive(false);
            painel.SetActive(true);
            frameAcao = Time.frameCount;
            Selecionar(0);
        }

        private void MostrarControles()
        {
            controles = true;
            painel.SetActive(false);
            painelControles.SetActive(true);
            frameAcao = Time.frameCount;
            remapeamento.Abrir();
            Selecionar(0);
        }

        public void Selecionar(int indice)
        {
            if (!Aberto || saindo || Remapeando || indice < 0 || indice >= ItensAtuais.Length) return;
            selecionado = indice;
            for (int i = 0; i < ItensAtuais.Length; i++) ItensAtuais[i].color = i == indice ? corSelecionada : corNormal;
            MarcadorAtual.rectTransform.anchoredPosition = ItensAtuais[indice].rectTransform.anchoredPosition + Vector2.left * distanciaMarcador;
            MarcadorAtual.color = corSelecionada;
            MarcadorAtual.enabled = true;
            tempoPiscar = 0f;
        }

        public void Confirmar(int indice)
        {
            if (!Aberto || saindo || Remapeando || indice < 0 || indice >= ItensAtuais.Length) return;
            Selecionar(indice);
            if (controles)
            {
                if (remapeamento.Confirmar(indice)) MostrarMenu();
                frameAcao = Time.frameCount;
            }
            else if (indice == 0) Fechar();
            else if (indice == 1 && painelControles != null) MostrarControles();
            else StartCoroutine(FadeEVoltar());
        }

        private IEnumerator FadeEVoltar()
        {
            saindo = true;
            if (fade != null)
            {
                fade.transform.SetAsLastSibling();
                fade.raycastTarget = true;
                for (float t = 0f; t < duracaoFade; t += Time.unscaledDeltaTime)
                {
                    fade.color = new Color(0f, 0f, 0f, t / duracaoFade);
                    yield return null;
                }
                fade.color = Color.black;
            }
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(cenaTelaInicial);
        }
    }
}
