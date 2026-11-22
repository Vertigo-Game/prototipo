using TMPro;
using Vertigo.Gameplay;

namespace Vertigo.UI
{
    internal sealed class RemapeamentoControles
    {
        private readonly TMP_Text[] itens;
        private readonly TMP_Text mensagem;
        private int acao = -1;

        public RemapeamentoControles(TMP_Text[] itens, TMP_Text mensagem)
        {
            this.itens = itens;
            this.mensagem = mensagem;
        }

        public bool AguardandoTecla => acao >= 0;

        public void Abrir()
        {
            acao = -1;
            AtualizarTextos("Selecione uma ação para alterar.");
        }

        public void Cancelar()
        {
            acao = -1;
            AtualizarTextos("Alteração cancelada.");
        }

        public void LerEntrada()
        {
            string caminho = ControlesJogo.LerNovaTecla();
            if (caminho == null) return;

            if (ControlesJogo.Remapear(acao, caminho, out string resultado)) acao = -1;
            AtualizarTextos(resultado);
        }

        // A última opção volta ao menu que abriu o painel.
        public bool Confirmar(int indice)
        {
            if (indice < ControlesJogo.Quantidade)
            {
                acao = indice;
                AtualizarTextos("Pressione uma tecla ou botão do mouse. Esc cancela.");
                return false;
            }
            if (indice == ControlesJogo.Quantidade)
            {
                ControlesJogo.Restaurar();
                AtualizarTextos("Controles padrão restaurados.");
                return false;
            }
            return true;
        }

        private void AtualizarTextos(string texto)
        {
            for (int i = 0; i < ControlesJogo.Quantidade; i++)
            {
                string tecla = i == acao ? "[ pressione uma tecla ]" : ControlesJogo.Tecla(i).Replace('|', '/');
                itens[i].text = ControlesJogo.Nome(i) + "<pos=240><color=#8F9CB5>" + tecla + "</color>";
            }
            mensagem.text = texto;
        }
    }
}
