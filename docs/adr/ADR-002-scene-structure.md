# ADR-002: シーン構成

**Status**: Accepted
**Date**: 2026-04-27

## Decision

2シーン構成（TitleScene + GameScene）を採用する。

## Scene Structure

### TitleScene.unity
```
TitleScene
├── [UI Root] Canvas
│   └── TitlePanel
│       ├── TitleText            "ばくれつクイズしてる"
│       ├── StartButton          → OnClick: LoadScene("GameScene") + PlayBGM()
│       └── CreditText
└── Main Camera
```

### GameScene.unity
```
GameScene
├── [Managers]
│   ├── GameFlowManager          GameFlowManager.cs
│   ├── AudioManager             AudioManager.cs
│   ├── BpmClock                 BpmClock.cs
│   ├── AnswerJudgment           AnswerJudgment.cs
│   ├── CriWareErrorHandler      CRI ADX 自動生成
│   ├── CriWareLibraryInitializer CRI ADX 自動生成
│   └── CRIWARE                  CriAtomAssets（DeployType: OnMemory）
│
├── [UI Root] Canvas (Screen Space - Camera)
│   ├── GamePanel                プレイ中
│   │   ├── QuestionText         QuestionDisplay.cs
│   │   ├── AnswerArea           4マス
│   │   ├── HiraganaButtons      HiraganaInputUI.cs（動的生成）
│   │   ├── BackspaceButton
│   │   └── ConfirmButton
│   └── ResultPanel              ResultScreen.cs
│       ├── ScoreText
│       ├── RankText
│       └── ReplayButton
│
├── [Stage]
│   ├── Background               背景スプライト
│   ├── Funnymon                 Animator 付き
│   └── MirrorBall               DOTween で回転
│
└── [VFX]
    ├── VFXDirector              VFXDirector.cs
    ├── ConfettiParticle         ParticleSystem
    └── ScreenOverlay            不正解時の青ざめパネル
```

## Rationale

- BGM 開始を TitleScene の StartButton 押下後にすることで WebGL 自動再生ポリシーを回避できる
- GameScene に全ゲームロジックを集中させることでシングルシーンに近い実装の簡潔さを保ちつつ、タイトル画面を分離できる
- ResultPanel は GameScene 内パネルとして実装（シーン遷移コストなし）
