using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using NaughtyAttributes;

namespace EDMQuiz
{
    /// <summary>ゲーム全体のフェーズ進行を UniTask で制御し、R3 で通知</summary>
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        [SerializeField] private QuizDatabase _quizDatabase;

        public GamePhase CurrentPhase { get; private set; } = GamePhase.Idle;
        public int QuestionIndex { get; private set; }
        public QuizQuestion CurrentQuestion =>
            _quizDatabase != null ? _quizDatabase.Get(QuestionIndex) : null;

        private static readonly Subject<GamePhase> _onPhaseChangedSubject = new();
        public static Observable<GamePhase> OnPhaseChanged => _onPhaseChangedSubject;

        private CancellationTokenSource _buildUpCts;
        private bool _isJudged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [Button("Start Game (Editor Test)")]
        public void StartGame()
        {
            if (_quizDatabase == null || _quizDatabase.Count == 0)
            {
                Debug.LogError("[GameFlowManager] QuizDatabase が未設定または空");
                return;
            }
            QuestionIndex = 0;
            RunGameLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid RunGameLoopAsync(CancellationToken ct)
        {
            while (QuestionIndex < GameConstants.TOTAL_QUESTIONS)
            {
                _isJudged = false;
                await RunQuestionPhaseAsync(ct);
                await RunBuildUpPhaseAsync(ct);
                await RunDropPhaseAsync(ct);
                await RunNextPhaseAsync(ct);
                QuestionIndex++;
            }
            TransitionTo(GamePhase.GameEnd);
        }

        private async UniTask RunQuestionPhaseAsync(CancellationToken ct)
        {
            TransitionTo(GamePhase.Question);
            await UniTask.Delay(
                TimeSpan.FromSeconds(GameConstants.QUESTION_PHASE_SEC),
                cancellationToken: ct);
        }

        private async UniTask RunBuildUpPhaseAsync(CancellationToken ct)
        {
            TransitionTo(GamePhase.BuildUp);
            _buildUpCts?.Dispose();
            _buildUpCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(GameConstants.GetBuildUpDurationSec()),
                    cancellationToken: _buildUpCts.Token);

                // タイムアウトで未確定なら不正解判定
                if (!_isJudged) AnswerJudgment.Judge("", CurrentQuestion);
            }
            catch (OperationCanceledException)
            {
                // 確定ボタンによる中断（正常）
            }
        }

        private async UniTask RunDropPhaseAsync(CancellationToken ct)
        {
            TransitionTo(GamePhase.Drop);
            await UniTask.Delay(
                TimeSpan.FromSeconds(GameConstants.DROP_REVEAL_SEC),
                cancellationToken: ct);
        }

        private async UniTask RunNextPhaseAsync(CancellationToken ct)
        {
            TransitionTo(GamePhase.Next);
            await UniTask.Delay(
                TimeSpan.FromSeconds(GameConstants.NEXT_TRANSITION_SEC),
                cancellationToken: ct);
        }

        private void TransitionTo(GamePhase phase)
        {
            CurrentPhase = phase;
            _onPhaseChangedSubject.OnNext(phase);
        }

        /// <summary>BuildUp 中のみ受付。判定後に BuildUp 待機を中断して Drop へ進む</summary>
        public void ConfirmAnswer(string answer)
        {
            if (CurrentPhase != GamePhase.BuildUp) return;
            if (_isJudged) return;
            _isJudged = true;
            AnswerJudgment.Judge(answer, CurrentQuestion);
            _buildUpCts?.Cancel();
        }
    }
}
