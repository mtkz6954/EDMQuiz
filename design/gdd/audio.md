# GDD — audio

**System Slug**: `audio`
**Layer**: Foundation（依存なし）
**Last Updated**: 2026-04-27
**Status**: Approved

---

## 1. Overview

CRI ADX LE + Asset Support Addon を使い、BGM と SE を WebGL / OnMemory 方式で再生するシステム。`AudioManager` シングルトンが全音声リソースのライフサイクルを管理し、他システムは `AudioManager` の API を通じてのみ音声を操作する。StreamingAssets は使用禁止（unityroom WebGL 制約）。

---

## 2. Player Fantasy

BGM が鳴り出した瞬間から身体がリズムに乗り、正解 SE・不正解 SE・歓声・ブーイングが演出と完全に同期して「全身で爆発する」体験を作る。音が遅れたり途切れたりすると爆発感が消える——音のタイトさがゲーム全体のテンポ感を決める。

---

## 3. Detailed Rules

### デプロイ方式

| 項目 | 設定 |
|------|------|
| CRI ADX プラグイン | CRI ADX LE Unity Plugin v3.13+ |
| アドオン | Asset Support Addon v1.2+ |
| デプロイタイプ | OnMemory（StreamingAssets 禁止） |
| アセット配置 | `Assets/SoundData/`（`_EDMQuiz/` 外に配置） |
| ACB/AWB ファイル | `Assets/SoundData/` 直下 |

### 音声リソース構成（プロトタイプ）

| キュー名 | 種別 | 用途 |
|---------|------|------|
| `BGM_MAIN` | BGM | ゲーム中ループBGM（1曲） |
| `SE_CORRECT` | SE | 正解時・歓声 |
| `SE_INCORRECT` | SE | 不正解時・ブーイング |
| `SE_UI_TAP` | SE | ひらがなボタンタップ音 |
| `SE_RESULT` | SE | 結果画面表示音 |

### AudioManager の責務

```csharp
// BGM
void PlayBGM()          // タイトル画面のボタン押下後に呼ぶ
void StopBGM()          // ゲーム終了・シーン遷移時
float GetBGMDspTime()   // BpmClock が参照する基準クロック

// SE
void PlaySE(string cueName)

// ライフサイクル
// DontDestroyOnLoad で TitleScene から GameScene をまたいで維持
```

### BGM 開始タイミング

- タイトル画面のスタートボタン押下後に `PlayBGM()` を呼ぶ（WebGL 自動再生ポリシー対応）
- BGM 開始と同時に `BpmClock` がクロック計測を開始する

### フォールバック

- BGM 読み込み失敗時: 無音で続行。`BpmClock` は `Time.unscaledTime` にフォールバック
- SE 再生失敗時: ログのみ出力、ゲーム続行

---

## 4. Formulas

```
// BPM同期クロックの取得（BpmClock が使用する）
elapsedSeconds = CriAtomExPlayback.GetTime() / 1000f

// BGM開始からの経過ビート数
elapsedBeats = elapsedSeconds * (BPM / 60f)
```

BPM 値と詳細な同期計算は `bpm-sync` GDD に委譲。

---

## 5. Edge Cases

| ケース | 対応 |
|--------|------|
| WebGL 自動再生ブロック | スタートボタン押下後にのみ `PlayBGM()` を呼ぶ |
| BGM 読み込み失敗 | 無音続行、`BpmClock` は `Time.unscaledTime` にフォールバック |
| SE 重複再生（ボタン連打） | CRI ADX 側でポリシー設定（同一キューは新しい再生を優先） |
| シーン遷移時の音声途切れ | `DontDestroyOnLoad` で `AudioManager` を維持し、BGM を継続再生 |
| WebGL シングルスレッドによる負荷 | DSPバスエフェクト最小限、HCAコーデック使用で CPU 負荷を抑制 |

---

## 6. Dependencies

| 方向 | システム | 内容 |
|------|---------|------|
| 通知受信 | `game-flow` | フェーズ変化イベントで SE を再生 |
| 通知受信 | `answer-judgment` | `OnAnswerJudged` で正解/不正解 SE を再生 |
| 提供 | `bpm-sync` | `GetBGMDspTime()` で基準クロックを供給 |

---

## 7. Tuning Knobs

| 定数名 | デフォルト値 | 説明 |
|--------|------------|------|
| `BGM_VOLUME` | 1.0f | BGM マスターボリューム |
| `SE_VOLUME` | 1.0f | SE マスターボリューム |
| `BGM_CUE_NAME` | `"BGM_MAIN"` | 再生する BGM キュー名 |

コーデックは ADX2 ツールで設定（HCA 推奨）。Unity 側での変更は不可。

---

## 8. Acceptance Criteria

- [ ] WebGL ビルドで BGM がスタートボタン押下後に再生される
- [ ] 正解時に `SE_CORRECT`、不正解時に `SE_INCORRECT` が再生される
- [ ] TitleScene → GameScene のシーン遷移で BGM が途切れない
- [ ] BGM 読み込み失敗時に無音でゲームが続行される（コンソールにエラーログのみ）
- [ ] `GetBGMDspTime()` が呼び出せ、正の浮動小数点値を返す
