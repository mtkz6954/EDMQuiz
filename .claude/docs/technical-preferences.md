# Technical Preferences — EDMQuiz

<!-- Populated by /setup-engine. Updated as decisions are made throughout development. -->

## Engine & Language

- **Engine**: Unity 6 LTS (6.3)
- **Language**: C# 9
- **Namespace**: `EDMQuiz`（全スクリプトに必須）
- **Rendering**: URP 2D
- **Physics**: Unity 2D Physics（使用しない予定だが URP 2D テンプレートに含まれる）

## Input & Platform

- **Target Platforms**: WebGL (unityroom), スマートフォン (iOS/Android ブラウザ)
- **Input Methods**: タップ / マウスクリック（UGUI Button.onClick のみ）
- **Primary Input**: タッチ/クリック
- **Gamepad Support**: None
- **Touch Support**: Full（UGUI は自動対応）
- **Platform Notes**:
  - unityroom は StreamingAssets をビルドに含められない → CRI ADX は Assets/SoundData/ に配置
  - WebGL は自動再生ポリシーあり → BGM はユーザーインタラクション後に再生
  - WebGL はシングルスレッド → DSP バスエフェクトは最小限に

## Naming Conventions

- **Classes / Methods / Properties**: PascalCase（例: `GameFlowManager`, `SubmitAnswer()`）
- **Private fields**: `_camelCase`（例: `_bgmPlayer`, `_inputBuffer`）
- **Constants**: ALL_CAPS（例: `TOTAL_QUESTIONS`, `BUILDUP_PHASE_SEC`）
- **Events**: `On` + PascalCase（例: `OnPhaseChanged`, `OnGameEnd`）
- **Enum values**: PascalCase（例: `GamePhase.BuildUp`）
- **ScriptableObjects**: 機能名のみ（例: `QuizQuestion`, `QuizDatabase`）
- **Files**: クラス名と同一

## Performance Budgets (WebGL)

- **Target FPS**: 60fps（デスクトップ）/ 30fps（スマホブラウザ）
- **Draw Calls**: < 50 / frame
- **Texture Memory**: < 256 MB total
- **ParticleSystem**: 最大パーティクル数 200 以下
- **Audio**: HCA コーデック使用（圧縮率重視）、ストリーミング禁止（OnMemory のみ）

## Forbidden Patterns

- `Find()` / `FindObjectOfType()` / `FindObjectsOfType()` を Update() 内で使用禁止
- `SendMessage()` / `BroadcastMessage()` 禁止（static event で代替）
- `Resources.Load()` 禁止（ScriptableObject または CRI ADX で管理）
- `StreamingAssets/` への CRI ADX データ配置禁止（unityroom WebGL 制約）
- `DontDestroyOnLoad()` は `AudioManager` のみ許可
- `Time.timeScale = 0` 中の DOTween は `.SetUpdate(true)` 必須

## Allowed Libraries / Addons

| ライブラリ | バージョン | 用途 |
|-----------|-----------|------|
| DOTween (HOTween v2) | 最新 | UI/VFXアニメーション全般 |
| CRI ADX LE Unity Plugin | v3.13+ | BGM/SE 再生 |
| Asset Support Addon | v1.2+ | WebGL OnMemory ビルド対応 |
| TextMeshPro | Unity 同梱版 | テキスト表示 |

## Architecture Decisions Log

| ADR | 決定内容 |
|-----|---------|
| ADR-001 | フォルダ構成: `Assets/_EDMQuiz/` プレフィックス方式 |
| ADR-002 | シーン構成: 2シーン（TitleScene + GameScene） |
| ADR-003 | UI フレームワーク: UI Toolkit + DOTween |
| ADR-004 | CRI ADX 統合: Asset Support Addon + OnMemory（記録済み） |

## Engine Specialists Routing

- Unity 全般: `unity-specialist`（~/.claude/Claude-Code-Game-Studios 参照）
- Audio / CRI ADX: `audio-director` + `sound-designer`
- UI / UGUI: `ux-designer` + `ui-programmer`
- VFX / DOTween: `visual-effects-artist` + `technical-artist`
