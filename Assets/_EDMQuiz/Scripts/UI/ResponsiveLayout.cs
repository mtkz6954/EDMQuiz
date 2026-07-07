using UnityEngine.UIElements;

namespace EDMQuiz
{
    /// <summary>
    /// UI Toolkit の root に対して、ビューポートのアスペクト比に応じた
    /// .layout-portrait / .layout-landscape クラスと、タッチ主体デバイス向けの
    /// .layout-touch クラスを付与するヘルパー。
    ///
    /// USS 側で `.layout-portrait .hiragana-buttons { ... }` のように
    /// セレクタを書くことで、レイアウトを切り替えられる。
    ///
    /// また PanelSettings を渡すと、参照解像度を画面の向きで切り替える:
    /// - 縦画面: 720x1280（スマホ縦デザインの参照空間）
    /// - 横画面: 1200x800（5月時点の PC 横型デザインの参照空間）
    /// どちらも幅基準 (match=0) なので、縦長スマホ (19.5:9 等) でも
    /// 参照幅が必ず画面内に収まり、横方向のはみ出しを防ぐ。
    /// </summary>
    public static class ResponsiveLayout
    {
        public const string PortraitClass  = "layout-portrait";
        public const string LandscapeClass = "layout-landscape";
        public const string TouchClass     = "layout-touch";

        /// <summary>縦画面の参照解像度（スマホ縦デザイン基準）</summary>
        private static readonly UnityEngine.Vector2Int PortraitReference  = new(720, 1280);
        /// <summary>横画面の参照解像度（旧 PC 横型デザイン基準）</summary>
        private static readonly UnityEngine.Vector2Int LandscapeReference = new(1200, 800);

        /// <summary>
        /// エディタでスマホ向けレイアウトを検証するための強制フラグ。
        /// true にすると IsTouchPrimary() が常に true を返す。ビルドでは常に false のまま使う。
        /// </summary>
        public static bool SimulateTouch = false;

        /// <summary>
        /// root に対してレスポンシブ追従を設定する。OnEnable 時に 1 度呼べばよい。
        /// GeometryChangedEvent でビューポートのサイズ変更（フルスクリーン化／回転）にも追従する。
        /// </summary>
        /// <param name="root">UIDocument.rootVisualElement</param>
        /// <param name="panelSettings">縦横で match を切り替える対象。null なら切り替えない</param>
        public static void Attach(VisualElement root, PanelSettings panelSettings = null)
        {
            if (root == null) return;
            // 初期反映
            Apply(root, panelSettings);
            // ジオメトリ変化（解像度変更・フルスクリーン化・端末回転）で再評価
            root.RegisterCallback<GeometryChangedEvent>(_ => Apply(root, panelSettings));
        }

        /// <summary>現在の root サイズから縦/横を判定してクラスと match を切り替える。</summary>
        public static void Apply(VisualElement root, PanelSettings panelSettings = null)
        {
            if (root == null) return;
            float width  = root.resolvedStyle.width;
            float height = root.resolvedStyle.height;
            // まだレイアウトが確定していないフレームでは Screen サイズで暫定判定
            if (width <= 0f || height <= 0f)
            {
                width  = UnityEngine.Screen.width;
                height = UnityEngine.Screen.height;
            }
            bool isLandscape = width > height;
            root.EnableInClassList(LandscapeClass, isLandscape);
            root.EnableInClassList(PortraitClass, !isLandscape);
            root.EnableInClassList(TouchClass, IsTouchPrimary());

            // 画面の向きで参照解像度を切り替える（常に幅基準 match=0）。
            // 横画面は旧横型デザインの 1200x800 に戻すことで、base ルールが
            // そのまま以前の PC フルスクリーン見た目を再現する。
            // 注意: PanelSettings はアセットなので、エディタ Play 中の変更は保存され得るが、
            //       起動時に必ずここで正しい値へ上書きされるため実害はない。
            if (panelSettings != null)
            {
                var targetReference = isLandscape ? LandscapeReference : PortraitReference;
                if (panelSettings.referenceResolution != targetReference)
                    panelSettings.referenceResolution = targetReference;
                if (!UnityEngine.Mathf.Approximately(panelSettings.match, 0f))
                    panelSettings.match = 0f;
            }
        }

        /// <summary>
        /// タッチデバイスかどうかを推定する（WebGL モバイルブラウザを含む）。
        /// SystemInfo.deviceType は WebGL でも User-Agent ベースで Handheld を返す。
        /// プロジェクトは New Input System を使用しているため、レガシーの
        /// UnityEngine.Input.touchSupported は使わない。
        /// </summary>
        public static bool IsTouchPrimary()
        {
            if (SimulateTouch) return true;
            return UnityEngine.SystemInfo.deviceType == UnityEngine.DeviceType.Handheld;
        }
    }
}
