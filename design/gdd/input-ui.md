# GDD — input-ui

**System Slug**: `input-ui`
**Layer**: Feature（quiz-data・game-flow・bpm-sync に依存）
**Last Updated**: 2026-04-28
**Status**: Approved（UI Toolkit 化に伴い改訂）

---

## 1. Overview

ひらがな選択ボタン群を表示し、プレイヤーの入力（タップ / クリック）を受け付けて4文字の入力バッファを組み立てるシステム。**UI Toolkit (UXML/USS) + DOTween** で構成し、`AnswerJudgment.Judge()` に渡す入力文字列を管理する。BuildUp フェーズ中のみ入力を受け付け、Drop フェーズ以降はロックする。

---

## 2. Player Fantasy

ビートに乗りながら直感でひらがなを選ぶ「リズム感のある入力体験」。ボタンがビートでパルスし、選んだ文字が画面上部に積み上がっていく視覚フィードバックが「組み立てている感」を生む。誤入力はバックスペースで消せる安心感も重要。

---

## 3. Detailed Rules

### UXML 構成（`game-panel.uxml`）

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <ui:VisualElement name="game-root" class="game-root">
    <ui:Label name="question-text" class="question-text"/>
    <ui:VisualElement name="answer-cells" class="answer-cells">
      <ui:Label class="answer-cell"/>
      <ui:Label class="answer-cell"/>
      <ui:Label class="answer-cell"/>
      <ui:Label class="answer-cell"/>
    </ui:VisualElement>
    <ui:VisualElement name="hiragana-buttons" class="hiragana-grid"/>
    <ui:VisualElement name="control-row" class="control-row">
      <ui:Button name="backspace-button" text="←" class="control-button"/>
      <ui:Button name="confirm-button" text="決定" class="control-button confirm-button"/>
    </ui:VisualElement>
  </ui:VisualElement>
</ui:UXML>
```

USS（`game-panel.uss`）でグリッドレイアウト・ビート連動アニメーションのスタイルを定義。

### 入力バッファ管理

```csharp
private readonly List<string> _inputBuffer = new();  // 最大4文字

private void OnHiraganaPressed(string kana)
{
    if (_inputBuffer.Count >= GameConstants.ANSWER_LENGTH) return;
    _inputBuffer.Add(kana);
    UpdateAnswerDisplay();
    UpdateConfirmButton();
}

private void OnBackspacePressed()
{
    if (_inputBuffer.Count == 0) return;
    _inputBuffer.RemoveAt(_inputBuffer.Count - 1);
    UpdateAnswerDisplay();
    UpdateConfirmButton();
}
```

### ボタンの動的生成

```csharp
private void BuildHiraganaButtons(string[] options)
{
    _buttonContainer.Clear();
    _hiraganaButtons.Clear();
    foreach (var ch in options)
    {
        string captured = ch;
        var btn = new Button(() => OnHiraganaPressed(captured)) { text = ch };
        btn.AddToClassList("hiragana-button");
        _buttonContainer.Add(btn);
        _hiraganaButtons.Add(btn);
    }
}
```

問題切り替え時に `_buttonContainer.Clear()` で既存ボタンを破棄して再生成。

### フェーズごとの入力制御

| フェーズ | ひらがなボタン | 確定ボタン | バックスペース |
|---------|--------------|-----------|--------------|
| Idle / Question | `SetEnabled(false)` | 無効 | 無効 |
| BuildUp | `SetEnabled(true)` | 4文字時のみ有効 | 有効 |
| Drop 以降 | `SetEnabled(false)` | 非表示（`display: none`） | 無効 |

### ビートパルス演出

`BpmClock.OnBeat`（R3 `Observable<Unit>`）を購読し、ボタンに `is-pulsing` USS クラスを一瞬付与する、または DOTween + DOVirtual.Float でスケール補間:

```csharp
BpmClock.OnBeat
    .Where(_ => _currentPhase == GamePhase.BuildUp)
    .Subscribe(_ => PulseAllButtons())
    .AddTo(this);

