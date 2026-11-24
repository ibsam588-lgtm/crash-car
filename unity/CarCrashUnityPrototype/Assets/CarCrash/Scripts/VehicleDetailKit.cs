using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarCrash
{
    internal sealed partial class VehicleDetailKit : MonoBehaviour
    {
        private readonly List<Material> _materials = new();
        private readonly List<(Renderer renderer, int slot, Color baseGlow)> _brakes = new();
        private readonly List<Renderer> _reverse = new();
        private readonly List<(Renderer renderer, Color glow)> _patrolLamps = new();
        private MaterialPropertyBlock _lampBlock;
        private Transform _chassis;
        private ArcadeVehicleController _patrolController;
        private int _patrolPhase = -1;
        private bool? _braking, _reversing;

        public void Fit(Transform chassis, GarageVehicle spec)
        {
            _lampBlock = new MaterialPropertyBlock();
            _chassis = chassis;
            var modernAtlas = spec.id == "hauler" && FitModernAtlas();
            var rear = -spec.size.z * .5f;
            Bounds? lampBounds = null;
            foreach (var filter in GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (mesh == null || renderer == null) continue;
                var materials = renderer.sharedMaterials;
                var changedMaterials = false;
                for (var slot = 0; slot < Mathf.Min(mesh.subMeshCount, materials.Length); slot++)
                {
                    var material = materials[slot];
                    if (material == null || !material.name.Contains("Taillamp", StringComparison.OrdinalIgnoreCase) ||
                        !material.HasProperty("_EmissionColor")) continue;
                    var bounds = ChassisBounds(filter.transform, mesh.GetSubMesh(slot).bounds);
                    if (bounds.center.z > -spec.size.z * .24f || bounds.size.z > spec.size.z * .7f) continue;
                    if (lampBounds.HasValue)
                    {
                        var merged = lampBounds.Value;
                        merged.Encapsulate(bounds);
                        lampBounds = merged;
                    }
                    else lampBounds = bounds;
                    var glow = material.GetColor("_EmissionColor");
                    _brakes.Add((renderer, slot, glow.maxColorComponent > .04f ? glow : new Color(.58f,.022f,.01f)));
                    if (material.IsKeywordEnabled("_EMISSION")) continue;
                    var lit = new Material(material);
                    lit.EnableKeyword("_EMISSION");
                    _materials.Add(lit);
                    materials[slot] = lit;
                    changedMaterials = true;
                }
                if (changedMaterials) renderer.sharedMaterials = materials;
            }

            if (modernAtlas)
            {
                SetSignals(false, false);
                return;
            }

            var tail = lampBounds ?? new Bounds(new Vector3(0, spec.id == "monster" ? 1.85f : spec.size.y * .43f, rear),
                new Vector3(spec.size.x * .76f, .23f, .04f));
            var black = Paint("Textured lamp gasket", new Color(.038f,.044f,.048f), .25f, .31f);
            var clear = Paint("Clear reverse lamp lens", new Color(.35f,.39f,.41f), .18f, .76f, Color.black);
            var plate = Paint("Number plate enamel", new Color(.63f,.64f,.60f), .07f, .57f);
            var metal = Paint("Satin fittings", new Color(.29f,.34f,.37f), .72f, .51f);
            var reverseY = Mathf.Max(.37f, tail.center.y - .03f);
            var reverseZ = tail.min.z - .02f;
            var reverseX = Mathf.Max(.34f, tail.extents.x - .16f);
            foreach (var side in new[] {-1f, 1f})
            {
                var point = new Vector3(side * reverseX, reverseY, reverseZ);
                Part("Reverse lamp surround", point, new Vector3(.20f,.096f,.037f), black);
                _reverse.Add(Part("Reverse lamp lens", point + Vector3.back * .023f, new Vector3(.15f,.064f,.015f), clear));
            }

            if (spec.id is "hauler" or "monster")
            {
                var y = spec.id == "hauler" ? .73f : 1.62f;
                var z = Mathf.Min(rear, tail.min.z) - .028f;
                Part("Recessed licence plate surround", new Vector3(0,y,z), new Vector3(.52f,.20f,.045f), black);
                Part("Pressed licence plate", new Vector3(0,y,z-.028f), new Vector3(.448f,.148f,.012f), plate);
                Letter("EL 04 39", new Vector3(0,y,z-.04f), .108f, new Color(.075f,.096f,.11f));
                foreach (var side in new[] {-1f, 1f})
                    Part("Plate fastener", new Vector3(side*.205f,y+.049f,z-.037f), new Vector3(.024f,.020f,.009f), metal);
            }
            if (spec.id == "hauler") FitHauler(spec, rear, metal, black);
            if (spec.id == "monster") FitMonster(spec, metal, black);
            if (spec.id == "police") FitPolice(spec, black);
            SetSignals(false, false);
        }

        public void SetSignals(bool braking, bool reversing)
        {
            if (_braking != braking)
            {
                _braking = braking;
                foreach (var (renderer, slot, glow) in _brakes)
                {
                    if (renderer == null) continue;
                    renderer.GetPropertyBlock(_lampBlock, slot);
                    _lampBlock.SetColor("_EmissionColor", glow * (braking ? 3.15f : .65f));
                    renderer.SetPropertyBlock(_lampBlock, slot);
                }
            }
            if (_reversing != reversing)
            {
                _reversing = reversing;
                _lampBlock.Clear();
                _lampBlock.SetColor("_EmissionColor", reversing ? new Color(.84f,.94f,1f) * 2.2f : Color.black);
                foreach (var lamp in _reverse) lamp.SetPropertyBlock(_lampBlock);
            }
            if (_patrolController != null)
            {
                var phase = _patrolController.SpeedKmh > 3f ? Mathf.FloorToInt(Time.time * 4f) % 2 : 2;
                if (_patrolPhase == phase) return;
                _patrolPhase = phase;
                _lampBlock.Clear();
                for (var i = 0; i < _patrolLamps.Count; i++)
                {
                    var (renderer, glow) = _patrolLamps[i];
                    _lampBlock.SetColor("_EmissionColor", glow * (phase == i ? 2.05f : .13f));
                    renderer.SetPropertyBlock(_lampBlock);
                }
            }
        }

        private void FitHauler(GarageVehicle spec, float rear, Material metal, Material black)
        {
            var red = Paint("Reflective safety tape red", new Color(.56f,.032f,.016f), .12f, .52f, new Color(.19f,.015f,.004f));
            var white = Paint("Reflective safety tape white", new Color(.84f,.84f,.77f), .11f, .59f);
            var bumper = rear - .02f;
            for (var stripe = -4; stripe <= 4; stripe++)
                Part("Rear conspicuity tape", new Vector3(stripe * .229f,.89f,bumper-.015f), new Vector3(.223f,.083f,.015f),
                    stripe % 2 == 0 ? red : white);

            foreach (var side in new[] { -1f, 1f })
            {
                var x = side * spec.size.x * .41f;
                Part("Cargo latch support", new Vector3(x,1.80f,bumper+.10f), new Vector3(.08f,1.08f,.04f), black);
                Part("Cargo door latch rod", new Vector3(x,1.80f,bumper+.07f), new Vector3(.034f,1.17f,.036f), metal, PrimitiveType.Cylinder);
                Part("Cargo latch handle", new Vector3(x-side*.085f,1.40f,bumper+.035f), new Vector3(.19f,.047f,.041f), metal);
                foreach (var y in new[] { 1.23f, 2.40f })
                    Part("Cargo latch bracket", new Vector3(x,y,bumper+.065f), new Vector3(.13f,.075f,.051f), metal);
            }
        }

        private void FitMonster(GarageVehicle spec, Material metal, Material black)
        {
            var coating = Paint("Powder-coated recovery hardware", new Color(.47f,.035f,.012f), .54f, .53f);
            var alloys = Paint("Shock reservoir alloy", new Color(.41f,.45f,.48f), .87f, .67f);
            foreach (var side in new[] {-1f, 1f})
            {
                foreach (var axle in new[] {spec.wheelCenters[0].z, spec.wheelCenters[2].z})
                {
                    Part("Shock reservoir clamp", new Vector3(side*.78f,1.31f,axle+.21f), new Vector3(.21f,.10f,.22f), black);
                    Part("Suspension nitrogen reservoir", new Vector3(side*.82f,1.31f,axle+.19f), new Vector3(.132f,.245f,.132f), alloys, PrimitiveType.Cylinder);
                    Part("Reservoir red collar", new Vector3(side*.82f,1.48f,axle+.19f), new Vector3(.145f,.06f,.145f), coating, PrimitiveType.Cylinder);
                }
                Part("Rear tow shackle mount", new Vector3(side*.49f,1.36f,-2.60f), new Vector3(.23f,.13f,.25f), metal);
                Part("Rear recovery eye", new Vector3(side*.49f,1.26f,-2.72f), new Vector3(.11f,.16f,.12f), coating);
            }
            Part("Ribbed rear differential guard", new Vector3(0,.83f,spec.wheelCenters[2].z), new Vector3(.49f,.30f,.38f), black);
            for (var rib = -2; rib <= 2; rib++)
                Part("Differential cover cooling rib", new Vector3(rib*.079f,.835f,spec.wheelCenters[2].z-.201f), new Vector3(.025f,.22f,.036f), metal);
        }

        private void FitPolice(GarageVehicle spec, Material black)
        {
            _patrolController = _chassis.GetComponent<ArcadeVehicleController>();
            var ivory = Paint("Ivory patrol service graphics", new Color(.83f,.85f,.80f), .08f, .44f);
            var ink = new Color(.045f,.075f,.103f);
            var red = new Color(.92f,.06f,.02f);
            var blue = new Color(.04f,.38f,1f);
            var redLens = Paint("Patrol red silicon lens", red * .49f, .28f, .70f, red * .13f);
            var blueLens = Paint("Patrol blue silicon lens", blue * .49f, .28f, .70f, blue * .13f);
            var roof = new Vector3(0,spec.size.y + .035f,-.015f);
            Part("Patrol lightbar plinth", roof, new Vector3(.97f,.07f,.21f), black);
            _patrolLamps.Add((Part("Patrol red strobe bank", roof + new Vector3(-.24f,.043f,0), new Vector3(.44f,.057f,.17f), redLens), red));
            _patrolLamps.Add((Part("Patrol blue strobe bank", roof + new Vector3(.24f,.043f,0), new Vector3(.44f,.057f,.17f), blueLens), blue));

            var nose = spec.size.z * .5f;
            Part("Patrol push beam", new Vector3(0,.53f,nose + .045f), new Vector3(1.02f,.072f,.092f), black);
            foreach (var side in new[] { -1f, 1f })
            {
                Part("Patrol push beam upright", new Vector3(side * .38f,.60f,nose + .017f), new Vector3(.092f,.32f,.15f), black);
                var decal = new Vector3(side * spec.size.x * .481f,.78f,.18f);
                Part("Patrol door graphic", decal, new Vector3(.022f,.145f,.84f), ivory);
                Letter("ELYSIAN PD", decal + Vector3.right * side * .015f, .077f, ink, .68f, -90f * side);
            }
        }

        private Bounds ChassisBounds(Transform source, Bounds bounds)
        {
            var result = new Bounds(_chassis.InverseTransformPoint(source.TransformPoint(bounds.center)), Vector3.zero);
            for (var corner = 0; corner < 8; corner++)
            {
                var offset = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                result.Encapsulate(_chassis.InverseTransformPoint(source.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, offset))));
            }
            return result;
        }

        private Material Paint(string name, Color color, float metal, float smoothness, Color? glow = null)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Metallic", metal);
            material.SetFloat("_Glossiness", smoothness);
            if (glow.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", glow.Value);
            }
            _materials.Add(material);
            return material;
        }

        private Renderer Part(string name, Vector3 localPoint, Vector3 size, Material material, PrimitiveType kind = PrimitiveType.Cube)
        {
            var part = GameObject.CreatePrimitive(kind);
            part.name = name;
            part.GetComponent<Collider>().enabled = false;
            Destroy(part.GetComponent<Collider>());
            part.transform.SetPositionAndRotation(_chassis.TransformPoint(localPoint), _chassis.rotation);
            part.transform.localScale = size;
            part.transform.SetParent(transform, true);
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private void Letter(string value, Vector3 point, float height, Color tint, float width = .39f, float yaw = 0f)
        {
            var font = Resources.Load<Font>("Fonts/Inter-SemiBold");
            var label = new GameObject(value, typeof(TextMesh));
            var text = label.GetComponent<TextMesh>();
            text.font = font;
            text.text = value;
            text.fontSize = 48;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = tint;
            text.characterSize = .05f;
            var mesh = label.GetComponent<MeshRenderer>();
            var face = CitySignFont.CreateWorldMaterial(font, label);
            if (face != null)
            {
                _materials.Add(face);
                mesh.sharedMaterial = face;
            }
            label.transform.localScale *= Mathf.Min(width / Mathf.Max(.001f, mesh.bounds.size.x), height / Mathf.Max(.001f, mesh.bounds.size.y));
            label.transform.SetPositionAndRotation(_chassis.TransformPoint(point), _chassis.rotation * Quaternion.Euler(0f, yaw, 0f));
            label.transform.SetParent(transform, true);
        }

        private void OnDestroy()
        {
            foreach (var material in _materials) Destroy(material);
            foreach (var mesh in _atlasMeshes) Destroy(mesh);
        }
    }
}
