using HookPrototype;
using UnityEngine;

namespace Vertigo.Gameplay
{
    // Estados do requisito: MOVING, STOPPED e HOOKED.
    public enum EstadoInimigo
    {
        Movendo,
        Parado,
        Fisgado
    }

    // Inimigo que patrulha uma rota de segmentos retos. A conta do caminho fica em RotaPatrulha;
    // aqui ficam só os estados e a reação ao hook. Ser capturado continua com o PlayerController
    // (o jogador quica e o inimigo é destruído).
    [RequireComponent(typeof(Rigidbody2D), typeof(HookTarget))]
    public sealed class InimigoMovel : MonoBehaviour
    {
        [Header("Rota (pontos relativos à posição do inimigo na cena)")]
        [SerializeField] private Vector2[] pontos = { new Vector2(-2f, 0f), new Vector2(2f, 0f) };
        [SerializeField, Tooltip("Desligado: vai e volta. Ligado: do último ponto segue para o primeiro.")]
        private bool emLoop;

        [Header("Movimento")]
        [SerializeField, Min(0f)] private float velocidade = 2f;
        [SerializeField, Range(0f, 1f), Tooltip("Onde ele começa na rota: 0 = primeiro ponto, 1 = fim.")]
        private float progressoInicial;
        [SerializeField] private bool comecaVoltando;
        [SerializeField, Min(0f), Tooltip("Segundos parado antes de inverter o sentido (só na ida e volta).")]
        private float pausaNosExtremos;

        private const int MaxReflexoesPorPasso = 4;

        private Rigidbody2D corpo;
        private HookTarget alvo;
        private RotaPatrulha rota;
        private Vector2 origem;
        private EstadoInimigo estadoAntesDoHook;
        private float tempoParado;

        public EstadoInimigo Estado { get; private set; }
        public int Sentido => rota.Sentido;
        public Vector2 PosicaoNaRota => rota.Posicao;

        public float Velocidade
        {
            get => velocidade;
            set => velocidade = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            corpo = GetComponent<Rigidbody2D>();
            corpo.bodyType = RigidbodyType2D.Kinematic;
            corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            alvo = GetComponent<HookTarget>();

            if (pontos == null || pontos.Length < 2)
            {
                Debug.LogWarning($"{name}: a rota do inimigo móvel precisa de pelo menos dois pontos.", this);
                enabled = false;
                return;
            }

            origem = transform.position;
            Reiniciar();
        }

        // Volta para o início configurado da rota, andando. Também usado quando o inimigo reaparece.
        public void Reiniciar()
        {
            rota = new RotaPatrulha(PontosNoMundo(origem), emLoop, progressoInicial, comecaVoltando);
            transform.position = new Vector3(rota.Posicao.x, rota.Posicao.y, transform.position.z);
            corpo.position = rota.Posicao;
            tempoParado = 0f;
            Estado = EstadoInimigo.Movendo;
        }

        // Retângulo que contém todos os pontos da rota, no mundo.
        public Bounds AreaDaRota
        {
            get
            {
                Bounds area = new Bounds(origem + pontos[0], Vector3.zero);
                foreach (Vector2 ponto in pontos)
                {
                    area.Encapsulate(origem + ponto);
                }

                return area;
            }
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

        private void FixedUpdate()
        {
            switch (Estado)
            {
                case EstadoInimigo.Movendo:
                    Mover(velocidade * Time.fixedDeltaTime);
                    break;
                case EstadoInimigo.Parado:
                    tempoParado -= Time.fixedDeltaTime;
                    if (tempoParado <= 0f)
                    {
                        Estado = EstadoInimigo.Movendo;
                    }

                    break;
                // Fisgado: fica imóvel até o hook capturar ou soltar.
            }
        }

        // Para por alguns segundos e depois volta a andar sozinho. Ignorado enquanto fisgado.
        public void Parar(float segundos)
        {
            if (Estado == EstadoInimigo.Fisgado)
            {
                return;
            }

            tempoParado = segundos;
            Estado = EstadoInimigo.Parado;
        }

        private void Mover(float passo)
        {
            // Sem pausa, o que sobrar do passo depois de bater no extremo já é andado na volta.
            for (int i = 0; i < MaxReflexoesPorPasso && rota.Avancar(passo, out float sobra); i++)
            {
                if (pausaNosExtremos > 0f)
                {
                    Parar(pausaNosExtremos);
                    break;
                }

                passo = sobra;
            }

            corpo.MovePosition(rota.Posicao);
        }

        private void AoSerFisgado()
        {
            if (Estado == EstadoInimigo.Fisgado)
            {
                return;
            }

            estadoAntesDoHook = Estado;
            Estado = EstadoInimigo.Fisgado;
        }

        private void AoSerSolto()
        {
            if (Estado != EstadoInimigo.Fisgado)
            {
                return;
            }

            // A rota não andou enquanto fisgado: segue do mesmo ponto e no mesmo sentido.
            Estado = estadoAntesDoHook;
        }

        private Vector2[] PontosNoMundo(Vector2 referencia)
        {
            Vector2[] mundo = new Vector2[pontos.Length];
            for (int i = 0; i < pontos.Length; i++)
            {
                mundo[i] = referencia + pontos[i];
            }

            return mundo;
        }

        private void OnDrawGizmos()
        {
            if (pontos == null || pontos.Length < 2)
            {
                return;
            }

            Vector2[] mundo = PontosNoMundo(Application.isPlaying && rota != null ? origem : (Vector2)transform.position);
            Gizmos.color = new Color(1f, 0.35f, 0.55f, 0.8f);
            for (int i = 0; i < mundo.Length - 1; i++)
            {
                Gizmos.DrawLine(mundo[i], mundo[i + 1]);
            }

            if (emLoop)
            {
                Gizmos.DrawLine(mundo[mundo.Length - 1], mundo[0]);
            }

            foreach (Vector2 ponto in mundo)
            {
                Gizmos.DrawWireSphere(ponto, 0.12f);
            }

            if (!Application.isPlaying)
            {
                Vector2 inicio = new RotaPatrulha(mundo, emLoop, progressoInicial, comecaVoltando).Posicao;
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(inicio, 0.15f);
            }
        }
    }
}
