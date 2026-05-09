using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EDMQuiz
{
    /// <summary>世界空間に常時表示する Light2D 受光対応の背景スプライト。
    /// UI Toolkit overlay より奥のソートで描画することで、
    /// AnswerSpotlight / MovingSpotlightController の Light2D が背景まで届くようにする。</summary>
    /// <remarks>
    /// UI Toolkit (Screen Space Overlay) は Light2D の影響を受けないため、
    /// UXML の .stage-background による背景画像は廃止し、世界空間スプライトに置き換える。
    /// シーンに空 GameObject を 1 つ用意してこのコンポーネントを Attach し、
    /// _backgroundSprite に背景画像 (Sprite) を割り当てるだけで動作する。
    /// </remarks>
    [DisallowMultipleComponent]
    public class WorldStageBackground : MonoBehaviour
    {
        [Header("背景")]
        [SerializeField] private Sprite _backgroundSprite;
        [SerializeField] private Color  _tint = Color.white;

        [Header("レンダリング")]
        [SerializeField] private int    _sortingOrder       = -200;
        [SerializeField] private float  _distanceFromCamera = 12f;
        [SerializeField] private float  _coverageMargin     = 1.05f;

        [Header("Global Light 2D (環境光)")]
        [Tooltip("Sprite-Lit シェーダ受光のためのアンビエントライト。0 にすると Light2D が当たらないとき真っ黒になる。")]
        [SerializeField] private bool  _spawnGlobalLight    = true;
        [SerializeField] private Color _globalLightColor    = Color.white;
        [SerializeField] private float _globalLightIntensity = 0.45f;

        private SpriteRenderer _renderer;
        private Light2D _globalLight;

        /// <summary>動的生成された BackgroundSprite の Transform。
        /// MovingSpotlightController が Spotlight_* の親として参照する。
        /// 未生成の場合は null を返す。</summary>
        public Transform BackgroundTransform => _renderer != null ? _renderer.transform : null;

        void Awake()
        {
            BuildBackground();
            BuildGlobalLight();
        }

        private void BuildBackground()
        {
            if (_backgroundSprite == null)
            {
                Debug.LogWarning("[WorldStageBackground] _backgroundSprite が未設定。背景は描画されません。", this);
                return;
            }

            var go = new GameObject("BackgroundSprite");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(0f, 0f, _distanceFromCamera);

            _renderer = go.AddComponent<SpriteRenderer>();
            _renderer.sprite       = _backgroundSprite;
            _renderer.color        = _tint;
            _renderer.sortingOrder = _sortingOrder;

            var litShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (litShader != null)
            {
                _renderer.sharedMaterial = new Material(litShader);
            }
            else
            {
                Debug.LogWarning("[WorldStageBackground] Sprite-Lit-Default シェーダが見つかりません。Light2D 影響を受けません。", this);
            }

            FitToCamera(go.transform);
        }

        private void BuildGlobalLight()
        {
            if (!_spawnGlobalLight) return;

            var go = new GameObject("AmbientGlobalLight");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;

            _globalLight = go.AddComponent<Light2D>();
            _globalLight.lightType       = Light2D.LightType.Global;
            _globalLight.color           = _globalLightColor;
            _globalLight.intensity       = _globalLightIntensity;
            _globalLight.shadowsEnabled  = false;
        }

        private void FitToCamera(Transform t)
        {
            var cam = Camera.main;
            if (cam == null) return;

            float visibleHeight, visibleWidth;
            if (cam.orthographic)
            {
                visibleHeight = cam.orthographicSize * 2f;
                visibleWidth  = visibleHeight * cam.aspect;
            }
            else
            {
                visibleHeight = 2f * _distanceFromCamera * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                visibleWidth  = visibleHeight * cam.aspect;
            }

            var spriteSize = _backgroundSprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

            float scaleX = visibleWidth  / spriteSize.x;
            float scaleY = visibleHeight / spriteSize.y;
            float scale  = Mathf.Max(scaleX, scaleY) * _coverageMargin;

            t.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