private void PulseAllButtons()
{
    foreach (var btn in _hiraganaButtons)
    {
        DOVirtual.Float(1f, 1.08f, _beatInterval * 0.2f, v =>
        {
            btn.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1)));
        }).SetLoops(2, LoopType.Yoyo);
    }
}
```

### 入力購読の R3 化

`Button.clicked` を Subject に流すことで「BuildUp 中のみ」「ロック中は無視」をストリーム合成で書ける:

```csharp
var hiraganaSubject = new Subject<string>();
btn.clicked += () => hiraganaSubject.OnNext(captured);

hiraganaSubject
    .Where(_ => _currentPhase == GamePhase.BuildUp)
    .Where(_ => _inputBuffer.Count < GameConstants.ANSWER_LENGTH)
    .Subscribe(OnHiraganaPressed)
    .AddTo(this);
```

---

## 4. Formulas

```
isReady    = (_inputBuffer.Count == GameConstants.ANSWER_LENGTH)
buttonCount = QuizQuestion.hiraganaOptions.Length  // 5〜8
```

| 変数 | 説明 |
|------|------|
| `ANSWER_LENGTH` | 4（GameConstants） |
| `_inputBuffer` | List<string>（最大4要素） |

---

## 5. Edge Cases

| ケース | 対応 |
|--------|------|
| 4文字入力済みでさらにタップ | `_inputBuffer.Count >= ANSWER_LENGTH` で無視（R3 `Where` で弾く） |
| 確定ボタンを0〜3文字で押下 | UI 側で `confirm-button` を 4 文字未満では `SetEnabled(false)` |
| ボタン連打 | `SetEnabled(false)` で UI Toolkit が自動的にイベントを抑制 |
| 問題切り替え時のバッファ残留 | `OnPhaseChanged(Question)` で `_inputBuffer.Clear()` |
| ひらがな選択肢が5未満 / 8超 | `OnValidate()` でデータ側が検出済み。UI 側は受け取った数をそのまま表示 |

---

## 6. Dependencies

| 方向 | システム | 内容 |
|------|---------|------|
| 読み取り | `quiz-data` | `QuizQuestion.hiraganaOptions` でボタン生成 |
| 購読 | `game-flow` | `OnPhaseChanged` で入力の有効/無効・バッファリセット |
| 購読 | `bpm-sync` | `OnBeat` でパルスアニメーション |
| 提供 | `answer-judgment` | 確定時に文字列を渡して `Judge()` を呼ぶ |

---

## 7. Tuning Knobs

| 定数名 | デフォルト値 | 説明 |
|--------|------------|------|
| `ANSWER_LENGTH` | 4 | 入力上限文字数 |
| `BUTTON_PULSE_SCALE` | 1.08f | ビートパルス時のスケール倍率 |
| `BUTTON_PULSE_DURATION_RATIO` | 0.2f | ビート間隔に対するパルス長の比率 |
| `BUTTON_GRID_COLUMNS` | 4 | ひらがなボタンのグリッド列数（USS で `grid-template-columns: repeat(4, 1fr)`） |

---

## 8. Acceptance Criteria

- [ ] UXML で `game-panel.uxml` がロードされ、UIDocument に紐付く
- [ ] ひらがなボタンが `hiraganaOptions` の数だけ表示される
- [ ] ボタンをタップ / クリックすると入力表示エリアに文字が追加される
- [ ] 4文字入力後、5文字目のタップが無視される
- [ ] バックスペースで最後の1文字が削除される
- [ ] Drop フェーズ開始後、ボタンが全て `SetEnabled(false)` になる
- [ ] `OnBeat` に合わせてボタン群がパルスアニメーションする
- [ ] 問題切り替え時に入力バッファと表示がリセットされる
- [ ] R3 の `AddTo(this)` で disposable が自動 dispose される
