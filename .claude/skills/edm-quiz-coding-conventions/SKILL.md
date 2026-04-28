---
name: edm-quiz-coding-conventions
description: コード規約・命名規則・禁止パターン・許可ライブラリの一覧。新規スクリプト作成・コードレビュー・既存コードのリファクタリング時に必ず参照する。
---

# コード規約 — EDMQuiz

## 命名規則

| 対象 | 規則 | 例 |
|------|------|-----|
| クラス / メソッド / プロパティ | PascalCase | `GameFlowManager`, `SubmitAnswer()` |
| Private フィールド | `_camelCase` | `_bgmPlayer`, `_inputBuffer` |
| 定数 | ALL_CAPS | `TOTAL_QUESTIONS`, `BPM` |
| イベント / R3 Subject 公開 | `On` + PascalCase | `OnPhaseChanged`, `OnJudged` |
| Enum 値 | PascalCase | `GamePhase.BuildUp` |
| ScriptableObject | 機能名 | `QuizQuestion`, `QuizDatabase` |
| UXML ファイル | kebab-case | `game-panel.uxml` |
| USS ファイル | kebab-case | `hiragana-button.uss` |
| USS クラス名 | kebab-case | `.hiragana-button`, `.is-active` |
| VisualElement name 属性 | kebab-case | `name="hiragana-buttons"` |
| C# ファイル名 | クラス名と同一 | `GameFlowManager.cs` |

すべてのスクリプトは `namespace EDMQuiz` 配下。

---

## 禁止パターン

### ❌ MonoBehaviour ライフサイクル

- **`Update()` 内での `Find()` / `FindObjectOfType()` 系禁止** — Awake/Start で参照取得 or DI
- **新規 `IEnumerator` コルーチン禁止** — `async UniTaskVoid` で代替
- **フレームポーリング型 `Update()` 禁止** — R3 / UniTask で時間管理

### ❌ メッセージング

- **`SendMessage()` / `BroadcastMessage()` 禁止** — `static event` または R3 Subject で代替
- **`Resources.Load()` 禁止** — ScriptableObject 直接参照 or CRI ADX で管理

### ❌ アセット配置

- **`StreamingAssets/` への CRI ADX 配置禁止** — unityroom WebGL 制約。`Assets/SoundData/` を使う

### ❌ シングルトン乱用

- **`DontDestroyOnLoad()` は `AudioManager` のみ** — 他はシーン内ライフサイクル

### ❌ DOTween

- **`Time.timeScale = 0` 中の DOTween は `.SetUpdate(true)` 必須** — でないと止まる

### ❌ UI

- **新規 UGUI 使用禁止** — UI Toolkit を使う（既存スクリプトは書き換え対象）
  - `Canvas` / `RectTransform` / `UnityEngine.UI.Button` / `TextMeshProUGUI` 新規禁止

---

## 許可ライブラリ

| ライブラリ | 用途 |
|-----------|------|
| DOTween Pro | アニメーション。UI Toolkit には `DOVirtual.Float` |
| CRI ADX LE | BGM/SE 再生 |
| CRI Asset Support Addon | WebGL OnMemory |
| TextMeshPro | テキスト表示（UI Toolkit Label と併用） |
| **UniTask** | 非同期処理・時間待機 |
| **R3** | リアクティブストリーム |
| NaughtyAttributes | Inspector 拡張 |
| HotReload | 開発時のスクリプト即時反映 |
| NuGetForUnity | NuGet 管理（R3 コア用） |

---

## ライブラリ使い分け（早見表）

### 「時間を待つ」
```csharp
// ❌ コルーチン禁止
yield return new WaitForSeconds(2f);

// ✅ UniTask
await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: token);
```

### 「イベント通知」
```csharp
// 静的グローバル → static event or R3 Subject
private static readonly Subject<GamePhase> _onPhaseChangedSubject = new();
public static Observable<GamePhase> OnPhaseChanged => _onPhaseChangedSubject;
_onPhaseChangedSubject.OnNext(newPhase);  // 発火

// 購読側
GameFlowManager.OnPhaseChanged
    .Where(p => p == GamePhase.BuildUp)
    .Subscribe(_ => EnableInput())
    .AddTo(this);  // 自動 dispose
```

### 「Inspector 拡張」
```csharp
[BoxGroup("Audio")]
[SerializeField] private CueReference _bgmCue;

[Button("Test BGM Play")]
private void TestPlay() => _audioManager.PlayBGM();
```

### 「DOTween + UI Toolkit」
```csharp
// VisualElement への直接拡張はないので DOVirtual を経由
DOVirtual.Float(1f, 1.08f, 0.1f, v => {
    btn.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1)));
}).SetLoops(2, LoopType.Yoyo);
```

---

## ファイル構成ルール

- 1ファイル = 1クラス（enum・小さな record は同居可）
- パブリック API には XML doc コメント（`/// <summary>...</summary>`）
- `[SerializeField]` でプライベート参照を Inspector に公開
- `MonoBehaviour` 継承クラスでも static helper は別 partial / 別ファイルへ

---

## イベント宣言テンプレート（R3）

```csharp
public class BpmClock : MonoBehaviour
{
    private static readonly Subject<Unit> _onBeatSubject = new();
    public static Observable<Unit> OnBeat => _onBeatSubject;

    void Update()
    {
        if (BeatCrossed())
            _onBeatSubject.OnNext(Unit.Default);
    }
}
```

外部公開は `Observable<T>` のみ、`Subject<T>` は private で隠蔽。

---

## 関連 Skill

- 非同期パターン: `edm-quiz-async-reactive`
- UI 実装パターン: `edm-quiz-ui-toolkit`
- 全体像: `edm-quiz-overview`
