---
name: edm-quiz-quiz-data
description: クイズ問題データ（QuizQuestion / QuizDatabase）の ScriptableObject 設計と作成方法。問題追加・データバリデーション・Inspector 拡張を扱うときに参照する。
---

# quiz-data — 問題データ管理

## 責務

問題文・ひらがな選択肢・正解を **ScriptableObject** として保持する。データのみ。実装ロジックなし。

---

## データ構造

### QuizQuestion.cs

```csharp
using NaughtyAttributes;
using UnityEngine;

namespace EDMQuiz
{
    [CreateAssetMenu(fileName = "Q_New", menuName = "EDMQuiz/QuizQuestion")]
    public class QuizQuestion : ScriptableObject
    {
        [BoxGroup("問題")]
        [TextArea(2, 4)]
        public string questionText;

        [BoxGroup("選択肢")]
        [InfoBox("5〜8文字。正解の文字をすべて含むこと（重複文字は重複数だけ）")]
        public string[] hiraganaOptions;

        [BoxGroup("正解")]
        [InfoBox("ひらがな4文字。hiraganaOptions の文字で組み立てられる文字列")]
        public string correctAnswer;

        void OnValidate()
        {
            if (correctAnswer != null && correctAnswer.Length != GameConstants.ANSWER_LENGTH)
                Debug.LogError($"[{name}] correctAnswer は {GameConstants.ANSWER_LENGTH} 文字必須");

            if (hiraganaOptions == null
                || hiraganaOptions.Length < GameConstants.MIN_OPTIONS
                || hiraganaOptions.Length > GameConstants.MAX_OPTIONS)
                Debug.LogError($"[{name}] hiraganaOptions は {GameConstants.MIN_OPTIONS}〜{GameConstants.MAX_OPTIONS} 文字");

            if (correctAnswer != null && hiraganaOptions != null)
            {
                var pool = new System.Collections.Generic.List<string>(hiraganaOptions);
                foreach (var ch in correctAnswer)
                {
                    if (!pool.Remove(ch.ToString()))
                    {
                        Debug.LogError($"[{name}] correctAnswer の '{ch}' が hiraganaOptions に不足");
                        break;
                    }
                }
            }
        }
    }
}
```

### QuizDatabase.cs

```csharp
using UnityEngine;

namespace EDMQuiz
{
    [CreateAssetMenu(fileName = "QuizDatabase", menuName = "EDMQuiz/QuizDatabase")]
    public class QuizDatabase : ScriptableObject
    {
        public QuizQuestion[] questions;

        public QuizQuestion Get(int index) =>
            (questions != null && index >= 0 && index < questions.Length)
                ? questions[index]
                : null;

        public int Count => questions?.Length ?? 0;

        void OnValidate()
        {
            if (questions != null && questions.Length != GameConstants.TOTAL_QUESTIONS)
                Debug.LogWarning($"[{name}] 問題数が {GameConstants.TOTAL_QUESTIONS} 問でない（現在: {questions.Length}）");
        }
    }
}
```

---

## アセット配置

```
Assets/_EDMQuiz/ScriptableObjects/
├── QuizDatabase.asset
└── Questions/
    ├── Q01_DropMoment.asset       # 例: 問題文「ビートが爆発する瞬間は？」、正解「どろっぷ」
    ├── Q02_*.asset
    ├── Q03_*.asset
    ├── Q04_*.asset
    └── Q05_*.asset
```

`QuizDatabase.asset` は ScriptableObjects/ 直下、個別問題は Questions/ サブフォルダ。

---

## 問題作成手順（Editor）

1. Project ウィンドウで `Assets/_EDMQuiz/ScriptableObjects/Questions/` を右クリック
2. `Create > EDMQuiz > QuizQuestion` で新規作成
3. ファイル名を `Q01_*.asset` にリネーム
4. Inspector で:
   - `questionText` に問題文入力
   - `hiraganaOptions` に5〜8文字のひらがな配列を設定
   - `correctAnswer` に4文字の正解を入力
5. `QuizDatabase.asset` の `questions` 配列に5問を登録

---

## チューニング項目

`GameConstants.cs` 側で定義:

```csharp
public const int TOTAL_QUESTIONS = 5;
public const int ANSWER_LENGTH   = 4;
public const int MIN_OPTIONS     = 5;
public const int MAX_OPTIONS     = 8;
```

---

## Edge Cases

| ケース | 対応 |
|--------|------|
| `correctAnswer` の文字が選択肢にない | `OnValidate()` でエラーログ |
| `hiraganaOptions` が範囲外 | 同上 |
| `QuizDatabase.questions` が null/空 | `GameFlowManager` がゲーム開始前にチェック |
| 同じ文字を正解で複数使う（例「ははは」）| 選択肢にも重複数だけ含める（OnValidate でチェック済み） |

---

## Acceptance Criteria

- [ ] `QuizQuestion.asset` が CreateAssetMenu から作れる
- [ ] OnValidate で文字数違反・選択肢不足を検出
- [ ] `QuizDatabase.asset` が5問を保持
- [ ] `QuizDatabase.Get(index)` で読み取れる

---

## 関連 Skill

- 規約: `edm-quiz-coding-conventions`
- 判定: `edm-quiz-answer-judgment`
- フロー: `edm-quiz-game-flow`
