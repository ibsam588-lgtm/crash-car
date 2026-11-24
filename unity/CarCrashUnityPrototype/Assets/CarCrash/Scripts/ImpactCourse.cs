using System;
using UnityEngine;

namespace CarCrash
{
    public enum ImpactTargetKind { Cone, Crate, Barrier, Streetlight, Wreck }

    public readonly struct ImpactTargetSpec
    {
        public readonly ImpactTargetKind Kind;
        public readonly Vector3 Position;
        public readonly float Yaw;

        public ImpactTargetSpec(ImpactTargetKind kind, float x, float z, float yaw = 0f, float groundHeight = .04f)
        {
            Kind = kind;
            Position = new Vector3(x, groundHeight, z);
            Yaw = yaw;
        }

        public static readonly int[] Points = { 10, 25, 50, 100, 200 };
        public static readonly string[] Labels = { "Cones", "Crates", "Barriers", "Streetlights", "Wrecks" };
        public static readonly string[] HitLabels = { "Cone", "Crate", "Barrier", "Streetlight", "Staged wreck" };
    }

    public sealed class ImpactGate
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly float Width;
        public readonly string Name;
        public readonly Vector3[] Turns;

        public ImpactGate(string name, float x, float z, float yaw, float width, params Vector3[] turns)
        {
            Position = new Vector3(x, 0.22f, z);
            Rotation = Quaternion.Euler(0f, yaw, 0f);
            Width = width;
            Name = name;
            Turns = turns;
        }

