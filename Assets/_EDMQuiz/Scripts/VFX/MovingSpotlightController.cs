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
        // Light2D スポットライトの受光面。半透明にして WorldStageBackground (EDM ステージ画像) を
        // 透けさせつつ、暗い灰色面に Light2D が当たった部分だけ色がクッキリ浮かぶ二段構成。
        [SerializeField] private bool _autoGenerateFloor = true;
        [SerializeField] private Color _floorColor = new Color(0.28f, 0.25f, 0.35f, 0.45f);
        [SerializeField] private Vector2 _floorSize = new Vector2(40f, 24f);

        [Header("Spotlight 親オーバーライド (空なら WorldStageBackground/BackgroundSprite を自動探索)")]
        [SerializeField] private Transform _spotlightParentOverride;

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
        private bool _hype;
        private float _t;

        void Awake()
        {
            BuildFloor();
            // Spotlights は WorldStageBackground.BackgroundSprite を親にしたいが、
            // BackgroundSprite は WorldStageBackground.Awake() で動的生成されるため、
            // Awake 順序が不定。最初の Activate() 呼び出し (OnEnable 以降) で lazy 構築する。
            Deactivate(); // 初期は OFF
        }

        /// <summary>Spotlight の親 Transform を解決する。
        /// Inspector で `_spotlightParentOverride` が指定されていればそれを優先、
        /// なければシーンの WorldStageBackground/BackgroundSprite を探す。
        /// 見つからない場合は自身 (従来挙動) にフォールバック。</summary>
        private Transform ResolveSpotlightParent()
        {
            if (_spotlightParentOverride != null) return _spotlightParentOverride;
            var stage = FindAnyObjectByType<WorldStageBackground>();
            return stage != null && stage.BackgroundTransform != null
                ? stage.BackgroundTransform
                : transform;
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
            var parent = ResolveSpotlightParent();
            _lights = new Light2D[GameConstants.SPOTLIGHT_COUNT];
            for (int i = 0; i < _lights.Length; i++)
            {
                var go = new GameObject($"Spotlight_{i}");
                go.transform.SetParent(parent, worldPositionStays: false);

                var l = go.AddComponent<Light2D>();
                l.lightType = Light2D.LightType.Point;
                l.blendStyleIndex = 1; // Renderer2D.asset の "Additive"。Multiply 既定だと暗い受光面に光が乗らない
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
            _hype = false;
            _t = 0f;
            if (_floor != null) _floor.enabled = true;
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] == null) continue;
                _lights[i].enabled = true;
                _lights[i].intensity = GameConstants.SPOTLIGHT_INTENSITY;
            }
        }

        /// <summary>正解時の激しい演出モード。
        /// サイン波で光量を 0.8↔2.5 に振動させ、軌道は高速化した8の字を全体回転させつつ
        /// 左右にヨーヨーのように振る。</summary>
        public void ActivateHype()
        {
            if (_lights == null) BuildLights();
            _active = true;
            _hype = true;
            _t = 0f;
            if (_floor != null) _floor.enabled = true;
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] == null) continue;
                _lights[i].enabled = true;
                _lights[i].intensity = GameConstants.SPOTLIGHT_HYPE_INTENSITY_MIN;
            }
        }

        public void Deactivate()
        {
            _active = false;
            _hype = false;
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

            float hypeIntensity = _hype
                ? MovingSpotlightMotion.GetHypeIntensity(
                    _t,
                    GameConstants.SPOTLIGHT_HYPE_INTENSITY_MIN,
                    GameConstants.SPOTLIGHT_HYPE_INTENSITY_MAX,
                    GameConstants.SPOTLIGHT_HYPE_PULSE_HZ)
                : 0f;

            for (int i = 0; i < _lights.Length; i++)
            {
                var l = _lights[i];
                if (l == null) continue;

                if (_hype)
                {
                    l.transform.localPosition = MovingSpotlightMotion.GetHypePosition(
                        _t,
                        i,
                        _lights.Length,
                        GameConstants.GetBeatDuration(),
                        GameConstants.BEATS_PER_BAR,
                        GameConstants.SPOTLIGHT_HYPE_BARS_PER_LAP,
                        GameConstants.SPOTLIGHT_HYPE_FIGURE8_SCALE,
                        GameConstants.SPOTLIGHT_HYPE_ROTATE_HZ,
                        GameConstants.SPOTLIGHT_HYPE_YOYO_AMPLITUDE,
                        GameConstants.SPOTLIGHT_HYPE_YOYO_HZ);
                    l.intensity = hypeIntensity;
                }
                else
                {
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
