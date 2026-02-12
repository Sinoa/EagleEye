# EagleEye アーキテクチャ設計書

本ドキュメントはEagleEyeプロジェクトの全設計決定事項を統合したものです。

**関連ドキュメント**:
- `DEVELOPMENT.md` — 開発計画・タスク管理・未解決課題

---

## 目次

1. [モデル全体アーキテクチャ](#1-モデル全体アーキテクチャ)
2. [牌埋め込み設計](#2-牌埋め込み設計)
3. [インスタンスID・手牌ソート](#3-インスタンスid手牌ソート)
4. [Attention・Transformer設計](#4-attentiontransformer設計)
5. [副露エンコーディング](#5-副露エンコーディング)
6. [場況特徴量](#6-場況特徴量)
7. [出力設計・アクション空間](#7-出力設計アクション空間)
8. [ONNX対応（Unity / Web）](#8-onnx対応unity--web)
9. [LoRAキャラクター個性化](#9-loraキャラクター個性化)
10. [データパイプライン](#10-データパイプライン)
11. [データ収集戦略](#11-データ収集戦略)
12. [バリアント設計（4人/3人麻雀）](#12-バリアント設計4人3人麻雀)
13. [既存実装（MjlogReader / MjlogReplayer）](#13-既存実装mjlogreader--mjlogreplayer)
14. [未解決課題・比較実験](#14-未解決課題比較実験)

---

## 1. モデル全体アーキテクチャ

### 入力

| 情報 | 次元 | 処理 |
|------|------|------|
| 手牌 | 2〜14枚 × 9次元 | 牌埋め込み(4) + インスタンスID埋め込み(2) + 赤ドラフラグ(1) + ツモフラグ(1) + ドラフラグ(1) |
| 捨て牌 | 0〜25枚 × 6次元 × 4系列 | 牌埋め込み(4) + 赤ドラフラグ(1) + ドラフラグ(1) + ALiBi位置バイアス |
| 副露 | 0〜4組 × 4プレイヤー | 構成牌集約 + 種別 + 鳴き元 + 順序 + 巡目 = 18次元 |
| 場況 | 約46〜58次元 | 点数・巡目・場風・自風・ドラ表示枚数・リーチ等 |
| アクションマスク | 各種 | 有効アクションのフラグ |

### エンコーダー構成

```
手牌 → Self-Attention（位置エンコーディングなし）→ CLS出力
捨て牌×4 → Self-Attention + ALiBi → CLS出力
副露×4 → MeldEncoder(Linear→SiLU) → 平均プーリング
場況 → MLP(2層) → 場況特徴ベクトル
```

### 融合・出力

```
Concat(全特徴) → MLP(2〜3層) → 共通特徴ベクトル
    → 手牌ロジット[14] + アクション種別ロジット[11]
    → Sigmoid(BCE) で各要素の自信度を出力
    → 推論時: アクション種別に応じてTopKでサンプリング
```

### モデルサイズ候補（未確定）

| 構成 | ブロック数 | d_model | d_ff | パラメータ |
|------|-----------|---------|------|-----------|
| 軽量 | 2 | 64 | 256 | 約50K |
| 標準 | 4 | 128 | 512 | 約500K |
| 大規模 | 6 | 256 | 1024 | 約2M |

→ まず軽量構成で Phase 1 実験、その後スケールアップ。

---

## 2. 牌埋め込み設計

### 決定事項

- **方式**: Skip-gram事前学習済み 4次元埋め込み
- **語彙**: 4人麻雀=34種、3人麻雀=27種（赤ドラは別フラグで表現、赤5と通常5は同一埋め込みを共有）
- **推奨戦略**: 事前学習で初期化 → ファインチューニング（requires_grad=true）

### 実装パターン

```csharp
// 推奨: ファインチューニング可能な実装
_embedding = nn.Embedding(vocabSize, embDim);
_embedding.weight = nn.Parameter(
    torch.tensor(pretrainedWeights),
    requires_grad: true  // ファインチューニング
);

// 固定する場合: register_buffer を使用
register_buffer("embeddings", tensor);  // state_dictに含まれるがオプティマイザ対象外
```

### 段階的学習

```
Stage 1: 埋め込み固定(requires_grad=false) → 他の層を学習
Stage 2: 埋め込み解凍(requires_grad=true) → 低学習率で微調整
```

差分学習率: 埋め込み=1e-5、その他=1e-3

### 比較実験対象（未実施）

| 方式 | 次元 | 事前学習 |
|------|------|----------|
| 埋め込み（主案） | 8 (牌4+ID2+赤1+ドラ1) | 必要（牌埋め込みのみ） |
| マルチホット | 41 (牌種37+ID4) | 不要（赤5は37種に含む） |

マルチホットもUnity6環境で十分実用的（追加計算量は全体の0.001%未満）。

### register_buffer vs Parameter

| 特性 | Parameter | Buffer |
|------|-----------|--------|
| state_dict()に含まれる | ✅ | ✅ |
| オプティマイザで更新 | ✅ | ❌ |
| .to(device)で移動 | ✅ | ✅ |
| 用途 | 学習する重み | 固定値・統計量 |

---

## 3. インスタンスID・手牌ソート

### インスタンスID

同一牌（例: 1萬が3枚）を区別するための 0〜3 の識別子。

```
牌:           [1萬, 1萬, 1萬, 2萬, ...]
インスタンスID: [0,   1,   2,   0,  ...]
```

- 埋め込み次元: 2（学習可能）
- ID=0: 孤立牌、ID=1: 対子、ID=2: 刻子、ID=3: 槓子候補

### 手牌入力構成

```
牌埋め込み(4) + インスタンスID埋め込み(2) + 赤ドラフラグ(1) + ツモフラグ(1) + ドラフラグ(1) = 9次元
```

### 手牌ソート（推奨）

理牌状態（牌IDの昇順）で入力。理由:
- インスタンスIDの割り当てが自明
- 同じ手牌が常に同じテンソル表現 → 勾配の分散が小さい
- デバッグ容易

```csharp
var sortedHand = hand.OrderBy(tile => tile.Id).ToList();
var instanceIds = new int[sortedHand.Count];
var counts = new Dictionary<int, int>();
for (int i = 0; i < sortedHand.Count; i++)
{
    var tileId = sortedHand[i].Id;
    instanceIds[i] = counts.GetValueOrDefault(tileId, 0);
    counts[tileId] = instanceIds[i] + 1;
}
```

---

## 4. Attention・Transformer設計

### 位置エンコーディング

| 対象 | 方式 | 理由 |
|------|------|------|
| 手牌 | なし | 集合（順序に意味なし） |
| 捨て牌 | ALiBi | 巡目（距離）が重要、ONNXフレンドリー |

**ALiBi CLS位置設計（決定済み）**: CLSトークンの距離テーブルは0埋め。CLS行・列のバイアスを
すべて0とし、CLSの注意配分を純粋にコンテンツベース（QK内積）で決定する。
牌トークン間の距離 `|i-j|` は通常通り保持。

**ALiBiバイアステーブル（決定済み）**: シーケンス最大長（CLS+捨て牌最大25=26）で事前計算し、
`register_buffer`でモデルパラメータとして保持。ONNXではInitializerとしてグラフに埋め込み。

**捨て牌Attentionヘッド数（決定済み）**: 初期実装はシングルヘッドで運用。
シーケンスが短く（最大26）、巡目情報が場況特徴量から別経路で供給されるため、
単一スロープでも十分にカバー可能と判断。性能不足時にマルチヘッド化を検討する段階的方針。
スロープ初期値は `m = 0.0625` とし、ハイパーパラメータとして調整可能。

### 集約手法

- **手牌・捨て牌**: CLSトークン方式（可変長対応、学習可能な集約）
- CLSトークンの出力はすべての入力トークンの加重和 → 勾配は全トークンに自動伝播

### Attention種類の使い分け

| 処理 | 種類 | 理由 |
|------|------|------|
| 手牌内 | Self-Attention | 牌同士の関係性 |
| 捨て牌内 | Self-Attention | 捨て牌の流れ |
| 手牌→捨て牌 | Cross-Attention（検討中） | 相手の捨て牌を自分の手牌視点で評価 |

### Transformerブロック構成

```
LayerNorm → Self-Attention → Residual → LayerNorm → FFN(2層) → Residual
```

FFN内部: Linear → SiLU → Dropout → Linear

### MLP深さ目安

| コンポーネント | 隠れ層数 |
|---------------|---------|
| 場況エンコーダー | 2〜3層 |
| 副露エンコーダー | 1層 |
| 融合層 | 2〜4層 |
| 出力ヘッド | 1〜2層 |

---

## 5. 副露エンコーディング

### 入力構成（18次元）

```
牌埋め込み(4) | 副露種別(5) | 鳴き元(4) | 順序(4) | 巡目(1) = 18次元
```

### 処理

```
入力(18) → Linear(18, output_dim) → SiLU → 出力
複数副露 → 各出力を平均プーリング
副露なし → ゼロベクトル
副露数 → 別途スカラー特徴（0〜4）
```

### 各要素の表現

| 対象 | カテゴリ数 | 方式 |
|------|----------|------|
| 副露種別 | 5 (ポン/チー/暗槓/明槓/加槓) | ワンホット |
| 鳴き元 | 4 (上家/対面/下家/自分) | ワンホット |
| 順序 | 4 (0〜3番目) | ワンホット |
| 巡目 | 1〜18 | 連続値 `(turn-1)/17` |
| 構成牌 | 34種 | 埋め込み → 平均プーリング |

### 順序と巡目の役割

- **順序**: 副露同士の相対的な前後関係（「2番目の鳴き」）
- **巡目**: 絶対的なタイミング（「5巡目に鳴いた」）
- **場況の巡目**: 現在の進行状況（グローバル）← これらは別の情報

---

## 6. 場況特徴量

### 次元構成（約46〜58次元）

```
【連続値】11次元
  自点数(区分線形):1, 他家点数×3:3, トップ差:1, ラス差:1,
  巡目(/18):1, 残り山(/70):1, 本場(/10 clip):1, 供託(/4):1,
  ドラ表示牌枚数(/5):1

【カテゴリ】20次元
  場風:4, 自風:4, 局:8, 順位:4

【バイナリ】9次元
  他家リーチ×3:3, 他家一発圏内×3:3, 親番:1, オーラス:1, 自分リーチ:1

【離散値】6〜18次元
  他家副露数×3:3or12, 他家リーチ巡目×3:3

→ MLP(2層) → 場況特徴ベクトル
```

### ドラ表現の変更（決定事項）

ドラ情報は場況のドラマスク34次元ではなく、手牌・捨て牌の各牌にドラフラグ（1次元）として付与する方式に変更。
場況にはドラ表示牌枚数（1〜5のスカラー、/5で正規化）のみ保持する。

### 点数の正規化（区分線形変換）

```csharp
float NormalizePoints(int points) => points switch
{
    < 0      => -1.0f + points / 10000f,                    // トビ領域（不連続ジャンプ）
    < 10000  => -0.5f + points / 10000f * 0.5f,             // 危険域
    < 20000  => 0.0f + (points - 10000) / 10000f * 0.25f,   // 苦しい
    < 35000  => 0.25f + (points - 20000) / 15000f * 0.35f,  // 平均〜優勢
    _        => 0.6f + (points - 35000) / 65000f * 0.4f     // 大トップ
};
```

- マイナス点で不連続ジャンプ（「質的に異なる状況」を明示）
- パラメータは設定ファイルで外部化し、学習と推論で一致を保証
- ルール差異（25000点持ち vs 30000点持ち）では閾値調整が必要

### 順位特徴量

```csharp
int GetCurrentRank(int selfPoints, int[] allPoints)
    => allPoints.Count(p => p > selfPoints);  // 0=トップ, 3=ラス
// ワンホット[4]で表現
```

区分線形点数（絶対的状況）とランク（相対的順位）は両方保持。

---

## 7. 出力設計・アクション空間

### 採用方式: 単一ヘッド + BCE

```
モデル出力:
├── 手牌ロジット[14] → 各牌の選択自信度
└── アクション種別ロジット[11] → 各アクションの自信度（ActionType enum対応）

損失: Binary Cross Entropy（BCE）で統一
推論: アクション種別に応じてTopKでサンプリング
  ├── 打牌/リーチ: Top1
  ├── チー/ポン: Top2
  └── ツモ/ロン/槓/九種九牌/スルー: 手牌ロジットは無視
```

### ActionType列挙型（11ビット）

```csharp
[Flags]
public enum ActionType
{
    None=0, Discard=1<<0, Riichi=1<<1, Tsumo=1<<2, Ron=1<<3,
    Pon=1<<4, Chi=1<<5, AnKan=1<<6, KaKan=1<<7,
    DaiMinKan=1<<8, KyuushuKyuuhai=1<<9, Skip=1<<10,
}
```

- Riichi: 打牌を内包するアトミックアクション（Riichi有効時はDiscardは立たない）
- Skip: 鳴き見送り、ロン見逃し、行動不可をすべて同値として扱う

### アクション種別と手牌ロジットの関係

| アクション | 手牌ロジット | 備考 |
|-----------|-------------|------|
| Discard / Riichi | Top1 | 打牌選択 |
| Pon / Chi | Top2 | 使用牌選択 |
| Tsumo / Ron / DaiMinKan / KyuushuKyuuhai / Skip | なし | 牌選択不要 |
| AnKan / KaKan | 後続フェーズで詳細化 | 複数候補時の選択方法は要検討 |

### チーのパターン選択（方式A: 粗粒度）

アクションマスクではChiの1ビットのみ。パターン選択は手牌ロジットのTop2に委任。
学習ベースで正しいパターンを学習し、推論時にルールベースのフォールバックを実装。

### BCE統一の利点

- 不確実性の表現（複数候補がある状況を自然に表現）
- 矛盾データの吸収（同じ状況で異なる選択があった場合も学習可能）
- サンプリング戦略を後から変更可能
- Softmaxへの切り替えも互換（TopKは値の大小関係のみ使用）

### アクションマスク

```csharp
masked_logits = logits + (-1e9f) * (1 - mask)
// mask: 有効=1, 無効=0
// softmax後にほぼ0 → 勾配も自動的にほぼゼロ
```

### アクション空間

**ツモ番**: Discard, Riichi, Tsumo, AnKan, KaKan, KyuushuKyuuhai
**他家打牌後**: Skip, Pon, Chi（上家のみ）, DaiMinKan, Ron

---

## 8. ONNX対応（Unity / Web）

### 対象ランタイム

| ランタイム | プラットフォーム | Swish演算子 |
|-----------|----------------|------------|
| Unity ONNX Runtime | モバイル / ゲーミングPC | ○ サポート |
| ONNX Runtime Web | ブラウザ（Chrome等） | × 非サポート（OpSet 22まで） |

### Attentionの分解

Unity ONNX RuntimeにAttention演算子がないため、基本演算子に分解:
MatMul, Transpose, Mul(スケーリング), Softmax, Reshape, Add

ALiBiバイアス行列は事前計算済みの定数テンソルとしてONNX Initializerに含まれる。
実行時の追加計算は不要（Addオペレータのみ）。

### 重み行列形状（シーケンス長非依存）

```
入力: [batch, seq_len, dim]    ← seq_lenは可変
W_Q:  [dim, dim]               ← 固定
W_K:  [dim, dim]               ← 固定
W_V:  [dim, dim]               ← 固定
```

### 活性化関数

SiLU（= Swish）を採用。学習時は `functional.silu()` を使用。

| ランタイム | 対応方式 |
|-----------|---------|
| Unity ONNX | Swish演算子（ネイティブ） |
| Web ONNX | `Sigmoid + Mul` に分解（OpSet 1の基本演算子で再現） |

融合版と分解版の精度差は 1e-7 程度で完全に無視可能。Mish関数も検討したがSiLU分解で対応可能のため不採用。

### 正規化

LayerNormを採用（BatchNormはバッチサイズ1で動作しない、学習・推論で挙動が異なる）。

### 動的軸

```csharp
// hand_tiles: [batch, seq_len] ← batch, seq_len ともに動的
// discard_tiles: [batch, seq_len] ← 同上
```

---

## 9. LoRAキャラクター個性化

### 構造

```
W' = W + α × (B × A)
W: 元の重み（凍結）, A: [rank, d_in]（学習）, B: [d_out, rank]（学習）
```

初期化: B=ゼロ、A=ランダム → 初期状態で影響ゼロ保証

### 階層的LoRA

```
ベースモデル（凍結）
├─ スタイルLoRA（10種程度, rank=16〜32）: 攻撃型/守備型/門前型/鳴き型
└─ キャラLoRA（数百種, rank=4〜8）: 固有微調整

推論時: W' = W_base + α₁(B₁A₁) + α₂(B₂A₂)
```

### 推奨適用箇所

| 箇所 | 推奨度 | 理由 |
|------|--------|------|
| 融合層 | ◎ | 複合的な判断基準 |
| 出力ヘッド直前 | ◎ | 最終的な行動傾向 |
| FFN層 | ○ | 状況判断の基準 |
| Attention QKV | △ | 基本読みが崩れるリスク |

### 個性強調（推論時調整可能）

```
Lv1: α=0.5〜0.8（控えめ）
Lv2: α=1.0, rank=8（標準）
Lv3: α=1.5〜2.0（強調）
Lv4: α=2.0〜3.0, rank=32, temp=0.3〜0.5（最大強調）
```

### 容量試算

| 項目 | 数量 | 合計 |
|------|------|------|
| ベースモデル | 2 | 10MB |
| スタイルLoRA (rank=16) | 10 | 500KB |
| キャラLoRA (rank=4) | 300 | 3.6MB |
| **合計** | - | **約14MB** |

---

## 10. データパイプライン

### 全体フロー

```
mjlog → MjlogReader → MjlogReplayer → GameState
    → DataPipeline(視点変換、特徴抽出) → JSONL
        → 学習時: テンソル化（埋め込み参照、正規化、ワンホット）
```

### 中間データ形式（JSONL 1行）

```json
{
  "round": 0, "turn": 5, "honba": 0, "kyotaku": 0,
  "scores": [25000, 25000, 25000, 25000],
  "dora_indicators": [15],
  "hands": [[0, 0, 1, 2, ...], [...], [...], [...]],
  "hand_red_flags": [[false, false, false, ...], [...], [...], [...]],
  "hand_dora_flags": [[false, true, false, ...], [...], [...], [...]],
  "discards": [[3, 5, 8], [...], [...], [...]],
  "discard_red_flags": [[false, false, false], [...], [...], [...]],
  "discard_dora_flags": [[false, false, true], [...], [...], [...]],
  "riichi_flags": [false, false, false, false],
  "riichi_turns": [-1, -1, -1, -1],
  "melds": [[], [], [], []],
  "actor_index": 0,
  "action_type": "Discard",
  "action_tile_index": 3,
  "valid_actions": ["Discard", "Riichi", "Tsumo"]
}
```

牌IDは `TileTypeId`（0-33の34種体系）。赤ドラは並行する `*_red_flags` 配列、ドラ（表ドラ）は `*_dora_flags` 配列で表現。有効アクションは `ActionType` enum名の配列（学習時にマルチホット11次元に変換）。

### 前処理で確定するもの

- 場況の復元（生値: 牌ID(TileTypeId 0-33)、点数等）
- 赤ドラフラグ（牌ID列と並行するbool配列）
- 正解ラベル（ActionType enum名 + インデックス）
- 有効アクション（ActionType enum名の配列）
- 牌ID列（整数配列）

### 学習時に行うもの

- 牌ID → 埋め込みルックアップ
- 赤ドラフラグ → float変換
- 点数 → 区分線形正規化
- カテゴリ → ワンホット
- 有効アクション → マルチホットベクトル（11次元）
- バッチ化（パディング、マスク生成）

### 視点変換

```
絶対位置: プレイヤー0, 1, 2, 3
相対位置: 自分=0, 下家=1, 対面=2, 上家=3

int[] GetRelativeOrder(int viewpoint)
    => Enumerable.Range(0, 4).Select(i => (viewpoint + i) % 4).ToArray();
```

### データリーク防止チェックリスト

| 情報 | 含めてOK |
|------|---------|
| 自分の手牌 | ✅ |
| 他家の手牌 | ❌ 絶対NG |
| 全員の捨て牌 | ✅ |
| 全員の副露 | ✅ |
| 全員の点数 | ✅ |
| リーチ状態 | ✅ |
| 山牌の残り枚数 | ✅ |
| 山牌の中身 | ❌ 絶対NG |
| 裏ドラ | ❌ |

---

## 11. データ収集戦略

### 用途別推奨手法

| 用途 | 手法 |
|------|------|
| ベースモデル | 教師あり学習（大量牌譜模倣） |
| キャラクターLoRA | スタイル別牌譜フィルタ + 直接ラベル付け |
| 難局対応 | スナップショット + 専門家回答 |
| 強化改善（オプション） | 自己対戦 + Eloベース |

### 段階的アプローチ

```
Phase 1: 教師あり学習でベースモデル構築
Phase 2: スタイル別フィルタリングでLoRA学習
Phase 3: スナップショット収集で不足部分を補完
Phase 4: 自己対戦による強化（オプション）
```

### RLHFの判断

フルRLHFは過剰。直接ラベル付けやスタイル別フィルタリングで代替可能。

---

## 12. バリアント設計（4人/3人麻雀）

### 方針: モデル別、コード共通

| 観点 | 4人麻雀 | 3人麻雀 |
|------|---------|---------|
| 牌種 | 34種 | 27種（2〜8萬なし） |
| プレイヤー数 | 4人 | 3人 |
| チー | あり | なし |
| 北抜き | なし | あり |
| 捨て牌系列 | 4 | 3 |

### 共通化（Core）

TransformerBlock, ALiBi, MaskedAttention, LayerNorm, MeldEncoder, BCE損失, ONNXエクスポート

### 分離（Variant固有）

牌埋め込みテーブル, アクション空間, 入力テンソル構築, 出力デコード, 場況特徴量構築

### 設定ファイル例（4人麻雀）

```json
{
  "variant": "FourPlayer",
  "tileVocabSize": 34, "tileEmbeddingDim": 4,
  "instanceIdCount": 4, "instanceIdEmbDim": 2,
  "playerCount": 4, "hasChi": true, "hasKitaNuki": false,
  "attentionDim": 64, "attentionHeads": 1,
  "attentionLayers": 2, "feedForwardDim": 256,
  "maxHandTiles": 14, "maxDiscardTiles": 25, "maxMelds": 4
}
```

---

## 13. 既存実装（MjlogReader / MjlogReplayer）

### MjlogReader

天鳳牌譜（.mjlog / XML）をC#オブジェクトに変換。

**主要モデル**:
- `MjlogDocument` → Header + Sessions
- `MjlogSession` → RoundWind, Honba, DealerId, InitialHands, Steps, Result
- `MjlogStep` → StepIndex, TurnNumber, PlayerId, Action
- `MjlogAction` → Draw, Discard, Meld, Reach, Dora, Agari, Ryuukyoku
- `Tile` → Suit, Number, IsRedDora, OriginalId(0-135), TileTypeId(0-33)
- `MeldInfo` → Type, Tiles, CalledTile, FromPlayer(相対位置), OriginalCode
- `GameRule` → HasRedDora, HasOpenTanyao, IsEastOnly, IsThreePlayer 等

**MeldType**: Chi, Pon, DaiMinKan, KaKan, AnKan, Nuki
**RyuukyokuType**: Exhaustive, NineTerminals, FourWinds, FourKans, FourReach, TripleRon, NagashiMangan

### MjlogReplayer

MjlogReaderの出力から各ステップのGameStateをイミュータブルに構築。

**主要モデル**:
- `GameState` (abstract) → RoundWind, RoundNumber, Honba, Kyotaku, DealerId, TurnNumber, StepIndex, Players, DoraIndicators
- `FourPlayerGameState` : GameState (PlayerCount=4)
- `ThreePlayerGameState` : GameState (PlayerCount=3, NukiDoras)
- `PlayerState` → PlayerId, Hand, Discards, Melds, IsReach, Score
- `DiscardedTile` → Tile, IsTsumogiri, IsReachDeclare, CalledByPlayerId

**巡目計算**: 全プレイヤー打牌完了→次巡。鳴き発生時に打牌カウントリセット。

**GameStateBuilder**: 外部入力からGameStateを構築可能（テスト用途にも有用）。

### DataPipelineへの接続時の確認事項

| 項目 | 状況 | 対処 |
|------|------|------|
| リーチ巡目 | ✅ `PlayerState.ReachTurnNumber` (int?) で取得可能 | 解決済み |
| 残り山牌数 | ✅ `GameState.RemainingTileCount` (int) で取得可能 | 解決済み |
| 副露の巡目 | ✅ `MeldInfo.TurnNumber` (int?) で取得可能 | 解決済み |
| 副露の順序 | ✅ `PlayerState.Melds` リスト順 = 時系列順で保証 | 確認済み |
| 鳴き元の暗槓 | FromPlayerの暗槓時の値 | 自分=0かどうか要確認 |

---

## 14. 未解決課題・比較実験

### 高優先度（Phase 0-1 ブロッカー）

- [ ] 前処理パイプライン実装（GameState → JSONL）
- [ ] ベースモデルアーキテクチャ確定（層数・次元数）
- [ ] ONNXエクスポート検証（Unity動作確認）

### 中優先度（Phase 2-3）

- [ ] Flatten vs CLS 性能比較
- [ ] Cross-Attention検討
- [ ] サンプル不均衡対策（和了・リーチ・槓の低頻度）
- [ ] 推論レイテンシ測定（Unity実機）
- [ ] 学習対象フィルタリング戦略

### 比較実験リスト

| 実験 | 方式A | 方式B |
|------|-------|-------|
| 牌表現 | 埋め込み(8D) | マルチホット(41D) |
| 埋め込み戦略 | 固定 / ファインチューニング | ランダム初期化 |
| モデルサイズ | 軽量(2層/64d) | 標準(4層/128d) |
| 他家副露数 | 線形正規化(/4) | ワンホット(4D×3) |
| 集約手法 | CLSトークン | Flatten |

### 推奨される次のアクション

1. **Phase 0 完了**: DataPipeline実装（GameState→JSONL）
2. **Phase 1 開始**: 軽量構成(2層/64d)でベースモデル実験
3. **ONNX検証**: Unity動作確認まで一気通貫


