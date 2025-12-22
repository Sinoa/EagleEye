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

using System.Xml.Linq;
using MjlogJ.Analyzers;
using MjlogJ.Decoders;
using MjlogJ.Models;

namespace MjlogJ.Parsers;

/// <summary>
/// 天鳳の牌譜XMLを解析するパーサー
/// </summary>
public class MjlogXmlParser
{
    // 役名テーブル
    private static readonly string[] YakuNames =
    [
        "門前清自摸和", "立直", "一発", "槍槓", "嶺上開花", // 0-4
        "海底摸月", "河底撈魚", "平和", "断幺九", "一盃口", // 5-9
        "自風 東", "自風 南", "自風 西", "自風 北", // 10-13
        "場風 東", "場風 南", "場風 西", "場風 北", // 14-17
        "役牌 白", "役牌 發", "役牌 中", // 18-20
        "両立直", "七対子", "混全帯幺九", "一気通貫", "三色同順", // 21-25
        "三色同刻", "三槓子", "対々和", "三暗刻", "小三元", // 26-30
        "混老頭", "二盃口", "純全帯幺九", "混一色", "清一色", // 31-35
        "", "", "", "", "", // 36-40 (未使用)
        "", "", "", "", "", // 41-45 (未使用)
        "", "", "", "", "", // 46-50 (未使用)
        "", "ドラ", "裏ドラ", "赤ドラ" // 51-54
    ];

    // 役満名テーブル
    private static readonly string[] YakumanNames =
    [
        "天和", "地和", "大三元", "四暗刻", "四暗刻単騎", // 0-4
        "字一色", "緑一色", "清老頭", "九蓮宝燈", "純正九蓮宝燈", // 5-9
        "国士無双", "国士無双１３面", "大四喜", "小四喜", "四槓子", // 10-14
        "", "", "", "", "", // 15-19
        "ドラ", "裏ドラ", "赤ドラ" // 20-22
    ];

    // 段位名テーブル
    private static readonly string[] DanNames =
    [
        "新人", "9級", "8級", "7級", "6級", "5級", "4級", "3級", "2級", "1級",
        "初段", "二段", "三段", "四段", "五段", "六段", "七段", "八段", "九段", "十段",
        "天鳳位"
    ];

    /// <summary>
    /// XMLファイルを解析してGameRecordを生成
    /// </summary>
    public GameRecord Parse(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid XML: no root element");

        var record = new GameRecord();

        // ルート要素からゲームデータを解析
        // mjloggm形式: ルート要素自体に子要素としてゲームデータが含まれる
        // 他の形式: ルート要素がゲームデータ
        ParseGameElement(root, record);

        return record;
    }

