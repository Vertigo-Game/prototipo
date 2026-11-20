using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Vertigo.Gameplay
{
    public enum AcaoJogo { Esquerda, Direita, Cima, Baixo, Pular, Dash, Gancho, Interagir, Reiniciar }

    public static class ControlesJogo
    {
        private const string Preferencia = "Vertigo.Controles.v1";
        [Serializable]
        private sealed class Preferencias
        {
            public string[] teclas;
        }
        private static InputActionMap mapa;
        private static readonly string[] nomes =
            { "Esquerda", "Direita", "Subir", "Descer", "Pular", "Dash", "Gancho", "Interagir", "Reiniciar fase" };
        private static readonly string[] padroes =
            { "<Keyboard>/a", "<Keyboard>/d", "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/space", "<Keyboard>/leftShift", "<Mouse>/leftButton", "<Keyboard>/e", "<Keyboard>/r" };
        private static readonly string[] setas = { "leftArrow", "rightArrow", "upArrow", "downArrow" };

        public static int Quantidade => nomes.Length;
        public static string Nome(int indice) => nomes[indice];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Recarregar()
        {
            mapa?.Dispose();
            mapa = null;
        }

        private static InputAction Acao(AcaoJogo acao)
        {
            if (mapa == null)
            {
                mapa = new InputActionMap("Jogo");
                for (int i = 0; i < padroes.Length; i++)
                {
                    InputAction entrada = mapa.AddAction(((AcaoJogo)i).ToString(), InputActionType.Button, padroes[i]);
                    if (i < setas.Length) entrada.AddBinding("<Keyboard>/" + setas[i]);
                }
                if (PlayerPrefs.HasKey(Preferencia))
                {
                    try
                    {
                        var preferencias = JsonUtility.FromJson<Preferencias>(PlayerPrefs.GetString(Preferencia));
                        if (preferencias?.teclas != null && preferencias.teclas.Length == padroes.Length)
                        {
                            for (int i = 0; i < padroes.Length; i++)
                                mapa.actions[i].ApplyBindingOverride(0, preferencias.teclas[i]);
                        }
                    }
                    catch (ArgumentException) { mapa.RemoveAllBindingOverrides(); }
                }
                mapa.Enable();
            }
            return mapa.actions[(int)acao];
        }

        public static bool Segurando(AcaoJogo acao) => Acao(acao).IsPressed();
        public static bool Pressionou(AcaoJogo acao) => Acao(acao).WasPressedThisFrame();

        public static float Horizontal()
        {
            float valor = (Segurando(AcaoJogo.Direita) ? 1f : 0f) - (Segurando(AcaoJogo.Esquerda) ? 1f : 0f);
            float analogico = Gamepad.current?.leftStick.x.ReadValue() ?? 0f;
            return Mathf.Abs(analogico) > Mathf.Abs(valor) ? analogico : valor;
        }

        public static float Vertical()
        {
            float valor = (Segurando(AcaoJogo.Cima) ? 1f : 0f) - (Segurando(AcaoJogo.Baixo) ? 1f : 0f);
            float analogico = Gamepad.current?.leftStick.y.ReadValue() ?? 0f;
            return Mathf.Abs(analogico) > Mathf.Abs(valor) ? analogico : valor;
        }

        public static bool Interagir() => Pressionou(AcaoJogo.Interagir) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        public static string Tecla(int indice) => Acao((AcaoJogo)indice).GetBindingDisplayString();

        public static bool Remapear(int indice, string caminho, out string mensagem)
        {
            InputAction entrada = Acao((AcaoJogo)indice);
            for (int i = 0; i < Quantidade; i++)
            {
                if (i == indice) continue;
                foreach (var binding in Acao((AcaoJogo)i).bindings)
                {
                    if (!string.Equals(binding.effectivePath, caminho, StringComparison.OrdinalIgnoreCase)) continue;
                    mensagem = "Tecla em uso: " + Nome(i) + ". Escolha outra.";
                    return false;
                }
            }
            entrada.ApplyBindingOverride(0, caminho);
            Salvar();
            mensagem = "Controle salvo. Selecione outra ação para alterar.";
            return true;
        }

        public static void Restaurar()
        {
            Acao(AcaoJogo.Esquerda);
            mapa.RemoveAllBindingOverrides();
            Salvar();
        }

        private static void Salvar()
        {
            // Os IDs dos bindings criados em código mudam a cada execução; salve por ação.
            var preferencias = new Preferencias { teclas = new string[Quantidade] };
            for (int i = 0; i < Quantidade; i++) preferencias.teclas[i] = mapa.actions[i].bindings[0].effectivePath;
            PlayerPrefs.SetString(Preferencia, JsonUtility.ToJson(preferencias));
            PlayerPrefs.Save();
        }

        public static string LerNovaTecla()
        {
            if (Keyboard.current != null)
            {
                foreach (var tecla in Keyboard.current.allKeys)
                {
                    if (tecla.keyCode != Key.Escape && tecla.wasPressedThisFrame)
                        return "<Keyboard>/" + tecla.name;
                }
            }
            if (Mouse.current == null) return null;
            if (Mouse.current.leftButton.wasPressedThisFrame) return "<Mouse>/leftButton";
            if (Mouse.current.rightButton.wasPressedThisFrame) return "<Mouse>/rightButton";
            if (Mouse.current.middleButton.wasPressedThisFrame) return "<Mouse>/middleButton";
            return null;
        }
    }
}
