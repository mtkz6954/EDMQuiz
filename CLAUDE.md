# Claude Code Game Studios — EDMQuiz

> ばくれつクイズしてる | Unity 6 LTS | WebGL (unityroom)

## Technology Stack

- **Engine**: Unity 6 LTS (6.3)
- **Language**: C# 9 | namespace: `EDMQuiz`
- **Rendering**: URP 2D
- **Input**: UGUI Button.onClick（New Input System 不使用 — UIタップ/クリックのみ）
- **Build Target**: WebGL (unityroom) + スマートフォン対応
- **Audio**: CRI ADX LE + Asset Support Addon（OnMemory方式）
- **Animation**: DOTween 4.x + AnimationClip
- **UI**: UGUI + TextMeshPro

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
