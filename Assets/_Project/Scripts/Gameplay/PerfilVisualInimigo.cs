using UnityEngine;

namespace Vertigo.Gameplay
{
    [CreateAssetMenu(menuName = "Vertigo/Perfil visual de inimigo")]
    public sealed class PerfilVisualInimigo : ScriptableObject
    {
        [SerializeField, Tooltip("Desmarque para manter a cor original do sprite e aplicar a cor só ao contorno.")]
        private bool recolorirSprite = true;
        [SerializeField, ColorUsage(false)] private Color cor = Color.white;
        [SerializeField, Range(0f, 1f)] private float opacidadeContorno = 0.5f;
        [SerializeField] private bool arcoIris;
        [SerializeField, Min(0f)] private float ciclosPorSegundo = 0.8f;

        public bool RecolorirSprite => recolorirSprite;
        public float OpacidadeContorno => opacidadeContorno;

        public Color CorNoTempo(float tempo)
        {
            if (!arcoIris)
            {
                return cor;
            }

            Color.RGBToHSV(cor, out float matiz, out float saturacao, out float brilho);
            matiz = Mathf.Repeat(matiz + tempo * ciclosPorSegundo, 1f);
            return Color.HSVToRGB(matiz, saturacao, brilho);
        }
    }
}