    private void ParseGameElement(XElement gameElement, GameRecord record)
    {
        RoundRecord? currentRound = null;
        var actionSequence = 0;
        var playerHands = new List<Tile>[4];
        var playerMelds = new List<MeldInfo>[4];
        var playerReached = new bool[4];
        var allDiscards = new List<Tile>(); // 見えている牌（捨て牌）
        int[] currentScores = [250000, 250000, 250000, 250000];

        for (var i = 0; i < 4; i++)
        {
            playerHands[i] = [];
            playerMelds[i] = [];
        }

        foreach (var element in gameElement.Elements())
        {
            var name = element.Name.LocalName.ToUpperInvariant();

            try
            {
                switch (name)
                {
                    case "SHUFFLE":
                        record.ShuffleSeed = element.Attribute("seed")?.Value;
                        record.Reference = element.Attribute("ref")?.Value;
                        break;

                    case "GO":
                        ParseGameOptions(element, record);
                        break;

                    case "UN":
                        ParseUserNames(element, record);
                        break;

                    case "TAIKYOKU":
                        // 対局開始、特に処理なし
                        break;

                    case "INIT":
                        // 新しい局の開始
                        currentRound = ParseInit(element, currentScores);
                        record.Rounds.Add(currentRound);
                        actionSequence = 0;

                        // プレイヤー手牌を初期化
                        for (var i = 0; i < 4; i++)
                        {
                            playerHands[i] = [.. currentRound.InitialHands[i]];
                            playerMelds[i] = [];
                            playerReached[i] = false;
                        }

                        allDiscards.Clear();
                        break;

                    case "DORA":
                        if (currentRound != null)
                        {
                            var doraAttr = element.Attribute("hai");
                            if (doraAttr != null && int.TryParse(doraAttr.Value, out var doraId))
                            {
                                var doraTile = TileDecoder.Decode(doraId);
                                currentRound.DoraIndicators.Add(doraTile);

                                // 新ドラとしてアクションに追加
                                currentRound.Actions.Add(new NewDoraAction
                                {
                                    PlayerId = -1, // システムアクション
                                    Sequence = actionSequence++,
                                    DoraTile = doraTile
                                });
                            }
                        }

                        break;

                    case "AGARI":
                        if (currentRound != null)
                        {
                            ParseAgari(element, currentRound, currentScores);
                        }

                        break;

                    case "RYUUKYOKU":
                        if (currentRound != null)
                        {
                            ParseRyuukyoku(element, currentRound, currentScores);
                        }

                        break;

                    case "BYE":
                        // プレイヤー切断、特に処理なし
                        break;

                    default:
                        // T0-T3: ツモ、D0-D3: 打牌、N: 鳴き、REACH: リーチ
                        if (currentRound != null)
                        {
                            if (name.Length >= 1 && (name[0] == 'T' || name[0] == 'U' || name[0] == 'V' || name[0] == 'W'))
                            {
                                // ツモ: T=0, U=1, V=2, W=3
                                var playerId = name[0] switch { 'T' => 0, 'U' => 1, 'V' => 2, 'W' => 3, _ => -1 };
                                if (playerId >= 0)
                                {
                                    var tileIdStr = name.Length > 1 ? name[1..] : element.Value;
                                    if (int.TryParse(tileIdStr, out var tileId))
                                    {
                                        var tile = TileDecoder.Decode(tileId);
                                        playerHands[playerId].Add(tile);

                                        currentRound.Actions.Add(new DrawAction
                                        {
                                            PlayerId = playerId,
                                            Sequence = actionSequence++,
                                            Tile = tile
                                        });
                                    }
                                }
                            }
                            else if (name.Length >= 1 && (name[0] == 'D' || name[0] == 'E' || name[0] == 'F' || name[0] == 'G'))
                            {
                                // 打牌: D=0, E=1, F=2, G=3
                                var playerId = name[0] switch { 'D' => 0, 'E' => 1, 'F' => 2, 'G' => 3, _ => -1 };
                                if (playerId >= 0)
                                {
                                    var tileIdStr = name.Length > 1 ? name[1..] : element.Value;
                                    if (int.TryParse(tileIdStr, out var tileId))
                                    {
                                        var tile = TileDecoder.Decode(tileId);

                                        // ツモ切り判定
                                        var lastDraw = currentRound.Actions
                                            .OfType<DrawAction>()
                                            .LastOrDefault(a => a.PlayerId == playerId);
                                        var isTsumogiri = lastDraw?.Tile?.OriginalId == tileId;

                                        // 手牌から削除
                                        var handTile = playerHands[playerId].FirstOrDefault(t => t.OriginalId == tileId);
                                        if (handTile != null)
                                        {
                                            playerHands[playerId].Remove(handTile);
                                        }

                                        allDiscards.Add(tile);

                                        currentRound.Actions.Add(new DiscardAction
                                        {
                                            PlayerId = playerId,
                                            Sequence = actionSequence++,
                                            Tile = tile,
                                            IsTsumogiri = isTsumogiri,
                                            HandAfterDiscard = [..playerHands[playerId]],
                                            MeldsAfterDiscard = [..playerMelds[playerId]]
                                        });
                                    }
                                }
                            }
                            else if (name == "N")
                            {
                                ParseMeld(element, currentRound, ref actionSequence, playerHands, playerMelds);
                            }
                            else if (name == "REACH")
                            {
                                ParseReach(element, currentRound, ref actionSequence,
                                    playerHands, playerMelds, playerReached, allDiscards);
                            }
                        }

                        break;
                }
            }
            catch (Exception)
            {
                // 個別要素の解析エラーは無視して続行
            }
        }

        // 最終結果を生成
        GenerateFinalResult(record);
    }

