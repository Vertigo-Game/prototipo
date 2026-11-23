using HookPrototype;
using UnityEngine;

namespace Vertigo.Gameplay
{
    // Impacto do gancho, no ponto da superfície do alvo de onde o gancho veio:
    // - ponto fixo (metal): clarão curto e faíscas esticadas ricocheteando para o lado do Marcos, com um
    //   "clank" metálico seguido do "clac" da trava;
    // - inimigo (rocha com cristais): poucas faíscas, cacos de cristal na cor do inimigo e pedrinhas caindo,
    //   com um baque de pedra, o "crec" dos grãos e tinidos de cristal.
    // O PlayerController adiciona este componente sozinho se ele não estiver no objeto; coloque-o no jogador
    // só para ajustar os valores.
    [RequireComponent(typeof(PlayerController))]
    public sealed class ImpactoGancho : MonoBehaviour
    {
        [Header("Metal (pontos fixos)")]
        [SerializeField, Min(0)] private int quantidade = 10;
        [Tooltip("Abertura do leque de faíscas, em graus para cada lado (o centro aponta para o Marcos).")]
        [SerializeField, Range(0f, 180f)] private float abertura = 55f;
        [SerializeField] private Vector2 velocidade = new Vector2(2f, 5.5f);
        [SerializeField] private Vector2 duracao = new Vector2(0.12f, 0.32f);
        [Tooltip("Largura das faíscas, em unidades do mundo (1 pixel = 0,0625). O comprimento vem da velocidade.")]
        [SerializeField] private Vector2 tamanho = new Vector2(0.05f, 0.08f);
        [SerializeField, Min(0f)] private float gravidade = 1.2f;
        [Tooltip("Tamanho do clarão no ponto do impacto. 0 desliga.")]
        [SerializeField, Min(0f)] private float tamanhoClarao = 0.25f;

        [Header("Rocha com cristais (inimigos)")]
        [SerializeField, Min(0)] private int faiscasNaRocha = 4;
        [SerializeField, Min(0)] private int cacosDeCristal = 7;
        [SerializeField, Min(0)] private int pedrinhas = 5;
        [SerializeField] private Vector2 velocidadeCacos = new Vector2(1.5f, 4f);
        [SerializeField] private Vector2 duracaoCacos = new Vector2(0.25f, 0.5f);
        [Tooltip("Tamanho dos cacos e pedrinhas (1 pixel = 0,0625).")]
        [SerializeField] private Vector2 tamanhoCacos = new Vector2(0.0625f, 0.125f);
        [SerializeField, Min(0f)] private float gravidadeCacos = 2f;
        [Tooltip("Cor dos cacos quando o inimigo não tem cor própria (cristal azul original).")]
        [SerializeField] private Color corCristalPadrao = new Color(0.55f, 0.9f, 1f, 1f);
        [SerializeField] private Color corPedra = new Color(0.45f, 0.43f, 0.45f, 1f);

        [Header("Som")]
        [Tooltip("Sons do gancho prendendo em metal (um é sorteado). Vazio: clanks gerados por código.")]
        [SerializeField] private AudioClip[] sonsMetal;
        [Tooltip("Sons do gancho prendendo em rocha com cristais (um é sorteado). Vazio: gerados por código.")]
        [SerializeField] private AudioClip[] sonsCristal;
        [SerializeField, Range(0f, 1f)] private float volume = 0.18f;

        private const int OrdemDesenho = 25;
        private static readonly float[] TonsMetal = { 1150f, 1300f, 1450f };
        private static readonly float[] TonsCristal = { 2400f, 2700f, 3000f };
        private static AudioClip[] metalGerados;
        private static AudioClip[] cristalGerados;
        private static Material materialParticulas;

        private PlayerController jogador;
        private ParticleSystem faiscas;
        private ParticleSystem cacos;
        private AudioSource fonte;
        private int ultimoSom = -1;

