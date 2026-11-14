using UnityEngine;

namespace CarCrash
{
    [RequireComponent(typeof(Camera))]
    public sealed class CinematicCameraGrade : MonoBehaviour
    {
        public Shader gradeShader;
        [Range(0,1)] public float contactShadowStrength=.8f;
        [Range(.1f,2f)] public float contactShadowRadius=.85f;
        [Range(0,.5f)] public float bloomStrength=.10f;
        private Material _material;

        private void OnEnable()=>GetComponent<Camera>().depthTextureMode|=DepthTextureMode.Depth;

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (gradeShader == null || !gradeShader.isSupported) { Graphics.Blit(source, destination); return; }
            if (_material == null) _material = new Material(gradeShader) { hideFlags = HideFlags.HideAndDontSave };
            var ao=RenderTexture.GetTemporary(Mathf.Max(64,source.width/2),Mathf.Max(64,source.height/2),0,
                SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32);
            var bloom=RenderTexture.GetTemporary(Mathf.Max(32,source.width/4),Mathf.Max(32,source.height/4),0,RenderTextureFormat.ARGB32);
            var blur=RenderTexture.GetTemporary(bloom.width,bloom.height,0,RenderTextureFormat.ARGB32);
            try
            {
                ao.filterMode=bloom.filterMode=blur.filterMode=FilterMode.Bilinear;
                ao.wrapMode=bloom.wrapMode=blur.wrapMode=TextureWrapMode.Clamp;
                _material.SetFloat("_OcclusionStrength",contactShadowStrength);
                _material.SetFloat("_OcclusionRadius",contactShadowRadius);
                _material.SetFloat("_BloomStrength",bloomStrength);
                Graphics.Blit(source,ao,_material,0);
                Graphics.Blit(source,bloom,_material,1);
                _material.SetVector("_BlurDirection",new Vector4(1f/bloom.width,0,0,0));
                Graphics.Blit(bloom,blur,_material,2);
                _material.SetVector("_BlurDirection",new Vector4(0,1f/bloom.height,0,0));
                Graphics.Blit(blur,bloom,_material,2);
                _material.SetTexture("_AOTexture",ao);_material.SetTexture("_BloomTexture",bloom);
                Graphics.Blit(source,destination,_material,3);
            }
            finally {RenderTexture.ReleaseTemporary(ao);RenderTexture.ReleaseTemporary(bloom);RenderTexture.ReleaseTemporary(blur);}
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
