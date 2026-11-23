using HookPrototype;
using UnityEngine;

namespace Vertigo.Gameplay
{
    // Vento do dash: linhas finas horizontais (como as do vento das fases) que ficam para trás do Marcos
    // durante o dash, escorrendo no sentido contrário e sumindo rápido. Uma leva sai na hora do impulso,
    // junto com um sopro de ar baixinho, e mais linhas saem enquanto o dash durar.
    // O PlayerController adiciona este componente sozinho se ele não estiver no objeto; coloque-o no jogador
    // só para ajustar os valores.
    [RequireComponent(typeof(PlayerController))]
    public sealed class VentoDash : MonoBehaviour
    {
        [Tooltip("Linhas que saem de uma vez no começo do dash.")]
        [SerializeField, Min(0)] private int noImpulso = 4;
        [Tooltip("Linhas por segundo enquanto o dash durar.")]
        [SerializeField, Min(0f)] private float porSegundo = 35f;
        [Tooltip("Comprimento das linhas, em unidades do mundo: mínimo e máximo.")]
        [SerializeField] private Vector2 comprimento = new Vector2(0.3f, 0.7f);
        [Tooltip("Espessura das linhas (1 pixel = 0,0625).")]
        [SerializeField, Min(0.001f)] private float espessura = 0.0625f;
        [SerializeField] private Vector2 duracao = new Vector2(0.12f, 0.28f);
        [Tooltip("Velocidade das linhas para trás: mínima e máxima.")]
        [SerializeField] private Vector2 velocidadeParaTras = new Vector2(1f, 3.5f);
        [Tooltip("Quanto atrás do centro do Marcos as linhas nascem.")]
        [SerializeField] private float atras = 0.35f;
        [SerializeField] private Color cor = new Color(0.8f, 0.95f, 1f, 0.45f);

        [Header("Som")]
        [Tooltip("Som do dash. Vazio: usa um gerado por código (um sopro de ar curto e baixo).")]
        [SerializeField] private AudioClip som;
        [SerializeField, Range(0f, 1f)] private float volume = 0.35f;

        private static Material materialVento;
        private static AudioClip somGerado;

        private PlayerController jogador;
        private ParticleSystem vento;
        private ParticleSystem.EmissionModule emissao;
        private ParticleSystem.ShapeModule forma;
        private ParticleSystem.VelocityOverLifetimeModule velocidade;
        private AudioSource fonte;
        private bool emDash;

        private void Awake()
        {
            jogador = GetComponent<PlayerController>();
            vento = CriarVento();
            emissao = vento.emission;
            forma = vento.shape;
            velocidade = vento.velocityOverLifetime;
            emissao.enabled = false;
            vento.Play();

            fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f;
        }

        private void LateUpdate()
        {
            bool dash = jogador.IsDashing;
            float sentido = jogador.DashDirection;
            Bounds corpo = jogador.BodyBounds;

            // Linhas ao longo da altura do corpo, um pouco atrás dele.
            vento.transform.position = new Vector3(corpo.center.x - sentido * atras, corpo.center.y, transform.position.z);
            forma.scale = new Vector3(0.3f, corpo.size.y * 0.85f, 0f);

            if (dash && !emDash)
            {
                velocidade.x = new ParticleSystem.MinMaxCurve(-sentido * velocidadeParaTras.x, -sentido * velocidadeParaTras.y);
                vento.Emit(noImpulso);
                TocarSom();
            }

            emissao.enabled = dash;
            emDash = dash;
        }

        private void TocarSom()
        {
            if (som == null && somGerado == null)
            {
                somGerado = CriarSom();
            }

            // Tom levemente diferente a cada dash, para não soar repetido.
            fonte.pitch = Random.Range(0.92f, 1.08f);
            fonte.PlayOneShot(som != null ? som : somGerado, volume);
        }

