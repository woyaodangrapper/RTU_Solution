# NuGet 打包配置文件
# 此文件用于定义包的通用配置，可以根据需要自定义

# 包信息配置
PACKAGE_AUTHORS="Asprtu"
PACKAGE_COMPANY="Asprtu"
PACKAGE_COPYRIGHT="Copyright ? Asprtu 2025"
PACKAGE_LICENSE="MIT"
PACKAGE_TAGS="rtu;dlt645;tcp;protocol;serial;modbus"

# 仓库信息
REPOSITORY_URL="https://github.com/woyaodangrapper/RTU_Solution"
REPOSITORY_TYPE="git"

# 项目 URL
PROJECT_URL="https://github.com/woyaodangrapper/RTU_Solution"

# NuGet 源配置
DEFAULT_NUGET_SOURCE="https://api.nuget.org/v3/index.json"

# 国内 NuGet 镜像源 (如果需要)
# CHINA_NUGET_SOURCE="https://nuget.cdn.azure.cn/v3/index.json"

# 私有 NuGet 源 (如果有)
# PRIVATE_NUGET_SOURCE="https://your-private-nuget-server/api/v3/index.json"

# 默认编译配置
DEFAULT_CONFIGURATION="Release"

# 符号包格式
SYMBOL_PACKAGE_FORMAT="snupkg"

# 是否包含符号包
INCLUDE_SYMBOLS=true

# 是否包含源码
INCLUDE_SOURCE=true

# 版本说明模板
# 可以在打包时引用这些模板
VERSION_NOTES_TEMPLATE="Release notes for version {VERSION}"

# 项目描述
declare -A PROJECT_DESCRIPTIONS=(
    ["Asprtu.Rtu"]="RTU core infrastructure library providing base functionality for communication protocols"
    ["Asprtu.Rtu.DLT645"]="Implementation of DLT645 power meter communication protocol"
    ["Asprtu.Rtu.TcpServer"]="TCP server implementation for RTU communication"
    ["Asprtu.Rtu.TcpClient"]="TCP client implementation for RTU communication"
)

# 项目特定的标签
declare -A PROJECT_TAGS=(
    ["Asprtu.Rtu"]="core;infrastructure"
    ["Asprtu.Rtu.DLT645"]="dlt645;power-meter;protocol"
    ["Asprtu.Rtu.TcpServer"]="tcp;server;networking"
    ["Asprtu.Rtu.TcpClient"]="tcp;client;networking"
)
