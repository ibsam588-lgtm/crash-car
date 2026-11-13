using System;
using System.Collections.Generic;
using System.IO;
using CarCrash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CarCrashEditor
{
    [InitializeOnLoad]
    public static class CarCrashFleetValidation
    {
        private const string ActiveKey="CarCrash.FleetQa.Active";
        private static CarCrashHudInput _session;
        private static int _stage,_index,_oldVehicle;
        private static bool _hadVehicle;
        private static double _next,_deadline,_reverseDeadline;
        private static Vector3 _start;
        private static float _heading;
        private static string _runtimeError;
        private static int _particlesAtPause;
        private static readonly List<string> Checks=new();
        private static readonly List<string> Failures=new();
        private static readonly List<DriveResult> Drives=new();
        private static DriveResult _drive;
        [Serializable] private class DriveResult { public string vehicle; public int grounded; public float forwardMetres,reverseMetres,reverseHeading; }
        [Serializable] private class Report { public bool passed; public string[] checks,failures; public DriveResult[] drives; }

        static CarCrashFleetValidation() { if(SessionState.GetBool(ActiveKey,false)) Attach(); }
        [MenuItem("Car Crash/Validate Garage Fleet and Crash Effects")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/CarCrash/Scenes/ImpactRunPrototype.unity");
            SessionState.SetBool(ActiveKey,true); Attach(); EditorApplication.isPlaying=true;
        }
        private static void Attach()
        {
            _stage=0; _index=0; _next=0; _deadline=EditorApplication.timeSinceStartup+180;
            Checks.Clear(); Failures.Clear(); Drives.Clear(); _runtimeError=null;
            EditorApplication.update-=Tick; EditorApplication.update+=Tick;
            Application.logMessageReceived-=OnLog; Application.logMessageReceived+=OnLog;
        }
        private static void OnLog(string message,string stack,LogType type)
        { if(type is LogType.Exception or LogType.Error or LogType.Assert) _runtimeError=message; }
        private static void Tick()
        {
            try
            {
                if(_runtimeError!=null) throw new Exception(_runtimeError);
                if(EditorApplication.timeSinceStartup>_deadline) throw new TimeoutException("Fleet QA stage "+_stage);
                if(!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup<_next)return;
                switch(_stage)
                {
                    case 0:
                        _session=Object.FindFirstObjectByType<CarCrashHudInput>();
                        if(_session==null || _session.Hud==null)return;
                        _hadVehicle=PlayerPrefs.HasKey("CarCrash.Vehicle"); _oldVehicle=PlayerPrefs.GetInt("CarCrash.Vehicle");
                        Check(_session.Garage!=null && _session.Garage.vehicles.Length==6,"The garage offers six complete playable vehicles");
                        _session.OpenGarage();
                        foreach(var spec in _session.Garage.vehicles)
                        {
                            var button=GameObject.Find("Select vehicle "+spec.id)?.GetComponent<Button>();
                            Check(button!=null,"A working garage card exists for "+spec.displayName);
                            button.onClick.Invoke();
                            Check(_session.Garage.Current==spec && Mathf.Abs(_session.vehicle.GetComponent<Rigidbody>().mass-spec.mass)<1f,
                                "The card selects the actual vehicle and its handling: "+spec.id);
                            var wheels=_session.vehicle.GetComponentsInChildren<WheelCollider>();
                            Check(wheels.Length==4 && _session.vehicle.frontLeftVisual!=null && _session.vehicle.rearRightVisual!=null,
                                "All four physical wheels bind to the selected model: "+spec.id);
                        }
                        _session.SelectVehicle(0);
                        foreach(var traffic in Object.FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))traffic.gameObject.SetActive(false);
                        foreach(var person in Object.FindObjectsByType<CityPedestrian>(FindObjectsSortMode.None))person.gameObject.SetActive(false);
                        foreach(var collider in Object.FindFirstObjectByType<CityRoadNetwork>().GetComponentsInChildren<Collider>())
                            if(collider.bounds.size.y>.3f)collider.enabled=false;
                        _session.SendMessage("OnApplicationFocus",true); _session.StartFreeDrive(); _stage=1; break;
                    case 1:
                        if(_session.Phase!=CarCrashHudInput.RunPhase.Driving)return;
                        _session.Hud.ReleaseControls(); _session.Garage.Select(_index);
                        _drive=new DriveResult { vehicle=_session.Garage.Current.id }; Drives.Add(_drive);
                        _next=EditorApplication.timeSinceStartup+1; _stage=2; break;
                    case 2:
                        foreach(var wheel in _session.vehicle.GetComponentsInChildren<WheelCollider>())if(wheel.isGrounded)_drive.grounded++;
                        Check(_drive.grounded==4,"All four wheels support the vehicle on the real road: "+_drive.vehicle+" grounded="+_drive.grounded);
                        _start=_session.vehicle.transform.position; Hold("Accelerator pedal",true,101);
                        _next=EditorApplication.timeSinceStartup+1.15; _stage=3; break;
                    case 3:
                        _drive.forwardMetres=_session.vehicle.transform.position.z-_start.z;
                        Check(_drive.forwardMetres>1f && _session.vehicle.ForwardSpeedKmh>5,"The selected vehicle accelerates forward: "+_drive.vehicle+
                            " travel="+_drive.forwardMetres+" speed="+_session.vehicle.ForwardSpeedKmh+" throttle="+_session.vehicle.CurrentThrottle+
                            " brake="+_session.vehicle.CurrentBrake+" phase="+_session.Phase+" start="+_start+" end="+_session.vehicle.transform.position+
                            " rotation="+_session.vehicle.transform.eulerAngles+" rpm="+_session.vehicle.rearLeftCollider.rpm+" steer="+_session.vehicle.frontLeftCollider.steerAngle);
                        _session.Hud.ReleaseControls(); Hold("Reverse pedal",true,102);
                        _next=EditorApplication.timeSinceStartup+.12; _stage=4; break;
                    case 4:
                        Check(_session.vehicle.CurrentBrake>0 && _session.vehicle.CurrentThrottle>=0,
                            "REV brakes forward motion before changing direction: "+_drive.vehicle);
                        _reverseDeadline=EditorApplication.timeSinceStartup+5; _stage=5; break;
                    case 5:
                        if(_session.vehicle.ForwardSpeedKmh>-3f)
                        {
                            if(EditorApplication.timeSinceStartup<_reverseDeadline)return;
                            Check(false,"REV reached backward motion within five seconds: "+_drive.vehicle);
                        }
                        _start=_session.vehicle.transform.position;
                        _next=EditorApplication.timeSinceStartup+.75; _stage=6; break;
                    case 6:
                        _drive.reverseMetres=_start.z-_session.vehicle.transform.position.z;
                        Check(_session.vehicle.Reversing && _drive.reverseMetres>.4f,"Holding REV really drives backward: "+_drive.vehicle+" travel="+_drive.reverseMetres);
                        _heading=_session.vehicle.transform.eulerAngles.y; Hold("Steer left",true,103);
                        _next=EditorApplication.timeSinceStartup+.75; _stage=7; break;
                    case 7:
                        _drive.reverseHeading=Mathf.DeltaAngle(_heading,_session.vehicle.transform.eulerAngles.y);
                        Check(_drive.reverseHeading>5f,"Reverse steering changes the real heading: "+_drive.vehicle+" heading="+_drive.reverseHeading);
                        _session.Hud.ReleaseControls(); _index++;
                        if(_index<_session.Garage.vehicles.Length){_stage=1;break;}
                        _session.Garage.Select(0); _next=EditorApplication.timeSinceStartup+.5; _stage=8; break;
                    case 8:
                        var vehicle=_session.vehicle;
                        vehicle.enabled=false;
                        foreach(var wheel in vehicle.GetComponentsInChildren<WheelCollider>())wheel.enabled=false;
                        var body=vehicle.GetComponent<Rigidbody>();
                        body.interpolation=RigidbodyInterpolation.None;
                        body.constraints=RigidbodyConstraints.FreezeRotation|RigidbodyConstraints.FreezePositionY;
                        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name="QA collision wall"; wall.transform.position=new Vector3(2.3f,1,-1);
                        wall.transform.localScale=new Vector3(10,2,.5f);
                        vehicle.RecoverAt(new Vector3(2.3f,.16f,-10),Quaternion.identity);
                        body.position=vehicle.transform.position; Physics.SyncTransforms(); body.linearVelocity=Vector3.forward*(80f/3.6f);
                        _next=EditorApplication.timeSinceStartup+.42; _stage=9; break;
                    case 9:
                        var effects=_session.vehicle.GetComponent<VehicleImpactEffects>();
                        Check(effects.TotalBursts>0 && effects.LiveParticles>50,"A real 80 km/h scenery crash emits a substantial sparks, dust and glass splash");
                        _session.PauseRun(); _particlesAtPause=effects.LiveParticles;
                        _next=EditorApplication.timeSinceStartup+.8; _stage=10; break;
                    case 10:
                        Check(_session.vehicle.GetComponent<VehicleImpactEffects>().LiveParticles==_particlesAtPause,"Pause freezes the crash particles with the game");
                        _session.ResumeRun(); _next=EditorApplication.timeSinceStartup+3; _stage=11; break;
                    case 11:
                        Check(_session.vehicle.GetComponent<VehicleImpactEffects>().LiveParticles==0,"Crash particles expire without accumulating across a session");
                        Complete();break;
                }
            }
            catch(Exception error){Failures.Add(error.ToString());Complete();}
        }
        private static void Hold(string name,bool down,int id)
        {
            var pointer=new PointerEventData(EventSystem.current){pointerId=id};
            if(down)ExecuteEvents.Execute(GameObject.Find(name),pointer,ExecuteEvents.pointerDownHandler);
            else ExecuteEvents.Execute(GameObject.Find(name),pointer,ExecuteEvents.pointerUpHandler);
        }
        private static void Check(bool condition,string message)
        { (condition?Checks:Failures).Add(message);Debug.Log("[Fleet QA] "+(condition?"PASS ":"FAIL ")+message); }
        private static void Complete()
        {
            EditorApplication.update-=Tick; Application.logMessageReceived-=OnLog; SessionState.SetBool(ActiveKey,false);
            if(_hadVehicle)PlayerPrefs.SetInt("CarCrash.Vehicle",_oldVehicle);else PlayerPrefs.DeleteKey("CarCrash.Vehicle");
            PlayerPrefs.Save(); Directory.CreateDirectory("Builds/Validation");
            var report=new Report{passed=Failures.Count==0,checks=Checks.ToArray(),failures=Failures.ToArray(),drives=Drives.ToArray()};
            File.WriteAllText("Builds/Validation/fleet-qa.json",JsonUtility.ToJson(report,true));
            Debug.Log("FLEET_QA_"+(report.passed?"PASS":"FAIL")+" checks="+Checks.Count+" failures="+Failures.Count);
            if(Application.isBatchMode)EditorApplication.Exit(report.passed?0:1);else EditorApplication.isPlaying=false;
        }
    }
}
