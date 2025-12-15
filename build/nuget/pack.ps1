# NuGet 打包和发布脚本 (PowerShell 版本)
# 用法: .\pack.ps1 [选项]
# 示例: .\pack.ps1 -Project Asprtu.Rtu.DLT645 -Version 1.0.0 -Publish

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string[]]$Project,
    
    [Parameter(Mandatory=$false)]
    [switch]$All,
    
    [Parameter(Mandatory=$false)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [string]$Suffix,
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".\nupkgs",
    
    [Parameter(Mandatory=$false)]
    [switch]$Publish,
    
    [Parameter(Mandatory=$false)]
    [string]$ApiKey = $env:NUGET_API_KEY,
    
    [Parameter(Mandatory=$false)]
    [string]$Source = "https://api.nuget.org/v3/index.json",
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun,
    
    [Parameter(Mandatory=$false)]
    [switch]$Help,
    
    [Parameter(Mandatory=$false)]
    [switch]$NoLogo
)

# 设置控制台编码为 UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
chcp 65001 | Out-Null

$ErrorActionPreference = "Stop"

# 项目配置
$AvailableProjects = @{
    "Asprtu.Rtu" = "..\..\src\Infrastructures\Asprtu.Rtu\Asprtu.Rtu.csproj"
    "Asprtu.Rtu.DLT645" = "..\..\src\Asprtu.Rtu.DLT645\Asprtu.Rtu.DLT645.csproj"
    "Asprtu.Rtu.TcpServer" = "..\..\src\Asprtu.Rtu.TcpServer\Asprtu.Rtu.TcpServer.csproj"
    "Asprtu.Rtu.TcpClient" = "..\..\src\Asprtu.Rtu.TcpClient\Asprtu.Rtu.TcpClient.csproj"
}

# 日志函数
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Blue
}

# 显示帮助
function Show-Help {
    Write-Host @"
NuGet 打包和发布脚本 (PowerShell)

用法: .\pack.ps1 [选项]

选项:
  -Project <name>           指定要打包的项目名称 (可多次使用)
                            可用项目: $($AvailableProjects.Keys -join ', ')
  -All                      打包所有可用项目
  -Version <version>        指定版本号 (例如: 1.0.0)
  -Suffix <suffix>          版本后缀 (例如: beta.1, rc.1)
  -Configuration <cfg>      编译配置 (默认: Release)
  -OutputPath <path>        输出目录 (默认: .\nupkgs)
  -Publish                  打包后发布到 NuGet
  -ApiKey <key>             NuGet API Key (或使用 NUGET_API_KEY 环境变量)
  -Source <url>             NuGet 源 (默认: nuget.org)
  -DryRun                   模拟运行，不实际执行打包
  -NoLogo                   不显示 .NET 启动横幅
  -Help                     显示此帮助信息

示例:
  # 打包单个项目
  .\pack.ps1 -Project Asprtu.Rtu.DLT645 -Version 1.0.0

  # 打包多个项目
  .\pack.ps1 -Project Asprtu.Rtu,Asprtu.Rtu.DLT645 -Version 1.2.0

  # 打包所有项目
  .\pack.ps1 -All -Version 1.0.0

  # 打包预览版
  .\pack.ps1 -Project Asprtu.Rtu.DLT645 -Version 1.0.0 -Suffix beta.1

  # 打包并发布
  .\pack.ps1 -Project Asprtu.Rtu.DLT645 -Version 1.0.0 -Publish

"@
}

# 显示标题
function Show-Banner {
    Write-Host @"

╔═══════════════════════════════════════╗
║   NuGet 打包和发布工具               ║
║   RTU Solution                        ║
╚═══════════════════════════════════════╝

"@ -ForegroundColor Cyan
}

# 验证参数
function Test-Arguments {
    if ($Help) {
        Show-Help
        exit 0
    }
    
    if (-not $All -and -not $Project) {
        Write-Error "请至少指定一个项目 (-Project) 或使用 -All 打包所有项目"
        Show-Help
        exit 1
    }
    
    if ($Project) {
        foreach ($proj in $Project) {
            if (-not $AvailableProjects.ContainsKey($proj)) {
                Write-Error "未知的项目: $proj"
                Write-Info "可用项目: $($AvailableProjects.Keys -join ', ')"
                exit 1
            }
        }
    }
    
    if ($Publish -and -not $ApiKey) {
        Write-Error "发布需要 API Key。请设置 NUGET_API_KEY 环境变量或使用 -ApiKey 参数"
        exit 1
    }
}

# 显示配置信息
function Show-Config {
    param([string[]]$Projects)
    
    Write-Step "配置信息"
    Write-Host "  项目: $($Projects -join ', ')"
    Write-Host "  配置: $Configuration"
    Write-Host "  输出: $OutputPath"
    if ($Version) {
        Write-Host "  版本: $Version"
    }
    if ($Suffix) {
        Write-Host "  后缀: $Suffix"
    }
    if ($Publish) {
        Write-Host "  发布: 是"
        Write-Host "  源: $Source"
    } else {
        Write-Host "  发布: 否"
    }
    if ($DryRun) {
        Write-Host "  模式: 模拟运行" -ForegroundColor Yellow
    }
    Write-Host ""
}

