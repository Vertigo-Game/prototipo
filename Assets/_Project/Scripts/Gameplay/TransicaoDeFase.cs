using System.Collections;
using HookPrototype;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Vertigo.Gameplay
{
    public sealed class TransicaoDeFase : MonoBehaviour
    {
        private const float DuracaoFade = 0.45f;
        private static TransicaoDeFase atual;
        private Image cortina;
        private PlayerController jogadorNaChegada;

        public static bool EmAndamento => atual != null;

        public static void Iniciar(PlayerController jogador, string proximaCena)
        {
            if (EmAndamento || jogador.IsDead || jogador.InputLocked)
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(proximaCena))
            {
                Debug.LogError($"A próxima fase '{proximaCena}' precisa estar habilitada no build.", jogador);
                return;
            }

            jogador.InputLocked = true;
            jogador.CompletePrototype();

            GameObject raiz = new GameObject("Transição de fase", typeof(RectTransform), typeof(Canvas));
            DontDestroyOnLoad(raiz);
            Canvas canvas = raiz.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            GameObject fundo = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)fundo.transform;
            rect.SetParent(raiz.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            atual = raiz.AddComponent<TransicaoDeFase>();
            atual.cortina = fundo.GetComponent<Image>();
            atual.cortina.color = Color.clear;
            atual.cortina.raycastTarget = false;
            atual.StartCoroutine(atual.Carregar(proximaCena));
        }

        private IEnumerator Carregar(string proximaCena)
        {
            yield return Fade(0f, 1f);

            Checkpoint.Limpar();
            SceneManager.sceneLoaded += BloquearEntradaNaChegada;
            yield return SceneManager.LoadSceneAsync(proximaCena, LoadSceneMode.Single);
            SceneManager.sceneLoaded -= BloquearEntradaNaChegada;

            yield return Fade(1f, 0f);

            Destroy(gameObject);
        }

        private void BloquearEntradaNaChegada(Scene cena, LoadSceneMode modo)
        {
            PlayerController jogador = FindAnyObjectByType<PlayerController>();
            if (jogador != null && jogador.gameObject.scene == cena && !jogador.InputLocked)
            {
                jogador.InputLocked = true;
                jogadorNaChegada = jogador;
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= BloquearEntradaNaChegada;
            if (jogadorNaChegada != null)
            {
                jogadorNaChegada.InputLocked = false;
            }
        }

        private IEnumerator Fade(float inicio, float fim)
        {
            for (float tempo = 0f; tempo < DuracaoFade; tempo += Time.unscaledDeltaTime)
            {
                cortina.color = new Color(0f, 0f, 0f, Mathf.Lerp(inicio, fim, tempo / DuracaoFade));
                yield return null;
            }

            cortina.color = new Color(0f, 0f, 0f, fim);
        }
    }
}
