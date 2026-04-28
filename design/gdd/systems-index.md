# Systems Index — ばくれつクイズしてる

**Last Updated**: 2026-04-27
**Review Mode**: Solo
**Total Systems**: 8（全て MVP）

---

## Systems Enumeration

| # | System Slug | 担当領域 | 優先度 | 設計状態 |
|---|-------------|---------|--------|---------|
| 1 | `game-flow` | フェーズ管理・5問ループ・タイマー制御 | MVP | Not Started |
| 2 | `input-ui` | ひらがなボタン・4文字アセンブリ・入力ロック | MVP | Not Started |
| 3 | `quiz-data` | ScriptableObject スキーマ・問題データ定義 | MVP | Not Started |
| 4 | `answer-judgment` | 完全一致判定・正誤イベント発火 | MVP | Not Started |
| 5 | `bpm-sync` | CRI ADX dspTime + DOTween タイミング制御 | MVP | Not Started |
| 6 | `presentation-vfx` | 正解/不正解演出・VFX・アニメーション | MVP | Not Started |
| 7 | `audio` | CRI ADX LE 管理・BGM/SE・WebGL OnMemory | MVP | Not Started |
| 8 | `score-result` | 盛り上がり度計算・ランク・結果画面 | MVP | Not Started |

---

## Dependency Map

```
Layer 1 — Foundation（依存なし）
  [quiz-data]   [audio]

Layer 2 — Core
  [answer-judgment] → quiz-data
  [bpm-sync]        → audio（CRI ADX dspTime を基準クロックとして使用）

Layer 3 — Orchestration
  [game-flow]   → answer-judgment, quiz-data

Layer 4 — Feature
  [input-ui]          → game-flow, bpm-sync, quiz-data
  [presentation-vfx]  → game-flow, bpm-sync, audio

Layer 5 — Presentation
  [score-result] → game-flow
```

---

## Design Order（実装推奨順）

```
1. quiz-data          → データ基盤を最初に確定
2. answer-judgment    → quiz-data が揃えばすぐ実装可能
3. audio              → CRI ADX セットアップ（WebGL対応含む）
4. bpm-sync           → audio の後に実装
5. game-flow          → 全依存が揃ったら中央司令塔を構築
6. input-ui           → game-flow + bpm-sync に乗せる
7. presentation-vfx   → game-flow + bpm-sync + audio を束ねる
8. score-result       → 最後の仕上げ
```

---

## High-Risk Systems

| システム | リスク | 理由 |
|---------|--------|------|
| `bpm-sync` | HIGH | CRI ADX dspTime API が Unity 6 + WebGL で動作するか要確認 |
| `audio` | HIGH | CRI ADX × WebGL × Asset Support Addon の組み合わせ検証が必要 |
| `presentation-vfx` | MEDIUM | `Time.timeScale=0` と DOTween の相互作用に注意 |

---

## Progress Tracker

| System | GDD | Code | Tested |
|--------|-----|------|--------|
| quiz-data | ☐ | ☐ | ☐ |
| answer-judgment | ☐ | ☐ | ☐ |
| audio | ☐ | ☐ | ☐ |
| bpm-sync | ☐ | ☐ | ☐ |
| game-flow | ☐ | ☐ | ☐ |
| input-ui | ☐ | ☐ | ☐ |
| presentation-vfx | ☐ | ☐ | ☐ |
| score-result | ☐ | ☐ | ☐ |
