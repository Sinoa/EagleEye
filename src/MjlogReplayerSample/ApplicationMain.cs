// zlib License
// 
// Copyright (c) 2026 Sinoa
// 
// This software is provided ‘as-is’, without any express or implied
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

using System.CommandLine;
using Foxtamp.MjlogReader;
using Foxtamp.MjlogReplayer;
using Foxtamp.MjlogReplayer.Models;

namespace Foxtamp.MjlogReplayerSample;

/// <summary>
/// MjlogReplayerサンプルアプリケーションのエントリポイント
/// </summary>
public static class ApplicationMain
{
    /// <summary>
    /// アプリケーションのエントリポイント
    /// </summary>
    /// <param name="args">コマンドライン引数</param>
    /// <returns>終了コード</returns>
    public static async Task<int> Main(string[] args)
    {
        var fileArgument = new Argument<FileInfo?>("file")
        {
            Description = "牌譜ファイルのパス（.mjlog または .xml）",
            Arity = ArgumentArity.ZeroOrOne
        };

        var xmlOption = new Option<string?>("-x", "--xml")
        {
            Description = "XML形式の牌譜文字列を直接指定"
        };

        var verboseOption = new Option<bool>("-v", "--verbose")
        {
            Description = "全ステップの詳細を表示"
        };

        var sessionOption = new Option<int?>("-s", "--session")
        {
            Description = "特定セッション（局）のみ表示（0から開始）"
        };

        var stepOption = new Option<int?>("--step")
        {
            Description = "特定ステップのみ表示（-s/--sessionと併用）"
        };

        var rootCommand = new RootCommand("MjlogReplayer サンプルアプリケーション - 天鳳牌譜を再現して試合状態を表示します")
        {
            fileArgument,
            xmlOption,
            verboseOption,
            sessionOption,
            stepOption
        };

        rootCommand.SetHandler(context =>
        {
            var file = context.ParseResult.GetValueForArgument(fileArgument);
            var xml = context.ParseResult.GetValueForOption(xmlOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var session = context.ParseResult.GetValueForOption(sessionOption);
            var step = context.ParseResult.GetValueForOption(stepOption);
            HandleCommand(file, xml, verbose, session, step);
        });
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// コマンドのハンドラー
    /// </summary>
    private static void HandleCommand(FileInfo? file, string? xml, bool verbose, int? sessionIndex, int? stepIndex)
    {
        // 入力バリデーション
        if (file != null && xml != null)
        {
            Console.Error.WriteLine("エラー: ファイルパスと --xml オプションは同時に指定できません。");
            return;
        }

        if (file == null && xml == null)
        {
            Console.Error.WriteLine("エラー: ファイルパスまたは --xml オプションのいずれかを指定してください。");
            return;
        }

        if (stepIndex.HasValue && !sessionIndex.HasValue)
        {
            Console.Error.WriteLine("エラー: --step オプションは -s/--session オプションと併用してください。");
            return;
        }

        try
        {
            // 牌譜の読み込み
            var document = xml != null
                ? MjlogDocumentReader.Parse(xml)
                : MjlogDocumentReader.Load(file!.FullName);

            if (xml != null)
            {
                Console.WriteLine("XML文字列から牌譜を読み込みました。");
            }
            else
            {
                if (!file!.Exists)
                {
                    Console.Error.WriteLine($"エラー: ファイルが見つかりません: {file.FullName}");
                    return;
                }

                Console.WriteLine($"ファイルから牌譜を読み込みました: {file.Name}");
            }

            Console.WriteLine();

            // 牌譜を再現
            var replayer = new DocumentReplayer(document);
            Console.WriteLine($"=== 牌譜再現結果 ({replayer.SessionCount}局) ===");
            Console.WriteLine();

            // 特定セッション・ステップの表示
            if (sessionIndex.HasValue)
            {
                if (sessionIndex.Value < 0 || sessionIndex.Value >= replayer.SessionCount)
                {
                    Console.Error.WriteLine($"エラー: セッションインデックスは0から{replayer.SessionCount - 1}の範囲で指定してください。");
                    return;
                }

                var session = replayer[sessionIndex.Value];

                if (stepIndex.HasValue)
                {
                    if (stepIndex.Value < 0 || stepIndex.Value >= session.StepCount)
                    {
                        Console.Error.WriteLine($"エラー: ステップインデックスは0から{session.StepCount - 1}の範囲で指定してください。");
                        return;
                    }

                    // 特定ステップのみ表示
                    var state = session[stepIndex.Value];
                    Console.WriteLine($"--- セッション {sessionIndex.Value}, ステップ {stepIndex.Value} ---");
                    DisplayGameStateDetail(state);
                }
                else
                {
                    // 特定セッションの全ステップまたは要約を表示
                    DisplaySession(session, sessionIndex.Value, verbose);
                }
            }
            else
            {
                // 全セッションを表示
                for (var i = 0; i < replayer.SessionCount; i++)
                {
                    DisplaySession(replayer[i], i, verbose);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"エラー: 牌譜の読み込みまたは再現に失敗しました。");
            Console.Error.WriteLine($"  詳細: {ex.Message}");
        }
    }

    /// <summary>
    /// セッションを表示
    /// </summary>
    private static void DisplaySession(SessionReplayer session, int sessionIndex, bool verbose)
    {
        var lastState = session[session.StepCount - 1];
        var honbaStr = lastState.Honba > 0 ? $" {lastState.Honba}本場" : "";

        Console.WriteLine($"--- セッション {sessionIndex}: {lastState.RoundName}{honbaStr} ---");
        Console.WriteLine($"  ステップ数: {session.StepCount}");

        if (verbose)
        {
            // 全ステップを表示
            Console.WriteLine();
            for (var i = 0; i < session.StepCount; i++)
            {
                DisplayStepSummary(session[i], i);
            }
        }
        else
        {
            // 最終状態のみ表示
            Console.WriteLine();
            Console.WriteLine("  [最終状態]");
            DisplayGameStateSummary(lastState);
        }

        Console.WriteLine();
    }

    /// <summary>
    /// ステップの要約を表示
    /// </summary>
    private static void DisplayStepSummary(GameState state, int stepIndex)
    {
        Console.Write($"  [ステップ {stepIndex,3}] 巡目 {state.TurnNumber,2}");
        Console.WriteLine($"  == 詳細 ==> {(state.SourceStep != null ? state.SourceStep.ToString() : "(初期状態)")}");

        for (var i = 0; i < state.PlayerCount; i++)
        {
            var player = state.GetPlayer(i);
            var handStr = string.Join(",", player.Hand.OrderBy(x => x.OriginalId).Select(t => t.DisplayName));
            var discardStr = string.Join(",", player.Discards.Select(d => d.Tile.DisplayName));
            var reachStr = player.IsReach ? " [リーチ]" : "";
            Console.WriteLine($"    P{i}: 手牌[{handStr}]{reachStr}");
            Console.WriteLine($"        捨て牌[{discardStr}]");
        }
    }

    /// <summary>
    /// GameStateの要約を表示
    /// </summary>
    private static void DisplayGameStateSummary(GameState state)
    {
        Console.WriteLine($"    巡目: {state.TurnNumber}");

        // ドラ表示
        if (state.DoraIndicators.Count > 0)
        {
            var doraStr = string.Join("", state.DoraIndicators.Select(t => t.DisplayName));
            Console.WriteLine($"    ドラ表示牌: {doraStr}");
        }

        Console.WriteLine();

        for (var i = 0; i < state.PlayerCount; i++)
        {
            var player = state.GetPlayer(i);
            DisplayPlayerStateSummary(player);
        }
    }

    /// <summary>
    /// PlayerStateの要約を表示
    /// </summary>
    private static void DisplayPlayerStateSummary(PlayerState player)
    {
        var handStr = string.Join("", player.Hand.Select(t => t.DisplayName));
        var discardStr = string.Join("", player.Discards.Select(d => d.Tile.DisplayName));
        var reachStr = player.IsReach ? " [リーチ]" : "";

        Console.WriteLine($"    P{player.PlayerId}: {player.Score}点{reachStr}");
        Console.WriteLine($"      手牌: {handStr}");
        Console.WriteLine($"      捨牌: {discardStr}");

        if (player.Melds.Count > 0)
        {
            var meldStr = string.Join(", ", player.Melds.Select(m => m.ToString()));
            Console.WriteLine($"      副露: {meldStr}");
        }
    }

    /// <summary>
    /// GameStateの詳細を表示
    /// </summary>
    private static void DisplayGameStateDetail(GameState state)
    {
        Console.WriteLine($"  局: {state.RoundName}");
        Console.WriteLine($"  本場: {state.Honba}");
        Console.WriteLine($"  供託: {state.Kyotaku}");
        Console.WriteLine($"  親: P{state.DealerId}");
        Console.WriteLine($"  巡目: {state.TurnNumber}");
        Console.WriteLine($"  ステップ: {state.StepIndex}");

        // ドラ表示
        if (state.DoraIndicators.Count > 0)
        {
            var doraStr = string.Join("", state.DoraIndicators.Select(t => t.DisplayName));
            Console.WriteLine($"  ドラ表示牌: {doraStr}");
        }

        Console.WriteLine();
        Console.WriteLine("  [プレイヤー状態]");

        for (var i = 0; i < state.PlayerCount; i++)
        {
            var player = state.GetPlayer(i);
            DisplayPlayerStateDetail(player);
        }
    }

    /// <summary>
    /// PlayerStateの詳細を表示
    /// </summary>
    private static void DisplayPlayerStateDetail(PlayerState player)
    {
        var reachStr = player.IsReach ? " [リーチ]" : "";
        Console.WriteLine($"  --- P{player.PlayerId}: {player.Score}点{reachStr} ---");

        // 手牌
        var handStr = string.Join("", player.Hand.Select(t => t.DisplayName));
        Console.WriteLine($"    手牌: {handStr}");

        // 捨て牌（詳細）
        if (player.Discards.Count > 0)
        {
            Console.WriteLine("    捨牌:");
            for (var i = 0; i < player.Discards.Count; i++)
            {
                var discard = player.Discards[i];
                var tsumogiriStr = discard.IsTsumogiri ? " (ツモ切り)" : "";
                var reachDeclareStr = discard.IsReachDeclare ? " (リーチ宣言)" : "";
                var calledStr = discard.CalledByPlayerId.HasValue ? $" (P{discard.CalledByPlayerId.Value}が鳴き)" : "";
                Console.WriteLine($"      [{i + 1,2}] {discard.Tile.DisplayName}{tsumogiriStr}{reachDeclareStr}{calledStr}");
            }
        }
        else
        {
            Console.WriteLine("    捨牌: (なし)");
        }

        // 副露
        if (player.Melds.Count > 0)
        {
            Console.WriteLine("    副露:");
            foreach (var meld in player.Melds)
            {
                Console.WriteLine($"      {meld}");
            }
        }
    }
}