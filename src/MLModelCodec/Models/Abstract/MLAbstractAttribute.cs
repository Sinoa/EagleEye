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

// ReSharper disable InconsistentNaming

using TorchSharp;

namespace Foxtamp.MLModelCodec.Models.Abstract;

/// <summary>
/// ONNX属性値として許容される型を示すマーカーインターフェースです。
/// </summary>
public interface IMLAttributeValue;

#region Attribute Value Wrapper Types

/// <summary>
/// float型属性値のラッパー構造体です。
/// </summary>
/// <param name="Value">属性値</param>
public readonly record struct MLAttrFloat(float Value) : IMLAttributeValue
{
    /// <summary>
    /// float値からの暗黙的変換演算子です。
    /// </summary>
    public static implicit operator MLAttrFloat(float value) => new(value);
}

/// <summary>
/// long型属性値のラッパー構造体です。
/// </summary>
/// <param name="Value">属性値</param>
public readonly record struct MLAttrLong(long Value) : IMLAttributeValue
{
    /// <summary>
    /// long値からの暗黙的変換演算子です。
    /// </summary>
    public static implicit operator MLAttrLong(long value) => new(value);
}

/// <summary>
/// string型属性値のラッパー構造体です。
/// </summary>
/// <param name="Value">属性値</param>
public readonly record struct MLAttrString(string Value) : IMLAttributeValue
{
    /// <summary>
    /// string値からの暗黙的変換演算子です。
    /// </summary>
    public static implicit operator MLAttrString(string value) => new(value);
}

/// <summary>
/// Tensor型属性値のラッパー構造体です。
/// </summary>
/// <param name="Value">属性値</param>
public readonly record struct MLAttrTensor(torch.Tensor Value) : IMLAttributeValue
{
    /// <summary>
    /// Tensor値からの暗黙的変換演算子です。
    /// </summary>
    public static implicit operator MLAttrTensor(torch.Tensor value) => new(value);
}

/// <summary>
/// float配列型属性値のラッパー構造体です。
/// </summary>
/// <param name="Value">属性値</param>
public readonly record struct MLAttrFloats(float[] Value) : IMLAttributeValue
{
    /// <summary>
    /// float配列からの暗黙的変換演算子です。
    /// </summary>
    public static implicit operator MLAttrFloats(float[] value) => new(value);
}

/// <summary>
/// long配列型属性値のラッパー構造体です。
/// </summary>
/// <param name="Value">属性値</param>
public readonly record struct MLAttrLongs(long[] Value) : IMLAttributeValue
{
    /// <summary>
    /// long配列からの暗黙的変換演算子です。
    /// </summary>
    public static implicit operator MLAttrLongs(long[] value) => new(value);
}

/// <summary>
/// string配列型属性値のラッパー構造体です。
/// </summary>
/// <param name="Value">属性値</param>
public readonly record struct MLAttrStrings(string[] Value) : IMLAttributeValue
{
    /// <summary>
    /// string配列からの暗黙的変換演算子です。
    /// </summary>
    public static implicit operator MLAttrStrings(string[] value) => new(value);
}

/// <summary>
/// Tensor配列型属性値のラッパー構造体です。
/// </summary>
/// <param name="Value">属性値</param>
public readonly record struct MLAttrTensors(torch.Tensor[] Value) : IMLAttributeValue
{
    /// <summary>
    /// Tensor配列からの暗黙的変換演算子です。
    /// </summary>
    public static implicit operator MLAttrTensors(torch.Tensor[] value) => new(value);
}

#endregion

/// <summary>
/// ONNX演算ノードの属性を表現する基底クラスです。
/// </summary>
public abstract class MLAbstractAttribute
{
    /// <summary>
    /// 属性名を取得します。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 属性値の型を取得します。
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// <see cref="MLAbstractAttribute"/>クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="name">属性名</param>
    /// <param name="type">属性値の型</param>
    protected MLAbstractAttribute(string name, Type type)
    {
        Name = name;
        Type = type;
    }

