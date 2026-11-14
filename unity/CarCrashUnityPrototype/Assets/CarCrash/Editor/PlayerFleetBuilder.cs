using System;
using System.Collections.Generic;
using System.IO;
using CarCrash;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CarCrashEditor
{
    public static class PlayerFleetBuilder
    {
        private const string Root = "Assets/CarCrash/";
        private const string Prefabs = Root + "Prefabs/Garage/";
        [Serializable] private class ModelData { public MatData[] materials; public float wheelRadius; }
        [Serializable] private class MatData { public string name, role, baseTexture, normalTexture; public float[] color; public float roughness; }

        public static void Create(GameObject player, GameObject originalVisual)
        {
            Directory.CreateDirectory(Prefabs);
            Directory.CreateDirectory(Root + "Materials/Garage");
            AssetDatabase.Refresh();
            var specs = new List<GarageVehicle>();
            var road = Object.Instantiate(originalVisual);
            road.transform.localPosition = originalVisual.transform.localPosition;
            road.transform.localRotation = originalVisual.transform.localRotation;
            road.transform.localScale = originalVisual.transform.localScale;
            foreach (var damage in road.GetComponentsInChildren<CrashDamageVisual>()) Object.DestroyImmediate(damage);
            var roadSpec = Save(road, "roadbreaker", "ROADBREAKER GT", "PERFORMANCE COUPE", 192, 1350, 7.2f);
            roadSpec.wheelCenters = new[] { new Vector3(-.975f,.39f,1.49f), new Vector3(.975f,.39f,1.49f), new Vector3(-.985f,.39f,-1.31f), new Vector3(.985f,.39f,-1.31f) };
            roadSpec.wheelRadius = .385f; roadSpec.colliderCenter = new Vector3(0,.5f,0); roadSpec.colliderSize = new Vector3(2.04f,.9f,4.38f);
            specs.Add(roadSpec);
            var gt = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Models/traffic_gt_detailed.fbx"));
            TrafficMaterialLibrary.Apply(gt, new Color(.18f,.24f,.31f));
            specs.Add(Save(gt,"apex","APEX GT","MID ENGINE GT",205,1420,7.7f));
            var ids = new[] { "suv", "pickup", "hauler", "monster" };
            var names = new[] { "SUMMIT AWD", "RIDGEBACK 4X4", "ATLAS HAULER", "IRONCLAW MONSTER" };
            var classes = new[] { "SPORT UTILITY", "CREW CAB PICKUP", "CARGO TRUCK", "LIFTED MONSTER TRUCK" };
            var speeds = new[] { 165f, 155f, 118f, 138f };
            var weights = new[] { 1900f, 2350f, 5400f, 4300f };
            var acceleration = new[] { 5.8f, 5.4f, 3.8f, 5f };
            for (var i = 0; i < ids.Length; i++)
            {
                var path = Root + "Models/Garage/" + ids[i];
                var data = JsonUtility.FromJson<ModelData>(File.ReadAllText(path + ".json"));
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path + ".fbx");
                if (source == null) throw new Exception("Prepare the garage model before building: " + path);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
                ApplyMaterials(model, data);
                var spec = Save(model, ids[i], names[i], classes[i], speeds[i], weights[i], acceleration[i]);
                // World-space wheel roots in the cooked FBX are the authoritative pivots.
                spec.wheelRadius = data.wheelRadius;
                spec.suspension = ids[i] == "monster" ? .42f : ids[i] == "hauler" ? .24f : .25f;
                spec.spring = weights[i] * (ids[i] == "monster" ? 27f : 34f);
                spec.damper = weights[i] * 3.7f;
                spec.steering = ids[i] == "hauler" ? 34f : ids[i] == "monster" ? 32f : 30f;
                spec.reverseSpeed = ids[i] == "hauler" ? 22f : 28f;
                spec.allWheelDrive = ids[i] is "suv" or "pickup" or "monster";
                spec.colliderSize = new Vector3(spec.size.x * .86f, spec.size.y * .74f, spec.size.z * .95f);
                spec.colliderCenter = new Vector3(0, spec.size.y * .54f, 0);
                specs.Add(spec);
            }
            foreach(var spec in specs)
            {
                var top=spec.colliderCenter.y+spec.colliderSize.y*.5f;
                // The coupe's wheel suspension can lower its root below road level.
                // Keep the collision hull above a normal pavement lip at full load.
                var bottom=Mathf.Max(.36f,spec.colliderCenter.y-spec.colliderSize.y*.5f);
                spec.colliderCenter.y=(top+bottom)*.5f;
                spec.colliderSize.y=top-bottom;
            }
            const string glidePath=Root+"Materials/Garage/VehicleGlide.physicMaterial";
            var glide=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(glidePath);
            if(glide==null){glide=new PhysicsMaterial("VehicleGlide");AssetDatabase.CreateAsset(glide,glidePath);}
            glide.staticFriction=.08f;glide.dynamicFriction=.05f;glide.frictionCombine=PhysicsMaterialCombine.Minimum;
            glide.bounciness=0;EditorUtility.SetDirty(glide);
            player.GetComponent<BoxCollider>().sharedMaterial=glide;
            player.GetComponent<Rigidbody>().solverIterations=10;
            player.GetComponent<Rigidbody>().solverVelocityIterations=4;
            var garage = player.AddComponent<VehicleGarage>();
            garage.vehicles = specs.ToArray();
            garage.visualRoot = originalVisual.transform;
            var effects = player.AddComponent<VehicleImpactEffects>();
            effects.particleMaterial = ImpactMaterial("M_ImpactParticles", false);
            effects.ringMaterial = ImpactMaterial("M_ImpactGroundSplash", true);
        }

        private static GarageVehicle Save(GameObject model, string id, string name, string category, float speed, float mass, float acceleration)
        {
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var spec = new GarageVehicle { id=id, displayName=name, category=category, topSpeed=speed, mass=mass, acceleration=acceleration,
                size=bounds.size, colliderCenter=new Vector3(0,bounds.size.y*.48f,0), colliderSize=new Vector3(bounds.size.x*.9f,bounds.size.y*.72f,bounds.size.z*.94f) };
            var keys = new[] { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };
            for (var i = 0; i < keys.Length; i++)
                foreach (var child in model.GetComponentsInChildren<Transform>())
                    if (child.name == keys[i])
                    {
                        spec.wheelCenters[i] = child.position;
                        var tires = child.GetComponentsInChildren<MeshRenderer>();
                        if (tires.Length > 0) spec.wheelRadius = Mathf.Max(tires[0].bounds.size.y, tires[0].bounds.size.z) * .5f;
                        break;
                    }
            // Keep the importer-provided axis/unit transform in the prefab.
            spec.visualPrefab = PrefabUtility.SaveAsPrefabAsset(model, Prefabs + id + ".prefab");
            Object.DestroyImmediate(model);
            return spec;
        }

        private static void ApplyMaterials(GameObject model, ModelData data)
        {
            var materials = new Dictionary<string, Material>();
            foreach (var info in data.materials)
            {
                var path = Root + "Materials/Garage/" + info.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material,path); }
                material.shader=Shader.Find("Standard");
                material.color = new Color(info.color[0],info.color[1],info.color[2],info.role == "Glass" ? .28f : 1f);
                material.SetFloat("_Metallic", info.role is "Alloy" or "Steel" ? .8f : info.role == "Paint" ? .45f : 0f);
                material.SetFloat("_Glossiness", info.role == "Paint" ? .76f : info.role == "Tire" ? .2f : 1f-info.roughness);
                if(info.role=="Paint")RealismMaterialLibrary.AutomotivePaint(material);
                if(info.role=="Tire")material.color=new Color(.055f,.06f,.065f);
                if(info.role=="Glass")material.color=new Color(.1f,.14f,.16f,.28f);
                if (!string.IsNullOrEmpty(info.baseTexture)) material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Textures/Garage/" + info.baseTexture);
                if (!string.IsNullOrEmpty(info.normalTexture))
                {
                    var texturePath = Root + "Textures/Garage/" + info.normalTexture;
                    var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                    importer.textureType = TextureImporterType.NormalMap; importer.SaveAndReimport();
                    material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); material.EnableKeyword("_NORMALMAP");
                }
                if (info.role == "Glass")
                {
                    material.SetFloat("_Mode",3); material.SetInt("_SrcBlend",(int)BlendMode.One); material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite",0); material.EnableKeyword("_ALPHAPREMULTIPLY_ON"); material.renderQueue=3000;
                }
                if (info.role is "Headlamp" or "Taillamp" or "Amber")
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor",info.role == "Taillamp" ? new Color(.7f,.015f,.01f) : new Color(.6f,.63f,.56f));
                }
                EditorUtility.SetDirty(material); materials[info.name]=material;
            }
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                var slots=renderer.sharedMaterials;
                for (var i=0;i<slots.Length;i++) if (slots[i]!=null && materials.TryGetValue(slots[i].name,out var material)) slots[i]=material;
                renderer.sharedMaterials=slots;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static Material ImpactMaterial(string name, bool ring)
        {
            var path=Root+"Materials/Garage/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material==null) { material=new Material(Shader.Find("CarCrash/ImpactParticle")); AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_Color",Color.white); material.SetFloat("_Ring",ring ? 1f : 0f); EditorUtility.SetDirty(material);
            return material;
        }
    }
}
