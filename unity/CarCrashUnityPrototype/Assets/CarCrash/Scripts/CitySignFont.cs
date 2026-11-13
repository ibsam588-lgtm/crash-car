using UnityEngine;

namespace CarCrash
{
    // Keep depth-tested world lettering bound to Unity's rebuilt dynamic atlas.
    public sealed class CitySignFont : MonoBehaviour
    {
        public Font font;
        public Material material;
        private void OnEnable() { Font.textureRebuilt += OnAtlasRebuilt; Bind(); }
        private void Start() => Bind();
        private void OnDisable() => Font.textureRebuilt -= OnAtlasRebuilt;
        private void OnAtlasRebuilt(Font rebuilt) { if (rebuilt == font) Bind(); }
        private void Bind() { if (font != null && material != null) material.mainTexture = font.material.mainTexture; }
    }
}
