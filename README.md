| ロゴ | マスコット |
| :---: | :---: |
| ![](./docs/logo.png) | ![](./docs/chara.png) |

# EagleEye

麻雀「何切る」問題を解くAI推論エンジン **EagleEye** を中核とした、学習パイプライン・ユーティリティを統合した総合プロジェクトです。

## 特徴

- 🀄 **天鳳牌譜対応** - 天鳳の牌譜ファイル（mjlog/XML）を読み込み、学習データとして活用
- 🤖 **複数フォーマット対応** - ONNX、Safetensors、Unity Sentis など主要なAIモデルフォーマットに対応
- 🎮 **ゲーム組み込み対応** - Unity Sentis 出力により、ゲームエンジンへの組み込みを想定した設計
- 📦 **モジュラー設計** - 各機能を独立したライブラリ/ツールとして提供し、必要な部分のみ利用可能

---

## プロジェクト構成

| プロジェクト | 種別 | 説明 | 状態 |
|-------------|------|------|:----:|
| [EagleEye](./EagleEye/) | ライブラリ | 何切るAI推論エンジン | 🚧 開発中 |
| [MjlogJ](./MjlogJ/README.md) | ライブラリ | 天鳳牌譜パーサー | ✅ 実装済 |
| [MjlogConverter](./MjlogConverter/README.md) | CLIツール | 牌譜→JSON変換ツール | ✅ 実装済 |
| [MLModelUtility](./MLModelUtility/README.md) | ライブラリ | AIモデル入出力ユーティリティ | ✅ 実装済 |
| [TileEmbedder](./TileEmbedder/) | CLIツール | 牌エンベディング生成ツール | 🚧 開発中 |

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

---

## ロードマップ

- [ ] EagleEye 推論エンジン実装
- [ ] TileEmbedder 実装
- [ ] 訓練用データセット生成パイプライン
- [ ] 訓練済みモデルの公開

---

## コントリビューション

### 現在のコントリビューター

- **Sinoa** ([@Sinoa](https://github.com/Sinoa)) - プロジェクト作成者・メンテナー

### コントリビューション受付について

本プロジェクトは初期リリース後、以下のような貢献を歓迎し順次受け付けています：

- 🔧 **AIモデルのチューニング・改善提案** - より精度の高い打牌選択のためのアーキテクチャ改善
- 📊 **学習用データセットの提供** - 天鳳プレイヤーの牌譜データ、アノテーション付きデータ等（将来的に何切るの単独データ入力も検討）
- 🐛 **バグ報告・修正** - Issue / Pull Request でお知らせください
- 📝 **ドキュメント改善** - 使用例の追加、翻訳等

興味のある方は Issue または Pull Request でお気軽にご連絡ください。

---

## ライセンス

[Zlib License](./LICENSE.md)

```
Copyright (c) 2025 Sinoa

This software is provided 'as-is', without any express or implied warranty.
```

