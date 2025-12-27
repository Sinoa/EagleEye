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

using System.Text.Json;
using MLModelUtility.Formats.Safetensors;
using MLModelUtility.Models;

namespace TileEmbedder;

/// <summary>
/// 埋め込みベクトルのエクスポート機能
/// </summary>
public class EmbeddingExporter
{
    /// <summary>
    /// 埋め込みベクトルをSafetensors形式でエクスポート
    /// </summary>
    /// <param name="trainer">学習済みトレーナー</param>
    /// <param name="filePath">出力ファイルパス</param>
    /// <param name="tensorName">テンソル名（デフォルト: "tile_embeddings"）</param>
    /// <param name="hyperparameters">学習に使用したハイパーパラメータ（オプション）</param>
    public static void ExportToSafetensors(
        SkipGramTrainer trainer,
        string filePath,
        string tensorName = "tile_embeddings",
        TrainingHyperparameters? hyperparameters = null)
    {
        var embeddings = trainer.GetFlatEmbeddings();
        var vocabSize = TrainingConstants.TotalVocabularySize;
        var embeddingDim = embeddings.Length / vocabSize;

        // TensorDataを作成
        var tensorData = TensorData.FromFloat32(
            tensorName,
            new long[] { vocabSize, embeddingDim },
            embeddings);

        // メタデータを作成
        var metadata = new Dictionary<string, string>
        {
            ["format"] = "tile_embeddings",
            ["version"] = "1.0",
            ["vocab_size"] = vocabSize.ToString(),
            ["embedding_dim"] = embeddingDim.ToString(),
            ["token_mapping"] = CreateTokenMappingJson()
        };

        // ハイパーパラメータをメタデータに追加
        if (hyperparameters != null)
        {
            foreach (var kvp in hyperparameters.ToDictionary())
            {
                metadata[$"hp_{kvp.Key}"] = kvp.Value;
            }
        }

        // TensorCollectionを作成
        using var collection = new TensorCollection(
            new[] { tensorData },
            metadata);

        // Safetensors形式で書き込み
        var handler = new SafetensorsFormatHandler();
        handler.WriteTensorsToFile(collection, filePath);
    }

    /// <summary>
    /// 埋め込みベクトルをSafetensors形式で非同期エクスポート
    /// </summary>
    public static async Task ExportToSafetensorsAsync(
        SkipGramTrainer trainer,
        string filePath,
        string tensorName = "tile_embeddings",
        TrainingHyperparameters? hyperparameters = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = trainer.GetFlatEmbeddings();
        var vocabSize = TrainingConstants.TotalVocabularySize;
        var embeddingDim = embeddings.Length / vocabSize;

        var tensorData = TensorData.FromFloat32(
            tensorName,
            new long[] { vocabSize, embeddingDim },
            embeddings);

        var metadata = new Dictionary<string, string>
        {
            ["format"] = "tile_embeddings",
            ["version"] = "1.0",
            ["vocab_size"] = vocabSize.ToString(),
            ["embedding_dim"] = embeddingDim.ToString(),
            ["token_mapping"] = CreateTokenMappingJson()
        };

        // ハイパーパラメータをメタデータに追加
        if (hyperparameters != null)
        {
            foreach (var kvp in hyperparameters.ToDictionary())
            {
                metadata[$"hp_{kvp.Key}"] = kvp.Value;
            }
        }

        using var collection = new TensorCollection(
            new[] { tensorData },
            metadata);

        var handler = new SafetensorsFormatHandler();
        await handler.WriteTensorsToFileAsync(collection, filePath, cancellationToken);
    }

    /// <summary>
    /// 埋め込みベクトルをJSON形式でエクスポート
    /// </summary>
    /// <param name="trainer">学習済みトレーナー</param>
    /// <param name="filePath">出力ファイルパス</param>
    /// <param name="indented">整形出力するか</param>
    /// <param name="hyperparameters">学習に使用したハイパーパラメータ（オプション）</param>
    public static void ExportToJson(
        SkipGramTrainer trainer,
        string filePath,
        bool indented = true,
        TrainingHyperparameters? hyperparameters = null)
    {
        var exportData = CreateExportData(trainer, hyperparameters);
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented
        };

        var json = JsonSerializer.Serialize(exportData, options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// 埋め込みベクトルをJSON形式で非同期エクスポート
    /// </summary>
    public static async Task ExportToJsonAsync(
        SkipGramTrainer trainer,
        string filePath,
        bool indented = true,
        TrainingHyperparameters? hyperparameters = null,
        CancellationToken cancellationToken = default)
    {
        var exportData = CreateExportData(trainer, hyperparameters);
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented
        };

        await using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);

