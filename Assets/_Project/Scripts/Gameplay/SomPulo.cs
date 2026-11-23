using HookPrototype;
using UnityEngine;

namespace Vertigo.Gameplay
{
    // Som do pulo: o casaco pesado farfalhando no ar, discreto. Toca sempre que o Marcos pula
    // (chão, coyote/buffer ou saindo da corda).
    // O PlayerController adiciona este componente sozinho se ele não estiver no objeto; coloque-o no jogador
    // só para ajustar os valores.
    [RequireComponent(typeof(PlayerController))]
    public sealed class SomPulo : MonoBehaviour
    {
        [Tooltip("Som do pulo. Vazio: usa um gerado por código (casaco pesado farfalhando no ar).")]
        [SerializeField] private AudioClip som;
        [SerializeField, Range(0f, 1f)] private float volume = 0.18f;

        private static AudioClip somGerado;

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
            jogador.Jumped += AoPular;
        }

        private void OnDisable()
        {
            jogador.Jumped -= AoPular;
        }

        private void AoPular()
        {
            if (som == null && somGerado == null)
            {
                somGerado = CriarSom();
            }

            // Tom levemente diferente a cada pulo, para não soar repetido (puxado para o grave: tecido pesado).
            fonte.pitch = Random.Range(0.9f, 1.02f);
            fonte.PlayOneShot(som != null ? som : somGerado, volume);
        }

        // Pulo: o casaco pesado se mexendo no ar. Um "fuuump" abafado de tecido grosso (ruído só nos médios-graves,
        // que incha e murcha), com textura irregular de pano roçando e um leve impulso grave dos pés por baixo.
        // 0,26 s no total. Semente fixa: o som é sempre o mesmo.
        private static AudioClip CriarSom()
        {
            const int taxa = 44100;
            const float total = 0.26f;
            int amostras = Mathf.RoundToInt(taxa * total);
            float[] dados = new float[amostras];
            System.Random sorteio = new System.Random(3);

            float fase = 0f;
            float passaBaixa = 0f;
            float grave = 0f;
            float textura = 1f;
            float texturaAlvo = 1f;
            int proximaTextura = 0;
            for (int i = 0; i < amostras; i++)
            {
                float t = i / (float)taxa;
                float progresso = t / total;

                // Tecido: ruído entre ~180 Hz e ~1,1 kHz (sem o chiado agudo de ar, que soa leve demais).
                // O corte de cima abre um pouco no meio do movimento e fecha de novo.
                float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
                float corteCima = Mathf.Lerp(850f, 1350f, Mathf.Sin(progresso * Mathf.PI));
                passaBaixa += (1f - Mathf.Exp(-2f * Mathf.PI * corteCima / taxa)) * (ruido - passaBaixa);
                grave += (1f - Mathf.Exp(-2f * Mathf.PI * 180f / taxa)) * (passaBaixa - grave);

                // Pano roçando: o volume do tecido muda a cada poucos milissegundos, de forma irregular.
                if (i >= proximaTextura)
                {
                    texturaAlvo = Mathf.Lerp(0.35f, 1f, (float)sorteio.NextDouble());
                    proximaTextura = i + Mathf.RoundToInt(taxa * Mathf.Lerp(0.006f, 0.014f, (float)sorteio.NextDouble()));
                }

                textura += (1f - Mathf.Exp(-1f / (taxa * 0.003f))) * (texturaAlvo - textura);

                // Incha devagar (o casaco sendo puxado) e murcha.
                float envelope = t < 0.045f
                    ? Mathf.SmoothStep(0f, 1f, t / 0.045f)
                    : Mathf.Pow(1f - (t - 0.045f) / (total - 0.045f), 1.6f);
                float tecido = (passaBaixa - grave) * textura * envelope;

                // Pés: senoide grave bem baixinha, de 110 para 80 Hz.
                float frequencia = Mathf.Lerp(110f, 80f, Mathf.Clamp01(t / 0.04f));
                fase += 2f * Mathf.PI * frequencia / taxa;
                float impulso = Mathf.Sin(fase) * Mathf.Exp(-t / 0.02f);

                dados[i] = tecido * 2.2f + impulso * 0.15f;
            }

            float maximo = 0.0001f;
            for (int i = 0; i < amostras; i++)
            {
                maximo = Mathf.Max(maximo, Mathf.Abs(dados[i]));
            }

            for (int i = 0; i < amostras; i++)
            {
                dados[i] = dados[i] / maximo * 0.7f;
            }

            AudioClip clipe = AudioClip.Create("Pulo", amostras, 1, taxa, false);
            clipe.SetData(dados, 0);
            return clipe;
        }
    }
}
