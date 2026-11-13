using System.Collections;
using HookPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Vertigo.UI
{
    // Esc (ou Start) pausa e abre o menu: 0 = Continuar, 1 = Tela inicial.
    public sealed class MenuPausa : MonoBehaviour, IMenuVertical
    {
        [SerializeField] private GameObject painel;
        [SerializeField] private TMP_Text[] itens;
        [SerializeField] private TMP_Text marcador;
        [SerializeField] private float distanciaMarcador = 16f;
        [SerializeField] private Color corNormal = new Color32(0xEE, 0xF2, 0xF6, 0xFF);
        [SerializeField] private Color corSelecionada = new Color32(0xF2, 0x99, 0x4A, 0xFF);
        [SerializeField] private float intervaloPiscarMarcador = 0.5f;

        [Header("Transição")]
        [SerializeField] private Image fade;
        [SerializeField] private float duracaoFade = 0.5f;
        [SerializeField] private string cenaTelaInicial = "TelaInicial";

        [Header("Referências")]
        [SerializeField] private PlayerController jogador;
        [SerializeField] private CaixaDeTexto caixa;

        private float escalaDeTempoAnterior = 1f;
        private int selecionado;
        private bool saindo;
        private float tempoPiscar;
        private int frameAberto = -1;

        public bool Aberto { get; private set; }

        private void Awake()
        {
            painel.SetActive(false);
            if (fade != null)
            {
                fade.color = new Color(0f, 0f, 0f, 0f);
                fade.raycastTarget = false;
            }
        }

        private void Update()
        {
            if (saindo)
            {
                return;
            }

            if (!Aberto)
            {
                bool caixaOcupada = caixa != null && (caixa.Aberta || caixa.FrameFechada == Time.frameCount);
                bool gameOver = jogador != null && jogador.IsDead;
                if (!caixaOcupada && !gameOver && MenuInput.PausePressed())
                {
                    Abrir();
                }
                return;
            }

            if (frameAberto == Time.frameCount)
            {
                return;
            }

            if (MenuInput.PausePressed())
            {
                Fechar();
                return;
            }

            if (MenuInput.UpPressed())
            {
                Selecionar((selecionado - 1 + itens.Length) % itens.Length);
            }
            else if (MenuInput.DownPressed())
            {
                Selecionar((selecionado + 1) % itens.Length);
            }
            else if (MenuInput.ConfirmPressed())
            {
                Confirmar(selecionado);
                return;
            }

            tempoPiscar += Time.unscaledDeltaTime;
            if (tempoPiscar >= intervaloPiscarMarcador)
            {
                tempoPiscar = 0f;
                marcador.enabled = !marcador.enabled;
            }
        }

        private void Abrir()
        {
            Aberto = true;
            frameAberto = Time.frameCount;
            escalaDeTempoAnterior = Time.timeScale;
            Time.timeScale = 0f;
            if (jogador != null)
            {
                jogador.InputLocked = true;
            }

            painel.SetActive(true);
            Selecionar(0);
        }

        private void Fechar()
        {
            Aberto = false;
            painel.SetActive(false);
            Time.timeScale = escalaDeTempoAnterior;
            if (jogador != null)
            {
                jogador.InputLocked = false;
            }
        }

        public void Selecionar(int indice)
        {
            if (!Aberto || saindo || indice < 0 || indice >= itens.Length)
            {
                return;
            }

            selecionado = indice;
            for (int i = 0; i < itens.Length; i++)
            {
                itens[i].color = i == selecionado ? corSelecionada : corNormal;
            }

            marcador.rectTransform.anchoredPosition =
                itens[selecionado].rectTransform.anchoredPosition + Vector2.left * distanciaMarcador;
            marcador.color = corSelecionada;
            marcador.enabled = true;
            tempoPiscar = 0f;
        }

        public void Confirmar(int indice)
        {
            if (!Aberto || saindo)
            {
                return;
            }

            Selecionar(indice);
            if (indice == 0)
            {
                Fechar();
            }
            else
            {
                StartCoroutine(FadeEVoltar());
            }
        }

        private IEnumerator FadeEVoltar()
        {
            saindo = true;
            marcador.enabled = true;

            if (fade != null)
            {
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
            SceneManager.LoadScene(cenaTelaInicial);
        }
    }
}
