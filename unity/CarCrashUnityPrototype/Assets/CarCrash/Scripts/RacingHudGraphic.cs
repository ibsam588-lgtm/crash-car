using UnityEngine;
using UnityEngine.UI;

namespace CarCrash
{
    // Resolution-independent HUD art; no baked gauge or mock telemetry textures.
    public sealed class RacingHudGraphic : MaskableGraphic
    {
        public enum Shape { Panel, Gauge, Radar, Atmosphere, CityMap, Circle, Icon, CoursePreview }
        public enum Symbol { Play, ArrowLeft, ArrowRight, Map, Camera, Recover, Pause, Garage, Bolt, Plus, Minus, Locate, Lock }
        public Symbol symbol;
        public float cornerRadius=16,strokeWidth=1;
        public Color borderColor=CoastalHudTheme.Border;
        private readonly Vector2[] _outer=new Vector2[36],_inner=new Vector2[36];
        public Shape shape;
        public float value;
        public bool boosting;
        public bool showroom;
        public Transform player;
        public MovingTrafficObstacle[] traffic;
        public BoxCollider[] cityBuildings;
        public CityNavigator navigation;
        public ImpactMission mission;
        public ImpactCourse previewCourse;
        public Color accent = CoastalHudTheme.Seafoam;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            switch (shape)
            {
                case Shape.Gauge: Gauge(vh, r); break;
                case Shape.Radar: Radar(vh, r); break;
                case Shape.CityMap: Radar(vh,r);break;
                case Shape.Atmosphere: Atmosphere(vh, r); break;
                case Shape.Circle:
                    var radius=Mathf.Min(r.width,r.height)*.5f;
                    if(borderColor.a>0&&strokeWidth>0){Disc(vh,r.center,radius,borderColor);Disc(vh,r.center,Mathf.Max(0,radius-strokeWidth),color);}
                    else Disc(vh,r.center,radius,color);
                    break;
                case Shape.Icon: Icon(vh,r);break;
                case Shape.CoursePreview: CoursePreview(vh, r);break;
                default: Panel(vh, r); break;
            }
        }

        private void Panel(VertexHelper vh, Rect r)
        {
            var radius=Mathf.Min(cornerRadius,Mathf.Min(r.width,r.height)*.5f);
            RoundedPoints(r,radius,_outer);
            var start = vh.currentVertCount;
            var top=new Color(color.r*1.14f,color.g*1.14f,color.b*1.14f,color.a);
            vh.AddVert(r.center,Color.Lerp(color,top,.5f),Vector2.zero);
            for(var i=0;i<_outer.Length;i++)vh.AddVert(_outer[i],Color.Lerp(color,top,Mathf.InverseLerp(r.yMin,r.yMax,_outer[i].y)),Vector2.zero);
            for(var i=0;i<_outer.Length;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%_outer.Length);
            if(strokeWidth<=0||borderColor.a<=0)return;
            var inset=new Rect(r.xMin+strokeWidth,r.yMin+strokeWidth,r.width-strokeWidth*2,r.height-strokeWidth*2);
            RoundedPoints(inset,Mathf.Max(0,radius-strokeWidth),_inner);
            var border=borderColor;border.a*=color.a;
            for(var i=0;i<_outer.Length;i++){var j=(i+1)%_outer.Length;Four(vh,_outer[i],_outer[j],_inner[j],_inner[i],border,border);}
        }

        private static void RoundedPoints(Rect r,float radius,Vector2[] points)
        {
            for(var corner=0;corner<4;corner++)
            {
                var center=new Vector2(corner is 0 or 3 ? r.xMax-radius : r.xMin+radius,corner<2 ? r.yMax-radius : r.yMin+radius);
                for(var step=0;step<=8;step++)
                {
                    var angle=(corner*90+step*90f/8)*Mathf.Deg2Rad;
                    points[corner*9+step]=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                }
            }
        }

