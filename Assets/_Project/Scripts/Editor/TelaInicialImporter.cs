using UnityEditor;
using UnityEngine;

namespace Vertigo.EditorTools
{
    // Garante as configurações de importação da arte da tela inicial (docs/tela-inicial.md).
    public sealed class TelaInicialImporter : AssetPostprocessor
    {
        private const string Pasta = "Assets/_Project/Art/UI/TelaInicial/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Pasta))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = assetPath.EndsWith("ti_05_logo.png")
                ? (int)SpriteAlignment.TopLeft
                : (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
