using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    /// <summary>結果画面 UI (UI Toolkit)。GameEnd フェーズで表示</summary>
    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

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

            // 初期状態
            if (_retryButton != null)
                _retryButton.style.scale = new StyleScale(new Scale(Vector3.zero));
            if (_rankTextLabel != null)
                _rankTextLabel.style.opacity = 0f;

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
            _rankLabel.style.color = new StyleColor(GetRankColor(rank));

            _rankLabel.DOScale(GameConstants.RANK_SCALE_PEAK, GameConstants.RANK_SCALE_DURATION).SetEase(Ease.OutBack);
            await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.RANK_SCALE_DURATION), cancellationToken: ct);
            _rankLabel.DOScale(1f, 0.2f);

            _rankTextLabel?.DOFade(1f, 0.4f);
            await UniTask.Delay(TimeSpan.FromSeconds(0.4f), cancellationToken: ct);

            if (rank == "S")
                _rankLabel.DOShakeOnce(0.5f, 8f, 8);

            AudioManager.Instance?.PlayResultSE();

            // リトライボタンをバウンスで登場
            if (_retryButton != null)
                _retryButton.DOScale(1.1f, 0.25f).SetEase(Ease.OutBack)
                    .OnComplete(() => _retryButton?.DOScale(1f, 0.12f));
            _retryButton?.SetEnabled(true);
        }

        private static Color GetRankColor(string rank) => rank switch
        {
            "S" => new Color(1f, 0.84f, 0f),       // ゴールド
            "A" => new Color(0f, 0.94f, 1f),        // シアン
            "B" => new Color(0.43f, 0.85f, 0.28f),  // ライム
            _   => new Color(0.78f, 0.78f, 0.78f),  // グレー
        };

        private void OnRetryClicked()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
            GameFlowManager.Instance?.StartGame();
        }
    }
}
