# GDD — presentation-vfx

**System Slug**: `presentation-vfx`
**Layer**: Feature（answer-judgment・game-flow・bpm-sync に依存）
**Last Updated**: 2026-04-28
**Status**: Approved（UI Toolkit 化に伴い改訂）

---

## 1. Overview

正解・不正解・ビート同期の3種類の演出を管理するシステム。`VFXDirector` が `AnswerJudgment.OnJudged` と `GameFlowManager.OnPhaseChanged` を R3 で購読し、DOTween（`DOVirtual.Float`）・ParticleSystem・USS クラス切り替えで「爆盛り上がり」と「スベり」の視覚体験を作る。

UI 系演出（オーバーレイ・テキスト拡大）は UI Toolkit の `VisualElement` を対象に、ワールド系演出（紙吹雪・キャラ・ミラーボール）は `GameObject` を対象とする二系統構成。

---

## 2. Player Fantasy

- **正解（Drop）**: 紙吹雪・ミラーボール・背景フラッシュ・キャラ跳ね・テキスト爆発が一斉発火する「全身で爆発する」体験
- **不正解（Drop）**: 青ざめ・画面揺れ・コケアニメ・ブーイングSEが重なる「スベった瞬間の落差」
- **ビート同期（BuildUp）**: 背景・テキストがビートでパルスし「音楽に乗っている感」を持続させる

---

## 3. Detailed Rules

### 演出の種類と構成

| 演出名 | トリガー | 対象 | 主要コンポーネント |
|--------|---------|------|-------------------|
| 正解爆発 | `AnswerJudgment.OnJudged(true)` | World + UI | ParticleSystem（紙吹雪）+ Animator + DOVirtual（UI スケール） |
| 不正解スベり | `AnswerJudgment.OnJudged(false)` | World + UI | DOTween（カメラシェイク）+ Animator + USS フェードオーバーレイ |
| ドロップ待機 | `OnPhaseChanged(Drop)` | UI | 背景フラッシュ + Label 拡大（DOVirtual） |
| ビートパルス | `BpmClock.OnBeat` | UI | 背景 VisualElement の `scale` を DOVirtual で補間 |
| 次問題遷移 | `OnPhaseChanged(Next)` | UI | パネル `opacity` フェードアウト/イン |
| 結果画面登場 | `OnPhaseChanged(GameEnd)` | UI | スコア数値カウントアップ + ランクテキスト拡大 |

### UI Toolkit × DOTween パターン

```csharp
// VisualElement のスケール補間（汎用ヘルパー）
public static Tween DOScale(VisualElement ve, float to, float duration)
{
    float from = ve.style.scale.value.value.x;
    return DOVirtual.Float(from, to, duration, v =>
    {
        ve.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1)));
    });
}

// 透明度補間
public static Tween DOFade(VisualElement ve, float to, float duration)
{
    float from = ve.resolvedStyle.opacity;
    return DOVirtual.Float(from, to, duration, v => ve.style.opacity = v);
}
```

これらは `UIToolkitTweenExtensions.cs` に集約。

### 正解演出の詳細（Drop フェーズ）

```csharp
public async UniTaskVoid PlayCorrectSequenceAsync()
{
    _confettiParticle.Play();
    _funnymonAnimator.SetTrigger("CorrectDance");
    StartMirrorBall();
    UIToolkitTweenExtensions.DOScale(_correctLabel, 1.2f, 0.3f).SetEase(Ease.OutBack);
    Camera.main.transform.DOShakePosition(0.4f, 10f, 20);
    AudioManager.Instance.PlaySE("SE_CORRECT");

    await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.DROP_REVEAL_SEC));

    UIToolkitTweenExtensions.DOScale(_correctLabel, 1f, 0.2f);
    StopMirrorBall();
}
```

### 不正解演出の詳細（Drop フェーズ）

