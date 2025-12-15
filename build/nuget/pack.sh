#!/bin/bash
# NuGet 打包和发布脚本
# 用法: ./pack.sh [选项]
# 示例: ./pack.sh -p Asprtu.Rtu.DLT645 -v 1.0.0 --publish

set -e

# 默认配置
CONFIGURATION="Release"
OUTPUT_PATH="./nupkgs"
PUBLISH=false
VERSION=""
VERSION_SUFFIX=""
API_KEY="${NUGET_API_KEY}"
SOURCE="https://api.nuget.org/v3/index.json"
SELECTED_PROJECTS=()
DRY_RUN=false

# 项目配置 - 可以发布的项目列表
declare -A AVAILABLE_PROJECTS=(
    ["Asprtu.Rtu"]="../../src/Infrastructures/Asprtu.Rtu/Asprtu.Rtu.csproj"
    ["Asprtu.Rtu.DLT645"]="../../src/Asprtu.Rtu.DLT645/Asprtu.Rtu.DLT645.csproj"
    ["Asprtu.Rtu.TcpServer"]="../../src/Asprtu.Rtu.TcpServer/Asprtu.Rtu.TcpServer.csproj"
    ["Asprtu.Rtu.TcpClient"]="../../src/Asprtu.Rtu.TcpClient/Asprtu.Rtu.TcpClient.csproj"
)

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# 显示帮助信息
show_help() {
    cat << EOF
NuGet 打包和发布脚本

用法: ./pack.sh [选项]

选项:
  -p, --project <name>       指定要打包的项目名称 (可多次使用)
                             可用项目: ${!AVAILABLE_PROJECTS[@]}
  -a, --all                  打包所有可用项目
  -v, --version <version>    指定版本号 (例如: 1.0.0)
  -s, --suffix <suffix>      版本后缀 (例如: beta.1, rc.1)
  -c, --configuration <cfg>  编译配置 (默认: Release)
  -o, --output <path>        输出目录 (默认: ./nupkgs)
  --publish                  打包后发布到 NuGet
  --api-key <key>            NuGet API Key (或使用 NUGET_API_KEY 环境变量)
  --source <url>             NuGet 源 (默认: nuget.org)
  --dry-run                  模拟运行，不实际执行打包
  -h, --help                 显示此帮助信息

示例:
  # 打包单个项目
  ./pack.sh -p Asprtu.Rtu.DLT645 -v 1.0.0

  # 打包多个项目
  ./pack.sh -p Asprtu.Rtu -p Asprtu.Rtu.DLT645 -v 1.2.0

  # 打包所有项目
  ./pack.sh -a -v 1.0.0

  # 打包预览版
  ./pack.sh -p Asprtu.Rtu.DLT645 -v 1.0.0 -s beta.1

  # 打包并发布
  ./pack.sh -p Asprtu.Rtu.DLT645 -v 1.0.0 --publish

  # 使用自定义 API Key 发布
  ./pack.sh -a -v 1.0.0 --publish --api-key YOUR_API_KEY

EOF
}

# 日志函数
log_info() {
    echo -e "${CYAN}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_step() {
    echo -e "${BLUE}==>${NC} $1"
}

# 解析命令行参数
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -p|--project)
                if [ -z "${AVAILABLE_PROJECTS[$2]}" ]; then
                    log_error "未知的项目: $2"
                    log_info "可用项目: ${!AVAILABLE_PROJECTS[@]}"
                    exit 1
                fi
                SELECTED_PROJECTS+=("$2")
                shift 2
                ;;
            -a|--all)
                SELECTED_PROJECTS=("${!AVAILABLE_PROJECTS[@]}")
                shift
                ;;
            -v|--version)
                VERSION="$2"
                shift 2
                ;;
            -s|--suffix)
                VERSION_SUFFIX="$2"
                shift 2
                ;;
            -c|--configuration)
                CONFIGURATION="$2"
                shift 2
                ;;
            -o|--output)
                OUTPUT_PATH="$2"
                shift 2
                ;;
            --publish)
                PUBLISH=true
                shift
                ;;
            --api-key)
                API_KEY="$2"
                shift 2
                ;;
            --source)
                SOURCE="$2"
                shift 2
                ;;
            --dry-run)
                DRY_RUN=true
                shift
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                log_error "未知选项: $1"
                show_help
                exit 1
                ;;
        esac
    done
}

