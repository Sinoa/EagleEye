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
using Foxtamp.MjlogReplayer.Models;

namespace Foxtamp.MjlogReplayer.Builders;

/// <summary>
/// GameStateを外部入力から構築するためのビルダークラス
/// </summary>
public class GameStateBuilder
{
    private int _playerCount = 4;
    private int _roundWind;
    private int _roundNumber;
    private int _honba;
    private int _kyotaku;
    private int _dealerId;
    private int _turnNumber = 1;
    private int _stepIndex;
    private MjlogStep? _sourceStep;
    private readonly List<PlayerStateBuilder> _playerBuilders = [];
    private readonly List<Tile> _doraIndicators = [];
    private int? _remainingTileCount;
    private readonly List<List<Tile>> _nukiDoras = [];

    /// <summary>
    /// 新しいビルダーインスタンスを作成
    /// </summary>
    /// <param name="playerCount">プレイヤー人数（3または4）</param>
    /// <returns>ビルダーインスタンス</returns>
    public static GameStateBuilder Create(int playerCount = 4)
    {
        if (playerCount != 3 && playerCount != 4)
        {
            throw new ArgumentException("プレイヤー人数は3または4である必要があります", nameof(playerCount));
        }

        var builder = new GameStateBuilder { _playerCount = playerCount };

        for (var i = 0; i < playerCount; i++)
        {
            builder._playerBuilders.Add(new PlayerStateBuilder(i));
            builder._nukiDoras.Add([]);
        }

        return builder;
    }

    /// <summary>
    /// 場風を設定
    /// </summary>
    /// <param name="roundWind">場風（0=東, 1=南, 2=西, 3=北）</param>
    /// <returns>ビルダーインスタンス</returns>
    public GameStateBuilder WithRoundWind(int roundWind)
    {
        _roundWind = roundWind;
        return this;
    }

    /// <summary>
    /// 局番号を設定
    /// </summary>
    /// <param name="roundNumber">局番号</param>
    /// <returns>ビルダーインスタンス</returns>
    public GameStateBuilder WithRoundNumber(int roundNumber)
    {
        _roundNumber = roundNumber;
        return this;
    }

    /// <summary>
    /// 本場数を設定
    /// </summary>
    /// <param name="honba">本場数</param>
    /// <returns>ビルダーインスタンス</returns>
    public GameStateBuilder WithHonba(int honba)
    {
        _honba = honba;
        return this;
    }

    /// <summary>
    /// 供託を設定
    /// </summary>
    /// <param name="kyotaku">供託数</param>
    /// <returns>ビルダーインスタンス</returns>
    public GameStateBuilder WithKyotaku(int kyotaku)
    {
        _kyotaku = kyotaku;
        return this;
    }

    /// <summary>
    /// 親プレイヤーIDを設定
    /// </summary>
    /// <param name="dealerId">親プレイヤーID</param>
    /// <returns>ビルダーインスタンス</returns>
    public GameStateBuilder WithDealerId(int dealerId)
    {
        _dealerId = dealerId;
        return this;
    }

    /// <summary>
    /// 巡目を設定
    /// </summary>
    /// <param name="turnNumber">巡目</param>
    /// <returns>ビルダーインスタンス</returns>
    public GameStateBuilder WithTurnNumber(int turnNumber)
    {
        _turnNumber = turnNumber;
        return this;
    }

    /// <summary>
    /// ステップインデックスを設定
    /// </summary>
    /// <param name="stepIndex">ステップインデックス</param>
    /// <returns>ビルダーインスタンス</returns>
    public GameStateBuilder WithStepIndex(int stepIndex)
    {
        _stepIndex = stepIndex;
        return this;
    }

    /// <summary>
    /// 起因ステップを設定
    /// </summary>
    /// <param name="sourceStep">起因となるステップ</param>
    /// <returns>ビルダーインスタンス</returns>
    public GameStateBuilder WithSourceStep(MjlogStep? sourceStep)
    {
        _sourceStep = sourceStep;
        return this;
    }

