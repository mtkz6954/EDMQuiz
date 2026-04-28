---
name: edm-quiz-game-flow
description: ゲーム全体のフェーズ進行管理 (GameFlowManager) の実装方針。GamePhase 状態機械、UniTask タイマー、5問ループ、早押しキャンセル処理、フェーズ通知 (R3) を扱うときに参照する。
---

# game-flow — フェーズ進行管理

## 責務

ゲーム全体の状態機械として `GamePhase` を管理し、UniTask タイマーでフェーズを進める。`OnPhaseChanged` を R3 で発火して全システムに通知する。

---

## GamePhase enum

```csharp
namespace EDMQuiz
{
    public enum GamePhase
    {
        Idle,       // 未開始（タイトル表示）
        Question,   // 問題文表示中
        BuildUp,    // 入力受付中
        Drop,       // 正誤判定 + 演出
        Next,       // 次問題への遷移
        GameEnd     // 結果画面
    }
}
```

---

## フェーズ遷移

```
Idle ─[StartGame()]─→ Question
Question ─[QUESTION_PHASE_SEC 経過]─→ BuildUp
BuildUp ─[BUILDUP_PHASE_SEC 経過 OR 確定ボタン]─→ Drop
Drop ─[DROP_REVEAL_SEC 経過]─→ Next
Next ─[NEXT_TRANSITION_SEC 経過]─→ Question (次問題)
                                  └─→ GameEnd (5問完了時)
```

---

## 実装

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace EDMQuiz
{
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        [SerializeField] private QuizDatabase _quizDatabase;

        public GamePhase CurrentPhase { get; private set; } = GamePhase.Idle;
        public int QuestionIndex { get; private set; }
        public QuizQuestion CurrentQuestion =>
            _quizDatabase != null ? _quizDatabase.Get(QuestionIndex) : null;

        private static readonly Subject<GamePhase> _onPhaseChangedSubject = new();
        public static Observable<GamePhase> OnPhaseChanged => _onPhaseChangedSubject;

        private CancellationTokenSource _phaseCts;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            // BuildUp 中の確定ボタン → Drop へ即遷移
            // 早押しキャンセルは ConfirmAnswer() で処理
        }

        public void StartGame()
        {
            QuestionIndex = 0;
            RunGameLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid RunGameLoopAsync(CancellationToken ct)
        {
            while (QuestionIndex < GameConstants.TOTAL_QUESTIONS)
            {
                await RunQuestionPhaseAsync(ct);
                await RunBuildUpPhaseAsync(ct);
                await RunDropPhaseAsync(ct);
                await RunNextPhaseAsync(ct);
                QuestionIndex++;
            }
            TransitionTo(GamePhase.GameEnd);
        }

        private async UniTask RunQuestionPhaseAsync(CancellationToken ct)
        {
            TransitionTo(GamePhase.Question);
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(GameConstants.QUESTION_PHASE_SEC),
                cancellationToken: ct);
        }

        private async UniTask RunBuildUpPhaseAsync(CancellationToken ct)
        {
            TransitionTo(GamePhase.BuildUp);
            _phaseCts?.Dispose();
            _phaseCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                await UniTask.Delay(
                    System.TimeSpan.FromSeconds(GameConstants.GetBuildUpDurationSec()),
                    cancellationToken: _phaseCts.Token);
            }
            catch (System.OperationCanceledException) { /* 早押しによる中断 */ }
        }

        private async UniTask RunDropPhaseAsync(CancellationToken ct)
        {
            TransitionTo(GamePhase.Drop);
            // 演出側 (VFXDirector) が AnswerJudgment.OnJudged を購読して走る
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(GameConstants.DROP_REVEAL_SEC),
                cancellationToken: ct);
        }

        private async UniTask RunNextPhaseAsync(CancellationToken ct)
        {
            TransitionTo(GamePhase.Next);
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(GameConstants.NEXT_TRANSITION_SEC),
                cancellationToken: ct);
        }

        private void TransitionTo(GamePhase phase)
        {
            CurrentPhase = phase;
            _onPhaseChangedSubject.OnNext(phase);
        }

        /// <summary>BuildUp 中に呼ばれる。早押しで Drop へ即遷移</summary>
        public void ConfirmAnswer(string answer)
        {
            if (CurrentPhase != GamePhase.BuildUp) return;
            AnswerJudgment.Judge(answer, CurrentQuestion);
            _phaseCts?.Cancel();  // BuildUp タイマーを中断
        }

        /// <summary>BuildUp タイムアウト時に4文字未満なら不正解判定</summary>
        public void TimeoutAnswer(string answer)
        {
            if (CurrentPhase != GamePhase.BuildUp) return;
            AnswerJudgment.Judge(answer, CurrentQuestion);
        }
    }
}
```

---

## チューニング項目

`GameConstants.cs`:

```csharp
public const int   TOTAL_QUESTIONS      = 5;
public const float QUESTION_PHASE_SEC   = 2.0f;
public const float DROP_REVEAL_SEC      = 4.0f;
public const float NEXT_TRANSITION_SEC  = 1.5f;
// BUILDUP_PHASE_SEC は GetBuildUpDurationSec() で BPM 連動
```

---

## 購読パターン（他システム）

```csharp
// HiraganaInputUI: BuildUp に入ったら入力可能化
GameFlowManager.OnPhaseChanged
    .Subscribe(phase =>
    {
        bool enable = (phase == GamePhase.BuildUp);
        SetInputEnabled(enable);
        if (phase == GamePhase.Question) ResetInputBuffer();
    })
    .AddTo(this);

// VFXDirector: フェーズ別演出
GameFlowManager.OnPhaseChanged
    .Where(p => p == GamePhase.Next)
    .Subscribe(_ => CancelOngoingVfx())
    .AddTo(this);

// ResultScreen: GameEnd で表示
GameFlowManager.OnPhaseChanged
    .Where(p => p == GamePhase.GameEnd)
    .Subscribe(_ => Show())
    .AddTo(this);
```

---

## 早押しキャンセルの仕組み

1. BuildUp 開始時に `_phaseCts = CreateLinkedTokenSource(ct)` を作る
2. `UniTask.Delay` をその token で待つ
3. 確定ボタン押下 → `ConfirmAnswer()` → `_phaseCts.Cancel()` で Delay を中断
4. catch で `OperationCanceledException` を握りつぶしてフロー継続

---

## Edge Cases

| ケース | 対応 |
|--------|------|
| 確定を BuildUp 外で押下 | `if (CurrentPhase != GamePhase.BuildUp) return;` |
| QuizDatabase が null | `Get()` が null を返す → `Question` フェーズで即エラーログ |
| Drop 中に再度 ConfirmAnswer | フェーズチェックで弾く |
| シーン破棄中に Delay 残り | `destroyCancellationToken` で全 Delay が連鎖 cancel |

---

## Acceptance Criteria

- [ ] StartGame() でタイトル → 5問 → GameEnd まで進む
- [ ] 各フェーズで `OnPhaseChanged` が1回ずつ発火
- [ ] 確定ボタン早押しで BuildUp が即 Drop へ
- [ ] BUILDUP_PHASE_SEC は BPM 連動（GetBuildUpDurationSec）
- [ ] 5問完了で GameEnd へ、それ以上遷移しない

---

## 関連 Skill

- データ: `edm-quiz-quiz-data`
- 判定: `edm-quiz-answer-judgment`
- 入力: `edm-quiz-input-ui`
- 演出: `edm-quiz-presentation-vfx`
- 結果: `edm-quiz-score-result`
- 非同期パターン: `edm-quiz-async-reactive`
