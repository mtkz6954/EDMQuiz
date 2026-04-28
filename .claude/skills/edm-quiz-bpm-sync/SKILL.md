---
name: edm-quiz-bpm-sync
description: BPM 同期クロック (BpmClock) の実装方針。CRI ADX dspTime を基準に OnBeat/OnBar イベントを R3 で発火する仕組み。BPM・小節数・秒数の整合計算、ビート連動アニメ、フェーズ遷移タイミングを扱うときに参照する。
---

# bpm-sync — BPM 同期クロック

## 責務

CRI ADX の `CriAtomExPlayback.GetTime()` を基準に経過ビート数・小節数を算出し、`OnBeat` / `OnBar` を R3 Subject で発火する。

---

## 基準クロック

```
elapsedSeconds = AudioManager.GetBGMElapsedSeconds()
              = CriAtomExPlayback.GetTime() / 1000f
```

BGM 未再生時は 0、フォールバック時は `Time.unscaledTime` ベース。

---

## 計算式

```csharp
beatDuration = 60f / BPM
elapsedBeats = elapsedSeconds / beatDuration
currentBeat  = (int)elapsedBeats % BEATS_PER_BAR
currentBar   = (int)(elapsedBeats / BEATS_PER_BAR)
```

| 変数 | 値（BPM=128） |
|------|---------------|
| beatDuration | 0.46875 sec |
| 1小節 | 1.875 sec（4拍） |
| 16小節 | 30 sec |

---

## BPM × 秒数の整合（重要）

`BUILDUP_PHASE_SEC` は固定値ではなく **BPM × BUILDUP_BARS から動的計算**する:

```csharp
// GameConstants.cs
public const float BPM            = 128f;
public const int   BEATS_PER_BAR  = 4;
public const int   BUILDUP_BARS   = 16;

public static float GetBeatDuration()       => 60f / BPM;
public static float GetBuildUpDurationSec() => BUILDUP_BARS * BEATS_PER_BAR * GetBeatDuration();
```

| BPM | BARS | 結果（秒） |
|-----|------|-----------|
| 128 | 16   | 30.0 |
| 140 | 16   | 27.4 |
| 128 | 8    | 15.0 |

BGM 確定後に `BUILDUP_BARS` を最終調整。

---

## 実装

```csharp
using R3;
using UnityEngine;

namespace EDMQuiz
{
    public class BpmClock : MonoBehaviour
    {
        public static BpmClock Instance { get; private set; }

        public float ElapsedSeconds { get; private set; }
        public float ElapsedBeats   { get; private set; }
        public int   CurrentBeat    { get; private set; }
        public int   CurrentBar     { get; private set; }

        private static readonly Subject<Unit> _onBeatSubject = new();
        private static readonly Subject<Unit> _onBarSubject  = new();
        public static Observable<Unit> OnBeat => _onBeatSubject;
        public static Observable<Unit> OnBar  => _onBarSubject;

        private bool _isRunning;
        private float _prevElapsedBeats;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartClock()
        {
            _isRunning = true;
            _prevElapsedBeats = 0f;
        }

        public void StopClock()
        {
            _isRunning = false;
        }

        void Update()
        {
            if (!_isRunning) return;

            ElapsedSeconds = AudioManager.Instance != null
                ? AudioManager.Instance.GetBGMElapsedSeconds()
                : 0f;

            float beatDuration = GameConstants.GetBeatDuration();
            ElapsedBeats = ElapsedSeconds / beatDuration;
            CurrentBeat  = (int)ElapsedBeats % GameConstants.BEATS_PER_BAR;
            CurrentBar   = (int)(ElapsedBeats / GameConstants.BEATS_PER_BAR);

            DetectBoundaries();
            _prevElapsedBeats = ElapsedBeats;
        }

        private void DetectBoundaries()
        {
            int prevBeatInt = (int)_prevElapsedBeats;
            int currBeatInt = (int)ElapsedBeats;
            if (currBeatInt > prevBeatInt)
            {
                _onBeatSubject.OnNext(Unit.Default);
                if (currBeatInt % GameConstants.BEATS_PER_BAR == 0)
                    _onBarSubject.OnNext(Unit.Default);
            }
        }
    }
}
```

---

## 購読パターン

### ビートに合わせて UI をパルス

```csharp
BpmClock.OnBeat
    .Where(_ => _phase == GamePhase.BuildUp)
    .Subscribe(_ => _backgroundElement.DOPulse(1.03f, 0.16f))
    .AddTo(this);
```

### 小節境界でログ

```csharp
BpmClock.OnBar
    .Subscribe(_ => Debug.Log($"Bar {BpmClock.Instance.CurrentBar}"))
    .AddTo(this);
```

---

## 開始タイミング

`AudioManager.PlayBGM()` の直後に `BpmClock.Instance.StartClock()` を呼ぶ:

```csharp
// TitleScene の Start ボタン
AudioManager.Instance.PlayBGM();
BpmClock.Instance.StartClock();
SceneManager.LoadScene("GameScene");
```

`BpmClock` を持つ GameObject も `DontDestroyOnLoad` でシーン跨ぎ可能（または GameScene にも常駐させる）。

---

## Edge Cases

| ケース | 対応 |
|--------|------|
| BGM 未再生で参照 | `_isRunning = false` で 0 を返す |
| BGM 読み込み失敗 | `AudioManager` 側で `Time.unscaledTime` フォールバック |
| フレームレート低下でビート飛ばし | 複数ビート分一括検出、最後の1回のみ `OnBeat` 発火 |
| BPM = 0 | `Mathf.Max(BPM, 1f)` でガード |

---

## 単体テスト

```csharp
[Test]
public void GetBuildUpDurationSec_BPM128_Bars16_Returns30()
{
    // BPM=128, BARS=16 → 30秒
    Assert.AreEqual(30f, GameConstants.GetBuildUpDurationSec(), 0.01f);
}

[Test]
public void GetBeatDuration_BPM120_Returns0_5()
{
    // 注: BPM は const なので、テスト用には GameConstants を見直す
}
```

---

## チューニング項目

| 定数 | デフォルト | 説明 |
|------|----------|------|
| `BPM` | 128 | BGM のテンポ |
| `BEATS_PER_BAR` | 4 | 4/4拍子 |
| `BUILDUP_BARS` | 16 | ビルドアップ小節数 |

---

## Acceptance Criteria

- [ ] BGM 再生中に `ElapsedSeconds` が単調増加
- [ ] BPM=128 で `OnBeat` が約 0.469s 間隔で発火（±20ms）
- [ ] `OnBar` が4ビートごとに発火
- [ ] BGM 読み込み失敗時に `Time.unscaledTime` で動作
- [ ] `GetBuildUpDurationSec()` が BPM 連動で正しい値を返す

---

## 関連 Skill

- 音声: `edm-quiz-audio`
- フロー: `edm-quiz-game-flow`
- 演出: `edm-quiz-presentation-vfx`
- 入力: `edm-quiz-input-ui`
