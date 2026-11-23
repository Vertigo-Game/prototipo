using UnityEngine;

namespace Vertigo.Gameplay
{
    // Efeitos da fogueira. Ao ser acesa: uma leva de faíscas espirradas em direções aleatórias (para cima e
    // para os lados, caindo com a gravidade) e o som do fogo pegando. Enquanto acesa: brasas soltas subindo
    // sem parar. A Fogueira adiciona este componente sozinha se ele não estiver no objeto; coloque-o no prefab
    // só para ajustar os valores.
    public sealed class EfeitosFogueira : MonoBehaviour
    {
        [Tooltip("De onde as faíscas e brasas saem, relativo à fogueira, em unidades do mundo.")]
        [SerializeField] private Vector2 origem = new Vector2(0f, 0.1f);
        [Tooltip("Tamanho em unidades do mundo (1 pixel da fogueira = 0,07).")]
        [SerializeField] private Vector2 tamanho = new Vector2(0.07f, 0.14f);
        [SerializeField] private Color corQuente = new Color(1f, 0.92f, 0.45f, 1f);
        [SerializeField] private Color corBrasa = new Color(1f, 0.42f, 0.08f, 1f);
        [Tooltip("Cor no fim da vida, já fria, antes de sumir (cinza de cinza).")]
        [SerializeField] private Color corFinal = new Color(0.45f, 0.45f, 0.47f, 1f);

        [Header("Faíscas ao acender")]
        [SerializeField, Min(0)] private int quantidade = 14;
        [Tooltip("Faíscas extras logo depois da primeira leva, enquanto o fogo pega.")]
        [SerializeField, Min(0)] private int quantidadeExtra = 4;
        [Tooltip("Velocidade lateral máxima (para cada lado).")]
        [SerializeField, Min(0f)] private float velocidadeLateral = 1.5f;
        [Tooltip("Velocidade para cima: mínima e máxima.")]
        [SerializeField] private Vector2 velocidadeSubida = new Vector2(0.8f, 3f);
        [SerializeField] private Vector2 duracao = new Vector2(0.4f, 0.9f);
        [SerializeField, Min(0f)] private float gravidade = 0.55f;

        [Header("Brasas enquanto acesa")]
        [SerializeField, Min(0f)] private float brasasPorSegundo = 3f;
        [Tooltip("Velocidade lateral máxima (para cada lado).")]
        [SerializeField, Min(0f)] private float brasasVelocidadeLateral = 0.6f;
        [Tooltip("Velocidade para cima: mínima e máxima.")]
        [SerializeField] private Vector2 brasasVelocidadeSubida = new Vector2(0.7f, 1.9f);
        [SerializeField] private Vector2 brasasDuracao = new Vector2(0.8f, 1.6f);
        [Tooltip("Baixa: as brasas sobem com o ar quente e só desaceleram um pouco.")]
        [SerializeField, Min(0f)] private float brasasGravidade = 0.05f;

        [Header("Som")]
        [Tooltip("Som do fogo acendendo. Vazio: usa um gerado por código (chiado do fogo pegando + estalos).")]
        [SerializeField] private AudioClip somAcender;
        [SerializeField, Range(0f, 1f)] private float volume = 0.45f;

        private static AudioClip somGerado;
        private static Material materialFaiscas;

        private ParticleSystem faiscas;
        private ParticleSystem brasas;
        private AudioSource fonte;

        // Acendida pelo jogador: faíscas, som e, dali em diante, as brasas.
        public void Acender()
        {
            if (faiscas == null)
            {
                faiscas = CriarSistema("Faiscas", false, velocidadeLateral, velocidadeSubida, duracao, gravidade, 0.6f);
                ParticleSystem.EmissionModule emissao = faiscas.emission;
                emissao.rateOverTime = 0f;
                emissao.SetBursts(new[]
                {
                    new ParticleSystem.Burst(0f, (short)quantidade),
                    new ParticleSystem.Burst(0.18f, (short)quantidadeExtra)
                });
                ParticleSystem.MainModule principal = faiscas.main;
                principal.maxParticles = Mathf.Max(1, quantidade + quantidadeExtra);
            }

            faiscas.Play();
            ManterBrasas(false);

            if (fonte == null)
            {
                fonte = gameObject.AddComponent<AudioSource>();
                fonte.playOnAwake = false;
                fonte.spatialBlend = 0f;
            }

            if (somAcender == null && somGerado == null)
            {
                somGerado = CriarSomAcender();
            }

            fonte.PlayOneShot(somAcender != null ? somAcender : somGerado, volume);
        }

