using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace EDMQuiz
{
    public class VFXDirector : MonoBehaviour
    {
        [Header("正解演出")]
        [SerializeField] private ParticleSystem _confettiParticle;
        [SerializeField] private Transform      _mirrorBallTransform;
        [SerializeField] private Animator       _funnymonAnimator;

        [Header("不正解演出")]
        [SerializeField] private CanvasGroup    _screenOverlay;  // 青ざめオーバーレイ

        [Header("共通")]
        [SerializeField] private Transform      _backgroundTransform;

        private float _beatInterval;
        private bool  _isPlayingSequence;
        private Tween _mirrorBallTween;

        void OnEnable()
        {
            AnswerJudgment.OnJudged += HandleJudgment; // TODO: staticイベント化
            BpmClock.OnBeat        += HandleBeat;
        }

        void OnDisable()
        {
            BpmClock.OnBeat -= HandleBeat;
        }

        void Start()
        {
            _beatInterval = 60f / GameConstants.BPM;
        }

        private void HandleJudgment(bool isCorrect)
        {
            if (_isPlayingSequence) return;
            StartCoroutine(isCorrect ? PlayCorrectSequence() : PlayIncorrectSequence());
        }

        private IEnumerator PlayCorrectSequence()
        {
            _isPlayingSequence = true;

            _confettiParticle?.Play();
            StartMirrorBall();
            _funnymonAnimator?.SetTrigger("CorrectDance");
            Camera.main?.transform.DOShakePosition(
                GameConstants.SCREEN_SHAKE_DURATION,
                GameConstants.SCREEN_SHAKE_STRENGTH
            );
            // AudioManager.Instance.PlaySE(seCorrect); // CRI ADX有効化後に解除

            yield return new WaitForSeconds(GameConstants.DROP_REVEAL_SEC);

            StopMirrorBall();
            _isPlayingSequence = false;
        }

        private IEnumerator PlayIncorrectSequence()
        {
            _isPlayingSequence = true;

            _funnymonAnimator?.SetTrigger("FailDance");
            // AudioManager.Instance.PlaySE(seIncorrect);

            if (_screenOverlay != null)
                _screenOverlay.DOFade(GameConstants.BLUE_OVERLAY_ALPHA, 0.2f);

            // 一瞬静止（DOTweenはSetUpdate(true)でtimeScaleに依存しない）
            yield return new WaitForSecondsRealtime(GameConstants.FREEZE_DURATION);

            yield return new WaitForSeconds(1.5f);

            if (_screenOverlay != null)
                _screenOverlay.DOFade(0f, 0.5f);

            yield return new WaitForSeconds(GameConstants.DROP_REVEAL_SEC - 1.5f);

            _isPlayingSequence = false;
        }

        private void HandleBeat()
        {
            _backgroundTransform?.DOPunchScale(Vector3.one * 0.02f, _beatInterval * 0.3f, 1, 0);
        }

        private void StartMirrorBall()
        {
            if (_mirrorBallTransform == null) return;
            float degreesPerBeat = 360f / 4;  // 4拍で1回転
            _mirrorBallTween = _mirrorBallTransform
                .DORotate(new Vector3(0, degreesPerBeat, 0), _beatInterval, RotateMode.LocalAxisAdd)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear);
        }

        private void StopMirrorBall()
        {
            _mirrorBallTween?.Kill();
        }
    }
}
