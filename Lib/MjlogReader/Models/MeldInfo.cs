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

namespace MjlogReader.Models;

/// <summary>
/// 鳴きの種類
/// </summary>
public enum MeldType
{
    /// <summary>チー</summary>
    Chi,

    /// <summary>ポン</summary>
    Pon,

    /// <summary>大明槓</summary>
    DaiMinKan,

    /// <summary>加槓</summary>
    KaKan,

    /// <summary>暗槓</summary>
    AnKan,

    /// <summary>北抜き（三麻）</summary>
    Nuki
}

/// <summary>
/// 鳴き情報
/// </summary>
public class MeldInfo
{
    /// <summary>鳴きの種類</summary>
    public MeldType Type { get; set; }

    /// <summary>構成牌のリスト</summary>
    public List<Tile> Tiles { get; set; } = [];

    /// <summary>鳴いた牌（他家から取得した牌）</summary>
    public Tile? CalledTile { get; set; }

    /// <summary>鳴き元プレイヤー（0-3、自分から見た相対位置: 1=下家, 2=対面, 3=上家）</summary>
    public int FromPlayer { get; set; }

    /// <summary>元の鳴きコード（デバッグ用）</summary>
    public int OriginalCode { get; set; }

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    public override string ToString()
    {
        var tilesStr = string.Join("", Tiles.Select(t => t.DisplayName));
        return $"{Type}[{tilesStr}]";
    }
}