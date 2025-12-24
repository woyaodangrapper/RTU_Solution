using Aspdcs.Rtu.Contracts;

namespace Aspdcs.Rtu.DLT645.Contracts;

/// <summary>
/// 工厂接口，用于创建 TCP 服务器。
/// </summary>
public interface IDlt645ClientFactory : ILibraryFactory<Dlt645Client>
{
    /// <summary>
    /// Creates a new client instance for communicating with a DLT645 device using the specified channel options.
    /// </summary>
    /// <param name="options">The configuration options that define how the client connects to the DLT645 device. Cannot be null.</param>
    /// <returns>An <see cref="IDlt645Client"/> instance configured with the specified channel options.</returns>
    IDlt645Client CreateDlt645Client(ChannelOptions options);

    /// <summary>
    /// Creates a new instance of a IDlt645Client for communicating with devices using the DL/T 645 protocol.
    /// </summary>
    /// <returns>A IDlt645Client instance that can be used to interact with DL/T 645 protocol devices.</returns>
    IDlt645Client CreateDlt645Client();


    /// <summary>
    /// 使用指定名称创建一个 Dlt645 客户端构建器。
    /// </summary>
    ChannelOptions.Builder CreateBuilder(string name);
}