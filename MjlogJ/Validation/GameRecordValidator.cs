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

namespace MjlogJ.Validation;

/// <summary>
/// GameRecordのバリデーター
/// </summary>
public class GameRecordValidator
{
    /// <summary>
    /// GameRecordをバリデート
    /// </summary>
    /// <param name="record">検証するGameRecord</param>
    /// <returns>バリデーション結果</returns>
    public ValidationResult Validate(GameRecord record)
    {
        var result = new ValidationResult();

        ValidateBasicInfo(record, result);
        ValidateRounds(record, result);
        ValidateScores(record, result);

        return result;
    }

    private void ValidateBasicInfo(GameRecord record, ValidationResult result)
    {
        // プレイヤー名のチェック
        var playerCount = record.Rule?.IsThreePlayer == true ? 3 : 4;
        for (int i = 0; i < playerCount; i++)
        {
            if (string.IsNullOrEmpty(record.PlayerNames[i]))
            {
                result.AddError("PLAYER_NAME_MISSING",
                    $"プレイヤー{i}の名前がありません",
                    ValidationSeverity.Warning,
                    playerId: i);
            }
        }

        // ルールのチェック
        if (record.Rule == null)
        {
            result.AddError("RULE_MISSING",
                "ゲームルールが設定されていません",
                ValidationSeverity.Warning);
        }
    }

    private void ValidateRounds(GameRecord record, ValidationResult result)
    {
        for (int roundIndex = 0; roundIndex < record.Rounds.Count; roundIndex++)
        {
            var round = record.Rounds[roundIndex];
            ValidateRound(round, roundIndex, record.Rule?.IsThreePlayer == true, result);
        }
    }

    private void ValidateRound(RoundRecord round, int roundIndex, bool isThreePlayer, ValidationResult result)
    {
        var playerCount = isThreePlayer ? 3 : 4;

        // 配牌の検証
        for (int playerId = 0; playerId < playerCount; playerId++)
        {
            var hand = round.InitialHands[playerId];
            var expectedCount = playerId == round.DealerId ? 14 : 13;

            if (hand.Count != expectedCount)
            {
                result.AddError("INITIAL_HAND_COUNT",
                    $"プレイヤー{playerId}の配牌が{hand.Count}枚です（期待値: {expectedCount}枚）",
                    ValidationSeverity.Warning,
                    roundIndex: roundIndex,
                    playerId: playerId);
            }
        }

        // 牌の枚数チェック（各牌が4枚以下）
        ValidateTileCounts(round, roundIndex, result);

        // 行動順序の検証
        ValidateActionSequence(round, roundIndex, result);

        // 結果の検証
        if (round.Result == null)
        {
            result.AddError("ROUND_RESULT_MISSING",
                $"局の結果がありません",
                ValidationSeverity.Warning,
                roundIndex: roundIndex);
        }
    }

    private void ValidateTileCounts(RoundRecord round, int roundIndex, ValidationResult result)
    {
        var tileCounts = new int[34];

        // 配牌をカウント
        foreach (var hand in round.InitialHands)
        {
            foreach (var tile in hand)
            {
                if (tile.TileTypeId >= 0 && tile.TileTypeId < 34)
                {
                    tileCounts[tile.TileTypeId]++;
                }
            }
        }

        // ドラ表示牌をカウント
        foreach (var dora in round.DoraIndicators)
        {
            if (dora.TileTypeId >= 0 && dora.TileTypeId < 34)
            {
                tileCounts[dora.TileTypeId]++;
            }
        }

        // 各牌が4枚以下かチェック
        for (int typeId = 0; typeId < 34; typeId++)
        {
            if (tileCounts[typeId] > 4)
            {
                result.AddError("TILE_COUNT_EXCEEDED",
                    $"牌TypeId={typeId}が{tileCounts[typeId]}枚あります（最大4枚）",
                    ValidationSeverity.Error,
                    roundIndex: roundIndex);
            }
        }
    }

    private void ValidateActionSequence(RoundRecord round, int roundIndex, ValidationResult result)
    {
        PlayerAction? lastAction = null;
        var playerLastActions = new Dictionary<int, PlayerAction>();

        foreach (var action in round.Actions)
        {
            // シーケンス番号の連続性チェック
            if (lastAction != null && action.Sequence != lastAction.Sequence + 1)
            {
                result.AddError("SEQUENCE_GAP",
                    $"シーケンス番号が連続していません（{lastAction.Sequence} -> {action.Sequence}）",
                    ValidationSeverity.Warning,
                    roundIndex: roundIndex,
                    sequence: action.Sequence);
            }

            // ツモ→打牌の順序チェック
            if (action is DiscardAction && action.PlayerId >= 0)
            {
                if (playerLastActions.TryGetValue(action.PlayerId, out var last))
                {
                    if (last is not DrawAction && last is not MeldAction)
                    {
                        result.AddError("DISCARD_WITHOUT_DRAW",
                            $"プレイヤー{action.PlayerId}がツモ/鳴きなしで打牌しています",
                            ValidationSeverity.Warning,
                            roundIndex: roundIndex,
                            playerId: action.PlayerId,
                            sequence: action.Sequence);
                    }
                }
            }

            if (action.PlayerId >= 0)
            {
                playerLastActions[action.PlayerId] = action;
            }

            lastAction = action;
        }
    }

    private void ValidateScores(GameRecord record, ValidationResult result)
    {
        if (record.Rounds.Count == 0) return;

        var isThreePlayer = record.Rule?.IsThreePlayer == true;
        var playerCount = isThreePlayer ? 3 : 4;
        var expectedTotal = isThreePlayer ? 105000 : 100000;

        // 最初の局の開始スコア合計をチェック
        var firstRound = record.Rounds[0];
        var initialTotal = firstRound.StartScores.Take(playerCount).Sum();

        // 供託を考慮
        var kyotakuPoints = firstRound.Kyotaku * 1000;
        var adjustedTotal = initialTotal + kyotakuPoints;

        if (adjustedTotal != expectedTotal)
        {
            result.AddError("INITIAL_SCORE_TOTAL",
                $"初期スコア合計が{adjustedTotal}点です（期待値: {expectedTotal}点、供託{firstRound.Kyotaku}本）",
                ValidationSeverity.Warning);
        }

        // 各局の点数移動チェック
        for (int i = 0; i < record.Rounds.Count; i++)
        {
            var round = record.Rounds[i];
            if (round.Result == null) continue;

            // 和了時の点数移動合計チェック
            if (round.Result.IsAgari)
            {
                foreach (var agari in round.Result.AgariResults)
                {
                    var changeTotal = agari.ScoreChanges.Values.Sum();
                    // 供託がある場合は合計が0にならない
                    if (round.Kyotaku == 0 && changeTotal != 0)
                    {
                        result.AddError("SCORE_CHANGE_MISMATCH",
                            $"点数移動の合計が0ではありません（{changeTotal}点）",
                            ValidationSeverity.Warning,
                            roundIndex: i);
                    }
                }
            }
        }
    }
}