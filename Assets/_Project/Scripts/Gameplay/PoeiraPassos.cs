using HookPrototype;
using UnityEngine;

namespace Vertigo.Gameplay
{
    // Poeira levantada pelos pés enquanto o Marcos corre no chão: nuvenzinhas que ficam para trás, sobem um
    // pouco e somem. Quanto mais rápido ele corre, mais poeira (a emissão é por distância percorrida).
    // O PlayerController adiciona este componente sozinho se ele não estiver no objeto; coloque-o no jogador
    // só para ajustar os valores.
    [RequireComponent(typeof(PlayerController))]
    public sealed class PoeiraPassos : MonoBehaviour
    {
        [Tooltip("Nuvenzinhas por unidade percorrida. Correndo (6,5 u/s) dá umas 12 por segundo.")]
        [SerializeField, Min(0f)] private float porDistancia = 1.0f;
        [Tooltip("Tamanho em unidades do mundo (1 pixel do Marcos = 0,0625).")]
        [SerializeField] private Vector2 tamanho = new Vector2(0.07f, 0.15f);
        [SerializeField] private Vector2 duracao = new Vector2(0.25f, 0.55f);
        [Tooltip("Quanto da velocidade do Marcos a poeira leva para trás ao nascer (mínimo e máximo).")]
        [SerializeField] private Vector2 empurraoParaTras = new Vector2(0.04f, 0.14f);
        [Tooltip("Velocidade para cima: mínima e máxima.")]
        [SerializeField] private Vector2 subida = new Vector2(0.1f, 0.6f);
        [Tooltip("Espalhamento para os lados (para cada lado).")]
        [SerializeField, Min(0f)] private float espalhamento = 0.25f;
        [SerializeField] private Color cor = new Color(0.86f, 0.88f, 0.92f, 0.75f);

        private static Material materialPoeira;

        private PlayerController jogador;
        private ParticleSystem poeira;
        private ParticleSystem.EmissionModule emissao;

        private void Awake()
        {
            jogador = GetComponent<PlayerController>();
            poeira = CriarPoeira();
            emissao = poeira.emission;
            emissao.enabled = false;
            poeira.Play();
        }

        private void LateUpdate()
        {
            // Pés: base do corpo, no centro.
            Bounds corpo = jogador.BodyBounds;
            poeira.transform.position = new Vector3(corpo.center.x, corpo.min.y + 0.03f, transform.position.z);
            emissao.enabled = jogador.IsRunningOnGround;
        }

        private ParticleSystem CriarPoeira()
        {
            GameObject objeto = new GameObject("PoeiraPassos");
            objeto.transform.SetParent(transform, false);
            ParticleSystem sistema = objeto.AddComponent<ParticleSystem>();
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule principal = sistema.main;
            principal.duration = 1f;
            principal.loop = true;
            principal.playOnAwake = false;
            principal.startSpeed = 0f;
            principal.startLifetime = new ParticleSystem.MinMaxCurve(duracao.x, duracao.y);
            principal.startSize = new ParticleSystem.MinMaxCurve(tamanho.x, tamanho.y);
            principal.startColor = cor;
            principal.gravityModifier = 0f;
            principal.simulationSpace = ParticleSystemSimulationSpace.World;
            // O Marcos tem escala diferente em cada cena: a poeira usa só a própria escala.
            principal.scalingMode = ParticleSystemScalingMode.Local;
            principal.maxParticles = 64;

            // Por distância, não por tempo: parado não sai nada, correndo mais rápido sai mais.
            ParticleSystem.EmissionModule emissaoSistema = sistema.emission;
            emissaoSistema.rateOverTime = 0f;
            emissaoSistema.rateOverDistance = porDistancia;

            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.enabled = false;

            // Fica para trás: herda uma fração negativa da velocidade do Marcos no momento em que nasce.
            ParticleSystem.InheritVelocityModule herdar = sistema.inheritVelocity;
            herdar.enabled = true;
            herdar.mode = ParticleSystemInheritVelocityMode.Initial;
            herdar.curve = new ParticleSystem.MinMaxCurve(-empurraoParaTras.y, -empurraoParaTras.x);

            ParticleSystem.VelocityOverLifetimeModule velocidade = sistema.velocityOverLifetime;
            velocidade.enabled = true;
            velocidade.space = ParticleSystemSimulationSpace.World;
            velocidade.x = new ParticleSystem.MinMaxCurve(-espalhamento, espalhamento);
            velocidade.y = new ParticleSystem.MinMaxCurve(subida.x, subida.y);
            velocidade.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            // Nuvenzinha: abre um pouco e some.
            ParticleSystem.SizeOverLifetimeModule tamanhoNaVida = sistema.sizeOverLifetime;
            tamanhoNaVida.enabled = true;
            tamanhoNaVida.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 1.3f));

            Gradient sumir = new Gradient();
            sumir.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule corNaVida = sistema.colorOverLifetime;
            corNaVida.enabled = true;
            corNaVida.color = sumir;

            ParticleSystemRenderer renderizador = objeto.GetComponent<ParticleSystemRenderer>();
            if (materialPoeira == null)
            {
                materialPoeira = new Material(Shader.Find("Sprites/Default"));
            }

            renderizador.sharedMaterial = materialPoeira;
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
