using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrashEditor
{
    public static class RealismMaterialLibrary
    {
        public const string TextureRoot = "Assets/CarCrash/Textures/";
        private const string MaterialRoot = "Assets/CarCrash/Materials/Realism/";
        private static readonly Dictionary<string, Material> Cache = new();

        [Serializable] private sealed class MaterialFile { public MaterialData[] materials; }
        [Serializable] private sealed class MaterialData
        {
            public string name;
            public float[] color;
            public float metallic, roughness;
            public float normalStrength, occlusionStrength;
            public float[] normalTiling;
            public int albedo, normal, metalRough, occlusion, emission;
            public float[] emissionColor;
        }

        public static void PrepareTextures()
        {
            Cache.Clear();
            Directory.CreateDirectory(MaterialRoot);
            AssetDatabase.Refresh();
            foreach (var path in Directory.GetFiles(TextureRoot + "Realism")) ConfigureTexture(path.Replace('\\', '/'));
            foreach (var path in Directory.GetFiles(TextureRoot + "Roadbreaker")) ConfigureTexture(path.Replace('\\', '/'));
        }

        private static void ConfigureTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            var normal = path.Contains("nor_gl") || IsVehicleNormal(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = !normal && !path.Contains("smooth") && !path.EndsWith("_ao.png") && !path.EndsWith("_rough.png") && !path.EndsWith(".hdr");
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.maxTextureSize = path.Contains("smooth")||path.EndsWith("_ao.png")||path.EndsWith("_rough.png") ? 1024 : 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static bool IsVehicleNormal(string path)
        {
            if (!path.Contains("Roadbreaker/source_")) return false;
            var info = JsonUtility.FromJson<MaterialFile>(File.ReadAllText(TextureRoot + "Roadbreaker/materials.json"));
            foreach (var data in info.materials)
                if (data.normal >= 0 && path.EndsWith($"source_{data.normal:00}.png", StringComparison.Ordinal)) return true;
            return false;
        }

        public static Material Standard(string name, Color color, float metallic = 0f, float smooth = 0.3f, float emission = 0f)
        {
            if (Cache.TryGetValue(name, out var cached)) return cached;
            var path = MaterialRoot + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = Shader.Find("Standard");
            material.SetFloat("_Mode",0);material.SetInt("_SrcBlend",(int)BlendMode.One);material.SetInt("_DstBlend",(int)BlendMode.Zero);
            material.SetInt("_ZWrite",1);material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.DisableKeyword("_ALPHABLEND_ON");material.renderQueue=-1;
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smooth);
            // The district is already combined by material; retain ordinary forward
            // passes for the imported car and the small number of static batches.
            material.enableInstancing = false;
            if (emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
            else {material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);}
            Cache[name] = material;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material Surface(string name, string source, Color tint, Vector2 tiling, float normalStrength, float smoothness)
        {
            var material = Standard(name, tint, 0f, smoothness);
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + "Realism/" + source + "_diff.png");
            material.mainTextureScale = tiling;
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + "Realism/" + source + "_nor_gl.png"));
            material.SetTextureScale("_BumpMap", tiling);
            material.SetFloat("_BumpScale", normalStrength);
            material.EnableKeyword("_NORMALMAP");
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + "Realism/" + source + "_smooth.png");
            material.SetTexture("_MetallicGlossMap", mask);
            material.SetTextureScale("_MetallicGlossMap", tiling);
            material.SetFloat("_GlossMapScale", smoothness);
            material.EnableKeyword("_METALLICGLOSSMAP");
            var ao=AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+"Realism/"+source+"_ao.png");
            if(ao!=null){material.SetTexture("_OcclusionMap",ao);material.SetTextureScale("_OcclusionMap",tiling);material.SetFloat("_OcclusionStrength",.65f);}
            return material;
        }

        public static Material WorldSurface(string name,string source,Color tint,float tilesPerMetre,float normal,float smooth,float weathering=.4f)
        {
            var material=Surface(name,source,tint,Vector2.one,normal,smooth);
            material.shader=Shader.Find("CarCrash/CitySurface");
            material.SetFloat("_WorldScale",tilesPerMetre);material.SetFloat("_Weathering",weathering);
            return material;
        }

        public static void AutomotivePaint(Material material)
        {
            material.shader=Shader.Find("CarCrash/AutomotivePaint");
            material.SetFloat("_Metallic",.38f);material.SetFloat("_Glossiness",.76f);material.SetFloat("_BumpScale",.018f);
            material.renderQueue=-1;material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        public static Material Window(string name,Color tint,float roomLight)
        {
            var material=Standard(name,tint,0,.9f);
            material.shader=Shader.Find("CarCrash/ArchitecturalGlass");
            material.SetFloat("_RoomLight",roomLight);
            return material;
        }

        public static void ApplyVehicle(GameObject model, bool detailed)
        {
            var sourceMaterials = detailed
                ? JsonUtility.FromJson<MaterialFile>(File.ReadAllText(TextureRoot + "Roadbreaker/materials.json")).materials
                : Array.Empty<MaterialData>();
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    var original = materials[i];
                    if (original == null) continue;
                    var name = original.name.Replace(" (Instance)", "");
                    var safe = name.Replace(' ', '_').Replace('.', '_');
                    MaterialData data = null;
                    foreach (var candidate in sourceMaterials)
                        if (candidate.name == name || candidate.name == "Material" && name.StartsWith("Material")) { data = candidate; break; }
                    var lower = name.ToLowerInvariant();
                    var isPaint = lower.Contains("paint 1") || lower.Contains("trafficpaint");
                    var isGlass = lower.Contains("glass") || lower.Contains("window");
                    var isTire = lower.Contains("tire") || lower.Contains("rubber");
                    var isRim = lower.Contains("rim") || lower.Contains("wheel");
                    var isTail = lower.Contains("brakelight") || lower.Contains("tail") || lower.Contains("red_led");
                    var isLamp = lower.Contains("headlight") || lower.Contains("headlamp");
                    var color = data != null ? new Color(Mathf.LinearToGammaSpace(data.color[0]), Mathf.LinearToGammaSpace(data.color[1]), Mathf.LinearToGammaSpace(data.color[2]), 1f) : original.color;
                    if (isPaint) color = new Color(0.2f, 0.24f, 0.28f);
                    if (isGlass) color = new Color(0.10f, 0.14f, 0.16f, 0.24f);
                    if (isTire) color = new Color(0.07f, 0.075f, 0.08f);
                    if (isRim) color = new Color(0.28f, 0.30f, 0.32f);
                    if (isTail) color = new Color(0.65f, 0.01f, 0.006f);
                    if (isLamp) color = new Color(0.73f, 0.85f, 1f);
                    var material = Standard((detailed ? "M_Roadbreaker_" : "M_CityTraffic_") + safe, color,
                        isPaint ? 0.48f : isRim ? 0.85f : isGlass ? 0f : data?.metallic ?? 0.1f,
                        isPaint ? 0.78f : isGlass ? 0.97f : isTire ? 0.25f : data != null ? 1f - data.roughness : 0.45f,
                        isTail ? 2.5f : isLamp ? 1.3f : 0f);
                    if (data != null)
                    {
                        if (data.albedo >= 0 && !lower.Contains("license"))
                            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + $"Roadbreaker/source_{data.albedo:00}.png");
                        if (data.normal >= 0)
                        {
                            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + $"Roadbreaker/source_{data.normal:00}.png"));
                            material.SetFloat("_BumpScale", isPaint ? 0.035f : data.normalStrength);
                            if (data.normalTiling != null && data.normalTiling.Length == 2)
                            {
                                material.SetTextureScale("_BumpMap", new Vector2(data.normalTiling[0], data.normalTiling[1]));
                                if (data.albedo < 0) material.mainTextureScale = new Vector2(data.normalTiling[0], data.normalTiling[1]);
                            }
                            material.EnableKeyword("_NORMALMAP");
                        }
                        if (data.metalRough >= 0 && !isPaint)
                        {
                            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + $"Roadbreaker/metal_smooth_{data.metalRough:00}.png"));
                            material.SetFloat("_GlossMapScale", 1f);
                            material.EnableKeyword("_METALLICGLOSSMAP");
                        }
                        if (data.occlusion >= 0)
                        {
                            material.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + $"Roadbreaker/source_{data.occlusion:00}.png"));
                            material.SetFloat("_OcclusionStrength", data.occlusionStrength);
                        }
                        if (data.emission >= 0)
                        {
                            material.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + $"Roadbreaker/source_{data.emission:00}.png"));
                            material.EnableKeyword("_EMISSION");
                            if (!isTail && !isLamp) material.SetColor("_EmissionColor", new Color(0.25f, 0.22f, 0.18f));
                        }
                    }
                    if(isPaint)AutomotivePaint(material);
                    if (isGlass)
                    {
                        material.SetFloat("_Mode", 3f);
                        material.SetInt("_SrcBlend", (int)BlendMode.One);
                        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 3000;
                    }
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
            }
        }
    }
}
