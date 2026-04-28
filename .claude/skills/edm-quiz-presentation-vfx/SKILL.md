---
name: edm-quiz-presentation-vfx
description: 演出システム (VFXDirector) の実装方針。正解・不正解・ビート同期の3種類の演出を UniTask シーケンス + DOTween + ParticleSystem + UI Toolkit で構成する。CancellationToken による中断、UI Toolkit と DOTween の連携を扱うときに参照する。
---

# presentation-vfx — 演出ディレクター

## 責務

`AnswerJudgment.OnJudged` と `GameFlowManager.OnPhaseChanged` を購読し、正解/不正解/ビート同期の演出を発火する `VFXDirector`。UI 系演出は UI Toolkit、ワールド系は GameObject。

---

## 演出マトリクス

| 演出名 | トリガー | 対象 | 主要技術 |
|--------|---------|------|---------|
| 正解爆発 | `OnJudged(true)` | World + UI | ParticleSystem + Animator + DOVirtual + DOShakePosition |
| 不正解スベり | `OnJudged(false)` | World + UI | DOTween shake + Animator + USS opacity |
| ドロップ待機 | `OnPhaseChanged(Drop)` | UI | Label 拡大 + 背景フラッシュ |
| ビートパルス | `BpmClock.OnBeat` | UI | VisualElement scale 補間 |
| 次問題遷移 | `OnPhaseChanged(Next)` | UI | パネル opacity フェード |
| 結果画面登場 | `OnPhaseChanged(GameEnd)` | UI | カウントアップ + Label 拡大 |

---

