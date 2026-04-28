# GDD — score-result

**System Slug**: `score-result`
**Layer**: Presentation（answer-judgment・game-flow に依存）
**Last Updated**: 2026-04-27
**Status**: Approved

---

## 1. Overview

5問分の正解数から「盛り上がり度」スコアとランクを算出し、結果画面に表示するシステム。`ScoreManager` が `OnAnswerJudged` を購読して正解数を蓄積し、`GameEnd` フェーズで結果画面 UI を表示する。

---

## 2. Player Fantasy

全5問が終わった直後、スコアが数値カウントアップしながら画面に現れ、ランク文字が「ドン！」と拡大する——「自分の盛り上がり度がわかる」達成感と、SNS でシェアしたくなる「映える結果画面」を提供する。

---

## 3. Detailed Rules

### スコア算出

```csharp
int _correctCount = 0;  // OnAnswerJudged(true) のたびにインクリメント

// GameEnd 時に算出
int excitementScore = (_correctCount * 100) / TOTAL_QUESTIONS;
// 結果: 0, 20, 40, 60, 80, 100 のいずれか
```

### ランク判定

```csharp
string rank = excitementScore switch
{
    >= RANK_S => "S",  // 神
    >= RANK_A => "A",  // 最高
    >= RANK_B => "B",  // いい感じ
    >= RANK_C => "C",  // まあまあ
    _         => "D"   // スベった
};
```

### 結果画面の表示要素

| UI 要素 | 内容 |
|--------|------|
| 盛り上がり度テキスト | `excitementScore` を 0 からカウントアップ（DOTween） |
| ランク文字 | S / A / B / C / D をドン拡大（Elastic ease） |
| 正解数テキスト | `{_correctCount} / {TOTAL_QUESTIONS}` |
| リトライボタン | タイトルシーンへ遷移 |
| （将来）シェアボタン | Twitter/X シェア（プロトタイプでは非実装） |

### 表示フロー

```
OnPhaseChanged(GameEnd)
 → ResultPanel を有効化
 → excitementScore を DOTween でカウントアップ（SCORE_COUNTUP_DURATION）
 → カウントアップ完了後、ランク文字をスケール 0 → 1.2 → 1.0（Elastic, RANK_SCALE_DURATION）
 → SE_RESULT 再生
 → リトライボタンを有効化
```

---

## 4. Formulas

```
excitementScore = (_correctCount * 100) / TOTAL_QUESTIONS

// ランク境界
S: score >= RANK_S (90)
A: score >= RANK_A (70)
B: score >= RANK_B (50)
C: score >= RANK_C (30)
D: score  < RANK_C
```

| 変数 | 型 | 説明 |
|------|----|------|
| `_correctCount` | int | 正解した問題数（0〜5） |
| `TOTAL_QUESTIONS` | int | 5（GameConstants） |
| `excitementScore` | int | 盛り上がり度（0, 20, 40, 60, 80, 100） |

---

## 5. Edge Cases

| ケース | 対応 |
|--------|------|
| 全問不正解（score = 0） | Rank D、スベり演出で結果画面表示 |
| 全問正解（score = 100） | Rank S、豪華演出（`presentation-vfx` に通知） |
| カウントアップ中にリトライボタンを押す | リトライボタンはカウントアップ完了後に有効化（`OnComplete` コールバック） |
| `_correctCount` が TOTAL_QUESTIONS を超える | `OnAnswerJudged` は1問1回しか発火しない設計のため発生しない |

---

## 6. Dependencies

| 方向 | システム | 内容 |
|------|---------|------|
| 購読 | `answer-judgment` | `OnAnswerJudged(true)` で `_correctCount++` |
| 購読 | `game-flow` | `OnPhaseChanged(GameEnd)` で結果画面表示開始 |
| 呼び出し | `audio` | `PlaySE("SE_RESULT")` で結果SEを再生 |

---

## 7. Tuning Knobs

| 定数名 | デフォルト値 | 説明 |
|--------|------------|------|
| `TOTAL_QUESTIONS` | 5 | 総問題数（GameConstants） |
| `RANK_S` | 90 | Rank S の下限スコア |
| `RANK_A` | 70 | Rank A の下限スコア |
| `RANK_B` | 50 | Rank B の下限スコア |
| `RANK_C` | 30 | Rank C の下限スコア |
| `SCORE_COUNTUP_DURATION` | 1.5f | スコアカウントアップのアニメ秒数 |
| `RANK_SCALE_PEAK` | 1.2f | ランク文字の最大スケール |
| `RANK_SCALE_DURATION` | 0.5f | ランクスケールアニメ秒数 |

---

## 8. Acceptance Criteria

- [ ] 5問正解で `excitementScore = 100`、Rank S が表示される
- [ ] 0問正解で `excitementScore = 0`、Rank D が表示される
- [ ] スコアが 0 からカウントアップして最終値で止まる
- [ ] ランク文字がカウントアップ完了後にドン拡大する
- [ ] リトライボタン押下でタイトルシーンに遷移する
- [ ] 単体テスト: correctCount 0〜5 それぞれの score・rank が正しいこと
