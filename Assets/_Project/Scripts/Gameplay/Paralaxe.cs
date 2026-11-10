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

            cameraAlvo = principal.transform;
            posicaoInicial = transform.position;
            cameraInicial = cameraAlvo.position;

            SpriteRenderer desenho = GetComponent<SpriteRenderer>();
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

            transform.position = posicao;
        }
    }
}
