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
/// テンソルのデータ型
/// </summary>
public enum TensorDataType
{
    /// <summary>不明なデータ型</summary>
    Unknown = 0,

    /// <summary>32ビット浮動小数点</summary>
    Float32,

    /// <summary>64ビット浮動小数点</summary>
    Float64,

    /// <summary>16ビット浮動小数点 (IEEE 754)</summary>
    Float16,

    /// <summary>16ビット浮動小数点 (Brain Float)</summary>
    BFloat16,

    /// <summary>8ビット符号なし整数</summary>
    UInt8,

    /// <summary>8ビット符号付き整数</summary>
    Int8,

    /// <summary>16ビット符号なし整数</summary>
    UInt16,

    /// <summary>16ビット符号付き整数</summary>
    Int16,

    /// <summary>32ビット符号なし整数</summary>
    UInt32,

    /// <summary>32ビット符号付き整数</summary>
    Int32,

    /// <summary>64ビット符号なし整数</summary>
    UInt64,

    /// <summary>64ビット符号付き整数</summary>
    Int64,

    /// <summary>ブール値</summary>
    Bool
}

/// <summary>
/// TensorDataType の拡張メソッド
/// </summary>
public static class TensorDataTypeExtensions
{
    /// <summary>
    /// データ型のバイトサイズを取得
    /// </summary>
    /// <param name="dataType">データ型</param>
    /// <returns>バイトサイズ</returns>
    public static int GetByteSize(this TensorDataType dataType)
    {
        return dataType switch
        {
            TensorDataType.Float32 => 4,
            TensorDataType.Float64 => 8,
            TensorDataType.Float16 => 2,
            TensorDataType.BFloat16 => 2,
            TensorDataType.UInt8 => 1,
            TensorDataType.Int8 => 1,
            TensorDataType.UInt16 => 2,
            TensorDataType.Int16 => 2,
            TensorDataType.UInt32 => 4,
            TensorDataType.Int32 => 4,
            TensorDataType.UInt64 => 8,
            TensorDataType.Int64 => 8,
            TensorDataType.Bool => 1,
            _ => throw new ArgumentException($"Unknown data type: {dataType}", nameof(dataType))
        };
    }

    /// <summary>
    /// Safetensors形式の型名からTensorDataTypeに変換
    /// </summary>
    /// <param name="typeName">Safetensors形式の型名</param>
    /// <returns>対応するTensorDataType</returns>
    public static TensorDataType FromSafetensorsTypeName(string typeName)
    {
        return typeName.ToUpperInvariant() switch
        {
            "F32" => TensorDataType.Float32,
            "F64" => TensorDataType.Float64,
            "F16" => TensorDataType.Float16,
            "BF16" => TensorDataType.BFloat16,
            "U8" => TensorDataType.UInt8,
            "I8" => TensorDataType.Int8,
            "U16" => TensorDataType.UInt16,
            "I16" => TensorDataType.Int16,
            "U32" => TensorDataType.UInt32,
            "I32" => TensorDataType.Int32,
            "U64" => TensorDataType.UInt64,
            "I64" => TensorDataType.Int64,
            "BOOL" => TensorDataType.Bool,
            _ => TensorDataType.Unknown
        };
    }

    /// <summary>
    /// TensorDataTypeをSafetensors形式の型名に変換
    /// </summary>
    /// <param name="dataType">データ型</param>
    /// <returns>Safetensors形式の型名</returns>
    public static string ToSafetensorsTypeName(this TensorDataType dataType)
    {
        return dataType switch
        {
            TensorDataType.Float32 => "F32",
            TensorDataType.Float64 => "F64",
            TensorDataType.Float16 => "F16",
            TensorDataType.BFloat16 => "BF16",
            TensorDataType.UInt8 => "U8",
            TensorDataType.Int8 => "I8",
            TensorDataType.UInt16 => "U16",
            TensorDataType.Int16 => "I16",
            TensorDataType.UInt32 => "U32",
            TensorDataType.Int32 => "I32",
            TensorDataType.UInt64 => "U64",
            TensorDataType.Int64 => "I64",
            TensorDataType.Bool => "BOOL",
            _ => throw new ArgumentException($"Unknown data type: {dataType}", nameof(dataType))
        };
    }

