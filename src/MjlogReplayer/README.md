# MjlogReplayer ライブラリ

麻雀の試合場況を再現するための .NET ライブラリです。MjlogReaderで読み込んだ牌譜データを基に、各ステップ時点の試合状態（GameState）をイミュータブルに構築します。

## 目次

- [概要](#概要)
- [クイックスタート](#クイックスタート)
- [API リファレンス](#api-リファレンス)
- [モデル構造](#モデル構造)

---

## 概要

MjlogReplayer は天鳳の牌譜を読み込んだ後、各ステップ時点の試合状態を再現し、インデックスアクセスで任意の瞬間の場況を取得できるようにします。

### 特徴

- **三麻/四麻対応**: 3人麻雀と4人麻雀の両方をサポート
- **イミュータブル設計**: 各試合状態は不変で、安全にアクセス可能
- **インデックスアクセス**: ステップ番号を指定して任意の瞬間の状態を取得
- **外部入力対応**: mjlog以外からもGameStateBuilderで状態を構築可能
- **捨て牌マーキング**: 鳴かれた牌、リーチ宣言牌、ツモ切りを追跡

---

## クイックスタート

### mjlogから再現

```csharp
using Foxtamp.MjlogReader;
using Foxtamp.MjlogReplayer;

// 牌譜を読み込み
var document = MjlogDocumentReader.Load("game.mjlog");

// ドキュメント全体を再現
var replayer = new DocumentReplayer(document);

// セッション0のステップ10時点の状態を取得
var state = replayer.GetState(sessionIndex: 0, stepIndex: 10);

Console.WriteLine($"局: {state.RoundName}");
Console.WriteLine($"巡目: {state.TurnNumber}");

// プレイヤー0の手牌を表示
var player0 = state.GetPlayer(0);
Console.WriteLine($"手牌: {string.Join("", player0.Hand.Select(t => t.DisplayName))}");
```

### 外部入力から構築

```csharp
using Foxtamp.MjlogReplayer.Builders;
using Foxtamp.MjlogReplayer.Models;

// 4人麻雀の状態を構築
var builder = GameStateBuilder.Create(playerCount: 4)
    .WithRoundNumber(0)  // 東1局
    .WithHonba(0)
    .WithDealerId(0)
    .WithTurnNumber(5);

builder.Player(0)
    .WithHand(tiles)
    .WithScore(25000);

var state = builder.Build();
```

---

## API リファレンス

### DocumentReplayer クラス

MjlogDocument全体を再現するメインクラス

```csharp
// コンストラクタ
var replayer = new DocumentReplayer(document);

// インデクサでセッションリプレイヤーを取得
SessionReplayer session = replayer[0];

// 直接状態を取得
GameState state = replayer.GetState(sessionIndex, stepIndex);

// 全状態を列挙
foreach (var (sessionIdx, stepIdx, state) in replayer.EnumerateAllStates())
{
    // ...
}
```

| プロパティ/メソッド | 説明 |
|-------------------|------|
| `Document` | 元のMjlogDocument |
| `SessionCount` | セッション数 |
| `Sessions` | 全SessionReplayerのリスト |
| `TotalStepCount` | 全ステップ数の合計 |
| `this[int]` | 指定インデックスのSessionReplayerを取得 |
| `GetState(int, int)` | セッション・ステップを指定して状態を取得 |
| `EnumerateAllStates()` | 全状態を列挙 |
| `EnumerateSessionStates(int)` | 指定セッションの全状態を列挙 |

### SessionReplayer クラス

1つのセッション（局）を再現するクラス

```csharp
// コンストラクタ
var replayer = new SessionReplayer(session);

// インデクサで状態を取得
GameState state = replayer[stepIndex];
```

| プロパティ/メソッド | 説明 |
|-------------------|------|
| `Session` | 元のMjlogSession |
| `States` | 全GameStateのリスト |
| `StepCount` | ステップ数 |
| `this[int]` | 指定インデックスのGameStateを取得 |

### GameStateBuilder クラス

外部入力からGameStateを構築するビルダー

```csharp
var builder = GameStateBuilder.Create(playerCount: 4)
    .WithRoundWind(0)
    .WithRoundNumber(0)
    .WithHonba(0)
    .WithKyotaku(0)
    .WithDealerId(0)
    .WithTurnNumber(1)
    .WithStepIndex(0)
    .WithRemainingTileCount(70)
    .AddDoraIndicator(tile);

builder.Player(0)
    .WithHand(tiles)
    .WithDiscards(discards)
    .WithMelds(melds)
    .WithReach(false)
    .WithReachTurnNumber(null)
    .WithScore(25000);

var state = builder.Build();
```

---

## モデル構造

### GameState（試合状態）- 抽象基底

```
GameState (abstract record)
├── RoundWind: int                    // 場風（0=東, 1=南, 2=西, 3=北）
├── RoundNumber: int                  // 局番号
├── RoundName: string                 // 局の表示名（例: "東1局"）※計算プロパティ
├── Honba: int                        // 本場数
├── Kyotaku: int                      // 供託リーチ棒の数
├── DealerId: int                     // 親プレイヤーID
├── TurnNumber: int                   // 巡目
├── StepIndex: int                    // ステップインデックス
├── Players: IReadOnlyList<PlayerState>  // 各プレイヤーの状態
├── DoraIndicators: IReadOnlyList<Tile>  // ドラ表示牌
├── RemainingTileCount: int           // 残り山牌数（ツモ可能な牌の残数）
├── SourceStep: MjlogStep?            // この状態になった起因となるステップ（初期状態の場合はnull）
└── PlayerCount: int                  // プレイヤー人数 ※抽象プロパティ
```

### FourPlayerGameState（4人麻雀）

GameStateを継承。PlayerCount = 4。

### ThreePlayerGameState（3人麻雀）

GameStateを継承。PlayerCount = 3。

```
ThreePlayerGameState : GameState
└── NukiDoras: IReadOnlyList<IReadOnlyList<Tile>>  // 各プレイヤーの抜きドラ（北抜き）
```

### PlayerState（プレイヤー状態）

```
PlayerState (record)
├── PlayerId: int                           // プレイヤーID
├── Hand: IReadOnlyList<Tile>               // 手牌
├── Discards: IReadOnlyList<DiscardedTile>  // 捨て牌
├── Melds: IReadOnlyList<MeldInfo>          // 副露（鳴き）
├── IsReach: bool                           // リーチ状態
├── ReachTurnNumber: int?                   // リーチ宣言時の巡目（未リーチの場合はnull）
└── Score: int                              // 現在の得点
```

### DiscardedTile（捨て牌）

```
DiscardedTile (record)
├── Tile: Tile                 // 捨てた牌
├── IsTsumogiri: bool          // ツモ切りかどうか
├── IsReachDeclare: bool       // リーチ宣言牌かどうか
├── CalledByPlayerId: int?     // 鳴かれた場合の相手ID（nullなら鳴かれていない）
└── IsCalled: bool             // 鳴かれたかどうか ※計算プロパティ
```

---

## 使用例

### 特定巡目の状態を検索

```csharp
var replayer = new DocumentReplayer(document);

// 各セッションの5巡目開始時点を取得
foreach (var session in replayer.Sessions)
{
    var turn5State = session.States.FirstOrDefault(s => s.TurnNumber == 5);
    if (turn5State != null)
    {
        Console.WriteLine($"{turn5State.RoundName}: 5巡目");
        foreach (var player in turn5State.Players)
        {
            Console.WriteLine($"  {player}");
        }
    }
}
```

### リーチ宣言時の状態を取得

```csharp
var replayer = new DocumentReplayer(document);

foreach (var (sessionIdx, stepIdx, state) in replayer.EnumerateAllStates())
{
    foreach (var player in state.Players)
    {
        var reachDiscard = player.Discards.FirstOrDefault(d => d.IsReachDeclare);
        if (reachDiscard != null)
        {
            Console.WriteLine($"Session{sessionIdx} Step{stepIdx}: P{player.PlayerId}がリーチ宣言 ({reachDiscard.Tile.DisplayName})");
        }
    }
}
```

### 3人麻雀の北抜き情報を取得

```csharp
var replayer = new DocumentReplayer(document);

foreach (var session in replayer.Sessions)
{
    var finalState = session.States.Last();
    
    if (finalState is ThreePlayerGameState threeState)
    {
        for (var i = 0; i < 3; i++)
        {
            var nukiCount = threeState.GetNukiDoras(i).Count;
            Console.WriteLine($"P{i}の北抜き: {nukiCount}枚");
        }
    }
}
```

---

## 関連ドキュメント

- [MjlogReader README](../MjlogReader/README.md) - 牌譜読み込みライブラリ
- [MjlogReplayerSample README](../MjlogReplayerSample/README.md) - サンプルアプリケーション

---

## ライセンス

zlib License - Copyright (c) 2025-2026 Sinoa
