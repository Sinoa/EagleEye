| ロゴ | マスコット |
| :---: | :---: |
| ![](./docs/logo.png) | ![](./docs/chara.png) |

# EagleEye

麻雀「何切る」問題を解くAI推論エンジン **EagleEye** を中核とした、学習パイプライン・ユーティリティを統合した総合プロジェクトです。

## 概要

**EagleEye** は、麻雀における「何切る」問題（手牌から最適な打牌を選択する問題）を解決するためのAI推論エンジンおよびそのアーキテクチャ名です。本プロジェクトでは、AIモデルの学習・推論・配布に必要なすべてのツールチェーンを統合し、汎用的なライブラリとして一般提供することを目指しています。

本リポジトリには、EagleEyeエンジン本体に加えて、天鳳牌譜の読み込み・変換、麻雀牌の埋め込みベクトル生成、AIモデルの入出力など、麻雀AIの学習・開発に必要な複数のライブラリとツールが含まれています。

### 誰のためのプロジェクト？

- 🎮 **ゲーム開発者** - Unity Sentis 対応により、麻雀ゲームへのAI組み込みが容易に
- 🔬 **研究者・開発者** - ONNX/Safetensors 対応により、独自の学習・解析が可能
- 🀄 **麻雀プレイヤー** - AIによる打牌選択の参考に

## 特徴

- 🧠 **何切る特化AI** - 麻雀の「何切る」問題に特化した推論エンジン「EagleEye」を開発
- 🀄 **天鳳牌譜対応** - 天鳳の牌譜ファイル（mjlog/XML）を読み込み、学習データとして活用
- 🎯 **牌エンベディング** - Skip-gramアーキテクチャによる麻雀牌の埋め込みベクトル生成
- 🤖 **複数フォーマット対応** - ONNX、Safetensors、Unity Sentis など主要なAIモデルフォーマットに対応
- 🎮 **ゲーム組み込み対応** - Unity Sentis 出力により、ゲームエンジンへの組み込みを想定した設計
- 📦 **モジュラー設計** - 各機能を独立したライブラリ/ツールとして提供し、必要な部分のみ利用可能
- 🔧 **学習パイプライン内蔵** - 牌譜からの特徴抽出、エンベディング生成、モデル訓練まで一貫して対応

---

## モデル配布について

訓練済みの EagleEye モデルは、将来的に何らかの形で配布を予定しています。配布方法・ライセンス形態については現在検討中です。

