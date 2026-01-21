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

using Foxtamp.MjlogReader.Models;

namespace Foxtamp.MjlogReplayer.Models;

/// <summary>
/// 試合状態を表す抽象基底レコード
/// </summary>
/// <param name="RoundWind">場風（0=東, 1=南, 2=西, 3=北）</param>
/// <param name="RoundNumber">局番号（0-3: 東1-4局、4-7: 南1-4局...）</param>
/// <param name="Honba">本場数</param>
/// <param name="Kyotaku">供託リーチ棒の数</param>
/// <param name="DealerId">親プレイヤーID（0-3）</param>
/// <param name="TurnNumber">巡目（1から始まる）</param>
/// <param name="StepIndex">ステップインデックス（0から始まる）</param>
/// <param name="Players">各プレイヤーの状態</param>
/// <param name="DoraIndicators">ドラ表示牌</param>
/// <param name="SourceStep">この状態になった起因となるステップ（初期状態の場合はnull）</param>
public abstract record GameState(
    int RoundWind,
    int RoundNumber,
    int Honba,
    int Kyotaku,
    int DealerId,
    int TurnNumber,
    int StepIndex,
    IReadOnlyList<PlayerState> Players,
    IReadOnlyList<Tile> DoraIndicators,
    MjlogStep? SourceStep)
{
    /// <summary>
    /// プレイヤー人数
    /// </summary>
    public abstract int PlayerCount { get; }

    /// <summary>
    /// 局の表示名（例: "東1局"）
    /// </summary>
    public string RoundName => $"{RoundWindName}{RoundNumber % 4 + 1}局";

    private string RoundWindName => (RoundNumber / 4) switch
    {
        0 => "東",
        1 => "南",
        2 => "西",
        3 => "北",
        _ => "?"
    };

    /// <summary>
    /// 指定プレイヤーの状態を取得
    /// </summary>
    /// <param name="playerId">プレイヤーID</param>
    /// <returns>プレイヤー状態</returns>
    public PlayerState GetPlayer(int playerId)
    {
        if (playerId < 0 || playerId >= PlayerCount)
        {
            throw new ArgumentOutOfRangeException(nameof(playerId), $"プレイヤーIDは0から{PlayerCount - 1}の範囲で指定してください");
        }

        return Players[playerId];
    }

    /// <summary>
    /// プレイヤー状態を更新した新しいGameStateを作成
    /// </summary>
    /// <param name="playerId">更新するプレイヤーID</param>
    /// <param name="newPlayerState">新しいプレイヤー状態</param>
    /// <returns>更新されたGameState</returns>
    public abstract GameState WithPlayer(int playerId, PlayerState newPlayerState);

    /// <summary>
    /// 巡目を更新した新しいGameStateを作成
    /// </summary>
    /// <param name="newTurnNumber">新しい巡目</param>
    /// <returns>更新されたGameState</returns>
    public abstract GameState WithTurnNumber(int newTurnNumber);

    /// <summary>
    /// ステップインデックスを更新した新しいGameStateを作成
    /// </summary>
    /// <param name="newStepIndex">新しいステップインデックス</param>
    /// <returns>更新されたGameState</returns>
    public abstract GameState WithStepIndex(int newStepIndex);

    /// <summary>
    /// ドラ表示牌を追加した新しいGameStateを作成
    /// </summary>
    /// <param name="doraIndicator">追加するドラ表示牌</param>
    /// <returns>更新されたGameState</returns>
    public abstract GameState AddDoraIndicator(Tile doraIndicator);

    /// <summary>
    /// 供託を更新した新しいGameStateを作成
    /// </summary>
    /// <param name="newKyotaku">新しい供託数</param>
    /// <returns>更新されたGameState</returns>
    public abstract GameState WithKyotaku(int newKyotaku);

    /// <summary>
    /// 起因ステップを更新した新しいGameStateを作成
    /// </summary>
    /// <param name="step">起因となるステップ</param>
    /// <returns>更新されたGameState</returns>
    public abstract GameState WithSourceStep(MjlogStep? step);

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    public override string ToString()
    {
        var doraStr = string.Join("", DoraIndicators.Select(t => t.DisplayName));
        return $"{RoundName} {Honba}本場 供託{Kyotaku} 巡目{TurnNumber} ドラ表示:[{doraStr}]";
    }
}