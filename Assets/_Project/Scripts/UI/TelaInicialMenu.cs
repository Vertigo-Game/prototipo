using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Vertigo.UI
{
    public sealed class TelaInicialMenu : MonoBehaviour, IMenuVertical
    {
        private enum Painel { Principal, Fases, Controles }

        private const int IndiceTrocarFase = 1;

        [Header("Itens (0 = Jogar, 1 = Trocar de fase, 2 = Controles)")]
        [SerializeField] private GameObject painelPrincipal;
        [SerializeField] private TMP_Text[] itens;
        [SerializeField] private TMP_Text marcador;
        [SerializeField] private float distanciaMarcador = 16f;

        [Header("Cores")]
        [SerializeField] private Color corNormal = new Color32(0xEE, 0xF2, 0xF6, 0xFF);
        [SerializeField] private Color corSelecionada = new Color32(0xF2, 0x99, 0x4A, 0xFF);

        [Header("Seleção de fase (Fase 1, Fase 2, Fase 3, Voltar)")]
        [SerializeField] private GameObject painelFases;
        [SerializeField] private TMP_Text[] itensFases;
        [SerializeField] private TMP_Text marcadorFases;
        [SerializeField] private string cenaFase2 = "PassagemDaGeada";
        [SerializeField] private string cenaFase3 = "fase_final_testes";

        [Header("Controles")]
        [SerializeField] private GameObject painelControles;
        [SerializeField] private TMP_Text[] itensControles;
        [SerializeField] private TMP_Text marcadorControles;
        [SerializeField] private TMP_Text mensagemControles;

        [Header("Transição")]
        [SerializeField] private Image fade;
        [SerializeField] private float duracaoFade = 0.5f;
        [SerializeField] private string cenaDoJogo = "Fase1_Validacao";

        [Header("Polimento")]
        [SerializeField] private float intervaloPiscarMarcador = 0.5f;

        private int selecionado;
        private bool confirmando;
        private Painel painelAtual;
        private RemapeamentoControles remapeamento;
        private float tempoPiscar;
        private int frameAcao = -1;

        private bool Remapeando => painelAtual == Painel.Controles && remapeamento.AguardandoTecla;
        private TMP_Text[] ItensAtuais => painelAtual switch
        {
            Painel.Fases => itensFases,
            Painel.Controles => itensControles,
            _ => itens
        };
        private TMP_Text MarcadorAtual => painelAtual switch
        {
            Painel.Fases => marcadorFases,
            Painel.Controles => marcadorControles,
            _ => marcador
        };

        private void Start()
        {
            if (fade != null)
            {
                fade.color = new Color(0f, 0f, 0f, 0f);
                fade.raycastTarget = false;
            }

            if (painelControles != null)
            {
                remapeamento = new RemapeamentoControles(itensControles, mensagemControles);
            }
            MostrarMenu(Painel.Principal);
        }

        private void Update()
        {
            if (confirmando || frameAcao == Time.frameCount)
            {
                return;
            }

            if (painelAtual == Painel.Controles && MenuInput.PausePressed())
            {
                if (Remapeando) remapeamento.Cancelar();
                else MostrarMenu(Painel.Principal);
                return;
            }
            if (painelAtual == Painel.Fases && MenuInput.BackPressed())
            {
                MostrarMenu(Painel.Principal, IndiceTrocarFase);
                return;
            }
            if (Remapeando)
            {
                remapeamento.LerEntrada();
                return;
            }

            if (MenuInput.UpPressed())
            {
                Selecionar((selecionado - 1 + ItensAtuais.Length) % ItensAtuais.Length);
            }
            else if (MenuInput.DownPressed())
            {
                Selecionar((selecionado + 1) % ItensAtuais.Length);
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
                MarcadorAtual.enabled = !MarcadorAtual.enabled;
            }
        }

        public void Selecionar(int indice)
        {
            if (confirmando || Remapeando || indice < 0 || indice >= ItensAtuais.Length)
            {
                return;
            }

            selecionado = indice;
            TMP_Text[] opcoes = ItensAtuais;
            for (int i = 0; i < opcoes.Length; i++)
            {
                opcoes[i].color = i == selecionado ? corSelecionada : corNormal;
            }

            RectTransform item = opcoes[selecionado].rectTransform;
            TMP_Text indicador = MarcadorAtual;
            indicador.rectTransform.anchoredPosition = item.anchoredPosition + Vector2.left * distanciaMarcador;
            indicador.color = corSelecionada;
            indicador.enabled = true;
            tempoPiscar = 0f;
        }

        public void Confirmar(int indice)
        {
            if (confirmando || Remapeando || indice < 0 || indice >= ItensAtuais.Length)
            {
                return;
            }

            Selecionar(indice);
            if (painelAtual == Painel.Controles)
            {
                if (remapeamento.Confirmar(indice)) MostrarMenu(Painel.Principal);
                frameAcao = Time.frameCount;
                return;
            }
            if (painelAtual == Painel.Fases)
            {
                ConfirmarFase(indice);
                return;
            }

            switch (indice)
            {
                case 0:
                    Jogar();
                    break;
                case IndiceTrocarFase when painelFases != null:
                    MostrarMenu(Painel.Fases);
                    break;
                case 2 when painelControles != null:
                    MostrarMenu(Painel.Controles);
                    break;
            }
        }

        private void ConfirmarFase(int indice)
        {
            if (indice == 3)
            {
                MostrarMenu(Painel.Principal, IndiceTrocarFase);
                return;
            }

            string destino = indice switch
            {
                0 => cenaDoJogo,
                1 => cenaFase2,
                2 => cenaFase3,
                _ => null
            };
            StartCoroutine(FadeECarregar(destino));
        }

        private void MostrarMenu(Painel destino, int indice = 0)
        {
            painelAtual = destino;
            painelPrincipal.SetActive(destino == Painel.Principal);
            if (painelFases != null) painelFases.SetActive(destino == Painel.Fases);
            if (painelControles != null) painelControles.SetActive(destino == Painel.Controles);
            if (destino == Painel.Controles) remapeamento.Abrir();
            frameAcao = Time.frameCount;
            Selecionar(indice);
        }

        private void Jogar()
        {
            string destino = cenaDoJogo;
#if UNITY_EDITOR
            string cenaDoEditor = Vertigo.Gameplay.ModoTeste.CenaDoJogoNoEditor;
            if (!string.IsNullOrEmpty(cenaDoEditor))
            {
                destino = cenaDoEditor;
            }
#endif
            StartCoroutine(FadeECarregar(destino));
        }

        private IEnumerator FadeECarregar(string destino)
        {
            if (!Application.CanStreamedLevelBeLoaded(destino))
            {
                Debug.LogError($"A fase '{destino}' precisa estar habilitada no build.", this);
                yield break;
            }

            confirmando = true;
            MarcadorAtual.enabled = true;

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

            // Entrar pela tela inicial sempre começa no ponto inicial da fase escolhida.
            Vertigo.Gameplay.Checkpoint.Limpar();
            SceneManager.LoadScene(destino);
        }

    }
}
