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

using MjlogJ;
using TileEmbedder;

namespace TileEmbedderCli;

/// <summary>
/// 牌埋め込みベクトル生成ツール メインクラス
/// </summary>
public static class ApplicationMain
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var options = ParseArguments(args);

            if (options.ShowHelp)
            {
                ShowHelp();
                return 0;
            }

            // ファイルから読み込んで可視化するモード
            if (!string.IsNullOrEmpty(options.LoadEmbeddingPath))
            {
                return ProcessVisualizationFromFile(options);
            }

            // 引数のバリデーション
            if (options.EmbeddingDim <= 0)
            {
                Console.Error.WriteLine($"エラー: 埋め込み次元数は正の整数である必要があります。指定値: {options.EmbeddingDim}");
                return 1;
            }

            if (options.Epochs <= 0)
            {
                Console.Error.WriteLine($"エラー: エポック数は正の整数である必要があります。指定値: {options.Epochs}");
                return 1;
            }

            if (options.NegativeSamples <= 0)
            {
                Console.Error.WriteLine($"エラー: ネガティブサンプル数は正の整数である必要があります。指定値: {options.NegativeSamples}");
                return 1;
            }

            if (options.LearningRate <= 0)
            {
                Console.Error.WriteLine($"エラー: 学習率は正の数である必要があります。指定値: {options.LearningRate}");
                return 1;
            }

            var format = options.Format.ToLowerInvariant();
            if (format != "safetensors" && format != "json" && format != "both")
            {
                Console.Error.WriteLine($"エラー: 出力形式は 'safetensors', 'json', 'both' のいずれかを指定してください。指定値: {options.Format}");
                return 1;
            }

            return await ProcessEmbedding(options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"致命的エラー: {ex.Message}");
            if (ex.StackTrace != null)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }

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

                case "--seed":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var seed))
                    {
                        options.RandomSeed = seed;
                    }

                    break;

                case "-r":
                case "--recursive":
                    options.Recursive = true;
                    break;

                case "-p":
                case "--progress":
                    options.ShowProgress = true;
                    break;

                case "--format":
                    if (i + 1 < args.Length)
                    {
                        options.Format = args[++i];
                    }

                    break;

                case "--embedding-dim":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var dim))
                    {
                        options.EmbeddingDim = dim;
                    }

                    break;

                case "--epochs":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var epochs))
                    {
                        options.Epochs = epochs;
                    }

                    break;

                case "--negative-samples":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var negSamples))
                    {
                        options.NegativeSamples = negSamples;
                    }

                    break;

                case "--learning-rate":
                    if (i + 1 < args.Length && float.TryParse(args[++i], out var lr))
                    {
                        options.LearningRate = lr;
                    }

                    break;

                case "-v":
                case "--visualize":
                    options.Visualize = true;
                    break;

                case "--visualize-method":
                    if (i + 1 < args.Length)
                    {
                        options.VisualizeMethod = args[++i];
                    }

                    break;

                case "--include-attributes":
                    options.IncludeAttributes = true;
                    break;

                case "--plot":
                    if (i + 1 < args.Length)
                    {
                        options.PlotOutputPath = args[++i];
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

                case "-l":
                case "--load":
                    if (i + 1 < args.Length)
                    {
                        options.LoadEmbeddingPath = args[++i];
                    }

                    break;

                case "-h":
                case "--help":
                    options.ShowHelp = true;
                    break;
            }
        }

        return options;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("""
                          牌埋め込みベクトル生成ツール (TileEmbedderCli)

                          使用方法:
                            TileEmbedderCli [-i <入力パス>] [-o <出力パス>] [オプション]
                            TileEmbedderCli -l <埋め込みファイル> [可視化オプション]

                          基本オプション:
                            -i, --input <パス>        牌譜ディレクトリのパス（省略時: ルールベースのみ生成）
                            -o, --output <パス>       出力ファイルのベース名（省略時: tile_embeddings）
                            -l, --load <パス>         既存の埋め込みファイルを読み込んで可視化 (.json/.safetensors)
                            --seed <数値>             乱数シード（再現性のため）
                            -r, --recursive           サブディレクトリも含めて牌譜を検索
                            -p, --progress            進捗表示を有効にする
                            --format <形式>           出力形式 (safetensors|json|both, デフォルト: both)
                            -h, --help                このヘルプを表示

                          ハイパーパラメータ:
                            --embedding-dim <数値>    埋め込み次元数（デフォルト: 4）
                            --epochs <数値>           学習エポック数（デフォルト: 100）
                            --negative-samples <数値> ネガティブサンプル数（デフォルト: 10）
                            --learning-rate <数値>    学習率（デフォルト: 0.025）

                          可視化オプション:
                            -v, --visualize           2次元可視化データをCSV形式で標準出力
                            --visualize-method <手法> 次元削減手法 (pca|umap, デフォルト: pca)
                            --include-attributes      属性トークンも含めて可視化
                            --plot <出力パス>         分布図を画像として出力（PNG形式）
                            --plot-width <数値>       プロット画像の幅（デフォルト: 800）
                            --plot-height <数値>      プロット画像の高さ（デフォルト: 600）

                          例:
                            # ルールベースのみで生成
                            TileEmbedderCli -p

                            # 牌譜から追加学習して生成
                            TileEmbedderCli -i ./mjlogs/ -o embeddings -p

                            # サブディレクトリも含めて学習、JSON形式のみ出力
                            TileEmbedderCli -i ./mjlogs/ -r --format json -p

                            # カスタムパラメータで学習
                            TileEmbedderCli -i ./mjlogs/ --embedding-dim 8 --epochs 200 --seed 42 -p

                            # PCAで可視化してCSV出力（スプレッドシートで可視化可能）
                            TileEmbedderCli -p --visualize > embeddings.csv

                            # UMAPで可視化
                            TileEmbedderCli -p --visualize --visualize-method umap > embeddings.csv

                            # 既存のJSONファイルから読み込んで可視化
                            TileEmbedderCli -l tile_embeddings.json > viz.csv

                            # 既存のSafetensorsファイルから読み込んで可視化
                            TileEmbedderCli -l tile_embeddings.safetensors > viz.csv

                            # Safetensorsファイルを属性トークン込みでUMAP可視化
                            TileEmbedderCli -l tile_embeddings.safetensors --visualize-method umap --include-attributes > viz.csv

                            # PCAで分布図を画像として出力
                            TileEmbedderCli -l tile_embeddings.json --plot distribution.png

                            # UMAPで分布図を画像として出力（サイズ指定）
                            TileEmbedderCli -l tile_embeddings.json --visualize-method umap --plot distribution.png --plot-width 1200 --plot-height 900

                            # 学習時に同時に分布図を生成
                            TileEmbedderCli -i ./mjlogs/ -p --plot distribution.png
                          """);
    }

    private static async Task<int> ProcessEmbedding(CommandLineOptions options)
    {
        // フェーズ1: 共起行列の構築
        if (options.ShowProgress && !options.Visualize)
        {
            Console.WriteLine("=== 共起行列の構築 ===");
        }

        var matrix = new CooccurrenceMatrix();

        if (options.ShowProgress && !options.Visualize)
        {
            Console.WriteLine("ルールベース共起行列を構築中...");
        }

        matrix.BuildBaseMatrix();

        if (options.ShowProgress && !options.Visualize)
        {
            Console.WriteLine("ルールベース共起行列の構築完了");
        }

        int processedCount = 0;
        int errorCount = 0;

        // 牌譜からの追加学習
        if (!string.IsNullOrEmpty(options.InputPath))
        {
            if (!Directory.Exists(options.InputPath))
            {
                Console.Error.WriteLine($"エラー: 入力ディレクトリが見つかりません: {options.InputPath}");
                return 1;
            }

            if (options.ShowProgress && !options.Visualize)
            {
                Console.WriteLine($"牌譜ディレクトリから共起データを抽出中: {options.InputPath}");
                if (options.Recursive)
                {
                    Console.WriteLine("（サブディレクトリを含む）");
                }
            }

            var readerOptions = new MjlogReaderOptions
            {
                IncludeSubdirectories = options.Recursive,
                ContinueOnError = true
            };

            await foreach (var record in MjlogReader.LoadManyAsync(options.InputPath, readerOptions))
            {
                try
                {
                    MjlogCooccurrenceExtractor.ExtractFromGameRecord(record, matrix);
                    processedCount++;

                    if (options.ShowProgress && !options.Visualize && processedCount % 100 == 0)
                    {
                        Console.WriteLine($"処理済み牌譜数: {processedCount}");
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    if (options.ShowProgress && !options.Visualize)
                    {
                        Console.Error.WriteLine($"警告: 牌譜の処理中にエラーが発生しました: {ex.Message}");
                    }
                }
            }

            if (options.ShowProgress && !options.Visualize)
            {
                Console.WriteLine($"牌譜からの共起データ抽出完了: {processedCount}件処理");
                if (errorCount > 0)
                {
                    Console.WriteLine($"エラー件数: {errorCount}");
                }
            }
        }
        else
        {
            if (options.ShowProgress && !options.Visualize)
            {
                Console.WriteLine("入力パスが指定されていないため、ルールベースのみで生成します");
            }
        }

        // フェーズ2: 学習
        if (options.ShowProgress && !options.Visualize)
        {
            Console.WriteLine();
            Console.WriteLine("=== Skip-gram学習 ===");
            Console.WriteLine($"パラメータ: 埋め込み次元={options.EmbeddingDim}, エポック={options.Epochs}, "
                              + $"ネガティブサンプル={options.NegativeSamples}, 学習率={options.LearningRate}");
            if (options.RandomSeed.HasValue)
            {
                Console.WriteLine($"乱数シード: {options.RandomSeed.Value}");
            }
        }

        var trainer = new SkipGramTrainer(
            vocabSize: TrainingConstants.TotalVocabularySize,
            embeddingDim: options.EmbeddingDim,
            randomSeed: options.RandomSeed);

        var trainingOptions = new SkipGramTrainingOptions
        {
            EmbeddingDim = options.EmbeddingDim,
            Epochs = options.Epochs,
            NegativeSamples = options.NegativeSamples,
            LearningRate = options.LearningRate,
            OnEpochComplete = (options.ShowProgress && !options.Visualize)
                ? (epoch, loss) => Console.WriteLine($"Epoch {epoch}/{options.Epochs}: Loss = {loss:F6}")
                : null
        };

        trainer.Train(matrix, trainingOptions);

        if (options.ShowProgress && !options.Visualize)
        {
            Console.WriteLine("学習完了");
        }

        // 可視化モードの場合はファイル出力をスキップ
        if (options.Visualize)
        {
            var visualizer = new EmbeddingVisualizer();
            var method = options.VisualizeMethod.ToLowerInvariant();

            if (method == "pca" || method == "default")
            {
                visualizer.OutputPCAToCSV(trainer, options.IncludeAttributes, false);
            }
            else if (method == "umap")
            {
                visualizer.OutputUMAPToCSV(trainer, options.IncludeAttributes, false);
            }
            else
            {
                Console.Error.WriteLine($"警告: 不明な可視化手法 '{options.VisualizeMethod}'。PCAを使用します。");
                visualizer.OutputPCAToCSV(trainer, options.IncludeAttributes, false);
            }

            return 0;
        }

        // 画像プロット出力（--plot指定時）
        if (!string.IsNullOrEmpty(options.PlotOutputPath))
        {
            var visualizer = new EmbeddingVisualizer();
            var method = options.VisualizeMethod.ToLowerInvariant();

            if (options.ShowProgress)
            {
                Console.WriteLine();
                Console.WriteLine("=== 分布図生成 ===");
            }

            if (method == "pca" || method == "default")
            {
                visualizer.OutputPCAToPlot(trainer, options.IncludeAttributes,
                    options.PlotOutputPath, options.PlotWidth, options.PlotHeight);
            }
            else if (method == "umap")
            {
                visualizer.OutputUMAPToPlot(trainer, options.IncludeAttributes,
                    options.PlotOutputPath, options.PlotWidth, options.PlotHeight);
            }
            else
            {
                Console.Error.WriteLine($"警告: 不明な可視化手法 '{options.VisualizeMethod}'。PCAを使用します。");
                visualizer.OutputPCAToPlot(trainer, options.IncludeAttributes,
                    options.PlotOutputPath, options.PlotWidth, options.PlotHeight);
            }
        }

        // フェーズ3: エクスポート（可視化モードでない場合のみ）
        if (options.ShowProgress)
        {
            Console.WriteLine();
            Console.WriteLine("=== エクスポート ===");
        }

        var outputBaseName = string.IsNullOrEmpty(options.OutputPath)
            ? "tile_embeddings"
            : options.OutputPath;

        var format = options.Format.ToLowerInvariant();

        // ハイパーパラメータ情報を作成
        var hyperparameters = new TrainingHyperparameters
        {
            EmbeddingDim = options.EmbeddingDim,
            Epochs = options.Epochs,
            NegativeSamples = options.NegativeSamples,
            LearningRate = options.LearningRate,
            RandomSeed = options.RandomSeed,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            if (format == "safetensors" || format == "both")
            {
                var safetensorsPath = Path.ChangeExtension(outputBaseName, ".safetensors");
                if (options.ShowProgress)
                {
                    Console.WriteLine($"Safetensors形式で保存中: {safetensorsPath}");
                }

                EmbeddingExporter.ExportToSafetensors(trainer, safetensorsPath, hyperparameters: hyperparameters);
                if (options.ShowProgress)
                {
                    Console.WriteLine($"✓ Safetensors形式で保存完了: {safetensorsPath}");
                }
            }

            if (format == "json" || format == "both")
            {
                var jsonPath = Path.ChangeExtension(outputBaseName, ".json");
                if (options.ShowProgress)
                {
                    Console.WriteLine($"JSON形式で保存中: {jsonPath}");
                }

                EmbeddingExporter.ExportToJson(trainer, jsonPath, indented: true, hyperparameters: hyperparameters);
                if (options.ShowProgress)
                {
                    Console.WriteLine($"✓ JSON形式で保存完了: {jsonPath}");
                }
            }

            // サマリーファイルを出力
            var summaryPath = outputBaseName + "_summary.txt";
            if (options.ShowProgress)
            {
                Console.WriteLine($"サマリーファイルを保存中: {summaryPath}");
            }

            EmbeddingExporter.ExportSummary(hyperparameters, summaryPath);
            if (options.ShowProgress)
            {
                Console.WriteLine($"✓ サマリーファイル保存完了: {summaryPath}");
            }

            if (options.ShowProgress)
            {
                Console.WriteLine();
                Console.WriteLine("=== 完了 ===");
                Console.WriteLine("埋め込みベクトルの生成が完了しました。");
                Console.WriteLine();
                // コンソールにもサマリーを表示
                Console.WriteLine(hyperparameters.ToSummaryString());
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"エラー: ファイルの保存中にエラーが発生しました: {ex.Message}");
            return 1;
        }
    }

    private static int ProcessVisualizationFromFile(CommandLineOptions options)
    {
        try
        {
            var loadPath = options.LoadEmbeddingPath!;

            if (!File.Exists(loadPath))
            {
                Console.Error.WriteLine($"エラー: ファイルが見つかりません: {loadPath}");
                return 1;
            }

            var visualizer = new EmbeddingVisualizer();
            var method = string.IsNullOrEmpty(options.VisualizeMethod) || options.VisualizeMethod == "default"
                ? "pca" // デフォルトをPCAに変更
                : options.VisualizeMethod;

            // ファイル形式の判定と処理
            var extension = Path.GetExtension(loadPath).ToLowerInvariant();

            // 画像出力が指定されている場合
            if (!string.IsNullOrEmpty(options.PlotOutputPath))
            {
                if (extension == ".json")
                {
                    visualizer.OutputPlotFromJsonFile(loadPath, method, options.IncludeAttributes,
                        options.PlotOutputPath, options.PlotWidth, options.PlotHeight);
                }
                else if (extension == ".safetensors")
                {
                    var trainer = SkipGramTrainer.LoadFromSafetensors(loadPath);

                    if (method == "pca")
                    {
                        visualizer.OutputPCAToPlot(trainer, options.IncludeAttributes,
                            options.PlotOutputPath, options.PlotWidth, options.PlotHeight);
                    }
                    else if (method == "umap")
                    {
                        visualizer.OutputUMAPToPlot(trainer, options.IncludeAttributes,
                            options.PlotOutputPath, options.PlotWidth, options.PlotHeight);
                    }
                    else
                    {
                        Console.Error.WriteLine($"警告: 不明な可視化手法 '{options.VisualizeMethod}'。PCAを使用します。");
                        visualizer.OutputPCAToPlot(trainer, options.IncludeAttributes,
                            options.PlotOutputPath, options.PlotWidth, options.PlotHeight);
                    }
                }
                else
                {
                    Console.Error.WriteLine($"エラー: サポートされていないファイル形式です。.json または .safetensors を指定してください。");
                    return 1;
                }
            }
            else
            {
                // CSV出力（従来の動作）
                if (extension == ".json")
                {
                    visualizer.OutputFromJsonFile(loadPath, method, options.IncludeAttributes);
                }
                else if (extension == ".safetensors")
                {
                    // Safetensors形式からロード
                    var trainer = SkipGramTrainer.LoadFromSafetensors(loadPath);

                    if (method == "pca")
                    {
                        visualizer.OutputPCAToCSV(trainer, options.IncludeAttributes, false);
                    }
                    else if (method == "umap")
                    {
                        visualizer.OutputUMAPToCSV(trainer, options.IncludeAttributes, false);
                    }
                    else
                    {
                        Console.Error.WriteLine($"警告: 不明な可視化手法 '{options.VisualizeMethod}'。PCAを使用します。");
                        visualizer.OutputPCAToCSV(trainer, options.IncludeAttributes, false);
                    }
                }
                else
                {
                    Console.Error.WriteLine($"エラー: サポートされていないファイル形式です。.json または .safetensors を指定してください。");
                    return 1;
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"エラー: 埋め込みベクトルの読み込みまたは可視化中にエラーが発生しました: {ex.Message}");
            return 1;
        }
    }
}

/// <summary>
/// コマンドライン引数
/// </summary>
internal class CommandLineOptions
{
    public string? InputPath { get; set; }
    public string? OutputPath { get; set; }
    public string? LoadEmbeddingPath { get; set; }
    public int? RandomSeed { get; set; }
    public bool Recursive { get; set; }
    public bool ShowProgress { get; set; }
    public string Format { get; set; } = "both";
    public int EmbeddingDim { get; set; } = TrainingConstants.DefaultEmbeddingDim;
    public int Epochs { get; set; } = TrainingConstants.DefaultEpochs;
    public int NegativeSamples { get; set; } = TrainingConstants.DefaultNegativeSamples;
    public float LearningRate { get; set; } = TrainingConstants.DefaultLearningRate;
    public bool ShowHelp { get; set; }
    public bool Visualize { get; set; }
    public string VisualizeMethod { get; set; } = "default";
    public bool IncludeAttributes { get; set; }
    public string? PlotOutputPath { get; set; }
    public int PlotWidth { get; set; } = 800;
    public int PlotHeight { get; set; } = 600;
}