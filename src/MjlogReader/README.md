# MjlogReader ライブラリ

天鳳牌譜（mjlog）を素直に読み込み、構造化されたモデルに変換するための .NET ライブラリです。

## 目次

- [概要](#概要)
- [クイックスタート](#クイックスタート)
- [API リファレンス](#api-リファレンス)
- [モデル構造](#モデル構造)

---

## 概要

MjlogReader は天鳳の牌譜ファイル（.mjlog / XML）を読み込み、C# オブジェクトに変換します。
牌譜データを忠実に表現し、他のライブラリやアプリケーションで利用しやすい構造化されたモデルを提供します。

### 特徴

- **シンプルなAPI**: 同期・非同期の両方に対応した読み込みメソッド
- **GZip自動判定**: `.mjlog` / `.gz` ファイルは自動的にGZip解凍
- **構造化されたモデル**: 牌、鳴き、行動、結果など全てのデータを型安全に表現
- **巡目計算**: 麻雀ルールに基づいた巡目の自動計算

---

## クイックスタート

```csharp
using MjlogReader;

// ファイルから読み込み（GZip自動判定）
MjlogDocument document = MjlogDocumentReader.Load("path/to/file.mjlog");

// 非同期で読み込み
MjlogDocument document = await MjlogDocumentReader.LoadAsync("path/to/file.mjlog");

// XML文字列からパース
MjlogDocument document = MjlogDocumentReader.Parse(xmlString);

// ストリームから読み込み
using var stream = File.OpenRead("path/to/file.xml");
MjlogDocument document = MjlogDocumentReader.Load(stream);
```

### 基本的な使用例

```csharp
using MjlogReader;
using MjlogReader.Models;
using MjlogReader.Models.Actions;

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
        Console.WriteLine($"  {step}");
        
        // 行動の種類に応じた処理
        switch (step.Action)
        {
            case DrawAction draw:
                Console.WriteLine($"    ツモ: {draw.Tile}");
                break;
            case DiscardAction discard:
                Console.WriteLine($"    打牌: {discard.Tile} (ツモ切り: {discard.IsTsumogiri})");
                break;
            case MeldAction meld:
                Console.WriteLine($"    鳴き: {meld.Meld}");
                break;
            case ReachAction reach:
                Console.WriteLine($"    リーチ: Step={reach.Step}");
                break;
        }
    }
    
    // 結果を表示
    if (session.Result != null)
    {
        Console.WriteLine($"  結果: {session.Result}");
    }
}
```

---

## API リファレンス

### MjlogDocumentReader クラス

牌譜ファイルを読み込むためのメインAPI（静的クラス）

#### 同期メソッド（ファイルパス）

| メソッド | 説明 |
|---------|------|
| `Load(string path)` | ファイルから読み込み（GZip自動判定） |
| `LoadFromGzip(string path)` | GZip圧縮ファイルから読み込み |
| `Parse(string xmlContent)` | XML文字列からパース |

#### 同期メソッド（ストリーム）

| メソッド | 説明 |
|---------|------|
| `Load(Stream stream)` | ストリームから読み込み |
| `LoadFromGzip(Stream gzipStream)` | GZipストリームから読み込み |

#### 非同期メソッド（ファイルパス）

| メソッド | 説明 |
|---------|------|
| `LoadAsync(string path, CancellationToken)` | 非同期でファイルから読み込み |
| `LoadFromGzipAsync(string path, CancellationToken)` | 非同期でGZipファイルから読み込み |

#### 非同期メソッド（ストリーム）

| メソッド | 説明 |
|---------|------|
| `LoadAsync(Stream stream, CancellationToken)` | 非同期でストリームから読み込み |
| `LoadFromGzipAsync(Stream gzipStream, CancellationToken)` | 非同期でGZipストリームから読み込み |

---

## モデル構造

### MjlogDocument（牌譜ドキュメント）

牌譜全体のルートオブジェクト

```
MjlogDocument
├── Header: MjlogHeader           // ヘッダー情報
└── Sessions: List<MjlogSession>  // セッション（局）のリスト
```

### MjlogHeader（ヘッダー情報）

SHUFFLE, GO, UN 要素のデータ

```
MjlogHeader
├── GameId: string              // 牌譜ID
├── PlayedAt: DateTime          // 対戦日時
├── PlayerNames: string[4]      // プレイヤー名（席順）
├── PlayerDans: string[4]       // プレイヤーの段位
├── PlayerRates: float[4]       // プレイヤーのレート
├── PlayerSexes: string[4]      // プレイヤーの性別
├── Rule: GameRule?             // ゲームルール
├── ShuffleSeed: string?        // シャッフル情報
└── Reference: string?          // リファレンス情報
```

### GameRule（ゲームルール）

```
GameRule
├── HasRedDora: bool       // 赤ドラの有無
├── HasOpenTanyao: bool    // 喰いタンの有無
├── IsEastOnly: bool       // 東風戦かどうか
├── IsThreePlayer: bool    // 三人麻雀かどうか
├── Speed: int             // 速度（0=普通, 1=高速, 2=超高速）
├── HasKuikae: bool        // 喰い替えの有無
├── OriginalFlags: int     // ルールのビットフラグ（元データ）
└── Lobby: int             // ロビー番号
```

### MjlogSession（セッション/局）

INIT から AGARI/RYUUKYOKU までの1局分

```
MjlogSession
├── RoundWind: int               // 場風（0=東, 1=南, 2=西, 3=北）
├── RoundNumber: int             // 局番号（0-3: 東1-4局、4-7: 南1-4局...）
├── RoundName: string            // 局の表示名（例: "東1局"）※計算プロパティ
├── Honba: int                   // 本場数
├── Kyotaku: int                 // 供託リーチ棒の数
├── DealerId: int                // 親プレイヤーID（0-3）
├── StartScores: int[4]          // 各プレイヤーの開始時得点
├── InitialHands: List<Tile>[4]  // 各プレイヤーの配牌
├── InitialDoraIndicator: Tile?  // ドラ表示牌（初期）
├── DoraIndicators: List<Tile>   // 全ドラ表示牌（追加ドラ含む）
├── UraDoraIndicators: List<Tile>// 裏ドラ表示牌
├── Steps: List<MjlogStep>       // 行動ステップ履歴
└── Result: MjlogSessionResult?  // セッションの結果
```

### MjlogStep（行動ステップ）

```
MjlogStep
├── StepIndex: int        // ステップインデックス（0から始まる連番）
├── TurnNumber: int       // 巡目（1から始まる）
├── PlayerId: int         // 行動したプレイヤーID（システム行動は-1）
└── Action: MjlogAction   // 行動内容
```

### MjlogAction（行動）と派生クラス

```
MjlogAction (abstract)
└── ActionType: ActionType      // 行動の種類

DrawAction : MjlogAction        // ツモ
└── Tile: Tile?                 // ツモった牌

DiscardAction : MjlogAction     // 打牌
├── Tile: Tile?                 // 捨てた牌
└── IsTsumogiri: bool           // ツモ切りかどうか

MeldAction : MjlogAction        // 鳴き
└── Meld: MeldInfo?             // 鳴きの詳細情報

ReachAction : MjlogAction       // リーチ
├── Step: int                   // リーチステップ（1=宣言、2=成立）
└── IsAccepted: bool            // 成立したかどうか ※計算プロパティ

DoraAction : MjlogAction        // 新ドラ表示
└── Tile: Tile?                 // 新ドラ表示牌

AgariAction : MjlogAction       // 和了（事実の記録のみ）

RyuukyokuAction : MjlogAction   // 流局（事実の記録のみ）
```

#### ActionType

| 値 | 説明 |
|---|------|
| `Draw` | ツモ |
| `Discard` | 打牌 |
| `Meld` | 鳴き |
| `Reach` | リーチ |
| `Dora` | 新ドラ表示 |
| `Agari` | 和了 |
| `Ryuukyoku` | 流局 |

### Tile（牌）

```
Tile (record)
├── Suit: TileSuit        // 牌の種別
├── Number: int           // 数字（数牌:1-9、字牌:1-7）
├── IsRedDora: bool       // 赤ドラかどうか
├── OriginalId: int       // 天鳳形式の元ID（0-135）
├── DisplayName: string   // 表示用文字列（例: "5mr", "東"）※計算プロパティ
└── TileTypeId: int       // 牌の種類ID（0-33）※計算プロパティ
```

#### TileSuit

| 値 | 説明 |
|---|------|
| `Man` | 萬子 |
| `Pin` | 筒子 |
| `Sou` | 索子 |
| `Honor` | 字牌 |

#### HonorType

| 値 | 数値 | 説明 |
|---|-----|------|
| `East` | 1 | 東 |
| `South` | 2 | 南 |
| `West` | 3 | 西 |
| `North` | 4 | 北 |
| `White` | 5 | 白 |
| `Green` | 6 | 發 |
| `Red` | 7 | 中 |

### MeldInfo（鳴き情報）

```
MeldInfo
├── Type: MeldType        // 鳴きの種類
├── Tiles: List<Tile>     // 構成牌のリスト
├── CalledTile: Tile?     // 鳴いた牌（他家から取得した牌）
├── FromPlayer: int       // 鳴き元プレイヤー（相対位置: 1=下家, 2=対面, 3=上家）
└── OriginalCode: int     // 元の鳴きコード（デバッグ用）
```

#### MeldType

| 値 | 説明 |
|---|------|
| `Chi` | チー |
| `Pon` | ポン |
| `DaiMinKan` | 大明槓 |
| `KaKan` | 加槓 |
| `AnKan` | 暗槓 |
| `Nuki` | 北抜き（三麻） |

### MjlogSessionResult（セッション結果）

```
MjlogSessionResult
├── IsAgari: bool                  // 和了による終局かどうか
├── AgariInfos: List<AgariInfo>    // 和了結果（複数=ダブロン・トリロン）
├── RyuukyokuInfo: RyuukyokuInfo?  // 流局結果
└── FinalScores: int[4]            // 終局後の各プレイヤーの得点
```

### AgariInfo（和了情報）

```
AgariInfo
├── WinnerId: int                     // 和了したプレイヤーID
├── LoserId: int                      // 放銃したプレイヤーID
├── IsTsumo: bool                     // ツモ和了かどうか ※計算プロパティ
├── WinningTile: Tile?                // 和了牌
├── Hand: List<Tile>                  // 手牌
├── Melds: List<MeldInfo>             // 副露（鳴き）
├── DoraIndicators: List<Tile>        // ドラ表示牌
├── UraDoraIndicators: List<Tile>     // 裏ドラ表示牌
├── Score: int                        // 得点
├── Fu: int                           // 符
├── Han: int                          // 飜数
├── Yakuman: int                      // 役満倍数
├── Yakus: List<YakuInfo>             // 成立した役のリスト
└── ScoreChanges: Dictionary<int, int>// 点数移動
```

### YakuInfo（役情報）

```
YakuInfo
├── Id: int           // 役ID（天鳳形式）
├── Name: string      // 役名
├── Han: int          // 飜数
└── Yakuman: int      // 役満倍数
```

### RyuukyokuInfo（流局情報）

```
RyuukyokuInfo
├── Type: RyuukyokuType                  // 流局の種類
├── TenpaiPlayerIds: List<int>           // テンパイしているプレイヤーIDリスト
├── ScoreChanges: Dictionary<int, int>   // 点数移動
└── NagashiManganPlayerIds: List<int>    // 流し満貫達成プレイヤーIDリスト
```

#### RyuukyokuType

| 値 | 説明 |
|---|------|
| `Exhaustive` | 通常流局（荒牌平局） |
| `NineTerminals` | 九種九牌 |
| `FourWinds` | 四風連打 |
| `FourKans` | 四槓散了 |
| `FourReach` | 四家立直 |
| `TripleRon` | 三家和了 |
| `NagashiMangan` | 流し満貫 |

---

## 巡目の計算ロジック

巡目（TurnNumber）は麻雀のルールに基づいて自動計算されます：

1. **初期値**: 1巡目から開始
2. **巡目の進行**: 全プレイヤー（4人麻雀なら4人、三麻なら3人）が打牌を完了し、親が打牌した時点で次の巡へ
3. **鳴きによるリセット**: チー・ポン・大明槓が発生すると、巡の途中でも打牌カウントがリセットされます（順番がスキップされるため）
4. **暗槓・加槓・北抜き**: 自分のターン内での行動のため、巡のカウントには影響しません

---

## ライセンス

zlib License - Copyright (c) 2025 Sinoa
