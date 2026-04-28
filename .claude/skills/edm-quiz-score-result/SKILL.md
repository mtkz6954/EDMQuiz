---
name: edm-quiz-score-result
description: スコア計算と結果画面 (ScoreManager / ResultScreen) の実装方針。盛り上がり度算出、ランク判定、UI Toolkit でのカウントアップアニメ、リトライ処理を扱うときに参照する。
---

# score-result — スコア・結果画面

## 責務

1. `AnswerJudgment.OnJudged(true)` を購読して正解数を集計
2. GameEnd フェーズで盛り上がり度・ランクを算出して表示
3. UI Toolkit + DOTween でカウントアップ + Label 拡大演出
4. リトライボタンで TitleScene へ遷移

---

## 計算式

```csharp
excitementScore = (correctCount * 100) / TOTAL_QUESTIONS
                = 0, 20, 40, 60, 80, 100 のいずれか

Rank:
  S: score >= RANK_S (90)
  A: score >= RANK_A (70)
  B: score >= RANK_B (50)
  C: score >= RANK_C (30)
  D: score < RANK_C
```

---

## ScoreManager 実装

```csharp
using R3;
using UnityEngine;

namespace EDMQuiz
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        private readonly ReactiveProperty<int> _correctCount = new(0);
        public ReadOnlyReactiveProperty<int> CorrectCount => _correctCount;

        public int ExcitementScore =>
            (_correctCount.Value * 100) / GameConstants.TOTAL_QUESTIONS;

        public string Rank => DetermineRank(ExcitementScore);

        public string RankLabel => Rank switch
        {
            "S" => "神",
            "A" => "最高",
            "B" => "いい感じ",
            "C" => "まあまあ",
            _   => "スベった"
        };

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            AnswerJudgment.OnJudged
                .Where(b => b)
                .Subscribe(_ => _correctCount.Value++)
                .AddTo(this);

            GameFlowManager.OnPhaseChanged
                .Where(p => p == GamePhase.Question)
                .Take(1)  // 最初の Question で初期化
                .Subscribe(_ => _correctCount.Value = 0)
                .AddTo(this);
        }

        public static string DetermineRank(int score) => score switch
        {
            >= GameConstants.RANK_S => "S",
            >= GameConstants.RANK_A => "A",
            >= GameConstants.RANK_B => "B",
            >= GameConstants.RANK_C => "C",
            _ => "D"
        };
    }
}
```

---

## ResultScreen（UI Toolkit）

### UXML（result-panel.uxml）

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <Style src="../Styles/result-panel.uss"/>
  <ui:VisualElement name="result-root" class="result-root" style="display: none;">
    <ui:Label text="結果" class="result-title"/>
    <ui:Label name="score-label" text="0" class="score-label"/>
    <ui:Label text="盛り上がり度" class="score-caption"/>
    <ui:Label name="rank-label" class="rank-label"/>
    <ui:Label name="rank-text-label" class="rank-text-label"/>
    <ui:Button name="retry-button" text="もう一度" class="retry-button"/>
  </ui:VisualElement>
</ui:UXML>
```

### C# 実装

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private Label _scoreLabel;
        private Label _rankLabel;
        private Label _rankTextLabel;
        private Button _retryButton;

        void OnEnable()
        {
            var doc = _uiDocument.rootVisualElement;
            _root          = doc.Q<VisualElement>("result-root");
            _scoreLabel    = doc.Q<Label>("score-label");
            _rankLabel     = doc.Q<Label>("rank-label");
            _rankTextLabel = doc.Q<Label>("rank-text-label");
            _retryButton   = doc.Q<Button>("retry-button");

            _retryButton.clicked += OnRetryClicked;

            GameFlowManager.OnPhaseChanged
                .Where(p => p == GamePhase.GameEnd)
                .Subscribe(_ => ShowAsync(destroyCancellationToken).Forget())
                .AddTo(this);
        }

        private async UniTaskVoid ShowAsync(CancellationToken ct)
        {
            _root.style.display = DisplayStyle.Flex;
            _retryButton.SetEnabled(false);

            // フェードイン
            _root.style.opacity = 0f;
            await _root.DOFade(1f, 0.5f).ToUniTask(cancellationToken: ct);

            // スコアカウントアップ
            int finalScore = ScoreManager.Instance.ExcitementScore;
            await DOTween.To(() => 0, v => _scoreLabel.text = v.ToString(),
                            finalScore, GameConstants.SCORE_COUNTUP_DURATION)
                         .SetEase(Ease.OutCubic)
                         .ToUniTask(cancellationToken: ct);

            // ランクをドン拡大
            _rankLabel.text     = ScoreManager.Instance.Rank;
            _rankTextLabel.text = ScoreManager.Instance.RankLabel;
            _rankLabel.style.scale = new StyleScale(new Scale(Vector3.zero));
            await _rankLabel.DOScale(GameConstants.RANK_SCALE_PEAK, GameConstants.RANK_SCALE_DURATION)
                            .SetEase(Ease.OutElastic)
                            .ToUniTask(cancellationToken: ct);
            _rankLabel.DOScale(1f, 0.2f);

            AudioManager.Instance.PlayResultSE();
            _retryButton.SetEnabled(true);
        }

        private void OnRetryClicked()
        {
            SceneManager.LoadScene("TitleScene");
        }
    }
}
```

---

## チューニング項目

```csharp
public const int RANK_S = 90;
public const int RANK_A = 70;
public const int RANK_B = 50;
public const int RANK_C = 30;
public const float SCORE_COUNTUP_DURATION = 1.5f;
public const float RANK_SCALE_PEAK        = 1.2f;
public const float RANK_SCALE_DURATION    = 0.5f;
```

---

## 単体テスト

```csharp
[TestCase(0, "D")]
[TestCase(20, "D")]
[TestCase(40, "C")]
[TestCase(60, "B")]
[TestCase(80, "A")]
[TestCase(100, "S")]
public void DetermineRank_AllScores(int score, string expected)
{
    Assert.AreEqual(expected, ScoreManager.DetermineRank(score));
}
```

---

## Edge Cases

| ケース | 対応 |
|--------|------|
| 全問不正解 | score=0 → Rank D |
| 全問正解 | score=100 → Rank S |
| カウントアップ中にリトライ | リトライボタンは完了後に SetEnabled(true) |
| 集計超過 | OnJudged は1問1回しか発火しない設計のため発生しない |

---

## Acceptance Criteria

- [ ] 全問正解で score=100, Rank S
- [ ] 全問不正解で score=0, Rank D
- [ ] スコアが 0 からカウントアップ
- [ ] ランクがカウントアップ後にドン拡大
- [ ] リトライボタン押下で TitleScene へ遷移
- [ ] 単体テスト: ランク境界 5 ケース全パス

---

## 関連 Skill

- UI 全般: `edm-quiz-ui-toolkit`
- 判定: `edm-quiz-answer-judgment`
- フロー: `edm-quiz-game-flow`
- 音声: `edm-quiz-audio`
- 非同期: `edm-quiz-async-reactive`
