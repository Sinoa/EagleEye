// zlib License
// 
// Copyright (c) 2026 Sinoa
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

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

try
{
    using var runtime = new LogicGateModelRuntime("Assets/logic_gate_model.onnx");
    Console.WriteLine($"AND (false, false) = {runtime.OperationAnd(false, false)}");
    Console.WriteLine($"AND (false, true) = {runtime.OperationAnd(false, true)}");
    Console.WriteLine($"AND (true, false) = {runtime.OperationAnd(true, false)}");
    Console.WriteLine($"AND (true, true) = {runtime.OperationAnd(true, true)}");
    Console.WriteLine($"OR (false, false) = {runtime.OperationOr(false, false)}");
    Console.WriteLine($"OR (false, true) = {runtime.OperationOr(false, true)}");
    Console.WriteLine($"OR (true, false) = {runtime.OperationOr(true, false)}");
    Console.WriteLine($"OR (true, true) = {runtime.OperationOr(true, true)}");
    Console.WriteLine($"NOT (false) = {runtime.OperationNot(false)}");
    Console.WriteLine($"NOT (true) = {runtime.OperationNot(true)}");
    Console.WriteLine($"XOR (false, false) = {runtime.OperationXor(false, false)}");
    Console.WriteLine($"XOR (false, true) = {runtime.OperationXor(false, true)}");
    Console.WriteLine($"XOR (true, false) = {runtime.OperationXor(true, false)}");
    Console.WriteLine($"XOR (true, true) = {runtime.OperationXor(true, true)}");
}
catch (Exception error)
{
    Console.WriteLine($"エラーが発生しました:\n{error}");
}

/// <summary>
/// 論理ゲートを表現する推論モデルの実行クラス
/// </summary>
public sealed class LogicGateModelRuntime : IDisposable
{
    private readonly InferenceSession _session;

    public LogicGateModelRuntime(string modelPath)
    {
        var options = new SessionOptions()
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            InterOpNumThreads = 8,
            IntraOpNumThreads = 8,
        };

        _session = new InferenceSession(modelPath, options);
    }

    public bool OperationAnd(bool x, bool y)
    {
        var inputData = new[]
        {
            x ? 1.0f : 0.0f,
            y ? 1.0f : 0.0f,
            1.0f, 0.0f, 0.0f, 0.0f
        };

        return RunSession(inputData) >= 0.9f;
    }

    public bool OperationOr(bool x, bool y)
    {
        var inputData = new[]
        {
            x ? 1.0f : 0.0f,
            y ? 1.0f : 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f
        };

        return RunSession(inputData) >= 0.9f;
    }

    public bool OperationNot(bool x)
    {
        var inputData = new[]
        {
            x ? 1.0f : 0.0f,
            0.0f,
            0.0f, 0.0f, 1.0f, 0.0f
        };

        return RunSession(inputData) >= 0.9f;
    }

    public bool OperationXor(bool x, bool y)
    {
        var inputData = new[]
        {
            x ? 1.0f : 0.0f,
            y ? 1.0f : 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        };

        return RunSession(inputData) >= 0.9f;
    }

    private float RunSession(float[] inputData)
    {
        var inputTensor = new DenseTensor<float>(inputData, new[] { 1, inputData.Length });
        var inputs = new[] { NamedOnnxValue.CreateFromTensor("X", inputTensor) };

        using var result = _session.Run(inputs);
        if (result == null || result.Count == 0)
        {
            return -1.0f;
        }

        return result[0].AsTensor<float>()[0];
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}