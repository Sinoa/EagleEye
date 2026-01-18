// zlib License
// 
// Copyright (c) 2026 Sinoa
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

using MjlogReader.Models;

namespace MjlogReader.Decoders;

/// <summary>
/// 天鳳形式の鳴きコードをデコードするクラス
/// </summary>
/// <remarks>
/// 天鳳の鳴きコード（16bit）の構造:
/// - bit 0-1: 鳴き元（0=自分, 1=下家, 2=対面, 3=上家）
/// - bit 2: チーフラグ（1ならチー）
/// - bit 3: ポンフラグ（1ならポン）
/// - bit 4: 加槓フラグ（1なら加槓）
/// - bit 5: 抜きドラフラグ（三麻用）
/// - 上位ビット: 牌情報（種別によって異なる）
/// </remarks>
public static class MeldDecoder
{
    /// <summary>
    /// 鳴きコードをデコード
    /// </summary>
    /// <param name="code">鳴きコード（16bit）</param>
    /// <param name="playerId">鳴いたプレイヤーID</param>
    /// <returns>MeldInfo</returns>
    public static MeldInfo Decode(int code, int playerId)
    {
        var meld = new MeldInfo
        {
            OriginalCode = code,
            FromPlayer = code & 0x3
        };

        if ((code & 0x4) != 0)
        {
            // チー
            DecodeChii(code, meld);
        }
        else if ((code & 0x8) != 0)
        {
            // ポン
            DecodePon(code, meld);
        }
        else if ((code & 0x10) != 0)
        {
            // 加槓
            DecodeKakan(code, meld);
        }
        else if ((code & 0x20) != 0)
        {
            // 北抜き（三麻）
            DecodeNuki(code, meld);
        }
        else
        {
            // 大明槓または暗槓
            DecodeKan(code, meld);
        }

        return meld;
    }

    private static void DecodeChii(int code, MeldInfo meld)
    {
        meld.Type = MeldType.Chi;

        // チーの構造:
        // bit 0-1: 鳴き元
        // bit 2: チーフラグ (1)
        // bit 3-4: 鳴いた牌の順子内位置 (0=最小, 1=中, 2=最大)
        // bit 5-6: unused
        // bit 7-15: base tile info

        var t = (code >> 10) * 3;
        var r = (code >> 3) & 0x3; // 鳴いた牌の位置

        var baseTypeId = t / 3;
        var suitOffset = baseTypeId / 7 * 9;
        var num = baseTypeId % 7;

        var tiles = new List<Tile>();
        Tile? calledTile = null;

        for (var i = 0; i < 3; i++)
        {
            // 各牌のインデックスを取得（bit 3-4, 5-6, 7-8）
            var tileIdx = (code >> (3 + i * 2)) & 0x3;
            var tileId = (suitOffset + num + i) * 4 + tileIdx;
            var tile = TileDecoder.Decode(tileId);
            tiles.Add(tile);

            if (i == r)
            {
                calledTile = tile;
            }
        }

        meld.Tiles = tiles;
        meld.CalledTile = calledTile;
    }

    private static void DecodePon(int code, MeldInfo meld)
    {
        meld.Type = MeldType.Pon;

        // ポンの構造:
        // bit 0-1: 鳴き元
        // bit 2: 0
        // bit 3: ポンフラグ (1)
        // bit 4: unused
        // bit 5-6: 使用しない牌のインデックス
        // bit 7-8: 鳴いた牌のインデックス
        // bit 9-15: 牌種 * 3

        var t = (code >> 9) * 3;
        var r = (code >> 5) & 0x3; // 使用しない牌
        var calledIdx = (code >> 7) & 0x3; // 鳴いた牌の位置

        var typeId = t / 3;
        var baseId = typeId * 4;

        var tiles = new List<Tile>();
        Tile? calledTile = null;

        var pos = 0;
        for (var i = 0; i < 4; i++)
        {
            if (i == r) continue; // 使用しない牌はスキップ

            var tile = TileDecoder.Decode(baseId + i);
            tiles.Add(tile);

            if (pos == calledIdx)
            {
                calledTile = tile;
            }

            pos++;
        }

        meld.Tiles = tiles;
        meld.CalledTile = calledTile;
    }

    private static void DecodeKakan(int code, MeldInfo meld)
    {
        meld.Type = MeldType.KaKan;

        // 加槓の構造:
        // bit 0-1: 鳴き元（元のポン相手）
        // bit 2: 0
        // bit 3: 0
        // bit 4: 加槓フラグ (1)
        // bit 5-6: 追加した牌のインデックス
        // bit 7-8: unused
        // bit 9-15: 牌種 * 3

        var t = (code >> 9) * 3;
        var addedIdx = (code >> 5) & 0x3;

        var typeId = t / 3;
        var baseId = typeId * 4;

        var tiles = new List<Tile>();
        Tile? calledTile = null;

        for (var i = 0; i < 4; i++)
        {
            var tile = TileDecoder.Decode(baseId + i);
            tiles.Add(tile);

            if (i == addedIdx)
            {
                calledTile = tile; // 追加した牌
            }
        }

        meld.Tiles = tiles;
        meld.CalledTile = calledTile;
    }

    private static void DecodeNuki(int code, MeldInfo meld)
    {
        meld.Type = MeldType.Nuki;

        // 北抜きの構造:
        // bit 0-1: 0 (自分)
        // bit 2-4: 0
        // bit 5: 抜きフラグ (1)
        // bit 6-7: 牌インデックス
        // bit 8-15: unused

        var tileIdx = (code >> 8);
        var tile = TileDecoder.Decode(tileIdx);

        meld.Tiles = [tile];
        meld.CalledTile = tile;
        meld.FromPlayer = 0; // 自分
    }

    private static void DecodeKan(int code, MeldInfo meld)
    {
        // 大明槓または暗槓の構造:
        // bit 0-1: 鳴き元（0=自分=暗槓, 1-3=大明槓）
        // bit 8-15: 牌ID

        var from = code & 0x3;
        meld.Type = from == 0 ? MeldType.AnKan : MeldType.DaiMinKan;

        var tileId = (code >> 8);
        var typeId = tileId / 4;
        var baseId = typeId * 4;

        var tiles = new List<Tile>();
        Tile? calledTile = null;

        for (var i = 0; i < 4; i++)
        {
            var tile = TileDecoder.Decode(baseId + i);
            tiles.Add(tile);

            // 大明槓の場合、鳴いた牌を特定
            if (meld.Type == MeldType.DaiMinKan && i == tileId % 4)
            {
                calledTile = tile;
            }
        }

        meld.Tiles = tiles;
        meld.CalledTile = calledTile;
    }
}