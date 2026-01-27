// zlib License
// 
// Copyright (c) 2026 Sinoa
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

namespace Foxtamp.MLModelCodec.Models.Abstract;

/// <summary>
/// テンソルの型情報と形状を表現する値情報クラスです。
/// </summary>
/// <remarks>
/// <para>このクラスはONNXのValueInfoProtoに対応し、グラフの入出力定義に使用されます。</para>
/// <para><see cref="ShapeDims"/>の各要素はテンソルの次元サイズを表し、動的（可変）次元は-1で表現されます。</para>
/// </remarks>
// ReSharper disable once InconsistentNaming
public sealed class MLAbstractValueInfo
{
    /// <summary>
    /// 値の名前を取得します。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 値の型を取得します。
    /// </summary>
    /// <remarks>
    /// 基本的にUnityのONNXランタイムを想定し、ほとんどのケースで<c>typeof(float)</c>となります。
    /// エクスポート時にONNXの<c>TensorProto.Types.DataType</c>へマッピングされます。
    /// </remarks>
    public Type ValueType { get; }

    /// <summary>
    /// テンソルの形状を表す次元サイズの配列を取得します。
    /// </summary>
    /// <remarks>
    /// 各要素はindex順に次元位置に対応します。動的（可変）次元は-1として表現されます。
    /// </remarks>
    public long[] ShapeDims { get; }

    /// <summary>
    /// <see cref="MLAbstractValueInfo"/>クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="name">値の名前</param>
    /// <param name="valueType">値の型</param>
    /// <param name="shapeDims">テンソルの形状（動的次元は-1）</param>
    public MLAbstractValueInfo(string name, Type valueType, params long[] shapeDims)
    {
        Name = name;
        ValueType = valueType;
        ShapeDims = shapeDims;
    }

    /// <summary>
    /// <c>typeof(float)</c>型の<see cref="MLAbstractValueInfo"/>クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="name">値の名前</param>
    /// <param name="shapeDims">テンソルの形状（動的次元は-1）</param>
    public MLAbstractValueInfo(string name, params long[] shapeDims) : this(name, typeof(float), shapeDims)
    {
    }
}