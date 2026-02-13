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
/// 4人麻雀の試合状態を表すレコード
/// </summary>
public record FourPlayerGameState : GameState
{
    /// <inheritdoc/>
    public override int PlayerCount => 4;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public FourPlayerGameState(
        int roundWind,
        int roundNumber,
        int honba,
        int kyotaku,
        int dealerId,
        int turnNumber,
        int stepIndex,
        IReadOnlyList<PlayerState> players,
        IReadOnlyList<Tile> doraIndicators,
        int remainingTileCount,
        MjlogStep? sourceStep)
        : base(roundWind, roundNumber, honba, kyotaku, dealerId, turnNumber, stepIndex, players, doraIndicators, remainingTileCount, sourceStep)
    {
        if (players.Count != 4)
        {
            throw new ArgumentException("4人麻雀では4人のプレイヤー状態が必要です", nameof(players));
        }
    }

    /// <summary>
    /// 初期状態を作成
    /// </summary>
    /// <param name="roundWind">場風</param>
    /// <param name="roundNumber">局番号</param>
    /// <param name="honba">本場</param>
    /// <param name="kyotaku">供託</param>
    /// <param name="dealerId">親ID</param>
    /// <param name="initialHands">各プレイヤーの配牌</param>
    /// <param name="initialScores">各プレイヤーの初期得点</param>
    /// <param name="initialDoraIndicator">初期ドラ表示牌</param>
    /// <returns>初期状態のGameState</returns>
    public static FourPlayerGameState CreateInitial(
        int roundWind,
        int roundNumber,
        int honba,
        int kyotaku,
        int dealerId,
        IReadOnlyList<IReadOnlyList<Tile>> initialHands,
        IReadOnlyList<int> initialScores,
        Tile? initialDoraIndicator)
    {
        if (initialHands.Count != 4)
        {
            throw new ArgumentException("4人麻雀では4人分の配牌が必要です", nameof(initialHands));
        }

        if (initialScores.Count != 4)
        {
            throw new ArgumentException("4人麻雀では4人分の初期得点が必要です", nameof(initialScores));
        }

        var players = new List<PlayerState>();
        for (var i = 0; i < 4; i++)
        {
            players.Add(PlayerState.CreateInitial(i, initialHands[i], initialScores[i]));
        }

        var doraIndicators = initialDoraIndicator != null
            ? new List<Tile> { initialDoraIndicator }
            : new List<Tile>();

        const int initialRemainingTiles = 70; // 136 - 14(王牌) - 52(配牌13×4)

        return new FourPlayerGameState(
            roundWind,
            roundNumber,
            honba,
            kyotaku,
            dealerId,
            1, // 巡目は1から開始
            0, // ステップは0から開始
            players,
            doraIndicators,
            initialRemainingTiles,
            null); // 初期状態なので起因ステップはなし
    }

    /// <inheritdoc/>
    public override GameState WithPlayer(int playerId, PlayerState newPlayerState)
    {
        if (playerId < 0 || playerId >= 4)
        {
            throw new ArgumentOutOfRangeException(nameof(playerId), "プレイヤーIDは0から3の範囲で指定してください");
        }

        var newPlayers = Players.ToList();
        newPlayers[playerId] = newPlayerState;

        return this with { Players = newPlayers };
    }

    /// <inheritdoc/>
    public override GameState WithTurnNumber(int newTurnNumber)
    {
        return this with { TurnNumber = newTurnNumber };
    }

    /// <inheritdoc/>
    public override GameState WithStepIndex(int newStepIndex)
    {
        return this with { StepIndex = newStepIndex };
    }

    /// <inheritdoc/>
    public override GameState AddDoraIndicator(Tile doraIndicator)
    {
        var newDoraIndicators = DoraIndicators.Append(doraIndicator).ToList();
        return this with { DoraIndicators = newDoraIndicators };
    }

    /// <inheritdoc/>
    public override GameState WithRemainingTileCount(int newRemainingTileCount)
    {
        return this with { RemainingTileCount = newRemainingTileCount };
    }

    /// <inheritdoc/>
    public override GameState WithKyotaku(int newKyotaku)
    {
        return this with { Kyotaku = newKyotaku };
    }

    /// <inheritdoc/>
    public override GameState WithSourceStep(MjlogStep? step)
    {
        return this with { SourceStep = step };
    }
}