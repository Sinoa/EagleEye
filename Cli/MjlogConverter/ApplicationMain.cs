// zlib License
// 
// Copyright (c) 2025 Sinoa
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

using MjlogConverter.Utils;
using MjlogJ;
using MjlogJ.Formatters;

namespace MjlogConverter;

/// <summary>
/// 天鳳牌譜変換ツール メインクラス
/// </summary>
public static class ApplicationMain
{
    public static int Main(string[] args)
    {
        try
        {
            var options = ParseArguments(args);

            if (options.ShowHelp)
            {
                ShowHelp();
                return 0;
            }

            if (string.IsNullOrEmpty(options.InputPath))
            {
                Console.Error.WriteLine("エラー: 入力パスを指定してください。");
                ShowHelp();
                return 1;
            }

            return ProcessFiles(options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"致命的エラー: {ex.Message}");
            return 1;
        }
    }

    private static CommandLineOptions ParseArguments(string[] args)
    {
        var options = new CommandLineOptions();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "-i":
                case "--input":
                    if (i + 1 < args.Length)
                    {
                        options.InputPath = args[++i];
                    }

                    break;

                case "-o":
                case "--output":
                    if (i + 1 < args.Length)
                    {
                        options.OutputPath = args[++i];
                    }

                    break;

                case "-p":
                case "--progress":
                    options.ShowProgress = true;
                    break;

                case "-h":
                case "--help":
                    options.ShowHelp = true;
                    break;

                default:
                    // 引数なしで指定された場合は入力パスとみなす
                    if (!args[i].StartsWith("-") && string.IsNullOrEmpty(options.InputPath))
                    {
                        options.InputPath = args[i];
                    }

                    break;
            }
        }

        return options;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("""
                          天鳳牌譜変換ツール (MjlogConverter)

                          使用方法:
                            MjlogConverter -i <入力パス> [-o <出力パス>] [-p]

                          オプション:
                            -i, --input <パス>    入力ファイルまたはディレクトリのパス (必須)
                            -o, --output <パス>   出力先ディレクトリのパス (省略時: 入力と同じ場所)
                            -p, --progress        進捗表示を有効にする
                            -h, --help            このヘルプを表示

                          例:
                            MjlogConverter -i game.xml -o output/
                            MjlogConverter -i logs/ -o converted/ -p
                            MjlogConverter game.xml
                          """);
    }

    private static int ProcessFiles(CommandLineOptions options)
    {
        IOutputFormatter formatter = new JsonOutputFormatter();
        var progress = new ProgressReporter(options.ShowProgress);

        // 入力ファイルリストを取得
        var inputFiles = GetInputFiles(options.InputPath!);

        if (inputFiles.Count == 0)
        {
            Console.Error.WriteLine("エラー: 処理対象のXMLファイルが見つかりません。");
            return 1;
        }

        // 出力ディレクトリを決定
        var outputDir = DetermineOutputDirectory(options);

        var processedCount = 0;
        var errorCount = 0;

        for (var i = 0; i < inputFiles.Count; i++)
        {
            var inputFile = inputFiles[i];
            var fileName = Path.GetFileName(inputFile);

            progress.Report(fileName, i + 1, inputFiles.Count);

            try
            {
                // MjlogReader APIを使用してパース
                var record = MjlogReader.Load(inputFile);

                // 出力ファイルパスを決定
                var outputFile = DetermineOutputFilePath(inputFile, options.InputPath!, outputDir, formatter.FileExtension);

                // 出力ディレクトリを作成
                var outputFileDir = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrEmpty(outputFileDir) && !Directory.Exists(outputFileDir))
                {
                    Directory.CreateDirectory(outputFileDir);
                }

                // 出力
                using var outputStream = File.Create(outputFile);
                formatter.Format(record, outputStream);

                processedCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                progress.ReportError(fileName, ex.Message);
            }
        }

        progress.Complete(processedCount, errorCount);

        return errorCount > 0 ? 1 : 0;
    }

    private static List<string> GetInputFiles(string inputPath)
    {
        if (File.Exists(inputPath))
        {
            // 単一ファイル
            return [inputPath];
        }

        if (Directory.Exists(inputPath))
        {
            // ディレクトリ内のXMLファイルを再帰的に取得
            return Directory.GetFiles(inputPath, "*.xml", SearchOption.AllDirectories)
                .OrderBy(f => f)
                .ToList();
        }

        return [];
    }

    private static string DetermineOutputDirectory(CommandLineOptions options)
    {
        if (!string.IsNullOrEmpty(options.OutputPath))
        {
            return options.OutputPath;
        }

        // 入力がファイルの場合はそのディレクトリ
        if (File.Exists(options.InputPath))
        {
            return Path.GetDirectoryName(options.InputPath) ?? ".";
        }

        // 入力がディレクトリの場合はそのディレクトリ
        return options.InputPath!;
    }

    private static string DetermineOutputFilePath(string inputFile, string inputBasePath, string outputDir, string extension)
    {
        // 入力ファイルの相対パスを維持
        string relativePath;

        if (File.Exists(inputBasePath))
        {
            // 単一ファイルの場合
            relativePath = Path.GetFileName(inputFile);
        }
        else
        {
            // ディレクトリの場合、相対パスを計算
            var fullInputBase = Path.GetFullPath(inputBasePath);
            var fullInputFile = Path.GetFullPath(inputFile);

            if (fullInputFile.StartsWith(fullInputBase, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = fullInputFile[(fullInputBase.Length + 1)..];
            }
            else
            {
                relativePath = Path.GetFileName(inputFile);
            }
        }

        // 拡張子を変更
        var outputFileName = Path.ChangeExtension(relativePath, extension);

        return Path.Combine(outputDir, outputFileName);
    }
}

/// <summary>
/// コマンドライン引数
/// </summary>
internal class CommandLineOptions
{
    public string? InputPath { get; set; }
    public string? OutputPath { get; set; }
    public bool ShowProgress { get; set; }
    public bool ShowHelp { get; set; }
}