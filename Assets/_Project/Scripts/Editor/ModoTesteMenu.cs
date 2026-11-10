using UnityEditor;
using Vertigo.Gameplay;

namespace Vertigo.EditorTools
{
    public static class ModoTesteMenu
    {
        private const string MenuAtivo = "Vertigo/Modo teste/Ativar (sem morte por frio)";
        private const string MenuPular = "Vertigo/Modo teste/Pular menu inicial";

        [MenuItem(MenuAtivo)]
        private static void Alternar()
        {
            ModoTeste.Ativo = !ModoTeste.Ativo;
            PlayPelaTelaInicial.Aplicar();
        }

        [MenuItem(MenuAtivo, true)]
        private static bool ValidarAtivo()
        {
            Menu.SetChecked(MenuAtivo, ModoTeste.Ativo);
            return true;
        }

        [MenuItem(MenuPular)]
        private static void AlternarPular()
        {
            ModoTeste.PularMenuInicial = !ModoTeste.PularMenuInicial;
            PlayPelaTelaInicial.Aplicar();
        }

        [MenuItem(MenuPular, true)]
        private static bool ValidarPular()
        {
            Menu.SetChecked(MenuPular, ModoTeste.PularMenuInicial);
            return true;
        }
    }
}
