namespace Asprtu.Rtu.TcpClient.Contracts;

/// <summary>
/// 工厂接口，用于创建 TCP 客户端。
/// </summary>
public interface ITcpClientFactory
{
    /// <summary>
    /// 创建一个 TCP 客户端实例，用于处理客户端请求。
    /// </summary>
    ITcpClient CreateTcpClient(ChannelOptions options);
}
