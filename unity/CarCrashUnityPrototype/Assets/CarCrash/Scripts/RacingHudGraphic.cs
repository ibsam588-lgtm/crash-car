using UnityEngine;
using UnityEngine.UI;

namespace CarCrash
{
    // Resolution-independent HUD art; no baked gauge or mock telemetry textures.
    public sealed class RacingHudGraphic : MaskableGraphic
    {
        public enum Shape { Panel, Gauge, Radar, Atmosphere, CityMap }
        public Shape shape;
        public float value;
        public bool boosting;
        public bool showroom;
        public Transform player;
        public MovingTrafficObstacle[] traffic;
        public BoxCollider[] cityBuildings;
        public CityNavigator navigation;
        public Color accent = new(0.78f, 1f, 0.22f);

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
                default: Panel(vh, r); break;
            }
        }

        private void Panel(VertexHelper vh, Rect r)
        {
            var cut = Mathf.Min(12f, r.height * 0.2f);
            var points = new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax - cut, r.yMin),
                new Vector2(r.xMax, r.yMin + cut), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };
            var start = vh.currentVertCount;
            foreach (var point in points) vh.AddVert(point, color, Vector2.zero);
            for (var i = 1; i < points.Length - 1; i++) vh.AddTriangle(start, start + i, start + i + 1);
            Line(vh, new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), 1f,
                new Color(0.77f, 0.87f, 0.91f, 0.23f));
        }

        private void Gauge(VertexHelper vh, Rect r)
        {
            var center = r.center;
            var radius = Mathf.Min(r.width, r.height) * 0.46f;
            Disc(vh, center, radius + 4f, new Color(0.015f, 0.025f, 0.035f, 0.85f));
            Arc(vh, center, radius, 1f, 0f, 1f, new Color(0.8f, 0.9f, 1f, 0.22f));
            Arc(vh, center, radius - 8f, 5f, 0f, 1f, new Color(0.7f, 0.8f, 0.87f, 0.12f));
            Arc(vh, center, radius - 8f, 5f, 0f, Mathf.Clamp01(value), boosting ? new Color(0.24f, 0.85f, 1f) : accent);
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
            Quad(vh, inner.min, inner.max, new Color(0.27f, 0.29f, 0.27f, 0.95f));
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
                    if (min.x < max.x && min.y < max.y) Quad(vh, min, max, new Color(0.15f, 0.17f, 0.16f));
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
                    new Vector2(Mathf.Min(inner.xMax, x + width * .5f), top),new Color(.69f,.70f,.66f));
            }
            foreach (var crossing in CityRoadNetwork.CrossStreetZ)
            {
                var y = Map(new Vector3(player.position.x, 0, crossing)).y;
                if (y < inner.yMin || y > inner.yMax) continue;
                var width = Mathf.Max(2f,14f*(overview ? scale : inner.height/spanZ));
                Quad(vh, new Vector2(Mathf.Max(inner.xMin,Map(new Vector3(CityRoadNetwork.MinX,0,crossing)).x), Mathf.Max(inner.yMin, y - width * .5f)),
                    new Vector2(Mathf.Min(inner.xMax,Map(new Vector3(CityRoadNetwork.MaxX,0,crossing)).x), Mathf.Min(inner.yMax, y + width * .5f)), new Color(.69f,.70f,.66f));
            }
            var coastSouth=Map(new Vector3(296,0,-385));var coastNorth=Map(new Vector3(296,0,615));
            if(coastSouth.x>=inner.xMin&&coastSouth.x<=inner.xMax&&coastNorth.y>=inner.yMin&&coastSouth.y<=inner.yMax)
                Line(vh,new Vector2(coastSouth.x,Mathf.Max(inner.yMin,coastSouth.y)),new Vector2(coastNorth.x,Mathf.Min(inner.yMax,coastNorth.y)),Mathf.Max(2,14*(overview ? scale : inner.width/spanX)),new Color(.69f,.70f,.66f));
            if(navigation!=null)
            {
                var previous=player.position;
                foreach(var waypoint in navigation.Route)
                {
                    var a=Map(previous);var b=Map(waypoint);previous=waypoint;
                    if((a.x<inner.xMin&&b.x<inner.xMin)||(a.x>inner.xMax&&b.x>inner.xMax)||(a.y<inner.yMin&&b.y<inner.yMin)||(a.y>inner.yMax&&b.y>inner.yMax))continue;
                    a=new Vector2(Mathf.Clamp(a.x,inner.xMin,inner.xMax),Mathf.Clamp(a.y,inner.yMin,inner.yMax));
                    b=new Vector2(Mathf.Clamp(b.x,inner.xMin,inner.xMax),Mathf.Clamp(b.y,inner.yMin,inner.yMax));
                    Line(vh,a,b,overview ? 3 : 2,new Color(.96f,.72f,.25f));
                }
            }
            for(var i=0;i<CityRoadNetwork.Places.Length;i++)
            {
                var marker=Map(CityRoadNetwork.Places[i].position);if(!inner.Contains(marker))continue;
                Disc(vh,marker,overview ? 9f : 3.5f,new Color(.93f,.73f,.33f));
            }
            if (traffic != null)
            {
                foreach (var target in traffic)
                {
                    if (target == null || !target.gameObject.activeInHierarchy) continue;
                    var point = Map(target.transform.position);
                    if (!inner.Contains(point)) continue;
                    Disc(vh, point, 2f, target.IsReleased ? new Color(0.85f, 0.29f, 0.20f) : new Color(0.50f, 0.71f, 0.81f));
                }
            }
            var p = Map(player.position);
            var direction = new Vector2(player.forward.x, player.forward.z).normalized;
            var side = new Vector2(-direction.y, direction.x);
            Triangle(vh, p + direction * 7, p - direction * 4 + side * 5, p - direction * 4 - side * 5, Color.white);
        }
        private void Atmosphere(VertexHelper vh, Rect r)
        {
            var dark = color;
            var topShade = new Color(dark.r, dark.g, dark.b, showroom ? 0.68f : 0.84f);
            GradientQuad(vh, new Vector2(r.xMin, r.yMax - r.height * (showroom ? 0.45f : 0.28f)), new Vector2(r.xMax, r.yMax), Color.clear, topShade);
            GradientQuad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin + r.height * 0.38f), dark, Color.clear);
            if (showroom && r.width > r.height * 1.4f)
                Four(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax),
                    new Vector2(r.xMin + r.width * 0.55f, r.yMax), new Vector2(r.xMin + r.width * 0.55f, r.yMin),
                    new Color(0.008f, 0.018f, 0.025f, 0.60f), Color.clear);
            if (value < 0.35f) return;
            var tint = boosting ? new Color(0.2f, 0.78f, 1f, 0.23f) : new Color(0.85f, 0.93f, 1f, 0.12f);
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
