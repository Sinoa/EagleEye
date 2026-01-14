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

using MLModelUtility.Capabilities;

namespace MLModelUtility.IO;

/// <summary>
/// モデルフォーマットハンドラの基底インターフェース
/// </summary>
public interface IModelFormatHandler
{
    /// <summary>
    /// フォーマットの名前（例: "Safetensors", "ONNX"）
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// ファイル拡張子（例: ".safetensors", ".onnx"）
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// このフォーマットがサポートする機能
    /// </summary>
    ModelFormatCapability Capability { get; }

    /// <summary>
    /// 指定した機能をサポートしているか確認
    /// </summary>
    /// <param name="capability">確認する機能</param>
    /// <returns>サポートしている場合true</returns>
    bool Supports(ModelFormatCapability capability)
    {
        return (Capability & capability) == capability;
    }
}