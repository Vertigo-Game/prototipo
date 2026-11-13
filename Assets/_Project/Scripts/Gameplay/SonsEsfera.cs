using HookPrototype;
using UnityEngine;

namespace Vertigo.Gameplay
{
    // Sons da esfera de cristal: as pontas se recolhem quando o gancho prende nela e voltam a sair
    // quando o gancho solta (uma vez por mudança, esteja o Marcos preso ou não). Encostar nela com
    // as pontas para fora mata, e o som de morte toca uma vez nesse contato.
    [RequireComponent(typeof(HookTarget))]
    public sealed class SonsEsfera : MonoBehaviour
    {
        [SerializeField] private AudioClip pontasEntrando;
        [SerializeField] private AudioClip pontasSaindo;
        [SerializeField] private AudioClip morte;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        private HookTarget alvo;
        private AudioSource fonte;

        private void Awake()
        {
            alvo = GetComponent<HookTarget>();
            fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            alvo.Hooked += AoSerFisgada;
            alvo.Released += AoSerSolta;
            alvo.KilledPlayer += AoMatarJogador;
        }

        private void OnDisable()
        {
            alvo.Hooked -= AoSerFisgada;
            alvo.Released -= AoSerSolta;
            alvo.KilledPlayer -= AoMatarJogador;
        }

        private void AoSerFisgada()
        {
            Tocar(pontasEntrando);
        }

        private void AoSerSolta()
        {
            Tocar(pontasSaindo);
        }

        private void AoMatarJogador()
        {
            Tocar(morte);
        }

        private void Tocar(AudioClip clipe)
        {
            if (clipe != null)
            {
                fonte.PlayOneShot(clipe, volume);
            }
        }
    }
}
