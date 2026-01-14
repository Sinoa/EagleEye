// zlib License
// 
// Copyright (c) 2025 Sinoa
// 
// This software is provided ‘as-is’, without any express or implied
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

namespace MjlogConverter.Utils;

/// <summary>
/// 進捗表示ユーティリティ
/// </summary>
public class ProgressReporter
{
    private readonly bool _enabled;
    private readonly Lock _lock = new();
    private readonly char[] _spinnerChars = ['|', '/', '-', '\\'];
    private int _lastLineLength;
    private int _spinnerIndex;

    public ProgressReporter(bool enabled)
    {
        _enabled = enabled;
    }

    /// <summary>
    /// 進捗を表示
    /// </summary>
    /// <param name="fileName">処理中のファイル名</param>
    /// <param name="current">現在の処理数</param>
    /// <param name="total">総数</param>
    public void Report(string fileName, int current, int total)
    {
        if (!_enabled) return;

        lock (_lock)
        {
            // 回転文字
            var spinner = _spinnerChars[_spinnerIndex];
            _spinnerIndex = (_spinnerIndex + 1) % _spinnerChars.Length;

            // コンソール幅を取得
            int consoleWidth;
            try
            {
                consoleWidth = Console.WindowWidth;
            }
            catch
            {
                consoleWidth = 80; // デフォルト幅
            }

            // 基本情報
            var info = $" {spinner} 処理中: {TruncateFileName(fileName, 30)} {current}/{total} ";

            // プログレスバーの幅を計算（両端の [ ] を含む）
            var progressBarWidth = consoleWidth - info.Length - 3; // 3 = "[]" + 余裕
            if (progressBarWidth < 10) progressBarWidth = 10;
            if (progressBarWidth > 50) progressBarWidth = 50;

            // プログレスバーを生成
            var progressBar = GenerateProgressBar(current, total, progressBarWidth);

            // 出力
            var line = $"\r{info}[{progressBar}]";

            // 前回の出力より短い場合、スペースで埋める
            if (line.Length < _lastLineLength)
            {
                line += new string(' ', _lastLineLength - line.Length);
            }

            _lastLineLength = line.Length;

            Console.Write(line);
        }
    }

    /// <summary>
    /// 処理完了を表示
    /// </summary>
    /// <param name="processedCount">処理したファイル数</param>
    /// <param name="errorCount">エラー数</param>
    public void Complete(int processedCount, int errorCount)
    {
        if (!_enabled) return;

        lock (_lock)
        {
            // 現在行をクリア
            Console.Write($"\r{new string(' ', _lastLineLength)}\r");

            // 完了メッセージ
            Console.WriteLine($"完了: {processedCount} ファイル処理, {errorCount} エラー");
        }
    }

    /// <summary>
    /// エラーを表示（進捗が無効でも表示）
    /// </summary>
    /// <param name="fileName">ファイル名</param>
    /// <param name="error">エラーメッセージ</param>
    public void ReportError(string fileName, string error)
    {
        lock (_lock)
        {
            // 進捗表示を一時的にクリア
            if (_enabled && _lastLineLength > 0)
            {
                Console.Write($"\r{new string(' ', _lastLineLength)}\r");
            }

            // エラーを標準エラーに出力
            Console.Error.WriteLine($"エラー: {fileName} - {error}");

            _lastLineLength = 0;
        }
    }

    private static string GenerateProgressBar(int current, int total, int width)
    {
        if (total <= 0) return new string('-', width);

        var ratio = (double)current / total;
        var filled = (int)(width * ratio);
        var empty = width - filled;

        var bar = new string('#', filled) + new string('-', empty);
        return bar;
    }

    private static string TruncateFileName(string fileName, int maxLength)
    {
        if (fileName.Length <= maxLength) return fileName;

        // "..." + 末尾部分
        var suffix = fileName[^(maxLength - 3)..];
        return "..." + suffix;
    }
}