# GDD — quiz-data

**System Slug**: `quiz-data`
**Layer**: Foundation（依存なし）
**Last Updated**: 2026-04-27
**Status**: Approved

---

## 1. Overview

ゲームの全問題データを ScriptableObject として管理するシステム。`QuizQuestion`（1問分のデータ）と `QuizDatabase`（問題リスト）の2層構造で構成される。問題文・ひらがな選択肢・正解文字列をデータとして保持し、ランタイムから読み取り専用でアクセスされる。実装コードは持たず、データコンテナとしての責務のみ担う。

---

## 2. Player Fantasy

直接の体験はないが、**良質な問題データが「ノっている感」の土台を作る**。エンタメ性の高い問題文・ちょうど良い難易度・適度に紛らわしい選択肢が揃ってこそ、ビルドアップ中の「迷い」→ドロップの「爆発」が成立する。

---

## 3. Detailed Rules

### データ構造

```
QuizQuestion (ScriptableObject)
├── questionText    : string   // 問題文（例: "ビートが爆発する瞬間は？"）
├── hiraganaOptions : string[] // ひらがなボタン一覧（5〜8文字）
└── correctAnswer   : string   // 正解文字列（4文字ひらがな）
                                // ※ correctAnswer は hiraganaOptions の文字を組み合わせた4文字

QuizDatabase (ScriptableObject)
└── questions : QuizQuestion[] // 全問題リスト（プロトタイプは5問）
```

### 選択肢設計ルール

- ひらがな選択肢は **5〜8文字** を手動定義
- 正解の4文字は必ず選択肢の中に含まれる
- 正解文字列に同じ文字が複数登場する場合、選択肢にも同数以上含める
  - 例: 正解に「は」が2回登場する場合、選択肢に「は」を2つ以上入れる
- 不正解を誘導するダミー文字を1〜4文字混ぜる（難易度調整の主要チューニングポイント）

### 問題の順序

- プロトタイプ版: `QuizDatabase.questions` の配列順に固定（シャッフルなし）
- ランタイムからは index で順次参照（`GameFlowManager` が管理）

### 配置場所

- ScriptableObject アセット: `Assets/_EDMQuiz/ScriptableObjects/Questions/`
- QuizDatabase インスタンス: `QuizDatabase.asset`（1ファイル）
- QuizQuestion インスタンス: `Q01_*.asset`〜`Q05_*.asset`（5ファイル）

---

## 4. Formulas

```
// ひらがな選択肢の最小文字数
MIN_OPTIONS = 5

// ひらがな選択肢の最大文字数
MAX_OPTIONS = 8

// 正解文字数（固定）
ANSWER_LENGTH = 4

// 問題数（プロトタイプ固定）
TOTAL_QUESTIONS = 5
```

正誤判定の式は `answer-judgment` システムが担当（このシステムは保持しない）。

---

## 5. Edge Cases

| ケース | 対応 |
|--------|------|
| `hiraganaOptions` が `correctAnswer` の文字を含まない | Inspector バリデーション（`OnValidate()`）でエラーログを出す |
| `hiraganaOptions` が5文字未満 / 8文字超 | 同上、エラーログ |
| `correctAnswer` が4文字でない | 同上、エラーログ |
| `QuizDatabase.questions` が空または null | `GameFlowManager` がゲーム開始前にチェック、ロード失敗としてタイトルに戻す |
| 同一問題が複数回参照される | 配列インデックスで管理するため発生しない |

---

## 6. Dependencies

**このシステムが依存するシステム: なし（Foundation 層）**

**このシステムを利用するシステム:**

- `game-flow` — `QuizDatabase` から問題を順次取得
- `input-ui` — `QuizQuestion.hiraganaOptions` を読んでボタン生成
- `answer-judgment` — `QuizQuestion.correctAnswer` と入力を比較

---

## 7. Tuning Knobs

| 定数名 | デフォルト値 | 説明 |
|--------|------------|------|
| `ANSWER_LENGTH` | 4 | 正解文字列の文字数（全問共通） |
| `MIN_OPTIONS` | 5 | ひらがな選択肢の最小数 |
| `MAX_OPTIONS` | 8 | ひらがな選択肢の最大数 |
| `TOTAL_QUESTIONS` | 5 | 1プレイの問題数（QuizDatabase の要素数と一致させる） |

これらは `GameConstants.cs` に定数として定義し、ScriptableObject 側の `OnValidate()` が参照する。

---

## 8. Acceptance Criteria

- [ ] `QuizQuestion` ScriptableObject を Inspector から作成・編集できる
- [ ] `QuizDatabase` が5問の `QuizQuestion` を保持できる
- [ ] `OnValidate()` が選択肢不足・文字数違反を検出しエラーログを出す
- [ ] `GameFlowManager`（モック）から `QuizDatabase[index]` で問題データを取得できる
- [ ] 正解文字列が必ず選択肢に含まれる問題を5問分作成してアセットとして保存できる
