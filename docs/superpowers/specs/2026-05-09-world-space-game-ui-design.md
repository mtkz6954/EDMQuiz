# RenderTexture Gameplay UI Design

**Status**: Draft
**Date**: 2026-05-09
**Target Scene**: `Assets/_EDMQuiz/Scenes/GameScene.unity`

## Goal

`GameScene` にある既存の `Light2D` を、現在の gameplay UI の見た目に反映させる。

ただし、`UI Toolkit` をそのまま 3D world space UI として扱うのではなく、`UI Toolkit -> RenderTexture -> Light2D を受ける world-space 表示板` の構成で実現する。

今回の対象は `ResultScreen` を除く主要 UI である。

- 問題文
- 問題番号
- 進行表示
- カウントダウン
- 回答欄
- ひらがなボタン
- 回答促し

`ResultScreen` は既存の overlay UI のまま維持する。

## Problem Statement

現状の `GameScene` では、`AnswerSpotlight` と `MovingSpotlightController` により `Light2D` は生成されているが、主要 UI 自体はその見え方に直接参加していない。

当初は gameplay UI を world space 化する案を検討したが、Unity の公式ドキュメント上、`UI Toolkit` はそのままの形では 3D world space の描画と入力に向いていない。  
そのため、`UI Toolkit` は引き続きランタイム UI の生成と入力処理に使い、可視表現だけを RenderTexture 経由で world space に出す構成へ切り替える。

## Scope

### In Scope

- `GameScene` の gameplay UI を `RenderTexture` 出力対応へ再構成
- gameplay UI を 2 系統の表示レイヤーに分割
- `PanelSettings` の `TargetTexture` を使った UI 描画
- RenderTexture を貼る world-space 表示板の追加
- `HiraganaInputUI` を複数 `UIDocument` / 複数出力先対応へ整理
- `VFXDirector` の UI 演出参照先整理
- 検証用 EditMode テスト追加
- `uloop` を使った compile / PlayMode / スクリーンショット確認

### Out of Scope

- `TitleScene` の UI 改修
- `ResultScreen` の world-space 化
- gameplay UI ロジックの全面書き換え
- `Light2D` 演出ロジック自体の大幅刷新
- uGUI への全面移行

## Decision

gameplay UI は `2 系統の UI Toolkit パネル` と `2 枚の world-space 表示板` に分割する。

### UI Toolkit Panel A

情報表示と可読性重視の UI を担当する。

- 問題文
- 問題番号
- 進行表示
- カウントダウン

### UI Toolkit Panel B

入力と演出重視の UI を担当する。

- 回答欄
- ひらがなボタン
- 回答促し

### World-Space Display A

`Panel A` の RenderTexture を受ける表示板。

### World-Space Display B

`Panel B` の RenderTexture を受ける表示板。
`Light2D` がもっとも分かりやすく見える前景側の見せ場にする。

### Overlay UI

overlay 側に残すもの:

- `ResultScreen`
- 正解 / 不正解ラベルなど、既存設計上 overlay に残した方が安定する演出

## Rationale

### なぜ UI Toolkit を直接 world space 化しないか

Unity の公式ドキュメント上、`UI Toolkit` は 3D world space の描画と入力をそのまま担う用途に向いていない。  
一方、`PanelSettings` の `TargetTexture` を使えば、UI Toolkit で描いた UI をテクスチャへ描画し、それを 3D ジオメトリへ表示できる。

このため、`UI Toolkit を維持しながら Light2D の視覚効果を見せる` 現実的な経路は RenderTexture 化である。

### なぜ 1 系統ではなく 2 系統か

問題文と HUD は可読性優先、回答欄とボタン群は演出優先で設計したい。  
同じ出力面にまとめると、ライトの見え方と文字可読性が衝突しやすい。

2 系統へ分けることで、次を両立しやすくする。

- Display A は見やすさ優先
- Display B は `Light2D` の見せ方優先

### なぜ ResultScreen を残すか

今回の主目的は「プレイ中の UI に `Light2D` を見せる」ことにある。  
結果画面まで同じ仕組みに巻き込むと変更範囲が広がり、リスクに対して効果が薄い。

## Architecture

## 1. Layout Assets

既存の `game-panel.uxml` / `game-panel.uss` を一枚岩のまま扱わず、役割別に分割する。

想定資産:

- `game-panel-display-a.uxml`
- `game-panel-display-a.uss`
- `game-panel-display-b.uxml`
- `game-panel-display-b.uss`

必要に応じて既存の `common.uss` から共有スタイルを引き継ぐ。

### Design Intent

- Display A は可読性優先
- Display B は発光・陰影の見え方優先
- 現行デザインの印象は保つが、表示板に貼ったときに破綻しない程度の余白・サイズ調整は許容する

## 2. Panel Settings and RenderTextures

gameplay UI 用に `PanelSettings` と `RenderTexture` を分ける。

### Assets

- `GameplayDisplayA.asset`
  - `PanelSettings`
  - `TargetTexture` に `RenderTexture A` を割り当てる

- `GameplayDisplayB.asset`
  - `PanelSettings`
  - `TargetTexture` に `RenderTexture B` を割り当てる

- `RenderTexture A`
  - 問題文 / HUD 用

- `RenderTexture B`
  - 回答欄 / ボタン群用

### Intent

