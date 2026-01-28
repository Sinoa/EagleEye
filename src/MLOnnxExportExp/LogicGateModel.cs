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

using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.utils.data;

namespace Foxtamp.MLOnnxExportExp;

/// <summary>
/// 論理ゲートを表現する推論モデル
/// </summary>
public sealed class LogicGateModel : Module<Tensor, Tensor>
{
    private const int InputSize = 2 + 4; // (X and Y) + LogicOneHot（And, Or, Not, Xor）
    private const int HiddenSize = 16;
    private const int OutputSize = 1;

    [ComponentName(Name = "mlp")]
    private readonly Sequential _mlp;

    public LogicGateModel() : base("LogicGateModel")
    {
        _mlp = Sequential(
            (name: "fc1", submodule: Linear(InputSize, HiddenSize)),
            (name: "mish1", submodule: Mish()),
            (name: "fc2", submodule: Linear(HiddenSize, HiddenSize)),
            (name: "mish2", submodule: Mish()),
            (name: "fc3", submodule: Linear(HiddenSize, HiddenSize)),
            (name: "mish3", submodule: Mish()),
            (name: "fc4", submodule: Linear(HiddenSize, OutputSize)),
            (name: "sigmoid1", submodule: Sigmoid())
        );

        RegisterComponents();
    }

    public override Tensor forward(Tensor input)
    {
        return _mlp.forward(input);
    }
}

/// <summary>
/// データセットクラス
/// </summary>
public class LogicDataset : Dataset
{
    public static readonly Tensor Input;
    public static readonly Tensor Output;

    static LogicDataset()
    {
        var inputData = new[,]
        {
            // X     Y    And    Or   Not   Xor
            { 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f }, // And(X:0, Y:0)
            { 0.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f }, // And(X:0, Y:1)
            { 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f }, // And(X:1, Y:0)
            { 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f }, // And(X:1, Y:1)
            { 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f }, // Or(X:0, Y:0)
            { 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f }, // Or(X:0, Y:1)
            { 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f }, // Or(X:1, Y:0)
            { 1.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f }, // Or(X:1, Y:1)
            { 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f }, // Not(X:0, Y:0)
            { 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f }, // Not(X:1, Y:0)
            { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f }, // Xor(X:0, Y:0)
            { 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f }, // Xor(X:0, Y:1)
            { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f }, // Xor(X:1, Y:0)
            { 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f }, // Xor(X:1, Y:1)
        };

        var outputData = new[,]
        {
            // Result
            { 0.0f }, // And(X:0, Y:0)
            { 0.0f }, // And(X:0, Y:1)
            { 0.0f }, // And(X:1, Y:0)
            { 1.0f }, // And(X:1, Y:1)
            { 0.0f }, // Or(X:0, Y:0)
            { 1.0f }, // Or(X:0, Y:1)
            { 1.0f }, // Or(X:1, Y:0)
            { 1.0f }, // Or(X:1, Y:1)
            { 1.0f }, // Not(X:0, Y:0)
            { 0.0f }, // Not(X:1, Y:0)
            { 0.0f }, // Xor(X:0, Y:0)
            { 1.0f }, // Xor(X:0, Y:1)
            { 1.0f }, // Xor(X:1, Y:0)
            { 0.0f }, // Xor(X:1, Y:1)
        };

        Input = tensor(inputData);
        Output = tensor(outputData);
    }

    public override Dictionary<string, Tensor> GetTensor(long index)
    {
        return new Dictionary<string, Tensor>
        {
            { "input", Input[index, TensorIndex.Colon] },
            { "output", Output[index, TensorIndex.Colon] },
        };
    }

    public override long Count => Input.shape[0];
}