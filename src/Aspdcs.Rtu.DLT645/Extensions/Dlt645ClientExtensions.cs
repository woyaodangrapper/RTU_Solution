using System.Diagnostics.CodeAnalysis;

namespace Aspdcs.Rtu.DLT645;

public static class Dlt645ClientExtensions
{

    /// <summary>
    /// 连接并初始化 Dlt645Client，封装 RunAsync，返回自身对象支持链式调用
    /// </summary>
    public static async Task<Dlt645Client> ConnectAsync([NotNull] this Dlt645Client client)
    {
        await client.RunAsync().ConfigureAwait(false);
        return client;
    }



    /// <summary>
    /// 连接并初始化 Dlt645Client，封装 RunAsync，返回自身对象支持链式调用
    /// </summary>
    public static async Task<IDlt645Client> ConnectAsync([NotNull] this IDlt645Client client, ChannelOptions options)
    {
        await ((Dlt645Client)client).RunAsync(options).ConfigureAwait(false);
        return client;
    }

}