# 准备输出目录
function Initialize-OutputDirectory {
    Write-Step "准备输出目录: $OutputPath"
    
    if (-not $DryRun) {
        if (Test-Path $OutputPath) {
            Remove-Item $OutputPath -Recurse -Force
            Write-Info "已清理旧的输出目录"
        }
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
        Write-Success "输出目录已创建"
    } else {
        Write-Info "[DRY RUN] 将创建目录: $OutputPath"
    }
    Write-Host ""
}

# 打包单个项目
function Invoke-PackProject {
    param(
        [string]$ProjectName,
        [string]$ProjectPath
    )
    
    Write-Step "打包项目: $ProjectName"
    Write-Info "项目路径: $ProjectPath"
    
    if (-not (Test-Path $ProjectPath)) {
        Write-Error "项目文件不存在: $ProjectPath"
        return $false
    }
    
    # 构建参数
    $packArgs = @(
        "pack", $ProjectPath,
        "-c", $Configuration,
        "-o", $OutputPath,
        "--include-symbols",
        "--include-source",
        "-p:SymbolPackageFormat=snupkg",
        "-p:PackageId=$ProjectName",
        "-p:Authors=Asprtu",
        "-p:Company=Asprtu",
        "-p:Product=$ProjectName",
        "-p:PackageProjectUrl=https://github.com/woyaodangrapper/RTU_Solution",
        "-p:RepositoryUrl=https://github.com/woyaodangrapper/RTU_Solution",
        "-p:RepositoryType=git",
        "-p:PackageLicenseExpression=MIT",
        "-p:PackageIcon="
    )
    
    if ($Version) {
        $packArgs += "-p:Version=$Version"
        Write-Info "版本: $Version"
    }
    
    if ($Suffix) {
        $packArgs += "--version-suffix", $Suffix
        Write-Info "后缀: $Suffix"
    }
    
    if ($NoLogo) {
        $packArgs += "--nologo"
    }
    
    if ($DryRun) {
        Write-Info "[DRY RUN] 将执行: dotnet $($packArgs -join ' ')"
        return $true
    } else {
        Write-Info "执行打包命令..."
        try {
            $output = & dotnet @packArgs 2>&1
            $exitCode = $LASTEXITCODE
            
            # 显示输出 (可选，用于调试)
            # $output | ForEach-Object { Write-Host $_ }
            
            if ($exitCode -eq 0) {
                Write-Success "打包成功: $ProjectName"
                return $true
            } else {
                Write-Error "打包失败: $ProjectName (退出代码: $exitCode)"
                Write-Host $output -ForegroundColor Red
                return $false
            }
        } catch {
            Write-Error "打包失败: $ProjectName - $_"
            return $false
        }
    }
}

# 发布包
function Publish-Packages {
    Write-Step "发布 NuGet 包"
    
    $packages = Get-ChildItem -Path $OutputPath -Filter "*.nupkg" | Where-Object { $_.Name -notlike "*.symbols.nupkg" }
    $publishedCount = 0
    $failedCount = 0
    
    if ($packages.Count -eq 0) {
        Write-Warning "未找到要发布的包文件"
        return $false
    }
    
    foreach ($package in $packages) {
        Write-Info "发布: $($package.Name)"
        
        if ($DryRun) {
            Write-Info "[DRY RUN] 将发布: $($package.Name) 到 $Source"
            $publishedCount++
        } else {
            try {
                & dotnet nuget push $package.FullName --api-key $ApiKey --source $Source --skip-duplicate
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "发布成功: $($package.Name)"
                    $publishedCount++
                } else {
                    Write-Error "发布失败: $($package.Name)"
                    $failedCount++
                }
            } catch {
                Write-Error "发布失败: $($package.Name) - $_"
                $failedCount++
            }
        }
    }
    
    Write-Host ""
    Write-Info "发布统计: 成功 $publishedCount 个, 失败 $failedCount 个"
    
    return ($failedCount -eq 0)
}

# 主函数
function Main {
    Show-Banner
    Test-Arguments
    
    # 确定要打包的项目列表
    $selectedProjects = if ($All) {
        $AvailableProjects.Keys
    } else {
        $Project
    }
    
    Show-Config -Projects $selectedProjects
    Initialize-OutputDirectory
    
    $successCount = 0
    $failedCount = 0
    
    # 打包所有选中的项目
    foreach ($projectName in $selectedProjects) {
        $projectPath = $AvailableProjects[$projectName]
        if (Invoke-PackProject -ProjectName $projectName -ProjectPath $projectPath) {
            $successCount++
        } else {
            $failedCount++
        }
        Write-Host ""
    }
    
    # 显示打包统计
    Write-Step "打包统计"
    Write-Host "  成功: $successCount 个项目"
    Write-Host "  失败: $failedCount 个项目"
    Write-Host ""
    
    if ($failedCount -gt 0) {
        Write-Error "部分项目打包失败"
        exit 1
    }
    
    # 发布包
    if ($Publish) {
        if (Publish-Packages) {
            Write-Success "所有包已成功发布!"
        } else {
            Write-Error "部分包发布失败"
            exit 1
        }
    }
    
    Write-Success "所有操作完成!"
    
    # 显示输出文件
    if (-not $DryRun -and (Test-Path $OutputPath)) {
        Write-Host ""
        Write-Step "生成的包文件:"
        Get-ChildItem -Path $OutputPath -Filter "*.nupkg" | Format-Table Name, Length, LastWriteTime -AutoSize
    }
}

# 运行主函数
Main
