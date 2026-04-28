# ADR-001: Assets/ フォルダ構成

**Status**: Accepted
**Date**: 2026-04-27

## Decision

`Assets/_EDMQuiz/` プレフィックス方式を採用する。

## Folder Structure

```
Assets/
├── _EDMQuiz/                      ← プロジェクト固有（先頭に固定）
│   ├── Scripts/
│   │   ├── Core/                  GameConstants, GamePhase, GameFlowManager
│   │   ├── Quiz/                  QuizQuestion, QuizDatabase, AnswerJudgment
│   │   ├── Audio/                 AudioManager
│   │   ├── BPM/                   BpmClock
│   │   ├── UI/                    HiraganaInputUI, ResultScreen 等
│   │   └── VFX/                   VFXDirector
│   ├── ScriptableObjects/
│   │   ├── QuizDatabase.asset
│   │   └── Questions/
│   ├── Prefabs/
│   │   ├── UI/
│   │   └── VFX/
│   ├── Sprites/
│   │   ├── Characters/
│   │   ├── Backgrounds/
│   │   └── UI/
│   └── Animations/
├── SoundData/                     ← CRI ADX（StreamingAssets 禁止）
│   ├── BGM.acb / BGM.acf
│   └── SE.acb
├── Scenes/
│   ├── TitleScene.unity
│   └── GameScene.unity
└── Settings/                      ← URP設定（変更しない）

## Rationale

- `_` プレフィックスで Project ウィンドウ先頭に固定され、CRI ADX が自動生成するフォルダと分離できる
- `SoundData/` は CRI ADX の制約（StreamingAssets 禁止）に対応するため最上位に配置
```
