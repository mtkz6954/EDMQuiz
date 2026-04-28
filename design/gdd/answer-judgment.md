# GDD — answer-judgment

**System Slug**: `answer-judgment`
**Layer**: Core（quiz-data に依存）
**Last Updated**: 2026-04-27
**Status**: Approved

---

## 1. Overview

プレイヤーが組み立てた4文字ひらがな文字列と、`QuizQuestion.correctAnswer` を照合して正誤を判定するシステム。完全一致のみ正解。判定結果（正解 / 不正解）を `game-flow` へ通知し、`presentation-vfx` の演出トリガーとなる。

---

## 2. Player Fantasy

「確定ボタンを押した一瞬」の緊張と解放を支える判定精度。迷いながら選んだ4文字が合っていた瞬間の爽快感、外れた瞬間の落差——その体験の鮮明さはここの判定速度と正確さにかかっている。

---

## 3. Detailed Rules

### 判定ロジック

```
入力: inputBuffer (string, 4文字)
正解: QuizQuestion.correctAnswer (string, 4文字)

isCorrect = (inputBuffer == correctAnswer)
```

- 大文字・小文字の区別なし（ひらがなのみなので実質不問）
- 前後スペースのトリム処理なし（UI 側が保証するため不要）
- 部分一致・前方一致は不正解扱い

### 判定タイミング

- `GamePhase.Drop` への遷移時に自動発火（`GameFlowManager` が呼び出す）
- 手動確定ボタン押下でも即時発火可能（BuildUp フェーズ中に早押し確定）

### 判定結果の通知

```csharp
// static event で通知
public static event Action<bool> OnAnswerJudged;  // true = 正解
```

`GameFlowManager`・`VFXDirector`・`ScoreManager` が購読する。

### 入力が空・3文字以下の場合

- 不正解として即時判定（`isCorrect = false`）
- `inputBuffer` の長さチェックを判定前に行う

---

## 4. Formulas

```
isCorrect = (inputBuffer.Length == ANSWER_LENGTH) && (inputBuffer == correctAnswer)
```

| 変数 | 型 | 説明 |
|------|----|------|
| `inputBuffer` | string | プレイヤーが組み立てた文字列（4文字以下） |
| `correctAnswer` | string | `QuizQuestion.correctAnswer`（4文字） |
| `ANSWER_LENGTH` | int | 4（GameConstants 定数） |

---

## 5. Edge Cases

| ケース | 対応 |
|--------|------|
| 4文字未入力で Drop フェーズに突入 | `inputBuffer.Length < ANSWER_LENGTH` → 不正解扱い |
| 確定ボタン連打 | `_isJudged` フラグで2回目以降の判定をスキップ |
| 判定後に入力バッファが変更される | 判定と同時に `input-ui` の入力をロックする（`OnAnswerJudged` 受信側が処理） |
| `correctAnswer` が null / 空 | `OnValidate()` でデータ側が保証済み → 発生しない想定、防衛的に `false` 返却 |

---

## 6. Dependencies

| 方向 | システム | 内容 |
|------|---------|------|
| 読み取り | `quiz-data` | `QuizQuestion.correctAnswer` を参照 |
| 読み取り | `input-ui` | `inputBuffer`（現在の入力文字列）を受け取る |
| 通知先 | `game-flow` | `OnAnswerJudged` イベントで正誤を通知 |
| 通知先 | `presentation-vfx` | 同イベントで演出トリガー |
| 通知先 | `score-result` | 同イベントで正解カウントをインクリメント |

---

## 7. Tuning Knobs

| 定数名 | デフォルト値 | 説明 |
|--------|------------|------|
| `ANSWER_LENGTH` | 4 | 判定する文字数（GameConstants 参照） |

チューニング要素はほぼなし（判定ロジックは完全一致固定）。

---

## 8. Acceptance Criteria

- [ ] 正解文字列と完全一致する入力で `OnAnswerJudged(true)` が発火する
- [ ] 1文字でも異なる入力で `OnAnswerJudged(false)` が発火する
- [ ] 4文字未満の入力（例: 3文字）で `OnAnswerJudged(false)` が発火する
- [ ] 確定ボタンを連打しても `OnAnswerJudged` が2回以上発火しない
- [ ] 単体テスト: 正解・不正解・空入力・3文字入力の4ケースがパスする
