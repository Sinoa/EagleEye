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
using Foxtamp.MjlogReader.Models.Actions;
using Foxtamp.MjlogReplayer.Models;

namespace Foxtamp.MjlogReplayer;

/// <summary>
/// MjlogSessionから試合状態の履歴を再現するクラス
/// </summary>
public class SessionReplayer
{
    private readonly MjlogSession _session;
    private readonly List<GameState> _states = [];

    /// <summary>
    /// 再現された試合状態の履歴
    /// </summary>
    public IReadOnlyList<GameState> States => _states;

    /// <summary>
    /// ステップ数
    /// </summary>
    public int StepCount => _states.Count;

    /// <summary>
    /// セッション情報
    /// </summary>
    public MjlogSession Session => _session;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="session">再現するセッション</param>
    public SessionReplayer(MjlogSession session)
    {
        _session = session;
        ReplaySession();
    }

    /// <summary>
    /// 指定インデックスの試合状態を取得
    /// </summary>
    /// <param name="stepIndex">ステップインデックス</param>
    /// <returns>試合状態</returns>
    public GameState this[int stepIndex]
    {
        get
        {
            if (stepIndex < 0 || stepIndex >= _states.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(stepIndex), $"ステップインデックスは0から{_states.Count - 1}の範囲で指定してください");
            }

            return _states[stepIndex];
        }
    }

    /// <summary>
    /// セッションを再現して状態履歴を構築
    /// </summary>
    private void ReplaySession()
    {
        // 初期状態を作成
        var initialState = CreateInitialState();
        _states.Add(initialState);

        // 各ステップを処理
        var currentState = initialState;
        var isReachDeclared = false; // リーチ宣言後の打牌を追跡

        foreach (var step in _session.Steps)
        {
            currentState = ProcessStep(currentState, step, ref isReachDeclared);
            _states.Add(currentState);
        }
    }

    /// <summary>
    /// 初期状態を作成
    /// </summary>
    private GameState CreateInitialState()
    {
        var playerCount = _session.PlayerCount;
        var initialHands = _session.InitialHands.Select(h => (IReadOnlyList<Tile>)h.ToList()).ToList();
        var initialScores = _session.StartScores.ToList();

        if (playerCount == 4)
        {
            return FourPlayerGameState.CreateInitial(
                _session.RoundWind,
                _session.RoundNumber,
                _session.Honba,
                _session.Kyotaku,
                _session.DealerId,
                initialHands,
                initialScores,
                _session.InitialDoraIndicator);
        }
        else
        {
            return ThreePlayerGameState.CreateInitial(
                _session.RoundWind,
                _session.RoundNumber,
                _session.Honba,
                _session.Kyotaku,
                _session.DealerId,
                initialHands,
                initialScores,
                _session.InitialDoraIndicator);
        }
    }

    /// <summary>
    /// ステップを処理して新しい状態を返す
    /// </summary>
    private GameState ProcessStep(GameState state, MjlogStep step, ref bool isReachDeclared)
    {
        var newState = state
            .WithStepIndex(step.StepIndex)
            .WithTurnNumber(step.TurnNumber);

        switch (step.Action)
        {
            case DrawAction draw:
                newState = ProcessDraw(newState, step.PlayerId, draw);
                break;

            case DiscardAction discard:
                newState = ProcessDiscard(newState, step.PlayerId, discard, isReachDeclared);
                isReachDeclared = false; // リーチ宣言フラグをリセット
                break;

            case MeldAction meld:
                newState = ProcessMeld(newState, step.PlayerId, meld);
                break;

            case ReachAction reach:
                newState = ProcessReach(newState, step.PlayerId, reach, ref isReachDeclared);
                break;

            case DoraAction dora:
                newState = ProcessDora(newState, dora);
                break;

            case AgariAction:
            case RyuukyokuAction:
                // 和了・流局は状態変更なし（結果情報はSessionに含まれる）
                break;
        }

        // 起因ステップを設定
        newState = newState.WithSourceStep(step);

        return newState;
    }

    /// <summary>
    /// ツモを処理
    /// </summary>
    private static GameState ProcessDraw(GameState state, int playerId, DrawAction draw)
    {
        if (draw.Tile == null)
        {
            return state;
        }

        var player = state.GetPlayer(playerId);
        var newPlayer = player.AddTileToHand(draw.Tile);
        var newState = state.WithPlayer(playerId, newPlayer);
        return newState.WithRemainingTileCount(state.RemainingTileCount - 1);
    }

    /// <summary>
    /// 打牌を処理
    /// </summary>
    private static GameState ProcessDiscard(GameState state, int playerId, DiscardAction discard, bool isReachDeclared)
    {
        if (discard.Tile == null)
        {
            return state;
        }

        var player = state.GetPlayer(playerId);
        var newPlayer = player.DiscardTile(discard.Tile, discard.IsTsumogiri, isReachDeclared);
        return state.WithPlayer(playerId, newPlayer);
    }

    /// <summary>
    /// 鳴きを処理
    /// </summary>
    private static GameState ProcessMeld(GameState state, int playerId, MeldAction meldAction)
    {
        if (meldAction.Meld == null)
        {
            return state;
        }

        var meld = meldAction.Meld;
        meld.TurnNumber = state.TurnNumber;
        var player = state.GetPlayer(playerId);
        var newState = state;

        switch (meld.Type)
        {
            case MeldType.Chi:
            case MeldType.Pon:
            case MeldType.DaiMinKan:
                // 他家の最後の捨て牌をマーク
                var fromPlayerId = (playerId + meld.FromPlayer) % state.PlayerCount;
                var fromPlayer = state.GetPlayer(fromPlayerId);
                var markedFromPlayer = fromPlayer.MarkLastDiscardAsCalled(playerId);
                newState = newState.WithPlayer(fromPlayerId, markedFromPlayer);

                // 副露を追加
                var newPlayer = newState.GetPlayer(playerId).AddMeld(meld);
                newState = newState.WithPlayer(playerId, newPlayer);
                break;

            case MeldType.KaKan:
                // 加槓
                var kakanPlayer = player.AddKaKan(meld);
                newState = newState.WithPlayer(playerId, kakanPlayer);
                break;

            case MeldType.AnKan:
                // 暗槓
                var ankanPlayer = player.AddMeld(meld);
                newState = newState.WithPlayer(playerId, ankanPlayer);
                break;

            case MeldType.Nuki:
                // 北抜き（3人麻雀）
                if (newState is ThreePlayerGameState threeState && meld.Tiles.Count > 0)
                {
                    newState = threeState.AddNukiDora(playerId, meld.Tiles[0]);
                }

                break;
        }

        return newState;
    }

    /// <summary>
    /// リーチを処理
    /// </summary>
    private static GameState ProcessReach(GameState state, int playerId, ReachAction reach, ref bool isReachDeclared)
    {
        if (reach.Step == 1)
        {
            // リーチ宣言
            isReachDeclared = true;
            var player = state.GetPlayer(playerId);
            var newPlayer = player.DeclareReach(state.TurnNumber);
            return state.WithPlayer(playerId, newPlayer);
        }
        else if (reach.Step == 2)
        {
            // リーチ成立（供託）
            var player = state.GetPlayer(playerId);
            var newPlayer = player.WithScore(player.Score - 1000);
            var newState = state.WithPlayer(playerId, newPlayer);
            return newState.WithKyotaku(state.Kyotaku + 1);
        }

        return state;
    }

    /// <summary>
    /// 新ドラを処理
    /// </summary>
    private static GameState ProcessDora(GameState state, DoraAction dora)
    {
        if (dora.Tile == null)
        {
            return state;
        }

        return state.AddDoraIndicator(dora.Tile);
    }
}