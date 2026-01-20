| ロゴ | マスコット |
| :---: | :---: |
| ![](./docs/logo.png) | ![](./docs/chara.png) |

# EagleEye

麻雀「何切る」問題を解くAI推論エンジン **EagleEye** を中核とした、学習パイプライン・ユーティリティを統合した総合プロジェクトです。

## 概要

**EagleEye** は、麻雀における「何切る」問題（手牌から最適な打牌を選択する問題）を解決するためのAI推論エンジンおよびそのアーキテクチャ名です。本プロジェクトでは、AIモデルの学習・推論・配布に必要なすべてのツールチェーンを統合し、汎用的なライブラリとして一般提供することを目指しています。

> **⚠️ 注意**: 本プロジェクトは現在大規模なリファクタリング中です。一部のコンポーネントは再設計・再実装中のため、利用できない場合があります。

### 誰のためのプロジェクト？

- 🎮 **ゲーム開発者** - 麻雀ゲームへのAI組み込みが容易に
- 🔬 **研究者・開発者** - 天鳳牌譜の解析・独自の学習・解析が可能
- 🀄 **麻雀プレイヤー** - AIによる打牌選択の参考に

## 特徴

- 🧠 **何切る特化AI** - 麻雀の「何切る」問題に特化した推論エンジン「EagleEye」を開発（予定）
- 🀄 **天鳳牌譜対応** - 天鳳の牌譜ファイル（mjlog/XML）を読み込み、学習データとして活用
- 📦 **モジュラー設計** - 各機能を独立したライブラリとして提供し、必要な部分のみ利用可能

---

## モデル配布について

訓練済みの EagleEye モデルは、将来的に何らかの形で配布を予定しています。配布方法・ライセンス形態については現在検討中です。

> **Note**: モデル配布の開始時期・形式については、[ロードマップ](#ロードマップ)をご確認ください。

---

## プロジェクト構成

| プロジェクト | 種別 | 説明 | 状態 |
|-------------|------|------|:----:|
| [MjlogReader](./src/MjlogReader/README.md) | ライブラリ | 天鳳牌譜パーサー（mjlog/XML対応） | ✅ 実装済 |
| EagleEye | ライブラリ | 何切るAI推論エンジン本体 | 🚧 計画中 |

### 各プロジェクトの役割

- **MjlogReader**: 天鳳の牌譜ファイル（mjlog形式）を読み込み、構造化されたC#オブジェクトに変換するライブラリ

---

## 必要環境

- .NET 10.0 SDK

---

## クイックスタート

### ビルド

```bash
# ソリューション全体をビルド
dotnet build

# リリースビルド
dotnet build -c Release
```

### 牌譜の読み込み（MjlogReader）

```csharp
using Foxtamp.MjlogReader;

// ファイルから読み込み（GZip自動判定）
MjlogDocument document = MjlogDocumentReader.Load("path/to/file.mjlog");

// 非同期で読み込み
MjlogDocument document = await MjlogDocumentReader.LoadAsync("path/to/file.mjlog");

// XML文字列からパース
MjlogDocument document = MjlogDocumentReader.Parse(xmlString);
```

#### 基本的な使用例

```csharp
using Foxtamp.MjlogReader;
using Foxtamp.MjlogReader.Models;
using Foxtamp.MjlogReader.Models.Actions;

// 牌譜を読み込み
var document = MjlogDocumentReader.Load("game.mjlog");

// ヘッダー情報を取得
Console.WriteLine($"対局日時: {document.Header.PlayedAt}");
Console.WriteLine($"プレイヤー: {string.Join(", ", document.Header.PlayerNames)}");

// 各局（セッション）を処理
foreach (var session in document.Sessions)
{
    Console.WriteLine($"\n{session.RoundName} {session.Honba}本場");
    Console.WriteLine($"親: P{session.DealerId}");
    
    // 行動ステップを処理
    foreach (var step in session.Steps)
    {
        switch (step.Action)
        {
            case DrawAction draw:
                Console.WriteLine($"  P{step.PlayerId} ツモ: {draw.Tile}");
                break;
            case DiscardAction discard:
                Console.WriteLine($"  P{step.PlayerId} 打牌: {discard.Tile}");
                break;
            case MeldAction meld:
                Console.WriteLine($"  P{step.PlayerId} 鳴き: {meld.Meld}");
                break;
            case ReachAction reach:
                Console.WriteLine($"  P{step.PlayerId} リーチ");
                break;
        }
    }
}
```

詳細なAPIリファレンスは [MjlogReader README](./src/MjlogReader/README.md) を参照してください。

---

## 対応フォーマット

### 牌譜フォーマット（MjlogReader）

| フォーマット | 読込 | 備考 |
|------------|:----:|------|
| mjlog (GZip) | ✅ | 天鳳標準形式 |
| mjlog (XML) | ✅ | 非圧縮形式 |

---

## ロードマップ

### v1.0 初期リリースに向けて

- [x] MjlogReader ライブラリ実装
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

現在のプロジェクトは外部ライブラリへの依存はありません。将来的に以下のライブラリの使用を予定しています：

| ライブラリ | ライセンス | 用途 | 状態 |
|----------|-----------|------|:----:|
| [TorchSharp](https://github.com/dotnet/TorchSharp) | BSD 3-Clause | 機械学習フレームワーク | 📋 予定 |
| [ONNX](https://github.com/onnx/onnx) | Apache 2.0 | モデルフォーマット定義 | 📋 予定 |

各ライブラリの詳細なライセンス条項については、[LICENSE.md](./LICENSE.md) をご確認ください。

---

## ライセンス

[Zlib License](./LICENSE.md)

```
Copyright (c) 2025-2026 Sinoa

This software is provided 'as-is', without any express or implied warranty.
```