# 验证参数
validate_args() {
    if [ ${#SELECTED_PROJECTS[@]} -eq 0 ]; then
        log_error "请至少指定一个项目 (-p) 或使用 -a 打包所有项目"
        show_help
        exit 1
    fi

    if [ "$PUBLISH" = true ] && [ -z "$API_KEY" ]; then
        log_error "发布需要 API Key。请设置 NUGET_API_KEY 环境变量或使用 --api-key 参数"
        exit 1
    fi
}

# 显示配置信息
show_config() {
    log_step "配置信息"
    echo "  项目: ${SELECTED_PROJECTS[*]}"
    echo "  配置: $CONFIGURATION"
    echo "  输出: $OUTPUT_PATH"
    if [ -n "$VERSION" ]; then
        echo "  版本: $VERSION"
    fi
    if [ -n "$VERSION_SUFFIX" ]; then
        echo "  后缀: $VERSION_SUFFIX"
    fi
    if [ "$PUBLISH" = true ]; then
        echo "  发布: 是"
        echo "  源: $SOURCE"
    else
        echo "  发布: 否"
    fi
    if [ "$DRY_RUN" = true ]; then
        echo -e "  ${YELLOW}模式: 模拟运行${NC}"
    fi
    echo ""
}

# 清理并创建输出目录
prepare_output() {
    log_step "准备输出目录: $OUTPUT_PATH"
    
    if [ "$DRY_RUN" = false ]; then
        if [ -d "$OUTPUT_PATH" ]; then
            rm -rf "$OUTPUT_PATH"
            log_info "已清理旧的输出目录"
        fi
        mkdir -p "$OUTPUT_PATH"
        log_success "输出目录已创建"
    else
        log_info "[DRY RUN] 将创建目录: $OUTPUT_PATH"
    fi
    echo ""
}

# 打包单个项目
pack_project() {
    local project_name=$1
    local project_path=${AVAILABLE_PROJECTS[$project_name]}
    
    log_step "打包项目: $project_name"
    log_info "项目路径: $project_path"
    
    # 检查项目文件是否存在
    if [ ! -f "$project_path" ]; then
        log_error "项目文件不存在: $project_path"
        return 1
    fi
    
    # 构建 dotnet pack 命令参数
    local pack_args=(
        "pack"
        "$project_path"
        "-c" "$CONFIGURATION"
        "-o" "$OUTPUT_PATH"
        "--include-symbols"
        "--include-source"
        "-p:SymbolPackageFormat=snupkg"
        "-p:PackageId=$project_name"
        "-p:Authors=Asprtu"
        "-p:Company=Asprtu"
        "-p:Product=$project_name"
        "-p:PackageProjectUrl=https://github.com/woyaodangrapper/RTU_Solution"
        "-p:RepositoryUrl=https://github.com/woyaodangrapper/RTU_Solution"
        "-p:RepositoryType=git"
        "-p:PackageLicenseExpression=MIT"
        "-p:PackageIcon="
    )
    
    # 添加版本参数
    if [ -n "$VERSION" ]; then
        pack_args+=("-p:Version=$VERSION")
        log_info "版本: $VERSION"
    fi
    
    if [ -n "$VERSION_SUFFIX" ]; then
        pack_args+=("--version-suffix" "$VERSION_SUFFIX")
        log_info "后缀: $VERSION_SUFFIX"
    fi
    
    # 执行打包
    if [ "$DRY_RUN" = false ]; then
        log_info "执行命令: dotnet ${pack_args[*]}"
        if dotnet "${pack_args[@]}"; then
            log_success "打包成功: $project_name"
            return 0
        else
            log_error "打包失败: $project_name"
            return 1
        fi
    else
        log_info "[DRY RUN] 将执行: dotnet ${pack_args[*]}"
        return 0
    fi
}

# 发布包
publish_packages() {
    log_step "发布 NuGet 包"
    
    local packages=("$OUTPUT_PATH"/*.nupkg)
    local published_count=0
    local failed_count=0
    
    for package in "${packages[@]}"; do
        # 跳过符号包
        if [[ "$package" =~ \.symbols\.nupkg$ ]]; then
            continue
        fi
        
        # 检查文件是否存在 (避免通配符不匹配的情况)
        if [ ! -f "$package" ]; then
            continue
        fi
        
        local package_name=$(basename "$package")
        log_info "发布: $package_name"
        
        if [ "$DRY_RUN" = false ]; then
            if dotnet nuget push "$package" \
                --api-key "$API_KEY" \
                --source "$SOURCE" \
                --skip-duplicate; then
                log_success "发布成功: $package_name"
                ((published_count++))
            else
                log_error "发布失败: $package_name"
                ((failed_count++))
            fi
        else
            log_info "[DRY RUN] 将发布: $package_name 到 $SOURCE"
            ((published_count++))
        fi
    done
    
    echo ""
    log_info "发布统计: 成功 $published_count 个, 失败 $failed_count 个"
    
    if [ $failed_count -gt 0 ]; then
        return 1
    fi
    return 0
}

# 主函数
main() {
    echo -e "${CYAN}"
    cat << "EOF"
╔═══════════════════════════════════════╗
║   NuGet 打包和发布工具               ║
║   RTU Solution                        ║
╚═══════════════════════════════════════╝
EOF
    echo -e "${NC}"
    
    parse_args "$@"
    validate_args
    show_config
    prepare_output
    
    local success_count=0
    local failed_count=0
    
    # 打包所有选中的项目
    for project_name in "${SELECTED_PROJECTS[@]}"; do
        if pack_project "$project_name"; then
            ((success_count++))
        else
            ((failed_count++))
        fi
        echo ""
    done
    
    # 显示打包统计
    log_step "打包统计"
    echo "  成功: $success_count 个项目"
    echo "  失败: $failed_count 个项目"
    echo ""
    
    if [ $failed_count -gt 0 ]; then
        log_error "部分项目打包失败"
        exit 1
    fi
    
    # 发布包 (如果需要)
    if [ "$PUBLISH" = true ]; then
        if publish_packages; then
            log_success "所有包已成功发布!"
        else
            log_error "部分包发布失败"
            exit 1
        fi
    fi
    
    log_success "所有操作完成!"
    
    # 显示输出文件
    if [ "$DRY_RUN" = false ] && [ -d "$OUTPUT_PATH" ]; then
        echo ""
        log_step "生成的包文件:"
        ls -lh "$OUTPUT_PATH"/*.nupkg 2>/dev/null || log_warning "未找到包文件"
    fi
}

# 运行主函数
main "$@"
