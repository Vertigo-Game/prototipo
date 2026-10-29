using UnityEditor;
using UnityEditor.SceneManagement;

namespace Vertigo.EditorTools
{
    // Faz o Play do editor começar pela primeira cena do build (a tela inicial), como no jogo exportado.
    // Desligue em Vertigo > Play começa pela tela inicial para testar uma fase direto.
    [InitializeOnLoad]
    public static class PlayPelaTelaInicial
    {
        private const string CaminhoMenu = "Vertigo/Play começa pela tela inicial";
        private const string Chave = "Vertigo.PlayPelaTelaInicial";

        static PlayPelaTelaInicial()
        {
            EditorApplication.delayCall += Aplicar;
            EditorApplication.playModeStateChanged += estado =>
            {
                if (estado == PlayModeStateChange.ExitingEditMode)
                {
                    Aplicar();
                }
            };
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

        private static void Aplicar()
        {
            bool ligado = EditorPrefs.GetBool(Chave, true);
            EditorBuildSettingsScene[] cenas = EditorBuildSettings.scenes;
            EditorSceneManager.playModeStartScene = ligado && cenas.Length > 0
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(cenas[0].path)
                : null;
        }
    }
}