        private void Awake()
        {
            jogador = GetComponent<PlayerController>();
            faiscas = CriarSistema("ImpactoGancho_Faiscas", gravidade, true);
            cacos = CriarSistema("ImpactoGancho_Cacos", gravidadeCacos, false);
            fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            jogador.HookAttached += AoPrender;
        }

        private void OnDisable()
        {
            jogador.HookAttached -= AoPrender;
        }

        private void AoPrender(HookTarget alvo)
        {
            Vector2 origem = jogador.BodyBounds.center;
            Vector2 ponto = PontoDeImpacto(alvo, origem);
            Vector2 paraOJogador = origem - ponto;
            float anguloBase = paraOJogador.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(paraOJogador.y, paraOJogador.x) * Mathf.Rad2Deg
                : 90f;

            if (alvo.IsEnemy)
            {
                Color corCristal = CorDoCristalDe(alvo);
                Clarao(ponto, tamanhoClarao * 0.7f, Color.Lerp(corCristal, Color.white, 0.6f));
                Faiscas(ponto, anguloBase, faiscasNaRocha);
                Cacos(ponto, anguloBase, cacosDeCristal, corCristal, Color.white);
                Cacos(ponto, anguloBase, pedrinhas, corPedra, Color.Lerp(corPedra, Color.black, 0.25f));
                TocarSom(sonsCristal, ref cristalGerados, TonsCristal, CriarCristal);
            }
            else
            {
                Clarao(ponto, tamanhoClarao, Color.white);
                Faiscas(ponto, anguloBase, quantidade);
                TocarSom(sonsMetal, ref metalGerados, TonsMetal, CriarClank);
            }
        }

        // O gancho bate na superfície do alvo do lado de onde veio, não no centro.
        private static Vector2 PontoDeImpacto(HookTarget alvo, Vector2 origem)
        {
            if (alvo.TryGetComponent(out Collider2D colisor) && colisor.enabled)
            {
                return colisor.ClosestPoint(origem);
            }

            return alvo.transform.position;
        }

        private Color CorDoCristalDe(HookTarget alvo)
        {
            CorDoCristal cristal = alvo.GetComponentInChildren<CorDoCristal>();
            return cristal != null && cristal.TentarCorVisivel(out Color cor) ? cor : corCristalPadrao;
        }

        private void Clarao(Vector2 ponto, float tamanhoDoClarao, Color cor)
        {
            if (tamanhoDoClarao <= 0f)
            {
                return;
            }

            // Um ponto grande e parado que some quase na hora.
            ParticleSystem.EmitParams parametros = new ParticleSystem.EmitParams
            {
                position = ponto,
                velocity = Vector3.zero,
                startSize = tamanhoDoClarao,
                startLifetime = 0.07f,
                startColor = cor
            };
            faiscas.Emit(parametros, 1);
        }

