---
name: edm-quiz-audio
description: CRI ADX LE + Asset Support Addon (OnMemory) による BGM/SE 再生システム (AudioManager) の実装方針。WebGL 自動再生対策、BGM 開始タイミング、シーン遷移時の音声維持を扱うときに参照する。
---

# audio — CRI ADX 音声システム

## CRI ADX の API 詳細を見るには

| やりたいこと | 参照 Skill |
|------------|-----------|
| CRI ADX 全体像 | `cri-adx-overview` |
| ACB アセット化・OnMemory | `cri-adx-asset-support` |
| CriAtomExPlayer 再生制御 | `cri-adx-playback` |
| BGM 再生時刻取得（リズム同期）| `cri-adx-bpm-sync` |
| WebGL ビルド設定・unityroom 制約 | `cri-adx-webgl` |

このスキルは EDMQuiz 固有の AudioManager 実装方針のみ扱う。

## 責務

CRI ADX LE で BGM/SE を WebGL/OnMemory 方式で再生する `AudioManager` シングルトン。BPM 同期の基準クロック（`CriAtomExPlayback.GetTime()`）も提供する。

---

## 配置・デプロイ方針

| 項目 | 設定 |
|------|------|
| プラグイン | CRI ADX LE Unity Plugin v3.13+ |
| アドオン | Asset Support Addon v1.2+ |
| デプロイ | **OnMemory**（StreamingAssets 禁止） |
| アセット配置 | `Assets/SoundData/` （`_EDMQuiz/` 外） |
| キュー定義 | ADX2 ツールで作成し ACB/ACF/AWB を SoundData/ にエクスポート |

---

## 音声リソース（プロトタイプ）

| キュー名 | 種別 | 用途 |
|---------|------|------|
| `BGM_MAIN` | BGM | ゲーム中ループBGM（1曲） |
| `SE_CORRECT` | SE | 正解時・歓声 |
| `SE_INCORRECT` | SE | 不正解時・ブーイング |
| `SE_UI_TAP` | SE | ひらがなボタンタップ音 |
| `SE_RESULT` | SE | 結果画面表示音 |

---

## AudioManager 実装

```csharp
using CriWare;
using CriWare.Assets;  // Asset Support Addon
using UnityEngine;
using NaughtyAttributes;

namespace EDMQuiz
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [BoxGroup("Cue References")]
        [SerializeField] private CriAtomCueReference _bgmCue;
        [BoxGroup("Cue References")]
        [SerializeField] private CriAtomCueReference _seCorrectCue;
        [BoxGroup("Cue References")]
        [SerializeField] private CriAtomCueReference _seIncorrectCue;
        [BoxGroup("Cue References")]
        [SerializeField] private CriAtomCueReference _seUiTapCue;
        [BoxGroup("Cue References")]
        [SerializeField] private CriAtomCueReference _seResultCue;

        private CriAtomExPlayer _bgmPlayer;
        private CriAtomExPlayer _sePlayer;
        private CriAtomExPlayback _bgmPlayback;

        public bool IsBgmPlaying { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmPlayer = new CriAtomExPlayer();
            _sePlayer  = new CriAtomExPlayer();
        }

        void OnDestroy()
        {
            _bgmPlayer?.Dispose();
            _sePlayer?.Dispose();
        }

        [Button("Play BGM")]
        public void PlayBGM()
        {
            if (_bgmCue == null) { Debug.LogError("[AudioManager] BGM Cue 未設定"); return; }
            _bgmPlayer.SetCue(_bgmCue.AcbAsset.Handle, _bgmCue.CueId);
            _bgmPlayback = _bgmPlayer.Start();
            IsBgmPlaying = true;
        }

        public void StopBGM()
        {
            _bgmPlayer.Stop();
            IsBgmPlaying = false;
        }

        public void PlaySE(CriAtomCueReference cueRef)
        {
            if (cueRef == null) return;
            _sePlayer.SetCue(cueRef.AcbAsset.Handle, cueRef.CueId);
            _sePlayer.Start();
        }

        // ショートカット
        public void PlayCorrectSE()   => PlaySE(_seCorrectCue);
        public void PlayIncorrectSE() => PlaySE(_seIncorrectCue);
        public void PlayUiTapSE()     => PlaySE(_seUiTapCue);
        public void PlayResultSE()    => PlaySE(_seResultCue);

        /// <summary>BGM 再生開始からの経過秒数（BpmClock の基準・リズム同期用）</summary>
        /// <remarks>GetTime() ではなく GetTimeSyncedWithAudio() を使う（cri-adx-bpm-sync 参照）</remarks>
        public double GetBGMElapsedSeconds()
        {
            if (!IsBgmPlaying) return 0.0;
            if (_bgmPlayback.GetStatus() != CriAtomExPlayback.Status.Playing) return 0.0;
            long us = _bgmPlayback.GetTimeSyncedWithAudio();
            return us / 1_000_000.0;  // マイクロ秒 → 秒
        }
    }
}
```

---

## BGM 開始タイミング（重要）

WebGL の自動再生ポリシーにより **必ずユーザーインタラクション後** に `PlayBGM()` を呼ぶ:

```
TitleScene の Start ボタン押下
  ↓
AudioManager.Instance.PlayBGM()
  ↓
BpmClock.StartClock()  ← BGM 開始と同時にクロック計測開始
  ↓
GameScene にロード（BGM は DontDestroyOnLoad で継続）
```

---

## シーン遷移時の維持

`AudioManager` は `DontDestroyOnLoad` で TitleScene → GameScene を跨ぐ。
Cue Reference のシリアライズは `CriAtomCueReference` を `[SerializeField]` で。

---

## フォールバック（BGM 読み込み失敗）

```csharp
public double GetBGMElapsedSeconds()
{
    if (!IsBgmPlaying)
        return Time.unscaledTimeAsDouble - _bgmStartTime;  // フォールバック
    return _bgmPlayback.GetTimeSyncedWithAudio() / 1_000_000.0;
}
```

`BpmClock` 側でこの値を使い続けるので透過的にフェイルオーバー。

---

## Edge Cases

| ケース | 対応 |
|--------|------|
| WebGL 自動再生ブロック | Start ボタン押下後にしか PlayBGM を呼ばない |
| BGM 読み込み失敗 | `Time.unscaledTime` ベースに切替 |
| SE 連打 | CRI ADX のキューのリミット設定で対応（ADX2 Tool 側） |
| シーン遷移で音声切れる | `DontDestroyOnLoad` で防ぐ |

---

## チューニング項目

| 定数 | 値 |
|------|-----|
| `BGM_VOLUME` | 1.0f |
| `SE_VOLUME` | 1.0f |

CRI ADX 側でカテゴリボリュームを設定するのが推奨（`CriAtomExCategory`）。

---

## Acceptance Criteria

- [ ] WebGL ビルドで Start ボタン押下後に BGM 再生
- [ ] 正解時 SE_CORRECT、不正解時 SE_INCORRECT が鳴る
- [ ] TitleScene → GameScene で BGM 途切れない
- [ ] BGM 読み込み失敗時に無音続行
- [ ] `GetBGMElapsedSeconds()` が単調増加

---

## 関連 Skill

- BPM 同期: `edm-quiz-bpm-sync`
- フロー: `edm-quiz-game-flow`
- 演出: `edm-quiz-presentation-vfx`
- コード規約: `edm-quiz-coding-conventions`
