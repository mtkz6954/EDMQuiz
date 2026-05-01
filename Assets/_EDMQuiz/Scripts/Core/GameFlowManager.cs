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

        private static readonly Subject<Unit> _onQuestionRevealSubject = new();
        public static Observable<Unit> OnQuestionReveal => _onQuestionRevealSubject;

        // BuildUp 中の残り秒数（カウントダウン UI 用、BGM 経過秒で駆動）
        // OnPhaseChanged と同じく static にして、購読側の Instance タイミング依存を排除する。
        private static readonly ReactiveProperty<float> _buildUpRemainingSec = new(GameConstants.BUILDUP_MUSIC_SEC);
        public static ReadOnlyReactiveProperty<float> BuildUpRemainingSec => _buildUpRemainingSec;

        private CancellationTokenSource _answerWindowCts;
        private CancellationTokenSource _gameLoopCts;
        private bool _isJudged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _answerWindowCts?.Cancel();
            _answerWindowCts?.Dispose();
            _answerWindowCts = null;
            _gameLoopCts?.Cancel();
            _gameLoopCts?.Dispose();
            _gameLoopCts = null;
        }

        void Start()
        {
            StartGame();
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
            _gameLoopCts?.Cancel();
            _gameLoopCts?.Dispose();
            _gameLoopCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            RunGameLoopAsync(_gameLoopCts.Token).Forget();
        }

        private async UniTaskVoid RunGameLoopAsync(CancellationToken ct)
        {
            while (QuestionIndex < GameConstants.TOTAL_QUESTIONS)
            {
                _isJudged = false;
                await RunQuestionPhaseAsync(ct);
                await RunBuildUpPhaseAsync(ct);
                await RunAnswerWindowPhaseAsync(ct);
                await RunDropPhaseAsync(ct);
                await RunFadeOutPhaseAsync(ct);
                await RunNextPhaseAsync(ct);
                QuestionIndex++;
            }
            TransitionTo(GamePhase.GameEnd);
        }

        private async UniTask RunQuestionPhaseAsync(CancellationToken ct)
        {
            AudioManager.Instance?.StopBGM();
            TransitionTo(GamePhase.Question);

            float questionIntroDuration = AudioManager.Instance?.PlayQuestionIntroSE()
                                       ?? GameConstants.QUESTION_PHASE_SEC;
            if (questionIntroDuration <= 0f)
            {
                _onQuestionRevealSubject.OnNext(Unit.Default);
                return;
            }

            float revealDelay = Mathf.Max(
                0f,
                questionIntroDuration - GameConstants.QUESTION_FADE_IN_DURATION);
            if (revealDelay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(revealDelay),
                    cancellationToken: ct);
            }

            _onQuestionRevealSubject.OnNext(Unit.Default);

            float remainingDelay = Mathf.Max(0f, questionIntroDuration - revealDelay);
            if (remainingDelay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(remainingDelay),
                    cancellationToken: ct);
            }
        }

        private async UniTask RunBuildUpPhaseAsync(CancellationToken ct)
        {
            TransitionTo(GamePhase.BuildUp);
            _buildUpRemainingSec.Value = GameConstants.BUILDUP_MUSIC_SEC;
            AudioManager.Instance?.PlayBGM();   // 各問題で BGM を 0s から再生

            // BGM の elapsed が BUILDUP_MUSIC_SEC に達するまで待機。入力はロック。
            while (!ct.IsCancellationRequested)
            {
                double elapsed = AudioManager.Instance?.GetBGMElapsedSeconds() ?? 0.0;
                float remaining = Mathf.Max(0f, GameConstants.BUILDUP_MUSIC_SEC - (float)elapsed);
                _buildUpRemainingSec.Value = remaining;
                if (remaining <= 0f) break;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        private async UniTask RunAnswerWindowPhaseAsync(CancellationToken ct)
        {
            TransitionTo(GamePhase.AnswerWindow);
            _answerWindowCts?.Dispose();
            _answerWindowCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(GameConstants.GetAnswerWindowSec()),
                    cancellationToken: _answerWindowCts.Token);

                // 4 拍経過しても未確定なら不正解扱い
                if (!_isJudged)
                {
                    _isJudged = true;
                    AnswerJudgment.Judge("", CurrentQuestion);
                }
            }
            catch (OperationCanceledException)
            {
                // ConfirmAnswer による早期遷移（正常）
            }
        }

        private async UniTask RunDropPhaseAsync(CancellationToken ct)
        {
            TransitionTo(GamePhase.Drop);
            // 結果 VFX は AnswerJudgment.OnJudged → VFXDirector が既に発火済み。
            // ここでは BGM を流したまま 10 秒の余韻を見せる。
            await UniTask.Delay(
                TimeSpan.FromSeconds(GameConstants.RESULT_HOLD_SEC),
                cancellationToken: ct);
        }

        private async UniTask RunFadeOutPhaseAsync(CancellationToken ct)
        {
            TransitionTo(GamePhase.FadeOut);
            if (AudioManager.Instance != null)
            {
                await AudioManager.Instance.FadeBgmOutAsync(GameConstants.BGM_FADE_OUT_SEC, ct);
                AudioManager.Instance.StopBGM();
            }
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

        /// <summary>AnswerWindow 中のみ受付。判定後に待機を中断して Drop へ進む</summary>
        public void ConfirmAnswer(string answer)
        {
            if (CurrentPhase != GamePhase.AnswerWindow) return;
            if (_isJudged) return;
            _isJudged = true;
            AnswerJudgment.Judge(answer, CurrentQuestion);
            _answerWindowCts?.Cancel();
        }
    }
}
