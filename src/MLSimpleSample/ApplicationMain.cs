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

using Foxtamp.MLModelCodec.Encoders;
using Foxtamp.MLModelCodec.Models.Abstract;
using Foxtamp.MLSimpleSample;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.optim;

using var dataset = new LogicDataset();
using var loader = new DataLoader(dataset, 1);
using var model = new LogicGateModel();
using var loss = MSELoss();
using var optimizer = Adam(model.parameters(), lr: 0.01);
var epochs = 10000;
var firstLoss = 0.0f;
var prevLoss = 0.0f;

var modelFileName = "logic_gate_model.pt";
if (File.Exists(modelFileName))
{
    Console.WriteLine("==================== モデル読み込み ====================");
    model.load(modelFileName);
}
else
{
    Console.WriteLine("==================== 訓練開始 ====================");
    model.train();
    for (int i = 0; i < epochs; ++i)
    {
        float totalLoss = 0.0f;

        foreach (var data in loader)
        {
            optimizer.zero_grad();

            using var inputBatch = data["input"];
            using var outputBatch = data["output"];

            using var prediction = model.forward(inputBatch);
            using var batchLoss = loss.forward(prediction, outputBatch);
            batchLoss.backward();

            optimizer.step();
            totalLoss += batchLoss.item<float>();
        }

        if (i % 1000 == 0 || i == epochs - 1)
        {
            if (i == 0)
            {
                prevLoss = totalLoss;
                firstLoss = totalLoss;
            }

            Console.WriteLine($"エポック: {i + 1,5}/{epochs,5} 損失: {totalLoss / loader.Count,15:N12} 前回比: {(totalLoss - prevLoss) / prevLoss,10:P4} 初回比: {(totalLoss - firstLoss) / firstLoss,10:P4}");
            prevLoss = totalLoss;
        }
    }

    model.save(modelFileName);
}

Console.WriteLine("==================== 推論開始 ====================");
model.eval();
using var result = model.forward(LogicDataset.Input);
for (int i = 0; i < result.shape[0]; ++i)
{
    var opText = "";
    if (LogicDataset.Input[i, 2].item<float>() > 0.9f)
    {
        opText = "And";
    }
    else if (LogicDataset.Input[i, 3].item<float>() > 0.9f)
    {
        opText = "Or";
    }
    else if (LogicDataset.Input[i, 4].item<float>() > 0.9f)
    {
        opText = "Not";
    }
    else if (LogicDataset.Input[i, 5].item<float>() > 0.9f)
    {
        opText = "Xor";
    }

    var inputX = LogicDataset.Input[i, 0].item<float>();
    var inputY = LogicDataset.Input[i, 1].item<float>();
    var expectedValue = LogicDataset.Output[i, 0].item<float>();
    var predictedValue = result[i, 0].item<float>();
    var predictedLoss = MathF.Abs(expectedValue - predictedValue);
    Console.WriteLine($"オペレータ: {opText}, 入力: [{inputX}, {inputY}], 期待値: {expectedValue:N4}, 予測値: {predictedValue:N4}, 誤差: {predictedLoss:N6}");
}

Console.WriteLine("==================== エクスポート開始 ====================");

// ReSharper disable HeapView.ObjectAllocation
var modelInputInfos = new[] { new MLAbstractValueInfo("X", 6) };
var modelOutputInfos = new[] { new MLAbstractValueInfo("Y", 1) };
var modelInitializers = model.state_dict().Select(x => new MLAbstractTensor(x.Key, x.Value)).ToArray();
var modelNodes = new MLAbstractNode[]
{
    // MLP_hidden1
    // new("linear1", "MatMul", ["mlp.fc1.weight", "X"], ["linear1_out"]),
    // new("add1", "Add", ["linear1_out", "mlp.fc1.bias"], ["add1_out"]),
    new("gemm1", "Gemm", ["mlp.fc1.weight", "X", "mlp.fc1.bias"], ["gemm1_out"]),
    new("silu1", "Swish", ["gemm1_out"], ["silu1_out"]),
    // MLP_hidden2
    // new("linear2", "MatMul", ["mlp.fc2.weight", "silu1_out"], ["linear2_out"]),
    // new("add2", "Add", ["linear2_out", "mlp.fc2.bias"], ["add2_out"]),
    new("gemm2", "Gemm", ["mlp.fc2.weight", "silu1_out", "mlp.fc2.bias"], ["gemm2_out"]),
    new("silu2", "Swish", ["gemm2_out"], ["silu2_out"]),
    // MLP_hidden3
    // new("linear3", "MatMul", ["mlp.fc3.weight", "silu2_out"], ["linear3_out"]),
    // new("add3", "Add", ["linear3_out", "mlp.fc3.bias"], ["add3_out"]),
    new("gemm3", "Gemm", ["mlp.fc3.weight", "silu2_out", "mlp.fc3.bias"], ["gemm3_out"]),
    new("silu3", "Swish", ["gemm3_out"], ["silu3_out"]),
    // MLP_output
    // new("linear4", "MatMul", ["mlp.fc4.weight", "silu3_out"], ["linear4_out"]),
    // new("add4", "Add", ["linear4_out", "mlp.fc4.bias"], ["add4_out"]),
    new("gemm4", "Gemm", ["mlp.fc4.weight", "silu3_out", "mlp.fc4.bias"], ["gemm4_out"]),
    new("sigmoid", "Sigmoid", ["gemm4_out"], ["Y"]),
};
var modelGraph = new MLAbstractGraph("model.graph", modelInputInfos, modelOutputInfos, modelInitializers, modelNodes);
var abstractModel = new MLAbstractModel("jp.foxtamp.logicgate", "Sinoa", 1, "Sample", "1.0.0", "", new Dictionary<string, string>(), modelGraph);

var encoder = new MLModelEncoder();
encoder.Export(abstractModel, "logic_gate_model.onnx");
Console.WriteLine("完了");
// ReSharper restore HeapView.ObjectAllocation
