using UnityEngine;
using UnityEngine.EventSystems;

namespace Vertigo.UI
{
    public interface IMenuVertical
    {
        void Selecionar(int indice);
        void Confirmar(int indice);
    }

    // Item de menu clicável: passar o mouse seleciona, clicar confirma.
    public sealed class MenuItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] private MonoBehaviour menu;
        [SerializeField] private int indice;

        private IMenuVertical Menu => menu as IMenuVertical;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Menu?.Selecionar(indice);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Menu?.Confirmar(indice);
            }
        }
    }
}
