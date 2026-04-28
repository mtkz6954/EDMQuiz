# Directory Structure — EDMQuiz

## プロジェクトルート

```text
EDMQuiz/
├── CLAUDE.md                       # マスター設定（Game Studios エントリ）
├── .gitignore
├── .claude/                        # スキル・docs
│   ├── docs/                       # technical-preferences, coding-standards 等
│   └── skills/                     # uloop-* + プロジェクト固有 skill
├── design/
│   └── gdd/                        # 各システムの GDD（8 ファイル）
├── docs/
│   ├── adr/                        # Architecture Decision Records
│   └── engine-reference/unity/     # Unity 6 API リファレンス
├── production/
│   ├── review-mode.txt
│   ├── session-state/              # gitignored
│   └── session-logs/               # gitignored
├── Assets/                         # Unity アセット
├── Packages/                       # UPM + NuGet
├── ProjectSettings/
└── Library/                        # gitignored（Unity 自動生成）
```

## Unity Assets レイアウト（ADR-001 準拠）

```text
Assets/
├── _EDMQuiz/                       # プロジェクト固有（先頭固定でソート上位）
│   ├── Scripts/
│   │   ├── Core/                   # GameConstants, GamePhase, GameFlowManager, Async ヘルパー
│   │   │   └── Async/              # AsyncExtensions
│   │   ├── Quiz/                   # QuizQuestion, QuizDatabase, AnswerJudgment
│   │   ├── Audio/                  # AudioManager（CRI ADX ラップ）
│   │   ├── BPM/                    # BpmClock
│   │   ├── UI/                     # HiraganaInputUI, QuestionDisplay, ResultScreen
│   │   │   └── Tween/              # UIToolkitTweenExtensions（DOScale, DOFade）
│   │   └── VFX/                    # VFXDirector
│   ├── UI/                         # UI Toolkit リソース
│   │   ├── Layouts/                # *.uxml（title-panel, game-panel, result-panel）
│   │   ├── Styles/                 # *.uss（共通スタイル + コンポーネント別）
│   │   ├── Themes/                 # ThemeStyleSheet（必要に応じて）
│   │   └── Fonts/                  # TMP Font Asset
│   ├── ScriptableObjects/
│   │   ├── QuizDatabase.asset      # 問題マスター（1ファイル）
│   │   └── Questions/              # Q01_*.asset 〜 Q05_*.asset
│   ├── Prefabs/                    # （UI Toolkit 化により最小限）
│   │   └── VFX/                    # ConfettiParticle.prefab 等
│   ├── Scenes/                     # TitleScene.unity, GameScene.unity
│   ├── Sprites/
│   │   ├── Characters/
│   │   ├── Backgrounds/
│   │   └── UI/                     # アイコン、装飾
│   └── Animations/                 # AnimationClip（キャラ動作）
├── SoundData/                      # CRI ADX 配置先（_EDMQuiz 外、Asset Support Addon の規約）
│   ├── BGM.acb / BGM.acf
│   └── SE.acb
├── Plugins/                        # DOTween, CRI ADX, NaughtyAttributes 等の外部
├── Editor/                         # エディタ拡張・ビルドスクリプト
└── Settings/                       # URP 設定（Unity 自動生成）
```

## Tests レイアウト

```text
Assets/
└── _EDMQuiz/
    └── Tests/
        ├── EditMode/               # EditMode（Unity Test Framework）
        │   ├── AnswerJudgmentTests.cs
        │   ├── BpmClockTests.cs
        │   ├── ScoreCalculationTests.cs
        │   └── EDMQuiz.Tests.EditMode.asmdef
        └── PlayMode/               # PlayMode（統合テスト）
            └── EDMQuiz.Tests.PlayMode.asmdef
```

## Packages レイアウト

```text
Packages/
├── manifest.json                   # UPM 依存（DOTween Pro, CRI ADX, UniTask, R3.Unity, …）
├── packages-lock.json
└── nuget-packages/                 # NuGetForUnity 管理（gitignored）
    └── packages.config             # R3 v1.3.0, Microsoft.Bcl.* 等
```

## 命名規約のおさらい

- スクリプト: PascalCase + 同名ファイル（`GameFlowManager.cs`）
- UXML / USS: kebab-case（`game-panel.uxml`, `hiragana-button.uss`）
- ScriptableObject: 機能名（`QuizDatabase.asset`, `Q01_DropMoment.asset`）
- シーン: PascalCase（`TitleScene.unity`, `GameScene.unity`）
- Prefab: PascalCase（`ConfettiParticle.prefab`）
