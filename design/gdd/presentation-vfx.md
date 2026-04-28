# GDD — presentation-vfx

**System Slug**: `presentation-vfx`
**Layer**: Feature（answer-judgment・game-flow・bpm-sync に依存）
**Last Updated**: 2026-04-27
**Status**: Approved

---

## 1. Overview

正解・不正解・ビート同期の3種類の演出を管理するシステム。`VFXDirector` が `OnAnswerJudged` と `OnPhaseChanged` を購読し、DOTween アニメーション・ParticleSystem・UI スケール演出を組み合わせて「爆盛り上がり」と「スベり」の視覚体験を作る。

---

## 2. Player Fantasy

- **正解（Drop）**: 紙吹雪・ミラーボール・背景フラッシュ・キャラ跳ね・テキスト爆発が一斉発火する「全身で爆発する」体験
- **不正解（Drop）**: 青ざめ・画面揺れ・コケアニメ・ブーイングSEが重なる「スベった瞬間の落差」
- **ビート同期（BuildUp）**: 背景・テキストがビートでパルスし「音楽に乗っている感」を持続させる

---

## 3. Detailed Rules

### 演出の種類と構成

| 演出名 | トリガー | 主要コンポーネント |
|--------|---------|----------------|
| 正解爆発 | `OnAnswerJudged(true)` | ParticleSystem（紙吹雪）+ DOTween（UI スケール）+ 背景フラッシュ |
| 不正解スベり | `OnAnswerJudged(false)` | DOTween（青ざめ色変化）+ カメラシェイク + コケアニメ |
| ドロップ待機 | `OnPhaseChanged(Drop)` | 背景フラッシュ + テキスト拡大 |
| ビートパルス | `BpmClock.OnBeat` | 背景 UI の `RectTransform.DOScale()` |
| 次問題遷移 | `OnPhaseChanged(Next)` | フェードアウト + フェードイン |
| 結果画面登場 | `OnPhaseChanged(GameEnd)` | スコア数値カウントアップ + ランクテキスト拡大 |

### 正解演出の詳細（Drop フェーズ）

```
1. 背景フラッシュ（白→通常: 0.2秒）
2. 紙吹雪 ParticleSystem 発火（DROP_REVEAL_SEC の間）
3. 正解テキスト DOTween スケール: 0 → 1.2 → 1.0（Elastic ease, 0.5秒）
4. キャラ跳ねアニメ（AnimationClip: 正解モーション）
5. SE_CORRECT 再生（AudioManager 経由）
```

### 不正解演出の詳細（Drop フェーズ）

```
1. 背景色変化（通常→青ざめ→通常: 0.5秒）
2. カメラシェイク（DOTween Punch: 強度 10, 振動数 20, 0.4秒）
3. コケアニメ（AnimationClip: 不正解モーション）
4. SE_INCORRECT 再生（AudioManager 経由）
```

### ビートパルスの詳細（BuildUp フェーズ中）

- 対象: 背景パネル・問題テキスト
- `OnBeat` 受信 → `DOScale(1.03f, 0.08f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo)`
- `input-ui` 側のボタンパルスとは独立して管理

---

## 4. Formulas

```
// カメラシェイク
shakeStrength = 10f
shakeVibrato  = 20
shakeDuration = 0.4f

// 正解テキストスケール
scaleTarget   = 1.2f → 1.0f（Elastic ease）
scaleDuration = 0.5f

// ビートパルス
pulseScale    = 1.03f
pulseDuration = 0.08f
```

---

## 5. Edge Cases

| ケース | 対応 |
|--------|------|
| Drop 演出中に Next フェーズ遷移が来る | `DOTween.KillAll()` を `OnPhaseChanged(Next)` で呼び、演出を強制終了 |
| ParticleSystem が WebGL で重い | 最大パーティクル数 200 以下に制限（Technical Preferences 準拠） |
| `OnAnswerJudged` が Drop フェーズ以外で届く | `_currentPhase != GamePhase.Drop` ならガードして無視 |
| カメラシェイク中に次問題フェードが重なる | Sequence で順番を保証（シェイク完了後にフェード開始） |

---

## 6. Dependencies

| 方向 | システム | 内容 |
|------|---------|------|
| 購読 | `answer-judgment` | `OnAnswerJudged` で正解/不正解演出をトリガー |
| 購読 | `game-flow` | `OnPhaseChanged` で演出の開始・停止 |
| 購読 | `bpm-sync` | `OnBeat` でビートパルスをトリガー |
| 呼び出し | `audio` | `PlaySE()` で効果音を再生 |

---

## 7. Tuning Knobs

| 定数名 | デフォルト値 | 説明 |
|--------|------------|------|
| `SHAKE_STRENGTH` | 10f | カメラシェイク強度 |
| `SHAKE_DURATION` | 0.4f | カメラシェイク秒数 |
| `CORRECT_SCALE_PEAK` | 1.2f | 正解テキストの最大スケール |
| `CORRECT_SCALE_DURATION` | 0.5f | 正解スケールアニメ秒数 |
| `BEAT_PULSE_SCALE` | 1.03f | ビートパルスのスケール倍率 |
| `BEAT_PULSE_DURATION` | 0.08f | ビートパルスの片道秒数 |
| `MAX_PARTICLES` | 200 | 紙吹雪の最大パーティクル数 |

---

## 8. Acceptance Criteria

- [ ] 正解時に紙吹雪・背景フラッシュ・テキスト拡大が同時発火する
- [ ] 不正解時にカメラシェイク・青ざめ演出が発火する
- [ ] BuildUp フェーズ中、`OnBeat` に合わせて背景がパルスする
- [ ] Drop 演出中に Next フェーズへ遷移した場合、演出が強制終了する
- [ ] WebGL ビルドで ParticleSystem が 60fps を維持する（デスクトップ）