```csharp
public async UniTaskVoid PlayIncorrectSequenceAsync()
{
    _funnymonAnimator.SetTrigger("FailDance");
    UIToolkitTweenExtensions.DOFade(_blueOverlay, 0.6f, 0.2f);
    Camera.main.transform.DOShakePosition(0.4f, 15f, 30);
    AudioManager.Instance.PlaySE("SE_INCORRECT");

    await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
    UIToolkitTweenExtensions.DOFade(_blueOverlay, 0f, 0.5f);

    await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.DROP_REVEAL_SEC - 0.7f));
}
```

### ビートパルスの詳細（BuildUp フェーズ中）

R3 で `OnBeat` を購読し、フェーズ条件付きで実行:

```csharp
BpmClock.OnBeat
    .Where(_ => _currentPhase == GamePhase.BuildUp)
    .Subscribe(_ => PulseBackground())
    .AddTo(this);

private void PulseBackground()
{
    UIToolkitTweenExtensions.DOScale(_backgroundPanel, 1.03f, _beatInterval * 0.2f)
        .SetLoops(2, LoopType.Yoyo);
}
```

---

## 4. Formulas

```
shakeStrength = 10f
shakeVibrato  = 20
shakeDuration = 0.4f

correctScalePeak    = 1.2f
correctScaleDuration = 0.3f

beatPulseScale    = 1.03f
beatPulseDuration = beatInterval * 0.2f

blueOverlayAlpha = 0.6f
```

---

## 5. Edge Cases

| ケース | 対応 |
|--------|------|
| Drop 演出中に Next フェーズ遷移 | `CancellationToken` で UniTask を中断 + `DOTween.Kill(target)` |
| ParticleSystem が WebGL で重い | 最大パーティクル数 200 以下に制限（Technical Preferences 準拠） |
| `OnJudged` が Drop フェーズ以外で届く | R3 `Where(_ => _currentPhase == GamePhase.Drop)` でフィルタ |
| カメラシェイク中に次問題フェードが重なる | `await UniTask.WhenAll` で順序保証 |
| UI Toolkit のスタイル変更がフレーム遅延 | `MarkDirtyRepaint()` を必要に応じて呼ぶ |

---

## 6. Dependencies

| 方向 | システム | 内容 |
|------|---------|------|
| 購読 | `answer-judgment` | `OnJudged` で正解/不正解演出をトリガー（R3） |
| 購読 | `game-flow` | `OnPhaseChanged` で演出の開始・停止（R3） |
| 購読 | `bpm-sync` | `OnBeat` でビートパルスをトリガー（R3） |
| 呼び出し | `audio` | `PlaySE()` で効果音を再生 |
| 利用 | `UIToolkitTweenExtensions` | UI Toolkit + DOTween のヘルパー |

---

## 7. Tuning Knobs

| 定数名 | デフォルト値 | 説明 |
|--------|------------|------|
| `SHAKE_STRENGTH` | 10f | カメラシェイク強度 |
| `SHAKE_DURATION` | 0.4f | カメラシェイク秒数 |
| `CORRECT_SCALE_PEAK` | 1.2f | 正解テキストの最大スケール |
| `CORRECT_SCALE_DURATION` | 0.3f | 正解スケールアニメ秒数 |
| `BEAT_PULSE_SCALE` | 1.03f | ビートパルスのスケール倍率 |
| `BEAT_PULSE_DURATION_RATIO` | 0.2f | ビート間隔に対するパルス長の比率 |
| `BLUE_OVERLAY_ALPHA` | 0.6f | 不正解時の青オーバーレイの最大透明度 |
| `MAX_PARTICLES` | 200 | 紙吹雪の最大パーティクル数 |

---

## 8. Acceptance Criteria

- [ ] 正解時に紙吹雪・USS フェードフラッシュ・Label 拡大が同時発火する
- [ ] 不正解時にカメラシェイク・青オーバーレイ（USS opacity）が発火する
- [ ] BuildUp フェーズ中、`OnBeat` に合わせて背景 VisualElement がパルスする
- [ ] Drop 演出中に Next フェーズへ遷移した場合、UniTask が CancellationToken でキャンセルされる
- [ ] WebGL ビルドで ParticleSystem が 60fps を維持する（デスクトップ）
- [ ] `UIToolkitTweenExtensions.DOScale/DOFade` が VisualElement に正しく適用される
