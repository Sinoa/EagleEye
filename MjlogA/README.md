# MjlogA - 牌譜データ分析ライブラリ

麻雀牌譜（mjlog形式）から機械学習の前処理に必要なデータ分析を行うライブラリです。

## 機能

- 牌譜データから統計情報を収集・分析
- 分析結果をCSV形式で出力

### 分析項目

1. **和了時の点数分布**
   - 最大値、最小値、平均値、中央値、最頻値、標準偏差
   - 第1四分位数(Q1)、第3四分位数(Q3)、四分位範囲(IQR)

2. **全試合の局数分布**
   - 最大値、最小値、平均値、中央値、最頻値、標準偏差
   - 第1四分位数(Q1)、第3四分位数(Q3)、四分位範囲(IQR)

3. **全試合の巡目分布**
   - 親のツモ回数で巡目をカウント（親が1回ツモ→1巡目）
   - 最大値、最小値、平均値、中央値、最頻値、標準偏差
   - 第1四分位数(Q1)、第3四分位数(Q3)、四分位範囲(IQR)

4. **各役の出現頻度**
   - 役ごとの出現回数、出現率（和了回数を母数）

5. **ドラ牌の出現頻度**
   - ドラ表示牌：表示牌そのままの出現頻度
   - 実際のドラ牌：表示牌から算出した実ドラの出現頻度

## 使用方法

### 基本的な使い方

```csharp
using MjlogA.Analyzers;
using MjlogA.Formatters;
using MjlogJ;

// 分析器を作成
var analyzer = new MjlogAnalyzer();

// 牌譜を読み込んで分析
await foreach (var result in MjlogReader.LoadManyParallelAsync(files))
{
    if (result.Record != null)
    {
        analyzer.Analyze(result.Record);
    }
}

// 結果を取得
var analysisResult = analyzer.GetResult();

// CSV形式で出力
CsvFormatter.Write(analysisResult, Console.Out);
```

### 個別の分析器を使用

```csharp
using MjlogA.Analyzers;
using MjlogA.Models;
using MjlogJ;

// 和了点数のみを分析
var scoreAnalyzer = new ScoreAnalyzer();
foreach (var game in games)
{
    scoreAnalyzer.Analyze(game);
}
var scoreStats = DistributionStatistics.Calculate(scoreAnalyzer.Scores);

// 役の出現頻度のみを分析
var yakuAnalyzer = new YakuAnalyzer();
foreach (var game in games)
{
    yakuAnalyzer.Analyze(game);
}
var yakuFrequencies = yakuAnalyzer.GetResults();
```

## 主要なクラス

### Analyzers

| クラス | 説明 |
|--------|------|
| `MjlogAnalyzer` | 統合分析クラス（すべての分析を一括実行） |
| `ScoreAnalyzer` | 和了時の点数分布を分析 |
| `RoundCountAnalyzer` | 試合の局数分布を分析 |
| `TurnCountAnalyzer` | 巡目分布を分析 |
| `YakuAnalyzer` | 役の出現頻度を分析 |
| `DoraAnalyzer` | ドラ牌の出現頻度を分析 |

### Models

| クラス | 説明 |
|--------|------|
| `DistributionStatistics` | 分布統計（最大/最小/平均/中央値/最頻値/標準偏差/Q1/Q3/IQR） |
| `FrequencyResult` | 出現頻度（項目名、出現回数、出現率） |
| `FrequencyCollection` | 出現頻度の集計ヘルパー |
| `AnalysisResult` | 全分析結果を保持 |

### Formatters

| クラス | 説明 |
|--------|------|
| `CsvFormatter` | 分析結果をCSV形式で出力 |

## 出力形式

CSV形式で以下のセクションを出力します：

```csv
# サマリー
項目,値
試合数,1000
局数,8500
和了回数,6200

# 和了時の点数分布
統計項目,値
データ数,6200
最小値,1000.00
最大値,48000.00
平均値,5420.50
...

# 各役の出現頻度
役名,出現回数,出現率
立直,2500,40.32%
断幺九,1800,29.03%
...
```

## 依存関係

- MjlogJ - 牌譜読み込みライブラリ

