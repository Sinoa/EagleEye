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

using Foxtamp.MLSimpleSample;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.optim;

using var dataset = new LogicDataset();
using var loader = new DataLoader(dataset, 1);
using var model = new LogicGateModel();
using var loss = MSELoss();
using var optimizer = Adam(model.parameters(), lr: 0.01);
var epochs = 2000;
var firstLoss = 0.0f;
var prevLoss = 0.0f;

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

    if (i % 100 == 0 || i == epochs - 1)
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