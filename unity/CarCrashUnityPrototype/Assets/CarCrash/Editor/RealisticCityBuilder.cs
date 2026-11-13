using System.Collections.Generic;
using System.IO;
using CarCrash;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrashEditor
{
    public static class RealisticCityBuilder
    {
        private const string MeshDirectory = "Assets/CarCrash/Generated/RealismGeometry/";
        private static Transform _city;
        private static Material _asphalt, _concrete, _stone, _metal, _glass, _warmGlass, _trim, _white, _yellow;
        private static Material _bark, _leaf, _light;
        private static Material _signMaterial;
        private static Font _signFont;
        private static float[] CrossStreets => CityRoadNetwork.CrossStreetZ;
        private static readonly Dictionary<string, Mesh> Meshes = new();

        public static void CreateLighting()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            QualitySettings.SetQualityLevel(3, true);
            QualitySettings.antiAliasing = 4;
            QualitySettings.pixelLightCount = 3;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 85f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.realtimeReflectionProbes = true;
            var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/CarCrash/Materials/Realism/M_Venice_Sunset.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Panoramic"));
                AssetDatabase.CreateAsset(sky, "Assets/CarCrash/Materials/Realism/M_Venice_Sunset.mat");
            }
            sky.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(RealismMaterialLibrary.TextureRoot + "Realism/venice_sunset_hdri.hdr"));
            sky.SetFloat("_Exposure", 0.85f);
            sky.SetFloat("_Rotation", 115f);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.40f, 0.47f, 0.56f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.34f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.17f, 0.15f, 0.14f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 256;
            RenderSettings.reflectionIntensity = 1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0022f;
            RenderSettings.fogColor = new Color(0.54f, 0.53f, 0.51f);

            var sun = new GameObject("Golden hour sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.84f, 0.65f);
            sun.intensity = 2.1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.88f;
            sun.shadowBias = 0.03f;
            sun.shadowNormalBias = 0.22f;
            sun.transform.rotation = Quaternion.Euler(24f, -38f, 0f);
            RenderSettings.sun = sun;
            var fill = new GameObject("Cool sky fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.60f, 0.72f, 1f);
            fill.intensity = 0.23f;
            fill.transform.rotation = Quaternion.Euler(54f, 145f, 0f);

            var probe = new GameObject("Street reflection probe").AddComponent<ReflectionProbe>();
            probe.transform.position = new Vector3(0, 3, -8);
            probe.mode = ReflectionProbeMode.Custom;
            probe.hdr = false;
            probe.resolution = 128;
            probe.size = new Vector3(38, 20, 70);
            probe.boxProjection = true;
            probe.intensity = 1f;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;
        }

        public static void CreateCity()
        {
            Directory.CreateDirectory(MeshDirectory);
            Meshes.Clear();
            _city = new GameObject("Elysian downtown district").transform;
            _city.gameObject.AddComponent<CityRoadNetwork>();
            _city.gameObject.AddComponent<CityLightingRig>();
            _signFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            const string signPath = "Assets/CarCrash/Materials/Realism/M_World_Sign_Text.mat";
            _signMaterial = AssetDatabase.LoadAssetAtPath<Material>(signPath);
            if (_signMaterial == null)
            {
                _signMaterial = new Material(Shader.Find("CarCrash/WorldSignText"));
                AssetDatabase.CreateAsset(_signMaterial,signPath);
            }
            _signMaterial.mainTexture = _signFont.material.mainTexture;
            var fontBinding = _city.gameObject.AddComponent<CitySignFont>();
            fontBinding.font = _signFont;
            fontBinding.material = _signMaterial;
            _asphalt = RealismMaterialLibrary.Surface("M_City_Asphalt", "asphalt_02", new Color(0.60f, 0.62f, 0.65f), Vector2.one, 0.7f, 0.65f);
            _concrete = RealismMaterialLibrary.Surface("M_City_Paving", "brick_wall_003", new Color(0.63f, 0.65f, 0.65f), new Vector2(2, 2), 0.25f, 0.20f);
            _stone = RealismMaterialLibrary.Standard("M_City_Concrete", new Color(0.48f, 0.47f, 0.43f), 0f, 0.28f);
            _metal = RealismMaterialLibrary.Standard("M_City_Powdercoat", new Color(0.055f, 0.065f, 0.073f), 0.6f, 0.6f);
            _glass = RealismMaterialLibrary.Standard("M_City_Window_Glass", new Color(0.12f, 0.17f, 0.20f), 0.68f, 0.92f);
            _warmGlass = RealismMaterialLibrary.Standard("M_City_Lit_Window", new Color(0.66f, 0.48f, 0.29f), 0.2f, 0.68f, 0.3f);
            _trim = RealismMaterialLibrary.Standard("M_City_Limestone_Trim", new Color(0.65f, 0.62f, 0.54f), 0.05f, 0.35f);
            _white = RealismMaterialLibrary.Standard("M_City_Aged_White_Paint", new Color(0.73f, 0.72f, 0.65f), 0f, 0.22f);
            _yellow = RealismMaterialLibrary.Standard("M_City_Road_Yellow", new Color(0.85f, 0.59f, 0.10f), 0f, 0.25f);
            _bark = RealismMaterialLibrary.Standard("M_Palm_Bark", new Color(0.27f, 0.22f, 0.16f), 0f, 0.1f);
            _leaf = RealismMaterialLibrary.Standard("M_Palm_Leaf", new Color(0.17f, 0.25f, 0.10f), 0f, 0.3f);
            _light = RealismMaterialLibrary.Standard("M_City_Lamp_Lens", new Color(1f, 0.83f, 0.56f), 0f, 0.7f, 1.8f);
            Ground();
            var district = _city;
            foreach (var avenue in CityRoadNetwork.AvenueX)
            {
                var neighborhood = new GameObject(avenue < 0 ? "Westside neighborhood" : avenue > 0 ? "Pacific neighborhood" : "Elysian neighborhood").transform;
                neighborhood.SetParent(district, false);
                _city = neighborhood;
                Buildings();
                StreetDetails(avenue < 0 ? "WESTSIDE AVE" : avenue > 0 ? "PACIFIC AVE" : "ELYSIAN BLVD");
                neighborhood.position = Vector3.right * avenue;
            }
            _city = district;
            Skyline();
            CombineStaticGeometry();
            BakeStreetReflection();
        }

        private static void BakeStreetReflection()
        {
            // Capture byte-format faces in the Editor. Realtime HDR cubemaps can
            // produce invalid PBR samples on browser graphics backends.
            var probe = Object.FindFirstObjectByType<ReflectionProbe>();
            probe.enabled = false;
            var captureObject = new GameObject("Reflection capture");
            var camera = captureObject.AddComponent<Camera>();
            camera.transform.position = probe.transform.position;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.farClipPlane = 650f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            var cube = new Cubemap(128, TextureFormat.RGBA32, true) { name = "Elysian street reflection" };
            if (!camera.RenderToCubemap(cube)) throw new System.InvalidOperationException("Street reflection capture failed.");
            var path = "Assets/CarCrash/Generated/ElysianStreetReflection.cubemap";
            var saved = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (saved == null) { AssetDatabase.CreateAsset(cube, path); saved = cube; }
            else { EditorUtility.CopySerialized(cube, saved); Object.DestroyImmediate(cube); }
            Object.DestroyImmediate(captureObject);
            probe.customBakedTexture = saved;
            probe.enabled = true;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = saved;
            EditorUtility.SetDirty(saved);
        }

        private static void Ground()
        {
            Plane("District ground", new Vector3(0, -0.16f, 100), 320, 720, _stone, true);
            Plane("Elysian Boulevard", new Vector3(0, 0, 100), 18f, 460, _asphalt, true);
            foreach (var x in new[] { -54f, 54f }) Plane("Parallel avenue", new Vector3(x, 0, 110), 18f, 460, _asphalt);
            foreach (var z in CrossStreets) Plane("Cross street", new Vector3(0, 0.006f, z), 190, 14, _asphalt);
            for (var i = 0; i < CrossStreets.Length; i++)
            {
                var start = i == 0 ? -125 : CrossStreets[i - 1] + 7;
                var end = CrossStreets[i] - 7;
                foreach (var avenue in CityRoadNetwork.AvenueX)
                foreach (var side in new[] { -1f, 1f })
                {
                    var x = avenue + side * 10.7f;
                    Box("Sidewalk foundation", new Vector3(x, 0.045f, (start + end) * 0.5f), new Vector3(3.35f, 0.20f, end - start), _stone, true);
                    Plane($"Walkway {i} {x}", new Vector3(x, 0.151f, (start + end) * 0.5f), 3.35f, end - start, _concrete);
                    Box("Raised curb", new Vector3(x - side * 1.63f, 0.12f, (start + end) * 0.5f), new Vector3(0.20f, 0.27f, end - start), _trim, true);
                }
            }
            for (var z = -115f; z < 320; z += 5.6f)
            {
                if (NearCrossStreet(z, 9f)) continue;
                foreach (var avenue in CityRoadNetwork.AvenueX)
                {
                    foreach (var x in new[] { -3.3f, 3.3f })
                        Box("Worn lane paint", new Vector3(avenue+x, 0.012f, z), new Vector3(0.085f, 0.018f, 2.5f), _white);
                    foreach (var x in new[] { -0.12f, 0.12f })
                        Box("Double yellow centerline", new Vector3(avenue+x, 0.014f, z), new Vector3(0.07f, 0.016f, 5.65f), _yellow);
                }
            }
            foreach (var z in CrossStreets)
            {
                foreach (var avenue in CityRoadNetwork.AvenueX)
                foreach (var side in new[] { -1, 1 })
                {
                    for (var x = -7.8f; x < 8.5f; x += 1.45f)
                        Box("Zebra crossing", new Vector3(avenue+x, 0.018f, z + side * 8.9f), new Vector3(0.8f, 0.015f, 2.5f), _white);
                    Box("Stop line", new Vector3(avenue+side * 4.6f, 0.019f, z + side * 11f), new Vector3(7.9f, 0.016f, 0.22f), _white);
                }
                foreach (var side in new[] {-1f,1f})
                foreach (var segment in new[] {new Vector2(12.4f,41.6f),new Vector2(66.4f,93.5f)})
                {
                    var center = side * (segment.x+segment.y)*.5f;
                    var length = segment.y-segment.x;
                    foreach (var edge in new[] {-1f,1f})
                    {
                        var walkZ = z+edge*8.7f;
                        Box("Cross street sidewalk",new Vector3(center,.045f,walkZ),new Vector3(length,.20f,3.35f),_stone,true);
                        Plane("Cross street paving",new Vector3(center,.151f,walkZ),length,3.35f,_concrete);
                        Box("Cross street curb",new Vector3(center,.12f,z+edge*7.07f),new Vector3(length,.27f,.20f),_trim,true);
                        Palm(new Vector3(center,.15f,walkZ),0);
                    }
                    foreach (var lane in new[] {-.12f,.12f})
                        Box("Cross street centerline",new Vector3(center,.014f,z+lane),new Vector3(length,.016f,.07f),_yellow);
                }
                foreach (var side in new[] {-1f,1f})
                {
                    Box("District edge safety barrier",new Vector3(side*94f,.65f,z),new Vector3(.65f,1.3f,14f),_stone,true);
                    Box("District edge reflective stripe",new Vector3(side*93.66f,.93f,z),new Vector3(.04f,.19f,14f),_yellow);
                    Sign("CITY LOOP / TURN",new Vector3(side*93.5f,2.45f,z),Quaternion.Euler(0,side*90f,0),.16f,new Color(.94f,.82f,.5f));
                }
            }
            foreach (var avenue in CityRoadNetwork.AvenueX)
            {
                const float width = 18f;
                var south = avenue == 0 ? -128f : -119f;
                var north = avenue == 0 ? 328f : 338f;
                foreach (var z in new[] { south, north })
                {
                    Box("Street end safety barrier", new Vector3(avenue, 0.65f, z), new Vector3(width, 1.3f, 0.65f), _stone, true);
                    Box("Street end reflective stripe", new Vector3(avenue, 0.93f, z - 0.34f), new Vector3(width, 0.19f, 0.04f), _yellow);
                    var signPosition = new Vector3(avenue, 2.45f, z + (z < 0 ? 0.4f : -0.4f));
                    Sign("CITY LOOP  /  TURN", signPosition, Quaternion.Euler(0, z < 0 ? 180 : 0, 0), 0.16f, new Color(0.94f, 0.82f, 0.5f));
                }
            }
            for (var i = 0; i < 24; i++)
            {
                var z = -55f + i * 14.4f;
                foreach (var x in new[] { -8.1f, 8.1f })
                {
                    Box("Storm drain", new Vector3(x, 0.022f, z), new Vector3(0.56f, 0.025f, 0.9f), _metal);
                    for (var n = 0; n < 5; n++) Box("Drain slot", new Vector3(x - 0.22f + n * 0.11f, 0.04f, z), new Vector3(0.03f, 0.015f, 0.75f), _stone);
                }
            }
        }

        private static bool NearCrossStreet(float z, float radius)
        {
            foreach (var crossing in CrossStreets) if (Mathf.Abs(z - crossing) < radius) return true;
            return false;
        }

        private static void Buildings()
        {
            var shopNames = new[] { "ELYSIAN MOTORS", "SUNDOWN COFFEE", "PACIFIC MARKET", "HOTEL PALOMA", "VINYL & CO.", "WESTSIDE DINER" };
            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 20; i++)
                {
                    var z = -100f + i * 21f;
                    if (NearCrossStreet(z, 16f)) continue;
                    var width = 10f + i % 3 * 1.3f;
                    var depth = 15.5f + i % 2 * 1.5f;
                    var height = 10.8f + i % 5 * 3.3f;
                    var centerX = side * (12.6f + width * 0.5f);
                    var facade = side * 12.6f;
                    var tint = i % 3 == 0 ? new Color(0.80f, 0.62f, 0.47f) : i % 3 == 1 ? new Color(0.70f, 0.70f, 0.65f) : new Color(0.51f, 0.44f, 0.37f);
                    var wall = RealismMaterialLibrary.Surface("M_Facade_Brick_" + i % 3, "brick_wall_003", tint, new Vector2(2.5f, 4.0f), 0.65f, 0.22f);
                    Box("Masonry building", new Vector3(centerX, height * 0.5f, z), new Vector3(width, height, depth), wall, true);
                    Box("Stone ground floor", new Vector3(facade + side * 0.08f, 1.9f, z), new Vector3(0.30f, 3.7f, depth + 0.1f), _stone);
                    Box("Roof parapet", new Vector3(centerX, height + 0.28f, z), new Vector3(width + 0.28f, 0.56f, depth + 0.28f), _trim);
                    Box("Upper cornice", new Vector3(facade - side * 0.15f, height - 0.65f, z), new Vector3(0.52f, 0.20f, depth + 0.35f), _trim);
                    for (var y = 4.9f; y < height - 1; y += 2.8f)
                    {
                        Box("Floor stringcourse", new Vector3(facade - side * 0.08f, y - 1.2f, z), new Vector3(0.30f, 0.10f, depth), _trim);
                        for (var column = -2; column <= 2; column++)
                        {
                            var wz = z + column * 2.7f;
                            var lit = (column + Mathf.RoundToInt(y) + i) % 7 == 0;
                            Box("Window surround", new Vector3(facade - side * 0.15f, y, wz), new Vector3(0.22f, 1.84f, 1.37f), _trim);
                            Box("Recessed glazing", new Vector3(facade - side * 0.28f, y, wz), new Vector3(0.08f, 1.53f, 1.10f), lit ? _warmGlass : _glass);
                            Box("Window mullion", new Vector3(facade - side * 0.34f, y, wz), new Vector3(0.05f, 1.56f, 0.045f), _metal);
                            Box("Window sill", new Vector3(facade - side * 0.30f, y - 0.90f, wz), new Vector3(0.38f, 0.12f, 1.50f), _trim);
                        }
                    }
                    for (var column = -1; column <= 1; column++)
                    {
                        Box("Shop glazing", new Vector3(facade - side * 0.22f, 1.55f, z + column * 4.3f), new Vector3(0.08f, 2.45f, 3.6f), _glass);
                        Box("Shop window frame", new Vector3(facade - side * 0.30f, 1.6f, z + column * 4.3f + 1.72f), new Vector3(0.14f, 2.75f, 0.08f), _metal);
                    }
                    var awning = RealismMaterialLibrary.Standard("M_Awning_" + i % 3, i % 3 == 0 ? new Color(0.22f, 0.31f, 0.25f) : i % 3 == 1 ? new Color(0.40f, 0.16f, 0.12f) : new Color(0.20f, 0.23f, 0.29f));
                    Box("Shop canopy", new Vector3(facade - side * 0.62f, 3.38f, z), new Vector3(1.35f, 0.17f, depth * 0.84f), awning);
                    var sign = Box("Shop fascia", new Vector3(facade - side * 0.38f, 3.92f, z), new Vector3(0.15f, 0.71f, depth * 0.82f), _metal);
                    Sign(shopNames[(i + (side > 0 ? 2 : 0)) % shopNames.Length], sign.transform.position - Vector3.right * side * 0.10f,
                        Quaternion.Euler(0, side * 90f, 0), 0.20f, new Color(0.90f, 0.85f, 0.67f));
                    Box("Rooftop air conditioning", new Vector3(centerX + side, height + 0.8f, z + 2), new Vector3(2.7f, 1.1f, 2.0f), _metal);
                    for (var n = 0; n < 5; n++) Box("AC grille", new Vector3(centerX + side, height + 1.37f, z + 1.2f + n * 0.3f), new Vector3(2.4f, 0.045f, 0.035f), _trim);
                    if (i % 3 == 0)
                    {
                        Box("Vertical hotel sign", new Vector3(facade - side * 0.8f, 7.7f, z - depth * 0.40f), new Vector3(1.4f, 5.0f, 0.25f), awning);
                        Sign("H\nO\nT\nE\nL", new Vector3(facade - side * 0.8f, 7.7f, z - depth * 0.40f - 0.15f), Quaternion.identity, 0.25f, new Color(1f, 0.63f, 0.28f));
                    }
                }
            }
        }

        private static void StreetDetails(string streetName)
        {
            for (var i = 0; i < 22; i++)
            {
                var z = -78f + i * 17.5f;
                if (NearCrossStreet(z, 10f)) continue;
                foreach (var side in new[] { -1f, 1f })
                {
                    var x = side * 10.7f;
                    Palm(new Vector3(x, 0.15f, z), i % 3);
                    var lampZ = z + 6;
                    Tube("Streetlight column", new Vector3(side * 9.9f, 0.15f, lampZ), new Vector3(side * 9.9f, 6.8f, lampZ), 0.075f, _metal, true);
                    Tube("Streetlight outreach", new Vector3(side * 9.9f, 6.8f, lampZ), new Vector3(side * 7.8f, 6.6f, lampZ), 0.07f, _metal);
                    Box("Streetlight housing", new Vector3(side * 7.75f, 6.55f, lampZ), new Vector3(0.65f, 0.14f, 0.35f), _metal);
                    Box("Streetlight lens", new Vector3(side * 7.75f, 6.45f, lampZ), new Vector3(0.5f, 0.025f, 0.25f), _light);
                    if (i % 3 == 0)
                    {
                        Box("Sidewalk bench seat", new Vector3(x, 0.58f, z + 3), new Vector3(0.58f, 0.12f, 1.75f), _bark, true);
                        Box("Sidewalk bench back", new Vector3(x + side * 0.25f, 0.95f, z + 3), new Vector3(0.10f, 0.60f, 1.75f), _bark, true);
                        foreach (var dz in new[] { -0.65f, 0.65f }) Box("Bench support", new Vector3(x, 0.32f, z + 3 + dz), new Vector3(0.48f, 0.5f, 0.08f), _metal);
                        Cylinder("Litter bin", new Vector3(x, 0.65f, z - 3), 0.34f, 1.05f, _metal, true);
                    }
                }
            }
            foreach (var z in CrossStreets)
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    Tube("Traffic signal pole", new Vector3(side * 9.6f, 0.2f, z - side * 6.5f), new Vector3(side * 9.6f, 5.8f, z - side * 6.5f), 0.10f, _metal, true);
                    Tube("Traffic signal arm", new Vector3(side * 9.6f, 5.8f, z - side * 6.5f), new Vector3(side * 4.8f, 5.8f, z - side * 6.5f), 0.075f, _metal);
                    var signal = Box("Three lens traffic signal", new Vector3(side * 4.8f, 5.25f, z - side * 6.5f), new Vector3(0.40f, 1.0f, 0.28f), _metal);
                    var green = RealismMaterialLibrary.Standard("M_Signal_Green", new Color(0.12f, 0.63f, 0.29f), 0, 0.6f, 1.4f);
                    Box("Signal green lens", signal.transform.position + new Vector3(0, -0.26f, -0.17f), new Vector3(0.21f, 0.21f, 0.04f), green);
                    var signPosition = new Vector3(side * 9.6f, 4.1f, z - side * 6.5f);
                    Box("Street name plate", signPosition, new Vector3(2.1f, 0.38f, 0.06f), green);
                    Sign(streetName, signPosition + Vector3.back * 0.05f, Quaternion.identity, 0.065f, Color.white);
                }
            }
            // Small curbside props break up the repeated streetscape at car height.
            var hydrant = RealismMaterialLibrary.Standard("M_Hydrant_Red", new Color(0.57f, 0.10f, 0.065f), 0.3f, 0.4f);
            for (var i = 0; i < 12; i++)
            {
                var z = -24f + i * 25f;
                Cylinder("Fire hydrant", new Vector3(-9.7f, 0.5f, z), 0.14f, 0.68f, hydrant, true);
                Tube("Hydrant valve", new Vector3(-10f, 0.6f, z), new Vector3(-9.4f, 0.6f, z), 0.085f, hydrant, true);
            }
        }

        private static void Skyline()
        {
            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 22; i++)
                {
                    var height = 24f + (i * 17 % 57);
                    // Keep towers outside the connected avenues and cross streets.
                    var x = side * (112f + i % 3 * 20f);
                    var z = -60f + i * 20f;
                    var facade = RealismMaterialLibrary.Standard("M_Skyline_" + i % 4,
                        Color.Lerp(new Color(0.27f, 0.32f, 0.35f), new Color(0.46f, 0.44f, 0.39f), i % 4 / 3f), 0.3f, 0.5f);
                    Box("Distant office tower", new Vector3(x, height * 0.5f, z), new Vector3(12 + i % 3 * 2, height, 14), facade, true);
                    for (var y = 5f; y < height - 2; y += 3.2f)
                        Box("Office ribbon glazing", new Vector3(x - side * (6.1f + i % 3), y, z), new Vector3(0.07f, 1.45f, 12.2f), _glass);
                    Box("Tower roof plant", new Vector3(x, height + 1, z), new Vector3(7, 2, 8), _metal);
                }
            }
        }

        private static void Palm(Vector3 position, int variant)
        {
            var height = 7.7f + variant * 0.9f;
            Box("Palm planter", position + Vector3.up * 0.10f, new Vector3(1.45f, 0.2f, 1.45f), _trim, true);
            var last = position;
            for (var i = 1; i <= 8; i++)
            {
                var t = i / 8f;
                var next = position + new Vector3(Mathf.Sin(t * 1.4f + variant) * t * 0.45f, t * height, 0.08f * t);
                Tube("Palm trunk", last, next, Mathf.Lerp(0.17f, 0.105f, t), _bark);
                last = next;
            }
            // One tapered-trunk approximation avoids seams between eight capsules.
            SolidCapsule("Palm trunk collision", position, last, 0.19f);
            var leafMesh = PalmLeaf();
            for (var i = 0; i < 15; i++)
            {
                var obj = new GameObject("Palm frond", typeof(MeshFilter), typeof(MeshRenderer));
                obj.transform.SetParent(_city, false);
                obj.transform.position = last;
                obj.transform.rotation = Quaternion.Euler(i % 3 * 12 - 8, i * 137.5f + variant * 20, 0);
                obj.transform.localScale = Vector3.one * (0.82f + i % 3 * 0.14f);
                obj.GetComponent<MeshFilter>().sharedMesh = leafMesh;
                obj.GetComponent<MeshRenderer>().sharedMaterial = _leaf;
            }
        }

        private static Mesh PalmLeaf()
        {
            if (Meshes.TryGetValue("PalmFrond", out var mesh)) return mesh;
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            for (var i = 0; i <= 14; i++)
            {
                var t = i / 14f;
                var center = new Vector3(0, Mathf.Sin(t * Mathf.PI) * 0.9f - t * t * 1.25f, t * 3.15f);
                var width = Mathf.Sin(Mathf.PI * t) * 0.46f * (i % 2 == 0 ? 1 : 0.68f);
                vertices.Add(center + new Vector3(-width, -width * 0.22f, 0));
                vertices.Add(center + Vector3.up * 0.03f);
                vertices.Add(center + new Vector3(width, -width * 0.22f, 0));
                uv.Add(new Vector2(0, t)); uv.Add(new Vector2(0.5f, t)); uv.Add(new Vector2(1, t));
                if (i == 0) continue;
                var start = (i - 1) * 3;
                for (var strip = 0; strip < 2; strip++)
                {
                    var a = start + strip;
                    triangles.AddRange(new[] { a, a + 3, a + 1, a + 1, a + 3, a + 4,
                        a + 1, a + 3, a, a + 4, a + 3, a + 1 });
                }
            }
            mesh = new Mesh { name = "PalmFrond" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            Meshes["PalmFrond"] = mesh;
            return mesh;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 size, Material material, bool collision = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.transform.SetParent(_city, false);
            obj.transform.position = position; obj.transform.localScale = size;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }

        private static void Cylinder(string name, Vector3 position, float radius, float height, Material material, bool collision = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name; obj.transform.SetParent(_city, false);
            obj.transform.position = position; obj.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            if (collision) SolidCapsule(name + " collision", position - Vector3.up * height * 0.5f, position + Vector3.up * height * 0.5f, radius);
        }

        private static void Tube(string name, Vector3 from, Vector3 to, float radius, Material material, bool collision = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name; obj.transform.SetParent(_city, false);
            obj.transform.position = (from + to) * 0.5f;
            obj.transform.rotation = Quaternion.FromToRotation(Vector3.up, to - from);
            obj.transform.localScale = new Vector3(radius * 2, Vector3.Distance(from, to) * 0.5f, radius * 2);
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            if (collision) SolidCapsule(name + " collision", from, to, radius);
        }

        private static void SolidCapsule(string name, Vector3 from, Vector3 to, float radius)
        {
            var obj = new GameObject(name, typeof(CapsuleCollider));
            obj.transform.SetParent(_city, false);
            obj.transform.SetPositionAndRotation((from + to) * 0.5f, Quaternion.FromToRotation(Vector3.up, to - from));
            obj.isStatic = true;
            var collider = obj.GetComponent<CapsuleCollider>();
            collider.radius = radius;
            collider.height = Vector3.Distance(from, to);
        }

        private static void Plane(string name, Vector3 position, float width, float length, Material material, bool collision = false)
        {
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(_city, false); obj.transform.position = position;
            var mesh = new Mesh { name = name };
            var columns = Mathf.Max(1, Mathf.CeilToInt(width / 45f));
            var rows = Mathf.Max(1, Mathf.CeilToInt(length / 45f));
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
            for (var z = 0; z <= rows; z++)
            {
                for (var x = 0; x <= columns; x++)
                {
                    vertices.Add(new Vector3(x * width / columns - width / 2, 0, z * length / rows - length / 2));
                    uv.Add(new Vector2(x * width / columns / 4, z * length / rows / 4));
                    if (z == 0 || x == 0) continue;
                    var a = (z - 1) * (columns + 1) + x - 1;
                    indices.AddRange(new[] { a, a + columns + 1, a + columns + 2, a, a + columns + 2, a + 1 });
                }
            }
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents();
            mesh = SaveMesh(name.Replace(' ', '_'), mesh);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (collision) obj.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void Sign(string text, Vector3 position, Quaternion rotation, float size, Color color)
        {
            var obj = new GameObject(text, typeof(TextMesh));
            obj.transform.SetParent(_city, false); obj.transform.SetPositionAndRotation(position, rotation);
            var sign = obj.GetComponent<TextMesh>();
            sign.font = _signFont;
            sign.text = text; sign.fontSize = 64; sign.fontStyle = FontStyle.Bold;
            sign.characterSize = size; sign.color = color; sign.anchor = TextAnchor.MiddleCenter;
            sign.alignment = TextAlignment.Center;
            obj.GetComponent<MeshRenderer>().sharedMaterial = _signMaterial;
        }

        private static Mesh SaveMesh(string name, Mesh mesh)
        {
            var path = MeshDirectory + name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); return saved;
        }

        private static void CombineStaticGeometry()
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var originals = new List<GameObject>();
            foreach (var filter in _city.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || filter.sharedMesh == null || renderer.sharedMaterials.Length != 1) continue;
                var material = renderer.sharedMaterial;
                if (!groups.TryGetValue(material, out var list)) { list = new List<CombineInstance>(); groups[material] = list; }
                list.Add(new CombineInstance { mesh = filter.sharedMesh, transform = filter.transform.localToWorldMatrix, subMeshIndex = 0 });
                originals.Add(filter.gameObject);
            }
            foreach (var group in groups)
            {
                var mesh = new Mesh { name = "CityBatch_" + group.Key.name, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                mesh = SaveMesh("CityBatch_" + group.Key.name, mesh);
                var obj = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                obj.transform.SetParent(_city, false);
                obj.GetComponent<MeshFilter>().sharedMesh = mesh;
                obj.GetComponent<MeshRenderer>().sharedMaterial = group.Key;
            }
            foreach (var obj in originals)
            {
                if (obj.GetComponent<Collider>() != null)
                {
                    Object.DestroyImmediate(obj.GetComponent<MeshFilter>());
                    Object.DestroyImmediate(obj.GetComponent<MeshRenderer>());
                }
                else Object.DestroyImmediate(obj);
            }
            Debug.Log($"Built textured city with {groups.Count} static material batches.");
        }
    }
}
