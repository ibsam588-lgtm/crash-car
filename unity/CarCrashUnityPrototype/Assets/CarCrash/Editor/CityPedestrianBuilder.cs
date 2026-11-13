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
    public static class CityPedestrianBuilder
    {
        private const string Models = "Assets/CarCrash/Models/Pedestrians/";
        private const string Textures = "Assets/CarCrash/Textures/Pedestrians/";
        private const string Materials = "Assets/CarCrash/Materials/Pedestrians/";
        [Serializable] private class MaterialList { public MaterialInfo[] materials; }
        [Serializable] private class MaterialInfo
        {
            public string name, baseTexture, normalTexture;
            public float[] color;
            public float roughness;
            public bool cutout;
        }

        public static void Create(Transform player)
        {
            Directory.CreateDirectory(Materials);
            AssetDatabase.Refresh();
            var names = new[] { "maya", "sam", "harper" };
            var prefabs = new GameObject[3];
            var clips = new AnimationClip[3];
            var materials = new Dictionary<string, Material>();
            for (var i = 0; i < names.Length; i++)
            {
                var path = Models + names[i] + "_walker.fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer == null) throw new Exception("Prepare the licensed city pedestrians before building: " + path);
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
                importer.SaveAndReimport();
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal)) { clips[i] = clip; break; }
                if (clips[i] == null) throw new Exception("No walking animation imported for " + names[i]);
                clips[i].wrapMode = WrapMode.Loop;
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var list = JsonUtility.FromJson<MaterialList>(File.ReadAllText(Models + names[i] + "_materials.json"));
                foreach (var info in list.materials)
                {
                    var materialPath = Materials + info.name + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, materialPath); }
                    material.color = new Color(info.color[0], info.color[1], info.color[2], 1f);
                    material.SetFloat("_Glossiness", Mathf.Clamp01(1f - info.roughness));
                    material.SetFloat("_Metallic", 0f);
                    if (!string.IsNullOrEmpty(info.baseTexture))
                    {
                        var texturePath = Textures + info.baseTexture;
                        var textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                        textureImporter.alphaIsTransparency = info.cutout;
                        textureImporter.maxTextureSize = info.name.Contains("03") ? 1024 : 512;
                        textureImporter.SaveAndReimport();
                        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    }
                    if (!string.IsNullOrEmpty(info.normalTexture))
                    {
                        var texturePath = Textures + info.normalTexture;
                        var textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                        textureImporter.textureType = TextureImporterType.NormalMap;
                        textureImporter.SaveAndReimport();
                        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                        material.EnableKeyword("_NORMALMAP");
                        material.SetFloat("_BumpScale", 0.65f);
                    }
                    if (info.cutout)
                    {
                        material.SetFloat("_Mode", 1);
                        material.SetFloat("_Cutoff", 0.35f);
                        material.SetOverrideTag("RenderType", "TransparentCutout");
                        material.SetInt("_SrcBlend", (int)BlendMode.One);
                        material.SetInt("_DstBlend", (int)BlendMode.Zero);
                        material.SetInt("_ZWrite", 1);
                        material.EnableKeyword("_ALPHATEST_ON");
                        material.renderQueue = (int)RenderQueue.AlphaTest;
                    }
                    EditorUtility.SetDirty(material);
                    materials[info.name] = material;
                }
            }
            const string contactPath = Materials + "WalkerContact.physicMaterial";
            var contact = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(contactPath);
            if (contact == null)
            {
                contact = new PhysicsMaterial("WalkerContact");
                AssetDatabase.CreateAsset(contact, contactPath);
            }
            contact.staticFriction = contact.dynamicFriction = 0f;
            contact.frictionCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(contact);
            var templates = new GameObject[3];
            for (var type = 0; type < names.Length; type++)
            {
                var template = CreateBody(prefabs[type], clips[type], materials, contact, type);
                templates[type] = PrefabUtility.SaveAsPrefabAsset(template, Models + names[type] + "_pedestrian.prefab");
                Object.DestroyImmediate(template);
            }
            var root = new GameObject("City pedestrians").transform;
            var index = 0;
            for (var block = 1; block < CityRoadNetwork.CrossStreetZ.Length; block++)
            foreach (var side in new[] { -1f, 1f })
            for (var lane = 0; lane < 2; lane++)
            {
                var type = index % names.Length;
                var x = side * (lane == 0 ? 9.5f : 11.75f);
                var a = new Vector3(x, 0.16f, CityRoadNetwork.CrossStreetZ[block - 1] + 12f);
                var b = new Vector3(x, 0.16f, CityRoadNetwork.CrossStreetZ[block] - 12f);
                var person = (GameObject)PrefabUtility.InstantiatePrefab(templates[type], root);
                person.name = "Pedestrian " + (index + 1) + " / " + names[type];
                person.transform.SetPositionAndRotation(Vector3.Lerp(a, b, lane == 0 ? 0.38f : 0.73f), Quaternion.Euler(0, lane == 0 ? 0 : 180, 0));
                var walker = person.GetComponent<CityPedestrian>();
                walker.pointA = a; walker.pointB = b;
                walker.walkSpeed = (type == 2 ? 1.7f : 1.3f) * (0.92f + lane * 0.1f);
                walker.sourceWalkSpeed = type == 2 ? 1.7f : 1.3f;
                walker.player = player;
                PrefabUtility.RecordPrefabInstancePropertyModifications(person);
                PrefabUtility.RecordPrefabInstancePropertyModifications(person.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(walker);
                index++;
            }
            Debug.Log("Created " + index + " animated city pedestrians on bounded sidewalk routes.");
        }

        private static GameObject CreateBody(GameObject source, AnimationClip clip, Dictionary<string, Material> materials, PhysicsMaterial contact, int type)
        {
            var person = new GameObject("City pedestrian", typeof(Rigidbody), typeof(CapsuleCollider), typeof(CityPedestrian));
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, person.transform);
            model.name = "Animated human";
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.localScale *= (1.7f + type * 0.055f) / bounds.size.y;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.position -= Vector3.up * bounds.min.y;
            foreach (var renderer in renderers)
            {
                var slots = renderer.sharedMaterials;
                for (var slot = 0; slot < slots.Length; slot++)
                    if (slots[slot] != null && materials.TryGetValue(slots[slot].name, out var material)) slots[slot] = material;
                renderer.sharedMaterials = slots;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(model.transform);
            var animation = model.GetComponent<Animation>() ?? model.AddComponent<Animation>();
            animation.AddClip(clip, "Walk");
            animation.clip = clip;
            animation.playAutomatically = false;
            animation.wrapMode = WrapMode.Loop;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animation);
            var body = person.GetComponent<Rigidbody>();
            body.mass = 75f;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.linearDamping = 0.3f;
            var capsule = person.GetComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 0.85f, 0);
            capsule.height = 1.7f;
            capsule.radius = 0.23f;
            capsule.sharedMaterial = contact;
            person.GetComponent<CityPedestrian>().walkClip = clip;
            return person;
        }
    }
}
