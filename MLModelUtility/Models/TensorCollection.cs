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

using System.Collections;

namespace MLModelUtility.Models;

/// <summary>
/// 複数のテンソルとメタデータを保持するコレクション
/// </summary>
public class TensorCollection : IReadOnlyList<ITensorData>, IDisposable
{
    private readonly List<ITensorData> _tensors;
    private readonly Dictionary<string, int> _nameIndex;
    private bool _disposed;

    /// <summary>
    /// メタデータ（オプション）
    /// Safetensorsの "__metadata__" フィールドに対応
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    /// <inheritdoc />
    public int Count => _tensors.Count;

    /// <inheritdoc />
    public ITensorData this[int index]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _tensors[index];
        }
    }

    /// <summary>
    /// 名前でテンソルを取得
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <returns>対応するテンソルデータ</returns>
    public ITensorData this[string name]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_nameIndex.TryGetValue(name, out var index))
            {
                throw new KeyNotFoundException($"Tensor '{name}' not found.");
            }

            return _tensors[index];
        }
    }

    /// <summary>
    /// TensorCollectionのコンストラクタ
    /// </summary>
    /// <param name="tensors">テンソルデータのリスト</param>
    /// <param name="metadata">オプションのメタデータ</param>
    public TensorCollection(IEnumerable<ITensorData> tensors, IReadOnlyDictionary<string, string>? metadata = null)
    {
        _tensors = tensors.ToList() ?? throw new ArgumentNullException(nameof(tensors));
        Metadata = metadata;

        _nameIndex = new Dictionary<string, int>();
        for (var i = 0; i < _tensors.Count; i++)
        {
            var tensor = _tensors[i];
            if (!_nameIndex.TryAdd(tensor.Info.Name, i))
            {
                throw new ArgumentException($"Duplicate tensor name: {tensor.Info.Name}", nameof(tensors));
            }
        }
    }

    /// <summary>
    /// 空のTensorCollectionを作成
    /// </summary>
    public TensorCollection() : this(Array.Empty<ITensorData>())
    {
    }

    /// <summary>
    /// 指定した名前のテンソルが存在するか確認
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <returns>存在する場合true</returns>
    public bool ContainsTensor(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _nameIndex.ContainsKey(name);
    }

    /// <summary>
    /// 指定した名前のテンソルを取得を試みる
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <param name="tensor">見つかったテンソル</param>
    /// <returns>見つかった場合true</returns>
    public bool TryGetTensor(string name, out ITensorData? tensor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_nameIndex.TryGetValue(name, out var index))
        {
            tensor = _tensors[index];
            return true;
        }

        tensor = null;
        return false;
    }

    /// <summary>
    /// すべてのテンソル名を取得
    /// </summary>
    /// <returns>テンソル名のコレクション</returns>
    public IEnumerable<string> GetTensorNames()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _nameIndex.Keys;
    }

    /// <inheritdoc />
    public IEnumerator<ITensorData> GetEnumerator()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _tensors.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        foreach (var tensor in _tensors)
        {
            tensor.Dispose();
        }

        _tensors.Clear();
        _nameIndex.Clear();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}