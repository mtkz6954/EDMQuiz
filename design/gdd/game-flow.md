# GDD — game-flow

**System Slug**: `game-flow`
**Layer**: Orchestration（quiz-data・answer-judgment・audio・bpm-sync に依存）
**Last Updated**: 2026-04-27
**Status**: Approved

---

## 1. Overview

ゲーム全体のフェーズ遷移・タイマー制御・問題進行を管理するシステム。`GameFlowManager` が唯一の状態機械として機能し、`GamePhase` enum で現在のフェーズを管理する。他システムはイベント購読でフェーズ変化を受け取り、自律的に動作する。

---

## 2. Player Fantasy

「問題表示→ビルドアップ→ドロップ→次の問題」のリズムが心地よく繰り返され、プレイヤーは「次は何が来るか」を体で覚えてくる。タイミングのズレや待ち時間のもたつきが緊張感を壊す——フロー管理が正確であるほど「乗れている感」が持続する。

---

## 3. Detailed Rules

### フェーズ定義

```csharp
public enum GamePhase
{
    Idle,       // 未開始（タイトル画面）
    Question,   // 問題文表示中
    BuildUp,    // ひらがな入力可能期間
    Drop,       // 正誤判定・演出発火
    Next,       // 次問題への遷移
    GameEnd     // 結果画面
}
```

### フェーズ遷移フロー

```
Idle
 → Question    スタートボタン押下 + BGM開始
 → BuildUp     QUESTION_PHASE_SEC 経過
 → Drop        BUILDUP_PHASE_SEC 経過 OR 確定ボタン押下
 → Next        DROP_REVEAL_SEC 経過（正誤判定完了後）
 → Question    NEXT_TRANSITION_SEC 経過（問題 index + 1）
 → GameEnd     5問完了後
```

### フェーズ変化の通知

```csharp
public static event Action<GamePhase> OnPhaseChanged;
```

全システムがこのイベントを購読してフェーズに応じた動作を切り替える。

### 問題進行

- `_questionIndex`（0〜4）で現在の問題を管理
- `QuizDatabase.questions[_questionIndex]` で問題データを取得
- `Next` フェーズ完了時に `_questionIndex++`、5問完了で `GameEnd`

### タイマー管理

- 各フェーズの持続時間は `GameConstants` の定数を参照
- `Update()` 内で `_phaseTimer` を加算し、閾値超過で次フェーズへ遷移
- Drop フェーズのみ、タイマーではなく `OnAnswerJudged` 受信後に遷移開始

---

## 4. Formulas

```
// フェーズタイマー
_phaseTimer += Time.deltaTime

// Question → BuildUp
if (_phaseTimer >= QUESTION_PHASE_SEC) → TransitionTo(BuildUp)

// BuildUp → Drop（タイムアウト）
if (_phaseTimer >= BUILDUP_PHASE_SEC) → TransitionTo(Drop)

// Drop → Next（判定完了後）
OnAnswerJudged 受信 → _phaseTimer リセット → DROP_REVEAL_SEC 後 → TransitionTo(Next)

// Next → Question or GameEnd
if (_questionIndex < TOTAL_QUESTIONS) → TransitionTo(Question)
else → TransitionTo(GameEnd)
```

| 定数 | 値 | 説明 |
|------|----|------|
| `QUESTION_PHASE_SEC` | 2.0f | 問題文表示秒数 |
| `BUILDUP_PHASE_SEC` | 24.0f | ビルドアップ秒数 |
| `DROP_REVEAL_SEC` | 4.0f | 演出発火〜次遷移までの秒数 |
| `NEXT_TRANSITION_SEC` | 1.5f | 次問題への遷移秒数 |
| `TOTAL_QUESTIONS` | 5 | 総問題数 |

---

## 5. Edge Cases

| ケース | 対応 |
|--------|------|
| 確定ボタンを BuildUp 中に早押し | `TransitionTo(Drop)` を即時呼び出し（タイマー無視） |
| `QuizDatabase` が null / 空 | `Idle` 状態でエラーログ、遷移せずタイトルに留まる |
| `Drop` 中に `OnAnswerJudged` が2回届く | `_isJudged` フラグで2回目以降を無視 |
| `GameEnd` 後にフェーズ遷移が呼ばれる | ガード節で無視（`if (_phase == GamePhase.GameEnd) return`） |
| BGM 未再生のまま Question フェーズに入る | `AudioManager` が PlayBGM 済みであることを前提（Idle → Question 遷移の事前条件） |

---

## 6. Dependencies

| 方向 | システム | 内容 |
|------|---------|------|
| 読み取り | `quiz-data` | `QuizDatabase` から問題を取得 |
| 呼び出し | `answer-judgment` | Drop 遷移時に判定を要求 |
| 呼び出し | `audio` | Idle → Question 遷移時に `PlayBGM()` |
| 購読 | `bpm-sync` | `OnBar` でビルドアップ終了タイミングを補助（オプション） |
| 通知先 | `input-ui` | `OnPhaseChanged` で入力の有効/無効を切り替え |
| 通知先 | `presentation-vfx` | `OnPhaseChanged` で演出を起動 |
| 通知先 | `score-result` | `OnPhaseChanged(GameEnd)` で結果画面を表示 |

---

## 7. Tuning Knobs

| 定数名 | デフォルト値 | 説明 |
|--------|------------|------|
| `QUESTION_PHASE_SEC` | 2.0f | 問題文表示時間 |
| `BUILDUP_PHASE_SEC` | 24.0f | 入力可能時間 |
| `DROP_REVEAL_SEC` | 4.0f | 演出表示時間 |
| `NEXT_TRANSITION_SEC` | 1.5f | 問題間の間隔 |
| `TOTAL_QUESTIONS` | 5 | 問題数 |

---

## 8. Acceptance Criteria

- [ ] タイトル→5問→結果画面まで通しで遷移できる
- [ ] `OnPhaseChanged` が各フェーズで1回ずつ発火する
- [ ] 確定ボタン早押しで BuildUp → Drop が即時遷移する
- [ ] 5問完了後に `GameEnd` へ遷移し、それ以上遷移しない
- [ ] `QuizDatabase` が null のとき、遷移せずエラーログが出る
