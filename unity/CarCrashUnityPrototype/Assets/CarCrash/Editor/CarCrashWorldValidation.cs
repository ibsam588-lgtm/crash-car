using System;
using System.Collections.Generic;
using System.IO;
using CarCrash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CarCrashEditor
{
    [InitializeOnLoad]
    public static class CarCrashWorldValidation
    {
        private const string Key="CarCrash.WorldQa";
        private static CarCrashHudInput _session;
        private static int _stage,_index,_oldExplored;
        private static bool _hadExplored,_captured;
        private static double _next,_deadline;
        private static Vector3 _start,_parked;
        private static float _pausedTime;
        private static GameObject _fixture;
        private static MovingTrafficObstacle _nudgeTraffic;
        private static readonly List<string> Checks=new(),Failures=new();
        [Serializable] private class Report { public bool passed;public int avenues,crossStreets,places;public float areaSquareMetres;public string[] checks,failures; }
        static CarCrashWorldValidation(){if(SessionState.GetBool(Key,false))Attach();}
        [MenuItem("Car Crash/Validate Open World and Unstuck")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/CarCrash/Scenes/ImpactRunPrototype.unity");
            SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;
        }
        private static void Attach()
        {
            _stage=0;_index=0;_next=0;_captured=false;_deadline=EditorApplication.timeSinceStartup+240;
            Checks.Clear();Failures.Clear();EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        private static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>_deadline)throw new TimeoutException("Open world QA stage "+_stage);
                if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<_next)return;
                switch(_stage)
                {
                    case 0:
                        _session=Object.FindFirstObjectByType<CarCrashHudInput>();if(_session==null||_session.Hud==null)return;
                        _hadExplored=PlayerPrefs.HasKey("CarCrash.ExploredPlaces");_oldExplored=PlayerPrefs.GetInt("CarCrash.ExploredPlaces");_captured=true;
                        foreach(var traffic in Object.FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))traffic.gameObject.SetActive(false);
                        foreach(var person in Object.FindObjectsByType<CityPedestrian>(FindObjectsSortMode.None))person.gameObject.SetActive(false);
                        _session.Garage.Select(0);_session.SendMessage("OnApplicationFocus",true);_session.StartFreeDrive();_stage=1;break;
                    case 1:
                        if(_session.Phase!=CarCrashHudInput.RunPhase.Driving)return;
                        CheckWorld();_session.ToggleMap();_parked=_session.vehicle.transform.position;_pausedTime=_session.ElapsedSeconds;
                        _next=EditorApplication.timeSinceStartup+.8;_stage=2;break;
                    case 2:
                        Check(_session.MapVisible&&_session.Phase==CarCrashHudInput.RunPhase.Paused&&Time.timeScale==0,"The city map safely pauses driving");
                        Check(Vector3.Distance(_parked,_session.vehicle.transform.position)<.001f&&Mathf.Abs(_pausedTime-_session.ElapsedSeconds)<.001f,"Browsing the map freezes the car and session clock");
                        _session.CloseMap();
                        for(var i=0;i<CityRoadNetwork.Places.Length;i++)
                        {
                            _session.ToggleMap();var button=GameObject.Find("Explore destination "+i)?.GetComponent<Button>();
                            Check(button!=null,"A working map destination button exists: "+CityRoadNetwork.Places[i].name);
                            button.onClick.Invoke();
                            Check(!_session.MapVisible&&_session.Phase==CarCrashHudInput.RunPhase.Driving&&_session.Navigation.Destination==i&&_session.Navigation.Route.Count>0,
                                "Selecting the destination creates a real road route and resumes: "+CityRoadNetwork.Places[i].name);
                        }
                        _session.Navigation.ClearRoute();_index=0;_stage=3;break;
                    case 3:
                        if(_index==CityRoadNetwork.Places.Length){_index=0;_stage=10;break;}
                        _start=CityRoadNetwork.Places[_index].entrance+Vector3.forward*5;
                        _session.vehicle.RecoverAt(_start,Quaternion.identity);_next=EditorApplication.timeSinceStartup+.6;_stage=4;break;
                    case 4:
                        Hold("Accelerator pedal");_next=EditorApplication.timeSinceStartup+1.8;_stage=5;break;
                    case 5:
                        var travel=_session.vehicle.transform.position.z-_start.z;
                        Check(travel>8f,"The real car can enter and drive through "+CityRoadNetwork.Places[_index].name+"; travel="+travel.ToString("F2"));
                        Check(!_session.RecoverySuggested,"Exploring "+CityRoadNetwork.Places[_index].name+" does not trigger obsolete world bounds");
                        _session.Hud.ReleaseControls();CapturePlace(_index);_index++;_stage=3;break;
                    case 10:
                        _start=new Vector3((_index==0 ? -270 : 270)+2.3f,.2f,410);
                        _session.vehicle.RecoverAt(_start,Quaternion.identity);_next=EditorApplication.timeSinceStartup+.5;_stage=11;break;
                    case 11:
                        Hold("Accelerator pedal");_next=EditorApplication.timeSinceStartup+1.5;_stage=12;break;
                    case 12:
                        Check(_session.vehicle.transform.position.z-_start.z>8&&Mathf.Abs(_session.vehicle.transform.position.x-_start.x)<.5f&&_session.Phase==CarCrashHudInput.RunPhase.Driving,
                            "Driving stays continuous beyond the old map on the "+(_index==0 ? "west" : "east")+" outer avenue");
                        Check(!_session.RecoverySuggested,"The expanded outer avenue is inside the real recovery bounds");
                        _session.Hud.ReleaseControls();_index++;if(_index<2)_stage=10;else{_index=0;_stage=20;}break;
                    case 20:
                        SetupNudge(_index==0);_next=EditorApplication.timeSinceStartup+.6;_stage=21;break;
                    case 21:
                        Hold(_index==0 ? "Accelerator pedal" : "Reverse pedal");_next=EditorApplication.timeSinceStartup+2.6;_stage=22;break;
                    case 22:
                        var signedTravel=(_session.vehicle.transform.position.z-_start.z)*(_index==0 ? 1 : -1);
                        Check(_nudgeTraffic.IsReleased&&signedTravel>2.5f,"Gentle "+(_index==0 ? "forward" : "reverse")+" pressure makes stopped traffic movable instead of trapping the player; travel="+signedTravel.ToString("F2")+
                            " released="+_nudgeTraffic.IsReleased+" traffic="+_fixture.transform.position+" throttle="+_session.vehicle.CurrentThrottle+" brake="+_session.vehicle.CurrentBrake);
                        Check(_session.scoring.Hits==0,"A gentle traffic nudge does not award a fake crash score");
                        _session.Hud.ReleaseControls();Object.Destroy(_fixture);_index++;_next=EditorApplication.timeSinceStartup+.1;
                        if(_index<2)_stage=20;else{_index=0;_stage=30;}break;
                    case 30:
                        for(var i=0;i<6;i++)
                        {
                            _session.Garage.Select(i);_session.vehicle.RecoverAt(new Vector3(108,.2f,470),Quaternion.Euler(0,0,90));
                            var elapsed=_session.ElapsedSeconds;var score=_session.scoring.Score;_session.RecoverVehicle();
                            var chassis=_session.vehicle.GetComponent<BoxCollider>();var body=_session.vehicle.GetComponent<Rigidbody>();
                            var free=true;
                            foreach(var hit in Physics.OverlapBox(body.position+body.rotation*chassis.center,chassis.size*.5f,body.rotation,~0,QueryTriggerInteraction.Ignore))
                                if(hit.attachedRigidbody!=body&&hit.bounds.max.y>.48f)free=false;
                            Check(free&&Vector3.Dot(_session.vehicle.transform.up,Vector3.up)>.99f&&CityRoadNetwork.InsideWorld(body.position,10),
                                "Recovery places the full "+_session.Garage.Current.id+" chassis upright in clear space");
                            Check(_session.ElapsedSeconds==elapsed&&_session.scoring.Score==score,"Recovery preserves progress for "+_session.Garage.Current.id);
                        }
                        _session.Garage.Select(0);_session.vehicle.RecoverAt(new Vector3(2.3f,.2f,-20),Quaternion.identity);
                        _fixture=GameObject.CreatePrimitive(PrimitiveType.Cube);_fixture.name="QA blocked-car wall";
                        _fixture.transform.position=new Vector3(2.3f,1.5f,-16);_fixture.transform.localScale=new Vector3(8,3,.5f);
                        _next=EditorApplication.timeSinceStartup+.6;_stage=31;break;
                    case 31:
                        Hold("Accelerator pedal");_next=EditorApplication.timeSinceStartup+4.8;_stage=32;break;
                    case 32:
                        Check(_session.RecoverySuggested,"Sustained throttle against an obstacle produces an unstuck hint");
                        var beforeTime=_session.ElapsedSeconds;_session.RecoverVehicle();
                        Check(!_session.RecoverySuggested&&_session.ElapsedSeconds==beforeTime,"Manual unstuck clears the warning without resetting the drive");
                        Object.Destroy(_fixture);Complete();break;
                }
            }
            catch(Exception error){Failures.Add(error.ToString());Complete();}
        }
        private static void CheckWorld()
        {
            Check(CityRoadNetwork.AvenueX.Length==7&&CityRoadNetwork.CrossStreetZ.Length==13,"Seven avenues and thirteen cross streets form a connected world");
            Check((CityRoadNetwork.MaxX-CityRoadNetwork.MinX)*(CityRoadNetwork.MaxZ-CityRoadNetwork.MinZ)>650000,"The explorable district exceeds 650,000 square metres");
            foreach(var x in CityRoadNetwork.AvenueX)
            {
                var clear=true;string obstacle="";
                for(var z=CityRoadNetwork.MinZ+15;z<CityRoadNetwork.MaxZ-15;z+=8)
                    if(!Clear(new Vector3(x,.9f,z),new Vector3(.95f,.35f,1.5f),out obstacle)){clear=false;break;}
                Check(clear,"The full avenue driving corridor is unobstructed at x="+x+" "+obstacle);
            }
            foreach(var z in CityRoadNetwork.CrossStreetZ)
            {
                var clear=true;string obstacle="";
                for(var x=CityRoadNetwork.MinX+15;x<CityRoadNetwork.MaxX-15;x+=8)
                    if(!Clear(new Vector3(x,.9f,z),new Vector3(1.5f,.35f,.95f),out obstacle)){clear=false;break;}
                Check(clear,"The full cross street is unobstructed at z="+z+" "+obstacle);
            }
            Check(GameObject.Find("Street end safety barrier")==null&&GameObject.Find("District edge safety barrier")==null,"The old road-end barriers no longer block exploration");
            var coastClear=true;string coastObstacle="";
            for(var z=-375f;z<610;z+=5)if(!Clear(new Vector3(296,.9f,z),new Vector3(.95f,.35f,1.5f),out coastObstacle)){coastClear=false;break;}
            Check(coastClear,"The coastal drive remains unobstructed through every cross-street junction: "+coastObstacle);
            for(var i=0;i<CityRoadNetwork.Places.Length;i++)
            {
                var place=CityRoadNetwork.Places[i];
                var route=CityRoadNetwork.RouteTo(_session.vehicle.transform.position,i);var aligned=true;
                for(var j=1;j<route.Count;j++)aligned&=Mathf.Abs(route[j].x-route[j-1].x)<.01f||Mathf.Abs(route[j].z-route[j-1].z)<.01f;
                Check(route.Count>2&&aligned,"Navigation follows real connected streets to "+place.name);
                Check(Physics.Raycast(place.position+Vector3.up*20,Vector3.down,out var floor,30)&&floor.point.y<.5f&&
                    Clear(place.position+Vector3.up*.75f,new Vector3(1,.3f,1.5f),out _),"The destination has accessible ground and arrival space: "+place.name);
            }
        }
        private static bool Clear(Vector3 position,Vector3 half,out string obstacle)
        {
            foreach(var hit in Physics.OverlapBox(position,half,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                if(hit.attachedRigidbody!=_session.vehicle.GetComponent<Rigidbody>()&&hit.bounds.max.y>.45f){obstacle=hit.name+" at "+hit.bounds.center;return false;}
            obstacle="";return true;
        }
        private static void SetupNudge(bool forward)
        {
            _session.Garage.Select(0);_start=new Vector3(2.3f,.2f,-20);_session.vehicle.RecoverAt(_start,Quaternion.identity);
            _fixture=new GameObject("QA stopped traffic");_fixture.transform.position=_start+Vector3.forward*(forward ? 5.5f : -5.5f);
            var shape=_fixture.AddComponent<BoxCollider>();shape.center=new Vector3(0,.65f,0);shape.size=new Vector3(2.2f,1.3f,4.65f);
            var body=_fixture.AddComponent<Rigidbody>();body.mass=1500;body.isKinematic=true;
            _nudgeTraffic=_fixture.AddComponent<MovingTrafficObstacle>();_nudgeTraffic.velocity=Vector3.zero;_nudgeTraffic.enabled=false;
            _fixture.AddComponent<TrafficImpactTarget>();
        }
        private static void CapturePlace(int index)
        {
            var place=CityRoadNetwork.Places[index];var camera=_session.CameraRig.GetComponent<Camera>();
            var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;var oldTarget=camera.targetTexture;var oldAspect=camera.aspect;
            var focus=place.position+Vector3.up*1.5f;
            var position=focus+new Vector3(28,15,-30);
            if(index==1){position=place.position+new Vector3(-10,8,-30);focus=place.position+new Vector3(32,1.2f,15);}
            if(index==2){position=place.position+new Vector3(-12,7,-26);focus=place.position+new Vector3(25,1,12);}
            if(index==3)focus=new Vector3(108,1,464);
            if(index==4)focus=new Vector3(-216,2,239);
            if(index==5){position=new Vector3(-176,30,510);focus=new Vector3(-216,0,556);}
            camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(focus-position));
            var target=new RenderTexture(1280,720,24);target.Create();camera.targetTexture=target;camera.aspect=1280f/720;camera.Render();
            var oldActive=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            Directory.CreateDirectory("../../build/open-world");File.WriteAllBytes("../../build/open-world/"+place.name.ToLowerInvariant().Replace(' ','-')+".png",image.EncodeToPNG());
            camera.targetTexture=oldTarget;camera.aspect=oldAspect;camera.transform.SetPositionAndRotation(oldPosition,oldRotation);
            RenderTexture.active=oldActive;target.Release();Object.Destroy(target);Object.Destroy(image);
        }
        private static void Hold(string name)=>ExecuteEvents.Execute(GameObject.Find(name),new PointerEventData(EventSystem.current){pointerId=90},ExecuteEvents.pointerDownHandler);
        private static void Check(bool value,string description){(value?Checks:Failures).Add(description);Debug.Log("[World QA] "+(value?"PASS ":"FAIL ")+description);}
        private static void Complete()
        {
            EditorApplication.update-=Tick;SessionState.SetBool(Key,false);
            if(_captured){if(_hadExplored)PlayerPrefs.SetInt("CarCrash.ExploredPlaces",_oldExplored);else PlayerPrefs.DeleteKey("CarCrash.ExploredPlaces");PlayerPrefs.Save();}
            Directory.CreateDirectory("Builds/Validation");
            var report=new Report{passed=Failures.Count==0,avenues=CityRoadNetwork.AvenueX.Length,crossStreets=CityRoadNetwork.CrossStreetZ.Length,places=CityRoadNetwork.Places.Length,
                areaSquareMetres=(CityRoadNetwork.MaxX-CityRoadNetwork.MinX)*(CityRoadNetwork.MaxZ-CityRoadNetwork.MinZ),checks=Checks.ToArray(),failures=Failures.ToArray()};
            File.WriteAllText("Builds/Validation/world-qa.json",JsonUtility.ToJson(report,true));
            Debug.Log("WORLD_QA_"+(report.passed?"PASS":"FAIL")+" checks="+Checks.Count+" failures="+Failures.Count);
            if(Application.isBatchMode)EditorApplication.Exit(report.passed?0:1);else EditorApplication.isPlaying=false;
        }
    }
}
