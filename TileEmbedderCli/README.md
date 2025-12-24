# TileEmbedderCli

麻雀牌の埋め込みベクトルを生成するコマンドラインツールです。ルールベースの共起関係のみで生成するか、天鳳牌譜ログから追加学習して生成することができます。

## 概要

このツールは以下の機能を提供します：

- **ルールベース生成**: 牌の種類や順子関係に基づく共起行列から埋め込みベクトルを生成
- **牌譜からの追加学習**: 天鳳牌譜（mjlog）の和了手牌から共起データを抽出して追加学習
- **カスタマイズ可能**: 埋め込み次元数、エポック数、学習率などのハイパーパラメータを自由に設定
- **複数の出力形式**: Safetensors形式とJSON形式での出力をサポート
- **可視化機能**: PCAやUMAPで2次元に削減してCSV出力（既存モデルからも可能）
- **再現性**: 乱数シードを指定して同じ結果を再現可能

## インストール

プロジェクトをビルドして実行ファイルを生成します。

```bash
dotnet build TileEmbedderCli/TileEmbedderCli.csproj -c Release
```

## 使用方法

### 基本構文

```bash
# 埋め込みベクトルの生成
TileEmbedderCli [-i <入力パス>] [-o <出力パス>] [オプション]

# 既存モデルから可視化
TileEmbedderCli -l <埋め込みファイル> [可視化オプション]
```

### 基本オプション

| オプション | 説明 | デフォルト |
|-----------|------|-----------|
| `-i, --input <パス>` | 牌譜ディレクトリのパス | なし（ルールベースのみ生成） |
| `-o, --output <パス>` | 出力ファイルのベース名 | `tile_embeddings` |
| `-l, --load <パス>` | 既存の埋め込みファイルを読み込んで可視化 (.json/.safetensors) | - |
| `--seed <数値>` | 乱数シード（再現性のため） | ランダム |
| `-r, --recursive` | サブディレクトリも含めて牌譜を検索 | false |
| `-p, --progress` | 進捗表示を有効にする | false |
| `--format <形式>` | 出力形式（safetensors/json/both） | `both` |
| `-h, --help` | ヘルプを表示 | - |

### ハイパーパラメータオプション

| オプション | 説明 | デフォルト |
|-----------|------|-----------|
| `--embedding-dim <数値>` | 埋め込み次元数 | 4 |
| `--epochs <数値>` | 学習エポック数 | 100 |
| `--negative-samples <数値>` | ネガティブサンプル数 | 10 |
| `--learning-rate <数値>` | 学習率 | 0.025 |

### 可視化オプション

| オプション | 説明 | デフォルト |
|-----------|------|-----------|
| `-v, --visualize` | 2次元可視化データをCSV形式で標準出力 | false |
| `--visualize-method <手法>` | 次元削減手法（pca/umap） | `pca` |
| `--include-attributes` | 属性トークンも含めて可視化 | false |

## 使用例

### 1. ルールベースのみで生成（最もシンプル）

```bash
TileEmbedderCli -p
```

入力パスを指定しない場合、ルールベースの共起関係のみから埋め込みベクトルを生成します。
- 出力: `tile_embeddings.safetensors`, `tile_embeddings.json`

### 2. 牌譜ディレクトリから追加学習

```bash
TileEmbedderCli -i ./mjlogs/ -o embeddings -p
```

指定したディレクトリ内の牌譜ファイル（.mjlog, .xml等）を読み込み、和了手牌の共起データを抽出して追加学習します。
- 出力: `embeddings.safetensors`, `embeddings.json`

### 3. サブディレクトリを含めて学習

```bash
TileEmbedderCli -i ./mjlogs/ -r -p
```

`-r` オプションを指定すると、サブディレクトリも含めて再帰的に牌譜ファイルを検索します。

### 4. JSON形式のみで出力

```bash
TileEmbedderCli -i ./mjlogs/ --format json -p
```

Safetensors形式は出力せず、JSON形式のみで出力します。

### 5. カスタムハイパーパラメータで学習

```bash
TileEmbedderCli -i ./mjlogs/ \
  --embedding-dim 8 \
  --epochs 200 \
  --negative-samples 15 \
  --learning-rate 0.01 \
  --seed 42 \
  -p
```

埋め込み次元数を8次元、エポック数を200、乱数シード42で学習します。

### 6. 高速テスト実行

```bash
TileEmbedderCli --epochs 10 -p
```

エポック数を減らして高速に動作確認できます。

### 7. PCAで2次元可視化（学習時）

