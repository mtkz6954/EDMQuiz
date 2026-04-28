---
name: cri-adx-bpm-sync
description: CRI ADX でリズムゲーム向けの BPM 同期再生時刻を取得する方法。GetTime() vs GetTimeSyncedWithAudio() の違い、サンプル精度、フレーム独立タイミングを扱う。EDMQuiz の BpmClock 実装で必須参照。
---

# CRI ADX BPM 同期 — 再生時刻の正確な取得

## 2 つの時刻取得 API

| API | 戻り値 | 精度 | 用途 |
|-----|--------|------|------|
| `GetTime()` | ミリ秒（long） | フレーム依存 | 一般的な再生時刻表示 |
| **`GetTimeSyncedWithAudio()`** | マイクロ秒（long） | サンプル精度 | **リズムゲーム・BPM 同期** |

リズムゲームでは **必ず `GetTimeSyncedWithAudio()` を使う**。

公式: 「リズムゲーム等、楽曲とゲーム内時間を同期する必要があるゲームでは後者を使用します」

---

## GetTime() — 一般用途

```csharp
long ms = _playback.GetTime();
float sec = ms / 1000f;
Debug.Log($"再生時刻: {sec}s");
```

- フレームごとに同じ値が返ることがある（フレーム同期）
- パフォーマンス: 低コスト
- 用途: スコア表示・進捗バー・デバッグ

---

## GetTimeSyncedWithAudio() — リズム同期

```csharp
long us = _playback.GetTimeSyncedWithAudio();
double sec = us / 1_000_000.0;
```

**特徴:**
- **マイクロ秒単位**（`GetTime` のミリ秒より3桁細かい）
- 音声サンプル単位の精度
- フレーム間でも単調増加（フレームレート依存しない）
- パフォーマンスは `GetTime()` より高コスト

**用途:**
- ビートに合わせた演出発火
- 音楽との完全同期が必要な処理
- BPM クロックの基準

⚠️ **注意**: 32 bit 精度内で動作するため、24日連続再生でオーバーフロー（プロトタイプ規模では無関係）。

---

## EDMQuiz BpmClock での実装

### AudioManager 側（基準クロック提供）

```csharp
public class AudioManager : MonoBehaviour
{
    private CriAtomExPlayback _bgmPlayback;

    /// <summary>BGM の経過秒数（サンプル精度）</summary>
    public double GetBGMElapsedSeconds()
    {
        if (!IsBgmPlaying) return 0.0;
        long us = _bgmPlayback.GetTimeSyncedWithAudio();
        return us / 1_000_000.0;  // マイクロ秒 → 秒
    }
}
```

`float` ではなく **`double`** を使う（マイクロ秒精度を活かすため）。

### BpmClock 側

```csharp
public class BpmClock : MonoBehaviour
{
    public double ElapsedSeconds { get; private set; }
    public double ElapsedBeats   { get; private set; }

    void Update()
    {
        if (!_isRunning || AudioManager.Instance == null) return;

        ElapsedSeconds = AudioManager.Instance.GetBGMElapsedSeconds();
        ElapsedBeats   = ElapsedSeconds / GameConstants.GetBeatDuration();

        DetectBeatBoundary();
    }
}
```

### ビート境界検出

```csharp
private double _prevElapsedBeats;

private void DetectBeatBoundary()
{
    int prevBeatInt = (int)_prevElapsedBeats;
    int currBeatInt = (int)ElapsedBeats;
    if (currBeatInt > prevBeatInt)
    {
        _onBeatSubject.OnNext(Unit.Default);
        if (currBeatInt % GameConstants.BEATS_PER_BAR == 0)
            _onBarSubject.OnNext(Unit.Default);
    }
    _prevElapsedBeats = ElapsedBeats;
}
```

---

## キュー長の取得

ACB 内のキューの全長を取得（カウントダウン表示などに）:

```csharp
CriAtomEx.CueInfo info;
if (_acbAsset.Handle.GetCueInfo(_cueRef.CueId, out info))
{
    long lengthMs = info.length;
    Debug.Log($"キュー長: {lengthMs}ms");
}
```

---

## 落とし穴

### ⚠️ float の精度不足

```csharp
// ❌ float で受けると精度ロス
float sec = (float)(_playback.GetTimeSyncedWithAudio() / 1_000_000.0);

// ✅ double で計算、必要なら最後に float に
double sec = _playback.GetTimeSyncedWithAudio() / 1_000_000.0;
```

### ⚠️ Status チェックを怠ると例外

```csharp
if (_playback.GetStatus() != CriAtomExPlayback.Status.Playing)
    return 0.0;
long us = _playback.GetTimeSyncedWithAudio();
```

### ⚠️ BGM 再生開始直後の 0 値

`Start()` 直後はまだ再生開始していないことがある。「Prep」状態のときは 0 やマイナスを返す可能性。`Status.Playing` 確認後に取得。

### ⚠️ ループ時の値リセット

ACB 側でループ設定がある場合、`GetTimeSyncedWithAudio()` の挙動はループ毎にリセットされる/されない を確認（ACB の Loop Type 設定次第）。**EDMQuiz の BGM はワンショット推奨**（プロトタイプは1プレイ1ループで完結）。

---

## デバッグ Tips

```csharp
[Button("Print BGM Time")]
private void DebugPrintTime()
{
    long us = _bgmPlayback.GetTimeSyncedWithAudio();
    Debug.Log($"BGM: {us / 1_000_000.0:F3}s ({us / 1000}ms)");
}
```

NaughtyAttributes の `[Button]` で Inspector からテスト可能。

---

## Acceptance Criteria

- [ ] `GetTimeSyncedWithAudio()` を使用している（`GetTime()` ではない）
- [ ] 戻り値を `double` で受ける
- [ ] BPM 計算が `double` 精度で行われる
- [ ] ビート境界検出が ±20ms 以内
- [ ] BGM 未再生時に 0 を返す

---

## 関連 Skill

- 概要: `cri-adx-overview`
- 再生制御: `cri-adx-playback`
- アセット: `cri-adx-asset-support`
- EDMQuiz 統合: `edm-quiz-audio` / `edm-quiz-bpm-sync`