    /// <summary>
    /// (string, float)タプルから<see cref="MLAbstractAttribute"/>への暗黙的変換演算子です。
    /// </summary>
    /// <param name="tuple">属性名と属性値のタプル</param>
    public static implicit operator MLAbstractAttribute((string name, float value) tuple) => new MLAbstractAttribute<MLAttrFloat>(tuple.name, tuple.value);

    /// <summary>
    /// (string, long)タプルから<see cref="MLAbstractAttribute"/>への暗黙的変換演算子です。
    /// </summary>
    /// <param name="tuple">属性名と属性値のタプル</param>
    public static implicit operator MLAbstractAttribute((string name, long value) tuple) => new MLAbstractAttribute<MLAttrLong>(tuple.name, tuple.value);

    /// <summary>
    /// (string, string)タプルから<see cref="MLAbstractAttribute"/>への暗黙的変換演算子です。
    /// </summary>
    /// <param name="tuple">属性名と属性値のタプル</param>
    public static implicit operator MLAbstractAttribute((string name, string value) tuple) => new MLAbstractAttribute<MLAttrString>(tuple.name, tuple.value);

    /// <summary>
    /// (string, torch.Tensor)タプルから<see cref="MLAbstractAttribute"/>への暗黙的変換演算子です。
    /// </summary>
    /// <param name="tuple">属性名と属性値のタプル</param>
    public static implicit operator MLAbstractAttribute((string name, torch.Tensor value) tuple) => new MLAbstractAttribute<MLAttrTensor>(tuple.name, tuple.value);

    /// <summary>
    /// (string, float[])タプルから<see cref="MLAbstractAttribute"/>への暗黙的変換演算子です。
    /// </summary>
    /// <param name="tuple">属性名と属性値配列のタプル</param>
    public static implicit operator MLAbstractAttribute((string name, float[] value) tuple) => new MLAbstractAttribute<MLAttrFloats>(tuple.name, tuple.value);

    /// <summary>
    /// (string, long[])タプルから<see cref="MLAbstractAttribute"/>への暗黙的変換演算子です。
    /// </summary>
    /// <param name="tuple">属性名と属性値配列のタプル</param>
    public static implicit operator MLAbstractAttribute((string name, long[] value) tuple) => new MLAbstractAttribute<MLAttrLongs>(tuple.name, tuple.value);

    /// <summary>
    /// (string, string[])タプルから<see cref="MLAbstractAttribute"/>への暗黙的変換演算子です。
    /// </summary>
    /// <param name="tuple">属性名と属性値配列のタプル</param>
    public static implicit operator MLAbstractAttribute((string name, string[] value) tuple) => new MLAbstractAttribute<MLAttrStrings>(tuple.name, tuple.value);

    /// <summary>
    /// (string, torch.Tensor[])タプルから<see cref="MLAbstractAttribute"/>への暗黙的変換演算子です。
    /// </summary>
    /// <param name="tuple">属性名と属性値配列のタプル</param>
    public static implicit operator MLAbstractAttribute((string name, torch.Tensor[] value) tuple) => new MLAbstractAttribute<MLAttrTensors>(tuple.name, tuple.value);

    /// <summary>
    /// 属性値をobject型として取得します。
    /// </summary>
    /// <returns>属性値</returns>
    public abstract object GetValue();
}

/// <summary>
/// 型パラメータで属性値の型を指定するONNX演算ノードの属性クラスです。
/// </summary>
/// <typeparam name="T">属性値の型（<see cref="IMLAttributeValue"/>を実装する型）</typeparam>
public sealed class MLAbstractAttribute<T> : MLAbstractAttribute where T : IMLAttributeValue
{
    /// <summary>
    /// 属性値を取得します。
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// <see cref="MLAbstractAttribute{T}"/>クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="name">属性名</param>
    /// <param name="value">属性値</param>
    public MLAbstractAttribute(string name, T value) : base(name, typeof(T))
    {
        Value = value;
    }

    /// <inheritdoc/>
    public override object GetValue() => Value;
}