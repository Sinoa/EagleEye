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

namespace TileEmbedder;

/// <summary>
/// 共起行列を管理するクラス
/// </summary>
public class CooccurrenceMatrix
{
    private readonly int[,] _matrix;
    private readonly int _size;

    /// <summary>
    /// 行列のサイズ
    /// </summary>
    public int Size => _size;

    /// <summary>
    /// 共起行列のコンストラクタ
    /// </summary>
    /// <param name="size">行列サイズ（デフォルト: 全語彙サイズ）</param>
    public CooccurrenceMatrix(int size = TrainingConstants.TotalVocabularySize)
    {
        _size = size;
        _matrix = new int[size, size];
    }

    /// <summary>
    /// 共起カウントを取得
    /// </summary>
    public int this[int i, int j]
    {
        get => _matrix[i, j];
        set => _matrix[i, j] = value;
    }

    /// <summary>
    /// 共起カウントを取得（TileTokenId版）
    /// </summary>
    public int this[TileTokenId i, TileTokenId j]
    {
        get => _matrix[(int)i, (int)j];
        set => _matrix[(int)i, (int)j] = value;
    }

    /// <summary>
    /// 共起を加算（対称行列として両方向に加算）
    /// </summary>
    /// <param name="i">トークンID 1</param>
    /// <param name="j">トークンID 2</param>
    /// <param name="count">加算するカウント（デフォルト: 1）</param>
    public void AddCooccurrence(int i, int j, int count = 1)
    {
        if (i < 0 || i >= _size || j < 0 || j >= _size) return;
        if (i == j) return; // 自己共起は除外

        _matrix[i, j] += count;
        _matrix[j, i] += count;
    }

    /// <summary>
    /// 共起を加算（TileTokenId版）
    /// </summary>
    public void AddCooccurrence(TileTokenId i, TileTokenId j, int count = 1)
    {
        AddCooccurrence((int)i, (int)j, count);
    }

    /// <summary>
    /// ルールベースの基底共起行列を構築
    /// </summary>
    public void BuildBaseMatrix()
    {
        // 萬子同士の共起
        AddSuitCooccurrence(TileTokenId.Man1, 9, TileTokenId.AttrMan, TileTokenId.AttrNumber);

        // 筒子同士の共起
        AddSuitCooccurrence(TileTokenId.Pin1, 9, TileTokenId.AttrPin, TileTokenId.AttrNumber);

        // 索子同士の共起
        AddSuitCooccurrence(TileTokenId.Sou1, 9, TileTokenId.AttrSou, TileTokenId.AttrNumber);

        // 風牌同士の共起（東南西北）
        AddGroupCooccurrence(TileTokenId.East, 4, TileTokenId.AttrWind, TileTokenId.AttrHonor);

        // 三元牌同士の共起（白發中）
        AddGroupCooccurrence(TileTokenId.White, 3, TileTokenId.AttrDragon, TileTokenId.AttrHonor);

        // 赤牌の属性共起
        AddCooccurrence(TileTokenId.RedMan5, TileTokenId.AttrMan);
        AddCooccurrence(TileTokenId.RedMan5, TileTokenId.AttrNumber);
        AddCooccurrence(TileTokenId.RedMan5, TileTokenId.AttrRed);

        AddCooccurrence(TileTokenId.RedPin5, TileTokenId.AttrPin);
        AddCooccurrence(TileTokenId.RedPin5, TileTokenId.AttrNumber);
        AddCooccurrence(TileTokenId.RedPin5, TileTokenId.AttrRed);

        AddCooccurrence(TileTokenId.RedSou5, TileTokenId.AttrSou);
        AddCooccurrence(TileTokenId.RedSou5, TileTokenId.AttrNumber);
        AddCooccurrence(TileTokenId.RedSou5, TileTokenId.AttrRed);

        // 赤5と通常5の共起（同じ牌種として）
        AddCooccurrence(TileTokenId.RedMan5, TileTokenId.Man5);
        AddCooccurrence(TileTokenId.RedPin5, TileTokenId.Pin5);
        AddCooccurrence(TileTokenId.RedSou5, TileTokenId.Sou5);

        // 順子近接共起を追加
        AddSequenceCooccurrence(TileTokenId.Man1, 9);
        AddSequenceCooccurrence(TileTokenId.Pin1, 9);
        AddSequenceCooccurrence(TileTokenId.Sou1, 9);
    }

    /// <summary>
    /// 数牌の同種共起と属性共起を追加
    /// </summary>
    private void AddSuitCooccurrence(TileTokenId start, int count, TileTokenId suitAttr, TileTokenId numberAttr)
    {
        int startId = (int)start;

        // 同種牌同士の共起
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                AddCooccurrence(startId + i, startId + j);
            }

            // 属性トークンとの共起
            AddCooccurrence(startId + i, (int)suitAttr);
            AddCooccurrence(startId + i, (int)numberAttr);
        }
    }

    /// <summary>
    /// 字牌グループの共起と属性共起を追加
    /// </summary>
    private void AddGroupCooccurrence(TileTokenId start, int count, TileTokenId groupAttr, TileTokenId honorAttr)
    {
        int startId = (int)start;

        // グループ内の共起
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                AddCooccurrence(startId + i, startId + j);
            }

            // 属性トークンとの共起
            AddCooccurrence(startId + i, (int)groupAttr);
            AddCooccurrence(startId + i, (int)honorAttr);
        }
    }

    /// <summary>
    /// 数牌の順子近接共起を追加（ウィンドウサイズ2）
    /// </summary>
    private void AddSequenceCooccurrence(TileTokenId start, int count, int windowSize = TrainingConstants.DefaultWindowSize)
    {
        int startId = (int)start;

        for (int i = 0; i < count; i++)
        {
            for (int w = 1; w <= windowSize; w++)
            {
                if (i + w < count)
                {
                    AddCooccurrence(startId + i, startId + i + w);
                }
            }
        }
    }

    /// <summary>
    /// 全ての共起ペアを列挙
    /// </summary>
    /// <returns>（トークンID1, トークンID2, 共起カウント）のタプル</returns>
    public IEnumerable<(int Token1, int Token2, int Count)> EnumerateCooccurrences()
    {
        for (int i = 0; i < _size; i++)
        {
            for (int j = i + 1; j < _size; j++)
            {
                if (_matrix[i, j] > 0)
                {
                    yield return (i, j, _matrix[i, j]);
                }
            }
        }
    }

    /// <summary>
    /// 学習用の正例ペアリストを生成（共起カウントに応じて複製）
    /// </summary>
    /// <returns>（トークンID1, トークンID2）のリスト</returns>
    public List<(int Token1, int Token2)> GenerateTrainingPairs()
    {
        var pairs = new List<(int, int)>();

        foreach (var (token1, token2, count) in EnumerateCooccurrences())
        {
            for (int c = 0; c < count; c++)
            {
                pairs.Add((token1, token2));
                pairs.Add((token2, token1)); // 双方向
            }
        }

        return pairs;
    }

    /// <summary>
    /// 行列の合計共起数を取得
    /// </summary>
    public long GetTotalCooccurrenceCount()
    {
        long total = 0;
        for (int i = 0; i < _size; i++)
        {
            for (int j = 0; j < _size; j++)
            {
                total += _matrix[i, j];
            }
        }

        return total;
    }
}