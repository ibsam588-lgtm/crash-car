using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrashEditor
{
    public static class TrafficMaterialLibrary
    {
        public static void Apply(GameObject model, Color paint)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var slot = 0; slot < materials.Length; slot++)
                {
                    if (materials[slot] == null) continue;
                    var name = materials[slot].name;
                    var role = name.Replace("M_Fleet_", "");
                    var color = new Color(0.045f, 0.05f, 0.055f);
                    var metal = 0.15f;
                    var smooth = 0.4f;
                    var emission = 0f;
                    if (role == "Paint") { color = paint; metal = 0.42f; smooth = 0.76f; }
                    if (role == "Tire") { color = new Color(0.095f, 0.10f, 0.11f); metal = 0; smooth = 0.19f; }
                    if (role == "Alloy") { color = new Color(0.47f, 0.50f, 0.53f); metal = 0.82f; smooth = 0.66f; }
                    if (role == "Glass") { color = new Color(0.08f, 0.14f, 0.18f, 0.58f); metal = 0.22f; smooth = 0.94f; }
                    if (role == "Interior") { color = new Color(0.08f, 0.065f, 0.055f); metal = 0; smooth = 0.25f; }
                    if (role == "Caliper") { color = new Color(0.55f, 0.025f, 0.02f); smooth = 0.48f; }
                    if (role == "Headlamp") { color = new Color(0.67f, 0.80f, 0.96f); emission = 1.2f; smooth = 0.8f; }
                    if (role == "Taillamp") { color = new Color(0.65f, 0.008f, 0.015f); emission = 1.4f; smooth = 0.8f; }
                    var suffix = role == "Paint" ? $"_{Mathf.RoundToInt(paint.r * 255)}_{Mathf.RoundToInt(paint.g * 255)}_{Mathf.RoundToInt(paint.b * 255)}" : "";
                    var material = RealismMaterialLibrary.Standard("M_Fleet_" + role + suffix, color, metal, smooth, emission);
                    if (role == "Glass")
                    {
                        material.SetFloat("_Mode", 3);
                        material.SetInt("_SrcBlend", (int)BlendMode.One);
                        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 3000;
                    }
                    EditorUtility.SetDirty(material);
                    materials[slot] = material;
                }
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }
    }
}
