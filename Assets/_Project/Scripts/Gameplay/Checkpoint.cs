using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vertigo.Gameplay
{
    // Último checkpoint aceso (uma fogueira marcada como checkpoint). Fica só na memória durante a sessão:
    // sobrevive ao recarregar a cena (morte e R) e é apagado ao começar pela tela inicial ou ao terminar o jogo.
    public static class Checkpoint
    {
        private static int cena = -1;
        private static string fogueira;

        public static bool Ativo { get; private set; }

        // Se o Marcos já tinha o gancho quando acendeu a fogueira: ao renascer ele volta com ele.
        public static bool ComGancho { get; private set; }

        public static void Salvar(Fogueira origem, bool comGancho)
        {
            Ativo = true;
            cena = origem.gameObject.scene.buildIndex;
            fogueira = origem.name;
            ComGancho = comGancho;
        }

        public static bool Eh(Fogueira candidata)
        {
            return Ativo && cena == candidata.gameObject.scene.buildIndex && fogueira == candidata.name;
        }

        public static bool GanchoGuardadoNa(Scene alvo)
        {
            return Ativo && ComGancho && cena == alvo.buildIndex;
        }

        // Sem domain reload ao entrar no Play, o estático ficaria da sessão anterior.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Limpar()
        {
            Ativo = false;
            cena = -1;
            fogueira = null;
            ComGancho = false;
        }
    }
}
