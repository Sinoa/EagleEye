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
/// テンソルデータへのアクセスを提供するインターフェース
/// オンメモリ実装やメモリマップ実装など、異なるバックエンドに対応可能
/// </summary>
public interface ITensorData : IDisposable
{
    /// <summary>
    /// テンソルのメタデータ情報
    /// </summary>
    TensorInfo Info { get; }

    /// <summary>
    /// テンソルデータへの読み取り専用アクセスを取得
    /// </summary>
    /// <returns>テンソルデータのバイト配列への読み取り専用スパン</returns>
    ReadOnlySpan<byte> GetDataSpan();

    /// <summary>
    /// テンソルデータへの読み取り専用メモリアクセスを取得
    /// 非同期操作での使用に適している
    /// </summary>
    /// <returns>テンソルデータのバイト配列への読み取り専用メモリ</returns>
    ReadOnlyMemory<byte> GetDataMemory();

    /// <summary>
    /// テンソルデータを指定した型の配列として取得
    /// </summary>
    /// <typeparam name="T">要素の型</typeparam>
    /// <returns>型変換されたデータのスパン</returns>
    ReadOnlySpan<T> GetDataAs<T>() where T : unmanaged;
}