        private void Faiscas(Vector2 ponto, float anguloBase, int total)
        {
            ParticleSystem.EmitParams parametros = new ParticleSystem.EmitParams();
            for (int i = 0; i < total; i++)
            {
                parametros.position = ponto;
                parametros.velocity = Direcao(anguloBase, abertura) * Random.Range(velocidade.x, velocidade.y);
                parametros.startSize = Random.Range(tamanho.x, tamanho.y);
                parametros.startLifetime = Random.Range(duracao.x, duracao.y);
                parametros.startColor = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.55f), Random.value);
                faiscas.Emit(parametros, 1);
            }
        }

        // Cacos e pedrinhas: quadradinhos que saltam para o lado do Marcos, giram e caem.
        private void Cacos(Vector2 ponto, float anguloBase, int total, Color corA, Color corB)
        {
            ParticleSystem.EmitParams parametros = new ParticleSystem.EmitParams();
            for (int i = 0; i < total; i++)
            {
                parametros.position = ponto;
                parametros.velocity = Direcao(anguloBase, abertura + 15f) * Random.Range(velocidadeCacos.x, velocidadeCacos.y);
                parametros.startSize = Random.Range(tamanhoCacos.x, tamanhoCacos.y);
                parametros.startLifetime = Random.Range(duracaoCacos.x, duracaoCacos.y);
                parametros.startColor = Color.Lerp(corA, corB, Random.value * 0.5f);
                parametros.rotation = Random.Range(0f, 360f);
                parametros.angularVelocity = Random.Range(-540f, 540f);
                cacos.Emit(parametros, 1);
            }
        }

        private static Vector2 Direcao(float anguloBase, float aberturaGraus)
        {
            float angulo = (anguloBase + Random.Range(-aberturaGraus, aberturaGraus)) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
        }

        private void TocarSom(AudioClip[] proprios, ref AudioClip[] gerados, float[] tons,
            System.Func<float, int, AudioClip> criar)
        {
            AudioClip[] opcoes = proprios;
            if (opcoes == null || opcoes.Length == 0)
            {
                if (gerados == null || gerados[0] == null)
                {
                    gerados = new AudioClip[tons.Length];
                    for (int i = 0; i < tons.Length; i++)
                    {
                        gerados[i] = criar(tons[i], 17 + i * 29);
                    }
                }

                opcoes = gerados;
            }

            int escolhido = Random.Range(0, opcoes.Length);
            if (opcoes.Length > 1 && escolhido == ultimoSom)
            {
                escolhido = (escolhido + Random.Range(1, opcoes.Length)) % opcoes.Length;
            }

            ultimoSom = escolhido;
            if (opcoes[escolhido] == null)
            {
                return;
            }

            fonte.pitch = Random.Range(0.96f, 1.04f);
            fonte.PlayOneShot(opcoes[escolhido], volume);
        }

        // esticadas: faíscas viram risquinhos na direção do voo; senão, quadradinhos que giram (cacos).
        private ParticleSystem CriarSistema(string nome, float gravidadeSistema, bool esticadas)
        {
            GameObject objeto = new GameObject(nome);
            objeto.transform.SetParent(transform, false);
            ParticleSystem sistema = objeto.AddComponent<ParticleSystem>();
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule principal = sistema.main;
            principal.duration = 1f;
            principal.loop = true;
            principal.playOnAwake = false;
            principal.startSpeed = 0f;
            principal.gravityModifier = gravidadeSistema;
            principal.simulationSpace = ParticleSystemSimulationSpace.World;
            // O Marcos tem escala diferente em cada cena: as partículas usam só a própria escala.
            principal.scalingMode = ParticleSystemScalingMode.Local;
            principal.maxParticles = 128;

            // Tudo é emitido à mão (Emit), com posição e velocidade de cada partícula.
            ParticleSystem.EmissionModule emissao = sistema.emission;
            emissao.enabled = false;
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.enabled = false;

            ParticleSystem.SizeOverLifetimeModule tamanhoNaVida = sistema.sizeOverLifetime;
            tamanhoNaVida.enabled = true;
            tamanhoNaVida.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, esticadas ? 0.4f : 0.7f));

            Gradient sumir = new Gradient();
            if (esticadas)
            {
                // Metal quente: branco, amarela, alaranja e some.
                sumir.SetKeys(
                    new[]
                    {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(new Color(1f, 0.75f, 0.3f), 0.35f),
                        new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f)
                    },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            }
            else
            {
                // Cacos mantêm a cor e só somem no fim.
                sumir.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            }

            ParticleSystem.ColorOverLifetimeModule corNaVida = sistema.colorOverLifetime;
            corNaVida.enabled = true;
            corNaVida.color = sumir;

            ParticleSystemRenderer renderizador = objeto.GetComponent<ParticleSystemRenderer>();
            if (materialParticulas == null)
            {
                materialParticulas = new Material(Shader.Find("Sprites/Default"));
            }

            renderizador.sharedMaterial = materialParticulas;
            if (esticadas)
            {
                renderizador.renderMode = ParticleSystemRenderMode.Stretch;
                renderizador.velocityScale = 0.035f;
                renderizador.lengthScale = 1f;
            }
            else
            {
                renderizador.renderMode = ParticleSystemRenderMode.Billboard;
            }

            // Na frente do alvo e do cabo do gancho, na camada do Marcos.
            SpriteRenderer visual = GetComponentInChildren<SpriteRenderer>();
            if (visual != null)
            {
                renderizador.sortingLayerID = visual.sortingLayerID;
            }

            renderizador.sortingOrder = OrdemDesenho;
            sistema.Play();
            return sistema;
        }

        // Gancho de metal batendo e travando, 0,45 s:
        // - estalo seco do contato (ruído curtíssimo, só agudo);
        // - corpo do impacto: um "tunc" grave e curto, que dá peso;
        // - o metal vibrando: parciais inarmônicas (como uma peça de ferro batida), cada uma decaindo num tempo;
        // - 35 ms depois, o "clac" menor da trava se prendendo.
        private static AudioClip CriarClank(float tom, int semente)
        {
            const int taxa = 44100;
            const float total = 0.45f;
            const float atrasoTrava = 0.035f;
            int amostras = Mathf.RoundToInt(taxa * total);
            float[] dados = new float[amostras];
            System.Random sorteio = new System.Random(semente);

            float[] razoes = { 1f, 1.48f, 2.17f, 2.83f, 3.61f, 4.42f };
            float[] forcas = { 1f, 0.7f, 0.5f, 0.35f, 0.22f, 0.15f };
            float[] decaimentos = { 0.22f, 0.15f, 0.1f, 0.07f, 0.05f, 0.035f };

            float anterior = 0f;
            for (int i = 0; i < amostras; i++)
            {
                float t = i / (float)taxa;
                float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
                float agudo = ruido - anterior;
                anterior = ruido;

                float estalo = agudo * Mathf.Exp(-t / 0.0015f);
                float tunc = Mathf.Sin(2f * Mathf.PI * 210f * t) * Mathf.Exp(-t / 0.03f);

                float metal = 0f;
                for (int p = 0; p < razoes.Length; p++)
                {
                    metal += forcas[p] * Mathf.Sin(2f * Mathf.PI * tom * razoes[p] * t) * Mathf.Exp(-t / decaimentos[p]);
                }

                float ataque = Mathf.Clamp01(t / 0.0008f);
                float valor = estalo * 0.9f + tunc * 0.5f + metal * 0.32f * ataque;

                float tt = t - atrasoTrava;
                if (tt >= 0f)
                {
                    // Trava: estalo menor e um tinido mais agudo e curto.
                    valor += agudo * Mathf.Exp(-tt / 0.001f) * 0.45f
                           + Mathf.Sin(2f * Mathf.PI * tom * 2.6f * tt) * Mathf.Exp(-tt / 0.025f) * 0.18f;
                }

                dados[i] = valor;
            }

            return Finalizar(dados, taxa, "ImpactoGancho_Metal_" + Mathf.RoundToInt(tom));
        }

        // Gancho batendo em rocha com cristais, 0,4 s:
        // - baque de pedra: grave, seco e mais curto que o do metal;
        // - "crec": ruído granulado dos grãos de pedra estalando;
        // - cristal: notas agudas e vítreas que soam um pouco, mais tinidos soltos de cacos caindo;
        // - a trava do gancho, mais baixa que no metal.
        private static AudioClip CriarCristal(float tom, int semente)
        {
            const int taxa = 44100;
            const float total = 0.4f;
            const float atrasoTrava = 0.03f;
            int amostras = Mathf.RoundToInt(taxa * total);
            float[] dados = new float[amostras];
            System.Random sorteio = new System.Random(semente);
            float Sortear(float min, float max) => Mathf.Lerp(min, max, (float)sorteio.NextDouble());

            float[] razoes = { 1f, 2.32f, 4.25f, 6.63f };
            float[] forcas = { 1f, 0.55f, 0.3f, 0.15f };
            float[] decaimentos = { 0.16f, 0.09f, 0.05f, 0.03f };

            // Tinidos de cacos: 3 a 5 notas agudas curtinhas espalhadas depois do impacto.
            int quantosTinidos = 3 + sorteio.Next(3);
            float[] inicioTinido = new float[quantosTinidos];
            float[] tomTinido = new float[quantosTinidos];
            float[] forcaTinido = new float[quantosTinidos];
            for (int n = 0; n < quantosTinidos; n++)
            {
                inicioTinido[n] = Sortear(0.03f, 0.16f);
                tomTinido[n] = Sortear(3200f, 5200f);
                forcaTinido[n] = Sortear(0.12f, 0.3f);
            }

            float anterior = 0f;
            float passaBaixa = 0f;
            float grao = 0f;
            int proximoGrao = 0;
            for (int i = 0; i < amostras; i++)
            {
                float t = i / (float)taxa;
                float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
                float agudo = ruido - anterior;
                anterior = ruido;

                float baque = Mathf.Sin(2f * Mathf.PI * 160f * t) * Mathf.Exp(-t / 0.022f);

                passaBaixa += (1f - Mathf.Exp(-2f * Mathf.PI * 3000f / taxa)) * (ruido - passaBaixa);
                if (i >= proximoGrao)
                {
                    grao = sorteio.NextDouble() < 0.6 ? Sortear(0.5f, 1f) : 0f;
                    proximoGrao = i + Mathf.RoundToInt(taxa * Sortear(0.0015f, 0.004f));
                }

                float crec = passaBaixa * grao * Mathf.Exp(-t / 0.035f);

                float cristal = 0f;
                for (int p = 0; p < razoes.Length; p++)
                {
                    cristal += forcas[p] * Mathf.Sin(2f * Mathf.PI * tom * razoes[p] * t) * Mathf.Exp(-t / decaimentos[p]);
                }

                for (int n = 0; n < quantosTinidos; n++)
                {
                    float tn = t - inicioTinido[n];
                    if (tn >= 0f)
                    {
                        cristal += forcaTinido[n] * Mathf.Sin(2f * Mathf.PI * tomTinido[n] * tn) * Mathf.Exp(-tn / 0.02f);
                    }
                }

                float ataque = Mathf.Clamp01(t / 0.001f);
                float valor = agudo * Mathf.Exp(-t / 0.001f) * 0.6f + baque * 0.7f + crec * 1.1f + cristal * 0.28f * ataque;

                float tt = t - atrasoTrava;
                if (tt >= 0f)
                {
                    valor += agudo * Mathf.Exp(-tt / 0.001f) * 0.25f;
                }

                dados[i] = valor;
            }

            return Finalizar(dados, taxa, "ImpactoGancho_Cristal_" + Mathf.RoundToInt(tom));
        }

        // Normaliza, faz um fade curtinho no fim (para não estalar) e vira AudioClip.
        private static AudioClip Finalizar(float[] dados, int taxa, string nome)
        {
            float maximo = 0.0001f;
            for (int i = 0; i < dados.Length; i++)
            {
                maximo = Mathf.Max(maximo, Mathf.Abs(dados[i]));
            }

            int fade = Mathf.RoundToInt(0.02f * taxa);
            for (int i = 0; i < dados.Length; i++)
            {
                float saida = Mathf.Min(1f, (dados.Length - 1 - i) / (float)fade);
                dados[i] = dados[i] / maximo * 0.8f * saida;
            }

            AudioClip clipe = AudioClip.Create(nome, dados.Length, 1, taxa, false);
            clipe.SetData(dados, 0);
            return clipe;
        }
    }
}
