# GDD — input-ui

**System Slug**: `input-ui`
**Layer**: Feature（quiz-data・game-flow・bpm-sync に依存）
**Last Updated**: 2026-04-27
**Status**: Approved

---

## 1. Overview

ひらがな選択ボタン群を表示し、プレイヤーの入力（タップ / クリック）を受け付けて4文字の入力バッファを組み立てるシステム。UGUI `Button` + `TextMeshPro` で構成し、`answer-judgment` が参照する `inputBuffer` を管理する。BuildUp フェーズ中のみ入力を受け付け、Drop フェーズ以降はロックする。

---

## 2. Player Fantasy

ビートに乗りながら直感でひらがなを選ぶ「リズム感のある入力体験」。ボタンがビートでパルスし、選んだ文字が画面上部に積み上がっていく視覚フィードバックが「組み立てている感」を生む。誤入力はバックスペースで消せる安心感も重要。

---

## 3. Detailed Rules

### UI 構成

```
[入力表示エリア]     ← 組み立て中の4文字を表示（_inputBuffer）
[ひらがなボタン群]   ← QuizQuestion.hiraganaOptions から動的生成（5〜8個）
[バックスペースボタン] ← 最後の1文字を削除
[確定ボタン]         ← Drop フェーズへ即時遷移（BuildUp 中のみ有効）
```

### 入力バッファ管理

```csharp
private string _inputBuffer = "";  // 最大4文字

// ひらがなボタン押下
void OnHiraganaPressed(string kana)
{
    if (_inputBuffer.Length >= ANSWER_LENGTH) return;  // 4文字上限
    _inputBuffer += kana;
    UpdateDisplay();
}

// バックスペース
void OnBackspacePressed()
{
    if (_inputBuffer.Length == 0) return;
    _inputBuffer = _inputBuffer[..^1];
    UpdateDisplay();
}
```

### ボタンの動的生成

- `GameFlowManager.OnPhaseChanged(Question)` を受けて、`QuizQuestion.hiraganaOptions` からボタンを生成
- ボタンは Prefab（`HiraganaButton.prefab`）をインスタンス化してグリッドに並べる
- 問題切り替え時に前問のボタン群を `Destroy` し、新しいボタン群を生成

### フェーズごとの入力制御

| フェーズ | ひらがなボタン | 確定ボタン | バックスペース |
|---------|--------------|-----------|--------------|
| Idle / Question | 無効（非表示） | 無効 | 無効 |
| BuildUp | 有効 | 有効（4文字のとき推奨） | 有効 |
| Drop 以降 | 無効（ロック） | 非表示 | 無効 |

### ビートパルス演出

- `BpmClock.OnBeat` を購読し、ひらがなボタン全体を DOTween でスケールパルス
- パルスは UGUI の `RectTransform.DOScale()` を使用

---

## 4. Formulas

```
// 確定ボタンの推奨状態（4文字入力時にハイライト）
isReady = (_inputBuffer.Length == ANSWER_LENGTH)

// ボタン生成数
buttonCount = QuizQuestion.hiraganaOptions.Length  // 5〜8
```

| 変数 | 説明 |
|------|------|
| `ANSWER_LENGTH` | 4（GameConstants） |
| `_inputBuffer` | string（最大4文字） |

---

## 5. Edge Cases

| ケース | 対応 |
|--------|------|
| 4文字入力済みでさらにタップ | `_inputBuffer.Length >= ANSWER_LENGTH` で無視 |
| 確定ボタンを0〜3文字で押下 | `answer-judgment` が不正解判定（UI側は止めない） |
| ボタン連打（タップ・クリック） | `Button.interactable` を Drop フェーズで `false` に設定 |
| 問題切り替え時のバッファ残留 | `OnPhaseChanged(Question)` で `_inputBuffer = ""` にリセット |
| ひらがな選択肢が5未満 / 8超（データ不備） | `OnValidate()` でデータ側が検出済み。UI 側は受け取った数をそのまま表示 |

---

## 6. Dependencies

| 方向 | システム | 内容 |
|------|---------|------|
| 読み取り | `quiz-data` | `QuizQuestion.hiraganaOptions` でボタン生成 |
| 購読 | `game-flow` | `OnPhaseChanged` で入力の有効/無効・バッファリセット |
| 購読 | `bpm-sync` | `OnBeat` でパルスアニメーション |
| 提供 | `answer-judgment` | `_inputBuffer` の参照を渡す（または確定時にコールバック） |

---

## 7. Tuning Knobs

| 定数名 | デフォルト値 | 説明 |
|--------|------------|------|
| `ANSWER_LENGTH` | 4 | 入力上限文字数 |
| `PULSE_SCALE` | 1.08f | ビートパルス時のスケール倍率 |
| `PULSE_DURATION` | 0.1f | パルスアニメーション秒数 |
| `BUTTON_GRID_COLUMNS` | 4 | ひらがなボタンのグリッド列数 |

---

## 8. Acceptance Criteria

- [ ] ひらがなボタンが `hiraganaOptions` の数だけ表示される
- [ ] ボタンをタップ / クリックすると入力表示エリアに文字が追加される
- [ ] 4文字入力後、5文字目のタップが無視される
- [ ] バックスペースで最後の1文字が削除される
- [ ] Drop フェーズ開始後、ボタンが全て無効化される
- [ ] `OnBeat` に合わせてボタン群がパルスアニメーションする
- [ ] 問題切り替え時に入力バッファと表示がリセットされる
