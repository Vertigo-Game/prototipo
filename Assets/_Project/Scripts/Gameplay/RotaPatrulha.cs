using System;
using UnityEngine;

namespace Vertigo.Gameplay
{
    // Caminho feito de segmentos retos entre pontos, percorrido em ida e volta ou em loop.
    // Só faz a conta da posição: não sabe nada de física, estados ou hook.
    public sealed class RotaPatrulha
    {
        private readonly Vector2[] pontos;
        private readonly float[] acumulado; // distância do início da rota até cada ponto
        private readonly bool emLoop;
        private float distancia;

        public RotaPatrulha(Vector2[] pontos, bool emLoop, float progressoInicial, bool comecaVoltando)
        {
            if (pontos == null || pontos.Length < 2)
            {
                throw new ArgumentException("A rota precisa de pelo menos dois pontos.", nameof(pontos));
            }

            this.pontos = (Vector2[])pontos.Clone();
            this.emLoop = emLoop;

            // No loop existe um trecho a mais, do último ponto de volta ao primeiro.
            int trechos = emLoop ? pontos.Length : pontos.Length - 1;
            acumulado = new float[trechos + 1];
            for (int i = 0; i < trechos; i++)
            {
                acumulado[i + 1] = acumulado[i] + Vector2.Distance(Ponto(i), Ponto(i + 1));
            }

            Comprimento = acumulado[trechos];
            distancia = Mathf.Clamp01(progressoInicial) * Comprimento;
            Sentido = comecaVoltando ? -1 : 1;
            Posicao = Avaliar(distancia);
        }

        public float Comprimento { get; }
        public Vector2 Posicao { get; private set; }
        public int Sentido { get; private set; }
        public float Progresso => Comprimento > 0f ? distancia / Comprimento : 0f;

        // Anda 'passo' unidades no sentido atual. Na ida e volta, para exatamente no extremo,
        // inverte o sentido e devolve em 'sobra' o que faltou andar. Retorna true ao chegar num extremo.
        public bool Avancar(float passo, out float sobra)
        {
            sobra = 0f;
            if (passo <= 0f || Comprimento <= 0f)
            {
                return false;
            }

            if (emLoop)
            {
                distancia = Mathf.Repeat(distancia + Sentido * passo, Comprimento);
                Posicao = Avaliar(distancia);
                return false;
            }

            float destino = distancia + Sentido * passo;
            float extremo = Sentido > 0 ? Comprimento : 0f;
            bool chegou = Sentido > 0 ? destino >= Comprimento : destino <= 0f;
            if (chegou)
            {
                sobra = Mathf.Abs(destino - extremo);
                destino = extremo;
                Sentido = -Sentido;
            }

            distancia = destino;
            Posicao = Avaliar(distancia);
            return chegou;
        }

        private Vector2 Avaliar(float d)
        {
            int trecho = 0;
            while (trecho < acumulado.Length - 2 && d > acumulado[trecho + 1])
            {
                trecho++;
            }

            float tamanho = acumulado[trecho + 1] - acumulado[trecho];
            float t = tamanho > 0f ? (d - acumulado[trecho]) / tamanho : 0f;
            return Vector2.Lerp(Ponto(trecho), Ponto(trecho + 1), t);
        }

        private Vector2 Ponto(int indice)
        {
            return pontos[indice % pontos.Length];
        }
    }
}
