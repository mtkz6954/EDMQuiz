using UnityEngine;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    /// <summary>Gameplay UI を RenderTexture 経由で world-space 表示へミラーする土台。
    /// 実装は段階的に拡張する。</summary>
    [DisallowMultipleComponent]
    public class GameplayRenderTextureMirror : MonoBehaviour
    {
        [SerializeField] private UIDocument _overlayDocument;
        [SerializeField] private PanelSettings _basePanelSettings;
        [SerializeField] private VisualTreeAsset _displayALayout;
        [SerializeField] private VisualTreeAsset _displayBLayout;

        public UIDocument OverlayDocument => _overlayDocument;
        public UIDocument DisplayADocument { get; private set; }
        public UIDocument DisplayBDocument { get; private set; }
        public bool IsReady => DisplayADocument != null && DisplayBDocument != null;
    }
}