    private void ParseGameOptions(XElement element, GameRecord record)
    {
        var type = int.Parse(element.Attribute("type")?.Value ?? "0");
        var lobby = int.Parse(element.Attribute("lobby")?.Value ?? "0");

        record.Rule = new GameRule
        {
            OriginalFlags = type,
            Lobby = lobby,
            HasRedDora = (type & 0x02) == 0, // bit1: 0=赤あり, 1=赤なし
            HasOpenTanyao = (type & 0x04) == 0, // bit2: 0=喰いタンあり, 1=なし
            IsEastOnly = (type & 0x08) != 0, // bit3: 1=東風戦
            IsThreePlayer = (type & 0x10) != 0, // bit4: 1=三人麻雀
            Speed = (type >> 6) & 0x03, // bit6-7: 速度
            HasKuikae = (type & 0x80) == 0 // bit7: 0=喰い替えあり
        };
    }

    private void ParseUserNames(XElement element, GameRecord record)
    {
        for (var i = 0; i < 4; i++)
        {
            var nameAttr = element.Attribute($"n{i}");
            if (nameAttr != null)
            {
                record.PlayerNames[i] = Uri.UnescapeDataString(nameAttr.Value);
            }

            var danAttr = element.Attribute("dan");
            if (danAttr != null)
            {
                var dans = danAttr.Value.Split(',');
                if (i < dans.Length && int.TryParse(dans[i], out var danIdx))
                {
                    record.PlayerDans[i] = danIdx < DanNames.Length ? DanNames[danIdx] : $"段位{danIdx}";
                }
            }

            var rateAttr = element.Attribute("rate");
            if (rateAttr != null)
            {
                var rates = rateAttr.Value.Split(',');
                if (i < rates.Length && float.TryParse(rates[i], out var rate))
                {
                    record.PlayerRates[i] = rate;
                }
            }
        }
    }

    private RoundRecord ParseInit(XElement element, int[] currentScores)
    {
        var round = new RoundRecord();

        // 局情報: seed="局,本場,供託,ダイス1,ダイス2,ドラ表示牌"
        var seedAttr = element.Attribute("seed")?.Value;
        if (seedAttr != null)
        {
            var seeds = seedAttr.Split(',');
            if (seeds.Length >= 6)
            {
                round.RoundNumber = int.Parse(seeds[0]);
                round.RoundWind = round.RoundNumber / 4;
                round.Honba = int.Parse(seeds[1]);
                round.Kyotaku = int.Parse(seeds[2]);
                // seeds[3], seeds[4] はダイス
                var doraId = int.Parse(seeds[5]);
                round.InitialDoraIndicator = TileDecoder.Decode(doraId);
                round.DoraIndicators.Add(round.InitialDoraIndicator);
            }
        }

        // 親
        var oyaAttr = element.Attribute("oya")?.Value;
        if (oyaAttr != null)
        {
            round.DealerId = int.Parse(oyaAttr);
        }

        // 得点
        var tenAttr = element.Attribute("ten")?.Value;
        if (tenAttr != null)
        {
            var tens = tenAttr.Split(',');
            for (var i = 0; i < Math.Min(4, tens.Length); i++)
            {
                round.StartScores[i] = int.Parse(tens[i]) * 100;
                currentScores[i] = round.StartScores[i];
            }
        }

        // 配牌
        for (var i = 0; i < 4; i++)
        {
            var haiAttr = element.Attribute($"hai{i}")?.Value;
            if (haiAttr != null)
            {
                round.InitialHands[i] = TileDecoder.DecodeMultiple(haiAttr);
            }
        }

        return round;
    }

    private void ParseMeld(XElement element, RoundRecord round, ref int sequence,
        List<Tile>[] playerHands, List<MeldInfo>[] playerMelds)
    {
        var whoAttr = element.Attribute("who")?.Value;
        var mAttr = element.Attribute("m")?.Value;

        if (whoAttr == null || mAttr == null) return;

        var playerId = int.Parse(whoAttr);
        var meldCode = int.Parse(mAttr);

        var meld = MeldDecoder.Decode(meldCode, playerId);
        playerMelds[playerId].Add(meld);

        // 手牌から鳴きに使った牌を削除（暗槓・加槓以外）
        if (meld.Type != MeldType.AnKan)
        {
            foreach (var tile in meld.Tiles)
            {
                if (tile != meld.CalledTile) // 鳴いた牌は他家から
                {
                    var handTile = playerHands[playerId].FirstOrDefault(t => t.OriginalId == tile.OriginalId);
                    if (handTile != null)
                    {
                        playerHands[playerId].Remove(handTile);
                    }
                }
            }
        }
        else
        {
            // 暗槓は4枚とも自分の牌
            foreach (var tile in meld.Tiles)
            {
                var handTile = playerHands[playerId].FirstOrDefault(t => t.OriginalId == tile.OriginalId);
                if (handTile != null)
                {
                    playerHands[playerId].Remove(handTile);
                }
            }
        }

        round.Actions.Add(new MeldAction
        {
            PlayerId = playerId,
            Sequence = sequence++,
            Meld = meld
        });
    }

