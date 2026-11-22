namespace Vertigo.Gameplay
{
    /// <summary>Preferências e estado da sessão do editor, independentes das cenas salvas.</summary>
    public static class ModoTeste
    {
#if UNITY_EDITOR
        private static string Prefixo => "Vertigo.ModoTeste." + UnityEngine.Application.dataPath + ".";

        public static string CenaDoJogoNoEditor
        {
            get => UnityEditor.SessionState.GetString(Prefixo + "CenaDoJogoNoEditor", string.Empty);
            set => UnityEditor.SessionState.SetString(Prefixo + "CenaDoJogoNoEditor", value);
        }

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

        // Libera o gancho sem coletar o item; desligado, vale a progressão normal.
        public static bool GanchoLiberado
        {
            get => UnityEditor.EditorPrefs.GetBool(Prefixo + "GanchoLiberado", false);
            set => UnityEditor.EditorPrefs.SetBool(Prefixo + "GanchoLiberado", value);
        }
#else
        public static bool Ativo => false;
        public static bool GanchoLiberado => false;
#endif
    }
}
