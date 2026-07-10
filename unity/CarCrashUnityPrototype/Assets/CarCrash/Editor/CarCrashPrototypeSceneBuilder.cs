using CarCrash;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrashEditor
{
    public static class CarCrashPrototypeSceneBuilder
    {
        private const string ModelPath = "Assets/CarCrash/Models/car_crash_gt650.fbx";
        private const string CityBackdropTexturePath = "Assets/CarCrash/Textures/option_c_city_backdrop.png";
        private const string GeneratedTextureDirectory = "Assets/CarCrash/Generated";
        private const string RoadTexturePath = GeneratedTextureDirectory + "/wet_asphalt_albedo.png";
        private const string ConcreteTexturePath = GeneratedTextureDirectory + "/weathered_concrete_albedo.png";
        private const string ScenePath = "Assets/CarCrash/Scenes/ImpactRunPrototype.unity";
        private const string WebGlBuildPath = "Builds/WebGL";

        [MenuItem("Car Crash/Build Production Impact Run Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ConfigureProject();
            CreateLightingAndAtmosphere();
            CreateDowntownArena();
            var player = CreatePlayerVehicle();
            CreateTraffic(player.model);
            CreateCameraAndGameSystems(player.root, player.body, player.vehicle, player.scoring);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Built production Impact Run scene at {ScenePath}");
        }

        [MenuItem("Car Crash/Build Production WebGL")]
        public static void BuildWebGl()
        {
            BuildScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            PlayerSettings.companyName = "Car Crash Studio";
            PlayerSettings.productName = "Car Crash";
            PlayerSettings.defaultScreenWidth = 720;
            PlayerSettings.defaultScreenHeight = 1280;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = false;

            Directory.CreateDirectory(WebGlBuildPath);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = WebGlBuildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"WebGL build failed: {report.summary.result}");
            }

            PatchWebGlIndex();
            Debug.Log($"Built production WebGL game at {WebGlBuildPath}");
        }

        private static void ConfigureProject()
        {
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowDistance = 95f;
            QualitySettings.realtimeReflectionProbes = true;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0065f;
            RenderSettings.fogColor = new Color(0.31f, 0.25f, 0.22f);

            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer != null)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importBlendShapes = true;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            ConfigureTexture(CityBackdropTexturePath, 4096);
            EnsureGeneratedSurfaceTextures();
        }

        private static void ConfigureTexture(string path, int maxSize)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void EnsureGeneratedSurfaceTextures()
        {
            Directory.CreateDirectory(GeneratedTextureDirectory);
            WriteSurfaceTexture(RoadTexturePath, true, 7319);
            WriteSurfaceTexture(ConcreteTexturePath, false, 1927);
            AssetDatabase.Refresh();

            foreach (var path in new[] { RoadTexturePath, ConcreteTexturePath })
            {
                ConfigureTexture(path, 1024);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.filterMode = FilterMode.Trilinear;
                    importer.anisoLevel = 6;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void WriteSurfaceTexture(string path, bool asphalt, int seed)
        {
            const int size = 512;
            var random = new System.Random(seed);
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var broad = Mathf.PerlinNoise(x * 0.018f + seed, y * 0.018f + seed * 0.1f);
                    var grain = Mathf.PerlinNoise(x * 0.19f + 8.2f, y * 0.21f + 3.7f);
                    var speckle = (float)random.NextDouble();
                    Color color;

                    if (asphalt)
                    {
                        var value = 0.29f + broad * 0.12f + grain * 0.07f;
                        if (speckle < 0.007f)
                        {
                            value += 0.27f;
                        }
                        else if (speckle > 0.994f)
                        {
                            value -= 0.16f;
                        }

                        var crackWave = x + Mathf.RoundToInt(Mathf.PerlinNoise(y * 0.028f, seed * 0.001f) * 95f);
                        if (Mathf.Abs((crackWave % 211) - 105) < 1 && y % 7 != 0)
                        {
                            value *= 0.42f;
                        }
                        color = new Color(value * 0.91f, value * 0.94f, value, 1f);
                    }
                    else
                    {
                        var value = 0.55f + broad * 0.16f + grain * 0.05f;
                        if (y % 128 < 2 || x % 192 < 2)
                        {
                            value *= 0.63f;
                        }
                        if (speckle < 0.012f)
                        {
                            value *= 0.76f;
                        }
                        color = new Color(value, value * 0.95f, value * 0.89f, 1f);
                    }

                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void CreateLightingAndAtmosphere()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.46f, 0.33f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.24f, 0.27f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.065f, 0.06f);
            RenderSettings.reflectionIntensity = 0.88f;

            var sun = new GameObject("Sunset_Key_Light");
            var sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = 1.42f;
            sunLight.color = new Color(1f, 0.70f, 0.43f);
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.82f;
            sun.transform.rotation = Quaternion.Euler(27f, -18f, 0f);

            var fill = new GameObject("Sky_Fill_Light");
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.38f;
            fillLight.color = new Color(0.38f, 0.55f, 0.82f);
            fill.transform.rotation = Quaternion.Euler(48f, 156f, 0f);

            var probeObject = new GameObject("Downtown_Reflection_Probe");
            probeObject.transform.position = new Vector3(0f, 4f, 34f);
            var probe = probeObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.resolution = 128;
            probe.size = new Vector3(26f, 14f, 110f);
            probe.intensity = 0.78f;
        }

        private static void CreateDowntownArena()
        {
            var asphalt = CreateMaterial("M_Wet_Asphalt", new Color(0.055f, 0.058f, 0.061f), 0.08f, 0.72f);
            var concrete = CreateMaterial("M_Concrete", new Color(0.33f, 0.31f, 0.29f), 0f, 0.34f);
            var laneWhite = CreateMaterial("M_Lane_White", new Color(0.88f, 0.86f, 0.78f), 0f, 0.24f);
            var laneYellow = CreateMaterial("M_Lane_Yellow", new Color(1f, 0.58f, 0.025f), 0f, 0.23f);
            var darkMetal = CreateMaterial("M_Dark_Metal", new Color(0.025f, 0.028f, 0.032f), 0.72f, 0.34f);
            var glass = CreateMaterial("M_Building_Glass", new Color(0.055f, 0.12f, 0.15f), 0.58f, 0.76f);
            var windowGlow = CreateEmissiveMaterial("M_Window_Glow", new Color(0.96f, 0.43f, 0.12f), 1.35f);
            var shopGlow = CreateEmissiveMaterial("M_Shop_Glow", new Color(0.08f, 0.58f, 0.72f), 0.85f);

            asphalt.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RoadTexturePath);
            asphalt.mainTextureScale = new Vector2(3.2f, 38f);
            concrete.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteTexturePath);
            concrete.mainTextureScale = new Vector2(2.2f, 34f);

            var road = CreateBox("Downtown_Wet_Road", new Vector3(0f, -0.08f, 70f), new Vector3(18f, 0.16f, 210f), asphalt);
            road.isStatic = true;

            foreach (var x in new[] { -10.8f, 10.8f })
            {
                CreateBox(x < 0 ? "Left_Sidewalk" : "Right_Sidewalk", new Vector3(x, 0.06f, 70f), new Vector3(3.6f, 0.24f, 210f), concrete).isStatic = true;
                CreateBox(x < 0 ? "Left_Curb" : "Right_Curb", new Vector3(x + (x < 0 ? 1.9f : -1.9f), 0.20f, 70f), new Vector3(0.22f, 0.36f, 210f), concrete).isStatic = true;
            }

            for (var i = 0; i < 54; i++)
            {
                var z = -20f + i * 4.2f;
                foreach (var x in new[] { -2.55f, 2.55f })
                {
                    var marker = CreateBox($"Lane_Marker_{i:00}_{x:+0.0;-0.0}", new Vector3(x, 0.018f, z), new Vector3(0.11f, 0.025f, 2.15f), laneWhite);
                    marker.isStatic = true;
                }
            }

            foreach (var x in new[] { -0.16f, 0.16f })
            {
                var center = CreateBox($"Center_Yellow_{x:+0.00;-0.00}", new Vector3(x, 0.022f, 70f), new Vector3(0.08f, 0.03f, 210f), laneYellow);
                center.isStatic = true;
            }

            CreateSkylineBackdrop();
            CreateBuildingRow(-15.2f, true, concrete, glass, windowGlow, shopGlow);
            CreateBuildingRow(15.2f, false, concrete, glass, windowGlow, shopGlow);
            CreateStreetFurniture(darkMetal, windowGlow);
            CreateOverpass(darkMetal, concrete, windowGlow);
            CreateCrashZone(concrete, darkMetal);
            CreateRoadSurfaceDetails(laneWhite);
            CreateStreetTrees();
        }

        private static void CreateSkylineBackdrop()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CityBackdropTexturePath);
            if (texture == null)
            {
                return;
            }

            var material = new Material(Shader.Find("Unlit/Texture"))
            {
                name = "M_Sunset_Skyline",
                mainTexture = texture
            };
            material.mainTextureScale = new Vector2(1f, 0.52f);
            material.mainTextureOffset = new Vector2(0f, 0.48f);

            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "Distant_Sunset_Skyline";
            backdrop.transform.position = new Vector3(0f, 24f, 174f);
            backdrop.transform.localScale = new Vector3(104f, 72f, 1f);
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(backdrop.GetComponent<Collider>());
        }

        private static void CreateBuildingRow(float x, bool left, Material concrete, Material glass, Material windowGlow, Material shopGlow)
        {
            for (var i = 0; i < 15; i++)
            {
                var z = -8f + i * 12.3f + (left ? 0f : 5.4f);
                var width = 6.8f + (i % 3) * 1.1f;
                var depth = 9.0f + (i % 2) * 1.8f;
                var height = 8.5f + (i % 5) * 2.7f;
                var bodyColor = Color.Lerp(new Color(0.18f, 0.17f, 0.16f), new Color(0.38f, 0.34f, 0.30f), (i % 4) / 3f);
                var bodyMaterial = CreateMaterial($"M_Building_{(left ? "L" : "R")}_{i:00}", bodyColor, 0.04f, 0.30f);
                bodyMaterial.mainTexture = concrete.mainTexture;
                bodyMaterial.mainTextureScale = new Vector2(2.1f + (i % 3) * 0.45f, 4.6f + (i % 4) * 0.7f);
                var building = CreateBox($"Building_{(left ? "L" : "R")}_{i:00}", new Vector3(x, height * 0.5f, z), new Vector3(width, height, depth), bodyMaterial);
                building.isStatic = true;

                var facadeX = x + (left ? width * 0.505f : -width * 0.505f);
                var paneDepth = depth * 0.18f;
                var rows = Mathf.Clamp(Mathf.FloorToInt(height / 1.6f), 3, 10);
                for (var row = 0; row < rows; row++)
                {
                    for (var column = -2; column <= 2; column++)
                    {
                        var pane = CreateBox(
                            $"Window_{(left ? "L" : "R")}_{i:00}_{row:00}_{column:+0;-0;0}",
                            new Vector3(facadeX, 1.6f + row * 1.45f, z + column * paneDepth),
                            new Vector3(0.045f, 0.72f, paneDepth * 0.58f),
                            (row + column + i) % 4 == 0 ? windowGlow : glass
                        );
                        UnityEngine.Object.DestroyImmediate(pane.GetComponent<Collider>());
                        pane.isStatic = true;
                    }
                }

                if (i % 2 == 0)
                {
                    var shop = CreateBox($"Shopfront_{(left ? "L" : "R")}_{i:00}", new Vector3(facadeX, 1.25f, z), new Vector3(0.12f, 2.25f, depth * 0.72f), shopGlow);
                    UnityEngine.Object.DestroyImmediate(shop.GetComponent<Collider>());
                    var awning = CreateBox($"Awning_{(left ? "L" : "R")}_{i:00}", new Vector3(facadeX + (left ? 0.55f : -0.55f), 2.75f, z), new Vector3(1.15f, 0.14f, depth * 0.78f), concrete);
                    awning.isStatic = true;
                }
            }
        }

        private static void CreateStreetFurniture(Material darkMetal, Material lampGlow)
        {
            for (var i = 0; i < 18; i++)
            {
                var z = -4f + i * 10.5f;
                foreach (var x in new[] { -8.8f, 8.8f })
                {
                    CreateBox($"Lamp_Post_{i:00}_{x:+0;-0}", new Vector3(x, 2.7f, z), new Vector3(0.12f, 5.4f, 0.12f), darkMetal);
                    CreateBox($"Lamp_Arm_{i:00}_{x:+0;-0}", new Vector3(x + (x < 0 ? 0.65f : -0.65f), 5.25f, z), new Vector3(1.25f, 0.10f, 0.10f), darkMetal);
                    var lamp = CreateBox($"Lamp_Glow_{i:00}_{x:+0;-0}", new Vector3(x + (x < 0 ? 1.15f : -1.15f), 5.12f, z), new Vector3(0.38f, 0.08f, 0.22f), lampGlow);
                    UnityEngine.Object.DestroyImmediate(lamp.GetComponent<Collider>());
                }
            }
        }

        private static void CreateOverpass(Material darkMetal, Material concrete, Material signalGlow)
        {
            CreateBox("Concrete_Overpass", new Vector3(0f, 5.25f, 58f), new Vector3(26f, 0.72f, 5.4f), concrete);
            foreach (var x in new[] { -9.5f, 9.5f })
            {
                CreateBox($"Overpass_Pillar_{x:+0;-0}", new Vector3(x, 2.55f, 58f), new Vector3(1.35f, 5.1f, 1.35f), concrete);
            }

            CreateBox("Signal_Gantry", new Vector3(0f, 6.5f, 29f), new Vector3(17f, 0.13f, 0.16f), darkMetal);
            foreach (var x in new[] { -4.5f, 0f, 4.5f })
            {
                CreateBox($"Signal_Box_{x:+0.0;-0.0;0}", new Vector3(x, 6.05f, 29f), new Vector3(0.42f, 0.82f, 0.24f), darkMetal);
                var glow = CreateSphere($"Signal_Green_{x:+0.0;-0.0;0}", new Vector3(x, 5.82f, 28.84f), 0.16f, signalGlow);
                UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());
            }
        }

        private static void CreateCrashZone(Material concrete, Material darkMetal)
        {
            for (var i = 0; i < 8; i++)
            {
                var x = i % 2 == 0 ? -7.1f : 7.1f;
                var barrier = CreateBox($"Crash_Barrier_{i:00}", new Vector3(x, 0.42f, 74f + i * 7f), new Vector3(1.0f, 0.84f, 4.8f), i % 3 == 0 ? darkMetal : concrete);
                var body = barrier.AddComponent<Rigidbody>();
                body.mass = 920f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }

        private static void CreateRoadSurfaceDetails(Material laneWhite)
        {
            foreach (var crossingZ in new[] { 24f, 89f, 142f })
            {
                for (var x = -7.25f; x <= 7.25f; x += 1.45f)
                {
                    var stripe = CreateBox($"Crosswalk_{crossingZ:000}_{x:+0.00;-0.00}", new Vector3(x, 0.024f, crossingZ), new Vector3(0.76f, 0.024f, 2.25f), laneWhite);
                    stripe.isStatic = true;
                    UnityEngine.Object.DestroyImmediate(stripe.GetComponent<Collider>());
                }
            }

            var puddleMaterial = CreateMaterial("M_Road_Puddles", new Color(0.028f, 0.045f, 0.055f), 0.18f, 0.96f);
            var puddles = new[]
            {
                new Vector4(-5.7f, 9f, 1.6f, 4.8f), new Vector4(4.8f, 17f, 1.2f, 3.2f),
                new Vector4(-3.6f, 43f, 1.0f, 5.6f), new Vector4(5.9f, 63f, 1.5f, 4.1f),
                new Vector4(-6.2f, 103f, 1.3f, 3.9f), new Vector4(4.4f, 126f, 1.1f, 5.2f)
            };
            for (var i = 0; i < puddles.Length; i++)
            {
                var data = puddles[i];
                var puddle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puddle.name = $"Road_Puddle_{i:00}";
                puddle.transform.position = new Vector3(data.x, 0.01f, data.y);
                puddle.transform.localScale = new Vector3(data.z, 0.012f, data.w);
                puddle.GetComponent<MeshRenderer>().sharedMaterial = puddleMaterial;
                UnityEngine.Object.DestroyImmediate(puddle.GetComponent<Collider>());
                puddle.isStatic = true;
            }
        }

        private static void CreateStreetTrees()
        {
            var trunk = CreateMaterial("M_Tree_Trunk", new Color(0.11f, 0.065f, 0.038f), 0f, 0.22f);
            var leaves = CreateMaterial("M_Tree_Foliage", new Color(0.055f, 0.15f, 0.075f), 0f, 0.30f);
            for (var i = 0; i < 10; i++)
            {
                var z = 5f + i * 17.2f;
                foreach (var x in new[] { -10.15f, 10.15f })
                {
                    var trunkObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    trunkObject.name = $"Street_Tree_Trunk_{i:00}_{x:+0;-0}";
                    trunkObject.transform.position = new Vector3(x, 1.3f, z + (x < 0 ? 0f : 5.2f));
                    trunkObject.transform.localScale = new Vector3(0.23f, 1.3f, 0.23f);
                    trunkObject.GetComponent<MeshRenderer>().sharedMaterial = trunk;
                    UnityEngine.Object.DestroyImmediate(trunkObject.GetComponent<Collider>());

                    for (var cluster = 0; cluster < 3; cluster++)
                    {
                        var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        canopy.name = $"Street_Tree_Canopy_{i:00}_{cluster:00}_{x:+0;-0}";
                        canopy.transform.position = trunkObject.transform.position + new Vector3((cluster - 1) * 0.55f, 2.05f + (cluster % 2) * 0.35f, (cluster % 2 == 0 ? -0.28f : 0.34f));
                        canopy.transform.localScale = new Vector3(1.55f, 1.75f, 1.38f);
                        canopy.GetComponent<MeshRenderer>().sharedMaterial = leaves;
                        UnityEngine.Object.DestroyImmediate(canopy.GetComponent<Collider>());
                    }
                }
            }
        }

        private static (GameObject root, GameObject model, Rigidbody body, ArcadeVehicleController vehicle, CrashScoring scoring) CreatePlayerVehicle()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                throw new FileNotFoundException($"Missing production car model at {ModelPath}");
            }

            var root = new GameObject("CarCrash_GT650_Player");
            root.transform.position = new Vector3(0f, 0.055f, -10f);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "CarCrash_GT650_Visual";
            instance.transform.SetParent(root.transform, false);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            SetRenderersEnabled(instance.transform, true);

            var body = root.AddComponent<Rigidbody>();
            body.mass = 1420f;
            body.centerOfMass = new Vector3(0f, -0.24f, 0.05f);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var bodyCollider = root.AddComponent<BoxCollider>();
            bodyCollider.center = new Vector3(0f, 0.44f, 0f);
            bodyCollider.size = new Vector3(2.06f, 0.78f, 4.18f);

            var vehicle = root.AddComponent<ArcadeVehicleController>();
            vehicle.launchDelaySeconds = 1.1f;
            vehicle.maxMotorTorque = 15000f;
            vehicle.arcadeDriveAcceleration = 7.2f;
            vehicle.maxSpeedKmh = 192f;
            vehicle.collisionVelocityCapKmh = 225f;
            vehicle.downforce = 118f;
            vehicle.boostMultiplier = 1.55f;
            vehicle.frontLeftCollider = CreateWheelCollider(root.transform, "WC_FL", new Vector3(-0.975f, 0.39f, 1.49f), 0.385f, false);
            vehicle.frontRightCollider = CreateWheelCollider(root.transform, "WC_FR", new Vector3(0.975f, 0.39f, 1.49f), 0.385f, false);
            vehicle.rearLeftCollider = CreateWheelCollider(root.transform, "WC_RL", new Vector3(-0.985f, 0.39f, -1.31f), 0.385f, true);
            vehicle.rearRightCollider = CreateWheelCollider(root.transform, "WC_RR", new Vector3(0.985f, 0.39f, -1.31f), 0.385f, true);
            vehicle.frontLeftVisual = FindChild(instance.transform, "Wheel_FL");
            vehicle.frontRightVisual = FindChild(instance.transform, "Wheel_FR");
            vehicle.rearLeftVisual = FindChild(instance.transform, "Wheel_RL");
            vehicle.rearRightVisual = FindChild(instance.transform, "Wheel_RR");

            var scoring = root.AddComponent<CrashScoring>();
            scoring.scorePerKmh = 36;
            scoring.minimumImpactKmh = 18f;
            scoring.impactCooldownSeconds = 0.14f;
            scoring.slowMotionScale = 0.18f;
            scoring.slowMotionSeconds = 0.82f;

            var taillights = FindRenderers(instance.transform, "BodyTaillights");
            var shellAnimator = root.AddComponent<CrashCarShellAnimator>();
            shellAnimator.vehicle = vehicle;
            shellAnimator.visibleShell = instance.transform;
            shellAnimator.spinningWheels = Array.Empty<Transform>();
            shellAnimator.tailLightRenderers = taillights;
            shellAnimator.tailLightGlows = new[]
            {
                CreateTailLightGlow(root.transform, "Tail_Glow_L", new Vector3(-0.58f, 0.50f, -2.03f)),
                CreateTailLightGlow(root.transform, "Tail_Glow_R", new Vector3(0.58f, 0.50f, -2.03f))
            };

            CreatePlayerRearDetails(root.transform);
            ConfigureDamageParts(instance.transform, scoring);
            return (root, instance, body, vehicle, scoring);
        }

        private static void CreatePlayerRearDetails(Transform parent)
        {
            var carbon = CreateMaterial("M_Player_Rear_Carbon", new Color(0.012f, 0.014f, 0.016f), 0.68f, 0.54f);
            var exhaust = CreateMaterial("M_Player_Exhaust", new Color(0.055f, 0.058f, 0.062f), 0.82f, 0.46f);
            var plate = CreateMaterial("M_Player_Plate", new Color(0.72f, 0.68f, 0.54f), 0.08f, 0.36f);
            var tail = CreateEmissiveMaterial("M_Player_Tail_Bar", new Color(1f, 0.018f, 0.006f), 2.2f);

            CreateChildBox(parent, "Player_Rear_Blackout", new Vector3(0f, 0.54f, -2.105f), new Vector3(1.62f, 0.34f, 0.065f), carbon);
            var diffuser = CreateChildBox(parent, "Player_Rear_Diffuser", new Vector3(0f, 0.24f, -2.02f), new Vector3(1.92f, 0.16f, 0.42f), carbon);
            diffuser.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            CreateChildBox(parent, "Player_Rear_Plate", new Vector3(0f, 0.48f, -2.148f), new Vector3(0.52f, 0.20f, 0.025f), plate);
            CreateChildBox(parent, "Player_Tail_Bar_L", new Vector3(-0.60f, 0.67f, -2.15f), new Vector3(0.52f, 0.105f, 0.032f), tail);
            CreateChildBox(parent, "Player_Tail_Bar_R", new Vector3(0.60f, 0.67f, -2.15f), new Vector3(0.52f, 0.105f, 0.032f), tail);

            foreach (var x in new[] { -0.70f, -0.43f, 0.43f, 0.70f })
            {
                CreateChildCylinder(parent, $"Player_Exhaust_{x:+0.00;-0.00}", new Vector3(x, 0.26f, -2.245f), new Vector3(0.115f, 0.16f, 0.115f), Quaternion.Euler(90f, 0f, 0f), exhaust);
            }
        }

        private static void ConfigureDamageParts(Transform visualRoot, CrashScoring scoring)
        {
            var cracks = new List<Renderer>();
            foreach (var transform in visualRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name.StartsWith("Damage_Windshield_Crack", StringComparison.Ordinal))
                {
                    foreach (var renderer in transform.GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.enabled = false;
                        cracks.Add(renderer);
                    }
                    continue;
                }

                if (!transform.name.StartsWith("Damage_", StringComparison.Ordinal))
                {
                    continue;
                }
            }

            var damage = visualRoot.gameObject.AddComponent<CrashDamageVisual>();
            damage.scoring = scoring;
            damage.windshieldCracks = cracks.ToArray();
        }

        private static void CreateTraffic(GameObject productionModel)
        {
            CreateTrafficCar(productionModel, "Traffic_Graphite_01", new Vector3(-2.65f, 0.055f, 20f), new Color(0.08f, 0.10f, 0.12f), new Vector3(0f, 0f, 5.4f));
            CreateTrafficCar(productionModel, "Traffic_Red_02", new Vector3(2.65f, 0.055f, 38f), new Color(0.62f, 0.025f, 0.018f), new Vector3(0f, 0f, 4.7f));
            CreateTrafficCar(productionModel, "Traffic_Blue_03", new Vector3(-0.72f, 0.055f, 120f), new Color(0.025f, 0.14f, 0.32f), new Vector3(0f, 0f, -7.2f));
            CreateTrafficCar(productionModel, "Traffic_Silver_04", new Vector3(2.65f, 0.055f, 94f), new Color(0.42f, 0.45f, 0.48f), new Vector3(0f, 0f, -6.8f));
            CreateDetailedTruck(new Vector3(-2.2f, 0.025f, 52f));
            CreateDetailedBus(new Vector3(3.3f, 0.025f, 82f));
        }

        private static void CreateTrafficCar(GameObject model, string name, Vector3 position, Color color, Vector3 velocity)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            var visual = UnityEngine.Object.Instantiate(model);
            visual.name = $"{name}_Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localRotation = velocity.z < 0f ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            visual.transform.localScale = Vector3.one * 0.94f;

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial == null || !renderer.sharedMaterial.name.StartsWith("Paint 1", StringComparison.Ordinal))
                {
                    continue;
                }
                var material = new Material(renderer.sharedMaterial) { color = color };
                renderer.sharedMaterial = material;
            }

            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.44f, 0f);
            collider.size = new Vector3(2.0f, 0.82f, 4.08f);
            var body = root.AddComponent<Rigidbody>();
            body.mass = 1380f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var mover = root.AddComponent<MovingTrafficObstacle>();
            mover.velocity = velocity;
            mover.resetAheadZ = 142f;
            mover.resetBehindZ = -32f;
            mover.laneJitter = 0.12f;
            mover.swaySpeed = 0.42f;
            mover.launchDelaySeconds = 1.1f;

            var wheels = new[]
            {
                FindChild(visual.transform, "Wheel_FL"),
                FindChild(visual.transform, "Wheel_FR"),
                FindChild(visual.transform, "Wheel_RL"),
                FindChild(visual.transform, "Wheel_RR")
            };
            var wheelAnimator = root.AddComponent<TrafficWheelAnimator>();
            wheelAnimator.wheels = wheels;
            wheelAnimator.speedMetersPerSecond = velocity.magnitude;
        }

        private static void CreateDetailedTruck(Vector3 position)
        {
            var white = CreateMaterial("M_Truck_White", new Color(0.68f, 0.70f, 0.68f), 0.18f, 0.42f);
            var dark = CreateMaterial("M_Truck_Dark", new Color(0.025f, 0.033f, 0.038f), 0.42f, 0.58f);
            var glass = CreateMaterial("M_Truck_Glass", new Color(0.035f, 0.10f, 0.13f), 0.45f, 0.76f);
            var red = CreateEmissiveMaterial("M_Truck_Tail", new Color(1f, 0.025f, 0.008f), 1.4f);
            var headlight = CreateEmissiveMaterial("M_Truck_Headlight", new Color(1f, 0.78f, 0.42f), 1.8f);
            var root = new GameObject("Traffic_Box_Truck");
            root.transform.position = position;
            CreateChildBox(root.transform, "Truck_Cargo", new Vector3(0f, 1.35f, 0.65f), new Vector3(2.35f, 2.65f, 4.9f), white);
            CreateChildBox(root.transform, "Truck_Cab", new Vector3(0f, 0.90f, -2.35f), new Vector3(2.25f, 1.85f, 1.65f), white);
            CreateChildBox(root.transform, "Truck_Windshield", new Vector3(0f, 1.20f, -3.19f), new Vector3(1.75f, 0.62f, 0.05f), glass);
            CreateChildBox(root.transform, "Truck_Rear_Lights", new Vector3(0f, 0.54f, 3.13f), new Vector3(1.70f, 0.18f, 0.06f), red);
            CreateChildBox(root.transform, "Truck_Grille", new Vector3(0f, 0.56f, -3.205f), new Vector3(1.55f, 0.44f, 0.055f), dark);
            CreateChildBox(root.transform, "Truck_Headlight_L", new Vector3(-0.82f, 0.70f, -3.24f), new Vector3(0.34f, 0.22f, 0.05f), headlight);
            CreateChildBox(root.transform, "Truck_Headlight_R", new Vector3(0.82f, 0.70f, -3.24f), new Vector3(0.34f, 0.22f, 0.05f), headlight);
            CreateChildBox(root.transform, "Truck_Front_Bumper", new Vector3(0f, 0.28f, -3.28f), new Vector3(2.12f, 0.20f, 0.12f), dark);
            foreach (var x in new[] { -0.82f, -0.41f, 0f, 0.41f, 0.82f })
            {
                CreateChildBox(root.transform, $"Truck_Door_Rib_{x:+0.00;-0.00;0}", new Vector3(x, 1.36f, 3.12f), new Vector3(0.045f, 2.15f, 0.06f), dark);
            }
            CreateVehicleWheels(root.transform, 1.12f, new[] { -2.35f, 1.8f }, 0.48f, 0.32f, dark);
            ConfigureTrafficPhysics(root, new Vector3(2.35f, 2.65f, 6.3f), 4200f, new Vector3(0f, 0f, -5.4f));
        }

        private static void CreateDetailedBus(Vector3 position)
        {
            var bodyMaterial = CreateMaterial("M_Bus_Silver", new Color(0.42f, 0.43f, 0.41f), 0.28f, 0.48f);
            var dark = CreateMaterial("M_Bus_Dark", new Color(0.018f, 0.024f, 0.028f), 0.36f, 0.62f);
            var glass = CreateMaterial("M_Bus_Glass", new Color(0.025f, 0.09f, 0.12f), 0.46f, 0.78f);
            var red = CreateEmissiveMaterial("M_Bus_Tail", new Color(1f, 0.02f, 0.006f), 1.3f);
            var headlight = CreateEmissiveMaterial("M_Bus_Headlight", new Color(1f, 0.80f, 0.46f), 1.6f);
            var root = new GameObject("Traffic_City_Bus");
            root.transform.position = position;
            CreateChildBox(root.transform, "Bus_Main_Body", new Vector3(0f, 1.35f, 0f), new Vector3(2.55f, 2.65f, 8.8f), bodyMaterial);
            for (var i = -3; i <= 3; i++)
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    CreateChildBox(root.transform, $"Bus_Window_{side:+0;-0}_{i:+0;-0;0}", new Vector3(side * 1.286f, 1.78f, i * 1.05f), new Vector3(0.045f, 0.82f, 0.78f), glass);
                }
            }
            CreateChildBox(root.transform, "Bus_Rear_Window", new Vector3(0f, 1.75f, 4.43f), new Vector3(1.86f, 0.85f, 0.05f), glass);
            CreateChildBox(root.transform, "Bus_Rear_Lights", new Vector3(0f, 0.62f, 4.47f), new Vector3(1.78f, 0.18f, 0.05f), red);
            CreateChildBox(root.transform, "Bus_Front_Windshield", new Vector3(0f, 1.78f, -4.43f), new Vector3(2.05f, 1.08f, 0.055f), glass);
            CreateChildBox(root.transform, "Bus_Front_Destination", new Vector3(0f, 2.45f, -4.455f), new Vector3(1.72f, 0.26f, 0.04f), dark);
            CreateChildBox(root.transform, "Bus_Headlight_L", new Vector3(-0.78f, 0.54f, -4.48f), new Vector3(0.42f, 0.20f, 0.05f), headlight);
            CreateChildBox(root.transform, "Bus_Headlight_R", new Vector3(0.78f, 0.54f, -4.48f), new Vector3(0.42f, 0.20f, 0.05f), headlight);
            CreateChildBox(root.transform, "Bus_Front_Bumper", new Vector3(0f, 0.25f, -4.53f), new Vector3(2.30f, 0.20f, 0.12f), dark);
            CreateVehicleWheels(root.transform, 1.28f, new[] { -3.0f, 3.0f }, 0.52f, 0.34f, dark);
            ConfigureTrafficPhysics(root, new Vector3(2.55f, 2.65f, 8.8f), 7800f, new Vector3(0f, 0f, -4.2f));
        }

        private static void ConfigureTrafficPhysics(GameObject root, Vector3 size, float mass, Vector3 velocity)
        {
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, size.y * 0.5f, 0f);
            collider.size = size;
            var body = root.AddComponent<Rigidbody>();
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var mover = root.AddComponent<MovingTrafficObstacle>();
            mover.velocity = velocity;
            mover.resetAheadZ = 142f;
            mover.resetBehindZ = -36f;
            mover.laneJitter = 0.08f;
            mover.swaySpeed = 0.28f;
            mover.launchDelaySeconds = 1.1f;
        }

        private static void CreateVehicleWheels(Transform parent, float x, float[] zPositions, float radius, float width, Material material)
        {
            var wheels = new List<Transform>();
            foreach (var z in zPositions)
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = $"Traffic_Wheel_{side:+0;-0}_{z:+0.0;-0.0}";
                    wheel.transform.SetParent(parent, false);
                    wheel.transform.localPosition = new Vector3(side * x, radius, z);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    wheel.transform.localScale = new Vector3(radius * 2f, width * 0.5f, radius * 2f);
                    wheel.GetComponent<MeshRenderer>().sharedMaterial = material;
                    UnityEngine.Object.DestroyImmediate(wheel.GetComponent<Collider>());
                    wheels.Add(wheel.transform);
                }
            }
            var animator = parent.gameObject.AddComponent<TrafficWheelAnimator>();
            animator.wheels = wheels.ToArray();
            animator.speedMetersPerSecond = 6f;
        }

        private static void CreateCameraAndGameSystems(GameObject player, Rigidbody body, ArcadeVehicleController vehicle, CrashScoring scoring)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 2.8f, -16.2f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 54f;
            camera.nearClipPlane = 0.12f;
            camera.farClipPlane = 290f;
            camera.allowHDR = true;
            camera.backgroundColor = new Color(0.16f, 0.12f, 0.12f);
            cameraObject.AddComponent<AudioListener>();

            var follow = cameraObject.AddComponent<CarCrashCameraFollow>();
            follow.target = player.transform;
            follow.offset = new Vector3(0f, 2.52f, -6.45f);
            follow.lookAhead = 11.2f;
            follow.followSharpness = 7.8f;

            var hud = cameraObject.AddComponent<CarCrashHudInput>();
            hud.vehicle = vehicle;
            hud.scoring = scoring;

            var loop = cameraObject.AddComponent<ImpactRunLoopDirector>();
            loop.vehicle = vehicle;
            loop.player = player.transform;
            loop.playerBody = body;
            loop.resetBeyondZ = 132f;
            loop.resetBeyondX = 9.4f;
            loop.resetAboveY = 7.5f;
        }

        private static WheelCollider CreateWheelCollider(Transform parent, string name, Vector3 localPosition, float radius, bool drive)
        {
            var wheel = new GameObject(name);
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = localPosition;
            var collider = wheel.AddComponent<WheelCollider>();
            collider.radius = radius;
            collider.mass = drive ? 44f : 38f;
            collider.suspensionDistance = 0.18f;
            collider.forceAppPointDistance = 0.14f;
            collider.wheelDampingRate = 0.48f;

            var spring = collider.suspensionSpring;
            spring.spring = drive ? 44000f : 40500f;
            spring.damper = 5400f;
            spring.targetPosition = 0.46f;
            collider.suspensionSpring = spring;

            var forward = collider.forwardFriction;
            forward.stiffness = drive ? 2.25f : 1.55f;
            collider.forwardFriction = forward;
            var sideways = collider.sidewaysFriction;
            sideways.stiffness = drive ? 1.38f : 1.18f;
            collider.sidewaysFriction = sideways;
            return collider;
        }

        private static Light CreateTailLightGlow(Transform parent, string name, Vector3 localPosition)
        {
            var glowObject = new GameObject(name);
            glowObject.transform.SetParent(parent, false);
            glowObject.transform.localPosition = localPosition;
            var light = glowObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.025f, 0.008f);
            light.range = 4.2f;
            light.intensity = 2.2f;
            return light;
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness)
        {
            var material = new Material(Shader.Find("Standard"))
            {
                name = name,
                color = color
            };
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            return material;
        }

        private static Material CreateEmissiveMaterial(string name, Color color, float intensity)
        {
            var material = CreateMaterial(name, color, 0.05f, 0.52f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * intensity);
            return material;
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private static GameObject CreateSphere(string name, Vector3 position, float diameter, Material material)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * diameter;
            sphere.GetComponent<MeshRenderer>().sharedMaterial = material;
            return sphere;
        }

        private static GameObject CreateChildBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        private static GameObject CreateChildCylinder(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localScale = localScale;
            cylinder.transform.localRotation = localRotation;
            cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            return cylinder;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }
            foreach (Transform child in root)
            {
                var found = FindChild(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static Renderer[] FindRenderers(Transform root, string nameFragment)
        {
            var renderers = new List<Renderer>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    renderers.Add(renderer);
                }
            }
            return renderers.ToArray();
        }

        private static void SetRenderersEnabled(Transform root, bool enabled)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = enabled;
            }
        }

        private static void PatchWebGlIndex()
        {
            var indexPath = Path.Combine(WebGlBuildPath, "index.html");
            if (!File.Exists(indexPath))
            {
                return;
            }

            var html = File.ReadAllText(indexPath);
            const string responsiveCss = @"    <meta name=""viewport"" content=""width=device-width, height=device-height, initial-scale=1.0, user-scalable=no"">
    <style>
      html, body { margin: 0; width: 100%; height: 100%; overflow: hidden; background: #050608; }
      #unity-container { position: fixed; inset: 0; width: 100vw; height: 100vh; background: #050608; }
      #unity-container.unity-desktop, #unity-container.unity-mobile { width: 100vw; height: 100vh; }
      #unity-canvas { width: 100vw !important; height: 100vh !important; display: block; background: #050608; }
      #unity-footer { display: none !important; }
      #unity-loading-bar { position: absolute; left: 50%; top: 50%; transform: translate(-50%, -50%); }
    </style>";

            if (!html.Contains("#unity-container { position: fixed;", StringComparison.Ordinal))
            {
                const string stylesheetLink = "    <link rel=\"stylesheet\" href=\"TemplateData/style.css\">";
                html = html.Replace(stylesheetLink, stylesheetLink + Environment.NewLine + responsiveCss);
            }
            html = html.Replace("canvas.style.width = \"960px\";", "canvas.style.width = \"100vw\";");
            html = html.Replace("canvas.style.height = \"600px\";", "canvas.style.height = \"100vh\";");
            File.WriteAllText(indexPath, html);
        }
    }
}
