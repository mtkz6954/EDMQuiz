# Systems Index — ばくれつクイズしてる

**Last Updated**: 2026-04-28
**Review Mode**: Solo
**Total Systems**: 8（全て MVP）

---

## Tech Stack（参照）

| 領域 | 採用技術 |
|------|---------|
| エンジン | Unity 6 LTS (6.3) / URP 2D |
| UI | UI Toolkit (UXML/USS) + TextMeshPro |
| アニメーション | DOTween Pro + DOVirtual.Float（UI Toolkit 用） |
| 音声 | CRI ADX LE + Asset Support Addon（OnMemory） |
| 非同期 | UniTask（コルーチン代替） |
| イベント | R3（Subject + Observable + AddTo） |
| Inspector | NaughtyAttributes |
| 開発支援 | HotReload, NuGetForUnity |

詳細: [`technical-preferences.md`](../../.claude/docs/technical-preferences.md)

---

## Systems Enumeration

| # | System Slug | 担当領域 | 優先度 | 設計状態 |
|---|-------------|---------|--------|---------|
| 1 | `game-flow` | フェーズ管理・5問ループ・タイマー（UniTask） | MVP | GDD ✅ |
| 2 | `input-ui` | ひらがなボタン（UI Toolkit）・4文字アセンブリ・入力ロック | MVP | GDD ✅ |
| 3 | `quiz-data` | ScriptableObject スキーマ・問題データ定義 | MVP | GDD ✅ |
| 4 | `answer-judgment` | 完全一致判定・R3 Subject で正誤通知 | MVP | GDD ✅ |
| 5 | `bpm-sync` | CRI ADX dspTime + R3 OnBeat/OnBar | MVP | GDD ✅ |
| 6 | `presentation-vfx` | 正解/不正解演出・UI Toolkit + DOVirtual + ParticleSystem | MVP | GDD ✅ |
| 7 | `audio` | CRI ADX LE 管理・BGM/SE・WebGL OnMemory | MVP | GDD ✅ |
| 8 | `score-result` | 盛り上がり度計算・ランク・結果画面 (UI Toolkit) | MVP | GDD ✅ |

---

## Dependency Map

```
Layer 1 — Foundation（依存なし）
  [quiz-data]   [audio]

Layer 2 — Core
  [answer-judgment] → quiz-data
  [bpm-sync]        → audio（CriAtomExPlayback.GetTime() を基準クロック）

Layer 3 — Orchestration
  [game-flow]   → answer-judgment, quiz-data, audio

Layer 4 — Feature
  [input-ui]          → game-flow, bpm-sync, quiz-data
  [presentation-vfx]  → game-flow, bpm-sync, audio, answer-judgment

Layer 5 — Presentation
  [score-result] → game-flow, answer-judgment
```

---

## Cross-cutting Components（横断ユーティリティ）

| コンポーネント | 役割 | 配置 |
|---------------|------|------|
| `GameConstants` | 全ゲーム定数（BPM, ANSWER_LENGTH, RANK 閾値, etc.） | `Scripts/Core/` |
| `GamePhase` | フェーズ enum | `Scripts/Core/` |
| `UIToolkitTweenExtensions` | UI Toolkit × DOTween ヘルパー（DOScale, DOFade） | `Scripts/UI/Tween/` |
| `AsyncExtensions` | UniTask × DOTween 連携、CancellationToken ヘルパー | `Scripts/Core/Async/` |

---

## Design Order（実装推奨順）

```
0. GameConstants / GamePhase / UIToolkitTweenExtensions  ← 横断ユーティリティ
1. quiz-data          → データ基盤
2. answer-judgment    → quiz-data 後すぐ実装可能
3. audio              → CRI ADX セットアップ（WebGL対応）
4. bpm-sync           → audio の後（CriAtomExPlayback 参照）
5. game-flow          → 全依存が揃ったら中央司令塔を構築
6. input-ui           → game-flow + bpm-sync + UI Toolkit
7. presentation-vfx   → game-flow + bpm-sync + audio + UI Toolkit
8. score-result       → 最後の仕上げ
```

---

## High-Risk Systems

| システム | リスク | 理由 |
|---------|--------|------|
| `bpm-sync` | HIGH | CRI ADX `CriAtomExPlayback.GetTime()` の WebGL 動作確認が必要 |
| `audio` | HIGH | CRI ADX × WebGL × Asset Support Addon の組み合わせ検証 |
| `presentation-vfx` | MEDIUM | UI Toolkit + DOTween 連携、CancellationToken 管理 |
| `input-ui` | MEDIUM | UI Toolkit の動的ボタン生成、R3 イベント合成 |

---

## Progress Tracker

| System | GDD | Code | Tested |
|--------|-----|------|--------|
| quiz-data | ✅ | ☐ | ☐ |
| answer-judgment | ✅ | ☐ (旧版あり、要 R3 化) | ☐ |
| audio | ✅ | ☐ (CRI ADX 統合待ち) | ☐ |
| bpm-sync | ✅ | ☐ (要 R3 化) | ☐ |
| game-flow | ✅ | ☐ (要 UniTask 化) | ☐ |
| input-ui | ✅ | ☐ (要 UI Toolkit 化) | ☐ |
| presentation-vfx | ✅ | ☐ (要 UI Toolkit + UniTask 化) | ☐ |
| score-result | ✅ | ☐ (要 UI Toolkit 化) | ☐ |
