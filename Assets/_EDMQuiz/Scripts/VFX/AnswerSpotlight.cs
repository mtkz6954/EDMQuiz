using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EDMQuiz
{
    /// <summary>AnswerWindow フェーズ中だけ点灯する回答エリア用スポットライト (Light2D)。
    /// 被照射用のソフト円スプライトを自動生成して回答エリアを照らす。</summary>
    /// <remarks>
    /// UI Toolkit (UIDocument / Screen Space Overlay) は Light2D の影響を受けないため、
    /// UI 越しではなく「世界側に置いたステージスプライト」を Light2D で照らす方式。
    /// シーンに空 GameObject を 1 つ用意してこのコンポーネントを Attach するだけで動作する。
    /// </remarks>
    public class AnswerSpotlight : MonoBehaviour
    {
        [Header("Light2D 設定")]
        [SerializeField] private float _targetIntensity = 1.8f;
        [SerializeField] private float _outerRadius     = 6f;
        [SerializeField] private float _innerRadius     = 1f;
        [SerializeField] private float _falloff         = 0.7f;
        [SerializeField] private Color _color           = new Color(1f, 0.95f, 0.85f, 1f);
        [SerializeField] private float _fadeInSec       = 0.15f;
        [SerializeField] private float _fadeOutSec      = 0.4f;

        [Header("被照射ステージ (自動生成)")]
        [SerializeField] private bool    _autoGenerateStage = true;
        [SerializeField] private Color   _stageColor        = new Color(0.35f, 0.35f, 0.4f, 1f);
        [SerializeField] private Vector2 _stageSize         = new Vector2(28f, 16f);
        [SerializeField] private int     _stageSortingOrder = -50;

        private Light2D _spotlight;
        private SpriteRenderer _stage;
        private Tween _intensityTween;
        private Tween _beatPulseTween;

        void Awake()
        {
            BuildStage();
            BuildLight();
            // 初期は OFF
            if (_spotlight != null)
            {
                _spotlight.intensity = 0f;
                _spotlight.enabled = false;
            }
            if (_stage != null) _stage.enabled = false;
        }

        void Start()
        {
            GameFlowManager.OnPhaseChanged
                .Subscribe(OnPhaseChanged)
                .AddTo(this);

            BpmClock.OnBeat
                .Where(_ => GameFlowManager.Instance != null
                         && GameFlowManager.Instance.CurrentPhase == GamePhase.AnswerWindow)
                .Subscribe(_ => PulseOnBeat())
                .AddTo(this);
        }

        void OnDisable()
        {
            _intensityTween?.Kill();
            _beatPulseTween?.Kill();
            _intensityTween = null;
            _beatPulseTween = null;
        }

        private void BuildStage()
        {
            if (!_autoGenerateStage) return;
            var go = new GameObject("AnswerStage");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(0f, 0f, 1f);

            _stage = go.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(2, 2);
            var pixels = new[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
            _stage.sprite = sprite;

            var litShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (litShader != null)
                _stage.sharedMaterial = new Material(litShader);
            _stage.color = _stageColor;
            _stage.transform.localScale = new Vector3(_stageSize.x, _stageSize.y, 1f);
            _stage.sortingOrder = _stageSortingOrder;
        }

        private void BuildLight()
        {
            var go = new GameObject("AnswerSpotlightLight");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;

            _spotlight = go.AddComponent<Light2D>();
            _spotlight.lightType            = Light2D.LightType.Point;
            _spotlight.blendStyleIndex      = 1; // Renderer2D.asset の "Additive"。Multiply 既定だと暗い受光面に光が乗らない
            _spotlight.color                = _color;
            _spotlight.intensity            = 0f;
            _spotlight.pointLightInnerRadius = _innerRadius;
            _spotlight.pointLightOuterRadius = _outerRadius;
            _spotlight.falloffIntensity      = _falloff;
            _spotlight.shadowsEnabled        = false;
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (_spotlight == null) return;

            if (phase == GamePhase.AnswerWindow)
            {
                if (_stage != null) _stage.enabled = true;
                _spotlight.enabled = true;
                _intensityTween?.Kill();
                _intensityTween = DOVirtual
                    .Float(_spotlight.intensity, _targetIntensity, _fadeInSec,
                        v => _spotlight.intensity = v)
                    .SetUpdate(true);
            }
            else if (phase == GamePhase.Drop || phase == GamePhase.FadeOut
                  || phase == GamePhase.Next || phase == GamePhase.Question
                  || phase == GamePhase.GameEnd || phase == GamePhase.Idle)
            {
                if (!_spotlight.enabled) return;
                _intensityTween?.Kill();
                _intensityTween = DOVirtual
                    .Float(_spotlight.intensity, 0f, _fadeOutSec,
                        v => _spotlight.intensity = v)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        _spotlight.enabled = false;
                        if (_stage != null) _stage.enabled = false;
                    });
            }
        }

        private void PulseOnBeat()
        {
            if (_spotlight == null || !_spotlight.enabled) return;

            _beatPulseTween?.Kill();
            _beatPulseTween = DOTween.Sequence()
                .Append(DOVirtual.Float(
                    _spotlight.intensity,
                    _targetIntensity * 1.35f,
                    GameConstants.GetBeatDuration() * 0.18f,
                    v => _spotlight.intensity = v))
                .Append(DOVirtual.Float(
                    _targetIntensity * 1.35f,
                    _targetIntensity,
                    GameConstants.GetBeatDuration() * 0.32f,
                    v => _spotlight.intensity = v))
                .SetUpdate(true);
        }
    }
}
