# MjlogJ ライブラリ

天鳳牌譜（mjlog）を読み込み、構造化されたモデルに変換するための .NET ライブラリです。

## 目次

- [概要](#概要)
- [クイックスタート](#クイックスタート)
- [API リファレンス](#api-リファレンス)
- [モデル構造](#モデル構造)
- [JSON 出力構造](#json-出力構造)

---

## 概要

MjlogJ は天鳳の牌譜ファイル（.mjlog / XML）を読み込み、C# オブジェクトに変換します。
読み込んだデータは JSON 形式で出力することも可能です。

## クイックスタート

```csharp
using MjlogJ;
using MjlogJ.Formatters;

// 単一ファイルを読み込む
GameRecord record = MjlogReader.Load("path/to/file.mjlog");

// JSON形式で出力
var formatter = new JsonOutputFormatter();
string json = formatter.FormatToString(record);
```

---

## API リファレンス

### MjlogReader クラス

牌譜ファイルを読み込むためのメインAPI（静的クラス）

#### 単一ファイル読み込み（同期）

| メソッド | 説明 |
|---------|------|
| `Load(string path)` | ファイルから牌譜を読み込み（GZip自動判定） |
| `LoadFromGzip(string path)` | GZip圧縮ファイルから読み込み |
| `Parse(string xmlContent)` | XML文字列から牌譜をパース |
| `Parse(Stream stream)` | ストリームから読み込み |
| `LoadWithValidation(string path)` | バリデーション付きで読み込み |

```csharp
// 基本的な使用方法
GameRecord record = MjlogReader.Load("game.mjlog");

// XML文字列からパース
GameRecord record = MjlogReader.Parse(xmlString);

// バリデーション付き
var (record, validation) = MjlogReader.LoadWithValidation("game.mjlog");
if (!validation.IsValid) {
    foreach (var error in validation.Errors) {
        Console.WriteLine(error);
    }
}
```

#### 単一ファイル読み込み（非同期）

| メソッド | 説明 |
|---------|------|
| `LoadAsync(string path, CancellationToken)` | 非同期でファイルから読み込み |
| `LoadFromGzipAsync(string path, CancellationToken)` | 非同期でGZipから読み込み |
| `ParseAsync(Stream stream, CancellationToken)` | 非同期でストリームから読み込み |
| `LoadWithValidationAsync(string path, CancellationToken)` | 非同期でバリデーション付き読み込み |

```csharp
// 非同期読み込み
GameRecord record = await MjlogReader.LoadAsync("game.mjlog");

// キャンセルトークン付き
var cts = new CancellationTokenSource();
GameRecord record = await MjlogReader.LoadAsync("game.mjlog", cts.Token);
```

#### 複数ファイル読み込み（順序保証）

| メソッド | 説明 |
|---------|------|
| `LoadManyAsync(string directoryPath, MjlogReaderOptions?, CancellationToken)` | ディレクトリから逐次読み込み |
| `LoadManyAsync(IEnumerable<string> paths, MjlogReaderOptions?, CancellationToken)` | パスリストから逐次読み込み |
| `LoadManyWithResultAsync(IEnumerable<string> paths, MjlogReaderOptions?, CancellationToken)` | エラー情報付きで逐次読み込み |

```csharp
// ディレクトリから読み込み
await foreach (var record in MjlogReader.LoadManyAsync("./logs/"))
{
    ProcessRecord(record);
}

// エラー情報付き
await foreach (var result in MjlogReader.LoadManyWithResultAsync(filePaths))
{
    if (result.IsSuccess) {
        ProcessRecord(result.Record!);
    } else {
        Console.WriteLine($"Error: {result.FilePath} - {result.Error?.Message}");
    }
}
```

#### 複数ファイル読み込み（並列・高速）

| メソッド | 説明 |
|---------|------|
| `LoadManyParallelAsync(IEnumerable<string> paths, MjlogReaderOptions?, CancellationToken)` | 並列で高速読み込み（順序非保証） |
| `LoadManyParallelAsync(string directoryPath, MjlogReaderOptions?, CancellationToken)` | ディレクトリから並列読み込み |

```csharp
// 並列読み込み（高速だが順序非保証）
var options = new MjlogReaderOptions
{
    MaxDegreeOfParallelism = 8,
    ContinueOnError = true
};

await foreach (var result in MjlogReader.LoadManyParallelAsync(directory, options))
{
    if (result.IsSuccess) {
        ProcessRecord(result.Record!);
    }
}
```

### MjlogReaderOptions クラス

複数ファイル読み込み時のオプション設定

| プロパティ | 型 | デフォルト | 説明 |
|-----------|-----|---------|------|
| `MaxDegreeOfParallelism` | `int` | `Environment.ProcessorCount` | 並列処理の最大並列度 |
| `ContinueOnError` | `bool` | `true` | エラー発生時に処理を続行するか |
| `EnableValidation` | `bool` | `false` | バリデーションを実行するか |
| `AutoDetectGzip` | `bool` | `true` | GZip圧縮を自動検出するか |
| `CancellationToken` | `CancellationToken` | `default` | キャンセルトークン |
| `SearchPattern` | `string` | `"*.xml"` | ファイル検索パターン |
| `IncludeSubdirectories` | `bool` | `true` | サブディレクトリも検索するか |

### LoadResult クラス

複数ファイル読み込み時の結果

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `FilePath` | `string` | ファイルパス |
| `Record` | `GameRecord?` | 読み込んだGameRecord（成功時） |
| `IsSuccess` | `bool` | 読み込みが成功したか |
| `Error` | `Exception?` | エラー情報（失敗時） |
| `Validation` | `ValidationResult?` | バリデーション結果（オプション有効時） |

### IOutputFormatter インターフェース

出力フォーマッタのインターフェース

| メソッド | 説明 |
|---------|------|
| `Format(GameRecord record, Stream output)` | GameRecordをストリームに出力 |
| `FormatToString(GameRecord record)` | GameRecordを文字列として出力 |
| `LoadFromString(string content)` | 文字列からGameRecordを読み込み |
| `LoadFromStream(Stream input)` | ストリームからGameRecordを読み込み |

### JsonOutputFormatter クラス

JSON形式で出力するフォーマッタ（`IOutputFormatter` 実装）

```csharp
var formatter = new JsonOutputFormatter();

// GameRecord → JSON
string json = formatter.FormatToString(record);

// JSON → GameRecord
GameRecord record = formatter.LoadFromString(jsonString);

// ストリームへ出力
using var stream = File.Create("output.json");
formatter.Format(record, stream);
```

---

## モデル構造

### GameRecord（試合全体の記録）

```
GameRecord
├── GameId: string              // 牌譜ID
├── PlayedAt: DateTime          // 対戦日時
├── PlayerNames: string[4]      // プレイヤー名（席順）
├── PlayerDans: string[4]       // プレイヤーの段位
├── PlayerRates: float[4]       // プレイヤーのレート
├── Rule: GameRule?             // ゲームルール
├── Rounds: List<RoundRecord>   // 局のリスト
├── Result: GameResult?         // 最終結果
├── ShuffleSeed: string?        // シャッフル情報
└── Reference: string?          // リファレンス情報
```

### GameRule（ゲームルール情報）

```
GameRule
├── HasRedDora: bool       // 赤ドラの有無
├── HasOpenTanyao: bool    // 喰いタンの有無
├── IsEastOnly: bool       // 東風戦かどうか（falseなら半荘戦）
├── IsThreePlayer: bool    // 三人麻雀かどうか
├── Speed: int             // 速度（0=普通, 1=高速, 2=超高速）
├── HasKuikae: bool        // 喰い替えの有無
├── OriginalFlags: int     // ルールのビットフラグ（元データ）
└── Lobby: int             // ロビー番号
```

### RoundRecord（局の記録）

```
RoundRecord
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
├── Actions: List<PlayerAction>  // 行動履歴
├── Result: RoundResult?         // 局の結果
└── ComputedData: RoundComputedData?  // 計算による追加情報
```

### Tile（麻雀牌）

```
Tile (record)
├── Suit: TileSuit        // 牌の種別
├── Number: int           // 数字（数牌:1-9、字牌:1-7）
├── IsRedDora: bool       // 赤ドラかどうか
├── OriginalId: int       // 天鳳形式の元ID（0-135）
├── DisplayName: string   // 表示用文字列（例: "5mr", "東"）※計算プロパティ
└── TileTypeId: int       // 牌の種類ID（同一牌判定用）※計算プロパティ
```

#### TileSuit（牌の種別）

| 値 | 説明 |
|---|------|
| `Man` | 萬子 |
| `Pin` | 筒子 |
| `Sou` | 索子 |
| `Honor` | 字牌 |

#### HonorType（字牌の種類）

| 値 | 数値 | 説明 |
|---|-----|------|
| `East` | 1 | 東 |
| `South` | 2 | 南 |
| `West` | 3 | 西 |
| `North` | 4 | 北 |
| `White` | 5 | 白 |
| `Green` | 6 | 發 |
| `Red` | 7 | 中 |

### PlayerAction（プレイヤー行動）

抽象基底クラスと派生クラス

```
PlayerAction (abstract)
├── Type: ActionType      // 行動の種類
├── PlayerId: int         // プレイヤーID（0-3）
└── Sequence: int         // 行動のシーケンス番号

DrawAction : PlayerAction
└── Tile: Tile?           // ツモった牌

DiscardAction : PlayerAction
├── Tile: Tile?           // 捨てた牌
├── IsTsumogiri: bool     // ツモ切りかどうか
├── HandAfterDiscard: List<Tile>     // 打牌後の手牌
└── MeldsAfterDiscard: List<MeldInfo>// 打牌後の副露

MeldAction : PlayerAction
└── Meld: MeldInfo?       // 鳴きの詳細情報

ReachAction : PlayerAction
└── Step: int             // リーチステップ（1=宣言、2=成立）

NewDoraAction : PlayerAction
└── DoraTile: Tile?       // 新ドラ表示牌
```

#### ActionType

| 値 | 説明 |
|---|------|
| `Draw` | ツモ（牌を引く） |
| `Discard` | 打牌（牌を捨てる） |
| `Meld` | 鳴き |
| `Reach` | リーチ宣言 |
| `ReachAccepted` | リーチ成立（供託） |
| `NewDora` | 新ドラ表示 |

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

### RoundResult（局の結果）

```
RoundResult
├── IsAgari: bool                  // 和了による終局かどうか
├── AgariResults: List<AgariResult>// 和了結果（複数の場合はダブロン・トリロン）
├── DrawResult: DrawResult?        // 流局結果
└── FinalScores: int[4]            // 終局後の各プレイヤーの得点
```

### AgariResult（和了結果）

```
AgariResult
├── WinnerId: int                     // 和了したプレイヤーID
├── LoserId: int                      // 放銃したプレイヤーID（ツモの場合は-1）
├── IsTsumo: bool                     // ツモ和了かどうか
├── WinningTile: Tile?                // 和了牌
├── Hand: List<Tile>                  // 手牌
├── Melds: List<MeldInfo>             // 副露（鳴き）
├── DoraIndicators: List<Tile>        // ドラ表示牌
├── UraDoraIndicators: List<Tile>     // 裏ドラ表示牌
├── Score: int                        // 得点
├── Fu: int                           // 符
├── Han: int                          // 飜数
├── Yakuman: int                      // 役満倍数（1=役満, 2=ダブル役満...）
├── Yakus: List<YakuInfo>             // 成立した役のリスト
└── ScoreChanges: Dictionary<int, int>// 点数移動（プレイヤーID -> 移動点）
```

### YakuInfo（役情報）

```
YakuInfo
├── Id: int           // 役ID（天鳳形式）
├── Name: string      // 役名
├── Han: int          // 飜数（役満の場合は0）
└── Yakuman: int      // 役満倍数（役満でない場合は0）
```

### DrawResult（流局結果）

```
DrawResult
├── Type: DrawType                       // 流局の種類
├── TenpaiPlayerIds: List<int>           // テンパイしているプレイヤーIDリスト
├── ScoreChanges: Dictionary<int, int>   // 点数移動
└── NagashiManganPlayerIds: List<int>    // 流し満貫達成プレイヤーIDリスト
```

#### DrawType

| 値 | 説明 |
|---|------|
| `Exhaustive` | 通常流局（荒牌平局） |
| `NineTerminals` | 九種九牌 |
| `FourWinds` | 四風連打 |
| `FourKans` | 四槓散了 |
| `FourReach` | 四家立直 |
| `TripleRon` | 三家和了 |
| `NagashiMangan` | 流し満貫 |

### GameResult（ゲーム最終結果）

```
GameResult
└── PlayerResults: List<PlayerResult>
```

### PlayerResult（プレイヤー最終結果）

```
PlayerResult
├── PlayerId: int      // プレイヤーID（0-3）
├── Name: string       // プレイヤー名
├── FinalScore: int    // 最終得点
├── Rank: int          // 順位（1-4）
├── Dan: string        // 段位
├── Rate: float        // レート
└── Sex: string        // 性別（不明な場合は空）
```

### RoundComputedData（局の計算追加情報）

```
RoundComputedData
├── ReachWaitingTiles: Dictionary<int, WaitingTilesInfo>  // リーチ時の待ち牌情報
└── TenpaiWaitingTiles: Dictionary<int, WaitingTilesInfo> // テンパイ時の待ち牌情報
```

### WaitingTilesInfo（待ち牌情報）

```
WaitingTilesInfo
├── PlayerId: int                // プレイヤーID
├── WaitingTiles: List<Tile>     // 待ち牌のリスト
├── WaitingKinds: int            // 待ち牌の種類数 ※計算プロパティ
├── RemainingCount: int          // 待ち牌の残り枚数
├── AtSequence: int              // 計算時点のシーケンス番号
├── HandAtCalculation: List<Tile>      // 計算時点の手牌
└── MeldsAtCalculation: List<MeldInfo> // 計算時点の副露
```

---

## JSON 出力構造

`JsonOutputFormatter` を使用して出力されるJSON構造の例：

```json
{
  "gameId": "2024010112gm-0029-0000-12345678",
  "playedAt": "2024-01-01T12:34:56",
  "playerNames": ["Player1", "Player2", "Player3", "Player4"],
  "playerDans": ["初段", "二段", "三段", "四段"],
  "playerRates": [1500.0, 1600.0, 1700.0, 1800.0],
  "rule": {
    "hasRedDora": true,
    "hasOpenTanyao": true,
    "isEastOnly": false,
    "isThreePlayer": false,
    "speed": 0,
    "hasKuikae": false,
    "originalFlags": 9,
    "lobby": 0
  },
  "rounds": [
    {
      "roundWind": 0,
      "roundNumber": 0,
      "honba": 0,
      "kyotaku": 0,
      "dealerId": 0,
      "startScores": [25000, 25000, 25000, 25000],
      "initialHands": [
        [
          {"suit": "man", "number": 1, "isRedDora": false, "originalId": 0, "displayName": "1m", "tileTypeId": 0},
          {"suit": "man", "number": 2, "isRedDora": false, "originalId": 8, "displayName": "2m", "tileTypeId": 1}
        ],
        [],
        [],
        []
      ],
      "initialDoraIndicator": {"suit": "pin", "number": 5, "isRedDora": false, "originalId": 52, "displayName": "5p", "tileTypeId": 13},
      "doraIndicators": [
        {"suit": "pin", "number": 5, "isRedDora": false, "originalId": 52, "displayName": "5p", "tileTypeId": 13}
      ],
      "uraDoraIndicators": [],
      "actions": [
        {
          "type": "draw",
          "playerId": 0,
          "sequence": 0,
          "tile": {"suit": "sou", "number": 3, "isRedDora": false, "originalId": 80, "displayName": "3s", "tileTypeId": 20}
        },
        {
          "type": "discard",
          "playerId": 0,
          "sequence": 1,
          "tile": {"suit": "sou", "number": 3, "isRedDora": false, "originalId": 80, "displayName": "3s", "tileTypeId": 20},
          "isTsumogiri": true,
          "handAfterDiscard": [],
          "meldsAfterDiscard": []
        },
        {
          "type": "meld",
          "playerId": 1,
          "sequence": 2,
          "meld": {
            "type": "pon",
            "tiles": [
              {"suit": "man", "number": 5, "isRedDora": false, "originalId": 16, "displayName": "5m", "tileTypeId": 4},
              {"suit": "man", "number": 5, "isRedDora": true, "originalId": 17, "displayName": "5mr", "tileTypeId": 4},
              {"suit": "man", "number": 5, "isRedDora": false, "originalId": 18, "displayName": "5m", "tileTypeId": 4}
            ],
            "calledTile": {"suit": "man", "number": 5, "isRedDora": false, "originalId": 16, "displayName": "5m", "tileTypeId": 4},
            "fromPlayer": 3,
            "originalCode": 12345
          }
        },
        {
          "type": "reach",
          "playerId": 0,
          "sequence": 10,
          "step": 1
        }
      ],
      "result": {
        "isAgari": true,
        "agariResults": [
          {
            "winnerId": 0,
            "loserId": 2,
            "isTsumo": false,
            "winningTile": {"suit": "man", "number": 1, "isRedDora": false, "originalId": 0, "displayName": "1m", "tileTypeId": 0},
            "hand": [],
            "melds": [],
            "doraIndicators": [],
            "uraDoraIndicators": [],
            "score": 8000,
            "fu": 40,
            "han": 3,
            "yakuman": 0,
            "yakus": [
              {"id": 1, "name": "立直", "han": 1, "yakuman": 0},
              {"id": 2, "name": "門前清自摸和", "han": 0, "yakuman": 0},
              {"id": 7, "name": "タンヤオ", "han": 1, "yakuman": 0},
              {"id": 8, "name": "平和", "han": 1, "yakuman": 0}
            ],
            "scoreChanges": {
              "0": 8000,
              "2": -8000
            }
          }
        ],
        "drawResult": null,
        "finalScores": [33000, 25000, 17000, 25000]
      },
      "computedData": {
        "reachWaitingTiles": {
          "0": {
            "playerId": 0,
            "waitingTiles": [
              {"suit": "man", "number": 1, "isRedDora": false, "originalId": 0, "displayName": "1m", "tileTypeId": 0}
            ],
            "remainingCount": 3,
            "atSequence": 10,
            "handAtCalculation": [],
            "meldsAtCalculation": []
          }
        },
        "tenpaiWaitingTiles": {}
      }
    }
  ],
  "result": {
    "playerResults": [
      {"playerId": 0, "name": "Player1", "finalScore": 45000, "rank": 1, "dan": "初段", "rate": 1500.0, "sex": ""},
      {"playerId": 1, "name": "Player2", "finalScore": 30000, "rank": 2, "dan": "二段", "rate": 1600.0, "sex": ""},
      {"playerId": 2, "name": "Player3", "finalScore": 15000, "rank": 3, "dan": "三段", "rate": 1700.0, "sex": ""},
      {"playerId": 3, "name": "Player4", "finalScore": 10000, "rank": 4, "dan": "四段", "rate": 1800.0, "sex": ""}
    ]
  },
  "shuffleSeed": null,
  "reference": null
}
```

### JSON フォーマットの特徴

- **プロパティ名**: camelCase で出力
- **Enum値**: 文字列（camelCase）で出力
- **Null値**: 出力時に省略
- **日本語**: エスケープなしで出力

---

## ライセンス

zlib License - Copyright (c) 2025 Sinoa

## 関連プロジェクト

- [MjlogConverter](../MjlogConverter/README.md) - 牌譜→JSON変換ツール
- [MjlogA](../MjlogA/README.md) - 牌譜データ分析ライブラリ
- [MjlogAnalyzer](../MjlogAnalyzer/README.md) - 牌譜データ分析ツール
- [TileEmbedder](../TileEmbedder/README.md) - 牌埋め込みベクトル生成ライブラリ