```bash
TileEmbedderCli -p --visualize > embeddings.csv
```

学習と同時に、埋め込みベクトルをPCAで2次元に削減してCSV形式で標準出力します。このCSVファイルはスプレッドシートやPythonで可視化できます。

### 8. UMAPで2次元可視化（学習時）

```bash
TileEmbedderCli -p --visualize --visualize-method umap > embeddings_umap.csv
```

UMAPを使用した次元削減で可視化します。UMAPはPCAよりも局所的な構造を保持しやすい手法です。

### 9. 既存のJSONファイルから可視化

```bash
TileEmbedderCli -l tile_embeddings.json > viz.csv
```

既に生成済みのJSON形式の埋め込みファイルを読み込んで可視化します。

### 10. 既存のSafetensorsファイルから可視化

```bash
TileEmbedderCli -l tile_embeddings.safetensors > viz.csv
```

Safetensors形式の学習済みモデルを読み込んで可視化します。

### 11. 属性トークンを含めて可視化

```bash
TileEmbedderCli -l tile_embeddings.safetensors --visualize-method umap --include-attributes > viz_all.csv
```

デフォルトでは実牌のみ可視化されますが、`--include-attributes` オプションで属性トークン（萬子/筒子/索子など）も含めて可視化できます。

## 出力形式

### Safetensors形式

機械学習モデルの標準的なフォーマットで、効率的にテンソルを保存します。

- メタデータに `vocab_size`, `embedding_dim`, `token_mapping` を含む
- Python（Hugging Face Transformers等）から読み込み可能
- 学習済みモデルとして再利用可能

### JSON形式

人間が読みやすい形式で、以下の構造を持ちます：

```json
{
  "format": "tile_embeddings",
  "version": "1.0",
  "vocab_size": 45,
  "embedding_dim": 4,
  "embeddings": {
    "Man1": [0.123, -0.456, ...],
    "Man2": [0.234, -0.567, ...],
    ...
  }
}
```

### 可視化CSV形式

`--visualize` オプションを使用すると、以下の形式のCSVが標準出力されます：

```csv
Token,X,Y,Type,Method
Man1,0.123,-0.456,Number,PCA
Man2,0.234,-0.567,Number,PCA
East,0.345,-0.678,Wind,PCA
...
```

- **Token**: 牌のトークン名
- **X, Y**: 2次元削減後の座標
- **Type**: 牌の種類（Number/Wind/Dragon/Red）
- **Method**: 次元削減手法（PCA/UMAP）

このCSVは以下のツールで可視化できます：

- **Excel/Google Sheets**: 散布図として可視化
- **Python (matplotlib/seaborn)**: より高度な可視化
- **R (ggplot2)**: 統計的な可視化

## 牌譜データについて

- 天鳳牌譜（.mjlog, .xml）に対応
- `MjlogJ` ライブラリで自動的にパースされます
- 和了時の手牌と副露牌から共起データを抽出
- エラーのある牌譜は自動的にスキップされます（エラー件数はサマリー表示）

## トークン定義

埋め込みベクトルは以下の45トークンで構成されます：

- **実牌トークン（0-36）**: 赤5萬/筒/索、1-9萬/筒/索、東南西北白發中
- **属性トークン（37-44）**: 萬子/筒子/索子/風牌/三元牌/数牌/字牌/赤牌属性

詳細は [TileEmbedder README](../TileEmbedder/README.md) を参照してください。

## パフォーマンス

- ルールベースのみ: 数秒で完了
- 牌譜10,000件の学習: 数分程度（エポック数とハイパーパラメータに依存）

進捗表示（`-p`オプション）を有効にすると、処理状況を確認できます。

## トラブルシューティング

### 牌譜ディレクトリが見つからない

```
エラー: 入力ディレクトリが見つかりません: ./mjlogs/
```

入力パスが正しいか確認してください。相対パスまたは絶対パスで指定できます。

### 不正なパラメータ値

```
エラー: 埋め込み次元数は正の整数である必要があります。指定値: 0
```

パラメータは正の数である必要があります。デフォルト値を使用するか、適切な値を指定してください。

### 出力形式のエラー

```
エラー: 出力形式は 'safetensors', 'json', 'both' のいずれかを指定してください。
```

`--format` オプションには `safetensors`, `json`, `both` のいずれかを指定してください。

## ライセンス

zlib License

## 関連プロジェクト

- [TileEmbedder](../TileEmbedder/README.md) - 埋め込みベクトル生成ライブラリ
- [MjlogJ](../MjlogJ/README.md) - 天鳳牌譜読み込みライブラリ

