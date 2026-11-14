using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CarCrash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Debug=UnityEngine.Debug;
using Object=UnityEngine.Object;

namespace CarCrashEditor
{
    [InitializeOnLoad]
    public static class CarCrashGraphicsValidation
    {
        private const string Key="CarCrash.GraphicsQa";
        private static CarCrashHudInput _session;
        private static int _stage,_index;
        private static double _next,_deadline;
        private static string _runtimeError;
        private static readonly List<string> Checks=new(),Failures=new();
        private static readonly List<Frame> Frames=new();
        private static readonly Dictionary<string,int> Prefs=new();
        private static readonly HashSet<string> HadPrefs=new();
        [Serializable] private class Frame {public string name;public float mean,range,blackFraction,magentaFraction;public long renderMilliseconds;}
        [Serializable] private class Report {public bool passed;public string device;public int foliageInstances,staticVertices,trafficModels;public string[] checks,failures;public Frame[] frames;}
        static CarCrashGraphicsValidation(){if(SessionState.GetBool(Key,false))Attach();}
        [MenuItem("Car Crash/Validate Realistic Graphics")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/CarCrash/Scenes/ImpactRunPrototype.unity");
            SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;
        }
        public static void BuildAndValidate(){CarCrashPrototypeSceneBuilder.BuildScene();Validate();}
        private static void Attach()
        {
            _stage=_index=0;_next=0;_runtimeError=null;_deadline=EditorApplication.timeSinceStartup+240;
            Checks.Clear();Failures.Clear();Frames.Clear();Prefs.Clear();HadPrefs.Clear();
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;
        }
        private static void OnLog(string text,string stack,LogType type){if(type is LogType.Error or LogType.Exception or LogType.Assert)_runtimeError=text;}
        private static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>_deadline)throw new TimeoutException("Graphics QA stage "+_stage);
                if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<_next)return;
                if(_runtimeError!=null)throw new Exception(_runtimeError);
                if(_stage==0)
                {
                    _session=Object.FindFirstObjectByType<CarCrashHudInput>();if(_session==null||_session.Hud==null)return;
                    foreach(var key in new[]{"CarCrash.Paint","CarCrash.Vehicle","CarCrash.BestImpactScore","CarCrash.ExploredPlaces"})
                    {if(PlayerPrefs.HasKey(key))HadPrefs.Add(key);Prefs[key]=PlayerPrefs.GetInt(key);}
                    Check(QualitySettings.activeColorSpace==ColorSpace.Linear,"The scene uses linear light and physically based surfaces");
                    foreach(var shaderName in new[]{"CarCrash/AutomotivePaint","CarCrash/CitySurface","CarCrash/ArchitecturalGlass","CarCrash/PalmFoliage","CarCrash/PalmBark","CarCrash/StreetWear","Hidden/CarCrash/CinematicGrade"})
                    {
                        var shader=Shader.Find(shaderName);Check(shader!=null&&shader.isSupported,"Rendering shader is present and supported: "+shaderName);
                        if(shader!=null)foreach(var message in ShaderUtil.GetShaderMessages(shader))Check(message.severity.ToString()!="Error","Shader compilation: "+shaderName+" / "+message.message);
                    }
                    var reflection=RenderSettings.customReflectionTexture as Cubemap;
                    Check(reflection!=null&&reflection.width>=256&&reflection.format==TextureFormat.RGBA32,"Street reflections retain browser-safe byte cubemaps with detailed faces");
                    var foliage=Object.FindFirstObjectByType<CityFoliageRenderer>();
                    Check(foliage!=null&&foliage.InstanceCount>4000&&foliage.mesh.vertexCount<1000,"Detailed palms share a small mesh instead of duplicating millions of stored vertices");
                    var vertices=0;var city=Object.FindFirstObjectByType<CityRoadNetwork>();
                    foreach(var mesh in city.GetComponentsInChildren<MeshFilter>())if(mesh.sharedMesh!=null)vertices+=mesh.sharedMesh.vertexCount;
                    Check(vertices<1500000,"City geometry remains within the browser vertex budget ("+vertices+")");
                    var trafficModels=new HashSet<Mesh>();
                    foreach(var car in Object.FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))
                    {
                        foreach(var filter in car.GetComponentsInChildren<MeshFilter>()){if(filter.sharedMesh!=null){trafficModels.Add(filter.sharedMesh);break;}}
                        var box=car.GetComponent<BoxCollider>();var bounds=car.GetComponentInChildren<MeshRenderer>().bounds;
                        foreach(var renderer in car.GetComponentsInChildren<MeshRenderer>())bounds.Encapsulate(renderer.bounds);
                        Check(box!=null&&box.size.z>bounds.size.z*.75f&&box.size.z<bounds.size.z*1.15f,"Traffic collision dimensions follow the visible vehicle: "+car.name);
                    }
                    Check(trafficModels.Count>=3,"The road fleet contains sports cars, SUVs and pickups");
                    AuditTrafficCamera();
                    _session.OpenGarage();_session.SelectVehicle(0);_session.CameraRig.enabled=false;
                    _next=EditorApplication.timeSinceStartup+.5;_stage=1;return;
                }
                if(_stage==1)
                {
                    var garage=_session.Garage;var bounds=BoundsOf(garage.visualRoot);
                    var camera=_session.CameraRig.GetComponent<Camera>();camera.fieldOfView=42;
                    var radius=Mathf.Max(bounds.size.z*1.6f,bounds.size.y*3.0f);
                    var point=bounds.center+new Vector3(.63f,.30f,.78f)*radius;
                    var frame=Capture("vehicle-"+garage.Current.id,point,bounds.center);
                    Audit(frame);
                    if(_index==0)
                    {
                        var grade=camera.GetComponent<CinematicCameraGrade>();var strength=grade.contactShadowStrength;
                        grade.contactShadowStrength=0;var unoccluded=Capture("vehicle-roadbreaker-no-ao",point,bounds.center);grade.contactShadowStrength=strength;
                        Check(unoccluded.mean-frame.mean>.00002f&&unoccluded.mean-frame.mean<.18f,"Contact occlusion produces bounded shading in real rendered frames");
                    }
                    _index++;
                    if(_index<garage.vehicles.Length){_session.SelectVehicle(_index);_next=EditorApplication.timeSinceStartup+.35;return;}
                    _index=0;_stage=2;return;
                }
                if(_stage==2)
                {
                    var positions=new[]{new Vector3(2.3f,.2f,35),CityRoadNetwork.Places[1].position,CityRoadNetwork.Places[3].position};
                    _session.vehicle.transform.position=positions[_index];_session.vehicle.GetComponent<Rigidbody>().position=positions[_index];
                    Physics.SyncTransforms();Object.FindFirstObjectByType<CityVisibility>().RefreshNow();
                    _next=EditorApplication.timeSinceStartup+.4;_stage=3;return;
                }
                if(_stage==3)
                {
                    var names=new[]{"downtown","marina","park"};
                    var cameras=new[]{new Vector3(3.7f,2.1f,-15),new Vector3(270,5,492),new Vector3(90,5,411)};
                    var targets=new[]{new Vector3(3,2,55),new Vector3(304,2,555),new Vector3(109,2,463)};
                    Audit(Capture(names[_index],cameras[_index],targets[_index]));
                    if(_index==2)
                    {
                        var board=GameObject.Find("CityBatch_M_City_Powdercoat_0_2")?.GetComponent<MeshRenderer>();
                        Check(board!=null&&board.enabled,"Nearby landmark architecture remains visible after changing districts");
                    }
                    _index++;if(_index<3)_stage=2;else Complete();
                }
            }
            catch(Exception e){Failures.Add(e.ToString());Complete();}
        }
        private static Bounds BoundsOf(Transform root)
        {
            var renderers=root.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);return bounds;
        }
        private static void AuditTrafficCamera()
        {
            var rig=_session.CameraRig;var camera=rig.GetComponent<Camera>();
            var traffic=GameObject.Find("Traffic_Pearl_GT").GetComponent<MovingTrafficObstacle>();
            var renderers=traffic.GetComponentsInChildren<Renderer>();var states=new bool[renderers.Length];
            for(var i=0;i<states.Length;i++)states[i]=renderers[i].forceRenderingOff;
            var position=camera.transform.position;var rotation=camera.transform.rotation;var oldTarget=camera.targetTexture;
            var texture=new RenderTexture(64,64,24);texture.Create();
            try
            {
                camera.targetTexture=texture;
                camera.transform.SetPositionAndRotation(traffic.GetComponent<Collider>().bounds.center,Quaternion.identity);
                camera.Render();Check(rig.LastOccludedVehicleCount>0,"A real camera render cuts away traffic overlapping the lens");
                var restored=true;for(var i=0;i<states.Length;i++)restored&=renderers[i].forceRenderingOff==states[i];
                Check(restored,"Traffic rendering flags restore after the camera completes its frame");
                camera.transform.position=new Vector3(0,20,-60);camera.Render();
                Check(rig.LastOccludedVehicleCount==0,"Traffic stays visible when it is outside the camera clearance zone");
            }
            finally{camera.targetTexture=oldTarget;camera.transform.SetPositionAndRotation(position,rotation);texture.Release();Object.Destroy(texture);}
        }
        private static Frame Capture(string name,Vector3 position,Vector3 target)
        {
            var camera=_session.CameraRig.GetComponent<Camera>();var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;var oldAspect=camera.aspect;
            var image=new Texture2D(960,540,TextureFormat.RGB24,false);var texture=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32){antiAliasing=4};texture.Create();
            var clock=Stopwatch.StartNew();
            try
            {
                camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));camera.aspect=960f/540;camera.targetTexture=texture;
                camera.Render();RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();clock.Stop();
                Directory.CreateDirectory("../../build/cinematic-city");File.WriteAllBytes("../../build/cinematic-city/native-"+name+".png",image.EncodeToPNG());
                var pixels=image.GetPixels32();var min=1f;var max=0f;var sum=0f;var count=0;var black=0;var magenta=0;
                for(var i=0;i<pixels.Length;i+=4)
                {
                    var p=pixels[i];var l=(.2126f*p.r+.7152f*p.g+.0722f*p.b)/255;sum+=l;min=Mathf.Min(min,l);max=Mathf.Max(max,l);count++;
                    if(l<.012f)black++;if(p.r>200&&p.b>200&&p.g<50)magenta++;
                }
                var frame=new Frame{name=name,mean=sum/count,range=max-min,blackFraction=(float)black/count,magentaFraction=(float)magenta/count,renderMilliseconds=clock.ElapsedMilliseconds};Frames.Add(frame);return frame;
            }
            finally{camera.targetTexture=oldTarget;camera.aspect=oldAspect;RenderTexture.active=oldActive;texture.Release();Object.Destroy(texture);Object.Destroy(image);}
        }
        private static void Audit(Frame frame)
        {
            Check(frame.mean>.045f&&frame.mean<.90f&&frame.range>.20f&&frame.blackFraction<.6f,"Rendered frame has usable lighting and contrast: "+frame.name);
            Check(frame.magentaFraction<.001f,"Rendered frame has no missing-shader pixels: "+frame.name);
        }
        private static void Check(bool value,string message){(value?Checks:Failures).Add(message);Debug.Log("[Graphics QA] "+(value ? "PASS " : "FAIL ")+message);}
        private static void Complete()
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;SessionState.SetBool(Key,false);
            foreach(var pref in Prefs){if(HadPrefs.Contains(pref.Key))PlayerPrefs.SetInt(pref.Key,pref.Value);else PlayerPrefs.DeleteKey(pref.Key);}PlayerPrefs.Save();
            var foliage=Object.FindFirstObjectByType<CityFoliageRenderer>();var vertices=0;var city=Object.FindFirstObjectByType<CityRoadNetwork>();
            if(city!=null)foreach(var mesh in city.GetComponentsInChildren<MeshFilter>())if(mesh.sharedMesh!=null)vertices+=mesh.sharedMesh.vertexCount;
            var models=new HashSet<Mesh>();foreach(var car in Object.FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))foreach(var filter in car.GetComponentsInChildren<MeshFilter>()){if(filter.sharedMesh!=null){models.Add(filter.sharedMesh);break;}}
            var report=new Report{passed=Failures.Count==0,device=SystemInfo.graphicsDeviceName,foliageInstances=foliage!=null ? foliage.InstanceCount : 0,staticVertices=vertices,trafficModels=models.Count,checks=Checks.ToArray(),failures=Failures.ToArray(),frames=Frames.ToArray()};
            Directory.CreateDirectory("Builds/Validation");File.WriteAllText("Builds/Validation/graphics-qa.json",JsonUtility.ToJson(report,true));
            Debug.Log("GRAPHICS_QA_"+(report.passed ? "PASS" : "FAIL")+" checks="+Checks.Count+" failures="+Failures.Count);
            if(Application.isBatchMode)EditorApplication.Exit(report.passed ? 0 : 1);else EditorApplication.isPlaying=false;
        }
    }
}
