# GDD — bpm-sync

**System Slug**: `bpm-sync`
**Layer**: Core（audio に依存）
**Last Updated**: 2026-04-27
**Status**: Approved

---

## 1. Overview

CRI ADX の再生時間（`CriAtomExPlayback.GetTime()`）を基準クロックとして、BGM のビートとゲームの演出タイミングを同期させるシステム。`BpmClock` が経過ビート数・小節数・フェーズ境界を算出し、`game-flow` や `presentation-vfx` へ提供する。

---

## 2. Player Fantasy

「BGMのビートにUIがピッタリ合っている」感覚——ひらがなボタンがビートで光る、ビルドアップの緊張が音楽と一体になって高まる、ドロップの瞬間に演出が「ズレずに」爆発する。このズレが 50ms あるだけで没入感が崩れる。

---

## 3. Detailed Rules

### 基準クロック

```
elapsedMs      = CriAtomExPlayback.GetTime()    // ミリ秒（long）
elapsedSeconds = elapsedMs / 1000f              // 秒（float）
```

BGM 読み込み失敗時は `Time.unscaledTime`（BGM 開始時刻を記録して差分を取る）にフォールバック。

### ビート・小節の算出

```
BPM           = 設定値（GameConstants）
beatDuration  = 60f / BPM                      // 1ビートの秒数
elapsedBeats  = elapsedSeconds / beatDuration  // 経過ビート数（float）
currentBeat   = (int)elapsedBeats % BEATS_PER_BAR  // 小節内ビート番号 (0-indexed)
currentBar    = (int)elapsedBeats / BEATS_PER_BAR  // 経過小節数
```

### BpmClock の公開 API

```csharp
float ElapsedSeconds    { get; }   // 経過秒
float ElapsedBeats      { get; }   // 経過ビート数
int   CurrentBeat       { get; }   // 小節内ビート (0〜BEATS_PER_BAR-1)
int   CurrentBar        { get; }   // 経過小節数

// ビート境界を購読するイベント
public static event Action OnBeat;  // 毎ビート発火
public static event Action OnBar;   // 毎小節の頭で発火
```

### 更新頻度

- `Update()` で毎フレーム `CriAtomExPlayback.GetTime()` を取得して計算
- `OnBeat` / `OnBar` は前フレームとの比較で境界越えを検出して発火

### BGM 開始の同期

- `AudioManager.PlayBGM()` が呼ばれた直後に `BpmClock.StartClock()` を呼ぶ
- `StartClock()` 内で `CriAtomExPlayback` の参照を取得し計測開始

---

## 4. Formulas

```
beatDuration   = 60f / BPM
elapsedBeats   = elapsedSeconds / beatDuration
currentBeat    = (int)elapsedBeats % BEATS_PER_BAR
currentBar     = (int)(elapsedBeats / BEATS_PER_BAR)

// ビート境界検出
prevBeatInt    = (int)prevElapsedBeats
currBeatInt    = (int)elapsedBeats
isBeatCrossed  = currBeatInt > prevBeatInt
```

| 変数 | デフォルト | 説明 |
|------|-----------|------|
| `BPM` | 128 | BGMのテンポ（要チューニング） |
| `BEATS_PER_BAR` | 4 | 1小節のビート数（4/4拍子） |

---

## 5. Edge Cases

| ケース | 対応 |
|--------|------|
| BGM 未再生状態で `ElapsedSeconds` を参照 | 0f を返す（`_isRunning` フラグで制御） |
| BGM 読み込み失敗 | `Time.unscaledTime` ベースに切り替え（`AudioManager` がフラグ通知） |
| フレームレート低下（30fps 以下）でビート境界を飛び越し | `(int)elapsedBeats` の差分で複数ビート分を一括検出、最後の1回のみ `OnBeat` 発火 |
| WebGL シングルスレッドで `GetTime()` がブロック | CRI ADX LE の WebGL 対応版では非ブロッキング — 問題なし |
| `BPM` が 0 以下 | `beatDuration` が無限大 → `Mathf.Max(BPM, 1f)` でガード |

---

## 6. Dependencies

| 方向 | システム | 内容 |
|------|---------|------|
| 読み取り | `audio` | `CriAtomExPlayback.GetTime()` で基準クロック取得 |
| 提供 | `game-flow` | `ElapsedBeats`・`OnBar` でフェーズ遷移タイミングを提供 |
| 提供 | `presentation-vfx` | `OnBeat` でビート連動アニメーションをトリガー |
| 提供 | `input-ui` | `OnBeat` でひらがなボタンのパルス演出をトリガー |

---

## 7. Tuning Knobs

| 定数名 | デフォルト値 | 説明 |
|--------|------------|------|
| `BPM` | 128 | BGMのテンポ。BGM差し替え時に合わせて変更 |
| `BEATS_PER_BAR` | 4 | 拍子（4/4拍子固定） |
| `BUILDUP_BARS` | 8 | ビルドアップの小節数（`BUILDUP_PHASE_SEC` と整合させる） |

`BUILDUP_PHASE_SEC`（24秒）= `BUILDUP_BARS`（8小節） × `beatDuration × 4`（BPM=128 で 1小節=1.875秒 × 8 ≒ 15秒）——**BPM と秒数の整合は BGM 確定後に要調整**。

---

## 8. Acceptance Criteria

- [ ] BGM 再生中に `ElapsedSeconds` が単調増加する
- [ ] BPM=128 で `OnBeat` が約 0.469 秒ごとに発火する（±20ms 以内）
- [ ] `OnBar` が4ビートごとに発火する
- [ ] BGM 読み込み失敗時に `Time.unscaledTime` ベースで動作し続ける
- [ ] 単体テスト: BPM=120 で 2秒後の `elapsedBeats` が 4.0 になる