        await JsonSerializer.SerializeAsync(stream, exportData, options, cancellationToken);
    }

    /// <summary>
    /// エクスポート用のデータ構造を作成
    /// </summary>
    private static Dictionary<string, object> CreateExportData(
        SkipGramTrainer trainer,
        TrainingHyperparameters? hyperparameters = null)
    {
        var vocabSize = TrainingConstants.TotalVocabularySize;
        var embeddings = new Dictionary<string, float[]>();

        foreach (TileTokenId tokenId in Enum.GetValues<TileTokenId>())
        {
            embeddings[tokenId.ToString()] = trainer.GetEmbedding(tokenId);
        }

        var result = new Dictionary<string, object>
        {
            ["format"] = "tile_embeddings",
            ["version"] = "1.0",
            ["vocab_size"] = vocabSize,
            ["embedding_dim"] = trainer.GetEmbedding(0).Length,
            ["embeddings"] = embeddings
        };

        // ハイパーパラメータを追加
        if (hyperparameters != null)
        {
            result["hyperparameters"] = new Dictionary<string, object>
            {
                ["embedding_dim"] = hyperparameters.EmbeddingDim,
                ["epochs"] = hyperparameters.Epochs,
                ["negative_samples"] = hyperparameters.NegativeSamples,
                ["learning_rate"] = hyperparameters.LearningRate,
                ["random_seed"] = hyperparameters.RandomSeed.HasValue
                    ? (object)hyperparameters.RandomSeed.Value
                    : null!,
                ["created_at"] = hyperparameters.CreatedAt.ToString("o"),
                ["updated_at"] = hyperparameters.UpdatedAt.ToString("o")
            };
        }

        return result;
    }

    /// <summary>
    /// ハイパーパラメータサマリーをテキストファイルにエクスポート
    /// </summary>
    /// <param name="hyperparameters">ハイパーパラメータ情報</param>
    /// <param name="filePath">出力ファイルパス</param>
    public static void ExportSummary(TrainingHyperparameters hyperparameters, string filePath)
    {
        var summary = hyperparameters.ToSummaryString();
        File.WriteAllText(filePath, summary);
    }

    /// <summary>
    /// ハイパーパラメータサマリーをテキストファイルに非同期エクスポート
    /// </summary>
    /// <param name="hyperparameters">ハイパーパラメータ情報</param>
    /// <param name="filePath">出力ファイルパス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public static async Task ExportSummaryAsync(
        TrainingHyperparameters hyperparameters,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var summary = hyperparameters.ToSummaryString();
        await File.WriteAllTextAsync(filePath, summary, cancellationToken);
    }

    /// <summary>
    /// トークンIDマッピングのJSONを作成
    /// </summary>
    private static string CreateTokenMappingJson()
    {
        var mapping = new Dictionary<string, int>();
        foreach (TileTokenId tokenId in Enum.GetValues<TileTokenId>())
        {
            mapping[tokenId.ToString()] = (int)tokenId;
        }

        return JsonSerializer.Serialize(mapping);
    }

    /// <summary>
    /// トークンIDのラベル一覧を取得
    /// </summary>
    public static IReadOnlyList<string> GetTokenLabels()
    {
        return Enum.GetNames<TileTokenId>();
    }

    /// <summary>
    /// 類似度行列をCSV形式でエクスポート
    /// </summary>
    /// <param name="trainer">学習済みトレーナー</param>
    /// <param name="filePath">出力ファイルパス</param>
    /// <param name="realTilesOnly">実牌のみ出力するか（デフォルト: true）</param>
    public static void ExportSimilarityMatrixToCsv(
        SkipGramTrainer trainer,
        string filePath,
        bool realTilesOnly = true)
    {
        int maxToken = realTilesOnly
            ? TrainingConstants.RealTileVocabularySize
            : TrainingConstants.TotalVocabularySize;

        var tokenNames = Enum.GetNames<TileTokenId>().Take(maxToken).ToArray();

        using var writer = new StreamWriter(filePath);

        // ヘッダー行
        writer.Write(",");
        writer.WriteLine(string.Join(",", tokenNames));

        // 各行
        for (int i = 0; i < maxToken; i++)
        {
            writer.Write(tokenNames[i]);
            for (int j = 0; j < maxToken; j++)
            {
                float similarity = trainer.CosineSimilarity(i, j);
                writer.Write($",{similarity:F4}");
            }

            writer.WriteLine();
        }
    }

    /// <summary>
    /// 類似度行列をCSV形式で非同期エクスポート
    /// </summary>
    public static async Task ExportSimilarityMatrixToCsvAsync(
        SkipGramTrainer trainer,
        string filePath,
        bool realTilesOnly = true,
        CancellationToken cancellationToken = default)
    {
        int maxToken = realTilesOnly
            ? TrainingConstants.RealTileVocabularySize
            : TrainingConstants.TotalVocabularySize;

        var tokenNames = Enum.GetNames<TileTokenId>().Take(maxToken).ToArray();

        await using var writer = new StreamWriter(filePath);

        // ヘッダー行
        await writer.WriteAsync(",");
        await writer.WriteLineAsync(string.Join(",", tokenNames));

        // 各行
        for (int i = 0; i < maxToken; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = new System.Text.StringBuilder();
            line.Append(tokenNames[i]);

            for (int j = 0; j < maxToken; j++)
            {
                float similarity = trainer.CosineSimilarity(i, j);
                line.Append($",{similarity:F4}");
            }

            await writer.WriteLineAsync(line.ToString());
        }
    }
}