    /// <summary>
    /// 残り山牌数を設定
    /// </summary>
    /// <param name="remainingTileCount">残り山牌数</param>
    /// <returns>ビルダーインスタンス</returns>
    public GameStateBuilder WithRemainingTileCount(int remainingTileCount)
    {
        _remainingTileCount = remainingTileCount;
        return this;
    }

    /// <summary>
    /// ドラ表示牌を追加
    /// </summary>
    /// <param name="doraIndicator">ドラ表示牌</param>
    /// <returns>ビルダーインスタンス</returns>
    public GameStateBuilder AddDoraIndicator(Tile doraIndicator)
    {
        _doraIndicators.Add(doraIndicator);
        return this;
    }

    /// <summary>
    /// ドラ表示牌を設定
    /// </summary>
    /// <param name="doraIndicators">ドラ表示牌リスト</param>
    /// <returns>ビルダーインスタンス</returns>
    public GameStateBuilder WithDoraIndicators(IEnumerable<Tile> doraIndicators)
    {
        _doraIndicators.Clear();
        _doraIndicators.AddRange(doraIndicators);
        return this;
    }

    /// <summary>
    /// プレイヤー設定にアクセス
    /// </summary>
    /// <param name="playerId">プレイヤーID</param>
    /// <returns>プレイヤービルダー</returns>
    public PlayerStateBuilder Player(int playerId)
    {
        if (playerId < 0 || playerId >= _playerCount)
        {
            throw new ArgumentOutOfRangeException(nameof(playerId), $"プレイヤーIDは0から{_playerCount - 1}の範囲で指定してください");
        }

        return _playerBuilders[playerId];
    }

    /// <summary>
    /// 抜きドラを追加（3人麻雀のみ）
    /// </summary>
    /// <param name="playerId">プレイヤーID</param>
    /// <param name="nukiTile">抜いた北牌</param>
    /// <returns>ビルダーインスタンス</returns>
    public GameStateBuilder AddNukiDora(int playerId, Tile nukiTile)
    {
        if (_playerCount != 3)
        {
            throw new InvalidOperationException("抜きドラは3人麻雀でのみ使用できます");
        }

        if (playerId < 0 || playerId >= 3)
        {
            throw new ArgumentOutOfRangeException(nameof(playerId), "プレイヤーIDは0から2の範囲で指定してください");
        }

        _nukiDoras[playerId].Add(nukiTile);
        return this;
    }

    /// <summary>
    /// GameStateを構築
    /// </summary>
    /// <returns>構築されたGameState</returns>
    public GameState Build()
    {
        var players = _playerBuilders.Select(b => b.Build()).ToList();

        if (_playerCount == 4)
        {
            return new FourPlayerGameState(
                _roundWind,
                _roundNumber,
                _honba,
                _kyotaku,
                _dealerId,
                _turnNumber,
                _stepIndex,
                players,
                _doraIndicators,
                _remainingTileCount ?? 70,
                _sourceStep);
        }
        else
        {
            return new ThreePlayerGameState(
                _roundWind,
                _roundNumber,
                _honba,
                _kyotaku,
                _dealerId,
                _turnNumber,
                _stepIndex,
                players,
                _doraIndicators,
                _remainingTileCount ?? 55,
                _nukiDoras.Select(l => (IReadOnlyList<Tile>)l.ToList()).ToList(),
                _sourceStep);
        }
    }
}

/// <summary>
/// PlayerStateを構築するためのビルダークラス
/// </summary>
public class PlayerStateBuilder
{
    private readonly int _playerId;
    private readonly List<Tile> _hand = [];
    private readonly List<DiscardedTile> _discards = [];
    private readonly List<MeldInfo> _melds = [];
    private bool _isReach;
    private int? _reachTurnNumber;
    private int _score = 25000;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="playerId">プレイヤーID</param>
    public PlayerStateBuilder(int playerId)
    {
        _playerId = playerId;
    }

