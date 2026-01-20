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
using Foxtamp.MjlogReader.Decoders;
using Foxtamp.MjlogReader.Models;
using Foxtamp.MjlogReader.Models.Actions;
using Foxtamp.MjlogReader.Models.Results;

namespace Foxtamp.MjlogReader.Parsers;

/// <summary>
/// 天鳳の牌譜XMLを解析するパーサー
/// </summary>
public class MjlogXmlParser
{
    /// <summary>
    /// 解析中の状態を管理するコンテキスト
    /// </summary>
    private class ParseContext
    {
        public MjlogSession? CurrentSession { get; set; }
        public int StepIndex { get; set; }
        public int TurnNumber { get; set; } = 1;
        public int DiscardCountInTurn { get; set; }
        public int DealerId { get; set; }
        public int PlayerCount { get; }
        public int[] CurrentScores { get; }

        public ParseContext(int playerCount)
        {
            PlayerCount = playerCount;
            // 得点追跡（三麻は35000点、四麻は25000点スタート）
            var initialScore = playerCount == 3 ? 35000 : 25000;
            CurrentScores = new int[playerCount];
            for (var i = 0; i < playerCount; i++)
            {
                CurrentScores[i] = initialScore;
            }
        }

        /// <summary>
        /// 新しい局の開始時に状態をリセット
        /// </summary>
        public void ResetForNewSession(MjlogSession session)
        {
            CurrentSession = session;
            StepIndex = 0;
            TurnNumber = 1;
            DiscardCountInTurn = 0;
            DealerId = session.DealerId;
        }

        /// <summary>
        /// ステップを追加し、StepIndexをインクリメント
        /// </summary>
        public void AddStep(int playerId, MjlogAction action)
        {
            CurrentSession?.Steps.Add(new MjlogStep
            {
                StepIndex = StepIndex++,
                TurnNumber = TurnNumber,
                PlayerId = playerId,
                Action = action
            });
        }

        /// <summary>
        /// 打牌後の巡目更新処理
        /// </summary>
        public void UpdateTurnAfterDiscard(int playerId)
        {
            DiscardCountInTurn++;
            // 全プレイヤーが打牌したら次の巡へ
            // （鳴きがあると人数が減るが、親が打牌したタイミングで巡目を進める）
            if (playerId == DealerId && DiscardCountInTurn >= PlayerCount)
            {
                TurnNumber++;
                DiscardCountInTurn = 0;
            }
        }
    }

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

        // 先にGOタグを解析してプレイヤー数を確定
        var playerCount = DeterminePlayerCount(root);
        var document = new MjlogDocument(playerCount);

        // ルート要素からゲームデータを解析
        ParseGameElement(root, document);

