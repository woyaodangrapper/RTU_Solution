using Asprtu.Rtu.Contracts;

namespace Asprtu.Rtu.TcpServer.Contracts;

/// <summary>
/// 工厂接口，用于创建 TCP 服务器。
/// </summary>
public interface ITcpServerFactory : ILibraryFactory<TcpServer>

{
    /// <summary>
    /// 创建一个 TCP 服务器实例，用于处理客户端请求。
    /// </summary>
    ITcpServer CreateTcpServer(ChannelOptions options);
}