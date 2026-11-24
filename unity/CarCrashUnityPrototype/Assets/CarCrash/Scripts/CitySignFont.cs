using UnityEngine;

namespace CarCrash
{
    // Keep depth-tested world lettering bound to Unity's rebuilt dynamic atlas.
    public sealed class CitySignFont : MonoBehaviour
    {
        public Font font;
        public Material material;

        public static Material CreateWorldMaterial(Font font, GameObject owner)
        {
            var shader = Shader.Find("CarCrash/WorldSignText");
            if (font == null || shader == null) return null;
            var material = new Material(shader) { mainTexture = font.material.mainTexture };
            var binding = owner.AddComponent<CitySignFont>();
            binding.font = font;
            binding.material = material;
            return material;
        }

        private void OnEnable() { Font.textureRebuilt += OnAtlasRebuilt; Bind(); }
        private void Start() => Bind();
        private void OnDisable() => Font.textureRebuilt -= OnAtlasRebuilt;
        private void OnAtlasRebuilt(Font rebuilt) { if (rebuilt == font) Bind(); }
        private void Bind() { if (font != null && material != null) material.mainTexture = font.material.mainTexture; }
    }
}
