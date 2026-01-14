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

using System.Runtime.InteropServices;

namespace MLModelUtility.Models;

/// <summary>
/// オンメモリでテンソルデータを保持する実装
/// </summary>
public class TensorData : ITensorData
{
    private readonly byte[] _data;
    private bool _disposed;

    /// <inheritdoc />
    public TensorInfo Info { get; }

    /// <summary>
    /// TensorDataのコンストラクタ
    /// </summary>
    /// <param name="info">テンソルのメタデータ</param>
    /// <param name="data">テンソルのバイトデータ</param>
    public TensorData(TensorInfo info, byte[] data)
    {
        Info = info ?? throw new ArgumentNullException(nameof(info));
        _data = data ?? throw new ArgumentNullException(nameof(data));

        if (data.Length != info.ByteSize)
        {
            throw new ArgumentException(
                $"Data size mismatch. Expected {info.ByteSize} bytes, but got {data.Length} bytes.",
                nameof(data));
        }
    }

    /// <summary>
    /// TensorDataのコンストラクタ（データをコピー）
    /// </summary>
    /// <param name="info">テンソルのメタデータ</param>
    /// <param name="data">テンソルのバイトデータ（コピーされる）</param>
    public TensorData(TensorInfo info, ReadOnlySpan<byte> data)
    {
        Info = info ?? throw new ArgumentNullException(nameof(info));

        if (data.Length != info.ByteSize)
        {
            throw new ArgumentException(
                $"Data size mismatch. Expected {info.ByteSize} bytes, but got {data.Length} bytes.",
                nameof(data));
        }

        _data = data.ToArray();
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> GetDataSpan()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _data.AsSpan();
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> GetDataMemory()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _data.AsMemory();
    }

    /// <inheritdoc />
    public ReadOnlySpan<T> GetDataAs<T>() where T : unmanaged
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return MemoryMarshal.Cast<byte, T>(_data.AsSpan());
    }

    /// <summary>
    /// 空のテンソルデータを作成
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <param name="shape">形状</param>
    /// <param name="dataType">データ型</param>
    /// <returns>ゼロで初期化されたテンソルデータ</returns>
    public static TensorData CreateEmpty(string name, IReadOnlyList<long> shape, TensorDataType dataType)
    {
        var info = new TensorInfo(name, shape, dataType);
        var data = new byte[info.ByteSize];
        return new TensorData(info, data);
    }

    /// <summary>
    /// Float32配列からテンソルデータを作成
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <param name="shape">形状</param>
    /// <param name="values">Float32の値配列</param>
    /// <returns>テンソルデータ</returns>
    public static TensorData FromFloat32(string name, IReadOnlyList<long> shape, float[] values)
    {
        var info = new TensorInfo(name, shape, TensorDataType.Float32);
        var data = MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
        return new TensorData(info, data);
    }

    /// <summary>
    /// Float64配列からテンソルデータを作成
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <param name="shape">形状</param>
    /// <param name="values">Float64の値配列</param>
    /// <returns>テンソルデータ</returns>
    public static TensorData FromFloat64(string name, IReadOnlyList<long> shape, double[] values)
    {
        var info = new TensorInfo(name, shape, TensorDataType.Float64);
        var data = MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
        return new TensorData(info, data);
    }

    /// <summary>
    /// Int32配列からテンソルデータを作成
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <param name="shape">形状</param>
    /// <param name="values">Int32の値配列</param>
    /// <returns>テンソルデータ</returns>
    public static TensorData FromInt32(string name, IReadOnlyList<long> shape, int[] values)
    {
        var info = new TensorInfo(name, shape, TensorDataType.Int32);
        var data = MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
        return new TensorData(info, data);
    }

    /// <summary>
    /// Int64配列からテンソルデータを作成
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <param name="shape">形状</param>
    /// <param name="values">Int64の値配列</param>
    /// <returns>テンソルデータ</returns>
    public static TensorData FromInt64(string name, IReadOnlyList<long> shape, long[] values)
    {
        var info = new TensorInfo(name, shape, TensorDataType.Int64);
        var data = MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
        return new TensorData(info, data);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}