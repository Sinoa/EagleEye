# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

EagleEyeは、麻雀「何切る」問題を解くAI推論エンジンを中核とした.NETプロジェクト。天鳳牌譜の解析、試合状態の再現、機械学習モデルの構築・ONNX形式へのエクスポートを行うモジュラー設計のライブラリ群で構成される。現在大規模リファクタリング中。

## ビルド・実行コマンド

```bash
# ソリューション全体をビルド
dotnet build

# リリースビルド
dotnet build -c Release

# 個別プロジェクトのビルド
dotnet build src/MjlogReader/MjlogReader.csproj

# サンプルアプリの実行
dotnet run --project src/MjlogReaderSample -- path/to/file.mjlog
dotnet run --project src/MjlogReplayerSample -- path/to/file.mjlog -s 0 --step 15
```

テストプロジェクトは現在未整備。

## 必要環境

- .NET 10.0 SDK

## ソリューション構成

ソリューションファイルは `EagleEye.slnx`（モダンXML形式）。プロジェクトは4つのフォルダに分類される:

- **Lib/** - コアライブラリ
  - `MjlogReader` - 天鳳牌譜パーサー（mjlog/XML、GZip自動判定）
  - `MjlogReplayer` - 牌譜から任意時点の試合状態をイミュータブルに再現
- **ML/** - 機械学習ライブラリ
  - `MLCoreModule` - アテンション機構（Self/Cross）・位置エンコーディング（RoPE/ALiBi）
  - `MLModelCodec` - TorchSharpモデル→ONNX形式へのエンコード
- **Sample/** - サンプルアプリケーション（MjlogReaderSample, MjlogReplayerSample）
- **Exp/** - 実験用アプリケーション（MLOnnxExportExp, MLOnnxRuntimeExp）

## アーキテクチャ

データフロー: `.mjlog` → MjlogReader（パース） → MjlogReplayer（状態再現） → MLCoreModule（ML処理） → MLModelCodec（ONNXエクスポート）

### 名前空間規約

すべてのプロジェクトは `Foxtamp.` プレフィックスのルート名前空間を使用:
- `Foxtamp.MjlogReader`
- `Foxtamp.MjlogReplayer`
- `Foxtamp.MLCoreModule`
- `Foxtamp.MLModelCodec`

### 主要な依存関係

- **TorchSharp-cpu 0.105.2** - MLCoreModule, MLModelCodecで使用するテンソル演算フレームワーク
- **Google.Protobuf** - MLModelCodecのONNXプロトコルバッファ（Generated/Onnx.csは自動生成コード）
- **System.CommandLine 2.0.0-beta4** - サンプルアプリのCLI引数パース
- **Microsoft.ML.OnnxRuntime 1.23.2** - 実験用ONNX推論ランタイム

### コミット規約

コミットメッセージは [Conventional Commits](https://www.conventionalcommits.org/) に従うこと。本文は日本語で記述する。

形式: `<type>: <description>`

主なtype:
- `feat` - 新機能の追加・既存機能の拡張
- `fix` - バグ修正
- `refactor` - リファクタリング（機能変更なし）
- `docs` - ドキュメントのみの変更
- `chore` - ビルド設定・補助ツール等の変更

例: `feat: MLAbstractAttributeにタプルからの暗黙的変換演算子を追加し、使いやすさを向上`

### コード規約

- ターゲットフレームワーク: `net10.0`
- Nullable参照型: 有効
- 暗黙的using: 有効
- ReSharperのコードスタイル設定あり（`EagleEye.sln.DotSettings`）

## 主要コンポーネントの設計ポイント

### MjlogReader
- エントリポイント: `MjlogDocumentReader`（同期/非同期のLoad、XMLのParse）
- アクションは `Models/Actions/` 配下に型別に定義（DrawAction, DiscardAction, MeldAction, ReachAction等）
- 結果は `Models/Results/` 配下（Agari, Ryuukyoku）

### MjlogReplayer
- `DocumentReplayer` がインデックスベースで任意時点のGameStateにアクセス
- `GameStateBuilder` で外部からの状態構築が可能
- GameStateはイミュータブルなスナップショット
- 三麻・四麻の両方に対応

### MLCoreModule
- `AttentionBase` を基底としたSelfAttention/CrossAttention
- `ScaledDotProductAttention` が実際のアテンション計算を担当
- 位置エンコーディングはRoPEとALiBiの2種類、KVキャッシュ用のオフセットサポートあり

### MLModelCodec
- `MLAbstractModel/Graph/Node/Tensor/ValueInfo/Attribute` による抽象モデル表現レイヤー
- `MLModelEncoder` がTorchSharpモデルをONNX ModelProtoに変換
- `Generated/Onnx.cs` は `onnx.proto3` からの自動生成コード（手動編集しないこと）
