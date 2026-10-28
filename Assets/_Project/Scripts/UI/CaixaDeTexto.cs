using System;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vertigo.UI
{
    // Caixa de leitura no rodapé da tela. Pausa o jogo enquanto está aberta e escreve o texto letra por letra.
    // Uma linha em branco no texto separa as páginas.
    public sealed class CaixaDeTexto : MonoBehaviour
    {
        [SerializeField] private GameObject painel;
        [SerializeField] private TMP_Text texto;
        [SerializeField] private TMP_Text indicadorFechar;
        [SerializeField] private TMP_Text contadorPaginas;

        [Header("Ritmo da escrita")]
        [SerializeField] private float caracteresPorSegundo = 60f;
        [SerializeField] private float pausaVirgula = 0.12f;
        [SerializeField] private float pausaPonto = 0.3f;

        private Action aoFechar;
        private float escalaDeTempoAnterior = 1f;
        private string[] paginas;
        private int paginaAtual;
        private float esperaAteProximo;
        private int frameAberta = -1;

        public bool Aberta { get; private set; }
        public int FrameFechada { get; private set; } = -1;

        private void Awake()
        {
            painel.SetActive(false);
        }

        public void Abrir(string conteudo, Action aoFecharCallback = null)
        {
            if (Aberta)
            {
                return;
            }

            paginas = conteudo.Replace("\r", "").Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (paginas.Length == 0)
            {
                return;
            }

            Aberta = true;
            frameAberta = Time.frameCount;
            aoFechar = aoFecharCallback;
            escalaDeTempoAnterior = Time.timeScale;
            Time.timeScale = 0f;

            painel.SetActive(true);
            MostrarPagina(0);
        }

        private void MostrarPagina(int indice)
        {
            paginaAtual = indice;
            texto.text = paginas[indice].Trim('\n');
            texto.maxVisibleCharacters = 0;
            texto.ForceMeshUpdate();
            esperaAteProximo = 0f;
            indicadorFechar.enabled = false;

            if (contadorPaginas != null)
            {
                contadorPaginas.enabled = paginas.Length > 1;
                contadorPaginas.text = (indice + 1) + "/" + paginas.Length;
            }
        }

        private void Update()
        {
            if (!Aberta)
            {
                return;
            }

            int total = texto.textInfo.characterCount;
            bool escrevendo = texto.maxVisibleCharacters < total;

            if (frameAberta != Time.frameCount)
            {
                if (ReadClosePressed())
                {
                    Fechar();
                    return;
                }

                if (ReadConfirmPressed())
                {
                    if (escrevendo)
                    {
                        texto.maxVisibleCharacters = total;
                    }
                    else if (paginaAtual + 1 < paginas.Length)
                    {
                        MostrarPagina(paginaAtual + 1);
                        return;
                    }
                    else
                    {
                        Fechar();
                        return;
                    }
                }
            }

            esperaAteProximo -= Time.unscaledDeltaTime;
            while (texto.maxVisibleCharacters < total && esperaAteProximo <= 0f)
            {
                int indice = texto.maxVisibleCharacters;
                char letra = texto.textInfo.characterInfo[indice].character;
                char seguinte = indice + 1 < total ? texto.textInfo.characterInfo[indice + 1].character : ' ';
                texto.maxVisibleCharacters++;
                // Reticências pausam uma vez só, no último ponto.
                float pausa = letra == seguinte ? 0f : PausaDepois(letra);
                esperaAteProximo += 1f / caracteresPorSegundo + pausa;
            }

            indicadorFechar.enabled = texto.maxVisibleCharacters >= total;
        }

        private float PausaDepois(char letra)
        {
            switch (letra)
            {
                case ',':
                case ':':
                    return pausaVirgula;
                case '.':
                case '?':
                case '!':
                    return pausaPonto;
                default:
                    return 0f;
            }
        }

        private void Fechar()
        {
            Aberta = false;
            FrameFechada = Time.frameCount;
            painel.SetActive(false);
            Time.timeScale = escalaDeTempoAnterior;

            Action callback = aoFechar;
            aoFechar = null;
            callback?.Invoke();
        }

        private void OnDisable()
        {
            if (Aberta)
            {
                Fechar();
            }
        }

        private static bool ReadConfirmPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        private static bool ReadClosePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
