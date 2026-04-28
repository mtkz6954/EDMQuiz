# Technical Preferences — EDMQuiz

**Last Updated**: 2026-04-28

## Engine & Language

- **Engine**: Unity 6 LTS (6.3) / 6000.3.6f1
- **Language**: C# 9
- **Namespace**: `EDMQuiz`（全スクリプトに必須）
- **Rendering**: URP 2D
- **Physics**: Unity 2D Physics（使用しない予定だが URP 2D テンプレートに含まれる）

## Input & Platform

- **Target Platforms**: WebGL (unityroom), スマートフォン (iOS/Android ブラウザ)
- **Input Methods**: タップ / マウスクリック（UI Toolkit `Button.clicked` のみ）
- **Primary Input**: タッチ/クリック
- **Gamepad Support**: None
- **Touch Support**: Full（UI Toolkit が自動対応）
- **Platform Notes**:
  - unityroom は StreamingAssets をビルドに含められない → CRI ADX は `Assets/SoundData/` に配置
  - WebGL は自動再生ポリシーあり → BGM はユーザーインタラクション後に再生
  - WebGL はシングルスレッド → DSP バスエフェクトは最小限に

## Naming Conventions

- **Classes / Methods / Properties**: PascalCase（例: `GameFlowManager`, `SubmitAnswer()`）
- **Private fields**: `_camelCase`（例: `_bgmPlayer`, `_inputBuffer`）
- **Constants**: ALL_CAPS（例: `TOTAL_QUESTIONS`, `BUILDUP_PHASE_SEC`）
- **Events / R3 Subjects**: `On` + PascalCase（例: `OnPhaseChanged`, `OnGameEnd`）
- **Enum values**: PascalCase（例: `GamePhase.BuildUp`）
- **ScriptableObjects**: 機能名のみ（例: `QuizQuestion`, `QuizDatabase`）
- **UXML / USS files**: kebab-case（例: `game-panel.uxml`, `hiragana-button.uss`）
- **USS class names**: kebab-case（例: `.hiragana-button`, `.is-active`）
- **VisualElement names (UXML id)**: kebab-case（例: `name="hiragana-buttons"`）
- **Files (C#)**: クラス名と同一

## Performance Budgets (WebGL)

- **Target FPS**: 60fps（デスクトップ）/ 30fps（スマホブラウザ）
- **Draw Calls**: < 50 / frame
- **Texture Memory**: < 256 MB total
- **ParticleSystem**: 最大パーティクル数 200 以下
- **Audio**: HCA コーデック使用（圧縮率重視）、ストリーミング禁止（OnMemory のみ）

## Forbidden Patterns

- `Find()` / `FindObjectOfType()` / `FindObjectsOfType()` を Update() 内で使用禁止
- `SendMessage()` / `BroadcastMessage()` 禁止（static event または R3 で代替）
- `Resources.Load()` 禁止（ScriptableObject または CRI ADX で管理）
- `StreamingAssets/` への CRI ADX データ配置禁止（unityroom WebGL 制約）
- `DontDestroyOnLoad()` は `AudioManager` のみ許可
- `Time.timeScale = 0` 中の DOTween は `.SetUpdate(true)` 必須
- 新規 `IEnumerator` コルーチンの作成禁止（UniTask `async UniTaskVoid` で代替）
- `Update()` 内のフレームポーリング型実装は避ける（R3 / UniTask で時間管理）
- UGUI（Canvas / RectTransform / Button (UnityEngine.UI)）の新規使用禁止 → **UI Toolkit を使う**

## Allowed Libraries / Addons

| ライブラリ | バージョン | 用途 |
|-----------|-----------|------|
| **DOTween Pro** | 最新 | アニメーション全般。UI Toolkit には `DOVirtual.Float` で対応 |
| **CRI ADX LE Unity Plugin** | v3.13+ | BGM/SE 再生 |
| **CRI Asset Support Addon** | v1.2+ | WebGL OnMemory ビルド対応 |
| **TextMeshPro** | Unity 同梱版 | テキスト表示（UI Toolkit Label と併用） |
| **UniTask** | 最新（git URL） | 非同期処理（async/await）・時間待機 |
| **R3** | v1.3.0（NuGet）+ R3.Unity（git URL） | リアクティブストリーム・イベント駆動 |
| **NaughtyAttributes** | 最新 | Inspector 拡張（[Button], [BoxGroup] 等） |
| **HotReload** | 最新 | エディタ Play 中のスクリプト即時反映 |
| **NuGetForUnity** | 最新（git URL） | NuGet パッケージ管理（R3 コア用） |

## ライブラリ使い分け方針

### イベント / メッセージング
- **静的グローバルイベント** → `static event Action<T>` または R3 `Subject<T>`
- **オブザーバブルストリーム** → R3 `Observable<T>` / `Subject<T>`（連続値・ビート・入力イベント）
- **MonoBehaviour 間の通知** → R3 + `AddTo(this)` で自動 dispose

### 非同期 / 時間制御
- **時間待機** → `await UniTask.Delay(TimeSpan.FromSeconds(x), cancellationToken: token)`
- **フェーズ進行のタイマー** → `UniTask` の async ループ + `CancellationToken`
- **DOTween との連携** → `await tween.AsyncWaitForCompletion()` or `await tween.ToUniTask()`
- **コルーチン (`StartCoroutine`)** は使用禁止 — UniTask へ移行

### UI（UI Toolkit）
- **画面構成**: UXML テンプレート（`Assets/_EDMQuiz/UI/Layouts/`）
- **スタイル**: USS（`Assets/_EDMQuiz/UI/Styles/`）
- **動的要素生成**: `VisualElement.Add()` または `VisualTreeAsset.CloneTree()`
- **アニメーション**: DOTween + `DOVirtual.Float` でスタイル値を補間
- **入力**: `Button.clicked += () => ...` または R3 `FromEvent`

### Inspector / デバッグ
- **テスト用ボタン** → `[Button("Test BGM Play")]`（NaughtyAttributes）
- **Inspector グルーピング** → `[BoxGroup("Audio")]`, `[Foldout("Tuning")]`
- **条件付き表示** → `[ShowIf]`, `[HideIf]`
- **必須参照** → `[Required]`

## Architecture Decisions Log

| ADR | 決定内容 |
|-----|---------|
| ADR-001 | フォルダ構成: `Assets/_EDMQuiz/` プレフィックス方式 |
| ADR-002 | シーン構成: 2シーン（TitleScene + GameScene） |
| ADR-003 | UI フレームワーク: **UI Toolkit + DOTween + TextMeshPro** |
| ADR-004 | CRI ADX 統合: Asset Support Addon + OnMemory |
| ADR-005 | 非同期・リアクティブ: UniTask + R3（2026-04-28 追加） |

## Engine Specialists Routing

- Unity 全般: `unity-specialist`（~/.claude/Claude-Code-Game-Studios 参照）
- Audio / CRI ADX: `audio-director` + `sound-designer`
- UI / UI Toolkit: `ux-designer` + `ui-programmer`
- VFX / DOTween: `visual-effects-artist` + `technical-artist`
