using UnityEngine;

namespace Vertigo.Gameplay
{
    // Camada de paralaxe: acompanha a câmera só em parte, então parece mais longe (ou mais perto).
    // Fator 0 = fica parada no mundo (como o cenário); fator 1 = acompanha a câmera (parada na tela, infinitamente longe).
    // Em geral: camada de trás perto de 0.9, do meio 0.6, da frente 0.3.
    [DefaultExecutionOrder(-50)] // depois da câmera (-100), antes de tudo que lê a posição dela
    public sealed class Paralaxe : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float fatorX = 0.5f;
        [SerializeField, Range(0f, 1f)] private float fatorY = 0.5f;

        [Tooltip("Repete a imagem na horizontal para nunca acabar (usa o modo Tiled do SpriteRenderer).")]
        [SerializeField] private bool repetirHorizontal = true;

        [Tooltip("Impede a imagem de sair da tela na vertical (quando o jogador sobe ou desce demais). Precisa de camera ortografica.")]
        [SerializeField] private bool manterCobrindoVertical = true;

        private Camera cameraPrincipal;
        private float metadeAltura;
        private float centroY;
        private Transform cameraAlvo;
        private Vector3 posicaoInicial;
        private Vector3 cameraInicial;
        private float larguraImagem;

        private void Start()
        {
            Camera principal = Camera.main;
            if (principal == null)
            {
                enabled = false;
                return;
            }

            cameraPrincipal = principal;
            cameraAlvo = principal.transform;
            posicaoInicial = transform.position;
            cameraInicial = cameraAlvo.position;

            SpriteRenderer desenho = GetComponent<SpriteRenderer>();
            if (desenho != null && desenho.sprite != null)
            {
                metadeAltura = desenho.sprite.bounds.size.y * Mathf.Abs(transform.lossyScale.y) * 0.5f;
                centroY = desenho.sprite.bounds.center.y * transform.lossyScale.y;
            }

            if (repetirHorizontal && desenho != null && desenho.sprite != null)
            {
                Vector2 tamanho = desenho.sprite.bounds.size;
                larguraImagem = tamanho.x * Mathf.Abs(transform.lossyScale.x);
                desenho.drawMode = SpriteDrawMode.Tiled;
                desenho.size = new Vector2(tamanho.x * 5f, tamanho.y);
            }
        }

        private void LateUpdate()
        {
            Vector3 deslocamento = cameraAlvo.position - cameraInicial;
            Vector3 posicao = posicaoInicial + new Vector3(deslocamento.x * fatorX, deslocamento.y * fatorY, 0f);

            if (larguraImagem > 0f)
            {
                // Anda de uma largura em uma largura para a imagem repetida ficar sempre em volta da câmera.
                float distancia = cameraAlvo.position.x - posicao.x;
                posicao.x += Mathf.Round(distancia / larguraImagem) * larguraImagem;
            }

            if (manterCobrindoVertical && cameraPrincipal.orthographic && metadeAltura > 0f)
            {
                // O centro da imagem tem que ficar entre estes limites para ela cobrir a tela de cima a baixo.
                float meiaTela = cameraPrincipal.orthographicSize;
                float minimo = cameraAlvo.position.y + meiaTela - metadeAltura;
                float maximo = cameraAlvo.position.y - meiaTela + metadeAltura;
                if (minimo <= maximo)
                    posicao.y = Mathf.Clamp(posicao.y + centroY, minimo, maximo) - centroY;
            }

            transform.position = posicao;
        }
    }
}
