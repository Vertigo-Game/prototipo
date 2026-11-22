using UnityEngine;

namespace Vertigo.Gameplay
{
    // Sons do gancho. O PlayerController avisa cada momento: mirando (câmera lenta), conectou
    // (conexão + puxão) e saiu do gancho com impulso. A câmera lenta toca uma vez, sem loop, com
    // entrada suave, e é cortada com uma saída curta assim que o tempo volta ao normal.
    // Os fades usam tempo real: não podem andar devagar junto com a câmera lenta.
    public sealed class SonsGancho : MonoBehaviour
    {
        [SerializeField] private AudioClip conectar;
        [SerializeField] private AudioClip cameraLenta;
        [SerializeField] private AudioClip puxar;
        [SerializeField] private AudioClip soltarImpulso;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Min(0f), Tooltip("Entrada suave da câmera lenta, em segundos.")]
        private float entradaCameraLenta = 0.03f;
        [SerializeField, Min(0f), Tooltip("Saída da câmera lenta quando o tempo volta ao normal, em segundos.")]
        private float saidaCameraLenta = 0.018f;

        private AudioSource efeitos;
        private AudioSource fonteCameraLenta;
        private float volumeAlvo;
        private float duracaoFade;

        private void Awake()
        {
            efeitos = CriarFonte();
            fonteCameraLenta = CriarFonte();
            fonteCameraLenta.clip = cameraLenta;
        }

        private AudioSource CriarFonte()
        {
            AudioSource fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.loop = false;
            fonte.spatialBlend = 0f;
            return fonte;
        }

        public void IniciarCameraLenta()
        {
            if (cameraLenta == null)
            {
                return;
            }

            fonteCameraLenta.volume = 0f;
            fonteCameraLenta.Play();
            Fade(volume, entradaCameraLenta);
        }

        public void PararCameraLenta()
        {
            if (fonteCameraLenta != null && fonteCameraLenta.isPlaying)
            {
                Fade(0f, saidaCameraLenta);
            }
        }

        // Conectar e puxar saem juntos: no jogo o gancho prende e o Marcos já começa a ser puxado.
        public void TocarConexao()
        {
            Tocar(conectar);
            Tocar(puxar);
        }

        public void TocarSoltura()
        {
            Tocar(soltarImpulso);
        }

        private void Tocar(AudioClip clipe)
        {
            if (clipe != null)
            {
                efeitos.PlayOneShot(clipe, volume);
            }
        }

        private void Fade(float alvo, float duracao)
        {
            volumeAlvo = alvo;
            duracaoFade = duracao;
            if (duracao <= 0f)
            {
                AplicarVolume(alvo);
            }
        }

        private void Update()
        {
            if (!fonteCameraLenta.isPlaying || Mathf.Approximately(fonteCameraLenta.volume, volumeAlvo))
            {
                return;
            }

            float passo = volume * Time.unscaledDeltaTime / Mathf.Max(duracaoFade, 0.0001f);
            AplicarVolume(Mathf.MoveTowards(fonteCameraLenta.volume, volumeAlvo, passo));
        }

        private void AplicarVolume(float valor)
        {
            fonteCameraLenta.volume = valor;
            if (valor <= 0f && volumeAlvo <= 0f)
            {
                fonteCameraLenta.Stop();
            }
        }
    }
}