    /// <summary>
    /// ONNXのDataType値からTensorDataTypeに変換
    /// </summary>
    /// <param name="onnxDataType">ONNXのDataType値</param>
    /// <returns>対応するTensorDataType</returns>
    public static TensorDataType FromOnnxDataType(int onnxDataType)
    {
        // ONNX TensorProto.DataType の値に基づく変換
        return onnxDataType switch
        {
            1 => TensorDataType.Float32, // FLOAT
            2 => TensorDataType.UInt8, // UINT8
            3 => TensorDataType.Int8, // INT8
            4 => TensorDataType.UInt16, // UINT16
            5 => TensorDataType.Int16, // INT16
            6 => TensorDataType.Int32, // INT32
            7 => TensorDataType.Int64, // INT64
            9 => TensorDataType.Bool, // BOOL
            10 => TensorDataType.Float16, // FLOAT16
            11 => TensorDataType.Float64, // DOUBLE
            12 => TensorDataType.UInt32, // UINT32
            13 => TensorDataType.UInt64, // UINT64
            16 => TensorDataType.BFloat16, // BFLOAT16
            _ => TensorDataType.Unknown
        };
    }

    /// <summary>
    /// TensorDataTypeをONNXのDataType値に変換
    /// </summary>
    /// <param name="dataType">データ型</param>
    /// <returns>ONNXのDataType値</returns>
    public static int ToOnnxDataType(this TensorDataType dataType)
    {
        return dataType switch
        {
            TensorDataType.Float32 => 1, // FLOAT
            TensorDataType.UInt8 => 2, // UINT8
            TensorDataType.Int8 => 3, // INT8
            TensorDataType.UInt16 => 4, // UINT16
            TensorDataType.Int16 => 5, // INT16
            TensorDataType.Int32 => 6, // INT32
            TensorDataType.Int64 => 7, // INT64
            TensorDataType.Bool => 9, // BOOL
            TensorDataType.Float16 => 10, // FLOAT16
            TensorDataType.Float64 => 11, // DOUBLE
            TensorDataType.UInt32 => 12, // UINT32
            TensorDataType.UInt64 => 13, // UINT64
            TensorDataType.BFloat16 => 16, // BFLOAT16
            _ => 0 // UNDEFINED
        };
    }

    /// <summary>
    /// Sentis ScalarType値からTensorDataTypeに変換
    /// </summary>
    /// <param name="scalarType">SentisのScalarType値</param>
    /// <returns>対応するTensorDataType</returns>
    public static TensorDataType FromSentisScalarType(int scalarType)
    {
        // SentisFlatBuffer.ScalarType の値に基づく変換
        return scalarType switch
        {
            0 => TensorDataType.Float32, // FLOAT
            1 => TensorDataType.Int32, // INT
            2 => TensorDataType.UInt8, // BYTE
            3 => TensorDataType.Int16, // SHORT
            _ => TensorDataType.Unknown
        };
    }

    /// <summary>
    /// TensorDataTypeをSentis ScalarType値に変換
    /// </summary>
    /// <param name="dataType">データ型</param>
    /// <returns>SentisのScalarType値</returns>
    public static int ToSentisScalarType(this TensorDataType dataType)
    {
        return dataType switch
        {
            TensorDataType.Float32 => 0, // FLOAT
            TensorDataType.Float64 => 0, // FLOAT (Sentisはfloat64未対応のためfloat32として扱う)
            TensorDataType.Int32 => 1, // INT
            TensorDataType.Int64 => 1, // INT (Sentisはint64未対応のためint32として扱う)
            TensorDataType.UInt8 => 2, // BYTE
            TensorDataType.Int8 => 2, // BYTE (符号付きもBYTEとして扱う)
            TensorDataType.Int16 => 3, // SHORT
            TensorDataType.UInt16 => 3, // SHORT (符号なしもSHORTとして扱う)
            _ => 0 // デフォルトはFLOAT
        };
    }
}