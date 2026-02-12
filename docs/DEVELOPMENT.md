# EagleEye 開発計画・進行管理

本ドキュメントは、開発フェーズ、進行管理、技術スタック、プロジェクト構成、および未解決課題をまとめたものです。

設計決定事項は `ARCHITECTURE.md` を参照してください。
設計の詳細な議論・根拠は Claude.ai プロジェクトの `MODEL_DESIGN_NOTES.md` / `DATA_PIPELINE.md` に記載。

---

## 目次

- [1. 開発フェーズ概要](#1-開発フェーズ概要)
- [2. Phase 0 設計ブロッカー（すべて解消済み）](#2-phase-0-設計ブロッカーすべて解消済み)
- [3. Phase 0 残タスク](#3-phase-0-残タスク)
- [4. フェーズ詳細](#4-フェーズ詳細)
- [5. 技術スタック](#5-技術スタック)
- [6. プロジェクト構成](#6-プロジェクト構成)
- [7. 未解決課題一覧](#7-未解決課題一覧)
- [8. 解決済み課題](#8-解決済み課題)
- [9. 比較実験メモ](#9-比較実験メモ)

---

## 1. 開発フェーズ概要

```
Phase 0: データパイプライン構築          ← 現在ここ（設計確定、実装開始可能）
Phase 1: 最小構成ベースモデル（手牌Attention + 基本スカラー特徴、打牌出力のみ）
Phase 2: 捨て牌追加（4プレイヤー分のAttention + ALiBi）
Phase 3: 全アクション対応（鳴き・リーチ・和了・槓の出力追加）
Phase 4: LoRA効果検証（極端なキャラで単層LoRA検証）
Phase 5: パイプライン構築（キャラ量産の自動化）
Phase 6: 大規模運用（数百体のキャラ管理）
Phase 7: 3人麻雀対応（オプション）
Phase 8: 強化学習（オプション）
```

---

## 2. Phase 0 設計ブロッカー（すべて解消済み）

| 項目 | 決定内容 | 状況 |
|------|----------|------|
| 牌ID体系 | TileTypeId (0-33) 34種、赤ドラは別フラグ | ✅ |
| 入力ベクトル（手牌） | 牌埋め込み(4) + インスタンスID(2) + 赤フラグ(1) + ツモフラグ(1) + ドラフラグ(1) = 9次元 | ✅ |
| 入力ベクトル（捨て牌） | 牌埋め込み(4) + 赤ドラフラグ(1) + ドラフラグ(1) = 6次元 | ✅ |
| ドラ表現 | 牌ごとのドラフラグ方式。場況はドラ表示牌枚数スカラー1次元 | ✅ |
| リーチ状態表現 | 他家リーチ（既存）+ 自分リーチフラグ（場況に1次元追加） | ✅ |
| アクション種別enum | 12種のFlags enum（Discard〜Nuki）、GameActionType として実装 | ✅ |
| マスク表現 | データセットはenum値配列、学習時にマルチホット変換 | ✅ |
| チーパターン処理 | 粗粒度マスク（1ビット）+ 手牌ロジットTop2に委任 | ✅ |
| 副露順序保証 | Meldsリストの配列順 = 時系列順（確認済み） | ✅ |
| Phase 0スコープ | 4人麻雀・3人麻雀の両方 | ✅ |
| JSONL赤ドラ表現 | フラグ配列分離（hand_red_flags等） | ✅ |

---

## 3. Phase 0 残タスク

- [x] ドラフラグ付与（`DoraCalculator` 三麻対応済み）
- [x] 有効アクションマスク生成（`ValidActionGenerator` + `AgariChecker` / `TenpaiChecker` / `FuritenChecker`、北抜き含む）
- [ ] GameState → GameStateSnapshot 変換ロジック
- [ ] インスタンスID付与（手牌ソート + 同一牌カウント）
- [ ] 赤ドラフラグ付与（`Tile.IsRedDora` 既存プロパティで取得可能）
- [ ] JSONL出力（シリアライズ + ファイル分割）
- [ ] 視点変換（絶対位置 → 相対位置）
- [ ] データリーク防止テスト
- [ ] train/valid/test 分割

### 既存実装への確認事項（すべて確認済み）

1. ~~**リーチ巡目**~~: `PlayerState.ReachTurnNumber` (int?) で取得可能 ✅
2. ~~**残り山牌数**~~: `GameState.RemainingTileCount` (int) で取得可能 ✅
3. ~~**副露の巡目**~~: `MeldInfo.TurnNumber` (int?) で取得可能 ✅
4. ~~**副露の順序**~~: `PlayerState.Melds` リストが時系列順に格納されていることを確認済み ✅

---

## 4. フェーズ詳細

```
Phase 0: データパイプライン構築
├── mjlog → JSONL前処理ツール実装
├── 場況復元ロジックの実装・検証
├── データリーク防止の単体テスト
├── サンプルデータでの動作確認
└── 学習用データローダー実装

Phase 1: 最小構成（ベースモデル）
├── 手牌Attention + 基本スカラー特徴
├── 打牌出力のみ
└── ONNXエクスポート → Unity動作確認

Phase 2: 捨て牌追加
├── 4プレイヤー分のAttention + ALiBi
├── パディング・マスク処理
└── 精度評価

Phase 3: 全アクション対応
├── 鳴き・リーチ・和了・槓の出力追加
├── アクションマスク実装
└── 複合損失関数の調整

Phase 4: LoRA効果検証
├── 極端な2〜3キャラ（超攻撃/超守備）で単層LoRA検証
├── スタイル別牌譜フィルタリングの実装
├── スナップショット収集ツールの試作（オプション）
└── 効果確認後、階層構造に移行

Phase 5: パイプライン構築
├── 打ち筋分析の指標固定
├── スタイル分類の自動化
├── キャラ定義JSONからの自動学習
└── スナップショット収集・回答UI（必要に応じて）

Phase 6: 大規模運用
├── 数百体のキャラ管理
├── ベース更新時の再学習パイプライン
└── 品質チェックの自動化

Phase 7: 3人麻雀対応（オプション）
├── 3人麻雀用牌埋め込み事前学習
├── 3人麻雀用モデル構築
└── 北抜きアクション実装

Phase 8: 強化学習による改善（オプション）
├── 自己対戦環境の構築
├── Eloベース報酬システム
├── ベースモデルの強化
└── 評価・比較実験
```

---

## 5. 技術スタック

| 用途 | 技術 |
|------|------|
| 学習 | C# + TorchSharp |
| 推論（Unity） | Unity + ONNX Runtime |
| 推論（Web） | ONNX Runtime Web（OpSet 22、Chrome等） |
| データ前処理 | C#（mjlog解析ツール） |
| 中間データ形式 | JSONL（gzip圧縮オプション） |
| キャラ管理 | JSON定義 + LRUキャッシュ |
| 活性化関数 | SiLU（Unity: Swish演算子、Web: Sigmoid+Mulに分解） |

---

## 6. プロジェクト構成

```
EagleEye/
├── EagleEye.Core/                    # 共通定義・ビルディングブロック
│   ├── Interfaces/
│   │   ├── IMahjongModel.cs          # 共通インターフェース
│   │   ├── IActionDecoder.cs
│   │   └── ITileEmbeddingProvider.cs
│   ├── Common/
│   │   ├── TransformerBlock.cs       # 再利用可能なブロック
│   │   ├── ALiBi.cs
│   │   ├── MaskedAttention.cs
│   │   ├── LayerNorm.cs
│   │   └── MeldEncoder.cs
│   ├── Configs/
│   │   └── ModelConfig.cs            # 設定クラス
│   └── Enums/
│       └── MahjongVariant.cs
│
├── EagleEye.DataPipeline/            # データパイプライン
│   ├── MjlogParser.cs                # mjlog解析
│   ├── GameStateReconstructor.cs     # 場況復元
│   ├── SnapshotSerializer.cs         # JSONL出力
│   ├── FeatureExtractor.cs           # 視点変換・特徴抽出
│   ├── DataLoader.cs                 # 学習用データローダー
│   ├── PlayStyleClassifier.cs        # 打ち筋分類
│   └── QualityFilter.cs              # 結果ベースフィルタリング
│
├── EagleEye.ScenarioCollector/       # スナップショット収集
│   ├── CriticalPointExtractor.cs     # 判断ポイント抽出
│   ├── ScenarioGenerator.cs          # シナリオ生成
│   └── ResponseCollector.cs          # 回答収集
│
├── EagleEye.FourPlayer/              # 4人麻雀固有
│   ├── FourPlayerModel.cs
│   ├── FourPlayerActionSpace.cs
│   ├── FourPlayerInputBuilder.cs
│   └── Embeddings/
│       └── tiles_4p.bin              # 4人麻雀用牌埋め込み
│
├── EagleEye.ThreePlayer/             # 3人麻雀固有
│   ├── ThreePlayerModel.cs
│   ├── ThreePlayerActionSpace.cs
│   ├── ThreePlayerInputBuilder.cs
│   ├── KitaNukiHandler.cs            # 北抜き処理
│   └── Embeddings/
│       └── tiles_3p.bin              # 3人麻雀用牌埋め込み
│
├── EagleEye.Trainer/                 # TorchSharp学習パイプライン
│   ├── TrainerBase.cs
│   ├── FourPlayerTrainer.cs
│   ├── ThreePlayerTrainer.cs
│   └── LoraTrainer.cs                # LoRA学習用
│
├── EagleEye.Reinforcement/           # 強化学習（オプション）
│   ├── SelfPlayEnvironment.cs        # 自己対戦環境
│   ├── EloRatingSystem.cs            # Eloレーティング
│   ├── RewardShaping.cs              # 報酬シェイピング
│   └── ValueNetwork.cs               # 価値関数ネットワーク
│
├── EagleEye.Runtime/                 # Unity ONNX推論
│   ├── ModelFactory.cs               # ファクトリで切り替え
│   └── OnnxInference.cs
│
└── Data/
    ├── raw/                          # mjlogファイル
    ├── processed/                    # JSONL中間形式
    │   ├── train/
    │   ├── valid/
    │   ├── test/
    │   └── metadata.json
    ├── scenarios/                    # スナップショット収集用
    │   ├── critical_points/
    │   ├── custom/
    │   └── responses/
    ├── cache/                        # テンソルキャッシュ（オプション）
    └── embeddings/
        ├── tiles_4p.bin              # 4人麻雀用牌埋め込み
        └── tiles_3p.bin              # 3人麻雀用牌埋め込み
```

### モデルファイル構成

```
models/
├── bases/
│   ├── four_player/
│   │   ├── v1.0.0.onnx
│   │   └── config.json
│   └── three_player/
│       ├── v1.0.0.onnx
│       └── config.json
├── styles/
│   ├── four_player/
│   │   ├── aggressive.lora
│   │   └── defensive.lora
│   └── three_player/
│       ├── aggressive.lora
│       └── speed.lora
├── characters/
│   ├── four_player/
│   └── three_player/
└── manifests/
    └── characters.json
```

---

## 7. 未解決課題一覧

### 高優先度

| 課題 | 内容 | 状況 |
|------|------|------|
| ONNXエクスポート検証 | Unity + Web動作確認（SiLUの自動分解可否含む） | ⏳ 未着手 |
| ベースモデルの具体的アーキテクチャ確定 | 層数、次元数の決定 | ⏳ 未着手 |
| スタイル分類の指標と閾値設計 | 自動分類システム | ⏳ 未着手 |
| スナップショット収集ツール実装 | 判断ポイント抽出、回答収集UI | ⏳ 未着手 |

### 中優先度

| 課題 | 内容 | 状況 |
|------|------|------|
| 集約手法の性能比較 | Flatten vs CLS | ⏳ 未着手 |
| プレイヤー間関係 | Cross-Attentionの検討 | ⏳ 未着手 |
| 計算コスト検証 | 推論レイテンシ確認 | ⏳ 未着手 |
| サンプル不均衡対策 | 和了・リーチの低頻度問題 | ⏳ 未着手 |
| 前処理パイプライン実装 | mjlog → JSONL変換ツール | ⏳ 未着手 |
| 学習対象フィルタリング戦略 | 段位・結果によるサンプル選択 | ⏳ 未着手 |
| 打ち筋分類器実装 | 攻撃率・リーチ率等の指標計算 | ⏳ 未着手 |
| 結果ベースフィルタリング実装 | 良い判断の自動抽出 | ⏳ 未着手 |

### 低優先度

| 課題 | 内容 | 状況 |
|------|------|------|
| 3人麻雀用牌埋め込み | Skip-gram事前学習 | ⏳ 未着手 |
| ~~北抜きアクション実装~~ | ~~3人麻雀固有処理~~ | ✅ 実装済み（GameActionType.Nuki + ValidActionGenerator） |
| データ圧縮形式の検討 | gzip, MessagePack等 | ⏳ 未着手 |
| 強化学習パイプライン | 自己対戦 + Eloベース評価 | ⏳ 未着手 |
| 価値関数ネットワーク | 状態価値推定モデル | ⏳ 未着手 |

---

## 8. 解決済み課題

| 課題 | 解決内容 | 参照先 |
|------|----------|--------|
| 牌埋め込み方式 | Skip-gram事前学習を採用 | ARCHITECTURE §2 |
| 手牌入力方式 | ソート + インスタンスID | ARCHITECTURE §3 |
| 正規化手法 | LayerNormを採用 | ARCHITECTURE §8 |
| 活性化関数 | SiLU（Swish）を採用 | ARCHITECTURE §8 |
| Web ONNX Runtime活性化関数 | SiLU分解（Sigmoid+Mul）で対応、Mishは不採用 | ARCHITECTURE §8 |
| インスタンスID学習方式 | 学習可能Embedding + 牌埋め込み固定 | ARCHITECTURE §3 |
| ワンホット vs 埋め込み | 埋め込み採用（計算効率の観点） | ARCHITECTURE §2 |
| 4人麻雀/3人麻雀の設計方針 | 別モデル + 共通ライブラリ | ARCHITECTURE §12 |
| 前処理と学習の分離 | 前処理で場況復元、学習時に特徴変換 | ARCHITECTURE §10 |
| 複数プレイヤー情報の扱い | 全員分を1サンプルに含め、学習時に視点変換 | ARCHITECTURE §10 |
| データリーク防止方針 | 観測可能性チェックリスト + 明示的特徴量列挙 | ARCHITECTURE §10 |
| マルチホット方式の実現可能性 | Unity6環境で十分実用的（比較実験の価値あり） | ARCHITECTURE §2 |
| nn.Embeddingの構造理解 | ルックアップテーブル（線形変換ではない） | ARCHITECTURE §2 |
| Skip-gramとEmbeddingの関係 | 同一構造、異なる学習信号 | ARCHITECTURE §2 |
| 埋め込みのファインチューニング | 事前学習済み重み + requires_grad=true が推奨 | ARCHITECTURE §2 |
| 出力形式の最終決定 | 単一ヘッド + BCE | ARCHITECTURE §7 |
| 点数の正規化 | 区分線形変換 + ランク | ARCHITECTURE §6 |
| アクションマスク生成 | ゲームルールに基づく有効判定 | ARCHITECTURE §7 |
| データ収集パラダイムの選択 | 牌譜収集（ベース） + スナップショット（LoRA） | ARCHITECTURE §11 |
| RLHFの適用判断 | フル実装は過剰、直接ラベル付けで代替 | ARCHITECTURE §11 |
| 強化学習の報酬設計方針 | Eloベース + ポテンシャルベースシェイピング | ARCHITECTURE §11 |
| キャラクターLoRAの学習データ戦略 | スタイル別フィルタ + 直接ラベル付け | ARCHITECTURE §11 |
| 位置エンコーディング方式 | RoPEからALiBiに変更（ONNXグラフ軽量化・小d_model対応） | ARCHITECTURE §4 |
| ALiBi CLS位置設計 | CLSトークンの距離テーブルを0埋め（位置バイアスなし、コンテンツベース集約） | ARCHITECTURE §4 |
| ALiBiバイアステーブル保持方式 | 事前計算済みテーブルをregister_buffer / ONNX Initializerとして保持 | ARCHITECTURE §4 |
| 捨て牌Attentionヘッド数 | シングルヘッドで開始、性能不足時にマルチヘッド化を検討 | ARCHITECTURE §4 |
| ドラ表現方式 | 牌ごとのドラフラグ方式を採用。場況ドラマスク34次元は削除、ドラ表示牌枚数スカラー1次元に置換 | ARCHITECTURE §6 |
| 捨て牌の赤ドラ・ドラフラグ | 捨て牌入力に赤ドラフラグ(1)+ドラフラグ(1)を追加（4→6次元） | ARCHITECTURE §7 |
| リーチ状態の表現 | 他家リーチ（既存）+ 自分リーチフラグ1次元を場況に追加 | ARCHITECTURE §6 |
| ドラ算出ユーティリティ | `DoraCalculator` 実装（三麻萬子サイクル対応） | `MjlogReader/Utilities/` |
| 合法手判定エンジン | `AgariChecker` / `TenpaiChecker` / `FuritenChecker` / `ValidActionGenerator` 実装 | `MjlogReplayer/Rules/` |
| GameActionType | 12ビットFlags enum（Nuki追加）、Discard/Riichi並立方式 | ARCHITECTURE §7 |
| 北抜きアクション | `GameActionType.Nuki` + `ValidActionGenerator` で三麻対応 | `MjlogReplayer/Rules/` |

---

## 9. 比較実験メモ

### 牌表現方式の比較実験

| 指標 | 測定方法 |
|------|----------|
| 学習収束速度 | 何エポックで収束するか |
| 最終精度 | 打牌一致率、鳴き判断精度 |
| 少データ性能 | 1万局、10万局、100万局での比較 |
| 推論レイテンシ | Unity実機での測定 |

### 埋め込み戦略の比較実験

| 戦略 | 初期化 | requires_grad |
|------|--------|---------------|
| 固定 | Skip-gram | false |
| ファインチューニング（推奨） | Skip-gram | true |
| ランダム初期化 | ランダム | true |

### データ収集戦略の比較実験

| 手法 | 評価指標 |
|------|----------|
| 牌譜のみ | ベースモデル精度 |
| 牌譜 + スタイルフィルタ | キャラクター識別率 |
| 牌譜 + スナップショット | 難局での判断精度 |
| 結果ベースフィルタ | 勝率への影響 |
