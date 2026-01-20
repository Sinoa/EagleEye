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

namespace Foxtamp.MjlogReader.Models;

/// <summary>
/// 牌譜ドキュメント（牌譜全体のルートオブジェクト）
/// </summary>
public class MjlogDocument
{
    /// <summary>プレイヤー人数（3または4）</summary>
    public int PlayerCount { get; }

    /// <summary>牌譜ヘッダー情報</summary>
    public MjlogHeader Header { get; set; }

    /// <summary>セッション（局）のリスト</summary>
    public List<MjlogSession> Sessions { get; set; } = [];

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="playerCount">プレイヤー人数（3または4）</param>
    public MjlogDocument(int playerCount)
    {
        PlayerCount = playerCount;
        Header = new MjlogHeader(playerCount);
    }
}