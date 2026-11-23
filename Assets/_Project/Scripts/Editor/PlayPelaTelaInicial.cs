using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Vertigo.Gameplay;

namespace Vertigo.EditorTools
{
    // Faz o Play do editor começar pela primeira cena do build (a tela inicial), como no jogo exportado.
    // Desligue em Vertigo > Play começa pela tela inicial para testar uma fase direto.
    [InitializeOnLoad]
    public static class PlayPelaTelaInicial
    {
        private const string CaminhoMenu = "Vertigo/Play começa pela tela inicial";
        private const string Chave = "Vertigo.PlayPelaTelaInicial";
        private const string CaminhoFase1 = "Assets/_Project/Scenes/Fase1_Validacao.unity";
        private const string CaminhoGeada = "Assets/_Project/Scenes/PassagemDaGeada.unity";

        static PlayPelaTelaInicial()
        {
            EditorApplication.delayCall += Aplicar;
            EditorApplication.playModeStateChanged += AoMudarModoDeJogo;
        }

        private static void AoMudarModoDeJogo(PlayModeStateChange estado)
        {
            if (estado != PlayModeStateChange.ExitingEditMode) return;

            string cenaAtual = SceneManager.GetActiveScene().path;
            ModoTeste.CenaDoJogoNoEditor = cenaAtual == CaminhoFase1 || cenaAtual == CaminhoGeada
                ? Path.GetFileNameWithoutExtension(cenaAtual)
                : string.Empty;
            Aplicar();
        }

        [MenuItem(CaminhoMenu)]
        private static void Alternar()
        {
            EditorPrefs.SetBool(Chave, !EditorPrefs.GetBool(Chave, true));
            Aplicar();
        }

        [MenuItem(CaminhoMenu, true)]
        private static bool AlternarValidar()
        {
            Menu.SetChecked(CaminhoMenu, EditorPrefs.GetBool(Chave, true));
            return true;
        }

        internal static void Aplicar()
        {
            // Preferência do editor: continua valendo após R, Stop/Play e recarga de scripts.
            string cenaAtual = SceneManager.GetActiveScene().path;
            if (ModoTeste.Ativo && ModoTeste.PularMenuInicial)
            {
                bool estaNoMenu = cenaAtual == "Assets/_Project/Scenes/TelaInicial.unity";
                EditorSceneManager.playModeStartScene = estaNoMenu
                    ? AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Project/Scenes/Fase1.unity")
                    : null;
                return;
            }

            bool ligado = EditorPrefs.GetBool(Chave, true);
            EditorBuildSettingsScene[] cenas = EditorBuildSettings.scenes;
            EditorSceneManager.playModeStartScene = ligado && cenas.Length > 0
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(cenas[0].path)
                : null;
        }
    }
}
