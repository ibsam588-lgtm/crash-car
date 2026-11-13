using UnityEngine;

namespace CarCrash
{
    [RequireComponent(typeof(Camera))]
    public sealed class CinematicCameraGrade : MonoBehaviour
    {
        public Shader gradeShader;
        private Material _material;

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (gradeShader == null || !gradeShader.isSupported) { Graphics.Blit(source, destination); return; }
            if (_material == null) _material = new Material(gradeShader) { hideFlags = HideFlags.HideAndDontSave };
            Graphics.Blit(source, destination, _material);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