    /// <summary>
    /// 手牌を設定
    /// </summary>
    /// <param name="hand">手牌</param>
    /// <returns>ビルダーインスタンス</returns>
    public PlayerStateBuilder WithHand(IEnumerable<Tile> hand)
    {
        _hand.Clear();
        _hand.AddRange(hand);
        return this;
    }

    /// <summary>
    /// 手牌に牌を追加
    /// </summary>
    /// <param name="tile">追加する牌</param>
    /// <returns>ビルダーインスタンス</returns>
    public PlayerStateBuilder AddTile(Tile tile)
    {
        _hand.Add(tile);
        return this;
    }

    /// <summary>
    /// 捨て牌を設定
    /// </summary>
    /// <param name="discards">捨て牌</param>
    /// <returns>ビルダーインスタンス</returns>
    public PlayerStateBuilder WithDiscards(IEnumerable<DiscardedTile> discards)
    {
        _discards.Clear();
        _discards.AddRange(discards);
        return this;
    }

    /// <summary>
    /// 捨て牌を追加
    /// </summary>
    /// <param name="discard">追加する捨て牌</param>
    /// <returns>ビルダーインスタンス</returns>
    public PlayerStateBuilder AddDiscard(DiscardedTile discard)
    {
        _discards.Add(discard);
        return this;
    }

    /// <summary>
    /// 捨て牌を追加（簡易版）
    /// </summary>
    /// <param name="tile">捨てた牌</param>
    /// <param name="isTsumogiri">ツモ切りか</param>
    /// <param name="isReachDeclare">リーチ宣言牌か</param>
    /// <param name="calledByPlayerId">鳴かれた場合の相手ID</param>
    /// <returns>ビルダーインスタンス</returns>
    public PlayerStateBuilder AddDiscard(Tile tile, bool isTsumogiri = false, bool isReachDeclare = false, int? calledByPlayerId = null)
    {
        _discards.Add(new DiscardedTile(tile, isTsumogiri, isReachDeclare, calledByPlayerId));
        return this;
    }

    /// <summary>
    /// 副露を設定
    /// </summary>
    /// <param name="melds">副露リスト</param>
    /// <returns>ビルダーインスタンス</returns>
    public PlayerStateBuilder WithMelds(IEnumerable<MeldInfo> melds)
    {
        _melds.Clear();
        _melds.AddRange(melds);
        return this;
    }

    /// <summary>
    /// 副露を追加
    /// </summary>
    /// <param name="meld">追加する副露</param>
    /// <returns>ビルダーインスタンス</returns>
    public PlayerStateBuilder AddMeld(MeldInfo meld)
    {
        _melds.Add(meld);
        return this;
    }

    /// <summary>
    /// リーチ状態を設定
    /// </summary>
    /// <param name="isReach">リーチ状態</param>
    /// <returns>ビルダーインスタンス</returns>
    public PlayerStateBuilder WithReach(bool isReach = true)
    {
        _isReach = isReach;
        return this;
    }

    /// <summary>
    /// リーチ巡目を設定
    /// </summary>
    /// <param name="turnNumber">リーチ宣言時の巡目</param>
    /// <returns>ビルダーインスタンス</returns>
    public PlayerStateBuilder WithReachTurnNumber(int? turnNumber)
    {
        _reachTurnNumber = turnNumber;
        return this;
    }

    /// <summary>
    /// 得点を設定
    /// </summary>
    /// <param name="score">得点</param>
    /// <returns>ビルダーインスタンス</returns>
    public PlayerStateBuilder WithScore(int score)
    {
        _score = score;
        return this;
    }

    /// <summary>
    /// PlayerStateを構築
    /// </summary>
    /// <returns>構築されたPlayerState</returns>
    public PlayerState Build()
    {
        return new PlayerState(
            _playerId,
            _hand.ToList(),
            _discards.ToList(),
            _melds.ToList(),
            _isReach,
            _reachTurnNumber,
            _score);
    }
}