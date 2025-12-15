# NuGet 打包和发布工具

这个工具用于自动化打包和发布 RTU Solution 的 NuGet 包，不会修改项目文件。

## 快速开始

### 1. 赋予脚本执行权限

```bash
chmod +x build/nuget/pack.sh
```

### 2. 基本用法

```bash
# 进入脚本目录
cd build/nuget

# 打包单个项目
./pack.sh -p Asprtu.Rtu.DLT645 -v 1.0.0

# 打包所有项目
./pack.sh -a -v 1.0.0

# 打包并发布
./pack.sh -p Asprtu.Rtu.DLT645 -v 1.0.0 --publish
```

## 可用项目

当前支持打包的项目：

- `Asprtu.Rtu` - 基础设施核心库
- `Asprtu.Rtu.DLT645` - DLT645 协议实现
- `Asprtu.Rtu.TcpServer` - TCP 服务器
- `Asprtu.Rtu.TcpClient` - TCP 客户端

## 命令行选项

### 项目选择

- `-p, --project <name>` - 指定要打包的项目（可多次使用）
- `-a, --all` - 打包所有可用项目

### 版本控制

- `-v, --version <version>` - 指定完整版本号（如 1.0.0）
- `-s, --suffix <suffix>` - 版本后缀（如 beta.1, rc.1）

### 编译配置

- `-c, --configuration <cfg>` - 编译配置（默认: Release）
- `-o, --output <path>` - 输出目录（默认: ./nupkgs）

### 发布选项

- `--publish` - 打包后发布到 NuGet
- `--api-key <key>` - NuGet API Key
- `--source <url>` - NuGet 源（默认: nuget.org）

### 其他选项

- `--dry-run` - 模拟运行，不实际执行
- `-h, --help` - 显示帮助信息

## 使用示例

### 示例 1: 打包单个项目（本地测试）

```bash
./pack.sh -p Asprtu.Rtu.DLT645 -v 1.0.0
```

这将创建：
- `nupkgs/Asprtu.Rtu.DLT645.1.0.0.nupkg` - 主包
- `nupkgs/Asprtu.Rtu.DLT645.1.0.0.snupkg` - 符号包

### 示例 2: 打包多个项目

```bash
./pack.sh -p Asprtu.Rtu -p Asprtu.Rtu.DLT645 -v 1.2.0
```

### 示例 3: 打包所有项目

```bash
./pack.sh -a -v 1.0.0
```

### 示例 4: 打包预览版本

```bash
./pack.sh -p Asprtu.Rtu.DLT645 -v 1.0.0 -s beta.1
```

这将生成版本号为 `1.0.0-beta.1` 的包。

### 示例 5: 打包 RC 版本

```bash
./pack.sh -a -v 2.0.0 -s rc.1
```

### 示例 6: 打包并发布到 NuGet.org

```bash
# 方式 1: 使用环境变量
export NUGET_API_KEY="your-api-key-here"
./pack.sh -p Asprtu.Rtu.DLT645 -v 1.0.0 --publish

# 方式 2: 使用命令行参数
./pack.sh -p Asprtu.Rtu.DLT645 -v 1.0.0 --publish --api-key "your-api-key-here"
```

### 示例 7: 发布到私有 NuGet 源

```bash
./pack.sh -a -v 1.0.0 --publish \
  --api-key "your-private-key" \
  --source "https://your-private-nuget-server/api/v3/index.json"
```

### 示例 8: 模拟运行（不实际执行）

```bash
./pack.sh -a -v 1.0.0 --publish --dry-run
```

这会显示将要执行的所有操作，但不会实际执行。

### 示例 9: 指定输出目录

```bash
./pack.sh -p Asprtu.Rtu.DLT645 -v 1.0.0 -o ./my-packages
```

## 工作流程建议

### 开发阶段

```bash
# 打包本地测试版本
./pack.sh -p Asprtu.Rtu.DLT645 -v 0.1.0 -s dev.$(date +%Y%m%d%H%M)
```

### 测试阶段

```bash
# 打包 beta 版本
./pack.sh -a -v 1.0.0 -s beta.1
```

### 发布候选

```bash
# 打包 RC 版本
./pack.sh -a -v 1.0.0 -s rc.1 --publish
```

### 正式发布

```bash
# 1. 先模拟运行检查
./pack.sh -a -v 1.0.0 --publish --dry-run

# 2. 确认无误后正式发布
./pack.sh -a -v 1.0.0 --publish
```

