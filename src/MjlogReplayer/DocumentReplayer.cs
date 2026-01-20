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

using Foxtamp.MjlogReader.Models;
using Foxtamp.MjlogReplayer.Models;

namespace Foxtamp.MjlogReplayer;

/// <summary>
/// MjlogDocumentから全セッションの試合状態履歴を再現するクラス
/// </summary>
public class DocumentReplayer
{
    private readonly MjlogDocument _document;
    private readonly List<SessionReplayer> _sessionReplayers = [];

    /// <summary>
    /// ドキュメント情報
    /// </summary>
    public MjlogDocument Document => _document;

    /// <summary>
    /// セッション数
    /// </summary>
    public int SessionCount => _sessionReplayers.Count;

    /// <summary>
    /// 全セッションのリプレイヤー
    /// </summary>
    public IReadOnlyList<SessionReplayer> Sessions => _sessionReplayers;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="document">再現するドキュメント</param>
    public DocumentReplayer(MjlogDocument document)
    {
        _document = document;
        ReplayDocument();
    }

    /// <summary>
    /// 指定セッションのリプレイヤーを取得
    /// </summary>
    /// <param name="sessionIndex">セッションインデックス</param>
    /// <returns>セッションリプレイヤー</returns>
    public SessionReplayer this[int sessionIndex]
    {
        get
        {
            if (sessionIndex < 0 || sessionIndex >= _sessionReplayers.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionIndex), $"セッションインデックスは0から{_sessionReplayers.Count - 1}の範囲で指定してください");
            }

            return _sessionReplayers[sessionIndex];
        }
    }

    /// <summary>
    /// セッションとステップを指定して試合状態を取得
    /// </summary>
    /// <param name="sessionIndex">セッションインデックス</param>
    /// <param name="stepIndex">ステップインデックス</param>
    /// <returns>試合状態</returns>
    public GameState GetState(int sessionIndex, int stepIndex)
    {
        return this[sessionIndex][stepIndex];
    }

    /// <summary>
    /// 全試合状態を列挙
    /// </summary>
    /// <returns>（セッションインデックス、ステップインデックス、試合状態）のタプル</returns>
    public IEnumerable<(int SessionIndex, int StepIndex, GameState State)> EnumerateAllStates()
    {
        for (var sessionIndex = 0; sessionIndex < _sessionReplayers.Count; sessionIndex++)
        {
            var session = _sessionReplayers[sessionIndex];
            for (var stepIndex = 0; stepIndex < session.StepCount; stepIndex++)
            {
                yield return (sessionIndex, stepIndex, session[stepIndex]);
            }
        }
    }

    /// <summary>
    /// 指定セッションの全試合状態を列挙
    /// </summary>
    /// <param name="sessionIndex">セッションインデックス</param>
    /// <returns>（ステップインデックス、試合状態）のタプル</returns>
    public IEnumerable<(int StepIndex, GameState State)> EnumerateSessionStates(int sessionIndex)
    {
        var session = this[sessionIndex];
        for (var stepIndex = 0; stepIndex < session.StepCount; stepIndex++)
        {
            yield return (stepIndex, session[stepIndex]);
        }
    }

    /// <summary>
    /// 全ステップ数を取得
    /// </summary>
    public int TotalStepCount => _sessionReplayers.Sum(s => s.StepCount);

    /// <summary>
    /// ドキュメントを再現して全セッションの状態履歴を構築
    /// </summary>
    private void ReplayDocument()
    {
        foreach (var session in _document.Sessions)
        {
            var replayer = new SessionReplayer(session);
            _sessionReplayers.Add(replayer);
        }
    }
}