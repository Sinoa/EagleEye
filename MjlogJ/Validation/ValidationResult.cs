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

namespace MjlogJ.Validation;

/// <summary>
/// バリデーションエラーの重要度
/// </summary>
public enum ValidationSeverity
{
    /// <summary>情報（参考情報）</summary>
    Info,
    /// <summary>警告（処理は続行可能だが注意が必要）</summary>
    Warning,
    /// <summary>エラー（データの整合性に問題あり）</summary>
    Error
}

/// <summary>
/// バリデーションエラー
/// </summary>
public class ValidationError
{
    /// <summary>エラーコード</summary>
    public string Code { get; init; } = "";

    /// <summary>エラーメッセージ</summary>
    public string Message { get; init; } = "";

    /// <summary>重要度</summary>
    public ValidationSeverity Severity { get; init; }

    /// <summary>関連する局番号（該当する場合）</summary>
    public int? RoundIndex { get; init; }

    /// <summary>関連するプレイヤーID（該当する場合）</summary>
    public int? PlayerId { get; init; }

    /// <summary>関連するシーケンス番号（該当する場合）</summary>
    public int? Sequence { get; init; }

    public override string ToString()
    {
        var location = "";
        if (RoundIndex.HasValue) location += $"局{RoundIndex + 1}";
        if (PlayerId.HasValue) location += $" P{PlayerId}";
        if (Sequence.HasValue) location += $" Seq{Sequence}";
        if (!string.IsNullOrEmpty(location)) location = $" [{location.Trim()}]";

        return $"[{Severity}] {Code}: {Message}{location}";
    }
}

/// <summary>
/// バリデーション結果
/// </summary>
public class ValidationResult
{
    /// <summary>バリデーションエラーのリスト</summary>
    public List<ValidationError> Errors { get; } = [];

    /// <summary>バリデーションが成功したかどうか（エラーがない）</summary>
    public bool IsValid => !HasErrors;

    /// <summary>エラーがあるかどうか</summary>
    public bool HasErrors => Errors.Any(e => e.Severity == ValidationSeverity.Error);

    /// <summary>警告があるかどうか</summary>
    public bool HasWarnings => Errors.Any(e => e.Severity == ValidationSeverity.Warning);

    /// <summary>エラーのみを取得</summary>
    public IEnumerable<ValidationError> GetErrors() =>
        Errors.Where(e => e.Severity == ValidationSeverity.Error);

    /// <summary>警告のみを取得</summary>
    public IEnumerable<ValidationError> GetWarnings() =>
        Errors.Where(e => e.Severity == ValidationSeverity.Warning);

    /// <summary>エラーを追加</summary>
    public void AddError(string code, string message, ValidationSeverity severity = ValidationSeverity.Error,
        int? roundIndex = null, int? playerId = null, int? sequence = null)
    {
        Errors.Add(new ValidationError
        {
            Code = code,
            Message = message,
            Severity = severity,
            RoundIndex = roundIndex,
            PlayerId = playerId,
            Sequence = sequence
        });
    }
}