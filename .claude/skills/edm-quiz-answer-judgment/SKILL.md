---
name: edm-quiz-answer-judgment
description: 正誤判定システム（AnswerJudgment）の実装方針。完全一致判定・R3 Subject による通知・単体テスト方針を扱うときに参照する。
---

# answer-judgment — 正誤判定

## 責務

入力文字列と `QuizQuestion.correctAnswer` を **完全一致** で比較し、結果を R3 Subject で通知する。MonoBehaviour 不要の純粋ロジック。

---

## 実装

```csharp
using System;
using R3;
using UnityEngine;

namespace EDMQuiz
{
    public static class AnswerJudgment
    {
        private static readonly Subject<bool> _onJudgedSubject = new();
        public static Observable<bool> OnJudged => _onJudgedSubject;

        /// <summary>入力と正解を完全一致比較し、結果を OnJudged で通知</summary>
        public static bool Judge(string inputAnswer, QuizQuestion question)
        {
            if (question == null)
            {
                Debug.LogError("[AnswerJudgment] question が null");
                _onJudgedSubject.OnNext(false);
                return false;
            }

            bool isCorrect = !string.IsNullOrEmpty(inputAnswer)
                          && inputAnswer.Length == GameConstants.ANSWER_LENGTH
                          && inputAnswer == question.correctAnswer;

            _onJudgedSubject.OnNext(isCorrect);
            return isCorrect;
        }
    }
}
```

---

## 判定ルール

- **完全一致のみ正解**（前後トリム・大文字小文字無視はしない）
- **長さチェック**: 4文字未満は無条件で false
- **null/empty**: false

---

## 呼び出しタイミング

`GameFlowManager` が以下のタイミングで呼ぶ:

1. **BuildUp 中の確定ボタン押下時**（早押し）
2. **BuildUp タイムアウト時**（4文字揃ってなくても false）
3. Drop フェーズへの遷移トリガーは `OnJudged` のサブスクライバ側で行う（VFXDirector / GameFlowManager）

---

## 購読側の実装パターン

### VFXDirector

```csharp
AnswerJudgment.OnJudged
    .Subscribe(isCorrect =>
    {
        if (isCorrect) PlayCorrectSequenceAsync(token).Forget();
        else PlayIncorrectSequenceAsync(token).Forget();
    })
    .AddTo(this);
```

### ScoreManager

```csharp
AnswerJudgment.OnJudged
    .Where(b => b)
    .Subscribe(_ => _correctCount.Value++)
    .AddTo(this);
```

### GameFlowManager（Drop → Next 遷移）

```csharp
AnswerJudgment.OnJudged
    .Subscribe(_ => StartDropPhaseAsync().Forget())
    .AddTo(this);
```

---

## 単体テスト

`Assets/_EDMQuiz/Tests/EditMode/AnswerJudgmentTests.cs`

```csharp
using NUnit.Framework;
using UnityEngine;

namespace EDMQuiz.Tests
{
    public class AnswerJudgmentTests
    {
        private QuizQuestion MakeQuestion(string correct)
        {
            var q = ScriptableObject.CreateInstance<QuizQuestion>();
            q.correctAnswer = correct;
            return q;
        }

        [Test]
        public void Judge_ExactMatch_ReturnsTrue()
        {
            var q = MakeQuestion("どろっぷ");
            Assert.IsTrue(AnswerJudgment.Judge("どろっぷ", q));
        }

        [Test]
        public void Judge_OneCharDifferent_ReturnsFalse()
        {
            var q = MakeQuestion("どろっぷ");
            Assert.IsFalse(AnswerJudgment.Judge("どろっぴ", q));
        }

        [Test]
        public void Judge_TooShort_ReturnsFalse()
        {
            var q = MakeQuestion("どろっぷ");
            Assert.IsFalse(AnswerJudgment.Judge("どろっ", q));
        }

        [Test]
        public void Judge_Empty_ReturnsFalse()
        {
            var q = MakeQuestion("どろっぷ");
            Assert.IsFalse(AnswerJudgment.Judge("", q));
        }

        [Test]
        public void Judge_NullQuestion_ReturnsFalse()
        {
            Assert.IsFalse(AnswerJudgment.Judge("どろっぷ", null));
        }
    }
}
```

---

## Edge Cases

| ケース | 期待 |
|--------|------|
| 4文字完全一致 | true |
| 1文字違う | false |
| 3文字以下 | false |
| 空文字 | false |
| null question | false（エラーログ） |
| 同じ判定が連続発火 | OnJudged は毎回 OnNext（受け側で `Distinct` するか抑制） |

---

## チューニング項目

`GameConstants.ANSWER_LENGTH = 4`（変更時は quiz-data 側も追従）

---

## Acceptance Criteria

- [ ] 完全一致で `OnJudged(true)` が発火
- [ ] 1文字でも違えば `OnJudged(false)`
- [ ] 4文字未満の入力で `false`
- [ ] null question で `false` + エラーログ
- [ ] 単体テスト 5ケース全パス

---

## 関連 Skill

- データ: `edm-quiz-quiz-data`
- フロー: `edm-quiz-game-flow`
- 演出: `edm-quiz-presentation-vfx`
- 結果: `edm-quiz-score-result`
