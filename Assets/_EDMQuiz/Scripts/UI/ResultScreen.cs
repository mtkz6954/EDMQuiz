using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    /// <summary>結果画面 UI (UI Toolkit)。GameEnd フェーズで表示</summary>
    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private string _titleSceneName = "TitleScene";

        private VisualElement _root;
        private Label _scoreLabel;
        private Label _rankLabel;
        private Label _rankTextLabel;
        private Button _retryButton;

        void OnEnable()
        {
            if (_uiDocument == null) return;
            var doc = _uiDocument.rootVisualElement;
            _root          = doc.Q<VisualElement>("result-root");
            _scoreLabel    = doc.Q<Label>("score-label");
            _rankLabel     = doc.Q<Label>("rank-label");
            _rankTextLabel = doc.Q<Label>("rank-text-label");
            _retryButton   = doc.Q<Button>("retry-button");

            if (_retryButton != null) _retryButton.clicked += OnRetryClicked;

            if (_root != null) _root.style.display = DisplayStyle.None;

            GameFlowManager.OnPhaseChanged
                .Where(p => p == GamePhase.GameEnd)
                .Subscribe(_ => ShowAsync(this.GetCancellationTokenOnDestroy()).Forget())
                .AddTo(this);
        }

        void OnDisable()
        {
            if (_retryButton != null) _retryButton.clicked -= OnRetryClicked;
        }

        private async UniTaskVoid ShowAsync(CancellationToken ct)
        {
            if (_root == null) return;

            _root.style.display = DisplayStyle.Flex;
            _retryButton?.SetEnabled(false);

            _root.style.opacity = 0f;
            _root.DOFade(1f, 0.5f);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: ct);

            int finalScore = ScoreManager.Instance != null
                ? ScoreManager.Instance.ExcitementScore
                : 0;

            _scoreLabel.DOCountUp(0, finalScore, GameConstants.SCORE_COUNTUP_DURATION).SetEase(Ease.OutCubic);
            await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.SCORE_COUNTUP_DURATION), cancellationToken: ct);

            string rank      = ScoreManager.DetermineRank(finalScore);
            string rankLabel = ScoreManager.GetRankLabel(rank);
            _rankLabel.text     = rank;
            _rankTextLabel.text = rankLabel;
            _rankLabel.style.scale = new StyleScale(new Scale(Vector3.zero));

            _rankLabel.DOScale(GameConstants.RANK_SCALE_PEAK, GameConstants.RANK_SCALE_DURATION).SetEase(Ease.OutBack);
            await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.RANK_SCALE_DURATION), cancellationToken: ct);
            _rankLabel.DOScale(1f, 0.2f);

            AudioManager.Instance?.PlayResultSE();
            _retryButton?.SetEnabled(true);
        }

        private void OnRetryClicked()
        {
            if (!string.IsNullOrEmpty(_titleSceneName))
                SceneManager.LoadScene(_titleSceneName);
        }
    }
}
