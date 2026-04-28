---
name: cri-adx-webgl
description: CRI ADX を WebGL ビルド (unityroom 含む) で動作させるための設定・制約・落とし穴。OnMemory ビルド必須、自動再生ポリシー、シングルスレッド対応、HCA コーデックの推奨を扱う。
---

# CRI ADX WebGL 対応

## 前提

CRI ADX LE は **WebGL ビルドに公式対応**している。ただし以下の制約がある:

| 制約 | 対応 |
|------|------|
| StreamingAssets が制限される（unityroom） | **OnMemory ビルドにする** |
| ブラウザの自動再生ポリシー | ユーザー操作後に再生開始 |
| シングルスレッド実行 | DSP バスエフェクトを最小限に |
| ファイルサイズ制約（unityroom） | HCA 圧縮で容量削減 |

---

## OnMemory ビルド設定（必須）

### 1. CriAtomAcbAsset の設定

各 ACB アセットを Inspector で:

```
Deploy Type: OnMemory  ← 必須（StreamingAssets ではない）
Acf Asset: Project.acf
```

### 2. ファイル配置

```
Assets/SoundData/        ← StreamingAssets を使わない
├─ Project.acf
├─ BGM.acb
└─ SE.acb
```

OnMemory にすると ACB データが Unity ビルドに埋め込まれる（ResourcesAssetBundle 不要）。

### 3. ビルド設定

```
Player Settings:
  WebGL:
    - Compression Format: Brotli or Gzip
    - Memory Size: 256〜512 MB
    - Run In Background: false
    - Decompression Fallback: true
```

---

## 自動再生ポリシー対策

ブラウザは **ユーザー操作前の音声再生をブロック** する。

### ❌ NG（Awake / Start で BGM 再生）

```csharp
void Start()
{
    AudioManager.Instance.PlayBGM();  // ブロックされる
}
```

### ✅ OK（ボタン押下で再生開始）

```csharp
public void OnStartButtonClicked()
{
    AudioManager.Instance.PlayBGM();   // ユーザー操作 → 再生可能
    SceneManager.LoadScene("GameScene");
}
```

### 推奨フロー

```
1. TitleScene 表示（無音 or AudioSource の通知音は OK）
2. プレイヤーが「Start」ボタンをクリック / タップ
3. OnClick ハンドラ内で PlayBGM() を呼ぶ
4. GameScene にロード（DontDestroyOnLoad で BGM 維持）
```

---

## シングルスレッド最適化

WebGL は単一スレッド実行。CRI ADX は内部でオーディオスレッドを使えないため CPU 負荷に注意。

### DSP バスエフェクト

ACF 側の DSP バス設定で:
- ❌ 多重リバーブ・複雑な EQ を避ける
- ✅ ボリューム調整・カテゴリ分けは OK

### HCA コーデック推奨

ACB 作成時のコーデック選択:
- **HCA**（推奨）: 高圧縮、低 CPU
- **HCA-MX**: 効果音向け、複数同時再生に最適
- **ADX**: 旧コーデック、互換性
- **PCM**: 圧縮なし、CPU 負荷ゼロだがファイル巨大

---

## unityroom 公開時の追加注意

### 容量制限

unityroom は WebGL ビルド全体で 50 MB 程度が推奨。BGM 1曲 + SE 数個なら HCA で十分収まる。

### ロード時間

OnMemory ビルドは初回ロードに時間がかかる。初期化完了通知を出す:

```csharp
public class AudioInitializer : MonoBehaviour
{
    [SerializeField] private CriAtomAcbAsset _bgmAcb;

    async void Start()
    {
        while (!_bgmAcb.Loaded)
            await UniTask.Yield();

        // ロード完了 → スタートボタン表示
        _startButton.SetEnabled(true);
    }
}
```

### 必須コンポーネント

シーンに以下のコンポーネントを配置（CRI ADX 初期化用）:
- `CriWareInitializer`（または `CriAtomServer`）
- `CriWareErrorHandler`
- `CriAtomAssets`（Asset Support Addon の場合）

**Title または Game シーンのいずれかに配置**。`DontDestroyOnLoad` で常駐させるのが確実。

---

## デバッグ Tips

### Console エラー確認

WebGL ビルド後、ブラウザ開発者ツールの Console で:

```
[CRIWARE] Initialize succeeded
[CRIWARE] ACB Loaded: BGM
```

が出れば OK。`AcfFile not found` などのエラーが出た場合:

1. ACF アセットのパスが正しいか
2. CriAtomAssets コンポーネントの `acfAsset` が割当てられているか

### ローカル WebGL テスト

`Build And Run` でブラウザ起動するか、ローカルサーバー（`python -m http.server`）で配信。`file://` プロトコルでは音声がブロックされるため必ず HTTP で確認。

---

## EDMQuiz の WebGL チェックリスト

- [ ] 全 ACB の Deploy Type が **OnMemory**
- [ ] `Assets/SoundData/` 配置（`StreamingAssets/` 不使用）
- [ ] HCA コーデックで圧縮
- [ ] BGM 再生は Start ボタン押下後
- [ ] `CriWareInitializer` + `CriWareErrorHandler` + `CriAtomAssets` 配置
- [ ] 初回ロード待ち UI 実装
- [ ] DSP バスエフェクトを最小化
- [ ] ビルド後ブラウザ Console で初期化エラー無し確認
- [ ] unityroom テストアップロードで動作確認

---

## 関連 Skill

- 概要: `cri-adx-overview`
- アセット: `cri-adx-asset-support`
- 再生: `cri-adx-playback`
- BPM 同期: `cri-adx-bpm-sync`
- EDMQuiz 統合: `edm-quiz-audio`
