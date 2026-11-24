using System.Collections.Generic;
using UnityEngine;

namespace CarCrash
{
    public readonly struct CityDestination
    {
        public readonly string name, description;
        public readonly Vector3 position, entrance;
        public CityDestination(string name,string description,Vector3 position,Vector3 entrance)
        { this.name=name;this.description=description;this.position=position;this.entrance=entrance; }
    }

    public sealed class CityRoadNetwork : MonoBehaviour
    {
        public const float MinX=-310,MaxX=310,MinZ=-410,MaxZ=650;
        public const float AvenueHalfWidth = 9f, CrossStreetHalfWidth = 7f;
        public const float CurbsideLightOffset = AvenueHalfWidth + .9f;
        public static readonly float[] CrossStreetZ={-380,-280,-180,-110,-58,35,108,187,266,315,410,510,610};
        public static readonly float[] AvenueX={-270,-162,-54,0,54,162,270};
        private static readonly float[] NavigationRoadX={-270,-162,-54,0,54,162,270,296};
        public static readonly CityDestination[] Places=
        {
            new("OLD TOWN","Historic shops and downtown streets",new Vector3(2.3f,.15f,35),new Vector3(2.3f,.15f,35)),
            new("SUNSET MARINA","Piers, boats and a waterfront drive",new Vector3(296,.15f,548),new Vector3(296,.15f,510)),
            new("VISTA BEACH","Boardwalk, beach and coastal parking",new Vector3(296,.15f,-235),new Vector3(296,.15f,-280)),
            new("PALOMA PARK","Tree-lined paths and a fountain plaza",new Vector3(108,.15f,435),new Vector3(108,.15f,410)),
            new("FREIGHT YARD","Warehouses and container service lanes",new Vector3(-216,.15f,225),new Vector3(-216,.15f,187)),
            new("APEX MOTOR PARK","An open driving circuit and paddock",new Vector3(-216,.15f,532),new Vector3(-216,.15f,510))
        };

        public static float Nearest(float[] values,float point)
        { var result=values[0];foreach(var value in values)if(Mathf.Abs(point-value)<Mathf.Abs(point-result))result=value;return result; }
        public static bool InsideWorld(Vector3 position,float margin=0)=>position.x>=MinX+margin&&position.x<=MaxX-margin&&position.z>=MinZ+margin&&position.z<=MaxZ-margin;
        public static string AvenueName(float x)=>x < -210 ? "FOUNDRY AVENUE" : x < -100 ? "HARBOR WEST" : x < -20 ? "WESTSIDE AVENUE" : x < 20 ? "ELYSIAN BOULEVARD" : x < 100 ? "PACIFIC AVENUE" : x < 220 ? "PALOMA DRIVE" : "COAST HIGHWAY";
        public static string DistrictAt(Vector3 position)
        {
            foreach(var place in Places)if(Vector3.Distance(position,place.position)<65f)return place.name;
            if(position.z>390)return position.x<0 ? "NORTHSIDE / MOTOR DISTRICT" : "PALOMA / WATERFRONT";
            if(position.z < -165)return position.x>200 ? "VISTA COAST" : "SOUTH QUARTER";
            return AvenueName(Nearest(AvenueX,position.x));
        }
        public static void NearestRoadPose(Vector3 point,Vector3 forward,out Vector3 position,out Quaternion rotation)
        {
            var avenue=Nearest(AvenueX,point.x);var crossing=Nearest(CrossStreetZ,point.z);
            if(Mathf.Abs(avenue-point.x)<=Mathf.Abs(crossing-point.z))
            {
                var direction=forward.z<0 ? -1f : 1f;
                position=new Vector3(avenue+direction*2.3f,.2f,Mathf.Clamp(point.z,MinZ+15,MaxZ-15));
                rotation=Quaternion.LookRotation(Vector3.forward*direction);
            }
            else
            {
                var direction=forward.x<0 ? -1f : 1f;
                position=new Vector3(Mathf.Clamp(point.x,MinX+15,MaxX-15),.2f,crossing-direction*2.3f);
                rotation=Quaternion.LookRotation(Vector3.right*direction);
            }
        }
        public static List<Vector3> RouteTo(Vector3 from,int destination)
        {
            var place=Places[Mathf.Clamp(destination,0,Places.Length-1)];
            var avenue=Nearest(NavigationRoadX,from.x);var crossing=Nearest(CrossStreetZ,from.z);
            var path=new List<Vector3>();
            if(Mathf.Abs(avenue-from.x)<=Mathf.Abs(crossing-from.z))
            {
                path.Add(new Vector3(avenue,0,from.z));
                path.Add(new Vector3(avenue,0,place.entrance.z));
            }
            else
            {
                var cost=float.MaxValue;
                foreach(var x in NavigationRoadX)
                {
                    var length=Mathf.Abs(x-from.x)+Mathf.Abs(x-place.entrance.x)+Mathf.Abs(crossing-place.entrance.z);
                    if(length>=cost)continue;cost=length;avenue=x;
                }
                path.Add(new Vector3(from.x,0,crossing));path.Add(new Vector3(avenue,0,crossing));
                path.Add(new Vector3(avenue,0,place.entrance.z));
            }
            path.Add(place.entrance);path.Add(place.position);
            return path;
        }
    }
}
