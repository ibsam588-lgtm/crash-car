using System;
using System.Collections.Generic;
using System.IO;
using CarCrash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace CarCrashEditor
{
    [InitializeOnLoad]
    public static class CarCrashTraversalValidation
    {
        private const string Key="CarCrash.TraversalQa";
        private static CarCrashHudInput _session;
        private static int _stage,_index;
        private static double _next,_deadline;
        private static readonly List<Result> Results=new();
        [Serializable] private class Result { public string vehicle; public float reachedX,reverseX,frontAxleX; public bool climbed,escaped; }
        [Serializable] private class Report { public bool passed; public Result[] vehicles; public string error; }
        static CarCrashTraversalValidation(){if(SessionState.GetBool(Key,false)) Attach();}
        [MenuItem("Car Crash/Validate Curb Traversal")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/CarCrash/Scenes/ImpactRunPrototype.unity");
            SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;
        }
        private static void Attach()
        {
            _stage=0;_index=0;_next=0;_deadline=EditorApplication.timeSinceStartup+100;Results.Clear();
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        private static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>_deadline)throw new TimeoutException("Traversal QA timed out");
                if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<_next)return;
                switch(_stage)
                {
                    case 0:
                        _session=Object.FindFirstObjectByType<CarCrashHudInput>();if(_session==null||_session.Hud==null)return;
                        foreach(var traffic in Object.FindObjectsByType<MovingTrafficObstacle>(FindObjectsSortMode.None))traffic.gameObject.SetActive(false);
                        foreach(var person in Object.FindObjectsByType<CityPedestrian>(FindObjectsSortMode.None))person.gameObject.SetActive(false);
                        _session.SendMessage("OnApplicationFocus",true);_session.StartFreeDrive();_stage=1;break;
                    case 1:
                        if(_session.Phase!=CarCrashHudInput.RunPhase.Driving)return;
                        _session.Garage.Select(_index);
                        _session.vehicle.RecoverAt(new Vector3(5,.15f,-28),Quaternion.LookRotation(Vector3.right));
                        _next=EditorApplication.timeSinceStartup+.6;_stage=2;break;
                    case 2:
                        Hold("Accelerator pedal");_next=EditorApplication.timeSinceStartup+2;_stage=3;break;
                    case 3:
                        var row=new Result{vehicle=_session.Garage.Current.id,reachedX=_session.vehicle.transform.position.x};
                        _session.vehicle.frontLeftCollider.GetWorldPose(out var axle,out _);row.frontAxleX=axle.x;
                        row.climbed=row.frontAxleX>9.75f;Results.Add(row);
                        _session.Hud.ReleaseControls();Hold("Reverse pedal");
                        _next=EditorApplication.timeSinceStartup+2;_stage=4;break;
                    case 4:
                        var result=Results[^1];result.reverseX=_session.vehicle.transform.position.x;
                        result.escaped=result.reverseX<result.reachedX-1.5f;
                        Debug.Log("TRAVERSAL "+JsonUtility.ToJson(result));
                        _session.Hud.ReleaseControls();_index++;
                        if(_index==6)Complete(null);else _stage=1;
                        break;
                }
            }
            catch(Exception error){Complete(error.ToString());}
        }
        private static void Hold(string name)=>ExecuteEvents.Execute(GameObject.Find(name),new PointerEventData(EventSystem.current){pointerId=70},ExecuteEvents.pointerDownHandler);
        private static void Complete(string error)
        {
            EditorApplication.update-=Tick;SessionState.SetBool(Key,false);
            var passed=error==null&&Results.Count==6;foreach(var row in Results)passed&=row.climbed&&row.escaped;
            Directory.CreateDirectory("Builds/Validation");
            File.WriteAllText("Builds/Validation/traversal-qa.json",JsonUtility.ToJson(new Report{passed=passed,vehicles=Results.ToArray(),error=error},true));
            Debug.Log("TRAVERSAL_QA_"+(passed?"PASS":"FAIL"));
            if(Application.isBatchMode)EditorApplication.Exit(passed?0:1);else EditorApplication.isPlaying=false;
        }
    }
}
