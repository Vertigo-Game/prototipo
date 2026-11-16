namespace Vertigo.Gameplay
{
    /// <summary>Preferências locais do editor, independentes do estado salvo nas cenas.</summary>
    public static class ModoTeste
    {
#if UNITY_EDITOR
        private static string Prefixo => "Vertigo.ModoTeste." + UnityEngine.Application.dataPath + ".";

        public static bool Ativo
        {
            get => UnityEditor.EditorPrefs.GetBool(Prefixo + "Ativo", false);
            set => UnityEditor.EditorPrefs.SetBool(Prefixo + "Ativo", value);
        }

        public static bool PularMenuInicial
        {
            get => UnityEditor.EditorPrefs.GetBool(Prefixo + "PularMenuInicial", true);
            set => UnityEditor.EditorPrefs.SetBool(Prefixo + "PularMenuInicial", value);
        }
#else
        public static bool Ativo => false;
#endif
    }
}
