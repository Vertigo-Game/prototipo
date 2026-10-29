using UnityEngine;

namespace Vertigo.UI
{
    // Flocos de 1 px caindo devagar com vento para a esquerda, travados no grid de pixels.
    // Substitui um ParticleSystem: mesmo efeito, sem as milhares de linhas que ele grava na cena.
    public sealed class NeveCaindo : MonoBehaviour
    {
        [SerializeField] private int quantidade = 150;
        [SerializeField] private Vector2 area = new Vector2(44f, 24f);
        [SerializeField] private Vector2 velocidadeQueda = new Vector2(1.4f, 2f);
        [SerializeField] private Vector2 vento = new Vector2(-0.6f, -0.2f);
        [SerializeField] private float balanco = 0.25f;
        [SerializeField] private Color cor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private int ordemDeDesenho = 7;
        [SerializeField] private float pixelsPorUnidade = 16f;

        private Transform[] flocos;
        private Vector2[] posicoes;
        private Vector2[] velocidades;
        private float[] fases;

        private void Awake()
        {
            Sprite pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            float tamanho = 1f / pixelsPorUnidade;

            flocos = new Transform[quantidade];
            posicoes = new Vector2[quantidade];
            velocidades = new Vector2[quantidade];
            fases = new float[quantidade];

            for (int i = 0; i < quantidade; i++)
            {
                var floco = new GameObject("Floco");
                floco.transform.SetParent(transform, false);
                floco.transform.localScale = Vector3.one * tamanho;
                var sr = floco.AddComponent<SpriteRenderer>();
                sr.sprite = pixel;
                sr.color = cor;
                sr.sortingOrder = ordemDeDesenho;

                flocos[i] = floco.transform;
                posicoes[i] = new Vector2(Random.Range(-area.x, area.x), Random.Range(-area.y, area.y)) * 0.5f;
                velocidades[i] = new Vector2(Random.Range(vento.x, vento.y), -Random.Range(velocidadeQueda.x, velocidadeQueda.y));
                fases[i] = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        private void Update()
        {
            Vector2 meia = area * 0.5f;
            for (int i = 0; i < flocos.Length; i++)
            {
                Vector2 p = posicoes[i] + velocidades[i] * Time.deltaTime;
                p.x += Mathf.Sin(Time.time + fases[i]) * balanco * Time.deltaTime;

                // Saiu por baixo ou pela esquerda: volta por cima ou pela direita.
                if (p.y < -meia.y) p.y += area.y;
                if (p.x < -meia.x) p.x += area.x;
                posicoes[i] = p;

                flocos[i].localPosition = new Vector3(
                    Mathf.Round(p.x * pixelsPorUnidade) / pixelsPorUnidade,
                    Mathf.Round(p.y * pixelsPorUnidade) / pixelsPorUnidade,
                    0f);
            }
        }
    }
}
