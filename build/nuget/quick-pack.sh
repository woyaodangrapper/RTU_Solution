#!/bin/bash
# 快速打包脚本示例

# 设置颜色
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}RTU Solution - 快速打包示例${NC}\n"

# 获取当前目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# 显示菜单
echo "请选择操作:"
echo "  1) 打包 DLT645 项目 (本地测试)"
echo "  2) 打包所有项目 (本地测试)"
echo "  3) 打包 DLT645 项目并发布"
echo "  4) 打包所有项目并发布"
echo "  5) 打包预览版本 (beta)"
echo "  6) 自定义打包"
echo ""
read -p "请输入选项 [1-6]: " choice

case $choice in
    1)
        echo -e "${YELLOW}打包 DLT645 项目...${NC}"
        ./pack.sh -p Asprtu.Rtu.DLT645 -v 1.0.0
        ;;
    2)
        echo -e "${YELLOW}打包所有项目...${NC}"
        ./pack.sh -a -v 1.0.0
        ;;
    3)
        read -p "请输入版本号 (例如 1.0.0): " version
        echo -e "${YELLOW}打包并发布 DLT645 项目...${NC}"
        ./pack.sh -p Asprtu.Rtu.DLT645 -v "$version" --publish
        ;;
    4)
        read -p "请输入版本号 (例如 1.0.0): " version
        echo -e "${YELLOW}打包并发布所有项目...${NC}"
        ./pack.sh -a -v "$version" --publish
        ;;
    5)
        read -p "请输入版本号 (例如 1.0.0): " version
        read -p "请输入后缀 (例如 beta.1): " suffix
        echo -e "${YELLOW}打包预览版本...${NC}"
        ./pack.sh -a -v "$version" -s "$suffix"
        ;;
    6)
        echo -e "${YELLOW}运行自定义打包...${NC}"
        echo "请输入完整的 pack.sh 命令参数 (不包括 ./pack.sh):"
        read -p "> " custom_args
        ./pack.sh $custom_args
        ;;
    *)
        echo "无效的选项"
        exit 1
        ;;
esac

echo ""
echo -e "${GREEN}完成!${NC}"
