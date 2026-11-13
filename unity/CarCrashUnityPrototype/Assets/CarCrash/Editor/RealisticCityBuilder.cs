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
            RenderSettings.fogDensity = 0.0032f;
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
            _city.gameObject.AddComponent<CityVisibility>();
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
                Buildings(avenue);
                StreetDetails(CityRoadNetwork.AvenueName(avenue));
                neighborhood.position = Vector3.right * avenue;
            }
            _city = district;
            ExplorationPlaces();
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
            var grass=RealismMaterialLibrary.Standard("M_District_Grass",new Color(.23f,.29f,.16f),0,.15f);
            var centerZ=(CityRoadNetwork.MinZ+CityRoadNetwork.MaxZ)*.5f;
            var length=CityRoadNetwork.MaxZ-CityRoadNetwork.MinZ;
            var width=CityRoadNetwork.MaxX-CityRoadNetwork.MinX;
            Plane("District ground",new Vector3(-140,-.07f,centerZ),width+280,length+300,grass,true);
            foreach(var avenue in CityRoadNetwork.AvenueX)
            {
                Plane("Connected avenue",new Vector3(avenue,0,centerZ),18,length,_asphalt,true);
                var previous=CityRoadNetwork.MinZ+1;
                foreach(var crossing in CrossStreets)
                {
                    foreach(var side in new[]{-1f,1f})
                        BeveledSlab("Sidewalk foundation",new Vector3(avenue+side*10.7f,0,(previous+crossing-7)*.5f),3.35f,crossing-7-previous,_concrete,side<0 ? .15f : .85f,side<0 ? .85f : .15f);
                    previous=crossing+7;
                }
                foreach(var side in new[]{-1f,1f})
                    BeveledSlab("Sidewalk foundation",new Vector3(avenue+side*10.7f,0,(previous+CityRoadNetwork.MaxZ-1)*.5f),3.35f,CityRoadNetwork.MaxZ-1-previous,_concrete,side<0 ? .15f : .85f,side<0 ? .85f : .15f);
                foreach(var z in new[]{CityRoadNetwork.MinZ+15,CityRoadNetwork.MaxZ-15})
                    Plane("Street turnaround",new Vector3(avenue,.009f,z),30,28,_asphalt,true);
            }
            var segments=new List<Vector2>();var left=CityRoadNetwork.MinX;
            foreach(var avenue in CityRoadNetwork.AvenueX){if(avenue-12.4f>left)segments.Add(new Vector2(left,avenue-12.4f));left=avenue+12.4f;}
            segments.Add(new Vector2(left,CityRoadNetwork.MaxX));
            foreach(var z in CrossStreets)
            {
                Plane("Connected cross street",new Vector3(0,.006f,z),width,14,_asphalt,true);
                foreach(var segment in segments)
                {
                    var x=(segment.x+segment.y)*.5f;var span=segment.y-segment.x;
                    foreach(var side in new[]{-1f,1f})
                    {
                        var hasDriveway=false;var drivewayX=0f;
                        if(segment.x<296&&segment.y>296){hasDriveway=true;drivewayX=296;}
                        if(side<0&&Mathf.Abs(z-510)<.1f&&segment.x<108&&segment.y>108){hasDriveway=true;drivewayX=108;}
                        if(side<0&&Mathf.Abs(z-610)<.1f&&segment.x < -216&&segment.y > -216){hasDriveway=true;drivewayX=-216;}
                        if(side>0)foreach(var place in CityRoadNetwork.Places)
                            if(Mathf.Abs(place.entrance.z-z)<.1f&&place.position.z>z+10&&place.entrance.x>segment.x&&place.entrance.x<segment.y)
                            {hasDriveway=true;drivewayX=place.entrance.x;break;}
                        if(hasDriveway)
                        {
                            var a=Mathf.Max(segment.x,drivewayX-7);var b=Mathf.Min(segment.y,drivewayX+7);
                            BeveledSlab("Driveway sidewalk left",new Vector3((segment.x+a)*.5f,0,z+side*8.7f),a-segment.x,3.35f,_concrete);
                            BeveledSlab("Driveway sidewalk right",new Vector3((b+segment.y)*.5f,0,z+side*8.7f),segment.y-b,3.35f,_concrete);
                            Plane("Destination driveway",new Vector3(drivewayX,.012f,z+side*10),14,10,_asphalt,true);
                            if(span>45)Palm(new Vector3(drivewayX-14,.14f,z+side*8.7f),0);
                        }
                        else
                        {
                            BeveledSlab("Cross street sidewalk",new Vector3(x,0,z+side*8.7f),span,3.35f,_concrete);
                            if(span>23)Palm(new Vector3(x,.14f,z+side*8.7f),0);
                        }
                    }
                    foreach(var line in new[]{-.12f,.12f})Box("Cross street centerline",new Vector3(x,.018f,z+line),new Vector3(span,.012f,.07f),_yellow);
                }
                foreach(var avenue in CityRoadNetwork.AvenueX)
                foreach(var side in new[]{-1f,1f})
                {
                    for(var x=-7.8f;x<8.5f;x+=1.45f)Box("Zebra crossing",new Vector3(avenue+x,.018f,z+side*8.9f),new Vector3(.8f,.015f,2.5f),_white);
                    Box("Stop line",new Vector3(avenue+side*4.6f,.019f,z+side*11),new Vector3(7.9f,.016f,.22f),_white);
                }
            }
            for(var z=CityRoadNetwork.MinZ+6;z<CityRoadNetwork.MaxZ-6;z+=5.6f)
            {
                if(NearCrossStreet(z,10))continue;
                foreach(var avenue in CityRoadNetwork.AvenueX)
                {
                    foreach(var x in new[]{-3.3f,3.3f})Box("Worn lane paint",new Vector3(avenue+x,.012f,z),new Vector3(.085f,.018f,2.5f),_white);
                    foreach(var x in new[]{-.12f,.12f})Box("Double yellow centerline",new Vector3(avenue+x,.014f,z),new Vector3(.07f,.016f,5.65f),_yellow);
                }
            }
            foreach(var x in new[]{CityRoadNetwork.MinX,CityRoadNetwork.MaxX})
                Box("District perimeter wall",new Vector3(x,.4f,centerZ),new Vector3(.5f,.8f,length),_stone,true);
            foreach(var z in new[]{CityRoadNetwork.MinZ,CityRoadNetwork.MaxZ})
                Box("District perimeter wall",new Vector3(0,.4f,z),new Vector3(width,.8f,.5f),_stone,true);
        }

        private static void BeveledSlab(string name,Vector3 position,float width,float length,Material material,float leftBevel=-1,float rightBevel=-1)
        {
            if(width<.2f||length<.2f)return;
            var key="Beveled_"+width.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"x"+length.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"_"+leftBevel+"_"+rightBevel;
            if(!Meshes.TryGetValue(key,out var mesh))
            {
                var w=width*.5f;var l=length*.5f;var bevel=Mathf.Min(.85f,w*.8f,l*.8f);
                var left=leftBevel<0 ? bevel : Mathf.Min(leftBevel,w*.8f);
                var right=rightBevel<0 ? bevel : Mathf.Min(rightBevel,w*.8f);
                var vertices=new[]{new Vector3(-w,.005f,-l),new Vector3(w,.005f,-l),new Vector3(w,.005f,l),new Vector3(-w,.005f,l),
                    new Vector3(-w+left,.145f,-l+bevel),new Vector3(w-right,.145f,-l+bevel),new Vector3(w-right,.145f,l-bevel),new Vector3(-w+left,.145f,l-bevel)};
                var triangles=new List<int>{4,6,5,4,7,6,0,1,2,0,2,3};
                for(var i=0;i<4;i++){var j=(i+1)%4;triangles.AddRange(new[]{i,i+4,j+4,i,j+4,j});}
                mesh=new Mesh{name=key,vertices=vertices,triangles=triangles.ToArray()};
                var uv=new Vector2[8];for(var i=0;i<8;i++)uv[i]=new Vector2(vertices[i].x/2,vertices[i].z/2);
                mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh=SaveMesh(key,mesh);Meshes[key]=mesh;
            }
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
            obj.transform.SetParent(_city,false);obj.transform.position=position;obj.isStatic=true;
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshCollider>().sharedMesh=mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial=material;
        }

        private static bool NearCrossStreet(float z, float radius)
        {
            foreach (var crossing in CrossStreets) if (Mathf.Abs(z - crossing) < radius) return true;
            return false;
        }

        private static void Buildings(float avenue)
        {
            var shopNames = new[] { "ELYSIAN MOTORS", "SUNDOWN COFFEE", "PACIFIC MARKET", "HOTEL PALOMA", "VINYL & CO.", "WESTSIDE DINER" };
            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 48; i++)
                {
                    var z = -360f + i * 21f;
                    if(Mathf.Abs(avenue)>100 && i%2==1)continue;
                    if(avenue==270 && side>0)continue;
                    if(((avenue==54 && side>0)||(avenue==162 && side<0)) && z>410 && z<510)continue;
                    if(((avenue==-270 && side>0)||(avenue==-162 && side<0)) && ((z>187 && z<266)||(z>510 && z<610)))continue;
                    if (NearCrossStreet(z, 16f)) continue;
                    var width = 10f + i % 3 * 1.3f;
                    var depth = 15.5f + i % 2 * 1.5f;
                    var height = Mathf.Abs(avenue)>100 ? 6.4f+i%3*2.8f : 10.8f+i%5*3.3f;
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
                    Box("Shop canopy", new Vector3(facade - side * 0.62f, 3.38f, z), new Vector3(1.35f, 0.17f, depth * 0.84f), awning, true);
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
            for (var i = 0; i < 44; i++)
            {
                var z = -355f + i * 22.5f;
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

        private static void ExplorationPlaces()
        {
            var grass=RealismMaterialLibrary.Standard("M_Park_Grass",new Color(.21f,.34f,.13f),0,.14f);
            var water=RealismMaterialLibrary.Standard("M_Coastal_Water",new Color(.07f,.29f,.34f),.35f,.88f);
            water.shader=Shader.Find("CarCrash/CoastalWater");water.color=new Color(.045f,.24f,.30f);EditorUtility.SetDirty(water);
            var sand=RealismMaterialLibrary.Standard("M_Beach_Sand",new Color(.73f,.63f,.43f),0,.13f);
            var red=RealismMaterialLibrary.Standard("M_Warehouse_Container_Red",new Color(.45f,.15f,.10f),.3f,.38f);
            var blue=RealismMaterialLibrary.Standard("M_Warehouse_Container_Blue",new Color(.10f,.24f,.32f),.35f,.38f);
            var park=_city;
            _city=new GameObject("Explorable landmarks").transform;_city.SetParent(park,false);
            Plane("Coastal access drive",new Vector3(296,.009f,115),26,1000,_asphalt,true);
            Plane("Coastal sand",new Vector3(330,-.055f,120),40,1130,sand);
            Plane("Ocean water",new Vector3(515,-.075f,120),330,1600,water);
            for(var z=-350f;z<620;z+=37)
            {
                Palm(new Vector3(307,.14f,z),0);
                Box("Coast parking stripe",new Vector3(299,.022f,z+9),new Vector3(8,.015f,.12f),_white);
            }
            Plane("Marina forecourt",new Vector3(296,.017f,555),25,76,_concrete,true);
            Plane("Marina water basin",new Vector3(354,-.045f,566),88,145,water);
            Box("Marina office",new Vector3(286,2.2f,578),new Vector3(10,4.4f,12),_stone,true);
            Box("Marina office glazing",new Vector3(291.1f,2.2f,578),new Vector3(.1f,2.8f,8),_glass);
            LandmarkSign("SUNSET MARINA",new Vector3(285,4.6f,540),12);
            foreach(var z in new[]{530f,565f,598f})
            {
                Box("Timber marina pier",new Vector3(333,.12f,z),new Vector3(48,.35f,4),_bark);
                for(var x=313f;x<357;x+=10)Cylinder("Pier piling",new Vector3(x,-.4f,z),.19f,1.8f,_bark);
                for(var i=0;i<3;i++)
                {
                    var boat=new Vector3(320+i*14,-.02f,z+7);
                    BoatHull(boat);
                    Box("Launch cabin",boat+new Vector3(0,1.05f,-.3f),new Vector3(2.7f,1.1f,3.2f),_trim);
                    Box("Launch windshield",boat+new Vector3(0,1.2f,1.34f),new Vector3(2.5f,.7f,.05f),_glass);
                    foreach(var side in new[]{-1f,1f})
                    {
                        Box("Launch side glazing",boat+new Vector3(side*1.37f,1.2f,-.3f),new Vector3(.05f,.65f,2.5f),_glass);
                        Tube("Launch deck rail",boat+new Vector3(side*1.6f,.9f,-3),boat+new Vector3(side*1.6f,.9f,1.7f),.028f,_metal);
                    }
                    Tube("Launch aerial",boat+Vector3.up*1.5f,boat+Vector3.up*3.9f,.025f,_metal);
                }
            }
            Plane("Vista boardwalk",new Vector3(296,.018f,-235),25,75,_bark,true);
            LandmarkSign("VISTA BEACH",new Vector3(286,3.8f,-258),11);
            for(var i=0;i<4;i++)
            {
                var z=-254+i*15f;
                Box("Beach kiosk",new Vector3(305,1.4f,z),new Vector3(4,2.8f,4.5f),i%2==0 ? blue : red,true);
                Box("Kiosk canopy",new Vector3(303.8f,3,z),new Vector3(6.5f,.16f,5.2f),_trim);
                Cylinder("Beach shade pole",new Vector3(336,.9f,z),.045f,2.1f,_metal);
                var shade=Box("Beach sunshade",new Vector3(336,2,z),new Vector3(4,.15f,3.2f),_white);
                shade.transform.rotation=Quaternion.Euler(0,i*24,8);
            }
            Plane("Paloma park drive",new Vector3(108,.014f,455),78,80,_concrete,true);
            Plane("Paloma park lawn",new Vector3(108,.024f,467),51,43,grass,true);
            Box("Park fountain basin",new Vector3(108,.2f,470),new Vector3(9,.4f,9),_trim,true);
            Plane("Fountain water",new Vector3(108,.415f,470),8.2f,8.2f,water);
            Cylinder("Fountain monument",new Vector3(108,1.7f,470),.55f,2.9f,_stone,true);
            LandmarkSign("PALOMA PARK",new Vector3(93,3.6f,425),12);
            for(var i=0;i<8;i++)
            {
                var angle=i*Mathf.PI/4;
                Palm(new Vector3(108+Mathf.Cos(angle)*31,.14f,459+Mathf.Sin(angle)*29),i%3);
            }
            foreach(var x in new[]{88f,128f})foreach(var z in new[]{444f,480f})
            {
                Box("Park bench seat",new Vector3(x,.55f,z),new Vector3(3,.18f,.65f),_bark,true);
                Box("Park bench back",new Vector3(x,.95f,z+.33f),new Vector3(3,.65f,.12f),_metal,true);
            }
            Plane("Freight service yard",new Vector3(-216,.015f,225),82,57,_asphalt,true);
            Box("Freight warehouse",new Vector3(-216,4.2f,250),new Vector3(65,8.4f,17),_stone,true);
            Box("Freight warehouse roof",new Vector3(-216,8.6f,250),new Vector3(67,.4f,19),_metal);
            for(var i=0;i<4;i++)
            {
                Box("Warehouse loading shutter",new Vector3(-238+i*14,2.2f,241.4f),new Vector3(9,4.2f,.08f),blue);
                Box("Cargo container",new Vector3(-247,1.35f,202+i*11),new Vector3(10,2.7f,5),i%2==0 ? blue : red,true);
                for(var rib=0;rib<11;rib++)Box("Container corrugation",new Vector3(-252.1f,1.4f,200+i*11+rib*.4f),new Vector3(.08f,2.4f,.07f),_metal);
            }
            Sign("FOUNDRY FREIGHT / DEPOT 04",new Vector3(-216,6.9f,241.3f),Quaternion.identity,.19f,new Color(1,.84f,.45f));
            Plane("Motor park circuit",new Vector3(-216,.014f,555),84,80,_asphalt,true);
            Plane("Motor park grass infield",new Vector3(-216,.024f,555),52,40,grass,true);
            foreach(var x in new[]{-253f,-179f})Box("Circuit edge marking",new Vector3(x,.031f,555),new Vector3(.16f,.014f,65),_white);
            foreach(var z in new[]{521f,589f})Box("Circuit edge marking",new Vector3(-216,.031f,z),new Vector3(73,.014f,.16f),_white);
            for(var i=0;i<12;i++)Box("Circuit start stripe",new Vector3(-241+i*4,.035f,529),new Vector3(2,.014f,1.8f),i%2==0 ? _white : _metal);
            foreach(var x in new[]{-244f,-188f})Box("Motor park entrance post",new Vector3(x,3.4f,519),new Vector3(.5f,6.8f,.5f),_metal,true);
            Box("Motor park entrance gantry",new Vector3(-216,6.8f,519),new Vector3(56,.6f,.5f),_metal);
            Sign("APEX MOTOR PARK",new Vector3(-216,6.8f,518.65f),Quaternion.identity,.25f,Color.white);
            Box("Paddock control cabin",new Vector3(-243,1.9f,597),new Vector3(11,3.8f,5),blue,true);
            foreach(var place in CityRoadNetwork.Places)
            {
                Box("Destination arrival marking",place.position+Vector3.up*.025f,new Vector3(5,.018f,.12f),_yellow);
            }
            _city=park;
        }

        private static void LandmarkSign(string text,Vector3 center,float width)
        {
            Box("Landmark sign board",center,new Vector3(width,1.25f,.18f),_metal);
            foreach(var side in new[]{-1f,1f})Tube("Landmark sign post",new Vector3(center.x+side*(width*.5f-.4f),0,center.z),
                new Vector3(center.x+side*(width*.5f-.4f),center.y,center.z),.065f,_metal,true);
            Sign(text,center+Vector3.back*.11f,Quaternion.identity,.13f,new Color(1,.88f,.57f));
        }

        private static void BoatHull(Vector3 position)
        {
            const string key="CurvedMarinaLaunch";
            if(!Meshes.TryGetValue(key,out var mesh))
            {
                var sections=new[]{new Vector2(-4.1f,1.45f),new Vector2(-2.5f,1.85f),new Vector2(0,2f),new Vector2(2.5f,1.45f),new Vector2(4.6f,.08f)};
                var vertices=new List<Vector3>();var triangles=new List<int>();
                foreach(var section in sections)
                {
                    vertices.Add(new Vector3(-section.y,.42f,section.x));vertices.Add(new Vector3(section.y,.42f,section.x));
                    vertices.Add(new Vector3(section.y*.65f,-.12f,section.x));vertices.Add(new Vector3(-section.y*.65f,-.12f,section.x));
                }
                for(var ring=0;ring<sections.Length-1;ring++)for(var side=0;side<4;side++)
                {
                    var a=ring*4+side;var b=ring*4+(side+1)%4;
                    triangles.AddRange(new[]{a,b+4,b,a,a+4,b+4});
                }
                triangles.AddRange(new[]{0,1,2,0,2,3,16,18,17,16,19,18});
                mesh=new Mesh{name=key};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
                mesh=SaveMesh(key,mesh);Meshes[key]=mesh;
            }
            var hull=new GameObject("Curved marina launch",typeof(MeshFilter),typeof(MeshRenderer));
            hull.transform.SetParent(_city,false);hull.transform.position=position;
            hull.GetComponent<MeshFilter>().sharedMesh=mesh;hull.GetComponent<MeshRenderer>().sharedMaterial=_white;
        }

        private static void Skyline()
        {
            foreach (var side in new[] { -1 })
            {
                for (var i = 0; i < 22; i++)
                {
                    var height = 24f + (i * 17 % 57);
                    // Keep towers outside the connected avenues and cross streets.
                    var x = side * (350f + i % 3 * 20f);
                    var z = -330f + i * 45f;
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
            mesh = SaveMesh(name.Replace(' ', '_')+"_"+width.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"x"+length.ToString("F2",System.Globalization.CultureInfo.InvariantCulture), mesh);
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
            var groups = new Dictionary<(Material material,int x,int z), List<CombineInstance>>();
            var originals = new List<GameObject>();
            foreach (var filter in _city.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || filter.sharedMesh == null || renderer.sharedMaterials.Length != 1) continue;
                var material = renderer.sharedMaterial;
                var center=renderer.bounds.center;
                var key=(material,Mathf.FloorToInt(center.x/160),Mathf.FloorToInt(center.z/160));
                if (!groups.TryGetValue(key, out var list)) { list = new List<CombineInstance>(); groups[key] = list; }
                list.Add(new CombineInstance { mesh = filter.sharedMesh, transform = filter.transform.localToWorldMatrix, subMeshIndex = 0 });
                originals.Add(filter.gameObject);
            }
            foreach (var group in groups)
            {
                var mesh = new Mesh { name = "CityBatch_" + group.Key.material.name+"_"+group.Key.x+"_"+group.Key.z, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                mesh = SaveMesh(mesh.name, mesh);
                var obj = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                obj.transform.SetParent(_city, false);
                obj.GetComponent<MeshFilter>().sharedMesh = mesh;
                obj.GetComponent<MeshRenderer>().sharedMaterial = group.Key.material;
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