        // Fogueira que já começa acesa: só as brasas, já em andamento (sem a leva de faíscas nem o som).
        public void ComecarAcesa()
        {
            ManterBrasas(true);
        }

        private void ManterBrasas(bool jaEmAndamento)
        {
            if (brasas == null)
            {
                brasas = CriarSistema("Brasas", true, brasasVelocidadeLateral, brasasVelocidadeSubida, brasasDuracao, brasasGravidade, 0.8f);
                ParticleSystem.EmissionModule emissao = brasas.emission;
                emissao.rateOverTime = brasasPorSegundo;
                ParticleSystem.MainModule principal = brasas.main;
                principal.maxParticles = Mathf.Max(1, Mathf.CeilToInt(brasasPorSegundo * brasasDuracao.y) + 2);
            }

            if (brasas.isPlaying)
            {
                return;
            }

            // Pré-aquecida, a fogueira já aparece com brasas no ar em vez de começar do zero.
            ParticleSystem.MainModule main = brasas.main;
            main.prewarm = jaEmAndamento;
            brasas.Play();
        }

        // Faíscas e brasas: quadradinhos que nascem amarelos/laranja, recebem uma velocidade sorteada
        // (para os lados e para cima), tremem um pouco e esfriam até sumir.
        private ParticleSystem CriarSistema(string nome, bool continuo, float lateral, Vector2 subida,
            Vector2 vida, float gravidadeSistema, float tremida)
        {
            GameObject objeto = new GameObject(nome);
            objeto.transform.SetParent(transform, false);
            objeto.transform.position = transform.position + (Vector3)origem;
            ParticleSystem sistema = objeto.AddComponent<ParticleSystem>();
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule principal = sistema.main;
            principal.duration = 1f;
            principal.loop = continuo;
            principal.playOnAwake = false;
            principal.startSpeed = 0f;
            principal.startLifetime = new ParticleSystem.MinMaxCurve(vida.x, vida.y);
            principal.startSize = new ParticleSystem.MinMaxCurve(tamanho.x, tamanho.y);
            // As cores ficam todas na cor ao longo da vida (que multiplica a inicial): com a inicial laranja,
            // o fim nunca chegaria num cinza neutro.
            principal.startColor = Color.white;
            principal.gravityModifier = gravidadeSistema;
            principal.simulationSpace = ParticleSystemSimulationSpace.World;
            // A fogueira tem escala 7: as partículas usam só a própria escala para o tamanho valer em unidades do mundo.
            principal.scalingMode = ParticleSystemScalingMode.Local;

            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.enabled = false;

            // Direção sorteada por partícula: qualquer ângulo para os lados e para cima, sem sair do plano 2D.
            ParticleSystem.VelocityOverLifetimeModule velocidade = sistema.velocityOverLifetime;
            velocidade.enabled = true;
            velocidade.space = ParticleSystemSimulationSpace.World;
            velocidade.x = new ParticleSystem.MinMaxCurve(-lateral, lateral);
            velocidade.y = new ParticleSystem.MinMaxCurve(subida.x, subida.y);
            velocidade.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            // Tremida, como brasa levada pelo ar quente.
            ParticleSystem.NoiseModule ruido = sistema.noise;
            ruido.enabled = true;
            ruido.strength = tremida;
            ruido.frequency = 1.5f;
            ruido.scrollSpeed = 0.5f;

            ParticleSystem.SizeOverLifetimeModule tamanhoNaVida = sistema.sizeOverLifetime;
            tamanhoNaVida.enabled = true;
            tamanhoNaVida.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            // Esfria enquanto voa: nasce amarela ou laranja (sorteado entre as duas), avermelha, vira cinza e some.
            ParticleSystem.ColorOverLifetimeModule corNaVida = sistema.colorOverLifetime;
            corNaVida.enabled = true;
            corNaVida.color = new ParticleSystem.MinMaxGradient(Esfriar(corQuente, corBrasa), Esfriar(corBrasa, new Color(1f, 0.3f, 0.15f)));

            ParticleSystemRenderer renderizador = objeto.GetComponent<ParticleSystemRenderer>();
            if (materialFaiscas == null)
            {
                materialFaiscas = new Material(Shader.Find("Sprites/Default"));
            }

            renderizador.sharedMaterial = materialFaiscas;
            renderizador.renderMode = ParticleSystemRenderMode.Billboard;
            if (TryGetComponent(out SpriteRenderer sprite))
            {
                renderizador.sortingLayerID = sprite.sortingLayerID;
                renderizador.sortingOrder = sprite.sortingOrder + 1;
            }

            return sistema;
        }

