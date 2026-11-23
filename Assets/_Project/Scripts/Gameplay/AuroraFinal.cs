using System.Collections.Generic;
using HookPrototype;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vertigo.Gameplay
{
    // Final do jogo. Quando o Marcos se senta na fogueira final, uma aurora boreal surge em volta dele,
    // brilha, cresce até cobrir a tela inteira com as cores e então aparece a tela de finalização.
    //
    // O desenho imita cortinas de aurora: cada faixa tem uma barra brilhante embaixo, que ondula e faz curvas,
    // e raios que sobem dela e vão sumindo, passando de verde ou rosa na barra para azul e violeta no alto.
    // Tudo é feito de pixels do tamanho dos pixels da arte do jogo (colunas de 1 pixel empilhadas em degraus de
    // cor), sem nenhuma imagem: a arte final pode entrar depois no lugar.
    //
    // Não precisa estar na cena: a Fogueira final cria este objeto sozinha. Para ajustar tempos e cores,
    // coloque um objeto com este componente na cena (ele é usado no lugar do automático).
    public sealed class AuroraFinal : MonoBehaviour
    {
        private enum Fase
        {
            Parada,
            Espera,
            Envolver,
            Cobrir,
            Pausa,
            Concluida
        }

        [Header("Tempos (segundos)")]
        [SerializeField, Min(0f), Tooltip("Tempo entre o Marcos se sentar e a aurora começar a surgir.")]
        private float atrasoInicial = 1.2f;
        [SerializeField, Min(0.1f), Tooltip("A aurora surge em volta do Marcos e brilha.")]
        private float duracaoEnvolver = 3f;
        [SerializeField, Min(0.1f), Tooltip("A aurora cresce até cobrir a tela inteira com as cores.")]
        private float duracaoCobrir = 3.5f;
        [SerializeField, Min(0f), Tooltip("Tela cheia de cores antes de aparecer a tela de finalização.")]
        private float pausaFinal = 0.8f;

        [Header("Cores")]
        [SerializeField, Tooltip("As seis cores da aurora, nesta ordem: verde, ciano, azul, violeta, magenta e lilás. Cada faixa usa três delas.")]
        private Color[] paleta =
        {
            new Color32(0x3D, 0xF5, 0x8C, 255), // verde
            new Color32(0x38, 0xE6, 0xF5, 255), // ciano
            new Color32(0x3A, 0x5C, 0xF2, 255), // azul
            new Color32(0x8A, 0x3F, 0xF5, 255), // violeta
            new Color32(0xE8, 0x3F, 0xE0, 255), // magenta
            new Color32(0xC9, 0x8B, 0xFF, 255)  // lilás
        };
        [SerializeField, Tooltip("Céu no alto da tela quando a aurora a cobre.")]
        private Color ceuAlto = new Color32(0x07, 0x0B, 0x3A, 255);
        [SerializeField, Tooltip("Céu perto de baixo, mais claro, como o horizonte.")]
        private Color ceuBaixo = new Color32(0x1E, 0x3F, 0xA8, 255);
        [SerializeField, Range(0f, 1f), Tooltip("Opacidade máxima das faixas.")]
        private float opacidade = 0.9f;

        [Header("Brilhinhos e estrelas")]
        [SerializeField, Range(0, 200), Tooltip("Brilhinhos em cruz que cintilam na aurora. Zero desliga.")]
        private int brilhinhos = 80;
        [SerializeField, Range(0, 300), Tooltip("Estrelas pequenas que surgem no céu quando a tela é coberta. Zero desliga.")]
        private int estrelas = 140;
        [SerializeField, Min(0.05f), Tooltip("Quantas vezes por segundo, em média, cada brilhinho acende e apaga.")]
        private float velocidadeBrilhinhos = 0.7f;
        [SerializeField, Tooltip("Cor do centro de cada brilhinho e das estrelas.")]
        private Color corBrilhinho = new Color32(0xFF, 0xFF, 0xEE, 255);

        [Header("Música do final")]
        [SerializeField, Tooltip("Música que toca no final. Se ficar vazia, usa a da Fogueira final.")]
        private AudioClip musicaFinal;
        [SerializeField, Range(0f, 1f), Tooltip("Volume final da música do final.")]
        private float volumeMusica = 0.8f;
        [SerializeField, Min(0f), Tooltip("Segundos para a música que estava tocando sumir (fade out).")]
        private float fadeOutMusicaAnterior = 1.5f;
        [SerializeField, Min(0f), Tooltip("Segundos para a música do final aparecer (fade in).")]
        private float fadeInMusicaFinal = 2.5f;

        [Header("Sons dos brilhinhos")]
        [SerializeField, Tooltip("Sons curtos de brilho (um é sorteado a cada vez). Se ficar vazio, usa os da Fogueira final e, sem eles, sininhos gerados por código.")]
        private AudioClip[] sonsBrilhinhos;
        [SerializeField, Range(0f, 1f), Tooltip("Volume dos sons de brilho.")]
        private float volumeSonsBrilhinhos = 0.05f;
        [SerializeField, Min(0f), Tooltip("Quantos sons por segundo no auge. Zero desliga. Com clipes seus, nunca passa de um por metade da duração do clipe, para não virar uma massa de sons.")]
        private float sonsPorSegundo = 2f;

        [Header("Formato")]
        [SerializeField, Min(1), Tooltip("Pixels por unidade da arte do jogo: as colunas têm um pixel desse tamanho.")]
        private int pixelsPorUnidade = 16;
        [SerializeField, Range(1, 6), Tooltip("Quantas faixas (cortinas) a aurora tem.")]
        private int faixas = 4;
        [SerializeField, Range(3, 16), Tooltip("Degraus de cor em cada raio, da barra brilhante até o alto.")]
        private int degraus = 12;
        [SerializeField, Min(0.1f), Tooltip("Velocidade da ondulação.")]
        private float velocidadeOnda = 0.9f;
        [SerializeField, Tooltip("Ordem de desenho: acima de todo o cenário.")]
        private int ordemDeDesenho = 100;

        private const int BandasDeCeu = 18;
        // Os degraus ficam mais finos perto da barra (onde a cor muda mais) e mais altos no alto.
        private const float ExpoenteDosDegraus = 1.6f;

        // Três cores da paleta por faixa, da barra para o alto: verde para azul, magenta para azul, e assim por diante.
        private static readonly int[][] Gradientes =
        {
            new[] { 0, 1, 2 },
            new[] { 4, 3, 2 },
            new[] { 1, 2, 3 },
            new[] { 5, 4, 3 }
        };

        private static readonly List<Vector3> Vertices = new List<Vector3>(80000);
        private static readonly List<Color32> Cores = new List<Color32>(80000);
        private static readonly List<int> Triangulos = new List<int>(120000);

        private PlayerController jogador;
        private Fase fase = Fase.Parada;
        private float tempoFase;
        private float tempoOnda;
        private Mesh malha;
        private MeshRenderer desenho;
        private Material material;

        private AudioSource fonteMusica;
        private AudioSource fonteSons;
        private AudioClip[] clipesSons;
        private bool sonsCriadosPorCodigo;
        private readonly List<AudioSource> musicasAnteriores = new List<AudioSource>();
        private readonly List<float> volumesAnteriores = new List<float>();
        private float tempoMusica;
        private float proximoSom;

        // Verdadeiro do momento em que o Marcos se senta até a cena ser recarregada.
        public static bool Ativa { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Ativa = false;
        }

        // Começa o final. Usa a aurora que estiver na cena ou cria uma com os valores padrão.
        public static AuroraFinal Iniciar(PlayerController jogador, AudioClip musica = null, AudioClip[] sons = null)
        {
            AuroraFinal aurora = FindAnyObjectByType<AuroraFinal>();
            if (aurora == null)
            {
                aurora = new GameObject("AuroraFinal").AddComponent<AuroraFinal>();
            }

            aurora.Comecar(jogador, musica, sons);
            return aurora;
        }

        private void Comecar(PlayerController alvo, AudioClip musica, AudioClip[] sons)
        {
            if (fase != Fase.Parada)
            {
                return;
            }

            // A música da própria aurora (se houver na cena) vale mais que a da fogueira.
            if (musicaFinal == null)
            {
                musicaFinal = musica;
            }

            if ((sonsBrilhinhos == null || sonsBrilhinhos.Length == 0) && sons != null)
            {
                sonsBrilhinhos = sons;
            }

            jogador = alvo;
            Ativa = true;
            if (jogador != null)
            {
                // A interação encerra a fase; a tela de finalização só aparece depois da aurora.
                jogador.InputLocked = true;
                jogador.CompletePrototype();
            }

            CriarDesenho();
            IniciarMusica();
            CriarSons();
            Trocar(Fase.Espera);
        }

        private void OnDestroy()
        {
            Ativa = false;
            if (malha != null) Destroy(malha);
            if (material != null) Destroy(material);
            if (sonsCriadosPorCodigo && clipesSons != null)
            {
                foreach (AudioClip clipe in clipesSons)
                {
                    if (clipe != null) Destroy(clipe);
                }
            }
        }

        private void CriarDesenho()
        {
            if (desenho != null)
            {
                return;
            }

            malha = new Mesh { name = "AuroraFinal", indexFormat = IndexFormat.UInt32 };
            malha.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = malha;
            desenho = gameObject.AddComponent<MeshRenderer>();
            material = new Material(Shader.Find("Sprites/Default"));
            desenho.sharedMaterial = material;
            desenho.sortingOrder = ordemDeDesenho;
            desenho.shadowCastingMode = ShadowCastingMode.Off;
            desenho.receiveShadows = false;
            desenho.enabled = false;
        }

        private void Trocar(Fase nova)
        {
            fase = nova;
            tempoFase = 0f;
        }

        private void Update()
        {
            AtualizarMusica();
            AtualizarSons();
            if (fase == Fase.Parada || fase == Fase.Concluida)
            {
                return;
            }

            tempoFase += Time.deltaTime;
            switch (fase)
            {
                case Fase.Espera:
                    if (tempoFase >= atrasoInicial) Trocar(Fase.Envolver);
                    break;
                case Fase.Envolver:
                    if (tempoFase >= duracaoEnvolver) Trocar(Fase.Cobrir);
                    break;
                case Fase.Cobrir:
                    if (tempoFase >= duracaoCobrir) Trocar(Fase.Pausa);
                    break;
                case Fase.Pausa:
                    if (tempoFase >= pausaFinal) Concluir();
                    break;
            }
        }

        // Música: a que estiver tocando (qualquer fonte em loop fora do jogador) some aos poucos enquanto a do
        // final aparece. Os dois fades começam juntos, na hora em que o Marcos interage com a fogueira.
        private void IniciarMusica()
        {
            if (musicaFinal == null)
            {
                return;
            }

            foreach (AudioSource fonte in FindObjectsByType<AudioSource>())
            {
                if (fonte != null && fonte.isPlaying && fonte.loop && fonte.clip != musicaFinal
                    && fonte.GetComponentInParent<PlayerController>() == null)
                {
                    musicasAnteriores.Add(fonte);
                    volumesAnteriores.Add(fonte.volume);
                }
            }

            fonteMusica = gameObject.AddComponent<AudioSource>();
            fonteMusica.clip = musicaFinal;
            fonteMusica.loop = true;
            fonteMusica.playOnAwake = false;
            fonteMusica.spatialBlend = 0f;
            fonteMusica.volume = 0f;
            fonteMusica.Play();
            tempoMusica = 0f;
        }

        private void AtualizarMusica()
        {
            if (fonteMusica == null)
            {
                return;
            }

            // Tempo real: o fade segue mesmo com o jogo em câmera lenta.
            tempoMusica += Time.unscaledDeltaTime;
            float saida = fadeOutMusicaAnterior <= 0f ? 1f : Mathf.Clamp01(tempoMusica / fadeOutMusicaAnterior);
            for (int i = 0; i < musicasAnteriores.Count; i++)
            {
                AudioSource anterior = musicasAnteriores[i];
                if (anterior == null)
                {
                    continue;
                }

                anterior.volume = volumesAnteriores[i] * (1f - Mathf.SmoothStep(0f, 1f, saida));
                if (saida >= 1f && anterior.isPlaying)
                {
                    anterior.Stop();
                }
            }

            float entrada = fadeInMusicaFinal <= 0f ? 1f : Mathf.Clamp01(tempoMusica / fadeInMusicaFinal);
            fonteMusica.volume = volumeMusica * Mathf.SmoothStep(0f, 1f, entrada);
        }

        // Sons de brilho: sininhos curtos e agudos, sorteados em intervalos irregulares, mais frequentes
        // conforme a aurora cresce.
        private void CriarSons()
        {
            if (sonsPorSegundo <= 0f || volumeSonsBrilhinhos <= 0f)
            {
                return;
            }

            if (sonsBrilhinhos != null && sonsBrilhinhos.Length > 0)
            {
                clipesSons = sonsBrilhinhos;
            }
            else
            {
                // Escala pentatônica aguda: qualquer sequência soa bem.
                float[] notas = { 1046.5f, 1174.7f, 1318.5f, 1568f, 1760f };
                clipesSons = new AudioClip[notas.Length];
                for (int i = 0; i < notas.Length; i++)
                {
                    clipesSons[i] = CriarSino(notas[i]);
                }

                sonsCriadosPorCodigo = true;
            }

            fonteSons = gameObject.AddComponent<AudioSource>();
            fonteSons.playOnAwake = false;
            fonteSons.spatialBlend = 0f;
        }

        private void AtualizarSons()
        {
            if (fase < Fase.Envolver || fonteSons == null || clipesSons == null || clipesSons.Length == 0)
            {
                return;
            }

            float intensidade = fase == Fase.Envolver ? Mathf.Lerp(0.35f, 0.8f, Mathf.Clamp01(tempoFase / duracaoEnvolver))
                              : fase == Fase.Cobrir ? 1f
                              : 0.6f;
            proximoSom -= Time.deltaTime;
            if (proximoSom > 0f)
            {
                return;
            }

            AudioClip sorteado = clipesSons[Random.Range(0, clipesSons.Length)];
            fonteSons.pitch = Random.Range(0.92f, 1.08f);
            fonteSons.PlayOneShot(sorteado, volumeSonsBrilhinhos * intensidade * Random.Range(0.6f, 1f));
            float intervalo = Random.Range(0.5f, 1.5f) / (sonsPorSegundo * intensidade);
            // Clipes mais longos que um sininho (sons de brilho de verdade) precisam de espaço para soar.
            proximoSom = sonsCriadosPorCodigo ? intervalo : Mathf.Max(intervalo, sorteado.length * 0.5f);
        }

        // Sininho: nota com dois harmônicos que decaem rápido, ataque de 4 ms e 0,9 s no total.
        private static AudioClip CriarSino(float frequencia)
        {
            const int taxa = 44100;
            int amostras = Mathf.RoundToInt(taxa * 0.9f);
            float[] dados = new float[amostras];
            float maximo = 0.0001f;
            for (int i = 0; i < amostras; i++)
            {
                float t = i / (float)taxa;
                float ataque = Mathf.Min(1f, t / 0.004f);
                float v = Mathf.Sin(2f * Mathf.PI * frequencia * t) * Mathf.Exp(-6f * t)
                        + 0.35f * Mathf.Sin(2f * Mathf.PI * frequencia * 2f * t) * Mathf.Exp(-9f * t)
                        + 0.15f * Mathf.Sin(2f * Mathf.PI * frequencia * 3.01f * t) * Mathf.Exp(-13f * t);
                dados[i] = v * ataque;
                maximo = Mathf.Max(maximo, Mathf.Abs(dados[i]));
            }

            for (int i = 0; i < amostras; i++)
            {
                dados[i] = dados[i] / maximo * 0.8f;
            }

            AudioClip clipe = AudioClip.Create("SinoBrilhinho_" + Mathf.RoundToInt(frequencia), amostras, 1, taxa, false);
            clipe.SetData(dados, 0);
            return clipe;
        }

        // Tela cheia de cores: solta o jogador e mostra a tela de finalização (a mesma da zona de chegada).
        private void Concluir()
        {
            fase = Fase.Concluida;
            if (jogador != null)
            {
                jogador.InputLocked = false;
            }
        }

        private void LateUpdate()
        {
            if (fase < Fase.Envolver || desenho == null)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            tempoOnda += Time.deltaTime;
            Montar(camera);
            desenho.enabled = true;
        }

        private void Montar(Camera camera)
        {
            Vertices.Clear();
            Cores.Clear();
            Triangulos.Clear();

            // Quanto já foi percorrido de cada etapa (0 a 1). Depois de cobrir, as duas ficam em 1.
            float envolver = fase == Fase.Envolver ? Mathf.Clamp01(tempoFase / duracaoEnvolver) : 1f;
            float cobrir = fase == Fase.Cobrir ? Mathf.Clamp01(tempoFase / duracaoCobrir)
                         : fase >= Fase.Pausa ? 1f : 0f;
            float entrada = fase == Fase.Envolver ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tempoFase / 0.8f)) : 1f;
            float suaveEnvolver = Mathf.SmoothStep(0f, 1f, envolver);
            float suaveCobrir = Mathf.SmoothStep(0f, 1f, cobrir);
            float brilho = 0.88f + 0.12f * Mathf.Sin(tempoOnda * 3f);

            float passo = 1f / pixelsPorUnidade;
            float meiaAltura = camera.orthographicSize;
            float meiaLargura = meiaAltura * camera.aspect;
            Vector2 centroCamera = camera.transform.position;
            float esquerda = centroCamera.x - meiaLargura - 1f;
            float direita = centroCamera.x + meiaLargura + 1f;
            float baixo = centroCamera.y - meiaAltura - 1f;
            float topo = centroCamera.y + meiaAltura + 1f;

            Vector2 centroJogador = jogador != null ? (Vector2)jogador.BodyBounds.center : centroCamera;

            // Primeiro o céu escuro e as estrelas (só quando a tela está sendo coberta), depois as faixas por cima.
            if (suaveCobrir > 0f)
            {
                MontarCeu(esquerda, direita, baixo, topo, passo, suaveCobrir);
                MontarEstrelas(passo, esquerda, direita, baixo, topo, suaveCobrir);
            }

            // Área das faixas: pequena em volta do Marcos, depois cresce e vai para o centro da tela.
            // No fim a área passa da borda da tela (x1,3), para as pontas macias ficarem fora de vista.
            float meiaLarguraAurora = Mathf.Lerp(Mathf.Lerp(1.4f, 5f, suaveEnvolver), (direita - esquerda) * 0.5f * 1.3f, suaveCobrir);
            float centroX = Mathf.Lerp(centroJogador.x, centroCamera.x, suaveCobrir);
            float baseY = Mathf.Lerp(centroJogador.y - 1f, baixo + (topo - baixo) * 0.05f, suaveCobrir);
            float alturaAurora = Mathf.Lerp(Mathf.Lerp(2.5f, 7.5f, suaveEnvolver), (topo - baixo) * 0.9f, suaveCobrir);
            float amplitude = 0.25f + 0.1f * alturaAurora;

            // Pontas macias: cada faixa tem a sua largura e o seu centro, então as bordas nunca ficam alinhadas
            // numa linha reta, e a luz some aos poucos até as pontas. Quanto mais a aurora cobre a tela, mais o
            // miolo cheio se alarga e a parte que some vai para fora da tela.
            float inicioDaPonta = Mathf.Lerp(0.1f, 0.8f, suaveCobrir);
            float alcance = meiaLarguraAurora * 1.3f;
            int primeira = Mathf.FloorToInt(Mathf.Max(esquerda, centroX - alcance) / passo);
            int ultima = Mathf.CeilToInt(Mathf.Min(direita, centroX + alcance) / passo);
            for (int coluna = primeira; coluna <= ultima; coluna++)
            {
                float x = coluna * passo;
                for (int faixa = 0; faixa < faixas; faixa++)
                {
                    float larguraFaixa = meiaLarguraAurora * (0.72f + 0.28f * Mathf.Cos(faixa * 1.7f + tempoOnda * 0.25f));
                    float centroFaixa = centroX
                        + meiaLarguraAurora * 0.22f * Mathf.Sin(faixa * 2.1f + 1f + tempoOnda * 0.2f) * (1f - suaveCobrir);
                    float u = Mathf.Abs((x - centroFaixa) / larguraFaixa);
                    float lateral = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inicioDaPonta, 1f, u));
                    if (lateral <= 0.01f)
                    {
                        continue;
                    }

                    MontarColuna(x, passo, faixa, centroX, baseY, alturaAurora, amplitude, lateral, brilho, entrada,
                        suaveCobrir, baixo, topo, centroJogador.x, centroJogador.y);
                }
            }

            MontarBrilhinhos(passo, centroX, meiaLarguraAurora, baseY, alturaAurora,
                esquerda, direita, baixo, topo, entrada, suaveCobrir);

            malha.Clear();
            malha.SetVertices(Vertices);
            malha.SetColors(Cores);
            malha.SetTriangles(Triangulos, 0);
            malha.RecalculateBounds();
        }

        // Uma coluna de uma faixa: a barra brilhante embaixo, que ondula e faz curva, e os raios subindo dela,
        // mais fracos e mais azuis quanto mais alto.
        private void MontarColuna(float x, float passo, int faixa, float centroX, float baseY, float alturaAurora, float amplitude,
            float lateral, float brilho, float entrada, float suaveCobrir, float baixo, float topo, float jogadorX, float alturaJogador)
        {
            float lado = faixa % 2 == 0 ? 1f : -1f;
            float ondaPrincipal = Mathf.Sin(x * 0.35f + tempoOnda * velocidadeOnda * (1f + 0.25f * faixa) + faixa * 1.7f) * amplitude
                                + Mathf.Sin(x * 0.9f - tempoOnda * velocidadeOnda * 0.7f + faixa) * amplitude * 0.35f;
            // Inclinação que faz a barra atravessar a aurora na diagonal, como as cortinas da referência.
            float diagonal = (x - centroX) * 0.12f * lado * (1f - 0.5f * suaveCobrir);
            float barra = baseY + alturaAurora * (0.12f + 0.2f * faixa) + ondaPrincipal + diagonal;

            float altura = Mathf.Lerp(alturaAurora * 0.5f, alturaAurora * 0.95f, suaveCobrir)
                         * (0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(x * 0.8f + tempoOnda * 0.7f + faixa * 1.3f)));
            float raio = 0.3f + 0.7f * Mathf.Pow(Mathf.Abs(Mathf.Sin(x * 2.7f + faixa * 2.1f + tempoOnda * 0.5f)), 1.6f)
                       * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(x * 0.83f + faixa + tempoOnda * 0.3f)));
            float deriva = 0.5f + 0.5f * Mathf.Sin(x * 0.15f + tempoOnda * 0.4f + faixa);

            float barraAlinhada = Mathf.Floor(barra / passo) * passo;
            for (int degrau = 0; degrau < degraus; degrau++)
            {
                float fracaoA = Mathf.Pow((float)degrau / degraus, ExpoenteDosDegraus);
                float fracaoB = Mathf.Pow((degrau + 1f) / degraus, ExpoenteDosDegraus);
                float y0 = Mathf.Max(Mathf.Floor((barra + altura * fracaoA) / passo) * passo, baixo);
                float y1 = Mathf.Min(Mathf.Floor((barra + altura * fracaoB) / passo) * passo, topo);
                if (y1 <= y0)
                {
                    continue;
                }

                // v: 0 na barra, 1 no alto. Perto da barra a luz é cheia; os raios só aparecem mais acima.
                float v = (fracaoA + fracaoB) * 0.5f;
                float queda = Mathf.Pow(1f - v, 1.3f);
                float alfa = opacidade * lateral * Mathf.Lerp(raio, 1f, Mathf.Pow(1f - v, 3f)) * queda * brilho * entrada;
                if (degrau == 0)
                {
                    // A barra de baixo é a parte mais forte e mais clara.
                    alfa = Mathf.Max(alfa, opacidade * 0.9f * lateral * brilho * entrada);
                }

                Color cor = Color.Lerp(CorDaFaixa(faixa, v), CorDaFaixa(faixa + 1, v), deriva * 0.5f);
                if (degrau == 0)
                {
                    cor = Color.Lerp(cor, Color.white, 0.12f);
                }

                // Deixa o Marcos à vista enquanto a aurora o envolve: a luz diminui aos poucos perto dele, numa
                // elipse macia (sem borda reta), e volta ao normal quando a tela está coberta.
                float dx = (x - jogadorX) / 1.2f;
                float dy = ((y0 + y1) * 0.5f - alturaJogador) / 1.5f;
                float distancia = Mathf.Sqrt(dx * dx + dy * dy);
                if (distancia < 1f)
                {
                    alfa *= Mathf.Lerp(Mathf.Lerp(0.35f, 1f, Mathf.SmoothStep(0.3f, 1f, distancia)), 1f, suaveCobrir);
                }

                AdicionarRetangulo(x, y0, x + passo, y1, ComAlfa(cor, alfa));
            }

            // Fio de luz de um pixel na borda da barra.
            if (barraAlinhada >= baixo && barraAlinhada < topo)
            {
                Color fio = Color.Lerp(CorDaFaixa(faixa, 0f), Color.white, 0.4f);
                AdicionarRetangulo(x, barraAlinhada, x + passo, barraAlinhada + passo,
                    ComAlfa(fio, opacidade * lateral * brilho * entrada));
            }
        }

        // Cor da faixa na altura v (0 na barra, 1 no alto), passando pelas três cores do gradiente dela.
        private Color CorDaFaixa(int faixa, float v)
        {
            int[] g = Gradientes[((faixa % Gradientes.Length) + Gradientes.Length) % Gradientes.Length];
            float posicao = Mathf.Clamp01(v) * 2f;
            return posicao < 1f
                ? Color.Lerp(CorDaPaleta(g[0]), CorDaPaleta(g[1]), posicao)
                : Color.Lerp(CorDaPaleta(g[1]), CorDaPaleta(g[2]), posicao - 1f);
        }

        private Color CorDaPaleta(int indice)
        {
            int n = paleta != null ? paleta.Length : 0;
            return n > 0 ? paleta[((indice % n) + n) % n] : Color.white;
        }

        // Céu que, no fim, cobre a tela inteira: azul-marinho no alto, mais claro perto do horizonte.
        private void MontarCeu(float esquerda, float direita, float baixo, float topo, float passo, float suaveCobrir)
        {
            float alfa = Mathf.Clamp01(Mathf.Pow(suaveCobrir, 1.2f) * 1.05f);
            float altura = topo - baixo;
            float anterior = baixo;
            for (int banda = 0; banda < BandasDeCeu; banda++)
            {
                float fim = banda == BandasDeCeu - 1
                    ? topo
                    : Mathf.Floor((baixo + altura * (banda + 1) / BandasDeCeu) / passo) * passo;
                if (fim > anterior)
                {
                    float t = 1f - (banda + 0.5f) / BandasDeCeu; // 0 no alto, 1 embaixo
                    AdicionarRetangulo(esquerda, anterior, direita, fim, ComAlfa(Color.Lerp(ceuAlto, ceuBaixo, t * t), alfa));
                    anterior = fim;
                }
            }
        }

        // Estrelas pequenas espalhadas pela tela: um ponto que cintila, algumas com uma cruz fraca.
        private void MontarEstrelas(float passo, float esquerda, float direita, float baixo, float topo, float suaveCobrir)
        {
            int quantas = Mathf.RoundToInt(estrelas * suaveCobrir);
            for (int i = 0; i < quantas; i++)
            {
                float x = Mathf.Floor(Mathf.Lerp(esquerda, direita, Aleatorio(i + 500, 1)) / passo) * passo;
                float y = Mathf.Floor(Mathf.Lerp(baixo, topo, Aleatorio(i + 500, 2)) / passo) * passo;
                float ritmo = Aleatorio(i + 500, 4);
                float cintilar = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(tempoOnda * (0.6f + ritmo) + Aleatorio(i + 500, 3) * 6.28f));
                float alfa = Mathf.Clamp01(suaveCobrir * cintilar);
                AdicionarRetangulo(x, y, x + passo, y + passo, ComAlfa(corBrilhinho, alfa));
                if (ritmo > 0.85f)
                {
                    Color32 braco = ComAlfa(corBrilhinho, alfa * 0.4f);
                    AdicionarRetangulo(x + passo, y, x + 2f * passo, y + passo, braco);
                    AdicionarRetangulo(x - passo, y, x, y + passo, braco);
                    AdicionarRetangulo(x, y + passo, x + passo, y + 2f * passo, braco);
                    AdicionarRetangulo(x, y - passo, x + passo, y, braco);
                }
            }
        }

        // Brilhinhos: pontos que acendem, viram uma cruz de pixels e apagam enquanto sobem devagar.
        // Cada um tem posição, ritmo e tamanho fixos (derivados do número dele), então não há sorteio por quadro.
        private void MontarBrilhinhos(float passo, float centroX, float meiaLarguraAurora, float baseY, float alturaAurora,
            float esquerda, float direita, float baixo, float topo, float entrada, float suaveCobrir)
        {
            int quantos = Mathf.RoundToInt(brilhinhos * Mathf.Lerp(0.45f, 1f, suaveCobrir) * entrada);
            for (int i = 0; i < quantos; i++)
            {
                float sorteioX = Aleatorio(i, 1);
                float sorteioY = Aleatorio(i, 2);
                float sorteioFase = Aleatorio(i, 3);
                float sorteioRitmo = Aleatorio(i, 4);

                float ciclo = Mathf.Repeat(tempoOnda * velocidadeBrilhinhos * (0.6f + sorteioRitmo) + sorteioFase, 1f);
                float vida = Mathf.Sin(ciclo * Mathf.PI);
                vida *= vida;
                if (vida < 0.05f)
                {
                    continue;
                }

                float x = centroX + (sorteioX * 2f - 1f) * meiaLarguraAurora;
                float y = baseY + sorteioY * alturaAurora + ciclo * 0.6f;
                if (x < esquerda || x > direita || y < baixo || y > topo)
                {
                    continue;
                }

                x = Mathf.Floor(x / passo) * passo;
                y = Mathf.Floor(y / passo) * passo;
                float alfa = Mathf.Clamp01(vida * entrada);
                AdicionarRetangulo(x, y, x + passo, y + passo, ComAlfa(corBrilhinho, alfa));

                // Passando do meio do ciclo o ponto vira uma cruz; os maiores têm braços de dois pixels.
                if (vida > 0.45f)
                {
                    int braco = sorteioRitmo > 0.75f ? 2 : 1;
                    Color32 corBraco = ComAlfa(Color.Lerp(CorDaPaleta(i), Color.white, 0.55f), alfa * 0.75f);
                    for (int p = 1; p <= braco; p++)
                    {
                        float d = p * passo;
                        AdicionarRetangulo(x + d, y, x + d + passo, y + passo, corBraco);
                        AdicionarRetangulo(x - d, y, x - d + passo, y + passo, corBraco);
                        AdicionarRetangulo(x, y + d, x + passo, y + d + passo, corBraco);
                        AdicionarRetangulo(x, y - d, x + passo, y - d + passo, corBraco);
                    }
                }
            }
        }

        private static float Aleatorio(int indice, int canal)
        {
            return Mathf.Repeat(Mathf.Sin(indice * 12.9898f + canal * 78.233f) * 43758.5453f, 1f);
        }

        private static Color32 ComAlfa(Color cor, float alfa)
        {
            cor.a = Mathf.Clamp01(alfa);
            return cor;
        }

        private static void AdicionarRetangulo(float x0, float y0, float x1, float y1, Color32 cor)
        {
            int inicio = Vertices.Count;
            Vertices.Add(new Vector3(x0, y0, 0f));
            Vertices.Add(new Vector3(x0, y1, 0f));
            Vertices.Add(new Vector3(x1, y1, 0f));
            Vertices.Add(new Vector3(x1, y0, 0f));
            Cores.Add(cor);
            Cores.Add(cor);
            Cores.Add(cor);
            Cores.Add(cor);
            Triangulos.Add(inicio);
            Triangulos.Add(inicio + 1);
            Triangulos.Add(inicio + 2);
            Triangulos.Add(inicio);
            Triangulos.Add(inicio + 2);
            Triangulos.Add(inicio + 3);
        }
    }
}
