# ADR-003: UI フレームワーク

**Status**: Accepted
**Date**: 2026-04-27

## Decision

**UI Toolkit + DOTween** を採用する。

## Rationale

- Unity 6 の推奨 UI フレームワークであり、将来的な保守性が高い
- DOTween は UI Toolkit の VisualElement にも `.DOFade()` 等で対応可能（要 DOTween 拡張）
- ひらがなボタンは UI Toolkit の `Button` で実装し、`clicked` イベントを使用する

## DOTween × UI Toolkit の注意点

DOTween はデフォルトでは UGUI（RectTransform）向けの拡張が豊富だが、
UI Toolkit の VisualElement に対しては `DOVirtual.Float()` を使う方式で対応する:

```csharp
// UI Toolkit のボタンをスケールアニメーション（パルス）
DOVirtual.Float(1f, 1.12f, beatInterval * 0.2f, v => {
    btn.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1)));
}).SetLoops(2, LoopType.Yoyo);
```

## Implementation Notes

- `UIDocument` コンポーネントでシーンに UXML を紐付ける
- HiraganaInputUI.cs は `UIDocument.rootVisualElement.Q<Button>()` でボタン参照を取得
- ひらがなボタンは UXML テンプレートで定義し、C# で動的生成（`VisualElement.Add()`）

## Trade-offs

- DOTween の `RectTransform` 拡張が使えないため、スケール・フェード等は `DOVirtual` で実装する必要がある
- UGUI より資料が少ないが、Unity 6 の公式ドキュメントが充実している