        private Gradient Esfriar(Color inicio, Color meio)
        {
            Gradient gradiente = new Gradient();
            gradiente.SetKeys(
                new[] { new GradientColorKey(inicio, 0f), new GradientColorKey(meio, 0.4f), new GradientColorKey(corFinal, 0.8f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            return gradiente;
        }

        // Fogo pegando: um chiado de ar (ruído que abre e fecha) com estalos de lenha por cima, 1,1 s.
        // Semente fixa: o som é sempre o mesmo.
        private static AudioClip CriarSomAcender()
        {
            const int taxa = 44100;
            const float total = 1.1f;
            int amostras = Mathf.RoundToInt(taxa * total);
            float[] dados = new float[amostras];
            System.Random sorteio = new System.Random(7);

            // Chiado: ruído passando por um passa-baixa cujo corte sobe rápido (o fogo pegando) e depois cai.
            float grave = 0f;
            float agudo = 0f;
            for (int i = 0; i < amostras; i++)
            {
                float t = i / (float)taxa;
                float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
                float corte = t < 0.12f ? Mathf.Lerp(300f, 2600f, t / 0.12f) : Mathf.Lerp(2600f, 700f, Mathf.Clamp01((t - 0.12f) / 0.7f));
                agudo += (1f - Mathf.Exp(-2f * Mathf.PI * corte / taxa)) * (ruido - agudo);
                grave += (1f - Mathf.Exp(-2f * Mathf.PI * 160f / taxa)) * (ruido - grave);
                float envelope = t < 0.06f ? Mathf.SmoothStep(0f, 1f, t / 0.06f) : Mathf.Exp(-3.2f * (t - 0.06f));
                dados[i] = (agudo * 0.9f + grave * 1.6f) * envelope;
            }

            // Estalos: cliques curtos e secos, mais frequentes no começo.
            for (int n = 0; n < 34; n++)
            {
                float u = (float)sorteio.NextDouble();
                float inicio = 0.03f + u * u * (total - 0.15f);
                float forca = Mathf.Lerp(0.35f, 1f, (float)sorteio.NextDouble()) * Mathf.Lerp(1f, 0.4f, inicio / total);
                float decaimento = Mathf.Lerp(0.0008f, 0.0025f, (float)sorteio.NextDouble());
                int primeiro = Mathf.RoundToInt(inicio * taxa);
                int fim = Mathf.Min(amostras, primeiro + Mathf.RoundToInt(decaimento * 8f * taxa));
                float anterior = 0f;
                for (int i = primeiro; i < fim; i++)
                {
                    float t = (i - primeiro) / (float)taxa;
                    float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
                    // Diferença entre amostras vizinhas: tira o grave e deixa o estalo seco.
                    dados[i] += (ruido - anterior) * 0.5f * forca * Mathf.Exp(-t / decaimento);
                    anterior = ruido;
                }
            }

            float maximo = 0.0001f;
            for (int i = 0; i < amostras; i++)
            {
                maximo = Mathf.Max(maximo, Mathf.Abs(dados[i]));
            }

            int fade = Mathf.RoundToInt(0.05f * taxa);
            for (int i = 0; i < amostras; i++)
            {
                float saida = Mathf.Min(1f, (amostras - 1 - i) / (float)fade);
                dados[i] = dados[i] / maximo * 0.8f * saida;
            }

            AudioClip clipe = AudioClip.Create("FogueiraAcendendo", amostras, 1, taxa, false);
            clipe.SetData(dados, 0);
            return clipe;
        }
    }
}
