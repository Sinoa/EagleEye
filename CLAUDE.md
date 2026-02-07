# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

EagleEyeは、麻雀の完全なゲームアクション（打牌・鳴き・リーチ・和了・槓）を行えるAI推論エンジンを中核とした.NETプロジェクト。天鳳牌譜の解析、試合状態の再現、機械学習モデルの構築・ONNX形式へのエクスポートを行うモジュラー設計のライブラリ群で構成される。数百体のキャラクターをLoRAで個性化し、Unity6上で動作させることを最終目標とする。

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

### 今後追加予定のプロジェクト

- `EagleEye.Core` - 共通定義・ビルディングブロック（TransformerBlock, RoPE, MeldEncoder等）
- `EagleEye.DataPipeline` - 前処理パイプライン（GameState → JSONL変換）
- `EagleEye.FourPlayer` - 4人麻雀固有モデル
- `EagleEye.ThreePlayer` - 3人麻雀固有モデル
- `EagleEye.Trainer` - TorchSharp学習パイプライン

## アーキテクチャ

データフロー: `.mjlog` → MjlogReader（パース） → MjlogReplayer（状態再現） → DataPipeline（前処理・JSONL出力） → Trainer（学習） → MLModelCodec（ONNXエクスポート）

詳細な設計決定事項は `docs/ARCHITECTURE.md` を参照。

### 名前空間規約

すべてのプロジェクトは `Foxtamp.` プレフィックスのルート名前空間を使用:
- `Foxtamp.MjlogReader`
- `Foxtamp.MjlogReplayer`
- `Foxtamp.MLCoreModule`
- `Foxtamp.MLModelCodec`

### 主要な依存関係

- **TorchSharp-cpu 0.105.2** - MLCoreModule, MLModelCodecで使用するテンソル演算フレームワーク
- **Google.Protobuf 3.21.9** - TorchSharpの推移的依存、MLModelCodecのONNXプロトコルバッファ（Generated/Onnx.csは自動生成コード）で使用
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
- Unity ONNX互換性を常に意識（カスタム演算子は避ける）
- 設定値はJSON設定ファイルで外部化（ハードコードしない）

## 主要コンポーネントの設計ポイント

### MjlogReader
- エントリポイント: `MjlogDocumentReader`（同期/非同期のLoad、XMLのParse）
- アクションは `Models/Actions/` 配下に型別に定義（DrawAction, DiscardAction, MeldAction, ReachAction等）
- 結果は `Models/Results/` 配下（Agari, Ryuukyoku）
- `Tile` → Suit, Number, IsRedDora, OriginalId(0-135), TileTypeId(0-33)
- `MeldInfo` → Type, Tiles, CalledTile, FromPlayer(相対位置), OriginalCode, TurnNumber(int?, 副露巡目)
- `MeldType`: Chi, Pon, DaiMinKan, KaKan, AnKan, Nuki

### MjlogReplayer
- `DocumentReplayer` がインデックスベースで任意時点のGameStateにアクセス
- `GameStateBuilder` で外部からの状態構築が可能
- GameStateはイミュータブルなスナップショット
- 三麻（`ThreePlayerGameState`）・四麻（`FourPlayerGameState`）の両方に対応
- `GameState` → RoundWind, RoundNumber, Honba, Kyotaku, DealerId, TurnNumber, StepIndex, Players, DoraIndicators, RemainingTileCount, SourceStep
- `PlayerState` → PlayerId, Hand, Discards, Melds, IsReach, ReachTurnNumber(int?), Score
- `DiscardedTile` → Tile, IsTsumogiri, IsReachDeclare, CalledByPlayerId
- 巡目計算: 全プレイヤー打牌完了→次巡、鳴き発生時にカウントリセット

### MLCoreModule
- `AttentionBase` を基底としたSelfAttention/CrossAttention
- `ScaledDotProductAttention` が実際のアテンション計算を担当
- 位置エンコーディングはRoPEとALiBiの2種類、KVキャッシュ用のオフセットサポートあり

### MLModelCodec
- `MLAbstractModel/Graph/Node/Tensor/ValueInfo/Attribute` による抽象モデル表現レイヤー
- `MLModelEncoder` がTorchSharpモデルをONNX ModelProtoに変換
- `Generated/Onnx.cs` は `onnx.proto3` からの自動生成コード（手動編集しないこと）

## 開発フェーズ

```
Phase 0: データパイプライン構築          ← 現在ここ（パーサー・場況復元まで完了）
Phase 1: 最小構成ベースモデル（手牌Attention + 基本スカラー特徴、打牌出力のみ）
Phase 2: 捨て牌追加（4プレイヤー分のAttention + RoPE）
Phase 3: 全アクション対応（鳴き・リーチ・和了・槓の出力追加）
Phase 4: LoRA効果検証（極端なキャラで単層LoRA検証）
Phase 5: パイプライン構築（キャラ量産の自動化）
Phase 6: 大規模運用（数百体のキャラ管理）
Phase 7: 3人麻雀対応（オプション）
Phase 8: 強化学習（オプション）
```

### Phase 0 残タスク

- [ ] GameState → GameStateSnapshot 変換ロジック
- [ ] インスタンスID付与（手牌ソート + 同一牌カウント）
- [ ] 有効アクションマスク生成（ルールベース合法手判定）
- [ ] JSONL出力（シリアライズ + ファイル分割）
- [ ] 視点変換（絶対位置 → 相対位置）
- [ ] データリーク防止テスト
- [ ] train/valid/test 分割

### 既存実装への確認事項（Replayer拡張の可能性）

1. ~~**リーチ巡目**~~: `PlayerState.ReachTurnNumber` (int?) で取得可能 ✅
2. ~~**残り山牌数**~~: `GameState.RemainingTileCount` (int) で取得可能 ✅
3. ~~**副露の巡目**~~: `MeldInfo.TurnNumber` (int?) で取得可能 ✅
4. **副露の順序**: Melds リストが時系列順に格納されているか（要確認）

## ML設計原則

### 前処理と学習の分離

```
牌譜（mjlog）→ MjlogReader → MjlogReplayer → GameState
    → EagleEye.DataPipeline → JSONL（生値で保存）
        → 学習時: 牌ID→埋め込み, 点数→正規化, カテゴリ→ワンホット, テンソル化
```

- 牌IDは整数のまま保存（埋め込みベクトルへの変換は学習時）
- 点数は生値で保存（正規化パラメータの実験的調整を可能に）
- 前処理は1回だけ実行、学習はエポックごと

### データリーク防止（厳守）

特徴量に含めてよいのは **観測可能な情報のみ**:
- ✅ 自分の手牌、全員の捨て牌、全員の副露、全員の点数、リーチ状態、山牌残り枚数
- ❌ 他家の手牌、山牌の中身、王牌の内容、裏ドラ

### 4人麻雀/3人麻雀

- モデル重みは完全に別
- 牌埋め込みも別々で事前学習（4麻=34種、3麻=27種）
- コードベース（TransformerBlock, RoPE等）は共通化
- ファクトリパターンで切り替え
