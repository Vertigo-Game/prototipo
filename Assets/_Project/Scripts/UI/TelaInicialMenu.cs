using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Vertigo.UI
{
    public sealed class TelaInicialMenu : MonoBehaviour, IMenuVertical
    {
        [Header("Itens (0 = Jogar, 1 = Controles)")]
        [SerializeField] private GameObject painelPrincipal;
        [SerializeField] private TMP_Text[] itens;
        [SerializeField] private TMP_Text marcador;
        [SerializeField] private float distanciaMarcador = 16f;

        [Header("Cores")]
        [SerializeField] private Color corNormal = new Color32(0xEE, 0xF2, 0xF6, 0xFF);
        [SerializeField] private Color corSelecionada = new Color32(0xF2, 0x99, 0x4A, 0xFF);

        [Header("Controles")]
        [SerializeField] private GameObject painelControles;
        [SerializeField] private TMP_Text[] itensControles;
        [SerializeField] private TMP_Text marcadorControles;
        [SerializeField] private TMP_Text mensagemControles;

        [Header("Transição")]
        [SerializeField] private Image fade;
        [SerializeField] private float duracaoFade = 0.5f;
        [SerializeField] private string cenaDoJogo = "Fase1";

        [Header("Polimento")]
        [SerializeField] private float intervaloPiscarMarcador = 0.5f;

        private int selecionado;
        private bool confirmando;
        private bool controles;
        private RemapeamentoControles remapeamento;
        private float tempoPiscar;
        private int frameAcao = -1;

        private bool Remapeando => controles && remapeamento.AguardandoTecla;
        private TMP_Text[] ItensAtuais => controles ? itensControles : itens;
        private TMP_Text MarcadorAtual => controles ? marcadorControles : marcador;

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
                MostrarMenu(false);
                return;
            }
            Selecionar(0);
        }

        private void Update()
        {
            if (confirmando || frameAcao == Time.frameCount)
            {
                return;
            }

            if (controles && MenuInput.PausePressed())
            {
                if (Remapeando) remapeamento.Cancelar();
                else MostrarMenu(false);
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
            if (controles)
            {
                if (remapeamento.Confirmar(indice)) MostrarMenu(false);
                frameAcao = Time.frameCount;
            }
            else if (indice == 0)
            {
                StartCoroutine(FadeECarregar());
            }
            else if (painelControles != null)
            {
                MostrarMenu(true);
            }
        }

        private void MostrarMenu(bool abrirControles)
        {
            controles = abrirControles;
            painelPrincipal.SetActive(!controles);
            painelControles.SetActive(controles);
            if (controles) remapeamento.Abrir();
            frameAcao = Time.frameCount;
            Selecionar(0);
        }

        private IEnumerator FadeECarregar()
        {
            confirmando = true;
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

            // Jogo novo pela tela inicial: começa do pé da montanha, sem checkpoint da partida anterior.
            Vertigo.Gameplay.Checkpoint.Limpar();
            string destino = cenaDoJogo;
#if UNITY_EDITOR
            string cenaDoEditor = Vertigo.Gameplay.ModoTeste.CenaDoJogoNoEditor;
            if (!string.IsNullOrEmpty(cenaDoEditor))
            {
                destino = cenaDoEditor;
            }
#endif
            SceneManager.LoadScene(destino);
        }

    }
}
