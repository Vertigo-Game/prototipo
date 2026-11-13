using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Vertigo.UI
{
    public sealed class TelaInicialMenu : MonoBehaviour, IMenuVertical
    {
        [Header("Itens (0 = Jogar, 1 = Sair)")]
        [SerializeField] private TMP_Text[] itens;
        [SerializeField] private TMP_Text marcador;
        [SerializeField] private float distanciaMarcador = 16f;

        [Header("Cores")]
        [SerializeField] private Color corNormal = new Color32(0xEE, 0xF2, 0xF6, 0xFF);
        [SerializeField] private Color corSelecionada = new Color32(0xF2, 0x99, 0x4A, 0xFF);

        [Header("Transição")]
        [SerializeField] private Image fade;
        [SerializeField] private float duracaoFade = 0.5f;
        [SerializeField] private string cenaDoJogo = "Fase1";

        [Header("Polimento")]
        [SerializeField] private float intervaloPiscarMarcador = 0.5f;

        private int selecionado;
        private bool confirmando;
        private float tempoPiscar;

        private void Start()
        {
            if (fade != null)
            {
                fade.color = new Color(0f, 0f, 0f, 0f);
                fade.raycastTarget = false;
            }
            Selecionar(0);
        }

        private void Update()
        {
            if (confirmando)
            {
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

        public void Selecionar(int indice)
        {
            if (confirmando || indice < 0 || indice >= itens.Length)
            {
                return;
            }

            selecionado = indice;
            for (int i = 0; i < itens.Length; i++)
            {
                itens[i].color = i == selecionado ? corSelecionada : corNormal;
            }

            RectTransform item = itens[selecionado].rectTransform;
            marcador.rectTransform.anchoredPosition = item.anchoredPosition + Vector2.left * distanciaMarcador;
            marcador.color = corSelecionada;
            marcador.enabled = true;
            tempoPiscar = 0f;
        }

        public void Confirmar(int indice)
        {
            if (confirmando)
            {
                return;
            }

            Selecionar(indice);
            if (indice == 0)
            {
                StartCoroutine(FadeECarregar());
            }
            else
            {
                Sair();
            }
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
            SceneManager.LoadScene(cenaDoJogo);
        }

        private static void Sair()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
