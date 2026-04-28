---
name: cri-adx-overview
description: CRI ADX (CRIWARE Unity Plugin) の全体像・主要 API・関連 Skill のインデックス。CRI ADX でサウンドを実装するときの最初のエントリポイント。
---

# CRI ADX 概要 — Unity Plugin

公式マニュアル: https://game.criware.jp/manual/unity_plugin/latest/contents/index.html

## CRI ADX とは

CRIWARE が提供する音声ミドルウェア。Unity プラグインとして利用可能で、AudioSource より高機能・高効率な音声再生・制御ができる。BGM 同期型ゲーム（リズムゲーム）・WebGL 公開ゲームで特に有利。

### 主な利点

| 項目 | AudioSource | CRI ADX |
|------|-------------|---------|
| 圧縮率 | mp3/ogg | HCA / HCA-MX（高圧縮） |
| 再生時刻精度 | dspTime（フレーム依存） | `GetTimeSyncedWithAudio()` でサンプル精度 |
| カテゴリ管理 | AudioMixer | CriAtomExCategory |
| ファイル管理 | 個別 AudioClip | キューシート（ACB）でまとめて管理 |
| ストリーミング | 自動 | OnMemory / Streaming を選択 |
| WebGL ストリーム | 制約あり | OnMemory ビルドで対応 |

---

## 主要コンポーネント・クラス

| 名前 | 役割 |
|------|------|
| **ACF ファイル** | 全体設定（カテゴリ・DSPバス）。1プロジェクトに1つ |
| **ACB ファイル** | キューシート（複数のキューをまとめたバイナリ） |
| **AWB ファイル** | ストリーミング用波形データ（OnMemory 不使用） |
| `CriAtomEx.Initialize()` | ライブラリ初期化 |
| `CriAtomExAcb` | ACB のランタイムハンドル |
| `CriAtomExPlayer` | 再生プレイヤー（複数同時再生に対応） |
| `CriAtomExPlayback` | 1つの再生インスタンス |
| `CriAtomSource` | MonoBehaviour ラッパー（簡易用途） |
| `CriAtomSourceForAsset` | Asset Support Addon 版 CriAtomSource |
| `CriAtomCueReference` | Inspector で参照可能なキュー指定（ACB + CueId） |
| `CriAtomAcbAsset` | Unity アセット化された ACB |
| `CriAtomAcfAsset` | Unity アセット化された ACF |
| `CriAtomAssetsLoader` | キューシート管理（AddCueSheet / ReleaseCueSheet） |

---

## 使い方の3レイヤー（選択肢）

### ① CriAtomSource（旧来方式）
- MonoBehaviour ベース、Inspector で CueSheet/CueName 文字列指定
- 文字列タイプミスのリスクあり
- 簡単だが拡張性に欠ける

### ② CriAtomSourceForAsset（Addon 提供）
- CriAtomCueReference を Inspector で選択（タイプセーフ）
- 1コンポーネント = 1キュー
- UI ボタン用 SE などの「貼り付け型」に向く

### ③ CriAtomExPlayer 直接使用（推奨：本プロジェクト）
- スクリプトから動的に再生制御
- 1つの Player で複数キューを切り替え可能
- BGM 1つ + SE 1つ の構成で `AudioManager` シングルトン化

EDMQuiz では **③ CriAtomExPlayer + CriAtomCueReference** を採用。

---

## 関連 Skill

| Skill | 内容 |
|-------|------|
| `cri-adx-asset-support` | Asset Support Addon・CriAtomCueReference・OnMemory ビルド |
| `cri-adx-playback` | CriAtomExPlayer による再生・停止・音量制御 |
| `cri-adx-bpm-sync` | `GetTimeSyncedWithAudio()` を使った BPM 同期 |
| `cri-adx-webgl` | WebGL ビルドの制約・unityroom 対応 |
| `edm-quiz-audio` | EDMQuiz 固有の AudioManager 実装方針（CRI ADX 統合） |

---

## 公式リソース

- [CRI ADX Unity Plugin マニュアル](https://game.criware.jp/manual/unity_plugin/latest/contents/index.html)
- [CRI ADX(Unity) チュートリアル](https://game.criware.jp/learn/tutorial/unity/)
- [CRI Asset Support Add-on 紹介記事](https://qiita.com/kowato/items/d3d0d2902a229ea40e0b)
- [unityroom 向け実装](https://qiita.com/kowato/items/69aab85934f98fabf98f)
