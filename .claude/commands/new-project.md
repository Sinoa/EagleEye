# 新プロジェクトのスキャフォールド

引数: $ARGUMENTS

## 手順

### 1. 引数のパース

`$ARGUMENTS` から以下を取得する:
- **プロジェクト名** (例: `EagleEye.Core`, `EagleEye.DataPipeline`)
- **カテゴリ** (例: `Lib`, `ML`, `Sample`, `Exp`, `Cli`)

引数が不足している場合はユーザーに確認する。

### 2. プロジェクトディレクトリとcsproj作成

`src/{ProjectName}/{ProjectName}.csproj` を以下のテンプレートで作成:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <RootNamespace>Foxtamp.{ProjectName}</RootNamespace>
    </PropertyGroup>

</Project>
```

- `Sample` / `Exp` / `Cli` カテゴリの場合は `<OutputType>Exe</OutputType>` を追加する。

### 3. ソリューションファイルへの追加

`EagleEye.slnx` の該当カテゴリフォルダにプロジェクト参照を追加する。

カテゴリとフォルダの対応:
- `Lib` → `/Lib/`
- `ML` → `/ML/`
- `Sample` → `/Sample/`
- `Exp` → `/Exp/`
- `Cli` → 新規 `/Cli/` フォルダを作成

### 4. 依存関係のテンプレート提示

カテゴリに応じた典型的な依存関係をユーザーに提案する:

- **Lib**: 依存なし（コアライブラリ）
- **ML**: `TorchSharp-cpu`, `MLCoreModule` への参照
- **Sample/Exp/Cli**: `System.CommandLine`, 関連Libプロジェクトへの参照
- **DataPipeline固有**: `MjlogReader`, `MjlogReplayer` への参照

ユーザーの確認を得てから `<ProjectReference>` や `<PackageReference>` を追加する。

### 5. 初期ファイルの生成

カテゴリに応じた初期ファイルを作成:

- **Lib/ML**: `src/{ProjectName}/Class1.cs` (空のpublicクラス、名前はユーザーに確認)
- **Sample/Exp/Cli**: `src/{ProjectName}/Program.cs` (エントリポイント)

### 6. ビルド確認

`dotnet build src/{ProjectName}/{ProjectName}.csproj` でビルドが通ることを確認する。

### 7. 完了報告

作成したファイル一覧と、次のステップ（依存関係の追加、CLAUDE.mdのソリューション構成セクション更新など）を提示する。
