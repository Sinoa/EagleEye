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

using TorchSharp;

namespace Foxtamp.MLModelExporter.Models.Abstract;

/// <summary>
/// TorchSharpテンソルデータを保持する抽象テンソル構造を表現するクラスです。
/// </summary>
/// <remarks>
/// このクラスはONNXのInitializerに対応し、モデルの学習済み重みや定数パラメータを表現します。
/// </remarks>
// ReSharper disable once InconsistentNaming
public sealed class MLAbstractTensor
{
    /// <summary>
    /// テンソル名を取得します。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// TorchSharpのテンソルデータを取得します。
    /// </summary>
    public torch.Tensor Tensor { get; }

    /// <summary>
    /// <see cref="MLAbstractTensor"/>クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="name">テンソル名</param>
    /// <param name="tensor">TorchSharpのテンソルデータ</param>
    public MLAbstractTensor(string name, torch.Tensor tensor)
    {
        Name = name;
        Tensor = tensor;
    }
}