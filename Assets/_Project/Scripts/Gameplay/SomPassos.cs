using HookPrototype;
using UnityEngine;

namespace Vertigo.Gameplay
{
    // Passos baixinhos enquanto o Marcos corre no chão: botas pisando na neve, um passo a cada "passada"
    // de distância percorrida (o ritmo acompanha a velocidade). Para não soar mecânico, cada passo sorteia
    // uma das variações (sem repetir a anterior), o tom, o volume e um pouco do espaçamento.
    // O PlayerController adiciona este componente sozinho se ele não estiver no objeto; coloque-o no jogador
    // só para ajustar os valores.
    [RequireComponent(typeof(PlayerController))]
    public sealed class SomPassos : MonoBehaviour
    {
        [Tooltip("Sons de passo (um é sorteado a cada passo). Vazio: usa passos na neve gerados por código.")]
        [SerializeField] private AudioClip[] sons;
        [SerializeField, Range(0f, 1f)] private float volume = 0.12f;
        [Tooltip("Distância entre dois passos, em unidades do mundo. 2,7 casa com a animação de corrida.")]
        [SerializeField, Min(0.1f)] private float passada = 2.7f;
        [Tooltip("Variação aleatória da passada (0,15 = até 15% mais curta ou mais longa).")]
        [SerializeField, Range(0f, 0.5f)] private float variacaoPassada = 0.15f;
        [SerializeField] private Vector2 variacaoTom = new Vector2(0.85f, 1.1f);
        [SerializeField] private Vector2 variacaoVolume = new Vector2(0.65f, 1f);

        private const int VariacoesGeradas = 6;
        private static AudioClip[] sonsGerados;

        private PlayerController jogador;
        private AudioSource fonte;
        private float ultimoX;
        private float andado;
        private float proximoPasso;
        private int ultimoSom = -1;
        private bool correndo;

        private void Awake()
        {
            jogador = GetComponent<PlayerController>();
            fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f;
            ultimoX = transform.position.x;
        }

        private void Update()
        {
            float x = transform.position.x;
            float deslocamento = Mathf.Abs(x - ultimoX);
            ultimoX = x;

            if (!jogador.IsRunningOnGround)
            {
                correndo = false;
                return;
            }

            if (!correndo)
            {
                // Começou a correr: o primeiro passo vem logo, não depois de uma passada inteira.
                correndo = true;
                andado = 0f;
                proximoPasso = passada * 0.35f;
            }

            andado += deslocamento;
            if (andado < proximoPasso)
            {
                return;
            }

            andado = 0f;
            proximoPasso = passada * Random.Range(1f - variacaoPassada, 1f + variacaoPassada);
            Pisar();
        }

        private void Pisar()
        {
            AudioClip[] opcoes = sons != null && sons.Length > 0 ? sons : SonsGerados();
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

            fonte.pitch = Random.Range(variacaoTom.x, variacaoTom.y);
            fonte.PlayOneShot(opcoes[escolhido], volume * Random.Range(variacaoVolume.x, variacaoVolume.y));
        }

        private static AudioClip[] SonsGerados()
        {
            if (sonsGerados == null || sonsGerados[0] == null)
            {
                sonsGerados = new AudioClip[VariacoesGeradas];
                for (int i = 0; i < VariacoesGeradas; i++)
                {
                    sonsGerados[i] = CriarPasso(101 + i * 37);
                }
            }

            return sonsGerados;
        }

        // Bota na neve: um baque grave e abafado (o peso) com o "cruc" granulado da neve compactando por cima,
        // cerca de 0,1 s. Cada semente muda os grãos, o tamanho e o equilíbrio entre baque e neve.
        private static AudioClip CriarPasso(int semente)
        {
            const int taxa = 44100;
            System.Random sorteio = new System.Random(semente);
            float Sortear(float min, float max) => Mathf.Lerp(min, max, (float)sorteio.NextDouble());

            float total = Sortear(0.08f, 0.12f);
            int amostras = Mathf.RoundToInt(taxa * total);
            float[] dados = new float[amostras];

            float frequenciaBaque = Sortear(70f, 100f);
            float pesoBaque = Sortear(0.5f, 0.8f);
            float cortaNeve = Sortear(1400f, 2400f);
            float duracaoNeve = Sortear(0.025f, 0.045f);

            float fase = 0f;
            float passaBaixa = 0f;
            float grave = 0f;
            float grao = 0f;
            int proximoGrao = 0;
            for (int i = 0; i < amostras; i++)
            {
                float t = i / (float)taxa;

                // Baque: senoide grave que cai um pouco de tom e some rápido.
                fase += 2f * Mathf.PI * frequenciaBaque * (1f - 0.3f * Mathf.Clamp01(t / 0.03f)) / taxa;
                float baque = Mathf.Sin(fase) * Mathf.Exp(-t / 0.018f);

                // Neve: ruído entre ~300 Hz e o corte, picotado em grãos curtos e irregulares.
                float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
                passaBaixa += (1f - Mathf.Exp(-2f * Mathf.PI * cortaNeve / taxa)) * (ruido - passaBaixa);
                grave += (1f - Mathf.Exp(-2f * Mathf.PI * 300f / taxa)) * (passaBaixa - grave);
                if (i >= proximoGrao)
                {
                    grao = (float)sorteio.NextDouble() < 0.7 ? Sortear(0.4f, 1f) : Sortear(0f, 0.15f);
                    proximoGrao = i + Mathf.RoundToInt(taxa * Sortear(0.002f, 0.006f));
                }

                float ataque = Mathf.Clamp01(t / 0.004f);
                float neve = (passaBaixa - grave) * grao * ataque * Mathf.Exp(-t / duracaoNeve);

                dados[i] = baque * pesoBaque + neve * 1.8f;
            }

            float maximo = 0.0001f;
            for (int i = 0; i < amostras; i++)
            {
                maximo = Mathf.Max(maximo, Mathf.Abs(dados[i]));
            }

            // Fade curtinho no fim para não estalar ao cortar.
            int fade = Mathf.RoundToInt(0.01f * taxa);
            for (int i = 0; i < amostras; i++)
            {
                float saida = Mathf.Min(1f, (amostras - 1 - i) / (float)fade);
                dados[i] = dados[i] / maximo * 0.7f * saida;
            }

            AudioClip clipe = AudioClip.Create("Passo_" + semente, amostras, 1, taxa, false);
            clipe.SetData(dados, 0);
            return clipe;
        }
    }
}
