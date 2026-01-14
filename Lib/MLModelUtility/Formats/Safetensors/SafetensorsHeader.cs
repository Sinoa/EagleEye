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

using System.Text.Json.Serialization;

namespace MLModelUtility.Formats.Safetensors;

/// <summary>
/// Safetensorsファイルの個々のテンソルヘッダー情報
/// </summary>
internal class SafetensorsTensorHeader
{
    /// <summary>
    /// データ型（例: "F32", "F16", "BF16"）
    /// </summary>
    [JsonPropertyName("dtype")]
    public string DataType { get; set; } = "";

    /// <summary>
    /// テンソルの形状
    /// </summary>
    [JsonPropertyName("shape")]
    public long[] Shape { get; set; } = [];

    /// <summary>
    /// データのオフセット範囲 [開始, 終了)
    /// バイナリデータ部分内での相対オフセット
    /// </summary>
    [JsonPropertyName("data_offsets")]
    public long[] DataOffsets { get; set; } = [];
}