        private void Icon(VertexHelper vh,Rect r)
        {
            Vector2 P(float x,float y)=>new(r.xMin+x*r.width,r.yMin+y*r.height);
            void L(float x,float y,float a,float b)=>Line(vh,P(x,y),P(a,b),Mathf.Max(1.5f,r.width*.075f),color);
            switch(symbol)
            {
                case Symbol.Play:Triangle(vh,P(.29f,.15f),P(.85f,.5f),P(.29f,.85f),color);break;
                case Symbol.ArrowLeft:L(.82f,.5f,.18f,.5f);L(.18f,.5f,.48f,.8f);L(.18f,.5f,.48f,.2f);break;
                case Symbol.ArrowRight:L(.18f,.5f,.82f,.5f);L(.82f,.5f,.52f,.8f);L(.82f,.5f,.52f,.2f);break;
                case Symbol.Pause:L(.34f,.2f,.34f,.8f);L(.67f,.2f,.67f,.8f);break;
                case Symbol.Plus:L(.2f,.5f,.8f,.5f);L(.5f,.2f,.5f,.8f);break;
                case Symbol.Minus:L(.2f,.5f,.8f,.5f);break;
                case Symbol.Bolt:L(.62f,.92f,.27f,.47f);L(.27f,.47f,.7f,.53f);L(.7f,.53f,.38f,.08f);break;
                case Symbol.Map:L(.12f,.25f,.12f,.8f);L(.12f,.8f,.37f,.9f);L(.37f,.9f,.65f,.76f);L(.65f,.76f,.88f,.86f);L(.88f,.86f,.88f,.3f);L(.88f,.3f,.65f,.2f);L(.65f,.2f,.37f,.34f);L(.37f,.34f,.12f,.25f);L(.37f,.34f,.37f,.9f);L(.65f,.2f,.65f,.76f);break;
                case Symbol.Camera:L(.14f,.24f,.86f,.24f);L(.14f,.24f,.14f,.73f);L(.86f,.24f,.86f,.73f);L(.14f,.73f,.34f,.73f);L(.34f,.73f,.4f,.85f);L(.4f,.85f,.66f,.85f);L(.66f,.85f,.72f,.73f);L(.72f,.73f,.86f,.73f);CircleArc(vh,P(.51f,.49f),r.width*.15f,r.width*.06f,0,360,color);break;
                case Symbol.Garage:L(.12f,.18f,.12f,.65f);L(.12f,.65f,.5f,.88f);L(.5f,.88f,.88f,.65f);L(.88f,.65f,.88f,.18f);L(.27f,.2f,.27f,.52f);L(.27f,.52f,.73f,.52f);L(.73f,.52f,.73f,.2f);L(.27f,.36f,.73f,.36f);break;
                case Symbol.Recover:CircleArc(vh,P(.5f,.5f),r.width*.32f,r.width*.065f,65,295,color);L(.82f,.5f,.67f,.65f);L(.82f,.5f,.96f,.65f);break;
                case Symbol.Locate:CircleArc(vh,r.center,r.width*.25f,r.width*.06f,0,360,color);L(.5f,.05f,.5f,.25f);L(.5f,.75f,.5f,.95f);L(.05f,.5f,.25f,.5f);L(.75f,.5f,.95f,.5f);break;
                case Symbol.Lock:
                    L(.27f,.17f,.73f,.17f); L(.73f,.17f,.73f,.55f); L(.73f,.55f,.27f,.55f); L(.27f,.55f,.27f,.17f);
                    L(.35f,.55f,.35f,.77f); L(.35f,.77f,.43f,.87f); L(.43f,.87f,.57f,.87f); L(.57f,.87f,.65f,.77f); L(.65f,.77f,.65f,.55f);
                    break;
            }
        }

        private void Gauge(VertexHelper vh, Rect r)
        {
            var center = r.center;
            var radius = Mathf.Min(r.width, r.height) * 0.46f;
            Disc(vh, center, radius + 4f, CoastalHudTheme.Panel);
            Arc(vh, center, radius, 1f, 0f, 1f, CoastalHudTheme.Border);
            Arc(vh, center, radius - 8f, 5f, 0f, 1f, new Color(.70f,.87f,.80f,.16f));
            Arc(vh, center, radius - 8f, 5f, 0f, Mathf.Clamp01(value), boosting ? CoastalHudTheme.Amber : accent);
            Arc(vh, center, radius - 8f, 5f, 0.83f, 1f, new Color(1f, 0.26f, 0.18f, 0.85f));
            for (var i = 0; i <= 40; i++)
            {
                var angle = (225f - i * 6.75f) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var major = i % 5 == 0;
                Line(vh, center + direction * (radius - 18f), center + direction * (radius - (major ? 30f : 23f)),
                    major ? 2f : 1f, i >= 34 ? new Color(1f, 0.3f, 0.2f) : new Color(0.9f, 0.95f, 1f, major ? 0.9f : 0.3f));
            }
            var needleAngle = (225f - Mathf.Clamp01(value) * 270f) * Mathf.Deg2Rad;
            var needle = new Vector2(Mathf.Cos(needleAngle), Mathf.Sin(needleAngle));
            var normal = new Vector2(-needle.y, needle.x);
            Triangle(vh, center + needle * (radius - 35f), center + normal * 3f - needle * 6f,
                center - normal * 3f - needle * 6f, new Color(1f, 0.38f, 0.2f));
            Disc(vh, center, 5f, new Color(0.8f, 0.86f, 0.88f));
        }

