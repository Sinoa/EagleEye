// zlib License
// 
// Copyright (c) 2025 Sinoa
// 
// This software is provided 'as-is', without any express or implied
// warranty. In no event will the authors be held liable for any damages
// arising from the use of this software.
// 
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 
// 1. The origin of this software must not be misrepresented; you must not
// claim that you wrote the original software. If you use this software
// in a product, an acknowledgment in the product documentation would be
// appreciated but is not required.
// 
// 2. Altered source versions must be plainly marked as such, and must not be
// misrepresented as being the original software.
// 
// 3. This notice may not be removed or altered from any source
// distribution.

using MjlogJ.Models;

namespace TileEmbedder;

/// <summary>
/// MjlogJの牌譜から共起データを抽出するクラス
/// </summary>
public class MjlogCooccurrenceExtractor
{
    /// <summary>
    /// MjlogJのTileをTileTokenIdに変換
    /// </summary>
    /// <param name="tile">MjlogJのTile</param>
    /// <returns>TileTokenId</returns>
    public static TileTokenId ConvertToTokenId(Tile tile)
    {
        return tile.Suit switch
        {
            TileSuit.Man => tile.IsRedDora && tile.Number == 5
                ? TileTokenId.RedMan5
                : (TileTokenId)(tile.Number), // 1萬=1, 2萬=2, ... 9萬=9

            TileSuit.Pin => tile.IsRedDora && tile.Number == 5
                ? TileTokenId.RedPin5
                : (TileTokenId)(10 + tile.Number), // 1筒=11, 2筒=12, ... 9筒=19

            TileSuit.Sou => tile.IsRedDora && tile.Number == 5
                ? TileTokenId.RedSou5
                : (TileTokenId)(20 + tile.Number), // 1索=21, 2索=22, ... 9索=29

            TileSuit.Honor => (HonorType)tile.Number switch
            {
                HonorType.East => TileTokenId.East,
                HonorType.South => TileTokenId.South,
                HonorType.West => TileTokenId.West,
                HonorType.North => TileTokenId.North,
                HonorType.White => TileTokenId.White,
                HonorType.Green => TileTokenId.Green,
                HonorType.Red => TileTokenId.Red,
                _ => throw new ArgumentException($"Unknown honor type: {tile.Number}")
            },

            _ => throw new ArgumentException($"Unknown tile suit: {tile.Suit}")
        };
    }

    /// <summary>
    /// 和了結果から全ての牌を取得（手牌 + 副露 + 和了牌）
    /// </summary>
    /// <param name="agari">和了結果</param>
    /// <returns>全牌のトークンIDリスト</returns>
    public static List<TileTokenId> ExtractAllTiles(AgariResult agari)
    {
        var tiles = new List<TileTokenId>();

        // 手牌を追加
        foreach (var tile in agari.Hand)
        {
            tiles.Add(ConvertToTokenId(tile));
        }

        // 副露の牌を追加
        foreach (var meld in agari.Melds)
        {
            foreach (var tile in meld.Tiles)
            {
                tiles.Add(ConvertToTokenId(tile));
            }
        }

        // 和了牌を追加
        if (agari.WinningTile != null)
        {
            tiles.Add(ConvertToTokenId(agari.WinningTile));
        }

        return tiles;
    }

    /// <summary>
    /// 和了結果から共起ペアを抽出し、共起行列に加算
    /// </summary>
    /// <param name="agari">和了結果</param>
    /// <param name="matrix">共起行列</param>
    public static void ExtractCooccurrences(AgariResult agari, CooccurrenceMatrix matrix)
    {
        var tiles = ExtractAllTiles(agari);

        // 全ペアの共起を加算
        for (int i = 0; i < tiles.Count; i++)
        {
            for (int j = i + 1; j < tiles.Count; j++)
            {
                matrix.AddCooccurrence(tiles[i], tiles[j]);
            }
        }
    }

    /// <summary>
    /// GameRecordから全ての和了結果を抽出し、共起行列に加算
    /// </summary>
    /// <param name="record">ゲーム記録</param>
    /// <param name="matrix">共起行列</param>
    public static void ExtractFromGameRecord(GameRecord record, CooccurrenceMatrix matrix)
    {
        foreach (var round in record.Rounds)
        {
            if (round.Result?.IsAgari == true)
            {
                foreach (var agari in round.Result.AgariResults)
                {
                    ExtractCooccurrences(agari, matrix);
                }
            }
        }
    }

    /// <summary>
    /// 複数のGameRecordから共起を抽出
    /// </summary>
    /// <param name="records">ゲーム記録のシーケンス</param>
    /// <param name="matrix">共起行列</param>
    /// <param name="progress">進捗報告（オプション）</param>
    public static void ExtractFromGameRecords(
        IEnumerable<GameRecord> records,
        CooccurrenceMatrix matrix,
        IProgress<int>? progress = null)
    {
        int count = 0;
        foreach (var record in records)
        {
            ExtractFromGameRecord(record, matrix);
            count++;
            progress?.Report(count);
        }
    }

    /// <summary>
    /// 非同期で複数のGameRecordから共起を抽出
    /// </summary>
    /// <param name="records">ゲーム記録の非同期シーケンス</param>
    /// <param name="matrix">共起行列</param>
    /// <param name="progress">進捗報告（オプション）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public static async Task ExtractFromGameRecordsAsync(
        IAsyncEnumerable<GameRecord> records,
        CooccurrenceMatrix matrix,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        int count = 0;
        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            ExtractFromGameRecord(record, matrix);
            count++;
            progress?.Report(count);
        }
    }
}