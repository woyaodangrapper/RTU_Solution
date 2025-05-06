using System.Collections.Concurrent;

namespace System.Net.Sockets;

/// <summary>
/// 提供对 NetworkStream 的扩展方法，用于批量写入数据以及获取 TcpClient 的网络流。
/// </summary>
public static class NetworkStreamExtensions
{
    /// <summary>
    /// 将字节数组异步写入到多个 NetworkStream 中，支持并发写入。
    /// </summary>
    /// <param name="networks">要写入的 NetworkStream 集合。</param>
    /// <param name="bytes">要写入的字节数组。</param>
    public static async Task WriteAsync(this IEnumerable<NetworkStream> networks, byte[] bytes)
        => await Task.WhenAll(
            networks.Select(network => network.WriteAsync(bytes, 0, bytes.Length))
        ).ConfigureAwait(false);

    /// <summary>
    /// 获取 TcpClient 的 NetworkStream。如果传入单个客户端，则返回该客户端的 NetworkStream；否则返回字典中所有 TcpClient 的 NetworkStream。
    /// </summary>
    /// <param name="clients">保存 TcpClient 的字典。</param>
    /// <param name="client">可选，指定单个 TcpClient。</param>
    /// <returns>NetworkStream 集合。</returns>
    public static IEnumerable<NetworkStream> GetStream(this ConcurrentDictionary<string, TcpClient> clients, TcpClient? client = null)
        => client is not null
            ? [client.GetStream()]
            : clients.Select(item => item.Value.GetStream());
}
