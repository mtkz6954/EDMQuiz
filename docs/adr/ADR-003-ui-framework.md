# ADR-003: UI フレームワーク

**Status**: Accepted
**Date**: 2026-04-27（初版）/ 2026-04-28（再確認・UI Toolkit 維持）

## Decision

**UI Toolkit + DOTween + TextMeshPro** を採用する。

## Rationale

- Unity 6 の推奨 UI フレームワークであり、将来的な保守性が高い
- レイアウトとスタイルが UXML/USS で宣言的に書けるため、UI 修正が高速
- 動的なボタン生成・配列レンダリングが `VisualTreeAsset.CloneTree()` で簡潔
- Web 開発者には HTML/CSS 風の感覚で扱える

## Implementation Notes

### シーン構成
- `UIDocument` コンポーネントを GameScene のルート GameObject に配置
- UXML テンプレート: `Assets/_EDMQuiz/UI/Layouts/`
- USS スタイル: `Assets/_EDMQuiz/UI/Styles/`
- パネル切り替えは `VisualElement.style.display` で制御

### 動的要素生成
```csharp
// HiraganaInputUI.cs から
var root = uiDocument.rootVisualElement;
var buttonContainer = root.Q<VisualElement>("hiragana-buttons");
foreach (var ch in question.hiraganaOptions)
{
    var btn = new Button(() => OnHiraganaPressed(ch)) { text = ch };
    btn.AddToClassList("hiragana-button");
    buttonContainer.Add(btn);
}
```

### TextMeshPro との併用
- UI Toolkit の Label は標準で TMP をサポート（`<ui:Label text-font-asset="..."/>`）
- TextMeshPro Asset を `Assets/_EDMQuiz/UI/Fonts/` に配置

## DOTween × UI Toolkit

DOTween は VisualElement への直接拡張がないため、`DOVirtual.Float()` を経由してスタイル値を更新する。

```csharp
// ボタンのパルスアニメーション（ビート同期）
DOVirtual.Float(1f, 1.08f, beatInterval * 0.2f, v => {
    btn.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1)));
}).SetLoops(2, LoopType.Yoyo);

// フェードイン
DOVirtual.Float(0f, 1f, 0.5f, v => {
    panel.style.opacity = v;
});
```

ヘルパーメソッドを `UIToolkitTweenExtensions.cs` に集約することを推奨。

## R3 との連携

`Button.clicked` を Observable 化して入力ストリームを構築:

```csharp
// IObservable<Unit> として購読可能に
button.clicked += () => clickSubject.OnNext(Unit.Default);
clickSubject
    .Where(_ => GameFlowManager.CurrentPhase == GamePhase.BuildUp)
    .Subscribe(_ => HandleClick())
    .AddTo(this);
```

## Trade-offs

- DOTween の `RectTransform` 拡張が直接使えない → `DOVirtual` ヘルパーで対応
- UGUI より日本語資料が少ない → Unity 6 公式ドキュメント + コミュニティ事例で補う
- 既存の UGUI ベースのスクリプト雛形は書き直しが必要（HiraganaInputUI, ResultScreen, QuestionDisplay, VFXDirector の UI 連携部分）

## 影響を受けるファイル（書き直し対象）

- `HiraganaInputUI.cs` — Button → UIDocument + VisualElement
- `QuestionDisplay.cs` — TextMeshProUGUI → Label (UI Toolkit)
- `ResultScreen.cs` — Canvas → UIDocument
- `VFXDirector.cs` — RectTransform.DOScale → DOVirtual.Float
