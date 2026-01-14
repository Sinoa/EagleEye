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

namespace MLModelUtility.Models;

/// <summary>
/// テンソルのメタデータ情報
/// </summary>
public class TensorInfo
{
    /// <summary>
    /// テンソルの名前
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// テンソルの形状（各次元のサイズ）
    /// </summary>
    public IReadOnlyList<long> Shape { get; }

    /// <summary>
    /// テンソルのデータ型
    /// </summary>
    public TensorDataType DataType { get; }

    /// <summary>
    /// テンソルの総要素数
    /// </summary>
    public long ElementCount
    {
        get
        {
            if (Shape.Count == 0) return 0;
            long count = 1;
            foreach (var dim in Shape)
            {
                count *= dim;
            }

            return count;
        }
    }

    /// <summary>
    /// テンソルのバイトサイズ
    /// </summary>
    public long ByteSize => ElementCount * DataType.GetByteSize();

    /// <summary>
    /// TensorInfoのコンストラクタ
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <param name="shape">形状</param>
    /// <param name="dataType">データ型</param>
    public TensorInfo(string name, IReadOnlyList<long> shape, TensorDataType dataType)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Shape = shape ?? throw new ArgumentNullException(nameof(shape));
        DataType = dataType;
    }

    /// <summary>
    /// 形状を文字列で表現
    /// </summary>
    /// <returns>形状の文字列表現 (例: "[3, 4, 5]")</returns>
    public string ShapeToString()
    {
        return $"[{string.Join(", ", Shape)}]";
    }

    public override string ToString()
    {
        return $"{Name}: {DataType} {ShapeToString()}";
    }
}