        return document;
    }

    /// <summary>
    /// GOタグを解析してプレイヤー数を確定
    /// </summary>
    private int DeterminePlayerCount(XElement gameElement)
    {
        var goElement = gameElement.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("GO", StringComparison.OrdinalIgnoreCase));
        if (goElement == null) return 4; // デフォルトは4人麻雀

        var type = int.Parse(goElement.Attribute("type")?.Value ?? "0");
        var isThreePlayer = (type & 0x10) != 0; // bit4: 1=三人麻雀
        return isThreePlayer ? 3 : 4;
    }

    private void ParseGameElement(XElement gameElement, MjlogDocument document)
    {
        var context = new ParseContext(document.PlayerCount);

        foreach (var element in gameElement.Elements())
        {
            var name = element.Name.LocalName.ToUpperInvariant();

            try
            {
                switch (name)
                {
                    case "SHUFFLE":
                        ParseShuffle(element, document.Header);
                        break;

                    case "GO":
                        ParseGameOptions(element, document.Header);
                        break;

                    case "UN":
                        ParseUserNames(element, document.Header);
                        break;

                    case "TAIKYOKU":
                        // 対局開始、特に処理なし
                        break;

                    case "INIT":
                        ParseSessionInit(element, document, context);
                        break;

                    case "DORA":
                        ParseDora(element, context);
                        break;

                    case "AGARI":
                        ParseAgariStep(element, context);
                        break;

                    case "RYUUKYOKU":
                        ParseRyuukyokuStep(element, context);
                        break;

                    case "BYE":
                        // プレイヤー切断、特に処理なし
                        break;

                    case "N":
                        ParseMeld(element, context);
                        break;

                    case "REACH":
                        ParseReach(element, context);
                        break;

                    default:
                        // ツモ・打牌の処理
                        ParseTileAction(name, element, context);
                        break;
                }
            }
            catch (Exception)
            {
                // 個別要素の解析エラーは無視して続行
            }
        }
    }

    private static void ParseShuffle(XElement element, MjlogHeader header)
    {
        header.ShuffleSeed = element.Attribute("seed")?.Value;
        header.Reference = element.Attribute("ref")?.Value;
    }

    private void ParseSessionInit(XElement element, MjlogDocument document, ParseContext context)
    {
        var session = ParseInit(element, context.CurrentScores, context.PlayerCount);
        document.Sessions.Add(session);
        context.ResetForNewSession(session);
    }

    private static void ParseDora(XElement element, ParseContext context)
    {
        if (context.CurrentSession == null) return;

        var doraAttr = element.Attribute("hai");
        if (doraAttr == null || !int.TryParse(doraAttr.Value, out var doraId)) return;

        var doraTile = TileDecoder.Decode(doraId);
        context.CurrentSession.DoraIndicators.Add(doraTile);
        context.AddStep(-1, new DoraAction { Tile = doraTile });
    }

    private void ParseAgariStep(XElement element, ParseContext context)
    {
        if (context.CurrentSession == null) return;

        var agariInfo = ParseAgari(element, context.CurrentScores, context.PlayerCount);
        context.AddStep(agariInfo.WinnerId, new AgariAction());

        // 結果を設定
        context.CurrentSession.Result ??= new MjlogSessionResult(context.PlayerCount) { IsAgari = true };
        context.CurrentSession.Result.AgariInfos.Add(agariInfo);
        Array.Copy(context.CurrentScores, context.CurrentSession.Result.FinalScores, context.PlayerCount);
    }

    private void ParseRyuukyokuStep(XElement element, ParseContext context)
    {
        if (context.CurrentSession == null) return;

        var ryuukyokuInfo = ParseRyuukyoku(element, context.CurrentScores, context.PlayerCount);
        context.AddStep(-1, new RyuukyokuAction());

        // 結果を設定
        context.CurrentSession.Result = new MjlogSessionResult(context.PlayerCount)
        {
            IsAgari = false,
            RyuukyokuInfo = ryuukyokuInfo
        };
        Array.Copy(context.CurrentScores, context.CurrentSession.Result.FinalScores, context.PlayerCount);
    }

    private static void ParseTileAction(string name, XElement element, ParseContext context)
    {
        if (context.CurrentSession == null || name.Length < 1) return;

        var firstChar = name[0];

        switch (firstChar)
        {
            case 'T':
            case 'U':
            case 'V':
            case 'W':
                ParseDraw(name, element, context, firstChar);
                break;

            case 'D':
            case 'E':
            case 'F':
            case 'G':
                ParseDiscard(name, element, context, firstChar);
                break;
        }
    }

    private static void ParseDraw(string name, XElement element, ParseContext context, char firstChar)
    {
        // ツモ: T=0, U=1, V=2, W=3
        var playerId = firstChar switch { 'T' => 0, 'U' => 1, 'V' => 2, 'W' => 3, _ => -1 };
        if (playerId < 0) return;

        var tileIdStr = name.Length > 1 ? name[1..] : element.Value;
        if (!int.TryParse(tileIdStr, out var tileId)) return;

        var tile = TileDecoder.Decode(tileId);
        context.AddStep(playerId, new DrawAction { Tile = tile });
    }

    private static void ParseDiscard(string name, XElement element, ParseContext context, char firstChar)
    {
        // 打牌: D=0, E=1, F=2, G=3
        var playerId = firstChar switch { 'D' => 0, 'E' => 1, 'F' => 2, 'G' => 3, _ => -1 };
        if (playerId < 0) return;

        var tileIdStr = name.Length > 1 ? name[1..] : element.Value;
        if (!int.TryParse(tileIdStr, out var tileId)) return;

        var tile = TileDecoder.Decode(tileId);

        // ツモ切り判定（直前のツモ牌と同じかどうか）
        var isTsumogiri = false;
        if (context.CurrentSession!.Steps.Count > 0)
        {
            var lastStep = context.CurrentSession.Steps[^1];
            if (lastStep.PlayerId == playerId
                && lastStep.Action is DrawAction drawAction
                && drawAction.Tile?.OriginalId == tileId)
            {
                isTsumogiri = true;
            }
        }

        context.AddStep(playerId, new DiscardAction
        {
            Tile = tile,
            IsTsumogiri = isTsumogiri
        });

        context.UpdateTurnAfterDiscard(playerId);
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
        for (var i = 0; i < header.PlayerCount; i++)
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

    private MjlogSession ParseInit(XElement element, int[] currentScores, int playerCount)
    {
        var session = new MjlogSession(playerCount);

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
            for (var i = 0; i < Math.Min(playerCount, tens.Length); i++)
            {
                session.StartScores[i] = int.Parse(tens[i]) * 100;
                currentScores[i] = session.StartScores[i];
            }
        }

        // 配牌
        for (var i = 0; i < playerCount; i++)
        {
            var haiAttr = element.Attribute($"hai{i}")?.Value;
            if (haiAttr != null)
            {
                session.InitialHands[i] = TileDecoder.DecodeMultiple(haiAttr);
            }
        }

        return session;
    }

    private static void ParseMeld(XElement element, ParseContext context)
    {
        if (context.CurrentSession == null) return;

        var whoAttr = element.Attribute("who")?.Value;
        var mAttr = element.Attribute("m")?.Value;

        if (whoAttr == null || mAttr == null) return;

        var playerId = int.Parse(whoAttr);
        var meldCode = int.Parse(mAttr);

        var meld = MeldDecoder.Decode(meldCode, playerId);
        context.AddStep(playerId, new MeldAction { Meld = meld });

        // 鳴きが発生すると巡の途中でもカウントをリセット（順番がスキップされるため）
        // ただし暗槓・加槓は自分のターンなのでスキップは発生しない
        if (meld.Type != MeldType.AnKan && meld.Type != MeldType.KaKan && meld.Type != MeldType.Nuki)
        {
            context.DiscardCountInTurn = 0;
        }
    }

    private static void ParseReach(XElement element, ParseContext context)
    {
        if (context.CurrentSession == null) return;

        var whoAttr = element.Attribute("who")?.Value;
        var stepAttr = element.Attribute("step")?.Value;

        if (whoAttr == null) return;

        var playerId = int.Parse(whoAttr);
        var step = int.Parse(stepAttr ?? "1");

        context.AddStep(playerId, new ReachAction { Step = step });
    }

    private AgariInfo ParseAgari(XElement element, int[] currentScores, int playerCount)
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
                var playerIndex = i / 2;
                if (playerIndex >= playerCount) break; // プレイヤー数を超えたらスキップ

                var baseScore = int.Parse(scs[i]) * 100;
                var change = int.Parse(scs[i + 1]) * 100;
                agari.ScoreChanges[playerIndex] = change;
                currentScores[playerIndex] = baseScore + change;
            }
        }

        return agari;
    }

    private RyuukyokuInfo ParseRyuukyoku(XElement element, int[] currentScores, int playerCount)
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
        for (var i = 0; i < playerCount; i++)
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
                var playerIndex = i / 2;
                if (playerIndex >= playerCount) break; // プレイヤー数を超えたらスキップ

                var baseScore = int.Parse(scs[i]) * 100;
                var change = int.Parse(scs[i + 1]) * 100;
                ryuukyoku.ScoreChanges[playerIndex] = change;
                currentScores[playerIndex] = baseScore + change;
            }
        }

        return ryuukyoku;
    }
}