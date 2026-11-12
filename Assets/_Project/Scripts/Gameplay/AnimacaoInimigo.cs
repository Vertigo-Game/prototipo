using HookPrototype;
using UnityEngine;

namespace Vertigo.Gameplay
{
    // Repassa o hook para o Animator: preso, o inimigo recolhe os espinhos e fica assim até ser solto,
    // quando os espinhos voltam (mesma animação ao contrário). Capturado, ele se quebra e fica a carcaça
    // (último quadro da morte) até reaparecer. As transições ficam no Animator Controller.
    [RequireComponent(typeof(HookTarget), typeof(Animator))]
    public sealed class AnimacaoInimigo : MonoBehaviour
    {
        private static readonly int FisgadoHash = Animator.StringToHash("Fisgado");
        private static readonly int MortoHash = Animator.StringToHash("Morto");

        private HookTarget alvo;
        private Animator animador;

        private void Awake()
        {
            alvo = GetComponent<HookTarget>();
            animador = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            alvo.Hooked += AoSerFisgado;
            alvo.Released += AoSerSolto;
        }

        private void OnDisable()
        {
            alvo.Hooked -= AoSerFisgado;
            alvo.Released -= AoSerSolto;
        }

        // Chamado pelo RenascimentoInimigo na captura. Ao reaparecer, ele reinicia o Animator de volta ao idle.
        public void Morrer()
        {
            animador.SetTrigger(MortoHash);
        }

        private void AoSerFisgado()
        {
            animador.SetBool(FisgadoHash, true);
        }

        private void AoSerSolto()
        {
            animador.SetBool(FisgadoHash, false);
        }
    }
}