    private void ParseReach(XElement element, RoundRecord round, ref int sequence,
        List<Tile>[] playerHands, List<MeldInfo>[] playerMelds, bool[] playerReached,
        List<Tile> allDiscards)
    {
        var whoAttr = element.Attribute("who")?.Value;
        var stepAttr = element.Attribute("step")?.Value;

        if (whoAttr == null) return;

        var playerId = int.Parse(whoAttr);
        var step = int.Parse(stepAttr ?? "1");

        round.Actions.Add(new ReachAction
        {
            PlayerId = playerId,
            Sequence = sequence++,
            Step = step
        });

        // リーチ成立時（step=2）に待ち牌を計算
        if (step == 2 && !playerReached[playerId])
        {
            playerReached[playerId] = true;

            // 待ち牌を計算
            var waitingInfo = WaitingTileAnalyzer.Analyze(
                playerHands[playerId],
                playerMelds[playerId],
                playerId,
                sequence - 1,
                allDiscards);

            if (waitingInfo != null)
            {
                round.ComputedData ??= new RoundComputedData();
                round.ComputedData.ReachWaitingTiles[playerId] = waitingInfo;
            }
        }
    }

    private void ParseAgari(XElement element, RoundRecord round, int[] currentScores)
    {
        round.Result ??= new RoundResult { IsAgari = true };

        var agari = new AgariResult();

        // 和了者と放銃者
        var whoAttr = element.Attribute("who")?.Value;
        var fromWhoAttr = element.Attribute("fromWho")?.Value;

        if (whoAttr != null)
        {
            agari.WinnerId = int.Parse(whoAttr);
        }

        if (fromWhoAttr != null)
        {
            agari.LoserId = int.Parse(fromWhoAttr);
            agari.IsTsumo = agari.WinnerId == agari.LoserId;
        }

        // 手牌
        var haiAttr = element.Attribute("hai")?.Value;
        if (haiAttr != null)
        {
            agari.Hand = TileDecoder.DecodeMultiple(haiAttr);
        }

        // 副露
        var mAttr = element.Attribute("m")?.Value;
        if (mAttr != null)
        {
            var meldCodes = mAttr.Split(',');
            foreach (var code in meldCodes)
            {
                if (int.TryParse(code, out var meldCode))
                {
                    agari.Melds.Add(MeldDecoder.Decode(meldCode, agari.WinnerId));
                }
            }
        }

        // 和了牌
        var machiAttr = element.Attribute("machi")?.Value;
        if (machiAttr != null && int.TryParse(machiAttr, out var machiId))
        {
            agari.WinningTile = TileDecoder.Decode(machiId);
        }

        // ドラ表示牌
        var doraHaiAttr = element.Attribute("doraHai")?.Value;
        if (doraHaiAttr != null)
        {
            agari.DoraIndicators = TileDecoder.DecodeMultiple(doraHaiAttr);
        }

        // 裏ドラ表示牌
        var doraHaiUraAttr = element.Attribute("doraHaiUra")?.Value;
        if (doraHaiUraAttr != null)
        {
            agari.UraDoraIndicators = TileDecoder.DecodeMultiple(doraHaiUraAttr);
            round.UraDoraIndicators = agari.UraDoraIndicators;
        }

        // 得点情報: ten="符,点数,?"
        var tenAttr = element.Attribute("ten")?.Value;
        if (tenAttr != null)
        {
            var tens = tenAttr.Split(',');
            if (tens.Length >= 2)
            {
                agari.Fu = int.Parse(tens[0]);
                agari.Score = int.Parse(tens[1]);
            }
        }

        // 役: yaku="役ID,飜,役ID,飜,..."
        var yakuAttr = element.Attribute("yaku")?.Value;
        if (yakuAttr != null)
        {
            var yakus = yakuAttr.Split(',');
            for (var i = 0; i + 1 < yakus.Length; i += 2)
            {
                var yakuId = int.Parse(yakus[i]);
                var han = int.Parse(yakus[i + 1]);
                agari.Han += han;
                agari.Yakus.Add(new YakuInfo
                {
                    Id = yakuId,
                    Name = yakuId < YakuNames.Length ? YakuNames[yakuId] : $"役{yakuId}",
                    Han = han
                });
            }
        }

        // 役満: yakuman="役満ID,役満ID,..."
        var yakumanAttr = element.Attribute("yakuman")?.Value;
        if (yakumanAttr != null)
        {
            var yakumans = yakumanAttr.Split(',');
            foreach (var ym in yakumans)
            {
                if (int.TryParse(ym, out var ymId))
                {
                    agari.Yakuman++;
                    agari.Yakus.Add(new YakuInfo
                    {
                        Id = ymId,
                        Name = ymId < YakumanNames.Length ? YakumanNames[ymId] : $"役満{ymId}",
                        Yakuman = 1
                    });
                }
            }
        }

        // 点数移動: sc="P0後点,P0変動,P1後点,P1変動,..."
        // 後点は局開始時点の点数、変動は点数の増減
        // 最終点 = 後点 + 変動
        var scAttr = element.Attribute("sc")?.Value;
        if (scAttr != null)
        {
            var scs = scAttr.Split(',');
            for (var i = 0; i + 1 < scs.Length; i += 2)
            {
                var baseScore = int.Parse(scs[i]) * 100;
                var change = int.Parse(scs[i + 1]) * 100;
                agari.ScoreChanges[i / 2] = change;
                currentScores[i / 2] = baseScore + change;
            }
        }

        round.Result.AgariResults.Add(agari);
        Array.Copy(currentScores, round.Result.FinalScores, 4);
    }

