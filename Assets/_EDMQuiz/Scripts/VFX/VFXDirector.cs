using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    /// <summary>正解・不正解・ビート同期の演出を統括</summary>
    public class VFXDirector : MonoBehaviour
    {
        [Header("World VFX")]
        [SerializeField] private ParticleSystem _confettiParticle;
        [SerializeField] private Transform      _mirrorBallTransform;
        [SerializeField] private Animator       _funnymonAnimator;

        [Header("UI VFX (UI Toolkit)")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private string _backgroundPanelName = "background-panel";
        [SerializeField] private string _blueOverlayName     = "blue-overlay";
        [SerializeField] private string _correctLabelName    = "correct-label";

        private VisualElement _backgroundPanel;
        private VisualElement _blueOverlay;
        private Label _correctLabel;

        private CancellationTokenSource _vfxCts;
        private Tween _mirrorBallTween;

        void OnEnable()
        {
            if (_uiDocument != null)
            {
                var root = _uiDocument.rootVisualElement;
                _backgroundPanel = root.Q<VisualElement>(_backgroundPanelName);
                _blueOverlay     = root.Q<VisualElement>(_blueOverlayName);
                _correctLabel    = root.Q<Label>(_correctLabelName);
            }

            AnswerJudgment.OnJudged
                .Subscribe(HandleJudged)
                .AddTo(this);

            BpmClock.OnBeat
                .Where(_ => GameFlowManager.Instance != null
                         && GameFlowManager.Instance.CurrentPhase == GamePhase.BuildUp)
                .Subscribe(_ => PulseBackground())
                .AddTo(this);

            GameFlowManager.OnPhaseChanged
                .Where(p => p == GamePhase.Next)
                .Subscribe(_ => CancelVfx())
                .AddTo(this);
        }

        void OnDisable()
        {
            CancelVfx();
        }

        private void HandleJudged(bool isCorrect)
        {
            CancelVfx();
            _vfxCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            if (isCorrect) PlayCorrectSequenceAsync(_vfxCts.Token).Forget();
            else PlayIncorrectSequenceAsync(_vfxCts.Token).Forget();
        }

        private async UniTaskVoid PlayCorrectSequenceAsync(CancellationToken ct)
        {
            try
            {
                _confettiParticle?.Play();
                _funnymonAnimator?.SetTrigger("CorrectDance");
                StartMirrorBall();
                AudioManager.Instance?.PlayCorrectSE();

                if (Camera.main != null)
                {
                    Camera.main.transform.DOShakePosition(
                        GameConstants.SHAKE_DURATION,
                        GameConstants.SHAKE_STRENGTH,
                        GameConstants.SHAKE_VIBRATO);
                }

                if (_correctLabel != null)
                {
                    _correctLabel
                        .DOScale(GameConstants.CORRECT_SCALE_PEAK, GameConstants.CORRECT_SCALE_DURATION)
                        .SetEase(Ease.OutBack);
                    await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.CORRECT_SCALE_DURATION), cancellationToken: ct);
                }

                float remain = GameConstants.DROP_REVEAL_SEC - GameConstants.CORRECT_SCALE_DURATION;
                if (remain > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(remain), cancellationToken: ct);

                _correctLabel?.DOScale(1f, 0.2f);
                StopMirrorBall();
            }
            catch (OperationCanceledException) { }
        }

        private async UniTaskVoid PlayIncorrectSequenceAsync(CancellationToken ct)
        {
            try
            {
                _funnymonAnimator?.SetTrigger("FailDance");
                AudioManager.Instance?.PlayIncorrectSE();

                if (Camera.main != null)
                {
                    Camera.main.transform.DOShakePosition(
                        GameConstants.SHAKE_DURATION,
                        GameConstants.SHAKE_STRENGTH * 1.5f,
                        GameConstants.SHAKE_VIBRATO);
                }

                _blueOverlay?.DOFade(GameConstants.BLUE_OVERLAY_ALPHA, 0.2f);

                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: ct);
                _blueOverlay?.DOFade(0f, 0.5f);

                float remain = GameConstants.DROP_REVEAL_SEC - 0.7f;
                if (remain > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(remain), cancellationToken: ct);
            }
            catch (OperationCanceledException) { }
        }

        private void PulseBackground()
        {
            if (_backgroundPanel == null) return;
            float duration = GameConstants.GetBeatDuration() * GameConstants.BEAT_PULSE_DURATION_RATIO;
            _backgroundPanel.DOPulse(GameConstants.BEAT_PULSE_SCALE, duration);
        }

        private void StartMirrorBall()
        {
            if (_mirrorBallTransform == null) return;
            _mirrorBallTween?.Kill();
            _mirrorBallTween = _mirrorBallTransform
                .DORotate(new Vector3(0, 360, 0),
                          GameConstants.GetBeatDuration() * 4f,
                          RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear);
        }

        private void StopMirrorBall()
        {
            _mirrorBallTween?.Kill();
            _mirrorBallTween = null;
        }

        private void CancelVfx()
        {
            _vfxCts?.Cancel();
            _vfxCts?.Dispose();
            _vfxCts = null;
            if (_correctLabel != null) DOTween.Kill(_correctLabel);
            if (_blueOverlay != null)  DOTween.Kill(_blueOverlay);
            StopMirrorBall();
        }
    }
}
