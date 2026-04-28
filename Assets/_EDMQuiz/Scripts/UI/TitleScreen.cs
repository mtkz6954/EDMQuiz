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

        void OnEnable()
        {
            if (_uiDocument == null) return;
            _startButton = _uiDocument.rootVisualElement.Q<Button>("start-button");
            if (_startButton != null) _startButton.clicked += OnStartClicked;
        }

        void OnDisable()
        {
            if (_startButton != null) _startButton.clicked -= OnStartClicked;
        }

        private void OnStartClicked()
        {
            AudioManager.Instance?.PlayUiTapSE();
            SceneManager.LoadScene(_gameSceneName);
        }
    }
}
