# EagleEye 既知の問題（天鳳ログ関連）

本ドキュメントは、天鳳牌譜の読み込み・状態再現・合法手判定に関する既知の問題を記録したものです。

開発計画は `DEVELOPMENT.md`、設計決定事項は `ARCHITECTURE.md` を参照してください。

---

## 目次

- [0. 背景と扱い](#0-背景と扱い)
- [1. MjlogReader（パーサー）](#1-mjlogreaderパーサー)
- [2. MjlogReplayer（状態再現）](#2-mjlogreplayer状態再現)
- [3. Rules（合法手判定）](#3-rules合法手判定)
- [4. サンプルアプリ・README](#4-サンプルアプリreadme)

---

## 0. 背景と扱い

天鳳の牌譜は現在、機械学習用途で使用できなくなった。そのため、天鳳ログ関連の実装（MjlogReader / MjlogReplayer / MjlogReaderSample / MjlogReplayerSample）は今後のロードマップでメンテナンスされず、将来削除される予定である。本ドキュメントに記載した問題も **修正しない**。本ドキュメントは、削除されるまでの間、問題の存在を記録することを目的とする。

- 調査日: 2026-09-25
- 基準コミット: `7dfbb68`（記載の行番号はすべて基準コミット時点のもの）
- 状態: 全項目 ⛔ 対応予定なし（実装ごと削除予定）

§3 の Rules（`MjlogReplayer/Rules/`）も削除対象のプロジェクトに含まれる。ただし Rules は `GameState` を入力とし、ログ形式には依存しない。削除前に別プロジェクトへ移設して再利用する場合は、修正が必須となる。

### 凡例

| 重要度 | 意味 |
|--------|------|
| 高 | 学習データ・アクションマスクの正しさに直接影響する |
| 中 | 特定条件で誤った結果になる、または利用時に誤用を招く |
| 低 | 表示・保守性・堅牢性の問題 |
| 要検証 | 実牌譜での確認が必要（問題でない可能性あり） |

---

## 1. MjlogReader（パーサー）

| ID | 問題 | 影響 | 該当箇所 | 重要度 |
|----|------|------|----------|--------|
| R-1 | 巡目カウントのずれ。巡目の更新が「親の打牌時かつ打牌数が人数以上」で行われるため、親の2打目以降が子より1巡小さく記録される。さらにチー・ポン・大明槓で打牌数が0にリセットされ、以降の進行が遅れる | `MjlogStep.TurnNumber`、`GameState.TurnNumber`、`PlayerState.ReachTurnNumber`、`MeldInfo.TurnNumber` | `MjlogXmlParser.cs:91-101, 631-634` | 高 |
| R-2 | リーチ宣言牌のツモ切り判定が常に false。天鳳のタグ順は T → REACH(step=1) → D のため、打牌の直前のステップが `ReachAction` になり、ツモ牌との比較が成立しない | `DiscardAction.IsTsumogiri`、`DiscardedTile.IsTsumogiri` | `MjlogXmlParser.cs:439-450` | 高 |
| R-3 | 役満名テーブルのID不一致。`YakumanNames` は0始まりだが、天鳳の `yakuman` 属性は通常役と共通のID（37=天和〜51=四槓子）を使う。`YakuNames` の37〜51も空文字 | `YakuInfo.Name`（役満名が「役満39」等になる） | `MjlogXmlParser.cs:119, 127-134, 781` | 中 |
| R-4 | 赤ドラ判定がルールを参照しない。赤なし卓でもID 16/52/88が赤ドラになる | `Tile.IsRedDora`（Phase 0 の赤ドラフラグで使用予定） | `TileDecoder.cs:63, 71, 79` | 高 |
| R-5 | GZip判定が拡張子のみ（`.mjlog` / `.gz`）。非圧縮の `.mjlog` や拡張子のないgzipを読めない。`Load(Stream)` は非圧縮前提 | `MjlogDocumentReader.Load` | `MjlogDocumentReader.cs:42-46` | 中 |
| R-6 | レートの `float.TryParse` がカルチャ依存。小数点が `,` のカルチャで誤読する | `MjlogHeader.PlayerRates` | `MjlogXmlParser.cs:522` | 低 |
| R-7 | 例外処理が粗い。1要素でも失敗すると `AggregateException` で文書全体が失われ、要素名・位置の情報も付かない。GO要素の `int.Parse` は try の外 | パース失敗時の原因特定 | `MjlogXmlParser.cs:178, 259-269` | 低 |
| R-8 | 未読込・未設定の情報。`owari`（最終順位・ウマ）を読まない。`MjlogSession.UraDoraIndicators` と `RyuukyokuInfo.NagashiManganPlayerIds` は一度も設定されない（裏ドラは `AgariInfo.UraDoraIndicators` のみに入る） | 空のプロパティの誤用 | `MjlogSession.cs:76`、`RyuukyokuInfo.cs:68` | 低 |
| R-9 | 鳴き情報の欠落。`MeldDecoder.Decode` の `playerId` 引数が未使用。加槓で元のポンの鳴き元位置を破棄している | `MeldInfo` | `MeldDecoder.cs:48, 182` | 低 |
| R-10 | 流局時のテンパイ者判定。`hai{i}` 属性の有無でテンパイとするため、九種九牌の宣言者（手牌公開）をテンパイ扱いしている可能性がある | `RyuukyokuInfo.TenpaiPlayerIds` | `MjlogXmlParser.cs:839-847` | 要検証 |

---

## 2. MjlogReplayer（状態再現）

| ID | 問題 | 影響 | 該当箇所 | 重要度 |
|----|------|------|----------|--------|
| P-1 | Replayerがパース結果を書き換える。鳴き処理で `MeldInfo.TurnNumber` に代入している | 同一 `MjlogDocument` の再利用時の副作用 | `SessionReplayer.cs:222` | 中 |
| P-2 | StepIndexの重複。初期状態のStepIndexが0で、`Steps[0]` 適用後の状態も0になる | `GameState.StepIndex`（サンプルの `--step N` 表示が N-1 になる） | `FourPlayerGameState.cs:110`、`ThreePlayerGameState.cs:131` | 中 |
| P-3 | 手牌からの削除失敗を黙殺する。該当牌が見つからなくても何もせず続行するため、状態の不整合を検知できない | 状態再現の正しさの検証 | `PlayerState.cs:191-201`、`ThreePlayerGameState.cs:209-213` | 中 |
| P-4 | 和了・流局の点数移動が未反映。AGARI / RYUUKYOKU で状態を変更しない | 局終了時の `PlayerState.Score` | `SessionReplayer.cs:168-171` | 中 |
| P-5 | `GameStateBuilder.Build` がドラ表示牌リストをコピーせずに渡す（Build後の `AddDoraIndicator` が生成済み状態に波及）。既定点数25000が三麻に非対応 | `GameStateBuilder` 利用時 | `GameStateBuilder.cs:249, 263, 283` | 低 |
| P-6 | イミュータブル性が浅い。`IReadOnlyList` の実体が `List<T>`、`MeldInfo` / `MjlogStep` はミュータブル、record の自動 Equals がリストを参照比較する | スナップショットの同一性判定・不変性の前提 | `Models/*` | 低 |
| P-7 | 加槓時にポンの `MeldInfo` を丸ごと置き換えるため、元のポンの `TurnNumber` / `CalledTile` が失われる | `PlayerState.Melds` | `PlayerState.cs:145-156` | 低 |
| P-8 | 三麻の鳴き元計算 `(playerId + FromPlayer) % PlayerCount` が天鳳のエンコードと一致するか未確認 | 三麻の `DiscardedTile.CalledByPlayerId` | `SessionReplayer.cs:232` | 要検証 |

---

## 3. Rules（合法手判定）

Rules は MjlogReplayer とともに削除予定。ただしログ形式には依存しないため、削除前に別プロジェクトへ移設して再利用する場合は、修正が必須となる。

| ID | 問題 | 影響 | 該当箇所 | 重要度 |
|----|------|------|----------|--------|
| U-1 | 九種九牌が親の第一ツモでしか成立しない。条件が「全員の河が空」のため、子の第一ツモ時点では親の打牌があり常に不成立 | `GameActionType.KyuushuKyuuhai` | `ValidActionGenerator.cs:241-253` | 高 |
| U-2 | リーチ中でもポン・チー・大明槓を合法として返す | 他家打牌時のアクションマスク | `ValidActionGenerator.cs:157-216` | 高 |
| U-3 | 役の有無を判定しない。和了形であれば役なしでもツモ・ロンを合法とする（`AgariChecker` は形のみ判定） | ツモ・ロンのアクションマスク | `ValidActionGenerator.cs:48, 173` | 高 |
| U-4 | 鳴き直後の打牌用APIがない。`GetValidActionsOnDraw` を流用すると枚数チェックを通過し、和了形なら誤ってツモが立つ | 鳴き後のアクションマスク | `ValidActionGenerator.cs:41-51` | 中 |
| U-5 | リーチ中の暗槓を一律禁止している（天鳳では待ちが変わらなければ可能） | `GameActionType.AnKan` | `ValidActionGenerator.cs:84-96` | 中 |
| U-6 | 振聴判定が永続振聴（自分の捨て牌）のみ。同巡内振聴・リーチ後の見逃し振聴に未対応 | ロンのアクションマスク | `FuritenChecker.cs:29-57` | 中 |
| U-7 | その他の未対応ルール: 喰い替え禁止、海底・河底での鳴き・槓の禁止、四槓・嶺上牌残数による槓の制限、槍槓（加槓へのロン）判定API、ロン判定時の手牌枚数チェック | 各アクションマスク | `ValidActionGenerator.cs` | 低 |

---

## 4. サンプルアプリ・README

| ID | 問題 | 影響 | 該当箇所 | 重要度 |
|----|------|------|----------|--------|
| S-1 | ロングオプションが登録されない。System.CommandLine 2.0.0-beta4 の `Option<T>(string, string)` は（名前, 説明）のため、`--xml` / `--verbose` / `--session` が説明文扱いになる | サンプルのCLI引数（README記載と不一致） | `MjlogReaderSample/ApplicationMain.cs:50, 55`、`MjlogReplayerSample/ApplicationMain.cs:49, 54, 59` | 中 |
| S-2 | ファイルの存在確認が読み込み後に行われるため機能しない。エラー時の終了コードが0 | サンプルのエラー処理 | `MjlogReplayerSample/ApplicationMain.cs:117-131` | 低 |
| S-3 | READMEの記載が実装と不一致。削除済みの `PlayedAt` / `GameId`、存在しない `Speed` / `HasKuikae`（実装は `IsFast`）、出力例の牌表示（Unicode牌だが実装は `1m` / `東` 形式） | READMEのサンプルコードがコンパイル不可 | `README.md:107`、`src/MjlogReader/README.md:58, 155-156, 174-175`、`src/MjlogReaderSample/README.md:91-110` | 低 |