- UI Toolkit はこれまで通り UXML/USS で UI を構築する
- 直接画面へ出すのではなく RenderTexture に描く
- 描かれたテクスチャを world-space の lit surface に貼る

## 3. Scene Composition

`GameScene` に gameplay 表示用オブジェクトを追加する。

### Scene Objects

- `GameUIRenderA`
  - `UIDocument`
  - `PanelSettings A`
  - UXML A を参照

- `GameUIRenderB`
  - `UIDocument`
  - `PanelSettings B`
  - UXML B を参照

- `GameplayDisplaySurfaceA`
  - `SpriteRenderer` または quad 相当の表示板
  - RenderTexture A を貼る
  - 可読性重視の配置

- `GameplayDisplaySurfaceB`
  - `SpriteRenderer` または quad 相当の表示板
  - RenderTexture B を貼る
  - `Light2D` を受ける前景配置

- `GameUIOverlay`
  - 既存 overlay 側 UI
  - `ResultScreen` などを保持

### Placement Principles

- Surface A は読みやすさ優先の位置とサイズに置く
- Surface B は `AnswerSpotlight` / `MovingSpotlightController` の演出がもっとも見える位置に置く
- Surface B は `Light2D` 受光が分かりやすいマテリアル / 色 / アルファにする
- カメラ距離、ソート順、受光の強さは `uloop` で PlayMode を見ながら調整する

## 4. Logic and Control

### HiraganaInputUI

`HiraganaInputUI` は引き続き gameplay UI の主制御を担当する。

残す責務:

- `GameFlowManager` のフェーズ購読
- 入力バッファ管理
- 正解送信
- ボタン活性 / 非活性制御
- カウントダウン表示更新

分離する点:

- 単一 `UIDocument` 前提をやめる
- Display A 用参照と Display B 用参照に分ける
- 問題文 / HUD 更新と、回答欄 / ボタン更新を別系統のメソッドへ分ける

### Input Model

入力は RenderTexture 側ではなく、`UIDocument` 側で処理する。

つまり:

- 見せる UI は world-space surface
- 実際に入力を受けるのは UI Toolkit のランタイムパネル

今回はマウス / キーボード入力の既存挙動を維持することを優先し、表示と入力を分ける。

### ResultScreen

`ResultScreen` は overlay 側に残し、RenderTexture 化対象から分離する。

### VFXDirector

`VFXDirector` は UI 演出の参照先を明確化する。

- overlay 側で行う演出
  - 結果系ラベル
  - 全画面フラッシュなど

- gameplay 側で行う演出
  - RenderTexture に反映したい panel 演出
  - Surface 表示と共存させたい UI アニメーション

## 5. Display Surface Strategy

world-space 表示板は、`Light2D` を見せるための受け皿である。

候補実装:

- `SpriteRenderer` に RenderTexture ベースのマテリアルを貼る
- `MeshRenderer` / quad に RenderTexture を貼る

選定基準:

- `Light2D` を受けられること
- 透過付き UI を破綻なく表示できること
- `GameScene` 既存の 2D 演出と整合すること

このプロジェクトは URP 2D ベースなので、まずは 2D 側の描画系に寄せる。

## Testing Strategy

## Automated Tests

EditMode テストを追加し、少なくとも次を守る。

- gameplay UI が display A / display B に分割されている
- RenderTexture 用資産が存在する
- `ResultScreen` が gameplay RenderTexture 側に混入していない
- Scene / スクリプト参照が壊れていない

候補:

- world-space 表示用 UXML / USS の存在確認
- RenderTexture / PanelSettings 参照の存在確認
- overlay 専用要素が gameplay 側 UXML に含まれないことの確認

## Manual Verification

実装後は以下を必須確認とする。

1. `uloop compile --force-recompile false --wait-for-domain-reload true`
2. PlayMode で `GameScene` を起動
3. BuildUp / AnswerWindow 中のスクリーンショット取得
4. `Light2D` が gameplay UI の見た目に反映されていることを確認
5. 問題文、回答欄、ボタンの可読性が崩れていないことを確認
6. 入力操作が従来どおり成立することを確認

## Risks

- RenderTexture を貼る受光面の実装方式によっては、透過や色が崩れる可能性
- 表示と入力を分けるため、見た目位置と入力位置のズレが出る可能性
- `HiraganaInputUI` が単一 `UIDocument` 前提のため、参照分離で見落としが出る可能性
- `VFXDirector` が gameplay UI と result UI をまたいで参照しているため、演出の依存整理が必要

## Rollout Plan

1. display A / display B 用 UXML / USS を作成
2. RenderTexture と PanelSettings を追加
3. `GameScene` に 2 つの `UIDocument` と 2 枚の表示板を追加
4. `HiraganaInputUI` を複数 panel 対応に分解
5. `VFXDirector` の UI 参照整理
6. overlay 側に `ResultScreen` を残す構成へ調整
7. EditMode テスト追加
8. `uloop` で compile と PlayMode スクリーンショット検証

## Open Questions

現時点でユーザー合意済みの前提:

- `UI Toolkit` は維持する
- gameplay UI は `RenderTexture + world-space 表示板`
- `ResultScreen` は現状維持
- 見た目は少しの調整を許容

実装段階で必要なら追加で詰める項目:

- Display Surface A / B の正確な配置座標
- RenderTexture の解像度
- 受光面のマテリアル構成
- 見た目と入力位置のズレ補正方法
