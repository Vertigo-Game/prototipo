using HookPrototype;
using UnityEngine;

namespace Vertigo.Gameplay
{
    // Barra de Frio: enche longe de fogueiras acesas e esvazia aos poucos perto delas.
    // Em 100% o Marcos congela e aparece o Game Over (R reinicia o loop).
    // Usa o tempo do jogo: enche mais devagar na câmera lenta e para na pausa/placa.
    [RequireComponent(typeof(PlayerController))]
    public sealed class BarraDeFrio : MonoBehaviour
    {
        [SerializeField, Tooltip("Área no mundo onde o frio começa. Se estiver vazia, o frio vale desde o início.")]
        private Rect areaDeInicio;

        [SerializeField, Tooltip("Segundos longe do fogo, de vazio até congelar.")]
        private float tempoParaCongelar = 40f;

        [SerializeField, Tooltip("Segundos perto do fogo para esvaziar a barra cheia.")]
        private float tempoParaAquecer = 8f;

        [SerializeField, Range(0f, 1f), Tooltip("Valor atual (só leitura, para ajuste no Play).")]
        private float frio;

        private PlayerController jogador;

        // 0 = quentinho, 1 = congelado.
        public float Frio => frio;
        public bool Ativo { get; private set; }
        public bool Aquecendo { get; private set; }
        internal bool TemAreaDeInicio => areaDeInicio.width > 0f && areaDeInicio.height > 0f;
        internal float SegundosDeFrioAcumulado => frio * tempoParaCongelar;

        private void Awake()
        {
            jogador = GetComponent<PlayerController>();
            frio = 0f;
            Ativo = !TemAreaDeInicio || Checkpoint.FrioAtivoNa(gameObject.scene);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || jogador.IsDead)
            {
                return;
            }

            Ativo = Ativo || areaDeInicio.Contains(transform.position);
            if (!Ativo)
            {
                return;
            }

            Aquecendo = Fogueira.AlgumaAquece(transform.position);

            if (Aquecendo)
            {
                frio = Mathf.Max(0f, frio - dt / tempoParaAquecer);
                return;
            }

            frio = Mathf.Min(1f, frio + dt / tempoParaCongelar);
            if (frio >= 1f && !ModoTeste.Ativo)
            {
                jogador.Die(CausaDaMorte.Congelamento);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (TemAreaDeInicio)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(areaDeInicio.center, areaDeInicio.size);
            }
        }
    }
}
