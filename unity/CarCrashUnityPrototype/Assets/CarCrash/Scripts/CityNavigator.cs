using System.Collections.Generic;
using UnityEngine;

namespace CarCrash
{
    public sealed class CityNavigator : MonoBehaviour
    {
        public Transform player;
        public int Destination { get; private set; }=-1;
        public readonly List<Vector3> Route=new();
        private readonly List<Vector3> _turnPath = new();
        public IReadOnlyList<Vector3> TurnPath => _turnPath;
        public int VisitedMask { get; private set; }
        public string Discovery { get; private set; }
        private float _discoveryUntil;
        private void Awake()=>VisitedMask=PlayerPrefs.GetInt("CarCrash.ExploredPlaces",0);
        public int VisitedCount { get {var count=0;for(var i=0;i<CityRoadNetwork.Places.Length;i++)if((VisitedMask&(1<<i))!=0)count++;return count;} }
        public void NavigateTo(int destination)
        {
            Destination = Mathf.Clamp(destination, 0, CityRoadNetwork.Places.Length - 1);
            _turnPath.Clear();
            foreach (var point in CityRoadNetwork.RouteTo(player.position, Destination))
                if (_turnPath.Count == 0 || (point - _turnPath[^1]).sqrMagnitude > 1f) _turnPath.Add(point);
            Route.Clear();
            Route.AddRange(_turnPath);
        }
        public void ClearRoute(){Destination=-1;Route.Clear();_turnPath.Clear();}
        private void Update()
        {
            if(player==null||Time.timeScale<=0)return;
            for(var i=0;i<CityRoadNetwork.Places.Length;i++)
            {
                if((VisitedMask&(1<<i))!=0||Vector3.Distance(player.position,CityRoadNetwork.Places[i].position)>23f)continue;
                VisitedMask|=1<<i;PlayerPrefs.SetInt("CarCrash.ExploredPlaces",VisitedMask);PlayerPrefs.Save();
                Discovery="DISCOVERED / "+CityRoadNetwork.Places[i].name;_discoveryUntil=Time.unscaledTime+4f;
            }
            if(Time.unscaledTime>_discoveryUntil)Discovery="";
            while(Route.Count>0&&Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(Route[0].x,Route[0].z))<12f)Route.RemoveAt(0);
            if(Destination>=0&&Vector3.Distance(player.position,CityRoadNetwork.Places[Destination].position)<23f)ClearRoute();
        }
        public string Hint
        {
            get
            {
                if(!string.IsNullOrEmpty(Discovery))return Discovery;
                if(Destination<0||Route.Count==0)return "";
                var distance=Vector3.Distance(player.position,Route[0]);
                for(var i=1;i<Route.Count;i++)distance+=Vector3.Distance(Route[i-1],Route[i]);
                var direction=Route[0]-player.position;direction.y=0;
                var angle=Vector3.SignedAngle(player.forward,direction,Vector3.up);
                var turn=Mathf.Abs(angle)>135 ? "TURN AROUND" : angle>25 ? "RIGHT" : angle < -25 ? "LEFT" : "STRAIGHT";
                return CityRoadNetwork.Places[Destination].name+" / "+turn+" / "+(distance>1000 ? (distance/1000).ToString("F1")+" KM" : distance.ToString("F0")+" M");
            }
        }
    }
}
