using HookPrototype;
using UnityEditor;
using UnityEngine;
using Vertigo.Gameplay;

namespace Vertigo.EditorTools
{
    // Atalho para ver o final sem andar até a fogueira final: em Play, dispara a aurora no jogador.
    public static class AuroraFinalMenu
    {
        private const string Menu = "Vertigo/Final/Testar a aurora (só em Play)";

        [MenuItem(Menu)]
        private static void Testar()
        {
            PlayerController jogador = Object.FindAnyObjectByType<PlayerController>();
            if (jogador == null)
            {
                Debug.LogWarning("Aurora final: não achei o jogador na cena.");
                return;
            }

            // Usa a música do final de alguma fogueira da cena, se tiver, para o teste soar como o final de verdade.
            AudioClip musica = null;
            AudioClip[] sons = null;
            foreach (Fogueira fogueira in Object.FindObjectsByType<Fogueira>())
            {
                if (fogueira.MusicaFinal != null)
                {
                    musica = fogueira.MusicaFinal;
                    sons = fogueira.SonsBrilhinhos;
                    break;
                }
            }

            AuroraFinal.Iniciar(jogador, musica, sons);
        }

        [MenuItem(Menu, true)]
        private static bool ValidarTestar()
        {
            return Application.isPlaying && !AuroraFinal.Ativa;
        }
    }
}
