using HookPrototype;
using UnityEngine;

namespace Vertigo.Gameplay
{
    // Som de morte do Marcos: o mesmo para qualquer morte (inimigo, espinho, queda ou frio), tocado uma vez.
    // O arquivo fica em Resources/Sons/morte.wav; troque-o para mudar o som de todas as mortes.
    // O PlayerController adiciona este componente sozinho se ele não estiver no objeto; coloque-o no jogador
    // só para ajustar os valores.
    [RequireComponent(typeof(PlayerController))]
    public sealed class SomMorte : MonoBehaviour
    {
        private const string CaminhoPadrao = "Sons/morte";

        [Tooltip("Som de morte. Vazio: usa Resources/Sons/morte.")]
        [SerializeField] private AudioClip som;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        private PlayerController jogador;
        private AudioSource fonte;

        private void Awake()
        {
            jogador = GetComponent<PlayerController>();
            fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            jogador.Died += AoMorrer;
        }

        private void OnDisable()
        {
            jogador.Died -= AoMorrer;
        }

        private void AoMorrer()
        {
            if (som == null)
            {
                som = Resources.Load<AudioClip>(CaminhoPadrao);
            }

            if (som != null)
            {
                fonte.PlayOneShot(som, volume);
            }
        }
    }
}
