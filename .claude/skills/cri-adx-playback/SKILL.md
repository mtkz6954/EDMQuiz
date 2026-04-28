---
name: cri-adx-playback
description: CriAtomExPlayer を使った再生・停止・音量・ループ・複数 SE 同時再生の制御方法。CriAtomExPlayback ハンドルでの個別制御、初期化、Dispose を扱う。
---

# CRI ADX 再生制御 — CriAtomExPlayer

## ライフサイクル

```
CriAtomExPlayer 生成 (new)
   ↓
SetCue(acbHandle, cueId) でキュー指定
   ↓
Start() で再生開始 → CriAtomExPlayback を返す
   ↓
（再生中）playback.GetTime() / Pause() / Resume() で制御
   ↓
Stop() / playback.Stop() で停止
   ↓
Dispose() でリソース解放
```

---

## 基本実装

```csharp
using CriWare;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private CriAtomCueReference _cue;

    private CriAtomExPlayer _player;
    private CriAtomExPlayback _playback;

    void Start()
    {
        _player = new CriAtomExPlayer();
    }

    void OnDestroy()
    {
        _player?.Dispose();  // 必須
    }

    public void Play()
    {
        _player.SetCue(_cue.AcbAsset.Handle, _cue.CueId);
        _playback = _player.Start();
    }

    public void Stop()
    {
        _player.Stop();  // この Player の全再生を停止
    }

    public void StopThis()
    {
        _playback.Stop();  // この playback だけ停止
    }
}
```

---

## SetCue のオーバーロード

```csharp
// CueId 指定（Asset Support Addon 推奨）
_player.SetCue(_acbAsset.Handle, _cueRef.CueId);

// CueName 指定（旧来方式・タイプミスリスク）
_player.SetCue(_acbAsset.Handle, "BGM_MAIN");

// 既に SetCue 済みなら省略可（同じキューを再生）
_player.Start();
```

`SetCue` 後は `Start()` で何度でも再生可能。複数同時再生する SE に向く。

---

## 音量・ピッチ・パン

```csharp
_player.SetVolume(0.5f);          // 0.0 〜 1.0
_player.SetPitch(100f);           // セント単位（100 = +1 半音）
_player.SetPan3dAngle(45f);       // 角度（3D サウンド）
_player.Update();                 // 次回 Start() 時に反映
                                  // すでに再生中なら Update(playback) で即時反映
```

⚠️ **重要**: `SetVolume` などのパラメータ変更は `Update()` を呼ぶまで反映されない。

```csharp
// 再生中の playback に即時反映
_player.Update(_playback);
```

---

## 一時停止・再開

```csharp
// Player 全体
_player.Pause();
_player.Pause(false);  // = Resume

// 個別 playback
_playback.Pause();
_playback.Resume();
```

---

## 再生状態の確認

```csharp
var status = _playback.GetStatus();
// CriAtomExPlayback.Status: Prep / Playing / Stopping / Stop / Error / Removed

if (status == CriAtomExPlayback.Status.Playing) { ... }
```

```csharp
// 再生時刻（ミリ秒）
long timeMs = _playback.GetTime();
float timeSec = timeMs / 1000f;

// BPM 同期用は GetTimeSyncedWithAudio() を使う（cri-adx-bpm-sync 参照）
```

---

## Player の使い分けパターン

### ① 単一 BGM 用 Player

```csharp
private CriAtomExPlayer _bgmPlayer;
private CriAtomExPlayback _bgmPlayback;

public void PlayBGM(CriAtomCueReference cue)
{
    _bgmPlayer.Stop();  // 前の BGM を止める
    _bgmPlayer.SetCue(cue.AcbAsset.Handle, cue.CueId);
    _bgmPlayback = _bgmPlayer.Start();
}
```

### ② 複数 SE 同時発火用 Player

```csharp
private CriAtomExPlayer _sePlayer;

public void PlaySE(CriAtomCueReference cue)
{
    // Stop しないで Start すると並列再生
    _sePlayer.SetCue(cue.AcbAsset.Handle, cue.CueId);
    _sePlayer.Start();
}
```

CRI ADX は内部でボイスプールを管理しているため、複数同時再生で Player を増やす必要はない。1つの Player で並列再生される。

### ③ 同時発火数の制御

ボイスプールの上限を超えると古いボイスから止まる。ACB 側でキューの「ボイスリミット」を設定するか、CueLimit を Player API で制御:

```csharp
_player.LimitLoopCount(5);
```

---

## ループ再生

ループは ACB ファイル側でキュー作成時に設定する（ADX2 Tool）。コードでは:

```csharp
_player.LimitLoopCount(0);  // ループしない（強制ワンショット）
_player.LimitLoopCount(-1); // 無限ループ（ACB の設定に従う）
```

---

## カテゴリ・ボリューム

カテゴリ（BGM / SE / Voice）は ACF 側で定義し、ランタイムで一括制御:

```csharp
// カテゴリ全体のボリューム変更
CriAtomExCategory.SetVolumeByName("BGM", 0.5f);
CriAtomExCategory.SetVolumeByName("SE", 1.0f);
```

各キューを ACF カテゴリに紐付けておけば、`SetVolume` の手動管理が不要。

---

## エラーハンドリング

```csharp
if (_acbAsset.Handle == null)
{
    Debug.LogError("ACB がロードされていません");
    return;
}

if (_acbAsset.Handle.IsDisposed)
{
    Debug.LogError("ACB が解放済み");
    return;
}
```

---

## Edge Cases

| ケース | 対応 |
|--------|------|
| `Dispose` 忘れ | リーク発生 → 必ず `OnDestroy` で `_player.Dispose()` |
| ACB が未ロードで `SetCue` | 警告 → `Loaded == true` を確認 |
| 同じ Player で BGM と SE 混在 | 推奨しない → BGM 用と SE 用で Player を分ける |
| Pause 中に Stop | OK、状態遷移問題なし |
| 再生終了後の `_playback` を再利用 | NG、新規 Start() を呼ぶ |

---

## Acceptance Criteria

- [ ] `_player = new CriAtomExPlayer()` で初期化、`Dispose()` で解放
- [ ] `SetCue` + `Start` で再生
- [ ] 複数 SE が同時発火する
- [ ] BGM の `Stop` で停止
- [ ] `playback.GetTime()` で再生時刻取得

---

## 関連 Skill

- 概要: `cri-adx-overview`
- アセット: `cri-adx-asset-support`
- BPM 同期: `cri-adx-bpm-sync`
- WebGL: `cri-adx-webgl`
- EDMQuiz 統合: `edm-quiz-audio`
