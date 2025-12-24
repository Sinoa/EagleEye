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

using TileEmbedder;

namespace TileEmbedderCli;

/// <summary>
/// 埋め込みベクトル可視化クラス
/// </summary>
internal class EmbeddingVisualizer
{
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
        Console.WriteLine($"Token,X,Y,Type,Method");

        for (int i = 0; i < labels.Count; i++)
        {
            var type = GetTileType(labels[i]);
            Console.WriteLine($"{labels[i]},{x[i]:F6},{y[i]:F6},{type},{method}");
        }
    }

    /// <summary>
    /// 牌の種類を取得
    /// </summary>
    private string GetTileType(string label)
    {
        if (label.Contains("Man")) return "萬子";
        if (label.Contains("Pin")) return "筒子";
        if (label.Contains("Sou")) return "索子";
        if (label is "East" or "South" or "West" or "North") return "風牌";
        if (label is "White" or "Green" or "Red") return "三元牌";
        if (label.Contains("Attr")) return "属性";
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

