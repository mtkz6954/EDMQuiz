---
name: edm-quiz-async-reactive
description: UniTask（非同期）と R3（リアクティブ）の使い分け・コードパターン集。タイマー・イベント購読・演出シーケンス・キャンセル処理を実装するときに参照する。
---

# UniTask + R3 パターン集

## 使い分け基準

| やりたいこと | 採用 |
|------------|------|
| 時間を待つ | UniTask `Delay` |
| 1回の async 処理（DOTween 完了待ちなど） | UniTask `await` |
| 演出シーケンス（A→B→C） | UniTask 直列 |
| 連続イベント（OnBeat, OnPhase, ボタンクリック） | R3 `Observable` |
| イベントを条件で絞る | R3 `Where` |
| 複数イベントを合成 | R3 `CombineLatest`, `Merge` |
| 値が変わったら通知 | R3 `ReactiveProperty` |

---

## R3 イベント宣言の規約

```csharp
public class GameFlowManager : MonoBehaviour
{
    // private Subject で保持
    private static readonly Subject<GamePhase> _onPhaseChangedSubject = new();

    // 公開は Observable<T> のみ
    public static Observable<GamePhase> OnPhaseChanged => _onPhaseChangedSubject;

    private void TransitionTo(GamePhase next)
    {
        _phase = next;
        _onPhaseChangedSubject.OnNext(next);
    }
}
```

**外部から `OnNext` できないように `Subject` は private に隠蔽**する。

---

## R3 購読のライフサイクル

```csharp
void Start()
{
    BpmClock.OnBeat
        .Where(_ => _phase == GamePhase.BuildUp)
        .Subscribe(_ => Pulse())
        .AddTo(this);  // ← MonoBehaviour 破棄時に自動 dispose
}
```

`AddTo(this)` は R3.Unity の拡張メソッド。`OnDisable` 不要。

### 複数イベントの合成

```csharp
// ビートかつ BuildUp 中だけ発火
BpmClock.OnBeat
    .WithLatestFrom(GameFlowManager.OnPhaseChanged.StartWith(GamePhase.Idle), (_, p) => p)
    .Where(p => p == GamePhase.BuildUp)
    .Subscribe(_ => DoSomething())
    .AddTo(this);
```

### 入力ストリームとして扱う

```csharp
// Button.clicked → Subject → 条件付き購読
var clickSubject = new Subject<string>();
button.clicked += () => clickSubject.OnNext("あ");

clickSubject
    .Where(_ => _inputBuffer.Count < 4)
    .Subscribe(OnHiraganaPressed)
    .AddTo(this);
```

---

## UniTask タイマー

### 基本パターン

```csharp
// CancellationToken は MonoBehaviour.destroyCancellationToken を使う
public async UniTaskVoid RunBuildUpPhaseAsync(CancellationToken token)
{
    try
    {
        var duration = TimeSpan.FromSeconds(GameConstants.GetBuildUpDurationSec());
        await UniTask.Delay(duration, cancellationToken: token);
        TransitionTo(GamePhase.Drop);
    }
    catch (OperationCanceledException)
    {
        // キャンセルは正常系
    }
}

// 呼び出し側
RunBuildUpPhaseAsync(this.GetCancellationTokenOnDestroy()).Forget();
```

### 早押しキャンセル

```csharp
private CancellationTokenSource _phaseCts;

public void StartBuildUp()
{
    _phaseCts?.Cancel();
    _phaseCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
    RunBuildUpPhaseAsync(_phaseCts.Token).Forget();
}

public void OnConfirmPressed()
{
    _phaseCts?.Cancel();  // BuildUp 中断 → 即 Drop
    TransitionTo(GamePhase.Drop);
}
```

### DOTween 完了待ち

```csharp
await transform.DOMove(target, 1f).ToUniTask(cancellationToken: token);
```

### 演出シーケンス

```csharp
public async UniTaskVoid PlayCorrectSequenceAsync(CancellationToken token)
{
    _confettiParticle.Play();
    AudioManager.Instance.PlaySE("SE_CORRECT");

    var scaleTween = UIToolkitTweenExtensions.DOScale(_correctLabel, 1.2f, 0.3f);
    await scaleTween.ToUniTask(cancellationToken: token);

    await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.DROP_REVEAL_SEC - 0.3f),
                       cancellationToken: token);

    UIToolkitTweenExtensions.DOScale(_correctLabel, 1f, 0.2f);
}
```

### 並列実行

```csharp
await UniTask.WhenAll(
    PlayConfettiAsync(token),
    PlayShakeAsync(token),
    PlaySoundAsync(token)
);
```

---

## ReactiveProperty（状態監視）

```csharp
public class ScoreManager : MonoBehaviour
{
    private readonly ReactiveProperty<int> _correctCount = new(0);
    public ReadOnlyReactiveProperty<int> CorrectCount => _correctCount;

    void Start()
    {
        AnswerJudgment.OnJudged
            .Where(b => b)
            .Subscribe(_ => _correctCount.Value++)
            .AddTo(this);
    }
}

// UI 側
ScoreManager.Instance.CorrectCount
    .Subscribe(n => _scoreLabel.text = $"{n}/5")
    .AddTo(this);
```

---

## 落とし穴

### ⚠️ Subject の dispose 漏れ

`static Subject` はシーン破棄をまたいで生き続けるため、外部の `Subscribe` 側で `AddTo(this)` を必ず使う。

### ⚠️ async void の禁止

```csharp
// ❌ 例外を握りつぶす
async void OnClick() { await ... }

// ✅
async UniTaskVoid OnClick() { await ... }
```

### ⚠️ destroyCancellationToken の罠

`destroyCancellationToken` は Unity 2022.3+ 標準。これより前は `this.GetCancellationTokenOnDestroy()`（UniTask 拡張）を使う。本プロジェクトは Unity 6 なので両方使える。

---

## 関連 Skill

- コード規約: `edm-quiz-coding-conventions`
- UI: `edm-quiz-ui-toolkit`
- フェーズ管理: `edm-quiz-game-flow`