## 环境变量

### NUGET_API_KEY

设置默认的 NuGet API Key，避免每次都输入：

```bash
# Linux/macOS - 添加到 ~/.bashrc 或 ~/.zshrc
export NUGET_API_KEY="your-api-key-here"

# Windows PowerShell
$env:NUGET_API_KEY="your-api-key-here"
```

## 输出结构

脚本执行后，会在 `build/nuget/nupkgs/` 目录生成以下文件：

```
nupkgs/
├── Asprtu.Rtu.1.0.0.nupkg
├── Asprtu.Rtu.1.0.0.snupkg
├── Asprtu.Rtu.DLT645.1.0.0.nupkg
├── Asprtu.Rtu.DLT645.1.0.0.snupkg
├── Asprtu.Rtu.TcpServer.1.0.0.nupkg
├── Asprtu.Rtu.TcpServer.1.0.0.snupkg
├── Asprtu.Rtu.TcpClient.1.0.0.nupkg
└── Asprtu.Rtu.TcpClient.1.0.0.snupkg
```

- `.nupkg` - 主 NuGet 包
- `.snupkg` - 符号包（包含 PDB 和源码信息，用于调试）

## 包元数据

脚本会自动设置以下包元数据（不修改项目文件）：

- **PackageId**: 项目名称
- **Authors**: Asprtu
- **Company**: Asprtu
- **Product**: 项目名称
- **PackageProjectUrl**: https://github.com/woyaodangrapper/RTU_Solution
- **RepositoryUrl**: https://github.com/woyaodangrapper/RTU_Solution
- **RepositoryType**: git
- **PackageLicenseExpression**: MIT
- **PackageTags**: rtu;dlt645;tcp;protocol

## 故障排除

### 问题 1: 权限被拒绝

```bash
bash: ./pack.sh: Permission denied
```

**解决方案**:
```bash
chmod +x pack.sh
```

### 问题 2: 找不到项目文件

```bash
[ERROR] 项目文件不存在: ../../src/xxx/xxx.csproj
```

**解决方案**: 
- 确保在 `build/nuget/` 目录下执行脚本
- 检查项目文件路径是否正确

### 问题 3: 发布失败 - 缺少 API Key

```bash
[ERROR] 发布需要 API Key
```

**解决方案**:
```bash
# 设置环境变量
export NUGET_API_KEY="your-key"

# 或使用命令行参数
./pack.sh -p xxx -v 1.0.0 --publish --api-key "your-key"
```

### 问题 4: dotnet 命令未找到

```bash
dotnet: command not found
```

**解决方案**: 安装 .NET SDK
```bash
# Linux (Ubuntu/Debian)
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0

# macOS
brew install dotnet

# 或从官网下载: https://dotnet.microsoft.com/download
```

## 高级用法

### 1. 添加新的可发布项目

编辑 `pack.sh`，在 `AVAILABLE_PROJECTS` 数组中添加：

```bash
declare -A AVAILABLE_PROJECTS=(
    # ...existing projects...
    ["YourNewProject"]="../../src/YourNewProject/YourNewProject.csproj"
)
```

### 2. 自定义包元数据

如果需要为特定项目自定义元数据，可以在 `pack_project()` 函数中添加条件逻辑：

```bash
# 在 pack_project() 函数中
if [ "$project_name" = "Asprtu.Rtu.DLT645" ]; then
    pack_args+=("-p:Description=DLT645 protocol implementation")
fi
```

### 3. 集成到 CI/CD

```yaml
# GitHub Actions 示例
- name: Pack and Publish NuGet
  run: |
    cd build/nuget
    chmod +x pack.sh
    ./pack.sh -a -v ${{ github.ref_name }} --publish
  env:
    NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
```

## 获取 NuGet API Key

1. 访问 https://www.nuget.org
2. 登录你的账号
3. 点击右上角用户名 -> API Keys
4. 创建新的 API Key，选择合适的权限和包范围

## 注意事项

1. **版本管理**: 脚本指定的版本会覆盖项目文件中的版本
2. **符号包**: 自动生成 `.snupkg` 符号包，用于源码调试
3. **依赖项**: 会自动包含项目引用的依赖项
4. **不修改源码**: 所有配置通过命令行参数传递，不修改 `.csproj` 文件
5. **重复发布**: 使用 `--skip-duplicate` 避免重复发布相同版本

## 许可证

MIT License
