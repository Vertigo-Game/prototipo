using TMPro;
using UnityEngine;
using Vertigo.Gameplay;

namespace Vertigo.UI
{
    // As dicas de interação acompanham a tecla escolhida no menu de controles.
    [RequireComponent(typeof(TMP_Text))]
    public sealed class AvisoControle : MonoBehaviour
    {
        [SerializeField] private AcaoJogo acao = AcaoJogo.Interagir;
        private TMP_Text texto;

        private void Awake() => texto = GetComponent<TMP_Text>();

        private void LateUpdate()
        {
            string tecla = ControlesJogo.Tecla((int)acao);
            if (texto.text != tecla) texto.text = tecla;
        }
    }
}
