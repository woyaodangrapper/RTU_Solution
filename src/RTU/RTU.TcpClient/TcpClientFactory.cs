using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace RTU.TCPClient;

public class TcpClientFactory : IDisposable
{
    private readonly ConcurrentDictionary<string, TcpClient> clients = new ConcurrentDictionary<string, TcpClient>();

    private static readonly Lazy<TcpClientFactory> instance = new Lazy<TcpClientFactory>(() => new TcpClientFactory());

    public static TcpClientFactory Instance => instance.Value;

    private TcpClientFactory()
    { }

    public TcpClient GetTcpClient(string serverIpAddress, int serverPort)
    {
        try
        {
            var client = new TcpClient();
            string key = $"{serverIpAddress}:{serverPort}";

            if (!client.Connected)
            {
                client.Connect(serverIpAddress, serverPort);
            }

            if (!clients.ContainsKey(key))
            {
                clients.TryAdd(key, client);
            }

            return client;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to connect to the server.", ex);
        }
    }

    public async Task<TcpClient> GetTcpClientAsync(string serverIpAddress, int serverPort, Action<TcpClient> onSuccess, Action<Exception> onError)
    {
        var client = new TcpClient();
        try
        {
            string key = $"{serverIpAddress}:{serverPort}";

            if (!client.Connected)
            {
                await client.ConnectAsync(serverIpAddress, serverPort);
            }

            if (!clients.ContainsKey(key))
            {
                clients.TryAdd(key, client);
            }
            onSuccess?.Invoke(client);

        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
        return client;

    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    // 获取所有 TcpClient 的状态信息
    public IEnumerable<TcpClientStatus> ClientsStatus()
    {
        var result = new List<TcpClientStatus>();

        foreach (var item in clients)
        {
            try
            {
                var tcpClientStatus = new TcpClientStatus
                {
                    Connected = item.Value.Connected,
                    LocalEndPoint = item.Value.Client?.LocalEndPoint,
                    RemoteEndPoint = item.Value.Client?.RemoteEndPoint
                };

                result.Add(tcpClientStatus);
            }
            catch (Exception ex)
            {
                // 处理异常情况
            }
        }

        return result;
    }
    protected virtual bool IsConnected(Socket socket)
    {
        try
        {
            // 检查连接状态
            return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
        }
        catch
        {
            return false;
        }
    }
    protected virtual void Dispose(bool disposing)
    {
        try
        {

            foreach (var client in clients.Values)
            {
                if (disposing && client.Connected)
                {
                    client.Close();
                }
            }

        }
        catch (Exception ex)
        {
            throw new Exception("Failed to release the TCP client.", ex);
        }
    }
    public class TcpClientStatus
    {
        public bool Connected { get; set; }
        public EndPoint? LocalEndPoint { get; set; } = default;
        public EndPoint? RemoteEndPoint { get; set; } = default;
    }


}
