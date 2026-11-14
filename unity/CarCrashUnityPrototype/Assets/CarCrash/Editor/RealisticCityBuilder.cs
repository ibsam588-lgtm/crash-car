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
        private static Material _asphalt, _concrete, _stone, _metal, _glass, _shopGlass, _warmGlass, _trim, _white, _yellow;
        private static Material _bark, _palmBark, _leaf, _light;
        private static Material _signMaterial;
        private static Font _signFont;
        private static float[] CrossStreets => CityRoadNetwork.CrossStreetZ;
        private static readonly Dictionary<string, Mesh> Meshes = new();

        public static void CreateLighting()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            QualitySettings.SetQualityLevel(3, true);
            QualitySettings.antiAliasing = 4;
            QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
            QualitySettings.pixelLightCount = 3;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 115f;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowCascade4Split=new Vector3(.08f,.25f,.55f);
            QualitySettings.realtimeReflectionProbes = true;
            var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/CarCrash/Materials/Realism/M_Venice_Sunset.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Panoramic"));
                AssetDatabase.CreateAsset(sky, "Assets/CarCrash/Materials/Realism/M_Venice_Sunset.mat");
            }
            sky.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(RealismMaterialLibrary.TextureRoot + "Realism/venice_sunset_hdri.hdr"));
            sky.SetFloat("_Exposure", 0.72f);
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
            RenderSettings.fogColor = new Color(0.47f, 0.53f, 0.60f);

            var sun = new GameObject("Golden hour sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.91f, 0.79f);
            sun.intensity = 1.55f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.88f;
            sun.shadowBias = 0.015f;
            sun.shadowNormalBias = 0.13f;
            sun.transform.rotation = Quaternion.Euler(24f, -38f, 0f);
            RenderSettings.sun = sun;
            var fill = new GameObject("Cool sky fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.60f, 0.72f, 1f);
            fill.intensity = 0.12f;
            fill.transform.rotation = Quaternion.Euler(54f, 145f, 0f);

            var probe = new GameObject("Street reflection probe").AddComponent<ReflectionProbe>();
            probe.transform.position = new Vector3(0, 3, -8);
            probe.mode = ReflectionProbeMode.Custom;
            probe.hdr = false;
            probe.resolution = 256;
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
            _signFont = Resources.Load<Font>("Fonts/Inter-Medium") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            _asphalt = RealismMaterialLibrary.WorldSurface("M_City_Asphalt", "asphalt_02", new Color(.64f,.65f,.67f),.25f,.8f,.50f,.55f);
            _concrete = RealismMaterialLibrary.WorldSurface("M_City_Paving", "concrete_pavement", new Color(.80f,.80f,.77f),.4f,.7f,.30f);
            _stone = RealismMaterialLibrary.WorldSurface("M_City_Concrete","plastered_wall_02",new Color(.62f,.62f,.59f),.55f,.5f,.28f);
            _metal = RealismMaterialLibrary.Standard("M_City_Powdercoat", new Color(0.055f, 0.065f, 0.073f), 0.6f, 0.6f);
            _glass = RealismMaterialLibrary.Window("M_City_Window_Glass",new Color(.10f,.14f,.18f),.12f);
            _warmGlass = RealismMaterialLibrary.Window("M_City_Lit_Window",new Color(.24f,.23f,.19f),.45f);
            _shopGlass=RealismMaterialLibrary.Window("M_City_Shopfront_Glass",new Color(.12f,.14f,.16f),.65f);
            _shopGlass.SetFloat("_Shopfront",1);
            _trim = RealismMaterialLibrary.WorldSurface("M_City_Limestone_Trim","plastered_wall_02",new Color(.78f,.76f,.69f),.7f,.3f,.32f,.25f);
            _white = RealismMaterialLibrary.Standard("M_City_Aged_White_Paint", new Color(0.73f, 0.72f, 0.65f), 0f, 0.22f);
            _yellow = RealismMaterialLibrary.Standard("M_City_Road_Yellow", new Color(0.85f, 0.59f, 0.10f), 0f, 0.25f);
            _bark=RealismMaterialLibrary.WorldSurface("M_City_Weathered_Timber","wood_planks",new Color(.69f,.61f,.50f),.66f,.6f,.32f,.5f);
            _palmBark = RealismMaterialLibrary.Standard("M_Palm_Bark", new Color(0.19f, 0.14f, 0.09f), 0f, 0.1f);
            _palmBark.shader=Shader.Find("CarCrash/PalmBark");
            _leaf = RealismMaterialLibrary.Standard("M_Palm_Leaf", new Color(0.17f, 0.25f, 0.10f), 0f, 0.3f);
            _leaf.shader=Shader.Find("CarCrash/PalmFoliage");_leaf.color=new Color(.13f,.26f,.075f);
            _leaf.enableInstancing=true;
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
            CreateFoliageInstances();
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
            var cube = new Cubemap(256, TextureFormat.RGBA32, true) { name = "Elysian street reflection" };
            if (!camera.RenderToCubemap(cube)) throw new System.InvalidOperationException("Street reflection capture failed.");
            FilterReflectionMipmaps(cube);
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

        private static void FilterReflectionMipmaps(Cubemap cube)
        {
            // A raw camera capture has ordinary image mipmaps, not a roughness
            // filter. Broaden the lower levels in linear light before saving
            // byte faces, avoiding both chrome-like paint and HDR WebGL issues.
            cube.Apply(true,false);cube.filterMode=FilterMode.Trilinear;
            for(var face=0;face<6;face++)
            for(var level=1;level<cube.mipmapCount;level++)
            {
                var size=Mathf.Max(1,cube.width>>level);var pixels=cube.GetPixels((CubemapFace)face,level);
                for(var i=0;i<pixels.Length;i++)pixels[i]=pixels[i].linear;
                var horizontal=new Color[pixels.Length];var filtered=new Color[pixels.Length];
                var radius=Mathf.Min(Mathf.Clamp(level+1,2,5),Mathf.Max(1,size/2));
                for(var y=0;y<size;y++)for(var x=0;x<size;x++)
                {
                    var sum=Color.clear;var weight=0f;
                    for(var offset=-radius;offset<=radius;offset++)
                    {
                        var w=Mathf.Exp(-offset*offset/(float)(radius*radius));
                        sum+=pixels[y*size+Mathf.Clamp(x+offset,0,size-1)]*w;weight+=w;
                    }
                    horizontal[y*size+x]=sum/weight;
                }
                for(var y=0;y<size;y++)for(var x=0;x<size;x++)
                {
                    var sum=Color.clear;var weight=0f;
                    for(var offset=-radius;offset<=radius;offset++)
                    {
                        var w=Mathf.Exp(-offset*offset/(float)(radius*radius));
                        sum+=horizontal[Mathf.Clamp(y+offset,0,size-1)*size+x]*w;weight+=w;
                    }
                    filtered[y*size+x]=(sum/weight).gamma;
                }
                cube.SetPixels(filtered,(CubemapFace)face,level);
            }
            cube.Apply(false,false);
        }

        private static void Ground()
        {
            var grass=RealismMaterialLibrary.WorldSurface("M_District_Grass","leafy_grass",new Color(.65f,.68f,.58f),.45f,.75f,.2f,.7f);
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
                    var plaster=i%6 is 1 or 4;
                    var tint = i % 3 == 0 ? new Color(.83f,.80f,.75f) : i % 3 == 1 ? new Color(.80f,.82f,.80f) : new Color(.63f,.64f,.62f);
                    var wall = RealismMaterialLibrary.WorldSurface("M_Facade_Surface_" + i % 6,plaster ? "plastered_wall_02" : "brick_wall_003",tint,plaster ? .5f : .45f,.65f,.26f,.65f);
                    // Keep the full original collision envelope, but set the
                    // rendered core back behind real window/shop recesses.
                    var core=Box("Masonry building",new Vector3(centerX+side*.24f,height*.5f,z),new Vector3(width-.48f,height,depth),wall,true);
                    core.GetComponent<BoxCollider>().size=new Vector3(width/(width-.48f),1,1);
                    core.GetComponent<BoxCollider>().center=new Vector3(-side*.24f/(width-.48f),0,0);
                    RecessedFacade(side,facade,z,depth,height,wall);
                    Box("Roof parapet", new Vector3(centerX, height + 0.28f, z), new Vector3(width + 0.28f, 0.56f, depth + 0.28f), _trim);
                    Box("Upper cornice", new Vector3(facade - side * 0.15f, height - 0.65f, z), new Vector3(0.52f, 0.20f, depth + 0.35f), _trim);
                    for (var y = 4.9f; y < height - 1; y += 2.8f)
                    {
                        Box("Floor stringcourse", new Vector3(facade - side * 0.08f, y - 1.2f, z), new Vector3(0.30f, 0.10f, depth), _trim);
                        for (var column = -2; column <= 2; column++)
                        {
                            var wz = z + column * 2.7f;
                            var lit = (column + Mathf.RoundToInt(y) + i) % 7 == 0;
                            foreach(var edge in new[]{-1f,1f})
                                Box("Deep window jamb",new Vector3(facade+side*.08f,y,wz+edge*.64f),new Vector3(.50f,1.84f,.14f),_trim);
                            Box("Window lintel",new Vector3(facade+side*.08f,y+.84f,wz),new Vector3(.50f,.16f,1.40f),_trim);
                            Box("Recessed glazing",new Vector3(facade+side*.29f,y,wz),new Vector3(.035f,1.53f,1.10f),lit ? _warmGlass : _glass);
                            Box("Window mullion",new Vector3(facade+side*.26f,y,wz),new Vector3(.05f,1.56f,.045f),_metal);
                            Box("Window sill", new Vector3(facade - side * 0.30f, y - 0.90f, wz), new Vector3(0.38f, 0.12f, 1.50f), _trim);
                        }
                    }
                    for (var column = -1; column <= 1; column++)
                    {
                        var shopZ=z+column*4.3f;
                        Box("Shop glazing",new Vector3(facade+side*.28f,1.55f,shopZ),new Vector3(.05f,2.45f,3.6f),_shopGlass);
                        Box("Shop window frame",new Vector3(facade+side*.20f,1.6f,shopZ+1.72f),new Vector3(.14f,2.75f,.08f),_metal);
                        Box("Shop window transom",new Vector3(facade+side*.18f,2.44f,shopZ),new Vector3(.14f,.06f,3.55f),_metal);
                        if(column==0)
                        {
                            Box("Entrance door mullion",new Vector3(facade+side*.18f,1.45f,shopZ-.85f),new Vector3(.14f,2.6f,.065f),_metal);
                            Box("Shop door pull",new Vector3(facade+side*.08f,1.3f,shopZ-.64f),new Vector3(.10f,.46f,.035f),_trim);
                            Box("Recessed entrance threshold",new Vector3(facade+side*.1f,.18f,shopZ-1.25f),new Vector3(.75f,.055f,1.22f),_stone);
                        }
                    }
                    var awning = RealismMaterialLibrary.Standard("M_Awning_" + i % 3, i % 3 == 0 ? new Color(0.22f, 0.31f, 0.25f) : i % 3 == 1 ? new Color(0.40f, 0.16f, 0.12f) : new Color(0.20f, 0.23f, 0.29f));
                    Box("Shop canopy", new Vector3(facade - side * 0.62f, 3.38f, z), new Vector3(1.35f, 0.17f, depth * 0.84f), awning, true);
                    var sign = Box("Shop fascia", new Vector3(facade - side * 0.38f, 3.92f, z), new Vector3(0.15f, 0.71f, depth * 0.82f), _metal);
                    Sign(shopNames[(i + (side > 0 ? 2 : 0)) % shopNames.Length], sign.transform.position - Vector3.right * side * 0.10f,
                        Quaternion.Euler(0, side * 90f, 0), 0.20f, new Color(0.90f, 0.89f, 0.82f),depth*.73f,.42f);
                    FacadeDetails(i,side,centerX,facade,z,width,depth,height);
                    if(Mathf.Abs(avenue)<=54)UrbanArchitecture(i,side,facade,z,depth,height,awning);
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

        private static void RecessedFacade(int side,float facade,float z,float depth,float height,Material wall)
        {
            var x=facade+side*.16f;
            Box("Ground floor stone plinth",new Vector3(x,.30f,z),new Vector3(.60f,.35f,depth),_stone);
            Box("Shopfront stone header",new Vector3(x,3.26f,z),new Vector3(.60f,.82f,depth),_stone);
            var previous=-depth*.5f;
            for(var column=-1;column<=1;column++)
            {
                var left=column*4.3f-1.8f;
                Box("Shopfront masonry pier",new Vector3(x,1.65f,z+(previous+left)*.5f),new Vector3(.60f,2.75f,left-previous),_stone);
                previous=column*4.3f+1.8f;
            }
            Box("Shopfront masonry pier",new Vector3(x,1.65f,z+(previous+depth*.5f)*.5f),new Vector3(.60f,2.75f,depth*.5f-previous),_stone);
            var below=3.67f;
            for(var y=4.9f;y<height-1;y+=2.8f)
            {
                var bottom=y-.92f;var top=y+.92f;
                Box("Facade masonry floor band",new Vector3(x,(below+bottom)*.5f,z),new Vector3(.60f,bottom-below,depth),wall);
                previous=-depth*.5f;
                for(var column=-2;column<=2;column++)
                {
                    var left=column*2.7f-.70f;
                    Box("Facade masonry window pier",new Vector3(x,y,z+(previous+left)*.5f),new Vector3(.60f,1.84f,left-previous),wall);
                    previous=column*2.7f+.70f;
                }
                Box("Facade masonry window pier",new Vector3(x,y,z+(previous+depth*.5f)*.5f),new Vector3(.60f,1.84f,depth*.5f-previous),wall);
                below=top;
            }
            if(height>below)Box("Facade masonry parapet band",new Vector3(x,(height+below)*.5f,z),new Vector3(.60f,height-below,depth),wall);
        }

        private static void UrbanArchitecture(int index,int side,float facade,float z,float depth,float height,Material awning)
        {
            // Service details sit against the existing building envelope and
            // above the pedestrian clearance, outside all driving lanes.
            var x=facade-side*.42f;
            var end=z+depth*.5f-.52f;
            Tube("Rainwater downpipe",new Vector3(x,.4f,end),new Vector3(x,height-.4f,end),.045f,_metal);
            foreach(var y in new[]{1.2f,3.5f,6.4f,9.2f})
                if(y<height)Box("Downpipe wall bracket",new Vector3(x+side*.08f,y,end),new Vector3(.20f,.055f,.15f),_metal);
            var box=new Vector3(facade-side*.23f,1.17f,z-depth*.5f+.65f);
            Box("Utility meter enclosure",box,new Vector3(.28f,.68f,.46f),_metal);
            Box("Meter glass face",box+new Vector3(-side*.15f,.08f,0),new Vector3(.025f,.23f,.30f),_glass);
            Tube("Meter service conduit",box+Vector3.up*.32f,box+Vector3.up*2.1f,.022f,_metal);
            if(index%3==1)
            {
                var escapeZ=z+3.95f;var escapeX=facade-side*.79f;
                for(var y=4.1f;y<Mathf.Min(height-1,13);y+=2.8f)
                {
                    Box("Fire escape landing",new Vector3(escapeX,y,escapeZ),new Vector3(1.20f,.075f,3.1f),_metal);
                    Box("Fire escape handrail",new Vector3(escapeX-side*.56f,y+1.05f,escapeZ),new Vector3(.045f,.045f,3.1f),_metal);
                    for(var n=0;n<8;n++)
                        Box("Fire escape baluster",new Vector3(escapeX-side*.56f,y+.55f,escapeZ-1.4f+n*.4f),new Vector3(.028f,1.04f,.028f),_metal);
                    if(y+2.8f<height-1)
                    {
                        for(var step=0;step<12;step++)
                            Box("Fire escape stair tread",new Vector3(escapeX,y+step*.233f,escapeZ-1.3f+step*.22f),new Vector3(.90f,.045f,.24f),_metal);
                        Tube("Fire escape diagonal rail",new Vector3(escapeX-side*.49f,y+.95f,escapeZ-1.3f),new Vector3(escapeX-side*.49f,y+3.52f,escapeZ+1.12f),.024f,_metal);
                    }
                }
            }
            if(index%4==0)
            {
                var shutterZ=z-4.3f;
                for(var n=0;n<15;n++)Box("Closed shop shutter slat",new Vector3(facade+side*.10f,.45f+n*.145f,shutterZ),new Vector3(.055f,.10f,3.36f),_metal);
                Box("Rolled shutter hood",new Vector3(facade-side*.12f,2.78f,shutterZ),new Vector3(.55f,.32f,3.58f),_trim);
            }
            if(index%4==2)
            {
                var top=new Vector3(facade-side*.84f,3.48f,z);
                for(var rib=-5;rib<=5;rib++)
                {
                    var panel=Box("Fabric awning panel",top+Vector3.forward*rib*1.05f,new Vector3(1.65f,.06f,1.02f),rib%2==0 ? awning : _trim);
                    panel.transform.rotation=Quaternion.Euler(0,0,side*10);
                }
                Box("Awning scallop valance",top+new Vector3(-side*.8f,-.22f,0),new Vector3(.055f,.30f,11.55f),awning);
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
                    Sign(streetName, signPosition + Vector3.back * 0.05f, Quaternion.identity, 0.065f, Color.white,1.85f,.22f);
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
            StreetSurfaceDetails();
            UrbanStreetFurniture();
        }

        private static void UrbanStreetFurniture()
        {
            var blue=RealismMaterialLibrary.Standard("M_Urban_Mailbox",new Color(.08f,.15f,.23f),.35f,.32f);
            for(var i=0;i<18;i++)
            {
                var z=-325+i*52f;if(NearCrossStreet(z,13))continue;
                var side=i%2==0 ? -1f : 1f;var x=side*9.83f;
                Tube("Parking meter post",new Vector3(x,.16f,z),new Vector3(x,1.1f,z),.035f,_metal,true);
                Box("Parking meter head",new Vector3(x,1.28f,z),new Vector3(.20f,.36f,.26f),_metal,true);
                Box("Meter display",new Vector3(x-side*.108f,1.34f,z),new Vector3(.012f,.10f,.17f),_glass);
                Box("Meter coin slot",new Vector3(x-side*.112f,1.2f,z),new Vector3(.012f,.025f,.08f),_trim);
                if(i%3==0)
                {
                    Box("Street mailbox",new Vector3(side*10.3f,.85f,z+4),new Vector3(.62f,.98f,.60f),blue,true);
                    Box("Mailbox collection slot",new Vector3(side*10.3f-side*.32f,1.1f,z+4),new Vector3(.012f,.08f,.40f),_metal);
                    foreach(var dz in new[]{-.22f,.22f})Box("Mailbox leg",new Vector3(side*10.3f,.28f,z+4+dz),new Vector3(.45f,.28f,.045f),_metal);
                }
                foreach(var curbSide in new[]{-1f,1f})
                for(var joint=0;joint<8;joint++)
                    Box("Curb expansion joint",new Vector3(curbSide*9.22f,.149f,z-9+joint*2.2f),new Vector3(.40f,.004f,.018f),_metal);
            }
            var repaired=RealismMaterialLibrary.WorldSurface("M_Asphalt_Repair","asphalt_02",new Color(.49f,.50f,.51f),.31f,.75f,.34f,.75f);
            for(var i=0;i<12;i++)
            {
                var z=-303+i*79f;if(NearCrossStreet(z,12))continue;
                Decal("Repaired asphalt patch",new Vector3(i%2==0 ? 4.5f : -4.5f,.021f,z),2.15f,3.7f,repaired,i%3*3);
            }
        }

        private static void FacadeDetails(int index,int side,float centerX,float facade,float z,float width,float depth,float height)
        {
            // Details stay on the building envelope; driving and pedestrian
            // clearances continue to use the existing collision geometry.
            foreach(var end in new[]{-1f,1f})
            {
                Box("Masonry corner pier",new Vector3(facade-side*.07f,height*.5f,z+end*(depth*.5f-.3f)),new Vector3(.36f,height,.34f),_trim);
                for(var y=4.9f;y<height-1;y+=2.8f)
                for(var column=-1;column<=1;column++)
                {
                    if(column==0&&height<14)continue;
                    var point=new Vector3(centerX+column*2.7f,y,z+end*(depth*.5f+.04f));
                    Box("Side street window surround",point,new Vector3(1.4f,1.82f,.18f),_trim);
                    Box("Side street glazing",point+Vector3.forward*end*.075f,new Vector3(1.12f,1.52f,.08f),(column+index)%4==0 ? _warmGlass : _glass);
                    Box("Side street window sill",point+new Vector3(0,-.88f,end*.13f),new Vector3(1.52f,.10f,.32f),_trim);
                }
            }
            if(index%3==0)
            {
                var unit=new Vector3(facade-side*.42f,5.65f,z-3.8f);
                Box("Facade air conditioner",unit,new Vector3(.65f,.5f,1.1f),_stone);
                for(var slat=0;slat<7;slat++)Box("Condenser vent",unit+new Vector3(-side*.34f,-.18f+slat*.055f,0),new Vector3(.025f,.018f,.85f),_metal);
            }
            if(index%5==0&&height>9)
            {
                var y=7.15f;var x=facade-side*.51f;
                Box("Apartment balcony slab",new Vector3(x,y,z+2.6f),new Vector3(1.0f,.13f,2.5f),_trim);
                Box("Balcony top rail",new Vector3(x-side*.45f,y+.95f,z+2.6f),new Vector3(.055f,.06f,2.5f),_metal);
                for(var rail=0;rail<9;rail++)Box("Balcony baluster",new Vector3(x-side*.45f,y+.49f,z+1.45f+rail*.29f),new Vector3(.035f,.91f,.035f),_metal);
            }
        }

        private static void StreetSurfaceDetails()
        {
            var oil=RealismMaterialLibrary.Standard("M_Street_Wear",new Color(.50f,.49f,.46f));oil.shader=Shader.Find("CarCrash/StreetWear");oil.SetFloat("_Pattern",0);
            var skid=RealismMaterialLibrary.Standard("M_Street_Tyre_Marks",new Color(.59f,.58f,.56f));skid.shader=Shader.Find("CarCrash/StreetWear");skid.SetFloat("_Pattern",1);
            var iron=RealismMaterialLibrary.Standard("M_Street_Cast_Iron",new Color(.12f,.13f,.135f),.7f,.35f);
            var grooves=RealismMaterialLibrary.Standard("M_Drain_Recess",new Color(.035f,.04f,.045f),.1f,.25f);
            for(var i=0;i<24;i++)
            {
                var z=-350+i*40f;if(NearCrossStreet(z,15))continue;
                Decal("Asphalt oil and wear",new Vector3((i%2==0 ? 2.2f : -5.8f),.027f,z),1.6f,3.1f,oil,i*37);
                if(i%3==0)Decal("Faded braking marks",new Vector3(-2.3f,.028f,z+7),1.8f,5.2f,skid,i%2*180);
                if(i%2==0)
                {
                    var point=new Vector3(1.1f,.018f,z+11);
                    Cylinder("Cast iron manhole",point,.39f,.018f,iron);
                    for(var slot=0;slot<7;slot++)Box("Manhole grip",point+new Vector3(-.25f+slot*.083f,.013f,0),new Vector3(.018f,.006f,.43f),grooves);
                }
                foreach(var side in new[]{-1f,1f})
                {
                    var point=new Vector3(side*8.72f,.026f,z-5);
                    Box("Storm drain surround",point,new Vector3(.48f,.012f,.82f),iron);
                    for(var slot=0;slot<7;slot++)Box("Drain opening",point+new Vector3(0,.008f,-.31f+slot*.103f),new Vector3(.35f,.005f,.04f),grooves);
                }
            }
        }

        private static void Decal(string name,Vector3 point,float width,float length,Material material,float angle)
        {
            if(!Meshes.TryGetValue("StreetDetailQuad",out var mesh))
            {
                mesh=new Mesh{name="StreetDetailQuad",vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)},
                    uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up},triangles=new[]{0,2,1,0,3,2}};
                mesh.RecalculateNormals();mesh.RecalculateTangents();mesh=SaveMesh("StreetDetailQuad",mesh);Meshes["StreetDetailQuad"]=mesh;
            }
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(_city,false);
            obj.transform.SetPositionAndRotation(point,Quaternion.Euler(0,angle,0));obj.transform.localScale=new Vector3(width,1,length);
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;
        }

        private static void ExplorationPlaces()
        {
            var grass=RealismMaterialLibrary.WorldSurface("M_Park_Grass","leafy_grass",new Color(.76f,.82f,.67f),.5f,.8f,.18f,.5f);
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
            width=Mathf.Min(width,6.8f);center.y=1.2f;
            Box("Landmark stone plinth",new Vector3(center.x,.16f,center.z),new Vector3(width+.3f,.32f,.55f),_trim,true);
            Box("Landmark sign board",center,new Vector3(width,.92f,.18f),_metal);
            foreach(var side in new[]{-1f,1f})Tube("Landmark sign post",new Vector3(center.x+side*(width*.5f-.4f),0,center.z),
                new Vector3(center.x+side*(width*.5f-.4f),center.y,center.z),.065f,_metal,true);
            Sign(text,center+Vector3.back*.11f,Quaternion.identity,.13f,new Color(.94f,.93f,.86f),width-.6f,.45f);
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
            var key="CurvedPalmTrunk_"+variant;
            Vector3 Center(float t)=>new(Mathf.Sin(t*1.4f+variant)*t*.45f,t*height,.08f*t);
            if(!Meshes.TryGetValue(key,out var trunkMesh))
            {
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                const int rings=10,sides=8;
                for(var ring=0;ring<=rings;ring++)
                {
                    var t=ring/(float)rings;var center=Center(t);var radius=Mathf.Lerp(.17f,.105f,t);
                    for(var side=0;side<=sides;side++)
                    {
                        var angle=side*Mathf.PI*2/sides;var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                        vertices.Add(center+radial*radius);normals.Add(radial);uv.Add(new Vector2(side/(float)sides,t*height));
                        if(ring==0||side==0)continue;var a=(ring-1)*(sides+1)+side-1;
                        triangles.AddRange(new[]{a,a+sides+2,a+1,a,a+sides+1,a+sides+2});
                    }
                }
                trunkMesh=new Mesh{name=key};trunkMesh.SetVertices(vertices);trunkMesh.SetNormals(normals);trunkMesh.SetUVs(0,uv);trunkMesh.SetTriangles(triangles,0);trunkMesh.RecalculateTangents();
                trunkMesh=SaveMesh(key,trunkMesh);Meshes[key]=trunkMesh;
            }
            var trunk=new GameObject("Curved palm trunk",typeof(MeshFilter),typeof(MeshRenderer));trunk.transform.SetParent(_city,false);trunk.transform.position=position;
            trunk.GetComponent<MeshFilter>().sharedMesh=trunkMesh;trunk.GetComponent<MeshRenderer>().sharedMaterial=_palmBark;
            var last=position+Center(1);
            // One tapered-trunk approximation avoids seams between eight capsules.
            SolidCapsule("Palm trunk collision", position, last, 0.19f);
            var leafMesh = PalmLeaf();
            for (var i = 0; i < 21; i++)
            {
                var obj = new GameObject("Palm frond", typeof(MeshFilter), typeof(MeshRenderer));
                obj.transform.SetParent(_city, false);
                obj.transform.position = last;
                obj.transform.rotation = Quaternion.Euler(i % 4 * 11 - 14, i * 137.5f + variant * 20, 0);
                obj.transform.localScale = Vector3.one * (0.78f + i % 4 * 0.09f);
                obj.GetComponent<MeshFilter>().sharedMesh = leafMesh;
                obj.GetComponent<MeshRenderer>().sharedMaterial = _leaf;
            }
        }

        private static Mesh PalmLeaf()
        {
            if (Meshes.TryGetValue("PalmFrond", out var mesh)) return mesh;
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();var colors=new List<Color>();
            Vector3 Spine(float t)=>new(0,Mathf.Sin(t*Mathf.PI)*.82f-t*t*1.38f,t*3.55f);
            void LeafVertex(Vector3 p,Vector2 tex,Color c){vertices.Add(p);uv.Add(tex);colors.Add(c);}
            for (var i = 0; i <= 25; i++)
            {
                var t=i/25f;var center=Spine(t);var width=Mathf.Lerp(.025f,.004f,t);
                LeafVertex(center+Vector3.left*width,new Vector2(0,t),new Color(.9f,1,.65f));
                LeafVertex(center+Vector3.right*width,new Vector2(1,t),new Color(.9f,1,.65f));
                if (i == 0) continue;
                var a=(i-1)*2;triangles.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});
            }
            for(var i=2;i<25;i++)
            {
                var t=i/25f;var center=Spine(t);
                foreach(var side in new[]{-1f,1f})
                {
                    var length=Mathf.Pow(Mathf.Sin(t*Mathf.PI),.62f)*.8f;
                    var tip=center+new Vector3(side*length,-length*.28f,.28f+length*.17f);
                    var middle=Vector3.Lerp(center,tip,.43f)+Vector3.up*.035f;
                    var a=vertices.Count;var tint=Color.Lerp(new Color(.58f,.80f,.48f),new Color(1.02f,1.05f,.76f),(i%5)/4f);
                    LeafVertex(center,new Vector2(.5f,t),tint);
                    LeafVertex(middle+Vector3.back*.045f,new Vector2(0,t),tint);
                    LeafVertex(tip,new Vector2(.5f,t+.04f),tint);
                    LeafVertex(middle+Vector3.forward*.045f,new Vector2(1,t),tint);
                    triangles.AddRange(new[]{a,a+1,a+2,a,a+2,a+3});
                }
            }
            mesh = new Mesh { name = "PalmFrond" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv);mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();mesh.RecalculateTangents();
            mesh=SaveMesh("PalmFrondRealistic",mesh);
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
            const string key="StreetCylinder8";
            if(!Meshes.TryGetValue(key,out var mesh))
            {
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();const int sides=8;
                for(var ring=0;ring<2;ring++)for(var side=0;side<=sides;side++)
                {
                    var angle=side*Mathf.PI*2/sides;var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    vertices.Add(radial*.5f+Vector3.up*(ring*2-1));normals.Add(radial);uv.Add(new Vector2(side/(float)sides,ring));
                    if(ring==0||side==0)continue;var a=side-1;triangles.AddRange(new[]{a,a+sides+2,a+1,a,a+sides+1,a+sides+2});
                }
                foreach(var end in new[]{-1f,1f})
                {
                    var center=vertices.Count;vertices.Add(Vector3.up*end);normals.Add(Vector3.up*end);uv.Add(new Vector2(.5f,.5f));
                    for(var side=0;side<=sides;side++)
                    {
                        var angle=side*Mathf.PI*2/sides;var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                        vertices.Add(radial*.5f+Vector3.up*end);normals.Add(Vector3.up*end);uv.Add(new Vector2(radial.x,radial.z)+Vector2.one*.5f);
                        if(side>0){if(end>0)triangles.AddRange(new[]{center,center+side+1,center+side});else triangles.AddRange(new[]{center,center+side,center+side+1});}
                    }
                }
                mesh=new Mesh{name=key};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateTangents();mesh=SaveMesh(key,mesh);Meshes[key]=mesh;
            }
            var obj = new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.GetComponent<MeshFilter>().sharedMesh=mesh;
            obj.name = name; obj.transform.SetParent(_city, false);
            obj.transform.position = (from + to) * 0.5f;
            obj.transform.rotation = Quaternion.FromToRotation(Vector3.up, to - from);
            obj.transform.localScale = new Vector3(radius * 2, Vector3.Distance(from, to) * 0.5f, radius * 2);
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
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

        private static void Sign(string text, Vector3 position, Quaternion rotation, float size, Color color,float maxWidth=0,float maxHeight=0)
        {
            var obj = new GameObject(text, typeof(TextMesh));
            obj.transform.SetParent(_city, false); obj.transform.SetPositionAndRotation(position, rotation);
            var sign = obj.GetComponent<TextMesh>();
            sign.font = _signFont;
            sign.text = text; sign.fontSize = 64; sign.fontStyle = FontStyle.Normal;
            sign.characterSize = size*.5f; sign.color = color; sign.anchor = TextAnchor.MiddleCenter;
            sign.alignment = TextAlignment.Center;
            obj.GetComponent<MeshRenderer>().sharedMaterial = _signMaterial;
            var bounds=obj.GetComponent<MeshRenderer>().bounds;
            var width=Mathf.Max(bounds.size.x,bounds.size.z);
            var fit=1f;
            if(maxWidth>0&&width>.001f)fit=Mathf.Min(fit,maxWidth/width);
            if(maxHeight>0&&bounds.size.y>.001f)fit=Mathf.Min(fit,maxHeight/bounds.size.y);
            obj.transform.localScale=Vector3.one*fit;
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
                if(material==_leaf)continue;
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

        private static void CreateFoliageInstances()
        {
            var groups=new Dictionary<(int,int),List<CityFoliageRenderer.Frond>>();var originals=new List<GameObject>();
            foreach(var filter in _city.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||renderer.sharedMaterial!=_leaf)continue;
                var t=filter.transform;var key=(Mathf.FloorToInt(t.position.x/80),Mathf.FloorToInt(t.position.z/80));
                if(!groups.TryGetValue(key,out var list)){list=new List<CityFoliageRenderer.Frond>();groups[key]=list;}
                list.Add(new CityFoliageRenderer.Frond{position=t.position,rotation=t.rotation,scale=t.lossyScale.x});originals.Add(filter.gameObject);
            }
            var batches=new List<CityFoliageRenderer.Batch>();
            foreach(var group in groups)
            for(var first=0;first<group.Value.Count;first+=500)
            {
                var count=Mathf.Min(500,group.Value.Count-first);var fronds=group.Value.GetRange(first,count).ToArray();
                var bounds=new Bounds(fronds[0].position,Vector3.one*9);
                foreach(var frond in fronds)bounds.Encapsulate(new Bounds(frond.position,Vector3.one*9));
                batches.Add(new CityFoliageRenderer.Batch{bounds=bounds,fronds=fronds});
            }
            var foliage=_city.gameObject.AddComponent<CityFoliageRenderer>();foliage.mesh=Meshes["PalmFrond"];
            foliage.material=_leaf;foliage.batches=batches.ToArray();
            foreach(var obj in originals)Object.DestroyImmediate(obj);
            // Explicitly drawn foliage needs instancing variants. Keep that
            // variant family while allowing unused lighting/fog variants to be
            // stripped normally; Always Included would retain tens of thousands.
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=settings.FindProperty("m_AlwaysIncludedShaders");
            for(var i=shaders.arraySize-1;i>=0;i--)
                if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==_leaf.shader)
                {shaders.GetArrayElementAtIndex(i).objectReferenceValue=null;shaders.DeleteArrayElementAtIndex(i);}
            settings.FindProperty("m_InstancingStripping").intValue=2;settings.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"Instanced {foliage.InstanceCount} palm fronds in {batches.Count} spatial groups.");
        }
    }
}
