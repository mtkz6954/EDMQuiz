# ADR-005: 非同期処理とリアクティブストリーム

**Status**: Accepted
**Date**: 2026-04-28

## Context

EDMQuiz は BGM 同期・フェーズタイマー・ボタン入力ストリーム・演出シーケンスなど、時間軸とイベントを組み合わせた処理が大量に発生する。Unity 標準の `IEnumerator` コルーチン + `Update()` 駆動では:

- キャンセル処理が煩雑（手動フラグ管理）
- フェーズ条件付き購読を書くと if ネストが深くなる
- async/await のエコシステムから取り残される

## Decision

**UniTask + R3 を併用する。**

- **UniTask**: 非同期処理・時間待機・シーケンス制御
- **R3**: イベントストリーム・状態購読・条件合成

両者は Cysharp 製で相互運用が公式にサポートされている（`Observable.ToUniTask()` / `UniTask.ToObservable()`）。

## 使い分け基準

### UniTask を使う場面

| シーン | 例 |
|--------|-----|
| 時間待機 | `await UniTask.Delay(TimeSpan.FromSeconds(2))` |
| フェーズ進行のタイマー | `await UniTask.Delay(BUILDUP_PHASE_SEC, cancellationToken: token)` |
| 演出シーケンス | `PlayCorrectSequenceAsync()` で正解演出を順番に発火 |
| DOTween 完了待ち | `await tween.AsyncWaitForCompletion()` |
| 1回限りの async 処理 | アセットロード、シーン遷移 |

### R3 を使う場面

| シーン | 例 |
|--------|-----|
| ビートイベント | `BpmClock.OnBeat: Observable<Unit>` |
| フェーズ変化 | `GameFlowManager.OnPhaseChanged: Observable<GamePhase>` |
| 入力ストリーム | `Button.clicked` を `Subject<string>` に流して条件付きで購読 |
| 連続値の補間 | `Observable.EveryUpdate().Select(_ => transform.position)` |
| イベント合成 | `OnPhaseChanged.CombineLatest(OnBeat, ...)` |

### イベント宣言の規約

- **静的グローバルイベント** → `public static Observable<T> OnXxx => _onXxxSubject`
- **インスタンスイベント** → `public Observable<T> OnXxx => _onXxxSubject`
- 内部は `Subject<T>` で保持し、外部公開は `Observable<T>` のみ

```csharp
private static readonly Subject<GamePhase> _onPhaseChangedSubject = new();
public static Observable<GamePhase> OnPhaseChanged => _onPhaseChangedSubject;

// 発火
_onPhaseChangedSubject.OnNext(newPhase);
```

### 購読のライフサイクル管理

すべての `Subscribe(...)` は `.AddTo(this)` で MonoBehaviour 破棄時に自動 dispose する:

```csharp
GameFlowManager.OnPhaseChanged
    .Where(p => p == GamePhase.BuildUp)
    .Subscribe(_ => EnableInput())
    .AddTo(this);
```

`AddTo(this)` は R3.Unity の拡張メソッド。

### キャンセル可能 async の規約

UniTask による async メソッドは原則 `CancellationToken` を引数に受け取る:

```csharp
public async UniTaskVoid PlayCorrectSequenceAsync(CancellationToken token)
{
    _confettiParticle.Play();
    await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.DROP_REVEAL_SEC),
                       cancellationToken: token);
}
```

呼び出し側は `MonoBehaviour.destroyCancellationToken` を渡す。

## Trade-offs

- ライブラリが2つ（UniTask + R3）になり学習コストが増える
- ただし両者ともドキュメントが充実しており、リズム/イベント駆動ゲームには標準パターン
- `IEnumerator` 禁止により既存の Unity 教科書的コードと乖離する

## 影響を受けるファイル

- `GameFlowManager.cs` — フェーズタイマーを UniTask に、フェーズイベントを R3 に
- `BpmClock.cs` — `OnBeat` / `OnBar` を `Subject<Unit>` で発火
- `AnswerJudgment.cs` — `OnJudged` を `Subject<bool>` に
- `VFXDirector.cs` — コルーチン → `UniTaskVoid`、購読を R3 + AddTo
- `HiraganaInputUI.cs` — Button.clicked を Subject に流して合成
