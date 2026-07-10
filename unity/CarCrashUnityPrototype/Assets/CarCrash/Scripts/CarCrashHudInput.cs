using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarCrash
{
    public sealed class CarCrashHudInput : MonoBehaviour
    {
        public ArcadeVehicleController vehicle;
        public CrashScoring scoring;

        private Texture2D _panel;
        private Texture2D _buttonCircle;
        private Texture2D _boostGlow;
        private Texture2D _gauge;
        private Texture2D _shade;
        private GUIStyle _smallLeft;
        private GUIStyle _metric;
        private GUIStyle _score;
        private GUIStyle _center;
        private GUIStyle _button;
        private bool _paused;
        private bool _runComplete;
        private float _finishAtRealtime = -1f;

        private void Awake()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
        }

        private void OnGUI()
        {
            if (vehicle == null)
            {
                return;
            }

            EnsureStyles();
            GUI.depth = -20;
            var width = Screen.width;
            var height = Screen.height;
            var scale = Mathf.Clamp(width / 576f, 0.72f, 1.38f);
            UpdateRunState();

            DrawScoreCard(scale);
            if (!_runComplete)
            {
                DrawPause(width, scale);
            }
            DrawImpactCallout(width, height, scale);
            if (_runComplete)
            {
                DrawResultOverlay(width, height, scale);
            }
            else
            {
                DrawControls(width, height, scale);
            }
        }

        private void UpdateRunState()
        {
            if (_runComplete || scoring == null || scoring.Hits <= 0)
            {
                return;
            }

            _finishAtRealtime = scoring.LastImpactRealtime + 2.15f;
            if (Time.realtimeSinceStartup < _finishAtRealtime)
            {
                return;
            }

            _runComplete = true;
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0.02f;
        }

        private void DrawResultOverlay(float width, float height, float scale)
        {
            GUI.DrawTexture(new Rect(0f, 0f, width, height), _shade, ScaleMode.StretchToFill, true);
            var panelWidth = Mathf.Min(width - 46f * scale, 430f * scale);
            var panelHeight = Mathf.Min(360f * scale, height * 0.74f);
            var panel = new Rect((width - panelWidth) * 0.5f, height * 0.22f, panelWidth, panelHeight);
            GUI.DrawTexture(panel, _panel, ScaleMode.StretchToFill, true);

            _center.fontSize = Mathf.RoundToInt(33f * scale);
            _center.normal.textColor = new Color(1f, 0.82f, 0.04f);
            GUI.Label(new Rect(panel.x + 18f * scale, panel.y + 30f * scale, panel.width - 36f * scale, 48f * scale), "WRECK COMPLETE", _center);

            _smallLeft.fontSize = Mathf.RoundToInt(17f * scale);
            _smallLeft.normal.textColor = new Color(0.75f, 0.78f, 0.80f);
            GUI.Label(new Rect(panel.x + 36f * scale, panel.y + 105f * scale, panel.width * 0.45f, 28f * scale), "TOTAL SCORE", _smallLeft);
            GUI.Label(new Rect(panel.x + 36f * scale, panel.y + 185f * scale, panel.width * 0.45f, 28f * scale), "VEHICLES HIT", _smallLeft);

            _score.fontSize = Mathf.RoundToInt(41f * scale);
            _score.normal.textColor = Color.white;
            GUI.Label(new Rect(panel.x + 36f * scale, panel.y + 130f * scale, panel.width - 72f * scale, 52f * scale), FormatScore(scoring.Score), _score);
            _metric.fontSize = Mathf.RoundToInt(32f * scale);
            _metric.normal.textColor = new Color(0.50f, 1f, 0.28f);
            GUI.Label(new Rect(panel.x + 36f * scale, panel.y + 210f * scale, panel.width - 72f * scale, 42f * scale), scoring.Hits.ToString(), _metric);

            var retry = new Rect(panel.x + 30f * scale, panel.y + panel.height - 79f * scale, panel.width - 60f * scale, 54f * scale);
            var oldColor = GUI.color;
            GUI.color = new Color(1f, 0.82f, 0.04f);
            GUI.DrawTexture(retry, _panel, ScaleMode.StretchToFill, true);
            GUI.color = oldColor;
            _button.fontSize = Mathf.RoundToInt(22f * scale);
            _button.normal.textColor = Color.black;
            GUI.Label(retry, "RETRY RUN", _button);
            if (GUI.Button(retry, string.Empty, GUIStyle.none))
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void DrawScoreCard(float scale)
        {
            var card = new Rect(22f * scale, 28f * scale, 158f * scale, 150f * scale);
            GUI.DrawTexture(card, _panel, ScaleMode.StretchToFill, true);

            _smallLeft.fontSize = Mathf.RoundToInt(16f * scale);
            _smallLeft.normal.textColor = Color.white;
            GUI.Label(new Rect(card.x + 15f * scale, card.y + 12f * scale, 100f * scale, 24f * scale), "SCORE", _smallLeft);

            _score.fontSize = Mathf.RoundToInt(32f * scale);
            _score.normal.textColor = new Color(1f, 0.82f, 0.04f);
            GUI.Label(new Rect(card.x + 15f * scale, card.y + 34f * scale, card.width - 25f * scale, 42f * scale), FormatScore(scoring != null ? scoring.Score : 0), _score);

            _smallLeft.fontSize = Mathf.RoundToInt(16f * scale);
            _smallLeft.normal.textColor = Color.white;
            GUI.Label(new Rect(card.x + 15f * scale, card.y + 82f * scale, 70f * scale, 22f * scale), "HITS", _smallLeft);

            _metric.fontSize = Mathf.RoundToInt(31f * scale);
            _metric.normal.textColor = new Color(0.50f, 1f, 0.28f);
            GUI.Label(new Rect(card.x + 15f * scale, card.y + 101f * scale, 65f * scale, 38f * scale), $"{(scoring != null ? scoring.Hits : 0)}", _metric);

            _score.fontSize = Mathf.RoundToInt(29f * scale);
            _score.alignment = TextAnchor.MiddleRight;
            _score.normal.textColor = new Color(1f, 0.82f, 0.04f);
            GUI.Label(new Rect(card.x + 88f * scale, card.y + 95f * scale, 54f * scale, 42f * scale), $"x{ComboValue()}", _score);
            _score.alignment = TextAnchor.MiddleLeft;
        }

        private void DrawPause(float width, float scale)
        {
            var size = 58f * scale;
            var rect = new Rect(width - 24f * scale - size, 30f * scale, size, size);
            GUI.DrawTexture(rect, _panel, ScaleMode.StretchToFill, true);
            _center.fontSize = Mathf.RoundToInt(29f * scale);
            _center.normal.textColor = Color.white;
            GUI.Label(rect, _paused ? ">" : "II", _center);

            if (GUI.Button(rect, string.Empty, GUIStyle.none))
            {
                _paused = !_paused;
                Time.timeScale = _paused ? 0f : 1f;
                Time.fixedDeltaTime = _paused ? 0.02f : 0.02f;
            }
        }

        private void DrawImpactCallout(float width, float height, float scale)
        {
            if (scoring == null || Time.realtimeSinceStartup - scoring.LastImpactRealtime > 1.55f)
            {
                return;
            }

            var centerY = height * 0.15f;
            _center.fontSize = Mathf.RoundToInt(51f * scale);
            _center.normal.textColor = new Color(1f, 0.80f, 0.03f);
            DrawShadowLabel(new Rect(width * 0.5f - 115f * scale, centerY, 230f * scale, 62f * scale), $"x{ComboValue()}", _center, 2f * scale);

            _center.fontSize = Mathf.RoundToInt(23f * scale);
            _center.normal.textColor = Color.white;
            DrawShadowLabel(new Rect(width * 0.5f - 115f * scale, centerY + 52f * scale, 230f * scale, 32f * scale), "SMASH!", _center, 2f * scale);

            _center.fontSize = Mathf.RoundToInt(33f * scale);
            _center.normal.textColor = new Color(1f, 0.80f, 0.03f);
            DrawShadowLabel(new Rect(width * 0.5f - 130f * scale, centerY + 78f * scale, 260f * scale, 42f * scale), $"+{scoring.LastImpactScore:N0}", _center, 2f * scale);
        }

        private void DrawControls(float width, float height, float scale)
        {
            var buttonSize = 96f * scale;
            var bottom = 26f * scale;
            var left = new Rect(28f * scale, height - bottom - buttonSize, buttonSize, buttonSize);
            var boostSize = 108f * scale;
            var boost = new Rect(width * 0.5f - boostSize * 0.5f, height - bottom - boostSize, boostSize, boostSize);
            var gaugeSize = 148f * scale;
            var gauge = new Rect(width - 22f * scale - gaugeSize, height - 18f * scale - gaugeSize, gaugeSize, gaugeSize);

            GUI.DrawTexture(left, _buttonCircle, ScaleMode.StretchToFill, true);
            _button.fontSize = Mathf.RoundToInt(48f * scale);
            _button.normal.textColor = Color.white;
            GUI.Label(left, "<", _button);

            var steer = Input.GetAxisRaw("Horizontal");
            if (GUI.RepeatButton(left, string.Empty, GUIStyle.none))
            {
                steer = -1f;
            }

            var rightSteerZone = new Rect(width * 0.56f, height * 0.40f, width * 0.42f, height * 0.30f);
            if (GUI.RepeatButton(rightSteerZone, string.Empty, GUIStyle.none))
            {
                steer = 1f;
            }
            var leftSteerZone = new Rect(width * 0.02f, height * 0.40f, width * 0.40f, height * 0.30f);
            if (GUI.RepeatButton(leftSteerZone, string.Empty, GUIStyle.none))
            {
                steer = -1f;
            }

            var boostPulse = 1f + Mathf.Sin(Time.unscaledTime * 5.8f) * 0.035f;
            var glowRect = ScaleAroundCenter(boost, boostPulse);
            GUI.DrawTexture(glowRect, _boostGlow, ScaleMode.StretchToFill, true);
            _button.fontSize = Mathf.RoundToInt(48f * scale);
            _button.normal.textColor = new Color(0.22f, 0.82f, 1f);
            GUI.Label(boost, "!", _button);
            if (GUI.Button(boost, string.Empty, GUIStyle.none))
            {
                vehicle.Boost(1.55f);
            }

            GUI.DrawTexture(gauge, _gauge, ScaleMode.StretchToFill, true);
            _center.fontSize = Mathf.RoundToInt(43f * scale);
            _center.normal.textColor = Color.white;
            GUI.Label(new Rect(gauge.x, gauge.y + 35f * scale, gauge.width, 55f * scale), $"{vehicle.SpeedKmh:0}", _center);
            _center.fontSize = Mathf.RoundToInt(16f * scale);
            GUI.Label(new Rect(gauge.x, gauge.y + 84f * scale, gauge.width, 25f * scale), "KM/H", _center);
            _center.fontSize = Mathf.RoundToInt(20f * scale);
            GUI.Label(new Rect(gauge.x, gauge.y + 108f * scale, gauge.width, 24f * scale), GearValue().ToString(), _center);

            if (!_paused)
            {
                vehicle.SetMobileInput(Mathf.Clamp(steer, -1f, 1f), 1f, 0f);
            }
        }

        private void EnsureStyles()
        {
            if (_panel != null)
            {
                return;
            }

            _panel = MakeRoundedPanel(96, new Color(0.015f, 0.018f, 0.021f, 0.72f), 17f);
            _buttonCircle = MakeCircleTexture(192, new Color(0.015f, 0.018f, 0.022f, 0.67f), new Color(0.86f, 0.66f, 0.27f, 0.76f), 5f);
            _boostGlow = MakeCircleTexture(192, new Color(0.02f, 0.12f, 0.21f, 0.80f), new Color(0.14f, 0.74f, 1f, 1f), 8f);
            _gauge = MakeGaugeTexture(256);
            _shade = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _shade.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.76f));
            _shade.Apply();

            _smallLeft = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip
            };
            _metric = new GUIStyle(_smallLeft) { fontStyle = FontStyle.Bold };
            _score = new GUIStyle(_smallLeft) { fontStyle = FontStyle.Bold };
            _center = new GUIStyle(_smallLeft)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            _button = new GUIStyle(_center) { fontStyle = FontStyle.Bold };
        }

        private static void DrawShadowLabel(Rect rect, string text, GUIStyle style, float offset)
        {
            var color = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.72f);
            GUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, style);
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        private static Rect ScaleAroundCenter(Rect rect, float scale)
        {
            var width = rect.width * scale;
            var height = rect.height * scale;
            return new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
        }

        private int ComboValue()
        {
            return Mathf.Clamp(1 + (scoring != null ? scoring.Hits / 2 : 0), 1, 9);
        }

        private int GearValue()
        {
            return Mathf.Clamp(Mathf.FloorToInt(vehicle.SpeedKmh / 42f) + 1, 1, 7);
        }

        private static string FormatScore(int value)
        {
            return value.ToString("N0");
        }

        private static Texture2D MakeRoundedPanel(int size, Color color, float radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(radius - x, 0f, x - (size - radius));
                    var dy = Mathf.Max(radius - y, 0f, y - (size - radius));
                    var inside = dx * dx + dy * dy <= radius * radius;
                    texture.SetPixel(x, y, inside ? color : Color.clear);
                }
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D MakeCircleTexture(int size, Color fill, Color ring, float ringWidth)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;
            var outer = center - 2f;
            var inner = outer - ringWidth;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    texture.SetPixel(x, y, distance <= inner ? fill : distance <= outer ? ring : Color.clear);
                }
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D MakeGaugeTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var outer = size * 0.48f;
            var inner = size * 0.39f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var delta = new Vector2(x, y) - center;
                    var distance = delta.magnitude;
                    if (distance > outer)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    if (distance < inner)
                    {
                        texture.SetPixel(x, y, new Color(0.012f, 0.014f, 0.018f, 0.78f));
                        continue;
                    }
                    var angle = Mathf.Repeat(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 360f, 360f);
                    var active = angle >= 120f || angle <= 48f;
                    if (!active)
                    {
                        texture.SetPixel(x, y, new Color(0.38f, 0.39f, 0.38f, 0.58f));
                    }
                    else if (angle > 120f && angle < 305f)
                    {
                        texture.SetPixel(x, y, new Color(0.98f, 0.88f, 0.06f, 1f));
                    }
                    else
                    {
                        texture.SetPixel(x, y, new Color(0.92f, 0.16f, 0.04f, 1f));
                    }
                }
            }
            texture.Apply();
            return texture;
        }
    }
}
