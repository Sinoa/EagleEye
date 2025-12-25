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
using ScottPlot;
using TileEmbedder;

namespace TileEmbedderCli;

/// <summary>
/// 埋め込みベクトル可視化クラス
/// </summary>
internal class EmbeddingVisualizer
{
    /// <summary>
    /// 日本語表示用フォント名
    /// </summary>
    private static readonly string JapaneseFontName = GetJapaneseFontName();

    /// <summary>
    /// 牌種類ごとの表示色
    /// </summary>
    private static readonly Dictionary<string, ScottPlot.Color> TileTypeColors = new()
    {
        { "萬子", ScottPlot.Color.FromHex("#e74c3c") }, // 赤系
        { "筒子", ScottPlot.Color.FromHex("#3498db") }, // 青系
        { "索子", ScottPlot.Color.FromHex("#2ecc71") }, // 緑系
        { "風牌", ScottPlot.Color.FromHex("#9b59b6") }, // 紫系
        { "三元牌", ScottPlot.Color.FromHex("#f39c12") }, // オレンジ系
        { "属性", ScottPlot.Color.FromHex("#95a5a6") }, // グレー系
        { "その他", ScottPlot.Color.FromHex("#34495e") } // ダークグレー
    };

    /// <summary>
    /// 日本語フォント名を取得（環境に応じて適切なフォントを選択）
    /// </summary>
    private static string GetJapaneseFontName()
    {
        // Windows: Yu Gothic UI, Meiryo UI など
        if (OperatingSystem.IsWindows())
        {
            return "Yu Gothic UI";
        }

        // macOS: Hiragino Sans など
        if (OperatingSystem.IsMacOS())
        {
            return "Hiragino Sans";
        }

        // Linux等: Noto Sans CJK JP など
        return "Noto Sans CJK JP";
    }

    /// <summary>
    /// プロットに日本語フォントを適用
    /// </summary>
    private static void ApplyJapaneseFont(Plot plot)
    {
        plot.Axes.Title.Label.FontName = JapaneseFontName;
        plot.Axes.Bottom.Label.FontName = JapaneseFontName;
        plot.Axes.Left.Label.FontName = JapaneseFontName;
        plot.Axes.Bottom.TickLabelStyle.FontName = JapaneseFontName;
        plot.Axes.Left.TickLabelStyle.FontName = JapaneseFontName;
    }

