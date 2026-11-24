using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    internal sealed class PremiumVehicleMaterials : MonoBehaviour
    {
        private readonly Dictionary<Material, Material> _materials = new();

        internal void Fit(GarageVehicle spec)
        {
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                var slots = renderer.sharedMaterials;
                for (var index = 0; index < slots.Length; index++)
                {
                    var source = slots[index];
                    if (source == null) continue;
                    if (!_materials.TryGetValue(source, out var instance))
                    {
                        instance = Create(source, spec);
                        _materials.Add(source, instance);
                    }
                    slots[index] = instance;
                }
                renderer.sharedMaterials = slots;
            }
        }

        private static Material Create(Material source, GarageVehicle spec)
        {
            var name = source.name;
            var isPaint = name.StartsWith("M_Garage_Paint", StringComparison.Ordinal);
            var shader = Shader.Find(isPaint ? "CarCrash/AutomotivePaint" : "Standard");
            var material = new Material(shader) { name = name, color = source.color };
            material.SetFloat("_Glossiness", .53f);
            if (isPaint)
            {
                material.color = CarCrashHudInput.PaintColors[spec.defaultPaint];
                material.SetFloat("_Metallic", .38f);
                material.SetFloat("_Glossiness", .76f);
                material.SetFloat("_BumpScale", .018f);
            }
            else if (name.Contains("Glass", StringComparison.OrdinalIgnoreCase))
            {
                material.color = new Color(.38f, .50f, .59f, .23f);
                material.SetFloat("_Glossiness", .87f);
                material.SetFloat("_Mode", 3f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (name.Contains("Headlamp", StringComparison.OrdinalIgnoreCase))
                SetEmitter(material, spec.stockHeadlight, .96f);
            else if (name.Contains("Taillamp", StringComparison.OrdinalIgnoreCase))
                SetEmitter(material, new Color(.88f, .045f, .023f), .78f);
            else if (name.Contains("RunningLight", StringComparison.OrdinalIgnoreCase))
                SetEmitter(material, spec.stockHeadlight, 1.16f);
            else if (name.Contains("Marker", StringComparison.OrdinalIgnoreCase))
                SetEmitter(material, new Color(1f, .48f, .08f), .74f);
            else if (name.Contains("Rim", StringComparison.OrdinalIgnoreCase) || name.Contains("Alloy", StringComparison.OrdinalIgnoreCase))
            {
                material.SetFloat("_Metallic", .79f);
                material.SetFloat("_Glossiness", .68f);
            }
            else if (name.Contains("Rubber", StringComparison.OrdinalIgnoreCase))
                material.SetFloat("_Glossiness", .21f);
            return material;
        }

        private static void SetEmitter(Material material, Color color, float strength)
        {
            material.color = color * .63f;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * strength);
        }

        private void OnDestroy()
        {
            foreach (var material in _materials.Values) Destroy(material);
        }
    }
}
