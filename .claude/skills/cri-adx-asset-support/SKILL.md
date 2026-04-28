---
name: cri-adx-asset-support
description: CRI Asset Support Addon の使い方。CriAtomCueReference / CriAtomAcbAsset / DeployType (OnMemory)、ACB/AWB/ACF を Unity アセットとしてインポートする方法、StreamingAssets を使わない配置方針を扱う。
---

# CRI Asset Support Addon

公式マニュアル: https://game.criware.jp/manual/unity_plugin/latest/contents/addon4u_assetsupport.html

## このアドオンが解決する問題

従来の CRI ADX は ACB/AWB/ACF を `StreamingAssets/` に置き、文字列でキュー名を指定する必要があった。

**問題:**
- `StreamingAssets` は WebGL（特に unityroom）で使用に制約あり
- キュー名の文字列指定でタイプミス検出が遅い
- アセット参照が型安全でない

**Asset Support Addon が提供:**
- ACB/AWB/ACF を **Unity アセット化**して任意のフォルダに配置可能
- `CriAtomCueReference` で Inspector からタイプセーフにキュー選択
- DeployType を **OnMemory** にすることで WebGL 制約回避

---

## セットアップ

### 1. インストール

CRI ADX LE を導入後、追加で:
```
cri/addons/asset_support/plugin/
└─ cri_asset_support_addon_vX.X.XX_ja.unitypackage
```

を Unity に Import。

### 2. ACB/AWB/ACF の配置

```
Assets/SoundData/        ← 任意フォルダ（StreamingAssets ではない）
├─ Project.acf
├─ BGM.acb
├─ BGM.awb              ← OnMemory なら不要
└─ SE.acb
```

### 3. アセットインポート設定

各 ACB を選択し Inspector で:

| 設定 | 値 |
|------|-----|
| **Deploy Type** | `OnMemory`（unityroom WebGL 必須） |
| **Acf Asset** | `Project.acf` を割り当て |
| **Memory Allocation** | デフォルト |

`OnMemory` は ACB/AWB を Unity ビルドに埋め込む方式（StreamingAssets 不使用）。

---

## CriAtomCueReference

`[SerializeField] private CriAtomCueReference _bgmCue;` のように Inspector で参照可能なキュー指定。

### Inspector での選択

1. ACB アセットをドロップ
2. プルダウンで CueName を選択
3. `(AcbAsset, CueId)` のペアとしてシリアライズされる

### コードからの利用

```csharp
public class AudioManager : MonoBehaviour
{
    [SerializeField] private CriAtomCueReference _bgmCue;

    private CriAtomExPlayer _player;

    void Start()
    {
        _player = new CriAtomExPlayer();
    }

    public void PlayBGM()
    {
        // AcbAsset.Handle で CriAtomExAcb を取得
        _player.SetCue(_bgmCue.AcbAsset.Handle, _bgmCue.CueId);
        _player.Start();
    }
}
```

---

## CriAtomAcbAsset

ACB ファイルを Unity アセット化したもの。

### 主要プロパティ

```csharp
public class CriAtomAcbAsset : ScriptableObject
{
    public CriAtomExAcb Handle { get; }   // ランタイムハンドル
    public bool Loaded { get; }            // ロード完了フラグ
}
```

### 動的ロード（必要な場合）

通常は Inspector 参照で自動ロードされるが、明示的に呼ぶ場合:

```csharp
CriAtomAssetsLoader.AddCueSheet(_acbAsset);

// 解放
CriAtomAssetsLoader.ReleaseCueSheet(_acbAsset.name);
```

⚠️ **注意**: `AddCueSheet()` を呼ぶ前に `LoadAsync()` を呼ぶと二重ロードになる。`AddCueSheet()` が内部で `LoadAsync()` を呼ぶ。

---

## CriAtomCueReference の構造体

```csharp
[Serializable]
public struct CriAtomCueReference
{
    public CriAtomAcbAsset AcbAsset;  // 参照する ACB
    public int CueId;                  // ACB 内の CueId（インデックス）
}
```

`CueId` は ACB 内で一意。CueName は実行時に取得可能:

```csharp
CriAtomEx.CueInfo info;
_bgmCue.AcbAsset.Handle.GetCueInfoByIndex(_bgmCue.CueId, out info);
string cueName = info.name;
```

---

## CriAtomSourceForAsset（簡易用途）

スクリプトを書きたくない場合の MonoBehaviour 版:

```csharp
// Inspector で Cue Reference を割り当て、ボタン onClick で Play() 呼び出し
public class SimpleSEPlayer : MonoBehaviour
{
    [SerializeField] private CriAtomSourceForAsset _source;

    public void OnButtonClick() => _source.Play();
}
```

`Source.Cue = new CriAtomCueReference(acbAsset, 0);` のようにコードからも設定可能。

---

## EDMQuiz での採用方針

| 項目 | 採用 |
|------|------|
| デプロイ | **OnMemory**（必須） |
| ACB ファイル | `Assets/SoundData/BGM.acb`, `SE.acb` |
| ACF ファイル | `Assets/SoundData/Project.acf` |
| キュー参照 | **CriAtomCueReference** を AudioManager にシリアライズ |
| 再生プレイヤー | **CriAtomExPlayer** を AudioManager 内で1つずつ（BGM 用 + SE 用） |
| シーン跨ぎ | AudioManager に `DontDestroyOnLoad` |

---

## Edge Cases

| ケース | 対応 |
|--------|------|
| ACB の Deploy Type が StreamingAssets | unityroom で動作不可 → 必ず OnMemory に変更 |
| AcbAsset.Handle が null | `Loaded == true` を待ってから使用 |
| 同じ ACB を複数 AddCueSheet | 内部で重複チェック済み |
| Inspector で CueReference がリセット | ACB の CueId は変化することがあるため、ACB 更新後に再選択 |

---

## 関連 Skill

- 概要: `cri-adx-overview`
- 再生制御: `cri-adx-playback`
- BPM 同期: `cri-adx-bpm-sync`
- WebGL: `cri-adx-webgl`
- EDMQuiz 統合: `edm-quiz-audio`
