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

namespace MLModelUtility.Capabilities;

/// <summary>
/// モデルフォーマットが対応している機能を表すフラグ
/// </summary>
[Flags]
public enum ModelFormatCapability
{
    /// <summary>機能なし</summary>
    None = 0,

    /// <summary>テンソルデータの読み込みに対応</summary>
    TensorRead = 1 << 0,

    /// <summary>テンソルデータの書き込みに対応</summary>
    TensorWrite = 1 << 1,

    /// <summary>計算グラフの読み込みに対応</summary>
    GraphRead = 1 << 2,

    /// <summary>計算グラフの書き込みに対応</summary>
    GraphWrite = 1 << 3,

    /// <summary>テンソルの読み書き両対応</summary>
    TensorOnly = TensorRead | TensorWrite,

    /// <summary>計算グラフの読み書き両対応</summary>
    GraphOnly = GraphRead | GraphWrite,

    /// <summary>全機能対応</summary>
    Full = TensorOnly | GraphOnly
}