using System.Collections.Generic;
using UnityEngine;

namespace CarCrash
{
    public enum GarageUpgrade { Wheels, RimFinish, Sidewalls, Headlights, AuxiliaryLights, PaintFinish, BodyKit, WindowTint }

    internal readonly struct HeadlightProfile
    {
        public readonly Color Color;
        public readonly float Range, Angle, Intensity;

        public HeadlightProfile(Color color, float range, float angle, float intensity)
        {
            Color = color;
            Range = range;
            Angle = angle;
            Intensity = intensity;
        }
    }

    public static class GarageCustomization
    {
        private static readonly string[] Wheels = { "Factory", "Split spoke", "Turbine", "Beadlock" };
        private static readonly string[] RimFinishes = { "Factory", "Graphite", "Brushed alloy", "Bronze" };
        private static readonly string[] Sidewalls = { "Factory", "Whitewall", "Coral pinstripe" };
        private static readonly string[] Headlights = { "Factory", "Warm halogen", "Ice white LED", "Rally beam" };
        private static readonly string[] AuxiliaryLights = { "None", "Fog pods", "LED bar" };
        private static readonly string[] PaintFinishes = { "Gloss", "Satin", "Matte" };
        private static readonly string[] Windows = { "Factory", "Clear", "Smoke", "Sea glass" };
        private static readonly string[] StreetKits = { "Factory", "Front splitter", "Rear lip" };
        private static readonly string[] UtilityKits = { "Factory", "Brush guard", "Recovery kit" };
        private static readonly string[] HaulerKits = { "Factory", "Bumper guard", "Recovery kit" };

        internal static readonly Color[] RimColors =
        {
            new(.65f, .68f, .67f), new(.13f, .15f, .16f), new(.71f, .74f, .76f), new(.63f, .36f, .17f)
        };

        internal static readonly HeadlightProfile[] Lamps =
        {
            new(new Color(.87f, .94f, 1f), 48f, 56f, 1f),
            new(new Color(1f, .83f, .57f), 41f, 64f, .94f),
            new(new Color(.81f, .94f, 1f), 54f, 52f, 1.06f),
            new(new Color(1f, .98f, .87f), 61f, 48f, 1.13f)
        };

        internal static readonly Color[] WindowColors =
        {
            Color.white, new(.77f, .90f, .94f, .12f), new(.34f, .40f, .44f, .34f), new(.46f, .76f, .73f, .20f)
        };

        internal static readonly float[] BodyGloss = { .76f, .45f, .22f };
        internal static readonly float[] BodyMetallic = { .38f, .25f, .12f };

        public static string UpgradeKey(string vehicleId, GarageUpgrade upgrade) => $"CarCrash.Garage.{vehicleId}.{upgrade}";
        public static string PaintKey(string vehicleId) => $"CarCrash.Garage.{vehicleId}.Paint";

        public static string Label(GarageUpgrade upgrade) => upgrade switch
        {
            GarageUpgrade.Wheels => "Wheel style",
            GarageUpgrade.RimFinish => "Rim finish",
            GarageUpgrade.Sidewalls => "Sidewalls",
            GarageUpgrade.Headlights => "Headlights",
            GarageUpgrade.AuxiliaryLights => "Auxiliary lights",
            GarageUpgrade.PaintFinish => "Paint finish",
            GarageUpgrade.BodyKit => "Body kit",
            GarageUpgrade.WindowTint => "Window tint",
            _ => ""
        };

        public static IReadOnlyList<string> Options(string vehicleId, GarageUpgrade upgrade) => upgrade switch
        {
            GarageUpgrade.Wheels => Wheels,
            GarageUpgrade.RimFinish => RimFinishes,
            GarageUpgrade.Sidewalls => Sidewalls,
            GarageUpgrade.Headlights => Headlights,
            GarageUpgrade.AuxiliaryLights => AuxiliaryLights,
            GarageUpgrade.PaintFinish => PaintFinishes,
            GarageUpgrade.WindowTint => Windows,
            GarageUpgrade.BodyKit when vehicleId == "hauler" => HaulerKits,
            GarageUpgrade.BodyKit when IsUtility(vehicleId) => UtilityKits,
            GarageUpgrade.BodyKit => StreetKits,
            _ => System.Array.Empty<string>()
        };

        internal static bool IsUtility(string vehicleId) => vehicleId is "pickup" or "monster" or "suv" or "hauler" or "bulwark";
    }
}
