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

using MjlogA.Analyzers;

namespace MjlogAnalyzer;

/// <summary>
/// コマンドラインオプション
/// </summary>
public class CommandLineOptions
{
    /// <summary>牌譜ディレクトリパス</summary>
    public string DirectoryPath { get; set; } = "";

    /// <summary>出力ファイルパス（nullの場合は標準出力）</summary>
    public string? OutputPath { get; set; }

    /// <summary>進捗表示を有効化</summary>
    public bool ShowProgress { get; set; }

    /// <summary>ヘルプ表示</summary>
    public bool ShowHelp { get; set; }

    /// <summary>サブディレクトリを含める</summary>
    public bool IncludeSubdirectories { get; set; }

    /// <summary>エラー時も処理を続行</summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>有効な分析器（デフォルトはすべて）</summary>
    public AnalyzerTypes EnabledAnalyzers { get; set; } = AnalyzerTypes.All;

    /// <summary>グラフ出力ディレクトリ（nullの場合はグラフ出力なし）</summary>
    public string? PlotOutputDirectory { get; set; }

    /// <summary>グラフの幅（ピクセル）</summary>
    public int PlotWidth { get; set; } = 800;

    /// <summary>グラフの高さ（ピクセル）</summary>
    public int PlotHeight { get; set; } = 600;

    /// <summary>
    /// コマンドライン引数をパース
    /// </summary>
    /// <param name="args">コマンドライン引数</param>
    /// <returns>パース結果</returns>
    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();
        var hasAnalyzerOption = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "-d":
                case "--directory":
                    if (i + 1 < args.Length)
                    {
                        options.DirectoryPath = args[++i];
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

                case "-r":
                case "--recursive":
                    options.IncludeSubdirectories = true;
                    break;

                case "--no-continue-on-error":
                    options.ContinueOnError = false;
                    break;

                case "-a":
                case "--analyzer":
                    if (i + 1 < args.Length)
                    {
                        var analyzerArg = args[++i];
                        var analyzerType = ParseAnalyzerType(analyzerArg);
                        if (analyzerType != AnalyzerTypes.None)
                        {
                            if (!hasAnalyzerOption)
                            {
                                options.EnabledAnalyzers = AnalyzerTypes.None;
                                hasAnalyzerOption = true;
                            }

                            options.EnabledAnalyzers |= analyzerType;
                        }
                    }

                    break;

                case "--plot":
                case "--plot-dir":
                    if (i + 1 < args.Length)
                    {
                        options.PlotOutputDirectory = args[++i];
                    }

                    break;

                case "--plot-width":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var plotWidth))
                    {
                        options.PlotWidth = plotWidth;
                    }

                    break;

                case "--plot-height":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var plotHeight))
                    {
                        options.PlotHeight = plotHeight;
                    }

                    break;

                case "-h":
                case "--help":
                    options.ShowHelp = true;
                    break;

                default:
                    // 位置引数としてディレクトリパスを受け付ける
                    if (!arg.StartsWith("-") && string.IsNullOrEmpty(options.DirectoryPath))
                    {
                        options.DirectoryPath = arg;
                    }

                    break;
            }
        }

        return options;
    }

    /// <summary>
    /// 分析器名をパース
    /// </summary>
    private static AnalyzerTypes ParseAnalyzerType(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "score" => AnalyzerTypes.Score,
            "round" or "roundcount" => AnalyzerTypes.RoundCount,
            "turn" or "turncount" => AnalyzerTypes.TurnCount,
            "yaku" => AnalyzerTypes.Yaku,
            "dora" => AnalyzerTypes.Dora,
            "point" or "pointdistribution" => AnalyzerTypes.PointDistribution,
            "all" => AnalyzerTypes.All,
            _ => AnalyzerTypes.None
        };
    }

    /// <summary>
    /// ヘルプメッセージを表示
    /// </summary>
    public static void ShowHelpMessage(TextWriter writer)
    {
        writer.WriteLine("MjlogAnalyzer - 牌譜データ分析ツール");
        writer.WriteLine();
        writer.WriteLine("使用方法:");
        writer.WriteLine("  MjlogAnalyzer [オプション] <ディレクトリパス>");
        writer.WriteLine("  MjlogAnalyzer -d <ディレクトリパス> [オプション]");
        writer.WriteLine();
        writer.WriteLine("オプション:");
        writer.WriteLine("  -d, --directory <パス>   牌譜ファイルのディレクトリパス");
        writer.WriteLine("  -o, --output <パス>      出力ファイルパス（省略時は標準出力）");
        writer.WriteLine("  -p, --progress           進捗表示を有効化（標準エラー出力）");
        writer.WriteLine("  -r, --recursive          サブディレクトリも対象に含める");
        writer.WriteLine("  -a, --analyzer <種類>    使用する分析器を指定（複数指定可）");
        writer.WriteLine("  --plot <ディレクトリ>    グラフ画像の出力先ディレクトリ");
        writer.WriteLine("  --plot-width <幅>        グラフの幅（デフォルト: 800）");
        writer.WriteLine("  --plot-height <高さ>     グラフの高さ（デフォルト: 600）");
        writer.WriteLine("  --no-continue-on-error   エラー発生時に処理を中断");
        writer.WriteLine("  -h, --help               このヘルプを表示");
        writer.WriteLine();
        writer.WriteLine("分析器の種類:");
        writer.WriteLine("  score      和了時の点数分布");
        writer.WriteLine("  round      局数分布");
        writer.WriteLine("  turn       巡目分布");
        writer.WriteLine("  yaku       役の出現頻度");
        writer.WriteLine("  dora       ドラ牌の出現頻度");
        writer.WriteLine("  point      持ち点分布");
        writer.WriteLine("  all        すべての分析器（デフォルト）");
        writer.WriteLine();
        writer.WriteLine("出力:");
        writer.WriteLine("  CSV形式で以下の分析結果を出力します:");
        writer.WriteLine("  - 和了時の点数分布（最大/最小/平均/中央値/最頻値/標準偏差/四分位数）");
        writer.WriteLine("  - 全試合の局数分布");
        writer.WriteLine("  - 全試合の巡目分布");
        writer.WriteLine("  - 各役の出現頻度（出現回数/出現率）");
        writer.WriteLine("  - ドラ表示牌の出現頻度");
        writer.WriteLine("  - 実際のドラ牌の出現頻度");
        writer.WriteLine("  - 局終了時の持ち点分布");
        writer.WriteLine();
        writer.WriteLine("例:");
        writer.WriteLine("  MjlogAnalyzer -d ./mjlogs -o result.csv -p");
        writer.WriteLine("  MjlogAnalyzer ./mjlogs -r -p > result.csv");
        writer.WriteLine("  MjlogAnalyzer -d ./mjlogs -a score -a yaku  # 点数と役のみ分析");
        writer.WriteLine("  MjlogAnalyzer -d ./mjlogs --plot ./plots    # グラフを出力");
        writer.WriteLine("  MjlogAnalyzer -d ./mjlogs -o result.csv --plot ./plots --plot-width 1200");
    }
}