# Claude Code Game Studios — EDMQuiz

> ばくれつクイズしてる | Unity 6 LTS | WebGL (unityroom)

## Technology Stack

- **Engine**: Unity 6 LTS (6.3) / 6000.3.6f1
- **Language**: C# 9 | namespace: `EDMQuiz`
- **Rendering**: URP 2D
- **Input**: UI Toolkit `Button.clicked`（New Input System 不使用 — UIタップ/クリックのみ）
- **Build Target**: WebGL (unityroom) + スマートフォン対応
- **Audio**: CRI ADX LE + Asset Support Addon（OnMemory方式）
- **Animation**: DOTween Pro + AnimationClip + DOVirtual.Float（UI Toolkit 用）
- **UI**: UI Toolkit (UXML/USS) + TextMeshPro
- **Async**: UniTask（コルーチン代替）
- **Reactive**: R3（イベントストリーム・購読管理）
- **Inspector**: NaughtyAttributes（[Button], [BoxGroup] 等）
- **Iteration**: HotReload（エディタ Play 中のスクリプト即時反映）

## Engine Version Reference

@docs/engine-reference/unity/VERSION.md

## Technical Preferences

@.claude/docs/technical-preferences.md

## Coding Standards

@.claude/docs/coding-standards.md

## Coordination Rules

@.claude/docs/coordination-rules.md

## Context Management

@.claude/docs/context-management.md

## Project Structure

@.claude/docs/directory-structure.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question → Options → Decision → Draft → Approval → Write**

- Agents MUST ask "May I write this to [filepath]?" before using Write/Edit tools
- Agents MUST show drafts before requesting approval
- No commits without user instruction

## First Session?

Run `/start` to begin guided onboarding.
Engine is configured — next step: `/map-systems`

---

## EDMQuiz Skill インデックス

ゲーム企画書から各分野の詳細設計・実装方針を Skill にまとめてある。
実装時に該当領域の Skill を `/edm-quiz-{name}` で呼び出して参照する。

### 全体・横断
| Skill | 内容 |
|-------|------|
| [`edm-quiz-overview`](.claude/skills/edm-quiz-overview/SKILL.md) | ゲーム全体像・コンセプト・技術スタック・システム構成 |
| [`edm-quiz-coding-conventions`](.claude/skills/edm-quiz-coding-conventions/SKILL.md) | 命名規則・禁止パターン・許可ライブラリ・コードスタイル |
| [`edm-quiz-async-reactive`](.claude/skills/edm-quiz-async-reactive/SKILL.md) | UniTask + R3 のパターン集（タイマー・購読・キャンセル） |
| [`edm-quiz-ui-toolkit`](.claude/skills/edm-quiz-ui-toolkit/SKILL.md) | UI Toolkit (UXML/USS) + DOTween 連携パターン |

### システム別（8 システム）
| Skill | 担当領域 |
|-------|---------|
| [`edm-quiz-quiz-data`](.claude/skills/edm-quiz-quiz-data/SKILL.md) | 問題データ ScriptableObject |
| [`edm-quiz-answer-judgment`](.claude/skills/edm-quiz-answer-judgment/SKILL.md) | 完全一致判定 + R3 通知 |
| [`edm-quiz-audio`](.claude/skills/edm-quiz-audio/SKILL.md) | CRI ADX LE + Asset Support Addon (OnMemory) |
| [`edm-quiz-bpm-sync`](.claude/skills/edm-quiz-bpm-sync/SKILL.md) | BPM 同期クロック (OnBeat/OnBar) |
| [`edm-quiz-game-flow`](.claude/skills/edm-quiz-game-flow/SKILL.md) | フェーズ管理 + UniTask タイマー |
| [`edm-quiz-input-ui`](.claude/skills/edm-quiz-input-ui/SKILL.md) | UI Toolkit ひらがなボタン入力 |
| [`edm-quiz-presentation-vfx`](.claude/skills/edm-quiz-presentation-vfx/SKILL.md) | 演出ディレクター（DOTween + UniTask + ParticleSystem） |
| [`edm-quiz-score-result`](.claude/skills/edm-quiz-score-result/SKILL.md) | スコア計算 + 結果画面 |

### 関連
- ゲーム企画書: [`design/gdd/game-concept.md`](design/gdd/game-concept.md)
- システム索引: [`design/gdd/systems-index.md`](design/gdd/systems-index.md)
- ADRs: [`docs/adr/`](docs/adr/)
- Unity Editor 操作: `uloop-*` skills
