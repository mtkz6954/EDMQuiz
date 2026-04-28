---
name: edm-quiz-input-ui
description: ひらがな入力 UI (HiraganaInputUI) の実装方針。UI Toolkit でのボタン動的生成、4文字バッファ管理、フェーズ別の入力ロック、ビート連動パルスを扱うときに参照する。
---

# input-ui — ひらがな入力 UI

## 責務

UI Toolkit (UXML/USS) でひらがなボタンを動的生成し、プレイヤーの入力を 4 文字バッファに蓄積する。BuildUp フェーズ中のみ受付。

---

## UXML（game-panel.uxml）

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <Style src="../Styles/game-panel.uss"/>
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

## USS（game-panel.uss）

```css
.hiragana-grid {
    flex-direction: row;
    flex-wrap: wrap;
    justify-content: center;
    gap: 12px;
    max-width: 600px;
}

.hiragana-button {
    width: 100px;
    height: 100px;
    font-size: 48px;
    background-color: #2a2a3a;
    color: white;
    border-radius: 12px;
    border-width: 0;
}

.hiragana-button:hover { background-color: #3a3a4a; }
.hiragana-button:disabled { opacity: 0.4; }

.answer-cells {
    flex-direction: row;
    justify-content: center;
    gap: 8px;
}

.answer-cell {
    width: 80px;
    height: 80px;
    font-size: 40px;
    -unity-text-align: middle-center;
    background-color: #1a1a2a;
    border-radius: 8px;
}
```

---

## C# 実装

```csharp
using System.Collections.Generic;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    public class HiraganaInputUI : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private Label _questionText;
        private VisualElement _answerCellsContainer;
        private Label[] _answerCells;
        private VisualElement _buttonContainer;
        private Button _backspaceButton;
        private Button _confirmButton;

        private readonly List<string> _inputBuffer = new();
        private readonly List<Button> _hiraganaButtons = new();

        void OnEnable()
        {
            _root = _uiDocument.rootVisualElement;
            _questionText        = _root.Q<Label>("question-text");
            _answerCellsContainer = _root.Q<VisualElement>("answer-cells");
            _buttonContainer     = _root.Q<VisualElement>("hiragana-buttons");
            _backspaceButton     = _root.Q<Button>("backspace-button");
            _confirmButton       = _root.Q<Button>("confirm-button");

            _answerCells = _answerCellsContainer.Query<Label>(className: "answer-cell").ToList().ToArray();

            _backspaceButton.clicked += OnBackspacePressed;
            _confirmButton.clicked   += OnConfirmPressed;

            // フェーズ変化を購読
            GameFlowManager.OnPhaseChanged
                .Subscribe(HandlePhaseChanged)
                .AddTo(this);

            // ビート購読でパルス
            BpmClock.OnBeat
                .Where(_ => GameFlowManager.Instance?.CurrentPhase == GamePhase.BuildUp)
                .Subscribe(_ => PulseAllButtons())
                .AddTo(this);

            SetInputEnabled(false);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Question:
                    LoadQuestion();
                    SetInputEnabled(false);
                    break;
                case GamePhase.BuildUp:
                    SetInputEnabled(true);
                    break;
                default:
                    SetInputEnabled(false);
                    break;
            }
        }

        private void LoadQuestion()
        {
            _inputBuffer.Clear();
            UpdateAnswerDisplay();
            UpdateConfirmButton();

            var q = GameFlowManager.Instance?.CurrentQuestion;
            if (q == null) return;

            _questionText.text = q.questionText;
            BuildHiraganaButtons(q.hiraganaOptions);
        }

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

        private void OnHiraganaPressed(string kana)
        {
            if (_inputBuffer.Count >= GameConstants.ANSWER_LENGTH) return;
            _inputBuffer.Add(kana);
            UpdateAnswerDisplay();
            UpdateConfirmButton();
            AudioManager.Instance?.PlayUiTapSE();
        }

        private void OnBackspacePressed()
        {
            if (_inputBuffer.Count == 0) return;
            _inputBuffer.RemoveAt(_inputBuffer.Count - 1);
            UpdateAnswerDisplay();
            UpdateConfirmButton();
        }

        private void OnConfirmPressed()
        {
            if (_inputBuffer.Count < GameConstants.ANSWER_LENGTH) return;
            string answer = string.Concat(_inputBuffer);
            GameFlowManager.Instance.ConfirmAnswer(answer);
        }

        private void UpdateAnswerDisplay()
        {
            for (int i = 0; i < _answerCells.Length; i++)
                _answerCells[i].text = i < _inputBuffer.Count ? _inputBuffer[i] : "";
        }

        private void UpdateConfirmButton()
        {
            _confirmButton.SetEnabled(_inputBuffer.Count == GameConstants.ANSWER_LENGTH);
        }

        private void SetInputEnabled(bool enabled)
        {
            foreach (var btn in _hiraganaButtons) btn.SetEnabled(enabled);
            _backspaceButton.SetEnabled(enabled);
            // confirm は4文字時のみ有効なので別管理
            _confirmButton.SetEnabled(enabled && _inputBuffer.Count == GameConstants.ANSWER_LENGTH);
        }

        private void PulseAllButtons()
        {
            foreach (var btn in _hiraganaButtons)
                btn.DOPulse(GameConstants.BUTTON_PULSE_SCALE,
                            GameConstants.GetBeatDuration() * GameConstants.BUTTON_PULSE_DURATION_RATIO);
        }
    }
}
```

`btn.DOPulse(...)` は `UIToolkitTweenExtensions` で定義（`edm-quiz-ui-toolkit` skill 参照）。

---

## チューニング項目

```csharp
public const float BUTTON_PULSE_SCALE          = 1.08f;
public const float BUTTON_PULSE_DURATION_RATIO = 0.2f;
public const int   BUTTON_GRID_COLUMNS         = 4;  // USS の flex-wrap で実現
```

---

## Edge Cases

| ケース | 対応 |
|--------|------|
| 4文字入力済みで5文字目 | `if (count >= ANSWER_LENGTH) return;` |
| 確定を3文字以下で押下 | `confirmButton.SetEnabled(false)` で UI 側でブロック |
| Drop フェーズで連打 | `SetInputEnabled(false)` で全 button.SetEnabled(false) |
| 問題切り替え時のバッファ残留 | `LoadQuestion()` で `_inputBuffer.Clear()` |

---

## Acceptance Criteria

- [ ] UXML がロードされ、`Q<Button>` で参照取得できる
- [ ] ひらがなボタンが `hiraganaOptions` の数だけ動的生成
- [ ] 4文字入力で確定ボタンが有効化
- [ ] バックスペースで1文字削除
- [ ] BuildUp 以外で全ボタン disabled
- [ ] OnBeat に同期してボタンがパルス
- [ ] 問題切り替え時に表示・バッファがリセット

---

## 関連 Skill

- UI 全般: `edm-quiz-ui-toolkit`
- データ: `edm-quiz-quiz-data`
- フロー: `edm-quiz-game-flow`
- BPM: `edm-quiz-bpm-sync`
- 非同期: `edm-quiz-async-reactive`