        private void Radar(VertexHelper vh, Rect r)
        {
            Panel(vh, r);
            var inner = new Rect(r.xMin + 4, r.yMin + 4, r.width - 8, r.height - 8);
            if (player == null) return;
            var overview=shape==Shape.CityMap;
            var spanX=overview ? CityRoadNetwork.MaxX-CityRoadNetwork.MinX : 240f;
            var spanZ=overview ? CityRoadNetwork.MaxZ-CityRoadNetwork.MinZ : 240f;
            var center=overview ? new Vector3(0,0,(CityRoadNetwork.MinZ+CityRoadNetwork.MaxZ)*.5f) : player.position;
            var scale=overview ? Mathf.Min(inner.width/spanX,inner.height/spanZ) : 0;
            Vector2 Map(Vector3 world) => new(inner.center.x+(world.x-center.x)*(overview ? scale : inner.width/spanX),
                inner.center.y+(world.z-center.z)*(overview ? scale : inner.height/spanZ));
            if (cityBuildings != null)
            {
                foreach (var building in cityBuildings)
                {
                    if (building == null) continue;
                    var bounds = building.bounds;
                    if (bounds.size.y < 4f) continue;
                    var a = Map(bounds.min); var b = Map(bounds.max);
                    var min = new Vector2(Mathf.Max(inner.xMin, a.x), Mathf.Max(inner.yMin, a.y));
                    var max = new Vector2(Mathf.Min(inner.xMax, b.x), Mathf.Min(inner.yMax, b.y));
                    if (min.x < max.x && min.y < max.y) Quad(vh, min, max, CoastalHudTheme.MapBuilding);
                }
            }
            foreach (var avenue in CityRoadNetwork.AvenueX)
            {
                var x = Map(new Vector3(avenue, 0, player.position.z)).x;
                if (x < inner.xMin || x > inner.xMax) continue;
                var width = Mathf.Max(2f,18f*(overview ? scale : inner.width/spanX));
                var bottom=Mathf.Max(inner.yMin,Map(new Vector3(avenue,0,CityRoadNetwork.MinZ)).y);
                var top=Mathf.Min(inner.yMax,Map(new Vector3(avenue,0,CityRoadNetwork.MaxZ)).y);
                Quad(vh, new Vector2(Mathf.Max(inner.xMin, x - width * .5f), bottom),
                    new Vector2(Mathf.Min(inner.xMax, x + width * .5f), top),CoastalHudTheme.MapRoad);
            }
            foreach (var crossing in CityRoadNetwork.CrossStreetZ)
            {
                var y = Map(new Vector3(player.position.x, 0, crossing)).y;
                if (y < inner.yMin || y > inner.yMax) continue;
                var width = Mathf.Max(2f,14f*(overview ? scale : inner.height/spanZ));
                Quad(vh, new Vector2(Mathf.Max(inner.xMin,Map(new Vector3(CityRoadNetwork.MinX,0,crossing)).x), Mathf.Max(inner.yMin, y - width * .5f)),
                    new Vector2(Mathf.Min(inner.xMax,Map(new Vector3(CityRoadNetwork.MaxX,0,crossing)).x), Mathf.Min(inner.yMax, y + width * .5f)), CoastalHudTheme.MapRoad);
            }
            var coastSouth=Map(new Vector3(296,0,-385));var coastNorth=Map(new Vector3(296,0,615));
            if(coastSouth.x>=inner.xMin&&coastSouth.x<=inner.xMax&&coastNorth.y>=inner.yMin&&coastSouth.y<=inner.yMax)
                Line(vh,new Vector2(coastSouth.x,Mathf.Max(inner.yMin,coastSouth.y)),new Vector2(coastNorth.x,Mathf.Min(inner.yMax,coastNorth.y)),Mathf.Max(2,14*(overview ? scale : inner.width/spanX)),CoastalHudTheme.MapRoad);
            if (mission?.Course != null || navigation != null)
            {
                var previous=player.position;
                var route = mission?.Course != null ? mission.Guidance : navigation.Route;
                foreach(var waypoint in route)
                {
                    var a=Map(previous);var b=Map(waypoint);previous=waypoint;
                    if((a.x<inner.xMin&&b.x<inner.xMin)||(a.x>inner.xMax&&b.x>inner.xMax)||(a.y<inner.yMin&&b.y<inner.yMin)||(a.y>inner.yMax&&b.y>inner.yMax))continue;
                    a=new Vector2(Mathf.Clamp(a.x,inner.xMin,inner.xMax),Mathf.Clamp(a.y,inner.yMin,inner.yMax));
                    b=new Vector2(Mathf.Clamp(b.x,inner.xMin,inner.xMax),Mathf.Clamp(b.y,inner.yMin,inner.yMax));
                    Line(vh,a,b,overview ? 3 : 2,accent);
                }
            }
            for(var i=0;i<CityRoadNetwork.Places.Length;i++)
            {
                var marker=Map(CityRoadNetwork.Places[i].position);if(!inner.Contains(marker))continue;
                var active=navigation!=null&&navigation.Destination==i;
                Disc(vh,marker,overview ? (active ? 13 : 11) : 5f,CoastalHudTheme.Ink);
                Disc(vh,marker,overview ? 9f : 3.5f,active ? CoastalHudTheme.Amber : accent);
            }
            if (traffic != null)
            {
                foreach (var target in traffic)
                {
                    if (target == null || !target.gameObject.activeInHierarchy) continue;
                    var point = Map(target.transform.position);
                    if (!inner.Contains(point)) continue;
                    Disc(vh, point, 2f, target.IsReleased ? CoastalHudTheme.Coral : CoastalHudTheme.Muted);
                }
            }
            var p = Map(player.position);
            Disc(vh,p,10,new Color(accent.r,accent.g,accent.b,.23f));
            var direction = new Vector2(player.forward.x, player.forward.z).normalized;
            var side = new Vector2(-direction.y, direction.x);
            Triangle(vh, p + direction * 7, p - direction * 4 + side * 5, p - direction * 4 - side * 5, Color.white);
        }
        private void CoursePreview(VertexHelper vh, Rect rect)
        {
            Panel(vh, rect);
            var course = previewCourse ?? mission?.Course;
            if (course == null) return;
            var bounds = new Bounds(course.Start, Vector3.zero);
            foreach (var gate in course.Gates)
            {
                bounds.Encapsulate(gate.Position);
                foreach (var turn in gate.Turns) bounds.Encapsulate(turn);
            }

            var inset = new Rect(rect.xMin + 21, rect.yMin + 20, rect.width - 42, rect.height - 40);
            var scale = Mathf.Min(inset.width / Mathf.Max(1, bounds.size.x), inset.height / Mathf.Max(1, bounds.size.z));
            Vector2 Map(Vector3 point) => inset.center + new Vector2(point.x - bounds.center.x, point.z - bounds.center.z) * scale;
            var grid = new Color(.50f,.73f,.66f,.18f);
            for (var x = inset.xMin; x <= inset.xMax; x += 25f) Line(vh, new Vector2(x,inset.yMin), new Vector2(x,inset.yMax), 1f, grid);
            for (var y = inset.yMin; y <= inset.yMax; y += 25f) Line(vh, new Vector2(inset.xMin,y), new Vector2(inset.xMax,y), 1f, grid);

            var previous = Map(course.Start);
            foreach (var gate in course.Gates)
            {
                foreach (var turn in gate.Turns)
                {
                    var point = Map(turn);
                    Line(vh, previous, point, 4, accent);
                    previous = point;
                }
                var endpoint = Map(gate.Position);
                Line(vh, previous, endpoint, 4, accent);
                previous = endpoint;
            }
            Disc(vh, Map(course.Start), 5, new Color(.59f,.94f,.72f));
            for (var i = 0; i < course.Gates.Length; i++)
            {
                var gate = course.Gates[i];
                var point = Map(gate.Position);
                var finish = i == course.Gates.Length - 1;
                var selected = mission?.NextGate == gate;
                var tint = finish ? CoastalHudTheme.Amber : selected ? CoastalHudTheme.Cream : accent;
                Disc(vh, point, selected ? 9 : 7, new Color(.03f,.10f,.14f));
                Disc(vh, point, selected ? 6.5f : 4.6f, tint);
            }
            if (player != null)
            {
                var point = Map(player.position);
                point.x = Mathf.Clamp(point.x, inset.xMin - 7, inset.xMax + 7);
                point.y = Mathf.Clamp(point.y, inset.yMin - 7, inset.yMax + 7);
                var forward = new Vector2(player.forward.x, player.forward.z).normalized;
                var side = new Vector2(-forward.y, forward.x);
                Triangle(vh, point + forward * 9, point - forward * 6 + side * 6, point - forward * 6 - side * 6, Color.white);
            }
        }