## VFXDirector 実装

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    public class VFXDirector : MonoBehaviour
    {
        [Header("World VFX")]
        [SerializeField] private ParticleSystem _confettiParticle;
        [SerializeField] private Transform      _mirrorBallTransform;
        [SerializeField] private Animator       _funnymonAnimator;

        [Header("UI VFX")]
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _backgroundPanel;
        private VisualElement _blueOverlay;
        private Label _correctLabel;

        private CancellationTokenSource _vfxCts;
        private Tween _mirrorBallTween;

        void OnEnable()
        {
            var root = _uiDocument.rootVisualElement;
            _backgroundPanel = root.Q<VisualElement>("background-panel");
            _blueOverlay     = root.Q<VisualElement>("blue-overlay");
            _correctLabel    = root.Q<Label>("correct-label");

            AnswerJudgment.OnJudged
                .Subscribe(HandleJudged)
                .AddTo(this);

            BpmClock.OnBeat
                .Where(_ => GameFlowManager.Instance?.CurrentPhase == GamePhase.BuildUp)
                .Subscribe(_ => PulseBackground())
                .AddTo(this);

            GameFlowManager.OnPhaseChanged
                .Where(p => p == GamePhase.Next)
                .Subscribe(_ => CancelVfx())
                .AddTo(this);
        }

        private void HandleJudged(bool isCorrect)
        {
            CancelVfx();
            _vfxCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            if (isCorrect) PlayCorrectSequenceAsync(_vfxCts.Token).Forget();
            else PlayIncorrectSequenceAsync(_vfxCts.Token).Forget();
        }

        private async UniTaskVoid PlayCorrectSequenceAsync(CancellationToken ct)
        {
            _confettiParticle.Play();
            _funnymonAnimator.SetTrigger("CorrectDance");
            StartMirrorBall();
            AudioManager.Instance.PlayCorrectSE();

            Camera.main.transform.DOShakePosition(
                GameConstants.SHAKE_DURATION,
                GameConstants.SHAKE_STRENGTH,
                GameConstants.SHAKE_VIBRATO);

            await _correctLabel.DOScale(GameConstants.CORRECT_SCALE_PEAK,
                                        GameConstants.CORRECT_SCALE_DURATION)
                               .SetEase(Ease.OutBack)
                               .ToUniTask(cancellationToken: ct);

            await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.DROP_REVEAL_SEC - GameConstants.CORRECT_SCALE_DURATION),
                               cancellationToken: ct);

            _correctLabel.DOScale(1f, 0.2f);
            StopMirrorBall();
        }

        private async UniTaskVoid PlayIncorrectSequenceAsync(CancellationToken ct)
        {
            _funnymonAnimator.SetTrigger("FailDance");
            AudioManager.Instance.PlayIncorrectSE();

            Camera.main.transform.DOShakePosition(
                GameConstants.SHAKE_DURATION,
                GameConstants.SHAKE_STRENGTH * 1.5f,
                GameConstants.SHAKE_VIBRATO);

            _blueOverlay.DOFade(GameConstants.BLUE_OVERLAY_ALPHA, 0.2f);

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: ct);
            _blueOverlay.DOFade(0f, 0.5f);

            await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.DROP_REVEAL_SEC - 0.7f),
                               cancellationToken: ct);
        }

        private void PulseBackground()
        {
            _backgroundPanel.DOPulse(
                GameConstants.BEAT_PULSE_SCALE,
                GameConstants.GetBeatDuration() * GameConstants.BEAT_PULSE_DURATION_RATIO);
        }

        private void StartMirrorBall()
        {
            _mirrorBallTween = _mirrorBallTransform
                .DORotate(new Vector3(0, 360, 0), GameConstants.GetBeatDuration() * 4f, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear);
        }

        private void StopMirrorBall()
        {
            _mirrorBallTween?.Kill();
        }

        private void CancelVfx()
        {
            _vfxCts?.Cancel();
            _vfxCts?.Dispose();
            _vfxCts = null;
            DOTween.Kill(_correctLabel);
            DOTween.Kill(_blueOverlay);
        }
    }
}
```

`DOScale` / `DOFade` / `DOPulse` は `UIToolkitTweenExtensions` の拡張メソッド（`edm-quiz-ui-toolkit` skill 参照）。

---

## チューニング項目

```csharp
public const float SHAKE_STRENGTH               = 10f;
public const int   SHAKE_VIBRATO                = 20;
public const float SHAKE_DURATION               = 0.4f;
public const float CORRECT_SCALE_PEAK           = 1.2f;
public const float CORRECT_SCALE_DURATION       = 0.3f;
public const float BEAT_PULSE_SCALE             = 1.03f;
public const float BEAT_PULSE_DURATION_RATIO    = 0.2f;
public const float BLUE_OVERLAY_ALPHA           = 0.6f;
public const int   MAX_PARTICLES                = 200;
```

---

## Edge Cases

| ケース | 対応 |
|--------|------|
| Drop 中に Next 遷移 | `CancelVfx()` → CancellationToken cancel + DOTween.Kill |
| WebGL でパーティクル重い | ParticleSystem 設定で `Max Particles ≤ 200` |
| OnJudged が Drop 外で発火 | フェーズチェックで弾くか、Subject 側で防ぐ |
| 連続発火（早押し→Drop） | 旧 `_vfxCts` をキャンセル → 新 `_vfxCts` で再起動 |

---

## Acceptance Criteria

- [ ] 正解時に紙吹雪・ミラーボール・Label 拡大・歓声 SE が同時発火
- [ ] 不正解時にカメラシェイク・青オーバーレイ・コケ・ブーイング SE
- [ ] BuildUp 中、OnBeat に同期して背景がパルス
- [ ] Drop 演出中に Next 遷移で全演出キャンセル
- [ ] WebGL で 60fps 維持

---

## 関連 Skill

- UI 全般: `edm-quiz-ui-toolkit`
- 判定: `edm-quiz-answer-judgment`
- フロー: `edm-quiz-game-flow`
- BPM: `edm-quiz-bpm-sync`
- 音声: `edm-quiz-audio`
- 非同期: `edm-quiz-async-reactive`