        public bool Crossed(Vector3 from, Vector3 to)
        {
            var forward = Rotation * Vector3.forward;
            var before = Vector3.Dot(from - Position, forward);
            var after = Vector3.Dot(to - Position, forward);
            if (before >= 0f || after < 0f) return false;
            var atGate = Vector3.Lerp(from, to, before / (before - after)) - Position;
            return Mathf.Abs(Vector3.Dot(atGate, Rotation * Vector3.right)) <= Width * 0.5f
                && atGate.y >= -1f && atGate.y <= 3.8f;
        }
    }

    public sealed class ImpactCourse
    {
        public readonly string Id, Name, Brief;
        public readonly Vector3 Start;
        public readonly Quaternion Heading;
        public readonly ImpactGate[] Gates;
        public readonly ImpactTargetSpec[] Targets;
        public readonly int Bronze, Silver, Gold, TimeLimit;

        public ImpactCourse(string id, string name, string brief, Vector3 start, float yaw, int bronze, int silver,
            int gold, int timeLimit, ImpactGate[] gates, ImpactTargetSpec[] targets)
        {
            Id = id; Name = name; Brief = brief; Start = start;
            Heading = Quaternion.Euler(0f, yaw, 0f);
            Bronze = bronze; Silver = silver; Gold = gold; TimeLimit = timeLimit;
            Gates = gates; Targets = targets;
        }

        public string Medal(int score) => score >= Gold ? "Gold" : score >= Silver ? "Silver" : score >= Bronze ? "Bronze" : "Finisher";

        private static Vector3 P(float x, float z) => new(x, 0.22f, z);
        private static ImpactTargetSpec C(float x, float z) => new(ImpactTargetKind.Cone, x, z);
        private static ImpactTargetSpec B(float x, float z, float yaw = 0f) => new(ImpactTargetKind.Barrier, x, z, yaw);
        private static ImpactTargetSpec W(float x, float z, float yaw = 0f) => new(ImpactTargetKind.Wreck, x, z, yaw);
        private static ImpactTargetSpec K(float x, float z, float yaw = 0f) => new(ImpactTargetKind.Crate, x, z, yaw);
        private static ImpactTargetSpec Lamp(float x, float z, float yaw = 0f) => new(ImpactTargetKind.Streetlight, x, z, yaw);
        private static ImpactTargetSpec CurbLamp(float avenue, float z) =>
            new(ImpactTargetKind.Streetlight, avenue + CityRoadNetwork.CurbsideLightOffset, z, 180f, .15f);

        public static readonly ImpactCourse[] Catalog =
        {
            new("freight-yard", "Freight Yard", "Follow Harbor West into the depot. Choose the wide lane or aim for the stacked cargo in the loading bays.",
                P(-159.7f, -347f), 0f, 180, 380, 620, 0,
                new ImpactGate[]
                {
                    new("Foundry junction", -238f, -55.5f, -90f, 12f, P(-159.7f, -55.5f)),
                    new("Freight approach", -267.7f, 141f, 0f, 15f, P(-267.7f, -55.5f)),
                    new("Yard entry", -216f, 204f, 0f, 12f, P(-267.7f, 184.6f), P(-216f, 184.6f)),
                    new("Depot finish", -216f, 236f, 0f, 56f),
                },
                new[]
                {
                    C(-159.7f,-330), C(-159.7f,-325), C(-159.7f,-320), C(-158.8f,-315),
                    K(-159.6f,-300), C(-159.5f,-265), C(-160,-259), K(-159,-249),
                    B(-158.7f,-222,15), K(-158.5f,-198), C(-158.5f,-160), K(-158.5f,-152),
                    B(-158.5f,-137), C(-161.1f,-96), K(-161,-85), C(-182,-55.2f),
                    B(-197,-55.3f,70), K(-210,-56.1f), C(-222,-55.2f),
                    C(-267.7f,-27), K(-267.7f,-20), C(-267.7f,-9), B(-266.4f,4,20),
                    K(-267.8f,54), K(-267.8f,57), C(-269,65), CurbLamp(-270,67),
                    C(-267.7f,116), B(-267.7f,126,12), C(-268,164),
                    K(-248,184.5f), C(-233,184.6f), C(-216,199), C(-214,210),
                    C(-207,218), C(-206,222), K(-206,225), C(-206,230),
                    B(-227.5f,215,35), K(-232.3f,219), new(ImpactTargetKind.Crate,-232.3f,219.9f,18),
                    K(-232.7f,222), Lamp(-240.7f,230), B(-227.5f,231,16), W(-215,229,14),
                }),
            new("old-town", "Old Town", "Wind between downtown storefronts. Line up small targets on the approaches, then slow for the turns.",
                P(2.3f, -253f), 0f, 160, 340, 520, 0,
                new ImpactGate[]
                {
                    new("Market street", 2.3f, -89f, 0f, 15f),
                    new("Service corner", 56.3f, -32f, 0f, 15f, P(2.3f, -60.3f), P(56.3f, -60.3f)),
                    new("Shop row", 23f, 110.3f, -90f, 12f, P(56.3f, 110.3f)),
                    new("Old Town finish", -39f, 110.3f, -90f, 12f),
                },
                new[]
                {
                    C(2.3f,-237), C(3.2f,-233), K(3.8f,-227), K(4.1f,-215), C(1.4f,-196),
                    K(2.3f,-155), B(1.6f,-138,-16), C(2.3f,-99), CurbLamp(0,-74),
                    C(15,-60.3f), K(25,-60.3f,70), C(35,-59.6f), B(44,-59.6f,75),
                    C(56.3f,-37), K(55.7f,-18), C(55.9f,-13), CurbLamp(54,2),
                    K(56.3f,57), B(55.2f,67,-15), C(55.2f,79), K(56.3f,92),
                    C(38,110.3f), K(30,110.3f,85), C(13,110.3f), K(-12,110.8f,95), W(-28,109.3f,105),
                }),
            new("sunset-marina", "Sunset Marina", "Build speed along the coast, then carry it through the turns into the marina.",
                P(272.3f, 127f), 0f, 170, 370, 570, 0,
                new ImpactGate[]
                {
                    new("Coast Highway", 272.3f, 286f, 0f, 15f),
                    new("Paloma approach", 178f, 412.3f, -90f, 12f, P(272.3f, 412.3f)),
                    new("Marina turn", 189f, 507.7f, 90f, 12f, P(164.3f, 412.3f), P(164.3f, 507.7f)),
                    new("Waterfront finish", 295f, 507.7f, 90f, 12f),
                },
                new[]
                {
                    C(272.3f,144), C(272.3f,149), K(273,158), B(273,172,10), C(272.3f,220),
                    K(272.3f,232), C(272,246), CurbLamp(270,279), K(273,335), C(272,345),
                    B(272.3f,359), K(272.3f,379), C(272.3f,391), C(251,412.3f),
                    K(241,412.3f,90), B(222,412.3f,90), C(209,412.3f), K(190,412.3f,70),
                    C(164.3f,438), K(163.9f,452), CurbLamp(162,478), C(164.3f,496),
                    C(199,507.7f), K(215,507.7f,90), B(237,507.7f,105), K(249,507.7f,90), W(282,507.7f,86),
                }),
            new("apex-motor-park", "Apex Motor Park", "Bank your score before the clock runs out. Use the circuit's outer lanes to reach the finish.",
                P(-267.7f, 341f), 0f, 160, 340, 540, 75,
                new ImpactGate[]
                {
                    new("Paddock road", -267.7f, 468f, 0f, 15f),
                    new("Circuit entry", -216f, 526f, 0f, 13f, P(-267.7f, 507.7f), P(-216f, 507.7f)),
                    new("Back straight", -182f, 571f, 0f, 12f, P(-216f, 531f), P(-182f, 531f)),
                    new("Circuit finish", -226f, 584f, -90f, 12f, P(-182f, 584f)),
                },
                new[]
                {
                    C(-267.7f,359), K(-267.7f,372), B(-266.5f,390), C(-267.7f,425),
                    K(-267.7f,436), CurbLamp(-270,451), C(-267.7f,492), C(-248,507.7f),
                    K(-236,507.7f,90), B(-226,507.7f,95), C(-216,521), C(-206,531),
                    K(-196,531,90), B(-190,531,110), K(-182,543), C(-182,550),
                    B(-182,560), K(-182,576), Lamp(-190,591.5f,90), K(-202,584,90), W(-216,584,86),
                }),
        };
    }

    public static class ImpactCareer
    {
        public const string TargetsKey = "CarCrash.CourseTargets";
        public static string ScoreKey(ImpactCourse course) => "CarCrash.CourseBest." + course.Id;
        public static string FinishKey(ImpactCourse course) => "CarCrash.CourseFinish." + course.Id;
        public static int Best(ImpactCourse course) => PlayerPrefs.GetInt(ScoreKey(course), 0);
        public static bool Finished(ImpactCourse course) => PlayerPrefs.GetInt(FinishKey(course), 0) == 1;
        public static bool Unlocked(int index) => index >= 0 && index < ImpactCourse.Catalog.Length &&
            (index == 0 || Finished(ImpactCourse.Catalog[index - 1]));
        public static bool GoldPaintUnlocked => Array.Exists(ImpactCourse.Catalog, course => Best(course) >= course.Gold);

        public static bool Record(ImpactCourse course, int score, int targets)
        {
            var newBest = score > Best(course);
            if (newBest) PlayerPrefs.SetInt(ScoreKey(course), score);
            PlayerPrefs.SetInt(FinishKey(course), 1);
            PlayerPrefs.SetInt(TargetsKey, PlayerPrefs.GetInt(TargetsKey, 0) + targets);
            PlayerPrefs.Save();
            return newBest;
        }
    }
}