        private void Atmosphere(VertexHelper vh, Rect r)
        {
            var dark = color;
            var topShade = new Color(dark.r, dark.g, dark.b, showroom ? 0.63f : 0.44f);
            GradientQuad(vh, new Vector2(r.xMin, r.yMax - r.height * (showroom ? 0.45f : 0.28f)), new Vector2(r.xMax, r.yMax), Color.clear, topShade);
            GradientQuad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin + r.height * 0.38f), dark, Color.clear);
            if (showroom && r.width > r.height * 1.4f)
                Four(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax),
                    new Vector2(r.xMin + r.width * 0.55f, r.yMax), new Vector2(r.xMin + r.width * 0.55f, r.yMin),
                    new Color(.014f, .047f, .043f, .54f), Color.clear);
            if (value < 0.35f) return;
            var tint = boosting ? new Color(1f,.58f,.30f,.21f) : new Color(.90f,.98f,.90f,.10f);
            for (var i = 0; i < 12; i++)
            {
                var t = Mathf.Repeat(Time.unscaledTime * (boosting ? 2f : 1.2f) + i * 0.187f, 1f);
                var side = i % 2 == 0 ? -1f : 1f;
                var x = side < 0f ? r.xMin : r.xMax;
                var y = Mathf.Lerp(r.yMin, r.yMax, i / 12f);
                var end = new Vector2(x + side * 40f * t, y);
                var start = new Vector2(x - side * r.width * 0.1f * (1f - t), Mathf.Lerp(y, r.center.y, 0.1f));
                Line(vh, start, end, boosting ? 2f : 1f, tint);
            }
        }

        private static void Arc(VertexHelper vh, Vector2 center, float radius, float thickness, float from, float to, Color tint)
        {
            var steps = Mathf.Max(1, Mathf.CeilToInt((to - from) * 100f));
            for (var i = 0; i < steps; i++)
            {
                var a = (225f - Mathf.Lerp(from, to, i / (float)steps) * 270f) * Mathf.Deg2Rad;
                var b = (225f - Mathf.Lerp(from, to, (i + 1f) / steps) * 270f) * Mathf.Deg2Rad;
                var da = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var db = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Four(vh, center + da * radius, center + db * radius,
                    center + db * (radius - thickness), center + da * (radius - thickness), tint, tint);
            }
        }

        private static void CircleArc(VertexHelper vh,Vector2 center,float radius,float thickness,float from,float sweep,Color tint)
        {
            const int steps=40;
            for(var i=0;i<steps;i++)
            {
                var a=(from+sweep*i/steps)*Mathf.Deg2Rad;var b=(from+sweep*(i+1)/steps)*Mathf.Deg2Rad;
                var da=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var db=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                Four(vh,center+da*radius,center+db*radius,center+db*(radius-thickness),center+da*(radius-thickness),tint,tint);
            }
        }

        private static void Disc(VertexHelper vh, Vector2 center, float radius, Color tint)
        {
            for (var i = 0; i < 48; i++)
            {
                var a = i * Mathf.PI * 2f / 48f;
                var b = (i + 1f) * Mathf.PI * 2f / 48f;
                Triangle(vh, center, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, tint);
            }
        }

        private static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            var i = vh.currentVertCount;
            vh.AddVert(a, tint, Vector2.zero); vh.AddVert(b, tint, Vector2.zero); vh.AddVert(c, tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
        }
        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            var normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * 0.5f;
            Four(vh, a - normal, b - normal, b + normal, a + normal, tint, tint);
        }
        private static void Quad(VertexHelper vh, Vector2 min, Vector2 max, Color tint) => GradientQuad(vh, min, max, tint, tint);
        private static void GradientQuad(VertexHelper vh, Vector2 min, Vector2 max, Color bottom, Color top)
        {
            Four(vh, min, new Vector2(max.x, min.y), max, new Vector2(min.x, max.y), bottom, top);
        }
        private static void Four(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color bottom, Color top)
        {
            var i = vh.currentVertCount;
            vh.AddVert(a, bottom, Vector2.zero); vh.AddVert(b, bottom, Vector2.zero);
            vh.AddVert(c, top, Vector2.zero); vh.AddVert(d, top, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
