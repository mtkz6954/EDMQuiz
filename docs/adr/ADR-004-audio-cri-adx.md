# ADR-004: 音声システム — CRI ADX LE + Asset Support Addon (OnMemory)

**Status**: Accepted  
**Date**: 2026-04-28  
**Author**: EDMQuiz Team

---

## Context

WebGL (unityroom) ターゲットで BGM/SE を再生するにあたり、以下の制約がある：

1. **WebGL 自動再生ポリシー**: ブラウザはユーザーインタラクションなしのオーディオ再生をブロックする
2. **unityroom の StreamingAssets 制約**: `StreamingAssets/` フォルダはビルドに同梱されない場合がある
3. **BPM 同期精度**: リズムゲームとして音楽の再生位置を高精度で取得する必要がある
4. **WebGL シングルスレッド**: 音声処理のオーバーヘッドを最小化する必要がある

Unity 組み込み `AudioSource` では BPM 同期精度が不足するため、CRI ADX LE (CRI Middleware) を採用した。

---

## Decision

**CRI ADX LE Unity Plugin v3.13+** を使用し、**Asset Support Addon v1.2+** を組み合わせる。

### デプロイ方式: OnMemory

| 方式 | 説明 | 採用理由 |
|------|------|---------|
| **OnMemory** | ACB/AWB を Unity アセットとして埋め込む | StreamingAssets 不使用 → unityroom 対応 |
| StreamingAssets | 従来の CRI ADX 配置方式 | unityroom WebGL で動作不可のため **不採用** |

### キュー参照: CriAtomCueReference

Asset Support Addon が提供する `CriAtomCueReference` を `[SerializeField]` でシリアライズし、Inspector でキューを型安全に設定する。文字列によるキュー名指定は**採用しない**（タイポ検出が遅い、リファクタリング困難）。

> **注意 (2026-05-03 現在)**: Asset Support Addon 未インストールのため、`AudioManager.cs` は暫定的に文字列ベース (`CriAtom.GetAcb()`) + AudioSource フォールバックで動作している。Addon インストール後に `CriAtomCueReference` パターンへ移行すること。

### BPM 同期: GetTimeSyncedWithAudio

`CriAtomExPlayback.GetTime()` ではなく `GetTimeSyncedWithAudio()` を使用する。後者は DSP クロックと同期した高精度な再生時刻を返す（`BpmClock` の基準値として使用）。

### AudioManager の責務

- **シングルトン** (`DontDestroyOnLoad`): TitleScene → GameScene を跨いで BGM を継続再生
- **BGM Player**: `CriAtomExPlayer` インスタンスを1つ専有
- **SE Player**: `CriAtomExPlayer` インスタンスを1つ専有（SE 連打は ADX2 Tool のキューリミットで制御）
- **BPM 同期インターフェース**: `GetBGMElapsedSeconds()` を公開し `BpmClock` が購読
- **WebGL 自動再生対応**: `PlayBGM()` はユーザーインタラクション後にのみ呼ぶ (TitleScreen の START ボタン押下)

---

## Alternatives Considered

| 案 | 評価 |
|----|------|
| Unity AudioSource のみ | BPM 同期精度が不足 (`AudioSource.time` は DSP クロックと非同期)。**不採用** |
| FMOD Studio | ライセンス費用、Unity パッケージとの統合コスト大。**不採用** |
| Wwise | 同上。**不採用** |
| CRI ADX LE (StreamingAssets 方式) | unityroom WebGL で動作不可。**不採用** |
| **CRI ADX LE + Asset Support Addon (OnMemory)** | BPM 精度・WebGL 対応・実績あり。**採用** |

---

## Consequences

**Positive:**
- `GetTimeSyncedWithAudio()` でサンプル精度の BPM 同期が実現できる
- OnMemory 方式により unityroom WebGL でも音声が正常に動作する
- `CriAtomCueReference` により Inspector でキューを視覚的に確認・設定できる

**Negative:**
- Asset Support Addon のセットアップが必要（ADX LE 本体とは別インストール）
- ACB/AWB/ACF は ADX2 Tool (CRI ADX LE の外部ツール) で作成が必要
- WebGL ビルドで CRI ADX のネイティブプラグインが正しくリンクされているか要確認

---

## Implementation Notes

```
Assets/SoundData/          ← _EDMQuiz/ 外に配置 (ADR-001 例外)
├── Project.acf
├── BGM.acb
└── SE.acb
```

| キュー名 | 種別 | 用途 |
|---------|------|------|
| `BGM_MAIN` | BGM | ゲーム中ループ BGM |
| `SE_CORRECT` | SE | 正解時 |
| `SE_INCORRECT` | SE | 不正解時 |
| `SE_UI_TAP` | SE | ひらがなボタンタップ |
| `SE_RESULT` | SE | 結果画面表示 |

---

## Related

- `Assets/_EDMQuiz/Scripts/Audio/AudioManager.cs`
- `Assets/_EDMQuiz/Scripts/BPM/BpmClock.cs`
- Skill: `edm-quiz-audio`, `cri-adx-asset-support`, `cri-adx-bpm-sync`
- ADR-002 (シーン構成 — DontDestroyOnLoad の根拠)
- ADR-005 (UniTask + R3 — BpmClock の `OnBeat` ストリーム)
