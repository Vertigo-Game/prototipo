using UnityEngine;

namespace Vertigo.Gameplay
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CorDoCristal : MonoBehaviour
    {
        private static readonly int CorId = Shader.PropertyToID("_CorCristal");

        [SerializeField, Tooltip("Define a cor do cristal e do contorno. Sem perfil, valem os ajustes de cada componente.")]
        private PerfilVisualInimigo perfil;
        [SerializeField, Tooltip("Sem perfil, troca os tons azuis e ciano do cristal em todos os quadros da animação.")]
        private bool recolorir;
        [SerializeField, ColorUsage(false)] private Color cor = Color.red;

        private SpriteRenderer visual;
        private MaterialPropertyBlock bloco;
        private Color corAplicada;
        private bool atualizarCor;

        public PerfilVisualInimigo Perfil => perfil;

        private void OnEnable()
        {
            visual = GetComponent<SpriteRenderer>();
            bloco ??= new MaterialPropertyBlock();
            AplicarCor(CorDoShader());
        }

        private void OnValidate()
        {
            atualizarCor = true;
        }

        private void Update()
        {
            if (atualizarCor || perfil != null)
            {
                Color corCristal = CorDoShader();
                if (corCristal != corAplicada)
                {
                    AplicarCor(corCristal);
                }

                atualizarCor = false;
            }
        }

        private void OnDisable()
        {
            if (visual != null)
            {
                AplicarCor(Color.clear);
            }
        }

        private Color CorDoShader()
        {
            Color corCristal = cor;
            bool recolorirCristal = recolorir;
            if (perfil != null)
            {
                corCristal = perfil.CorNoTempo(Application.IsPlaying(gameObject) ? Time.time : 0f);
                recolorirCristal = perfil.RecolorirSprite;
            }

            corCristal.a = recolorirCristal ? 1f : 0f;
            return corCristal;
        }

        private void AplicarCor(Color corCristal)
        {
            corAplicada = corCristal;
            visual.GetPropertyBlock(bloco);
            bloco.SetColor(CorId, corCristal);
            visual.SetPropertyBlock(bloco);
        }
    }
}
