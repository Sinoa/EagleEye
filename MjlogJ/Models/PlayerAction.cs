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

namespace MjlogJ.Models;

/// <summary>
/// プレイヤー行動の種類
/// </summary>
public enum ActionType
{
    /// <summary>ツモ（牌を引く）</summary>
    Draw,
    /// <summary>打牌（牌を捨てる）</summary>
    Discard,
    /// <summary>鳴き</summary>
    Meld,
    /// <summary>リーチ宣言</summary>
    Reach,
    /// <summary>リーチ成立（供託）</summary>
    ReachAccepted,
    /// <summary>新ドラ表示</summary>
    NewDora
}

/// <summary>
/// プレイヤー行動の基底クラス
/// </summary>
public abstract class PlayerAction
{
    /// <summary>行動の種類</summary>
    public abstract ActionType Type { get; }

    /// <summary>プレイヤーID（0-3）</summary>
    public int PlayerId { get; set; }

    /// <summary>行動のシーケンス番号</summary>
    public int Sequence { get; set; }
}

/// <summary>
/// ツモ（牌を引く）行動
/// </summary>
public class DrawAction : PlayerAction
{
    public override ActionType Type => ActionType.Draw;

    /// <summary>ツモった牌</summary>
    public Tile? Tile { get; set; }
}

/// <summary>
/// 打牌行動
/// </summary>
public class DiscardAction : PlayerAction
{
    public override ActionType Type => ActionType.Discard;

    /// <summary>捨てた牌</summary>
    public Tile? Tile { get; set; }

    /// <summary>ツモ切りかどうか</summary>
    public bool IsTsumogiri { get; set; }

    /// <summary>打牌後の手牌（この行動完了時点での手牌）</summary>
    public List<Tile> HandAfterDiscard { get; set; } = [];

    /// <summary>打牌後の副露（この行動完了時点での副露）</summary>
    public List<MeldInfo> MeldsAfterDiscard { get; set; } = [];
}

/// <summary>
/// 鳴き行動
/// </summary>
public class MeldAction : PlayerAction
{
    public override ActionType Type => ActionType.Meld;

    /// <summary>鳴きの詳細情報</summary>
    public MeldInfo? Meld { get; set; }
}

/// <summary>
/// リーチ宣言行動
/// </summary>
public class ReachAction : PlayerAction
{
    public override ActionType Type => ActionType.Reach;

    /// <summary>リーチステップ（1=宣言、2=成立）</summary>
    public int Step { get; set; }
}

/// <summary>
/// 新ドラ表示行動（カン後のドラめくり）
/// </summary>
public class NewDoraAction : PlayerAction
{
    public override ActionType Type => ActionType.NewDora;

    /// <summary>新ドラ表示牌</summary>
    public Tile? DoraTile { get; set; }
}