# MjlogReplayerSample

MjlogReplayer ライブラリの使用方法を示すサンプルアプリケーションです。天鳳牌譜（mjlog）を読み込み、試合状態を再現してコンソールに表示します。

## 概要

このサンプルアプリケーションでは、以下の機能をデモンストレーションしています：

- 牌譜ファイル（.mjlog / .xml）の読み込み
- 試合状態（GameState）の再現
- 特定セッション・ステップの状態表示
- プレイヤー状態（手牌、捨て牌、副露、リーチ状態）の詳細表示
- ドラ表示牌、巡目などの場況情報の表示

## 必要要件

- .NET 10.0 以上
- MjlogReader ライブラリ
- MjlogReplayer ライブラリ

## ビルド

```bash
cd src/MjlogReplayerSample
dotnet build
```

## 使用方法

### 基本的な使い方

```bash
# ファイルから牌譜を読み込んで再現
dotnet run -- path/to/game.mjlog

# 全ステップを詳細表示
dotnet run -- path/to/game.mjlog -v
```

### コマンドラインオプション

| オプション | 説明 |
|-----------|------|
| `file` | 牌譜ファイルのパス（.mjlog または .xml） |
| `-x, --xml <文字列>` | XML形式の牌譜文字列を直接指定 |
| `-v, --verbose` | 全ステップの詳細を表示 |
| `-s, --session <番号>` | 特定セッション（局）のみ表示（0から開始） |
| `--step <番号>` | 特定ステップのみ表示（-s と併用） |

### 使用例

```bash
# 全局の最終状態を表示
dotnet run -- game.mjlog

# 全局の全ステップを表示
dotnet run -- game.mjlog -v

# セッション0（東1局）のみ表示
dotnet run -- game.mjlog -s 0

# セッション0の全ステップを詳細表示
dotnet run -- game.mjlog -s 0 -v

# セッション2のステップ15時点の状態を詳細表示
dotnet run -- game.mjlog -s 2 --step 15

# XML文字列から直接読み込み
dotnet run -- -x "<mjloggm ver=\"2.3\">...</mjloggm>"
```

### ヘルプの表示

```bash
dotnet run -- --help
```

## 出力例

### 基本出力（最終状態のみ）

```
ファイルから牌譜を読み込みました: game.mjlog

=== 牌譜再現結果 (8局) ===

--- セッション 0: 東1局 ---
  ステップ数: 48

  [最終状態]
    巡目: 12
    ドラ表示牌: 🀓

    P0: 24300点
      手牌: 🀇🀇🀈🀉🀊🀋🀌🀍🀎🀏🀐🀑🀒
      捨牌: 🀀🀁🀂🀄🀆🀫🀖🀗🀘

    P1: 24300点
      手牌: 🀙🀚🀛🀜🀝🀞🀟🀠🀡🀢🀣🀤🀥
      捨牌: 🀃🀅🀆🀇🀈🀉🀊

    ...
```

### 詳細モード出力（-v オプション）

```
--- セッション 0: 東1局 ---
  ステップ数: 48

  [ステップ   0] 巡目  1  == 詳細 ==> (初期状態)
    P0: 手牌[🀇,🀈,🀉,🀊,🀋,🀌,🀍,🀎,🀏,🀐,🀑,🀒,🀓,🀀]
        捨て牌[]
    P1: 手牌[🀙,🀚,🀛,🀜,🀝,🀞,🀟,🀠,🀡,🀢,🀣,🀤,🀥]
        捨て牌[]
    P2: 手牌[🀀,🀁,🀂,🀃,🀄,🀅,🀆,🀇,🀈,🀉,🀊,🀋,🀌]
        捨て牌[]
    P3: 手牌[🀐,🀑,🀒,🀓,🀔,🀕,🀖,🀗,🀘,🀙,🀚,🀛,🀜]
        捨て牌[]

  [ステップ   1] 巡目  1  == 詳細 ==> Discard { ... }
    P0: 手牌[🀇,🀈,🀉,🀊,🀋,🀌,🀍,🀎,🀏,🀐,🀑,🀒,🀓]
        捨て牌[🀀]
    ...
```

### 特定ステップの詳細表示（--step オプション）

```
--- セッション 0, ステップ 15 ---
  局: 東1局
  本場: 0
  供託: 0
  親: P0
  巡目: 4
  ステップ: 15
  ドラ表示牌: 🀓

  [プレイヤー状態]
  --- P0: 25000点 ---
    手牌: 🀇🀈🀉🀊🀋🀌🀍🀎🀏🀐🀑🀒🀓
    捨牌:
      [ 1] 🀀
      [ 2] 🀁
      [ 3] 🀂 (ツモ切り)
      [ 4] 🀃

  --- P1: 25000点 ---
    手牌: 🀙🀚🀛🀜🀝🀞🀟🀠🀡🀢🀣🀤🀥
    捨牌:
      [ 1] 🀄
      [ 2] 🀅 (P2が鳴き)
      [ 3] 🀆

  --- P2: 25000点 ---
    手牌: 🀀🀁🀂🀃🀄🀇🀈🀉🀊🀋🀌
    捨牌:
      [ 1] 🀫
      [ 2] 🀖
    副露:
      ポン 🀅🀅🀅 (from P1)
```

## コード構造

```
MjlogReplayerSample/
├── ApplicationMain.cs          # エントリポイント・コマンドライン処理
└── MjlogReplayerSample.csproj  # プロジェクトファイル
```

### 主要メソッド

| メソッド | 説明 |
|---------|------|
| `Main` | エントリポイント、コマンドラインオプションを定義 |
| `HandleCommand` | 牌譜の再現と表示処理のメインハンドラー |
| `DisplaySession` | セッション（局）を表示 |
| `DisplayStepSummary` | ステップの要約を表示 |
| `DisplayGameStateSummary` | GameStateの要約を表示 |
| `DisplayGameStateDetail` | GameStateの詳細を表示 |
| `DisplayPlayerStateSummary` | PlayerStateの要約を表示 |
| `DisplayPlayerStateDetail` | PlayerStateの詳細を表示 |

## MjlogReaderSample との違い

| 観点 | MjlogReaderSample | MjlogReplayerSample |
|------|-------------------|---------------------|
| 主な用途 | 牌譜データの内容確認 | 試合状態の再現・分析 |
| 表示データ | 行動ログ（イベント列） | 各時点の場況（スナップショット） |
| 依存ライブラリ | MjlogReader | MjlogReader + MjlogReplayer |
| 主要クラス | MjlogDocument, MjlogSession | DocumentReplayer, GameState |

## 関連ドキュメント

- [MjlogReplayer README](../MjlogReplayer/README.md) - Replayerライブラリの詳細ドキュメント
- [MjlogReader README](../MjlogReader/README.md) - Readerライブラリの詳細ドキュメント
- [MjlogReaderSample README](../MjlogReaderSample/README.md) - Readerサンプルアプリケーション

## ライセンス

zlib License - 詳細は [LICENSE.md](../../LICENSE.md) を参照してください。