    /// <summary>
    /// JSONファイルから埋め込みベクトルを読み込んでCSV出力
    /// </summary>
    public void OutputFromJsonFile(string jsonPath, string method, bool includeAttributes)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"JSONファイルが見つかりません: {jsonPath}");
        }

        var json = File.ReadAllText(jsonPath);
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        if (!data.TryGetProperty("embeddings", out var embeddingsObj))
        {
            throw new InvalidDataException("JSONファイルに'embeddings'プロパティが見つかりません");
        }

        var embeddings = new List<float[]>();
        var labels = new List<string>();

        foreach (var prop in embeddingsObj.EnumerateObject())
        {
            var tokenName = prop.Name;

            // 属性トークンを除外するオプション
            if (!includeAttributes && tokenName.StartsWith("Attr"))
            {
                continue;
            }

            var vector = new List<float>();
            foreach (var element in prop.Value.EnumerateArray())
            {
                vector.Add(element.GetSingle());
            }

            embeddings.Add(vector.ToArray());
            labels.Add(tokenName);
        }

        // 次元削減とCSV出力
        float[] x, y;
        var methodLower = method.ToLowerInvariant();

        if (methodLower == "pca")
        {
            (x, y) = PerformPCA(embeddings);
        }
        else if (methodLower == "umap")
        {
            (x, y) = PerformUMAP(embeddings);
        }
        else
        {
            throw new ArgumentException($"不明な可視化手法: {method}。'pca'または'umap'を指定してください。");
        }

        OutputCSV(labels, x, y, methodLower.ToUpperInvariant());
    }

    /// <summary>
    /// PCAで2次元削減してCSV出力
    /// </summary>
    public void OutputPCAToCSV(SkipGramTrainer trainer, bool includeAttributes, bool showProgress)
    {
        var (embeddings, labels) = GetEmbeddings(trainer, includeAttributes);
        var (x, y) = PerformPCA(embeddings);

        OutputCSV(labels, x, y, "PCA");
    }

    /// <summary>
    /// UMAPで2次元削減してCSV出力
    /// </summary>
    public void OutputUMAPToCSV(SkipGramTrainer trainer, bool includeAttributes, bool showProgress)
    {
        var (embeddings, labels) = GetEmbeddings(trainer, includeAttributes);
        var (x, y) = PerformUMAP(embeddings);

        OutputCSV(labels, x, y, "UMAP");
    }

    /// <summary>
    /// 埋め込みベクトルとラベルを取得
    /// </summary>
    private (List<float[]> embeddings, List<string> labels) GetEmbeddings(SkipGramTrainer trainer,
        bool includeAttributes)
    {
        var embeddings = new List<float[]>();
        var labels = new List<string>();

        foreach (TileTokenId tokenId in Enum.GetValues<TileTokenId>())
        {
            var tokenIndex = (int)tokenId;

            if (!includeAttributes && tokenIndex >= 37)
            {
                continue;
            }

            embeddings.Add(trainer.GetEmbedding(tokenId));
            labels.Add(tokenId.ToString());
        }

        return (embeddings, labels);
    }

    /// <summary>
    /// CSV形式で標準出力
    /// </summary>
    private void OutputCSV(List<string> labels, float[] x, float[] y, string method)
    {
        Console.WriteLine("Token,X,Y,Type,Method");

        for (int i = 0; i < labels.Count; i++)
        {
            var type = GetTileType(labels[i]);
            Console.WriteLine($"{labels[i]},{x[i]:F6},{y[i]:F6},{type},{method}");
        }
    }

    /// <summary>
    /// JSONファイルから埋め込みベクトルを読み込んで画像出力
    /// </summary>
    public void OutputPlotFromJsonFile(string jsonPath, string method, bool includeAttributes, string outputPath, int width, int height)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"JSONファイルが見つかりません: {jsonPath}");
        }

        var json = File.ReadAllText(jsonPath);
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        if (!data.TryGetProperty("embeddings", out var embeddingsObj))
        {
            throw new InvalidDataException("JSONファイルに'embeddings'プロパティが見つかりません");
        }

        var embeddings = new List<float[]>();
        var labels = new List<string>();

        foreach (var prop in embeddingsObj.EnumerateObject())
        {
            var tokenName = prop.Name;

            // 属性トークンを除外するオプション
            if (!includeAttributes && tokenName.StartsWith("Attr"))
            {
                continue;
            }

            var vector = new List<float>();
            foreach (var element in prop.Value.EnumerateArray())
            {
                vector.Add(element.GetSingle());
            }

            embeddings.Add(vector.ToArray());
            labels.Add(tokenName);
        }

        // 次元削減と画像出力
        float[] x, y;
        var methodLower = method.ToLowerInvariant();

        if (methodLower == "pca")
        {
            (x, y) = PerformPCA(embeddings);
        }
        else if (methodLower == "umap")
        {
            (x, y) = PerformUMAP(embeddings);
        }
        else
        {
            throw new ArgumentException($"不明な可視化手法: {method}。'pca'または'umap'を指定してください。");
        }

        SavePlotImage(labels, x, y, methodLower.ToUpperInvariant(), outputPath, width, height);
    }

    /// <summary>
    /// PCAで2次元削減して画像出力
    /// </summary>
    public void OutputPCAToPlot(SkipGramTrainer trainer, bool includeAttributes, string outputPath, int width, int height)
    {
        var (embeddings, labels) = GetEmbeddings(trainer, includeAttributes);
        var (x, y) = PerformPCA(embeddings);

        SavePlotImage(labels, x, y, "PCA", outputPath, width, height);
    }

    /// <summary>
    /// UMAPで2次元削減して画像出力
    /// </summary>
    public void OutputUMAPToPlot(SkipGramTrainer trainer, bool includeAttributes, string outputPath, int width, int height)
    {
        var (embeddings, labels) = GetEmbeddings(trainer, includeAttributes);
        var (x, y) = PerformUMAP(embeddings);

        SavePlotImage(labels, x, y, "UMAP", outputPath, width, height);
    }

    /// <summary>
    /// 散布図を画像として保存
    /// </summary>
    private void SavePlotImage(List<string> labels, float[] x, float[] y, string method, string outputPath, int width, int height)
    {
        var plot = new Plot();
        ApplyJapaneseFont(plot);

        // 牌の種類ごとにグループ化
        var groups = new Dictionary<string, List<(float x, float y, string label)>>();

        for (int i = 0; i < labels.Count; i++)
        {
            var tileType = GetTileType(labels[i]);
            if (!groups.ContainsKey(tileType))
            {
                groups[tileType] = new List<(float, float, string)>();
            }

            groups[tileType].Add((x[i], y[i], labels[i]));
        }

        // 各グループごとに散布図を描画
        foreach (var group in groups)
        {
            var tileType = group.Key;
            var points = group.Value;
            var color = TileTypeColors.GetValueOrDefault(tileType, TileTypeColors["その他"]);

            double[] xValues = points.Select(p => (double)p.x).ToArray();
            double[] yValues = points.Select(p => (double)p.y).ToArray();

            var scatter = plot.Add.Scatter(xValues, yValues);
            scatter.LineWidth = 0;
            scatter.MarkerSize = 10;
            scatter.MarkerColor = color;
            scatter.LegendText = tileType;
        }

        // 各ポイントにラベルを追加
        for (int i = 0; i < labels.Count; i++)
        {
            var tileType = GetTileType(labels[i]);
            var color = TileTypeColors.GetValueOrDefault(tileType, TileTypeColors["その他"]);

            // ラベルテキストを取得（日本語表示用に変換）
            var displayLabel = GetDisplayLabel(labels[i]);

            var text = plot.Add.Text(displayLabel, x[i], y[i]);
            text.LabelFontName = JapaneseFontName;
            text.LabelFontSize = 9;
            text.LabelFontColor = color;
            text.LabelOffsetX = 5;
            text.LabelOffsetY = -5;
        }

        // タイトルと軸ラベル
        plot.Title($"牌埋め込みベクトル分布図 ({method})");
        plot.XLabel($"{method} 第1成分");
        plot.YLabel($"{method} 第2成分");

        // 凡例を表示
        plot.ShowLegend();

        // 出力ディレクトリが存在しない場合は作成
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 画像を保存
        plot.SavePng(outputPath, width, height);
        Console.WriteLine($"画像を保存しました: {outputPath}");
    }

    /// <summary>
    /// トークン名を表示用ラベルに変換
    /// </summary>
    private string GetDisplayLabel(string tokenName)
    {
        // 字牌・属性トークンを先に判定（SouthがSouを含むため）
        var result = tokenName switch
        {
            "East" => "東",
            "South" => "南",
            "West" => "西",
            "North" => "北",
            "White" => "白",
            "Green" => "發",
            "Red" => "中",
            "AttrMan" => "萬",
            "AttrPin" => "筒",
            "AttrSou" => "索",
            "AttrWind" => "風",
            "AttrDragon" => "元",
            "AttrNumber" => "数",
            "AttrHonor" => "字",
            "AttrRed" => "赤",
            _ => tokenName
        };

        if (result != null) return result;

        // 萬子
        if (tokenName == "RedMan5") return "赤5m";
        if (tokenName.StartsWith("Man")) return tokenName.Replace("Man", "") + "m";

        // 筒子
        if (tokenName == "RedPin5") return "赤5p";
        if (tokenName.StartsWith("Pin")) return tokenName.Replace("Pin", "") + "p";

        // 索子
        if (tokenName == "RedSou5") return "赤5s";
        if (tokenName.StartsWith("Sou")) return tokenName.Replace("Sou", "") + "s";

        return tokenName;
    }

    /// <summary>
    /// 牌の種類を取得
    /// </summary>
    private string GetTileType(string label)
    {
        // 字牌を先に判定（SouthがSouを含むため）
        if (label is "East" or "South" or "West" or "North") return "風牌";
        if (label is "White" or "Green" or "Red") return "三元牌";
        if (label.Contains("Attr")) return "属性";
        // 数牌
        if (label.Contains("Man")) return "萬子";
        if (label.Contains("Pin")) return "筒子";
        if (label.Contains("Sou")) return "索子";
        return "その他";
    }

    /// <summary>
    /// PCAで2次元に削減
    /// </summary>
    private (float[] x, float[] y) PerformPCA(List<float[]> embeddings)
    {
        int n = embeddings.Count;
        int d = embeddings[0].Length;

        // 平均を計算
        var mean = new float[d];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < d; j++)
            {
                mean[j] += embeddings[i][j];
            }
        }

        for (int j = 0; j < d; j++)
        {
            mean[j] /= n;
        }

        // 中心化
        var centered = new float[n][];
        for (int i = 0; i < n; i++)
        {
            centered[i] = new float[d];
            for (int j = 0; j < d; j++)
            {
                centered[i][j] = embeddings[i][j] - mean[j];
            }
        }

        // 共分散行列
        var covariance = new float[d, d];
        for (int i = 0; i < d; i++)
        {
            for (int j = 0; j < d; j++)
            {
                float sum = 0;
                for (int k = 0; k < n; k++)
                {
                    sum += centered[k][i] * centered[k][j];
                }

                covariance[i, j] = sum / (n - 1);
            }
        }

        // 主成分を計算
        var pc1 = ComputePrincipalComponent(covariance, d);
        var pc2 = ComputePrincipalComponent(covariance, d, pc1);

        // 射影
        var x = new float[n];
        var y = new float[n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < d; j++)
            {
                x[i] += centered[i][j] * pc1[j];
                y[i] += centered[i][j] * pc2[j];
            }
        }

        return (x, y);
    }

    /// <summary>
    /// パワー法で主成分を計算
    /// </summary>
    private float[] ComputePrincipalComponent(float[,] covariance, int dim, float[]? orthogonalTo = null)
    {
        var vector = new float[dim];
        var random = new Random(42);

        // ランダム初期化
        for (int i = 0; i < dim; i++)
        {
            vector[i] = (float)random.NextDouble();
        }

        // 正規化
        float norm = 0;
        for (int i = 0; i < dim; i++)
        {
            norm += vector[i] * vector[i];
        }

        norm = MathF.Sqrt(norm);
        for (int i = 0; i < dim; i++)
        {
            vector[i] /= norm;
        }

        // パワー反復
        for (int iter = 0; iter < 100; iter++)
        {
            var newVector = new float[dim];

            // 行列ベクトル積
            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    newVector[i] += covariance[i, j] * vector[j];
                }
            }

            // 直交化
            if (orthogonalTo != null)
            {
                float dot = 0;
                for (int i = 0; i < dim; i++)
                {
                    dot += newVector[i] * orthogonalTo[i];
                }

                for (int i = 0; i < dim; i++)
                {
                    newVector[i] -= dot * orthogonalTo[i];
                }
            }

            // 正規化
            norm = 0;
            for (int i = 0; i < dim; i++)
            {
                norm += newVector[i] * newVector[i];
            }

            norm = MathF.Sqrt(norm);

            if (norm < 1e-10f)
            {
                break;
            }

            for (int i = 0; i < dim; i++)
            {
                newVector[i] /= norm;
            }

            vector = newVector;
        }

        return vector;
    }

    /// <summary>
    /// UMAPで2次元に削減（簡易実装）
    /// </summary>
    private (float[] x, float[] y) PerformUMAP(List<float[]> embeddings)
    {
        int n = embeddings.Count;
        int nNeighbors = Math.Min(15, n - 1);
        int nComponents = 2;

        // k近傍グラフを構築
        var neighbors = new List<int>[n];
        var distances = new List<float>[n];

        for (int i = 0; i < n; i++)
        {
            neighbors[i] = new List<int>();
            distances[i] = new List<float>();

            // 全点との距離を計算
            var dists = new (int idx, float dist)[n];
            for (int j = 0; j < n; j++)
            {
                if (i == j)
                {
                    dists[j] = (j, float.MaxValue);
                }
                else
                {
                    dists[j] = (j, EuclideanDistance(embeddings[i], embeddings[j]));
                }
            }

            // 距離でソートして近傍を取得
            Array.Sort(dists, (a, b) => a.dist.CompareTo(b.dist));

            for (int k = 0; k < nNeighbors; k++)
            {
                neighbors[i].Add(dists[k].idx);
                distances[i].Add(dists[k].dist);
            }
        }

        // 低次元表現を初期化（ランダム）
        var random = new Random(42);
        var embedding = new float[n, nComponents];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < nComponents; j++)
            {
                embedding[i, j] = (float)(random.NextDouble() * 20 - 10);
            }
        }

        // 確率的勾配降下で最適化
        int nEpochs = 200;
        float learningRate = 1.0f;

        for (int epoch = 0; epoch < nEpochs; epoch++)
        {
            float alpha = learningRate * (1.0f - (float)epoch / nEpochs);

            for (int i = 0; i < n; i++)
            {
                // 正例（近傍）を引き寄せる
                for (int k = 0; k < neighbors[i].Count; k++)
                {
                    int j = neighbors[i][k];
                    float dist = LowDimDistance(embedding, i, j, nComponents);

                    // 引力
                    float force = -2.0f / (1.0f + dist * dist);

                    for (int d = 0; d < nComponents; d++)
                    {
                        float grad = force * (embedding[i, d] - embedding[j, d]);
                        embedding[i, d] -= alpha * grad;
                    }
                }

                // 負例（ランダムサンプル）を遠ざける
                for (int neg = 0; neg < 5; neg++)
                {
                    int j = random.Next(n);
                    if (i == j || neighbors[i].Contains(j))
                    {
                        continue;
                    }

                    float dist = LowDimDistance(embedding, i, j, nComponents);

                    // 斥力
                    if (dist < 1e-5f) continue;

                    float force = 2.0f / ((1.0f + dist * dist) * dist);

                    for (int d = 0; d < nComponents; d++)
                    {
                        float grad = force * (embedding[i, d] - embedding[j, d]);
                        embedding[i, d] -= alpha * grad;
                    }
                }
            }
        }

        // 結果を抽出
        var x = new float[n];
        var y = new float[n];
        for (int i = 0; i < n; i++)
        {
            x[i] = embedding[i, 0];
            y[i] = embedding[i, 1];
        }

        return (x, y);
    }

    /// <summary>
    /// ユークリッド距離
    /// </summary>
    private float EuclideanDistance(float[] a, float[] b)
    {
        float sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            float diff = a[i] - b[i];
            sum += diff * diff;
        }

        return MathF.Sqrt(sum);
    }

    /// <summary>
    /// 低次元空間での距離
    /// </summary>
    private float LowDimDistance(float[,] embedding, int i, int j, int nComponents)
    {
        float sum = 0;
        for (int d = 0; d < nComponents; d++)
        {
            float diff = embedding[i, d] - embedding[j, d];
            sum += diff * diff;
        }

        return MathF.Sqrt(sum);
    }
}