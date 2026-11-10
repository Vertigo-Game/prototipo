using HookPrototype;
using UnityEngine;

namespace Vertigo.Gameplay
{
    // Inimigo capturado pelo hook some e reaparece quando o jogador desce e a área do inimigo
    // fica inteira acima da câmera. Funciona com o inimigo parado e com o InimigoMovel
    // (que volta ao começo da rota). Se o jogador morrer, a cena recarrega e todos voltam de qualquer jeito.
    [RequireComponent(typeof(HookTarget))]
    public sealed class RenascimentoInimigo : MonoBehaviour
    {
        private HookTarget alvo;
        private InimigoMovel movel;
        private Renderer[] visuais;
        private Collider2D[] colisores;
        private bool[] visualLigado;
        private bool[] colisorLigado;
        private bool movelLigado;
        // Tudo o que o inimigo pode ocupar: o próprio corpo ou, se ele anda, a rota inteira mais o corpo.
        private Bounds area;

        public bool Capturado { get; private set; }

        private void Awake()
        {
            alvo = GetComponent<HookTarget>();
            movel = GetComponent<InimigoMovel>();
            visuais = GetComponentsInChildren<Renderer>();
            colisores = GetComponentsInChildren<Collider2D>();
            visualLigado = new bool[visuais.Length];
            colisorLigado = new bool[colisores.Length];
        }

        private void Start()
        {
            // No Start o InimigoMovel já se posicionou no começo da rota.
            area = CalcularArea();
        }

        private void OnEnable()
        {
            alvo.Captured += AoSerCapturado;
        }

        private void OnDisable()
        {
            alvo.Captured -= AoSerCapturado;
        }

        private void Update()
        {
            if (Capturado && AcimaDaCamera(area, Camera.main))
            {
                Reaparecer();
            }
        }

        private static bool AcimaDaCamera(Bounds area, Camera camera)
        {
            if (camera == null)
            {
                return false;
            }

            Vector3 baseDaArea = new Vector3(area.center.x, area.min.y, area.center.z);
            return camera.WorldToViewportPoint(baseDaArea).y > 1f;
        }

        private void AoSerCapturado()
        {
            if (Capturado)
            {
                return;
            }

            Capturado = true;
            for (int i = 0; i < visuais.Length; i++)
            {
                visualLigado[i] = visuais[i].enabled;
                visuais[i].enabled = false;
            }

            for (int i = 0; i < colisores.Length; i++)
            {
                colisorLigado[i] = colisores[i].enabled;
                colisores[i].enabled = false;
            }

            movelLigado = movel != null && movel.enabled;
            if (movelLigado)
            {
                movel.enabled = false;
            }
        }

        private void Reaparecer()
        {
            Capturado = false;
            if (movelLigado)
            {
                movel.enabled = true;
                movel.Reiniciar();
            }

            for (int i = 0; i < visuais.Length; i++)
            {
                visuais[i].enabled = visualLigado[i];
            }

            for (int i = 0; i < colisores.Length; i++)
            {
                colisores[i].enabled = colisorLigado[i];
            }
        }

        private Bounds CalcularArea()
        {
            Bounds corpo = new Bounds(transform.position, Vector3.zero);
            foreach (Renderer visual in visuais)
            {
                corpo.Encapsulate(visual.bounds);
            }

            foreach (Collider2D colisor in colisores)
            {
                corpo.Encapsulate(colisor.bounds);
            }

            if (movel == null || !movel.enabled)
            {
                return corpo;
            }

            Bounds rota = movel.AreaDaRota;
            rota.Expand(corpo.size);
            return rota;
        }
    }
}
