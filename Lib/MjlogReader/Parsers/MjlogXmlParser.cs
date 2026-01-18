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

using System.Xml.Linq;
using MjlogReader.Decoders;
using MjlogReader.Models;
using MjlogReader.Models.Actions;
using MjlogReader.Models.Results;

namespace MjlogReader.Parsers;

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
        "人和", // 36 (満貫扱い)
        "", "", "", "", "", "", "", "", "", "",　"", "", "", "", "", // 37-51 (未使用)
        "ドラ", "裏ドラ", "赤ドラ" // 52-54
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
    /// XMLコンテンツを解析してMjlogDocumentを生成
    /// </summary>
    public MjlogDocument Parse(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid XML: no root element");

        var document = new MjlogDocument();

        // ルート要素からゲームデータを解析
        ParseGameElement(root, document);

        return document;
    }

    private void ParseGameElement(XElement gameElement, MjlogDocument document)
    {
        MjlogSession? currentSession = null;
        var stepIndex = 0;

        // 巡目計算用の状態
        var turnNumber = 1;
        var discardCountInTurn = 0; // 現在の巡での打牌数
        var dealerId = 0; // 親のプレイヤーID
        var playerCount = 4; // プレイヤー数（三麻対応用）

        // 得点追跡
        int[] currentScores = [25000, 25000, 25000, 25000];

        foreach (var element in gameElement.Elements())
        {
            var name = element.Name.LocalName.ToUpperInvariant();

            try
            {
                switch (name)
                {
                    case "SHUFFLE":
                        document.Header.ShuffleSeed = element.Attribute("seed")?.Value;
                        document.Header.Reference = element.Attribute("ref")?.Value;
                        break;

                    case "GO":
                        ParseGameOptions(element, document.Header);
                        playerCount = document.Header.Rule?.IsThreePlayer == true ? 3 : 4;
                        break;

                    case "UN":
                        ParseUserNames(element, document.Header);
                        break;

                    case "TAIKYOKU":
                        // 対局開始、特に処理なし
                        break;

                    case "INIT":
                        // 新しい局の開始
                        currentSession = ParseInit(element, currentScores);
                        document.Sessions.Add(currentSession);
                        stepIndex = 0;
                        turnNumber = 1;
                        discardCountInTurn = 0;
                        dealerId = currentSession.DealerId;
                        break;

                    case "DORA":
                        if (currentSession != null)
                        {
                            var doraAttr = element.Attribute("hai");
                            if (doraAttr != null && int.TryParse(doraAttr.Value, out var doraId))
                            {
                                var doraTile = TileDecoder.Decode(doraId);
                                currentSession.DoraIndicators.Add(doraTile);

                                // 新ドラとしてステップに追加
                                currentSession.Steps.Add(new MjlogStep
                                {
                                    StepIndex = stepIndex++,
                                    TurnNumber = turnNumber,
                                    PlayerId = -1, // システム行動
                                    Action = new DoraAction { Tile = doraTile }
                                });
                            }
                        }

                        break;

                    case "AGARI":
                        if (currentSession != null)
                        {
                            var agariInfo = ParseAgari(element, currentScores);

                            // 和了ステップを追加
                            currentSession.Steps.Add(new MjlogStep
                            {
                                StepIndex = stepIndex++,
                                TurnNumber = turnNumber,
                                PlayerId = agariInfo.WinnerId,
                                Action = new AgariAction()
                            });

                            // 結果を設定
                            currentSession.Result ??= new MjlogSessionResult { IsAgari = true };
                            currentSession.Result.AgariInfos.Add(agariInfo);
                            Array.Copy(currentScores, currentSession.Result.FinalScores, 4);
                        }

                        break;

                    case "RYUUKYOKU":
                        if (currentSession != null)
                        {
                            var ryuukyokuInfo = ParseRyuukyoku(element, currentScores);

                            // 流局ステップを追加
                            currentSession.Steps.Add(new MjlogStep
                            {
                                StepIndex = stepIndex++,
                                TurnNumber = turnNumber,
                                PlayerId = -1, // システム行動
                                Action = new RyuukyokuAction()
                            });

                            // 結果を設定
                            currentSession.Result = new MjlogSessionResult
                            {
                                IsAgari = false,
                                RyuukyokuInfo = ryuukyokuInfo
                            };
                            Array.Copy(currentScores, currentSession.Result.FinalScores, 4);
                        }

                        break;

                    case "BYE":
                        // プレイヤー切断、特に処理なし
                        break;

                    default:
                        // T0-T3: ツモ、D0-D3: 打牌、N: 鳴き、REACH: リーチ
                        if (currentSession != null)
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

                                        currentSession.Steps.Add(new MjlogStep
                                        {
                                            StepIndex = stepIndex++,
                                            TurnNumber = turnNumber,
                                            PlayerId = playerId,
                                            Action = new DrawAction { Tile = tile }
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

                                        // ツモ切り判定（直前のツモ牌と同じかどうか）
                                        var isTsumogiri = false;
                                        if (currentSession.Steps.Count > 0)
                                        {
                                            var lastStep = currentSession.Steps[^1];
                                            if (lastStep.PlayerId == playerId
                                                && lastStep.Action is DrawAction drawAction
                                                && drawAction.Tile?.OriginalId == tileId)
                                            {
                                                isTsumogiri = true;
                                            }
                                        }

                                        currentSession.Steps.Add(new MjlogStep
                                        {
                                            StepIndex = stepIndex++,
                                            TurnNumber = turnNumber,
                                            PlayerId = playerId,
                                            Action = new DiscardAction
                                            {
                                                Tile = tile,
                                                IsTsumogiri = isTsumogiri
                                            }
                                        });

                                        // 巡目の更新
                                        discardCountInTurn++;
                                        // 全プレイヤーが打牌したら次の巡へ
                                        // （鳴きがあると人数が減るが、親が打牌したタイミングで巡目を進める）
                                        if (playerId == dealerId && discardCountInTurn >= playerCount)
                                        {
                                            turnNumber++;
                                            discardCountInTurn = 0;
                                        }
                                    }
                                }
                            }
                            else if (name == "N")
                            {
                                ParseMeld(element, currentSession, ref stepIndex, turnNumber, ref discardCountInTurn);
                            }
                            else if (name == "REACH")
                            {
                                ParseReach(element, currentSession, ref stepIndex, turnNumber);
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
    }

    private void ParseGameOptions(XElement element, MjlogHeader header)
    {
        var type = int.Parse(element.Attribute("type")?.Value ?? "0");
        var lobby = int.Parse(element.Attribute("lobby")?.Value ?? "0");

        header.Rule = new GameRule
        {
            OriginalFlags = type,
            Lobby = lobby,
            HasRedDora = (type & 0x02) == 0, // bit1: 0=赤あり, 1=赤なし (NOAKA)
            HasOpenTanyao = (type & 0x04) == 0, // bit2: 0=喰いタンあり, 1=なし (NOKUI)
            IsEastOnly = (type & 0x08) == 0, // bit3: 0=東風戦, 1=東南戦 (NAN)
            IsThreePlayer = (type & 0x10) != 0, // bit4: 1=三人麻雀 (SANMA)
            IsFast = (type & 0x40) != 0, // bit6: 1=速卓 (SAKU)
            TierLevel = ((type & 0x20) >> 4) | ((type & 0x80) >> 7) // 0=一般, 1=上級, 2=特上, 3=鳳凰 (TOKU|HIGH)
        };
    }

    private void ParseUserNames(XElement element, MjlogHeader header)
    {
        for (var i = 0; i < 4; i++)
        {
            var nameAttr = element.Attribute($"n{i}");
            if (nameAttr != null)
            {
                header.PlayerNames[i] = Uri.UnescapeDataString(nameAttr.Value);
            }

            var danAttr = element.Attribute("dan");
            if (danAttr != null)
            {
                var dans = danAttr.Value.Split(',');
                if (i < dans.Length && int.TryParse(dans[i], out var danIdx))
                {
                    header.PlayerDans[i] = danIdx < DanNames.Length ? DanNames[danIdx] : $"段位{danIdx}";
                }
            }

            var rateAttr = element.Attribute("rate");
            if (rateAttr != null)
            {
                var rates = rateAttr.Value.Split(',');
                if (i < rates.Length && float.TryParse(rates[i], out var rate))
                {
                    header.PlayerRates[i] = rate;
                }
            }

            var sexAttr = element.Attribute("sx");
            if (sexAttr != null)
            {
                var sexes = sexAttr.Value.Split(',');
                if (i < sexes.Length)
                {
                    header.PlayerSexes[i] = sexes[i];
                }
            }
        }
    }

    private MjlogSession ParseInit(XElement element, int[] currentScores)
    {
        var session = new MjlogSession();

        // 局情報: seed="局,本場,供託,ダイス1,ダイス2,ドラ表示牌"
        var seedAttr = element.Attribute("seed")?.Value;
        if (seedAttr != null)
        {
            var seeds = seedAttr.Split(',');
            if (seeds.Length >= 6)
            {
                session.RoundNumber = int.Parse(seeds[0]);
                session.RoundWind = session.RoundNumber / 4;
                session.Honba = int.Parse(seeds[1]);
                session.Kyotaku = int.Parse(seeds[2]);
                // seeds[3], seeds[4] はダイス
                var doraId = int.Parse(seeds[5]);
                session.InitialDoraIndicator = TileDecoder.Decode(doraId);
                session.DoraIndicators.Add(session.InitialDoraIndicator);
            }
        }

        // 親
        var oyaAttr = element.Attribute("oya")?.Value;
        if (oyaAttr != null)
        {
            session.DealerId = int.Parse(oyaAttr);
        }

        // 得点
        var tenAttr = element.Attribute("ten")?.Value;
        if (tenAttr != null)
        {
            var tens = tenAttr.Split(',');
            for (var i = 0; i < Math.Min(4, tens.Length); i++)
            {
                session.StartScores[i] = int.Parse(tens[i]) * 100;
                currentScores[i] = session.StartScores[i];
            }
        }

        // 配牌
        for (var i = 0; i < 4; i++)
        {
            var haiAttr = element.Attribute($"hai{i}")?.Value;
            if (haiAttr != null)
            {
                session.InitialHands[i] = TileDecoder.DecodeMultiple(haiAttr);
            }
        }

        return session;
    }

    private void ParseMeld(XElement element, MjlogSession session, ref int stepIndex, int turnNumber, ref int discardCountInTurn)
    {
        var whoAttr = element.Attribute("who")?.Value;
        var mAttr = element.Attribute("m")?.Value;

        if (whoAttr == null || mAttr == null) return;

        var playerId = int.Parse(whoAttr);
        var meldCode = int.Parse(mAttr);

        var meld = MeldDecoder.Decode(meldCode, playerId);

        session.Steps.Add(new MjlogStep
        {
            StepIndex = stepIndex++,
            TurnNumber = turnNumber,
            PlayerId = playerId,
            Action = new MeldAction { Meld = meld }
        });

        // 鳴きが発生すると巡の途中でもカウントをリセット（順番がスキップされるため）
        // ただし暗槓・加槓は自分のターンなのでスキップは発生しない
        if (meld.Type != MeldType.AnKan && meld.Type != MeldType.KaKan && meld.Type != MeldType.Nuki)
        {
            discardCountInTurn = 0;
        }
    }

    private void ParseReach(XElement element, MjlogSession session, ref int stepIndex, int turnNumber)
    {
        var whoAttr = element.Attribute("who")?.Value;
        var stepAttr = element.Attribute("step")?.Value;

        if (whoAttr == null) return;

        var playerId = int.Parse(whoAttr);
        var step = int.Parse(stepAttr ?? "1");

        session.Steps.Add(new MjlogStep
        {
            StepIndex = stepIndex++,
            TurnNumber = turnNumber,
            PlayerId = playerId,
            Action = new ReachAction { Step = step }
        });
    }

    private AgariInfo ParseAgari(XElement element, int[] currentScores)
    {
        var agari = new AgariInfo();

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

        return agari;
    }

    private RyuukyokuInfo ParseRyuukyoku(XElement element, int[] currentScores)
    {
        var ryuukyoku = new RyuukyokuInfo();

        // 流局種類
        var typeAttr = element.Attribute("type")?.Value;
        ryuukyoku.Type = typeAttr switch
        {
            "yao9" => RyuukyokuType.NineTerminals, // 九種九牌
            "kaze4" => RyuukyokuType.FourWinds, // 四風連打
            "kan4" => RyuukyokuType.FourKans, // 四槓散了
            "reach4" => RyuukyokuType.FourReach, // 四家立直
            "ron3" => RyuukyokuType.TripleRon, // 三家和了
            "nm" => RyuukyokuType.NagashiMangan, // 流し満貫
            _ => RyuukyokuType.Exhaustive // 通常流局
        };

        // テンパイ者: hai0="牌,牌,..." (存在すればテンパイ)
        for (var i = 0; i < 4; i++)
        {
            var haiAttr = element.Attribute($"hai{i}")?.Value;
            if (haiAttr != null && !string.IsNullOrEmpty(haiAttr))
            {
                ryuukyoku.TenpaiPlayerIds.Add(i);
            }
        }

        // 点数移動: sc="P0後点,P0変動,P1後点,P1変動,..."
        var scAttr = element.Attribute("sc")?.Value;
        if (scAttr != null)
        {
            var scs = scAttr.Split(',');
            for (var i = 0; i + 1 < scs.Length; i += 2)
            {
                var baseScore = int.Parse(scs[i]) * 100;
                var change = int.Parse(scs[i + 1]) * 100;
                ryuukyoku.ScoreChanges[i / 2] = change;
                currentScores[i / 2] = baseScore + change;
            }
        }

        return ryuukyoku;
    }
}