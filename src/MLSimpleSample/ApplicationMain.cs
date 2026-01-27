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
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.optim;

var dataset = new LogicDataset();
var loader = new DataLoader(dataset, 1);

var model = new LogicGateModel();
var loss = MSELoss();
var optimizer = Adam(model.parameters(), lr: 0.01);
var epochs = 10000;
var prevLoss = 0.0f;

using (torch.enable_grad())
{
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
            if (prevLoss == 0.0f)
            {
                prevLoss = totalLoss;
            }

            Console.WriteLine($"エポック: {i + 1,5}/{epochs,5} 損失: {totalLoss / loader.Count,15:N12} 前回比: {(totalLoss - prevLoss) / prevLoss,10:P4}");
            prevLoss = totalLoss;
        }
    }
}