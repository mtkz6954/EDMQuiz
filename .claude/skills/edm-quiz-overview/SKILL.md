---
name: edm-quiz-overview
description: ばくれつクイズしてる の全体像（コンセプト・プレイフロー・スコープ・技術スタック）を確認したいときに使用。実装の方針判断・設計レビュー・新規スクリプト作成前のコンテキスト取得に最適。
---

# ばくれつクイズしてる — Overview

## ゲームの核

EDMのビルドアップでリズムに乗りながら **ひらがな選択式4文字クイズ** に答え、ドロップの瞬間に演出が爆発する **1プレイ約3分** のリズム×クイズゲーム。

### 体験の2段階構造

1. **ビルドアップ中**: BGMビートに乗りながらひらがなを選んで4文字を組み立てる「ノっている感」
2. **ドロップ直後**: 正解→紙吹雪・歓声・ミラーボール／不正解→青ざめ・ブーイング・コケ演出

「クイズに正解する気持ちよさ」と「EDMのドロップに合わせる気持ちよさ」を2段階で積み上げて一気に解放する。

---

## プロトタイプスコープ

| 項目 | 値 |
|------|-----|
| 問題数 | 5問固定 |
| BGM | 1曲 |
| キャラ・背景 | 1パターン（5問共通） |
| 1プレイ時間 | 約3分 |
| 公開先 | unityroom (WebGL) |

---

## プレイフロー

```
TitleScene
  ↓ スタートボタン → BGM 開始 + GameScene へロード
GameScene
  ├ Question フェーズ（QUESTION_PHASE_SEC = 2.0s）
  ├ BuildUp フェーズ（BUILDUP_PHASE_SEC = BPM × BARS から逆算）
  ├ Drop フェーズ（DROP_REVEAL_SEC = 4.0s）
  ├ Next フェーズ（NEXT_TRANSITION_SEC = 1.5s）
  └ × 5問繰り返し → GameEnd → ResultPanel
```

`GamePhase` enum: `Idle`, `Question`, `BuildUp`, `Drop`, `Next`, `GameEnd`

---

## 技術スタック

| 領域 | 採用技術 |
|------|---------|
| Engine | Unity 6 LTS (6.3) / 6000.3.6f1 |
| Language | C# 9（namespace `EDMQuiz`） |
| Rendering | URP 2D |
| UI | **UI Toolkit (UXML/USS)** + TextMeshPro |
| Animation | DOTween Pro + DOVirtual.Float（UI Toolkit 用） |
| Audio | CRI ADX LE + Asset Support Addon（OnMemory） |
| Async | **UniTask**（コルーチン禁止） |
| Reactive | **R3**（イベントストリーム） |
| Inspector | NaughtyAttributes |
| Iteration | HotReload |

詳細: `.claude/docs/technical-preferences.md`

---

## システム構成（8システム）

```
Layer 1 Foundation:    [quiz-data] [audio]
Layer 2 Core:          [answer-judgment]→quiz-data
                       [bpm-sync]→audio
Layer 3 Orchestration: [game-flow]→answer-judgment, quiz-data, audio
Layer 4 Feature:       [input-ui]→game-flow, bpm-sync, quiz-data
                       [presentation-vfx]→game-flow, bpm-sync, audio, answer-judgment
Layer 5 Presentation:  [score-result]→game-flow, answer-judgment
```

各システムの詳細は `edm-quiz-{system-name}` skill を参照。

---

## シーン構成（ADR-002）

- **TitleScene** — タイトル + Start ボタン（BGM 再生のためユーザーインタラクション必須）
- **GameScene** — Managers + UIDocument（GamePanel + ResultPanel）+ ステージ + VFX

---

## Acceptance Criteria（プロジェクト全体）

- [ ] タイトル → 5問プレイ → 結果画面まで通しで動作
- [ ] PCクリック・スマホタップ両対応（UI Toolkit が自動対応）
- [ ] BGM ビートに合わせて UI がパルスする
- [ ] 正解時に紙吹雪・歓声SE・ミラーボール演出
- [ ] 不正解時に青ざめ・ブーイング・コケ演出
- [ ] 結果画面に盛り上がり度（数値）+ ランク文字
- [ ] WebGL ビルドで unityroom 公開可能

---

## 関連 Skill

- 各システム実装: `edm-quiz-quiz-data` / `edm-quiz-answer-judgment` / `edm-quiz-audio` / `edm-quiz-bpm-sync` / `edm-quiz-game-flow` / `edm-quiz-input-ui` / `edm-quiz-presentation-vfx` / `edm-quiz-score-result`
- 横断: `edm-quiz-coding-conventions` / `edm-quiz-async-reactive` / `edm-quiz-ui-toolkit`
- Unity Editor 操作: `uloop-*`
