# Game Concept — ばくれつクイズしてる

**System Slug**: `game-concept`
**Last Updated**: 2026-04-27
**Status**: Approved

---

## 1. Overview

EDMのビルドアップでリズムに乗りながらひらがな選択式4文字クイズに答え、ドロップの瞬間に演出が爆発する1プレイ約3分のリズム×クイズゲーム。「クイズに正解する気持ちよさ」と「EDMのドロップに合わせる気持ちよさ」を2段階で積み上げて一気に解放する体験を核とする。SNS・配信映えを最優先に設計し、unityroom にて無料公開する。

---

## 2. Player Fantasy

- **ビルドアップ中**: BGMのビートに身を任せながらひらがなを選んでいく「ノっている感」
- **ドロップ直後**: 正解した瞬間に音・キャラ・背景・UIが全部ハネて「爆盛り上がり」
- **不正解時**: 落差で笑いが取れる「スベり演出」でネタとして消費できる
- **全体**: 「見ているだけで楽しい」配信・実況向けの視覚的派手さ

---

## 3. Detailed Rules

### プレイフロー
```
タイトル画面
  ↓ スタートボタン
問題表示（Question フェーズ）
  ↓ BGM 開始・タイマー起動
ビルドアップ（BuildUp フェーズ）
  ↓ プレイヤーがひらがなを選んで4文字を組み立てる
ドロップ（Drop フェーズ）
  ↓ 入力確定 → 正誤判定 → 演出発火
次問題へ（Next フェーズ）× 5問
  ↓
結果画面（GameEnd）
```

### スコープ（プロトタイプ版）
| 項目 | 値 |
|------|-----|
| 問題数 | 5問固定 |
| BGM | 1曲 |
| キャラ・背景パターン | 1パターン（5問共通） |
| 1プレイ時間 | 約3分 |

### プラットフォーム
- PCブラウザ（unityroom WebGL）
- スマートフォン（タップ操作対応）
- 画面サイズ基準: 1280×720（16:9）、スマホは縦横両対応不要・横固定

### エンジン・主要技術
| 技術 | バージョン・用途 |
|------|----------------|
| Unity | 6 LTS (6.3) |
| レンダリング | URP |
| UI | UGUI（DOTween との安定した相互運用性を優先） |
| アニメーション | DOTween 4.x + AnimationClip |
| BPM同期 | CRI ADX dspTime + DOTween |
| サウンド | CRI ADX（ADX2 LE または製品版） |
| データ | ScriptableObject（問題データ） |

---

## 4. Formulas

### 盛り上がり度
```
excitementScore = (correctCount / TOTAL_QUESTIONS) * 100
```
- `correctCount`: 正解した問題数（0〜5）
- `TOTAL_QUESTIONS`: 5（定数）
- 結果範囲: 0, 20, 40, 60, 80, 100

### ランク判定
```
score >= 90  → Rank S（神）
score >= 70  → Rank A（最高）
score >= 50  → Rank B（いい感じ）
score >= 30  → Rank C（まあまあ）
score  < 30  → Rank D（スベった）
```

---

## 5. Edge Cases

| ケース | 対応 |
|--------|------|
| 4文字未入力で Drop フェーズになった場合 | 不正解扱い・演出発火 |
| BGM 読み込み失敗 | フォールバック: 無音でゲーム続行（演出タイマーは AudioSettings.dspTime で継続） |
| WebGL でのオーディオ自動再生ブロック | タイトル画面のボタン押下後に BGM 開始（ユーザーインタラクション後） |
| タップ連打による多重入力 | 確定ボタンに isConfirmed フラグで二重実行を防止 |

---

## 6. Dependencies

このゲームコンセプトを実現するシステム群:

- `game-flow` — フェーズ管理・タイマー制御
- `input-ui` — ひらがな入力UI
- `quiz-data` — 問題データ・ScriptableObject
- `answer-judgment` — 正誤判定
- `bpm-sync` — BPM同期クロック
- `presentation-vfx` — 正解/不正解演出
- `audio` — CRI ADX オーディオ管理
- `score-result` — 盛り上がり度・結果画面

---

## 7. Tuning Knobs

| 定数名 | デフォルト値 | 説明 |
|--------|-------------|------|
| `TOTAL_QUESTIONS` | 5 | 1プレイの問題数 |
| `QUESTION_PHASE_SEC` | 2.0f | 問題文表示の秒数 |
| `BUILDUP_PHASE_SEC` | 24.0f | ビルドアップ（入力可能）の秒数 |
| `DROP_REVEAL_SEC` | 4.0f | ドロップ演出の秒数 |
| `NEXT_TRANSITION_SEC` | 1.5f | 次問題への遷移秒数 |

---

## 8. Acceptance Criteria

- [ ] タイトル→5問プレイ→結果画面まで通しで動作する
- [ ] PCクリック・スマホタップ両方でひらがなボタンが反応する
- [ ] BGM開始後、ビートに合わせてUIがパルスアニメーションする
- [ ] 正解時に紙吹雪・ミラーボール・歓声SEが同時発火する
- [ ] 不正解時に青ざめ・ブーイングSE・コケアニメが同時発火する
- [ ] 結果画面に盛り上がり度（数値）とランク文字が表示される
- [ ] WebGL ビルドで unityroom に公開可能な状態になる
