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
/// 3人麻雀の試合状態を表すレコード
/// </summary>
public record ThreePlayerGameState : GameState
{
    /// <inheritdoc/>
    public override int PlayerCount => 3;

    /// <summary>
    /// 各プレイヤーの抜きドラ（北抜き）リスト
    /// </summary>
    public IReadOnlyList<IReadOnlyList<Tile>> NukiDoras { get; init; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public ThreePlayerGameState(
        int roundWind,
        int roundNumber,
        int honba,
        int kyotaku,
        int dealerId,
        int turnNumber,
        int stepIndex,
        IReadOnlyList<PlayerState> players,
        IReadOnlyList<Tile> doraIndicators,
        IReadOnlyList<IReadOnlyList<Tile>> nukiDoras,
        MjlogStep? sourceStep)
        : base(roundWind, roundNumber, honba, kyotaku, dealerId, turnNumber, stepIndex, players, doraIndicators, sourceStep)
    {
        if (players.Count != 3)
        {
            throw new ArgumentException("3人麻雀では3人のプレイヤー状態が必要です", nameof(players));
        }

        if (nukiDoras.Count != 3)
        {
            throw new ArgumentException("3人麻雀では3人分の抜きドラリストが必要です", nameof(nukiDoras));
        }

        NukiDoras = nukiDoras;
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
    public static ThreePlayerGameState CreateInitial(
        int roundWind,
        int roundNumber,
        int honba,
        int kyotaku,
        int dealerId,
        IReadOnlyList<IReadOnlyList<Tile>> initialHands,
        IReadOnlyList<int> initialScores,
        Tile? initialDoraIndicator)
    {
        if (initialHands.Count != 3)
        {
            throw new ArgumentException("3人麻雀では3人分の配牌が必要です", nameof(initialHands));
        }

        if (initialScores.Count != 3)
        {
            throw new ArgumentException("3人麻雀では3人分の初期得点が必要です", nameof(initialScores));
        }

        var players = new List<PlayerState>();
        for (var i = 0; i < 3; i++)
        {
            players.Add(PlayerState.CreateInitial(i, initialHands[i], initialScores[i]));
        }

        var doraIndicators = initialDoraIndicator != null
            ? new List<Tile> { initialDoraIndicator }
            : new List<Tile>();

        // 空の抜きドラリストを初期化
        var nukiDoras = new List<IReadOnlyList<Tile>>
        {
            new List<Tile>(),
            new List<Tile>(),
            new List<Tile>()
        };

        return new ThreePlayerGameState(
            roundWind,
            roundNumber,
            honba,
            kyotaku,
            dealerId,
            1, // 巡目は1から開始
            0, // ステップは0から開始
            players,
            doraIndicators,
            nukiDoras,
            null); // 初期状態なので起因ステップはなし
    }

    /// <inheritdoc/>
    public override GameState WithPlayer(int playerId, PlayerState newPlayerState)
    {
        if (playerId < 0 || playerId >= 3)
        {
            throw new ArgumentOutOfRangeException(nameof(playerId), "プレイヤーIDは0から2の範囲で指定してください");
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
    public override GameState WithKyotaku(int newKyotaku)
    {
        return this with { Kyotaku = newKyotaku };
    }

    /// <inheritdoc/>
    public override GameState WithSourceStep(MjlogStep? step)
    {
        return this with { SourceStep = step };
    }

    /// <summary>
    /// 北抜きを追加
    /// </summary>
    /// <param name="playerId">プレイヤーID</param>
    /// <param name="nukiTile">抜いた北牌</param>
    /// <returns>更新されたGameState</returns>
    public ThreePlayerGameState AddNukiDora(int playerId, Tile nukiTile)
    {
        if (playerId < 0 || playerId >= 3)
        {
            throw new ArgumentOutOfRangeException(nameof(playerId), "プレイヤーIDは0から2の範囲で指定してください");
        }

        var newNukiDoras = NukiDoras.Select(list => list.ToList()).ToList();
        newNukiDoras[playerId].Add(nukiTile);

        // 手牌から北牌を削除
        var player = Players[playerId];
        var newHand = player.Hand.ToList();
        var index = newHand.FindIndex(t => t.OriginalId == nukiTile.OriginalId);
        if (index >= 0)
        {
            newHand.RemoveAt(index);
        }

        var newPlayer = player with { Hand = newHand };

        var newPlayers = Players.ToList();
        newPlayers[playerId] = newPlayer;

        return this with { Players = newPlayers, NukiDoras = newNukiDoras.Select(l => (IReadOnlyList<Tile>)l).ToList() };
    }

    /// <summary>
    /// 指定プレイヤーの抜きドラを取得
    /// </summary>
    /// <param name="playerId">プレイヤーID</param>
    /// <returns>抜きドラリスト</returns>
    public IReadOnlyList<Tile> GetNukiDoras(int playerId)
    {
        if (playerId < 0 || playerId >= 3)
        {
            throw new ArgumentOutOfRangeException(nameof(playerId), "プレイヤーIDは0から2の範囲で指定してください");
        }

        return NukiDoras[playerId];
    }

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    public override string ToString()
    {
        var baseStr = base.ToString();
        var totalNuki = NukiDoras.Sum(list => list.Count);
        return $"{baseStr} 抜きドラ総数:{totalNuki}";
    }
}