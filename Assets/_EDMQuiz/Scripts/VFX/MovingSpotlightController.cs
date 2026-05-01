using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EDMQuiz
{
    /// <summary>正解時の Light2D ムービングスポットライト群を管理する。
    /// 動的に Light2D × N 灯と被照射用の DanceFloor SpriteRenderer を生成し、
    /// 8 の字軌道で動かしビート毎に色をパレットローテートする。</summary>
    public class MovingSpotlightController : MonoBehaviour
    {
        [Header("DanceFloor 背景 sprite (動的生成可)")]
        [SerializeField] private bool _autoGenerateFloor = true;
        [SerializeField] private Color _floorColor = new Color(0.4f, 0.4f, 0.5f, 1f);
        [SerializeField] private Vector2 _floorSize = new Vector2(40f, 24f);

        [Header("EDM 4 色パレット (マゼンタ / シアン / イエロー / ライム)")]
        [SerializeField] private Color[] _palette = new[]
        {
            new Color(1.0f, 0.2f, 0.8f, 1f), // マゼンタ
            new Color(0.2f, 0.9f, 1.0f, 1f), // シアン
            new Color(1.0f, 1.0f, 0.2f, 1f), // イエロー
            new Color(0.4f, 1.0f, 0.3f, 1f), // ライム
        };

        private Light2D[] _lights;
        private SpriteRenderer _floor;
        private bool _active;
        private float _t;

        void Awake()
        {
            BuildFloor();
            BuildLights();
            Deactivate(); // 初期は OFF
        }

        private void BuildFloor()
        {
            if (!_autoGenerateFloor) return;
            var go = new GameObject("DanceFloor");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(0f, 0f, 1f); // Camera 寄りでなく後方

            _floor = go.AddComponent<SpriteRenderer>();
            // 1x1 白テクスチャ → スプライト化
            var tex = new Texture2D(2, 2);
            var pixels = new Color[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
            _floor.sprite = sprite;
            var litShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (litShader != null)
                _floor.sharedMaterial = new Material(litShader);
            _floor.color = _floorColor;
            _floor.transform.localScale = new Vector3(_floorSize.x, _floorSize.y, 1f);
            _floor.sortingOrder = -100;
        }

        private void BuildLights()
        {
            _lights = new Light2D[GameConstants.SPOTLIGHT_COUNT];
            for (int i = 0; i < _lights.Length; i++)
            {
                var go = new GameObject($"Spotlight_{i}");
                go.transform.SetParent(transform, worldPositionStays: false);

                var l = go.AddComponent<Light2D>();
                l.lightType = Light2D.LightType.Point;
                l.color = GetPaletteColor(i);
                l.intensity = GameConstants.SPOTLIGHT_INTENSITY;
                l.pointLightInnerRadius = GameConstants.SPOTLIGHT_INNER_RADIUS;
                l.pointLightOuterRadius = GameConstants.SPOTLIGHT_OUTER_RADIUS;
                l.falloffIntensity = GameConstants.SPOTLIGHT_FALLOFF;
                l.shadowsEnabled = false;

                _lights[i] = l;
            }
        }

        public void Activate()
        {
            if (_lights == null) BuildLights();
            _active = true;
            _t = 0f;
            if (_floor != null) _floor.enabled = true;
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] == null) continue;
                _lights[i].enabled = true;
                _lights[i].intensity = GameConstants.SPOTLIGHT_INTENSITY;
            }
        }

        public void Deactivate()
        {
            _active = false;
            if (_floor != null) _floor.enabled = false;
            if (_lights == null) return;
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] == null) continue;
                _lights[i].intensity = 0f;
                _lights[i].enabled = false;
            }
        }

        void Update()
        {
            if (!_active || _lights == null) return;
            _t += Time.deltaTime;

            for (int i = 0; i < _lights.Length; i++)
            {
                var l = _lights[i];
                if (l == null) continue;
                l.transform.localPosition = MovingSpotlightMotion.GetFigureEightPosition(
                    _t,
                    i,
                    _lights.Length,
                    GameConstants.GetBeatDuration(),
                    GameConstants.BEATS_PER_BAR,
                    GameConstants.SPOTLIGHT_BARS_PER_LAP,
                    GameConstants.SPOTLIGHT_FIGURE8_SCALE);
            }
        }

        /// <summary>BpmClock.OnBeat と同期して呼ばれる。色をパレットローテート。</summary>
        public void OnBeatTick(int beatIndex)
        {
            if (_lights == null) return;
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] == null) continue;
                _lights[i].color = GetPaletteColor(MovingSpotlightMotion.GetPaletteIndex(beatIndex, i, _palette.Length));
            }
        }

        private Color GetPaletteColor(int index)
        {
            if (_palette == null || _palette.Length == 0) return Color.white;
            return _palette[MovingSpotlightMotion.GetPaletteIndex(0, index, _palette.Length)];
        }
    }
}
