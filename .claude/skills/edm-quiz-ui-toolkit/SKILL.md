---
name: edm-quiz-ui-toolkit
description: UI Toolkit (UXML/USS) の実装パターン集。UIDocument の使い方、DOTween との連携 (DOVirtual)、動的ボタン生成、フェード/スケール演出を実装するときに参照する。
---

# UI Toolkit 実装パターン — EDMQuiz

## 基本構成

```
Assets/_EDMQuiz/UI/
├── Layouts/                  # *.uxml
│   ├── title-panel.uxml
│   ├── game-panel.uxml
│   └── result-panel.uxml
├── Styles/                   # *.uss
│   ├── common.uss
│   ├── game-panel.uss
│   └── result-panel.uss
├── Themes/                   # ThemeStyleSheet (必要時のみ)
└── Fonts/                    # TMP Font Asset
```

シーンには `UIDocument` を持つ GameObject を1つ配置し、`Source Asset` に UXML を割り当てる。

---

## UXML 例（game-panel.uxml）

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xmlns:uie="UnityEditor.UIElements"
         editor-extension-mode="False">
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

---

## C# 側の参照取得

```csharp
public class HiraganaInputUI : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    private VisualElement _root;
    private Label _questionText;
    private VisualElement _answerCells;
    private VisualElement _buttonContainer;
    private Button _backspaceButton;
    private Button _confirmButton;

    void Awake()
    {
        _root = _uiDocument.rootVisualElement;
        _questionText    = _root.Q<Label>("question-text");
        _answerCells     = _root.Q<VisualElement>("answer-cells");
        _buttonContainer = _root.Q<VisualElement>("hiragana-buttons");
        _backspaceButton = _root.Q<Button>("backspace-button");
        _confirmButton   = _root.Q<Button>("confirm-button");

        _backspaceButton.clicked += OnBackspacePressed;
        _confirmButton.clicked   += OnConfirmPressed;
    }
}
```

---

## 動的ボタン生成

```csharp
private void BuildHiraganaButtons(string[] options)
{
    _buttonContainer.Clear();
    foreach (var ch in options)
    {
        string captured = ch;
        var btn = new Button(() => OnHiraganaPressed(captured)) { text = ch };
        btn.AddToClassList("hiragana-button");
        _buttonContainer.Add(btn);
    }
}
```

---

## USS パターン

### グリッドレイアウト

```css
.hiragana-grid {
    flex-direction: row;
    flex-wrap: wrap;
    justify-content: center;
    gap: 12px;
    /* UI Toolkit には CSS Grid がないため flex-wrap で代替 */
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
.hiragana-button:active { scale: 0.95; }
```

### 状態切り替え（USS クラス）

```css
.is-disabled { opacity: 0.4; }
.is-pulsing  { scale: 1.08; transition-duration: 0.1s; }
```

```csharp
btn.AddToClassList("is-disabled");     // 追加
btn.RemoveFromClassList("is-disabled"); // 削除
btn.EnableInClassList("is-pulsing", isOn); // 条件付き
```

### 表示/非表示

```csharp
ve.style.display = DisplayStyle.None;  // 非表示（レイアウトから消える）
ve.style.display = DisplayStyle.Flex;  // 表示
ve.visible = false;                    // 透明化（レイアウトは保持）
```

### Enable/Disable

```csharp
btn.SetEnabled(false);  // クリック不可、見た目もグレー
```

---

## UIToolkitTweenExtensions（DOTween 連携ヘルパー）

`Assets/_EDMQuiz/Scripts/UI/Tween/UIToolkitTweenExtensions.cs` に集約:

```csharp
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    public static class UIToolkitTweenExtensions
    {
        public static Tween DOScale(this VisualElement ve, float to, float duration)
        {
            float from = ve.resolvedStyle.scale.value.x;
            return DOVirtual.Float(from, to, duration, v =>
            {
                ve.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1)));
            });
        }

        public static Tween DOFade(this VisualElement ve, float to, float duration)
        {
            float from = ve.resolvedStyle.opacity;
            return DOVirtual.Float(from, to, duration, v => ve.style.opacity = v);
        }

        public static Tween DOPulse(this VisualElement ve, float peak, float duration)
        {
            return ve.DOScale(peak, duration / 2)
                .OnComplete(() => ve.DOScale(1f, duration / 2));
        }
    }
}
```

---

## R3 と組み合わせ

```csharp
// Button.clicked を Observable 化
var clickSubject = new Subject<Unit>();
button.clicked += () => clickSubject.OnNext(Unit.Default);

clickSubject
    .Where(_ => _phase == GamePhase.BuildUp)
    .Subscribe(_ => HandleClick())
    .AddTo(this);
```

---

## UniTask との連携

```csharp
public async UniTaskVoid FadeOutAndHideAsync(VisualElement panel, CancellationToken token)
{
    await panel.DOFade(0f, 0.5f).ToUniTask(cancellationToken: token);
    panel.style.display = DisplayStyle.None;
}
```

---

## 落とし穴

### ⚠️ resolvedStyle vs style

- `style.opacity` → `StyleFloat`（書き込み用）
- `resolvedStyle.opacity` → `float`（読み取り用、計算済みの実値）
- DOTween の補間は `resolvedStyle` で読んで `style` で書く

### ⚠️ Q<T>(string name) の null チェック

UXML 側で名前が変わると static エラーにならず実行時 null。`Q<T>` の戻りを Awake で検証すると安全。

### ⚠️ TextMeshPro Font

UI Toolkit の `Label` は SDF Font Asset を `style.unityFontDefinition` で設定。USS で:

```css
.question-text {
    -unity-font-definition: url("project://database/Assets/_EDMQuiz/UI/Fonts/JapaneseFont.asset");
}
```

### ⚠️ WebGL での読み込み順

`UIDocument.rootVisualElement` は `Awake` で取れないことがある。`Start` で取るか `OnEnable` を使う。

---

## 関連 Skill

- コード規約: `edm-quiz-coding-conventions`
- 非同期: `edm-quiz-async-reactive`
- 入力 UI: `edm-quiz-input-ui`
- 演出: `edm-quiz-presentation-vfx`
