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
            var performers = 0;
            foreach (var avenue in CityRoadNetwork.AvenueX)
            for (var block = 1; block < CityRoadNetwork.CrossStreetZ.Length; block++)
            foreach (var side in new[] { -1f, 1f })
            for (var lane = 0; lane < 1; lane++)
            {
                if(Mathf.Abs(avenue)>54 && block%2!=Array.IndexOf(CityRoadNetwork.AvenueX,avenue)%2)continue;
                var type = index % names.Length;
                var x = avenue + side * 11.8f;
                var a = new Vector3(x, 0.16f, CityRoadNetwork.CrossStreetZ[block - 1] + 12f);
                var b = new Vector3(x, 0.16f, CityRoadNetwork.CrossStreetZ[block] - 12f);
                var person = (GameObject)PrefabUtility.InstantiatePrefab(templates[type], root);
                var jogger = index % 6 == 3;
                var dogWalker = index % 8 == 1;
                var role = jogger ? "Jogger" : dogWalker ? "Dog walker" : "Pedestrian";
                person.name = role + " " + (index + 1) + " / " + names[type];
                person.transform.SetPositionAndRotation(Vector3.Lerp(a, b, lane == 0 ? 0.38f : 0.73f), Quaternion.Euler(0, lane == 0 ? 0 : 180, 0));
                var walker = person.GetComponent<CityPedestrian>();
                walker.pointA = a; walker.pointB = b;
                walker.walkSpeed = (type == 2 ? 1.7f : 1.3f) * (0.92f + lane * 0.1f);
                if (jogger) walker.walkSpeed *= 1.86f;
                else if (dogWalker) walker.walkSpeed *= .89f;
                walker.sourceWalkSpeed = type == 2 ? 1.7f : 1.3f;
                walker.player = player;
                walker.avenueCenterX = avenue;
                PrefabUtility.RecordPrefabInstancePropertyModifications(person);
                PrefabUtility.RecordPrefabInstancePropertyModifications(person.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(walker);
                if (dogWalker) AddLeashedDog(person.transform,index);
                if (Mathf.Abs(avenue) <= 54f && (block + (side > 0f ? 1 : 0)) % 3 == 0)
                {
                    var center = Vector3.Lerp(a,b,.67f);
                    AddStreetPerformer(templates[(type+1)%templates.Length],root,player,center,side);
                    performers++;
                }
                index++;
            }
            Debug.Log("Created " + index + " animated city pedestrians on bounded sidewalk routes.");
            var attendants=0;
            foreach(var shop in Object.FindObjectsByType<CityBuildingProfile>(FindObjectsSortMode.None))
            {
                if(!shop.hasInterior||shop.plot%2!=1)continue;
                var actor=CreateStreetActor(templates[attendants%templates.Length],root,player,"Shop attendant / "+shop.shop);
                var person=actor.gameObject;
                person.transform.position=shop.transform.TransformPoint(shop.attendantLocalPosition);
                person.transform.rotation=Quaternion.LookRotation(Vector3.left*shop.side);
                actor.shop=shop;
                actor.escapePoint=shop.transform.TransformPoint(shop.egressLocalPosition+new Vector3(-shop.side*.55f,0f,6.1f));
                attendants++;
            }
            Debug.Log("Created "+attendants+" shop attendants with clear exit routes and "+performers+" street performers.");
        }

        private static CityStreetActor CreateStreetActor(GameObject template,Transform root,Transform player,string name)
        {
            var actorObject=(GameObject)PrefabUtility.InstantiatePrefab(template,root);
            actorObject.name=name;
            var walker=actorObject.GetComponent<CityPedestrian>();
            var clip=walker.walkClip;
            Object.DestroyImmediate(walker);
            Object.DestroyImmediate(actorObject.GetComponent<Collider>());
            Object.DestroyImmediate(actorObject.GetComponent<Rigidbody>());
            var actor=actorObject.AddComponent<CityStreetActor>();
            actor.poseClip=clip;
            actor.player=player;
            return actor;
        }

        private static void AddStreetPerformer(GameObject template,Transform root,Transform player,Vector3 center,float side)
        {
            var actor=CreateStreetActor(template,root,player,"Street performer / juggler");
            actor.transform.SetPositionAndRotation(center+Vector3.left*(side*.20f),Quaternion.LookRotation(Vector3.left*side));
            actor.escapePoint=center+Vector3.back*6f;
            actor.performanceProps=new Transform[3];
            var palette=new[] {new Color(.88f,.24f,.12f),new Color(.11f,.69f,.79f),new Color(.92f,.66f,.13f)};
            for(var i=0;i<actor.performanceProps.Length;i++)
            {
                var material=RealismMaterialLibrary.Standard("M_Street_Juggling_Ball_"+i,palette[i],0f,.47f);
                actor.performanceProps[i]=ActorPrimitive("Juggling ball " + (i+1),PrimitiveType.Sphere,actor.transform, new Vector3(i*.18f-.18f,1.4f,.46f),Vector3.one*.13f,material);
            }
            var fabric=RealismMaterialLibrary.Standard("M_Street_Performer_Mat",new Color(.08f,.27f,.29f),0f,.24f);
            ActorPrimitive("Street performance mat",PrimitiveType.Cube,root,center+Vector3.up*.004f,new Vector3(.87f,.014f,1.75f),fabric).position=center+Vector3.up*.004f;
        }

        private static void AddLeashedDog(Transform owner,int index)
        {
            var colors=new[] {new Color(.38f,.22f,.12f),new Color(.64f,.47f,.28f),new Color(.23f,.17f,.13f)};
            var variant=(index/8)%colors.Length;
            var coat=RealismMaterialLibrary.Standard("M_Dog_Coat_"+variant,colors[variant],0f,.34f);
            var cream=RealismMaterialLibrary.Standard("M_Dog_Chest",new Color(.77f,.69f,.55f),0f,.37f);
            var dark=RealismMaterialLibrary.Standard("M_Dog_Nose",new Color(.06f,.043f,.034f),0f,.61f);
            var collar=RealismMaterialLibrary.Standard("M_Dog_Collar",new Color(.075f,.45f,.56f),0f,.4f);
            var dog=new GameObject("Leashed sidewalk dog").transform;
            dog.SetParent(owner,false);
            dog.localPosition=new Vector3(.53f,0f,.50f);
            var companion=dog.gameObject.AddComponent<CityLeashedDog>();
            companion.owner=owner.GetComponent<Rigidbody>();

            void Shape(string name,Transform parent,Vector3 point,Vector3 size,Material material) =>
                ActorPrimitive(name,PrimitiveType.Sphere,parent,point,size,material);

            Shape("Body",dog,new Vector3(0f,.40f,-.015f),new Vector3(.32f,.30f,.68f),coat);
            Shape("Chest",dog,new Vector3(0f,.39f,.19f),new Vector3(.28f,.32f,.28f),cream);
            Shape("Rump",dog,new Vector3(0f,.38f,-.26f),new Vector3(.31f,.29f,.28f),coat);
            Shape("Neck",dog,new Vector3(0f,.50f,.25f),new Vector3(.205f,.235f,.22f),coat);
            Shape("Head",dog,new Vector3(0f,.59f,.34f),new Vector3(.25f,.245f,.25f),coat);
            Shape("Muzzle",dog,new Vector3(0f,.539f,.475f),new Vector3(.155f,.115f,.218f),cream);
            Shape("Nose",dog,new Vector3(0f,.564f,.573f),new Vector3(.099f,.073f,.061f),dark);
            Shape("Collar",dog,new Vector3(0f,.493f,.272f),new Vector3(.225f,.065f,.241f),collar);
            foreach(var side in new[] {-1f,1f})
            {
                Shape("Eye",dog,new Vector3(side*.091f,.625f,.423f),Vector3.one*.039f,dark);
                Shape("Ear",dog,new Vector3(side*.101f,.635f,.296f),new Vector3(.080f,.22f,.105f),coat);
            }
            companion.legs=new Transform[4];
            for(var leg=0;leg<companion.legs.Length;leg++)
            {
                var pivot=new GameObject("Leg pivot "+leg).transform;
                pivot.SetParent(dog,false);
                pivot.localPosition=new Vector3(leg%2==0?-.108f:.108f,.33f,leg<2?.206f:-.234f);
                companion.legs[leg]=pivot;
                Shape("Upper leg",pivot,new Vector3(0f,-.067f,0f),new Vector3(.105f,.194f,.112f),coat);
                Shape("Lower leg",pivot,new Vector3(0f,-.202f,0f),new Vector3(.072f,.159f,.078f),coat);
                Shape("Paw",pivot,new Vector3(0f,-.299f,.019f),new Vector3(.102f,.064f,.118f),cream);
            }
            companion.tail=new GameObject("Tail pivot").transform;
            companion.tail.SetParent(dog,false);
            companion.tail.localPosition = new Vector3(0f,.44f,-.33f);
            var tail=ActorPrimitive("Tail",PrimitiveType.Sphere,companion.tail,new Vector3(0f,.085f,-.126f),new Vector3(.072f,.087f,.34f),coat);
            tail.localRotation=Quaternion.Euler(31f,0f,0f);

            var leash=new GameObject("Leash",typeof(LineRenderer));
            leash.transform.SetParent(dog,false);
            companion.leash=leash.GetComponent<LineRenderer>();
            companion.leash.sharedMaterial=dark;
            companion.leash.positionCount=3;
            companion.leash.widthMultiplier=.013f;
            companion.leash.shadowCastingMode=ShadowCastingMode.Off;
        }

        private static Transform ActorPrimitive(string name,PrimitiveType type,Transform parent,Vector3 point,Vector3 size,Material material)
        {
            var obj=GameObject.CreatePrimitive(type);
            obj.name=name;
            obj.transform.SetParent(parent,false);
            obj.transform.localPosition=point;
            obj.transform.localScale=size;
            obj.GetComponent<MeshRenderer>().sharedMaterial=material;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj.transform;
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