> **Note**: モデル配布の開始時期・形式については、[ロードマップ](#ロードマップ)をご確認ください。

---

## プロジェクト構成

| プロジェクト | 種別 | 説明 | 状態 |
|-------------|------|------|:----:|
| [EagleEye](./EagleEye/) | ライブラリ | 何切るAI推論エンジン本体 | 🚧 開発中 |
| [MjlogJ](./MjlogJ/README.md) | ライブラリ | 天鳳牌譜パーサー（mjlog/XML対応） | ✅ 実装済 |
| [MjlogConverter](./MjlogConverter/README.md) | CLIツール | 牌譜→JSON一括変換ツール | ✅ 実装済 |
| [MjlogA](./MjlogA/README.md) | ライブラリ | 牌譜データ分析ライブラリ | ✅ 実装済 |
| [MjlogAnalyzer](./MjlogAnalyzer/README.md) | CLIツール | 牌譜データ分析ツール（グラフ出力対応） | ✅ 実装済 |
| [MLModelUtility](./MLModelUtility/README.md) | ライブラリ | AIモデル入出力（Safetensors/ONNX/Sentis） | ✅ 実装済 |
| [TileEmbedder](./TileEmbedder/README.md) | ライブラリ | 牌エンベディング生成（Skip-gram） | ✅ 実装済 |
| [TileEmbedderCli](./TileEmbedderCli/README.md) | CLIツール | 牌エンベディング生成ツール（PCA/UMAP可視化・プロット出力対応） | ✅ 実装済 |

### 各プロジェクトの役割

- **EagleEye**: 麻雀の「何切る」問題を解く推論エンジン本体（開発中）
- **MjlogJ**: 天鳳の牌譜ファイル（mjlog形式）を読み込み、構造化されたC#オブジェクトに変換
- **MjlogConverter**: MjlogJを使用して牌譜ファイルをJSON形式に一括変換するCLIツール
- **MjlogA**: 牌譜データから統計情報（点数分布・役出現頻度・ドラ出現頻度など）を収集・分析するライブラリ
- **MjlogAnalyzer**: MjlogAを使用して牌譜ファイルを分析し、CSVやグラフ画像として出力するCLIツール
- **MLModelUtility**: Safetensors、ONNX、Unity Sentis形式のAIモデルを読み書きするユーティリティライブラリ
- **TileEmbedder**: Skip-gramアーキテクチャで麻雀牌の埋め込みベクトルを生成するライブラリ
- **TileEmbedderCli**: TileEmbedderを使用してコマンドラインから牌エンベディングを生成し、可視化・プロット出力するツール

---

## 必要環境

- .NET 9.0 SDK

---

## クイックスタート

### ビルド

```bash
# ソリューション全体をビルド
dotnet build

# リリースビルド
dotnet build -c Release
```

### 牌譜の読み込み（MjlogJ）

```csharp
using MjlogJ;
using MjlogJ.Formatters;

// 牌譜ファイルを読み込み
GameRecord record = MjlogReader.Load("path/to/file.mjlog");

// JSON形式で出力
var formatter = new JsonOutputFormatter();
string json = formatter.FormatToString(record);
```

### 牌譜の一括変換（MjlogConverter）

```bash
# ディレクトリ内のすべての牌譜をJSONに変換
MjlogConverter -i logs/ -o converted/ -p
```

### 牌エンベディング生成（TileEmbedderCli）

```bash
# ルールベースの共起関係から牌エンベディングを生成
TileEmbedderCli -p

# 牌譜データを使った追加学習
TileEmbedderCli -i ./mjlogs/ -o embeddings -p

# PCAで可視化してCSV出力
TileEmbedderCli -p --visualize > embeddings.csv

# 分布図をPNG画像として出力
TileEmbedderCli -l tile_embeddings.json --plot distribution.png
```

### 牌譜データ分析（MjlogAnalyzer）

```bash
# 牌譜ディレクトリを分析してCSV出力
MjlogAnalyzer -d ./mjlogs/ -o result.csv -p

# サブディレクトリも含めてグラフ出力
MjlogAnalyzer -d ./mjlogs/ -r --plot ./plots -p
```

### AIモデルの読み込み（MLModelUtility）

```csharp
using MLModelUtility.Formats.Safetensors;

var handler = new SafetensorsFormatHandler();
using TensorCollection tensors = handler.ReadTensorsFromFile("model.safetensors");

foreach (var tensor in tensors)
{
    Console.WriteLine($"{tensor.Info.Name}: {tensor.Info.ShapeToString()}");
}
```

---

## 対応フォーマット

### AIモデルフォーマット（MLModelUtility）

| フォーマット | テンソル読込 | テンソル書込 | グラフ読込 | グラフ書込 |
|------------|:----------:|:----------:|:--------:|:--------:|
| Safetensors | ✅ | ✅ | - | - |
| ONNX | ✅ | - | ✅ | ✅ |
| Unity Sentis | - | ✅ | - | ✅ |

### 牌譜フォーマット（MjlogJ）

| フォーマット | 読込 | 備考 |
|------------|:----:|------|
| mjlog (GZip) | ✅ | 天鳳標準形式 |
| mjlog (XML) | ✅ | 非圧縮形式 |

### 牌エンベディングフォーマット（TileEmbedder）

| フォーマット | 読込 | 書込 | 備考 |
|------------|:----:|:----:|------|
| Safetensors | ✅ | ✅ | 推論・配布用（推奨） |
| JSON | ✅ | ✅ | デバッグ・可読性確認用 |
| バイナリ (.bin) | ✅ | ✅ | 学習の完全な再開用 |
| CSV | - | ✅ | 2次元可視化用（PCA/UMAP） |

---

## ロードマップ

### v1.0 初期リリースに向けて

- [ ] EagleEye 推論エンジンコア実装
- [ ] 訓練用データセット生成パイプライン
- [ ] 基本的な訓練済みモデルの公開

---

## コントリビューション

### 現在のコントリビューター

- **Sinoa** <sinoans@gmail.com> ([@Sinoa](https://github.com/Sinoa)) - プロジェクト作成者・メンテナー

### コントリビューション受付について

本プロジェクトは現在開発中であり、初期リリース後に以下のような貢献を歓迎し順次受け付ける予定です：

- 🔧 **AIモデルのチューニング・改善提案** - より精度の高い打牌選択のためのアーキテクチャ改善
- 📊 **学習用データセットの提供** - 天鳳プレイヤーの牌譜データ、アノテーション付きデータ等（将来的に何切るの単独データ入力も検討）
- 🐛 **バグ報告・修正** - Issue / Pull Request でお知らせください
- 📝 **ドキュメント改善** - 使用例の追加、翻訳等
- 💡 **機能要望** - 新しいアイデアや改善案をお聞かせください

興味のある方は Issue または Pull Request でお気軽にご連絡ください。

---

## 依存ライブラリ

本プロジェクトは以下のオープンソースライブラリを使用しています：

| ライブラリ | ライセンス | 用途 |
|----------|-----------|------|
| [TorchSharp](https://github.com/dotnet/TorchSharp) | BSD 3-Clause | 機械学習フレームワーク |
| [ScottPlot](https://github.com/ScottPlot/ScottPlot) | MIT | データ可視化・グラフ描画 |
| [ONNX](https://github.com/onnx/onnx) | Apache 2.0 | モデルフォーマット定義 |
| [Unity Sentis](https://docs.unity3d.com/Packages/com.unity.sentis@latest) | Unity Companion License | Unityモデルフォーマット定義 |

各ライブラリの詳細なライセンス条項については、[LICENSE.md](./LICENSE.md) をご確認ください。

---

## ライセンス

[Zlib License](./LICENSE.md)

```
Copyright (c) 2025 Sinoa

This software is provided 'as-is', without any express or implied warranty.
```