    private void ParseRyuukyoku(XElement element, RoundRecord round, int[] currentScores)
    {
        round.Result ??= new RoundResult { IsAgari = false };

        var draw = new DrawResult();

        // 流局種類
        var typeAttr = element.Attribute("type")?.Value;
        draw.Type = typeAttr switch
        {
            "yao9" => DrawType.NineTerminals, // 九種九牌
            "kaze4" => DrawType.FourWinds, // 四風連打
            "kan4" => DrawType.FourKans, // 四槓散了
            "reach4" => DrawType.FourReach, // 四家立直
            "ron3" => DrawType.TripleRon, // 三家和了
            "nm" => DrawType.NagashiMangan, // 流し満貫
            _ => DrawType.Exhaustive // 通常流局
        };

        // テンパイ者: hai0="牌,牌,..." (存在すればテンパイ)
        for (var i = 0; i < 4; i++)
        {
            var haiAttr = element.Attribute($"hai{i}")?.Value;
            if (haiAttr != null && !string.IsNullOrEmpty(haiAttr))
            {
                draw.TenpaiPlayerIds.Add(i);
            }
        }

        // 点数移動: sc="P0後点,P0変動,P1後点,P1変動,..."
        // 後点は局開始時点の点数、変動は点数の増減
        // 最終点 = 後点 + 変動
        var scAttr = element.Attribute("sc")?.Value;
        if (scAttr != null)
        {
            var scs = scAttr.Split(',');
            for (var i = 0; i + 1 < scs.Length; i += 2)
            {
                var baseScore = int.Parse(scs[i]) * 100;
                var change = int.Parse(scs[i + 1]) * 100;
                draw.ScoreChanges[i / 2] = change;
                currentScores[i / 2] = baseScore + change;
            }
        }

        round.Result.DrawResult = draw;
        Array.Copy(currentScores, round.Result.FinalScores, 4);
    }

    private void GenerateFinalResult(GameRecord record)
    {
        if (record.Rounds.Count == 0) return;

        var lastRound = record.Rounds[^1];
        if (lastRound.Result == null) return;

        var finalScores = lastRound.Result.FinalScores;

        record.Result = new GameResult();

        // 順位を計算
        var indexed = finalScores
            .Select((score, idx) => (score, idx))
            .OrderByDescending(x => x.score)
            .ToList();

        for (var rank = 0; rank < indexed.Count; rank++)
        {
            var (score, playerId) = indexed[rank];
            record.Result.PlayerResults.Add(new PlayerResult
            {
                PlayerId = playerId,
                Name = record.PlayerNames[playerId] ?? "",
                FinalScore = score,
                Rank = rank + 1,
                Dan = record.PlayerDans[playerId] ?? "",
                Rate = record.PlayerRates[playerId]
            });
        }
    }
}