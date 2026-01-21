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
using System.CommandLine.Parsing;
using Foxtamp.MjlogReader;
using Foxtamp.MjlogReader.Models;
using Foxtamp.MjlogReader.Models.Actions;

namespace Foxtamp.MjlogReaderSample;

/// <summary>
/// MjlogReaderサンプルアプリケーションのエントリポイント
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
            Description = "詳細な行動ステップを表示"
        };

        var rootCommand = new RootCommand("MjlogReader サンプルアプリケーション - 天鳳牌譜を読み込んで表示します")
        {
            fileArgument,
            xmlOption,
            verboseOption
        };

        rootCommand.SetHandler(HandleCommand, fileArgument, xmlOption, verboseOption);
        return await rootCommand.Parse(args).InvokeAsync();
    }

    /// <summary>
    /// コマンドのハンドラー
    /// </summary>
    private static void HandleCommand(FileInfo? file, string? xml, bool verbose)
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

        try
        {
            // 牌譜の読み込み
            MjlogDocument document;
            if (xml != null)
            {
                document = MjlogDocumentReader.Parse(xml);
                Console.WriteLine("XML文字列から牌譜を読み込みました。");
            }
            else
            {
                if (!file!.Exists)
                {
                    Console.Error.WriteLine($"エラー: ファイルが見つかりません: {file.FullName}");
                    return;
                }

                document = MjlogDocumentReader.Load(file.FullName);
                Console.WriteLine($"ファイルから牌譜を読み込みました: {file.Name}");
            }

            Console.WriteLine();

            // ヘッダー情報の表示
            DisplayHeader(document);

            // セッション情報の表示
            DisplaySessions(document, verbose);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"エラー: 牌譜の読み込みに失敗しました。");
            Console.Error.WriteLine($"  詳細: {ex.Message}");
        }
    }

    /// <summary>
    /// ヘッダー情報を表示
    /// </summary>
    private static void DisplayHeader(MjlogDocument document)
    {
        var header = document.Header;

        Console.WriteLine("=== 対局情報 ===");
        Console.WriteLine($"プレイヤー人数: {document.PlayerCount}人打ち");
        Console.WriteLine();

        Console.WriteLine("--- プレイヤー ---");
        for (var i = 0; i < header.PlayerCount; i++)
        {
            var name = header.PlayerNames[i];
            var dan = header.PlayerDans[i];
            var rate = header.PlayerRates[i];
            Console.WriteLine($"  P{i}: {name} ({dan}, R{rate:F2})");
        }

        if (header.Rule != null)
        {
            Console.WriteLine();
            Console.WriteLine("--- ルール ---");
            Console.WriteLine($"  {header.Rule}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// セッション情報を表示
    /// </summary>
    private static void DisplaySessions(MjlogDocument document, bool verbose)
    {
        Console.WriteLine($"=== 対局結果 ({document.Sessions.Count}局) ===");
        Console.WriteLine();

        foreach (var session in document.Sessions)
        {
            DisplaySession(session, verbose);
        }
    }

    /// <summary>
    /// 個別のセッション情報を表示
    /// </summary>
    private static void DisplaySession(MjlogSession session, bool verbose)
    {
        // 局の基本情報
        var honbaStr = session.Honba > 0 ? $" {session.Honba}本場" : "";
        var kyotakuStr = session.Kyotaku > 0 ? $" (供託{session.Kyotaku}本)" : "";
        Console.WriteLine($"--- {session.RoundName}{honbaStr}{kyotakuStr} ---");
        Console.WriteLine($"  親: P{session.DealerId}");

        // 開始時得点
        var scoreStrs = session.StartScores.Select((s, i) => $"P{i}:{s}").ToArray();
        Console.WriteLine($"  開始時得点: {string.Join(", ", scoreStrs)}");

        // ドラ表示
        if (session.DoraIndicators.Count > 0)
        {
            var doraStrs = session.DoraIndicators.Select(t => t.DisplayName).ToArray();
            Console.WriteLine($"  ドラ表示牌: {string.Join(", ", doraStrs)}");
        }

        // 詳細モードの場合、行動ステップを表示
        if (verbose)
        {
            DisplaySteps(session);
        }

        // 結果の表示
        DisplayResult(session);

        Console.WriteLine();
    }

    /// <summary>
    /// 行動ステップを表示（詳細モード）
    /// </summary>
    private static void DisplaySteps(MjlogSession session)
    {
        Console.WriteLine();
        Console.WriteLine("  [行動ステップ]");

        foreach (var step in session.Steps)
        {
            var playerStr = step.PlayerId >= 0 ? $"P{step.PlayerId}" : "SYS";
            var actionStr = step.Action switch
            {
                DrawAction draw => $"ツモ: {draw.Tile?.DisplayName ?? "?"}",
                DiscardAction discard => $"打牌: {discard.Tile?.DisplayName ?? "?"}{(discard.IsTsumogiri ? " (ツモ切り)" : "")}",
                MeldAction meld => $"鳴き: {meld.Meld}",
                ReachAction reach => $"リーチ: {(reach.IsAccepted ? "成立" : "宣言")}",
                DoraAction => "新ドラ",
                _ => step.Action.ToString()
            };

            Console.WriteLine($"    [{step.StepIndex,3}] 巡目{step.TurnNumber,2} {playerStr}: {actionStr}");
        }
    }

    /// <summary>
    /// セッションの結果を表示
    /// </summary>
    private static void DisplayResult(MjlogSession session)
    {
        var result = session.Result;
        if (result == null)
        {
            Console.WriteLine("  結果: (不明)");
            return;
        }

        Console.WriteLine();
        if (result.IsAgari)
        {
            foreach (var agari in result.AgariInfos)
            {
                var agariType = agari.IsTsumo ? "ツモ" : "ロン";
                Console.WriteLine($"  結果: {agariType} - P{agari.WinnerId}の和了");
                Console.WriteLine($"    得点: {agari.Score}点");

                if (agari.Yakus.Count > 0)
                {
                    var yakuStrs = agari.Yakus.Select(y => $"{y.Name}({y.Han}翻)").ToArray();
                    Console.WriteLine($"    役: {string.Join(", ", yakuStrs)}");
                }
            }
        }
        else if (result.RyuukyokuInfo != null)
        {
            Console.WriteLine($"  結果: 流局 - {result.RyuukyokuInfo.Type}");
        }

        // 終局後得点
        var finalScoreStrs = result.FinalScores.Select((s, i) => $"P{i}:{s}").ToArray();
        Console.WriteLine($"  終局時得点: {string.Join(", ", finalScoreStrs)}");
    }
}