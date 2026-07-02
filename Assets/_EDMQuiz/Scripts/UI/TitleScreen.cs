using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    /// <summary>タイトル画面 UI。start-button でゲームシーンへ遷移</summary>
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private string _gameSceneName = "GameScene";

        private Button _startButton;
        private VisualElement _root;

        void OnEnable()
        {
            if (_uiDocument == null) return;
            _root = _uiDocument.rootVisualElement;

            _startButton = _root.Q<Button>("start-button");
            if (_startButton != null) _startButton.clicked += OnStartClicked;

            // BGM 未再生なら最初のタップ/クリックで開始（WebGL 自動再生制約対応）
            if (AudioManager.Instance == null || !AudioManager.Instance.IsBgmPlaying)
                _root.RegisterCallback<PointerDownEvent>(OnFirstInteraction);

            // ビューポートのアスペクト比に応じて .layout-portrait / .layout-landscape を切替
            // （PanelSettings も渡し、縦=幅基準 / 横=高さ基準のスケーリング切替を有効化）
            ResponsiveLayout.Attach(_root, _uiDocument.panelSettings);
        }

        void OnDisable()
        {
            if (_startButton != null) _startButton.clicked -= OnStartClicked;
            _root?.UnregisterCallback<PointerDownEvent>(OnFirstInteraction);
        }

        private void OnFirstInteraction(PointerDownEvent evt)
        {
            _root?.UnregisterCallback<PointerDownEvent>(OnFirstInteraction);
            AudioManager.Instance?.PlayBGM(looped: true);
        }

        private void OnStartClicked()
        {
            // WebGL の自動再生ポリシー対策: PointerDownEvent が UI Toolkit Button に
            // 捕捉されて root まで bubble しない場合でも、click 経由で必ず AudioContext を解除する
            if (AudioManager.Instance != null && !AudioManager.Instance.IsBgmPlaying)
                AudioManager.Instance.PlayBGM(looped: true);
            AudioManager.Instance?.PlayUiTapSE();
            SceneManager.LoadScene(_gameSceneName);
        }
    }
}
