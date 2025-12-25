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

using System.Diagnostics;
using System.Text;
using MjlogA.Formatters;
using MjlogJ;

namespace MjlogAnalyzer;

/// <summary>
/// 牌譜データ分析ツール メインクラス
/// </summary>
public static class ApplicationMain
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var options = CommandLineOptions.Parse(args);

            if (options.ShowHelp)
            {
                CommandLineOptions.ShowHelpMessage(Console.Out);
                return 0;
            }

            if (string.IsNullOrEmpty(options.DirectoryPath))
            {
                Console.Error.WriteLine("エラー: ディレクトリパスを指定してください。");
                Console.Error.WriteLine("ヘルプを表示するには -h または --help を使用してください。");
                return 1;
            }

            if (!Directory.Exists(options.DirectoryPath))
            {
                Console.Error.WriteLine($"エラー: ディレクトリが存在しません: {options.DirectoryPath}");
                return 1;
            }

            return await RunAnalysis(options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"致命的エラー: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RunAnalysis(CommandLineOptions options)
    {
        var analyzer = new MjlogA.Analyzers.MjlogAnalyzer();
        var stopwatch = Stopwatch.StartNew();

        // ファイル一覧を取得
        var searchOption = options.IncludeSubdirectories
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var files = Directory.GetFiles(options.DirectoryPath, "*.mjlog", searchOption)
            .Concat(Directory.GetFiles(options.DirectoryPath, "*.xml", searchOption))
            .ToArray();

        if (files.Length == 0)
        {
            Console.Error.WriteLine("警告: 牌譜ファイルが見つかりませんでした。");
            return 0;
        }

        if (options.ShowProgress)
        {
            Console.Error.WriteLine($"対象ファイル数: {files.Length}");
        }

        var processedCount = 0;
        var errorCount = 0;

        var readerOptions = new MjlogReaderOptions
        {
            ContinueOnError = options.ContinueOnError
        };

        await foreach (var result in MjlogReader.LoadManyParallelAsync(files, readerOptions))
        {
            processedCount++;

            if (result.Error != null)
            {
                errorCount++;
                if (options.ShowProgress)
                {
                    Console.Error.WriteLine($"エラー: {result.FilePath} - {result.Error.Message}");
                }

                continue;
            }

            if (result.Record != null)
            {
                analyzer.Analyze(result.Record);
            }

            if (options.ShowProgress && processedCount % 100 == 0)
            {
                Console.Error.WriteLine($"進捗: {processedCount}/{files.Length} ({100.0 * processedCount / files.Length:F1}%)");
            }
        }

        stopwatch.Stop();

        if (options.ShowProgress)
        {
            Console.Error.WriteLine($"処理完了: {processedCount} ファイル, エラー: {errorCount}, 所要時間: {stopwatch.Elapsed.TotalSeconds:F2}秒");
        }

        // 結果を出力
        var analysisResult = analyzer.GetResult();

        if (string.IsNullOrEmpty(options.OutputPath))
        {
            // 標準出力へ
            CsvFormatter.Write(analysisResult, Console.Out);
        }
        else
        {
            // ファイルへ出力
            await using var writer = new StreamWriter(options.OutputPath, false, Encoding.UTF8);
            CsvFormatter.Write(analysisResult, writer);

            if (options.ShowProgress)
            {
                Console.Error.WriteLine($"結果を出力しました: {options.OutputPath}");
            }
        }

        return 0;
    }
}