        // Sopro de ar: ruído filtrado numa faixa que desce do agudo para o médio (o ar passando), 0,3 s,
        // com ataque curto e cauda suave. Semente fixa: o som é sempre o mesmo.
        private static AudioClip CriarSom()
        {
            const int taxa = 44100;
            const float total = 0.3f;
            int amostras = Mathf.RoundToInt(taxa * total);
            float[] dados = new float[amostras];
            System.Random sorteio = new System.Random(11);

            float passaBaixa = 0f;
            float grave = 0f;
            float maximo = 0.0001f;
            for (int i = 0; i < amostras; i++)
            {
                float t = i / (float)taxa;
                float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
                float progresso = t / total;
                // Faixa entre "corte de baixo" e "corte de cima": tira o grave e o chiado agudo demais.
                float corteCima = Mathf.Lerp(2400f, 700f, progresso);
                float corteBaixo = corteCima * 0.35f;
                passaBaixa += (1f - Mathf.Exp(-2f * Mathf.PI * corteCima / taxa)) * (ruido - passaBaixa);
                grave += (1f - Mathf.Exp(-2f * Mathf.PI * corteBaixo / taxa)) * (passaBaixa - grave);
                float faixa = passaBaixa - grave;
                float envelope = t < 0.02f ? Mathf.SmoothStep(0f, 1f, t / 0.02f) : Mathf.Pow(1f - (t - 0.02f) / (total - 0.02f), 2f);
                dados[i] = faixa * envelope;
                maximo = Mathf.Max(maximo, Mathf.Abs(dados[i]));
            }

            for (int i = 0; i < amostras; i++)
            {
                dados[i] = dados[i] / maximo * 0.8f;
            }

            AudioClip clipe = AudioClip.Create("VentoDash", amostras, 1, taxa, false);
            clipe.SetData(dados, 0);
            return clipe;
        }

        private ParticleSystem CriarVento()
        {
            GameObject objeto = new GameObject("VentoDash");
            objeto.transform.SetParent(transform, false);
            ParticleSystem sistema = objeto.AddComponent<ParticleSystem>();
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule principal = sistema.main;
            principal.duration = 1f;
            principal.loop = true;
            principal.playOnAwake = false;
            principal.startSpeed = 0f;
            principal.startLifetime = new ParticleSystem.MinMaxCurve(duracao.x, duracao.y);
            // Linha: comprida em x, fina em y.
            principal.startSize3D = true;
            principal.startSizeX = new ParticleSystem.MinMaxCurve(comprimento.x, comprimento.y);
            principal.startSizeY = espessura;
            principal.startSizeZ = 1f;
            principal.startColor = cor;
            principal.gravityModifier = 0f;
            principal.simulationSpace = ParticleSystemSimulationSpace.World;
            // O Marcos tem escala diferente em cada cena: as linhas usam só a própria escala.
            principal.scalingMode = ParticleSystemScalingMode.Local;
            principal.maxParticles = 64;

            ParticleSystem.EmissionModule emissaoSistema = sistema.emission;
            emissaoSistema.rateOverTime = porSegundo;

            ParticleSystem.ShapeModule formaSistema = sistema.shape;
            formaSistema.enabled = true;
            formaSistema.shapeType = ParticleSystemShapeType.Box;

            ParticleSystem.VelocityOverLifetimeModule velocidadeSistema = sistema.velocityOverLifetime;
            velocidadeSistema.enabled = true;
            velocidadeSistema.space = ParticleSystemSimulationSpace.World;
            velocidadeSistema.x = new ParticleSystem.MinMaxCurve(-velocidadeParaTras.x, -velocidadeParaTras.y);
            velocidadeSistema.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocidadeSistema.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            // Encolhe no comprimento enquanto some.
            ParticleSystem.SizeOverLifetimeModule tamanhoNaVida = sistema.sizeOverLifetime;
            tamanhoNaVida.enabled = true;
            tamanhoNaVida.separateAxes = true;
            tamanhoNaVida.x = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
            tamanhoNaVida.y = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 1f));
            tamanhoNaVida.z = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 1f));

            Gradient sumir = new Gradient();
            sumir.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule corNaVida = sistema.colorOverLifetime;
            corNaVida.enabled = true;
            corNaVida.color = sumir;

            ParticleSystemRenderer renderizador = objeto.GetComponent<ParticleSystemRenderer>();
            if (materialVento == null)
            {
                materialVento = new Material(Shader.Find("Sprites/Default"));
            }

            renderizador.sharedMaterial = materialVento;
            renderizador.renderMode = ParticleSystemRenderMode.Billboard;
            // Atrás do Marcos, na mesma camada dele.
            SpriteRenderer visual = GetComponentInChildren<SpriteRenderer>();
            if (visual != null)
            {
                renderizador.sortingLayerID = visual.sortingLayerID;
                renderizador.sortingOrder = visual.sortingOrder - 1;
            }

            return sistema;
        }
    }
}
