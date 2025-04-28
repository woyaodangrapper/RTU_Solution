using System.Net.Sockets;
using static RTU.TCPClient.TcpClientFactory;
namespace RTU.TCPClient.Extensions;


public static class TcpClientExtensions
{
    public static void SendData(this TcpClient client, byte[] data)
    {
        try
        {
            var stream = client.GetStream();
            stream.Write(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to send data.", ex);
        }
    }
    public static TcpClientStatus GetStatus(this TcpClient client)
    {
        if (!client.Connected)
            return new TcpClientStatus
            {
                Connected = client.Connected
            };

        return new TcpClientStatus
        {
            Connected = client.Connected,
            LocalEndPoint = client.Client?.LocalEndPoint,
            RemoteEndPoint = client.Client?.RemoteEndPoint
        };
    }
    public static byte[] ReceiveData(this TcpClient client, int bufferSize)
    {
        try
        {
            var stream = client.GetStream();
            byte[] buffer = new byte[bufferSize];
            int bytesRead = stream.Read(buffer, 0, bufferSize);
            byte[] receivedData = new byte[bytesRead];
            Buffer.BlockCopy(buffer, 0, receivedData, 0, bytesRead);
            return receivedData;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to receive data.", ex);
        }
    }
    public static void Reconnect(this TcpClient tcpClient, string serverIpAddress, int serverPort, int maxRetryAttempts = 3, int retryDelayMilliseconds = 1000)
    {
        int retryCount = 0;

        while (retryCount < maxRetryAttempts)
        {
            try
            {
                if (!tcpClient.Connected)
                {
                    tcpClient.Connect(serverIpAddress, serverPort);
                }
                else
                {
                    // 检查连接状态
                    bool isConnected = IsConnected(tcpClient.Client);
                    if (!isConnected)
                    {
                        // 关闭原连接
                        tcpClient.Close();
                        // 创建新的 TcpClient 对象
                        tcpClient = new TcpClient();
                        // 连接服务器
                        tcpClient.Connect(serverIpAddress, serverPort);
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                Thread.Sleep(retryDelayMilliseconds);
            }
        }

        throw new Exception($"Failed to connect to the server after {maxRetryAttempts} attempts.");
    }
    public static async Task<TcpClient> ReconnectAsync(this TcpClient tcpClient, string serverIpAddress, int serverPort, int maxRetryAttempts = 3, int retryDelayMilliseconds = 1000, Action<TcpClient>? onSuccess = null)
    {
        int retryCount = 0;

        while (retryCount < maxRetryAttempts)
        {
            try
            {
                if (!tcpClient.Connected)
                {
                    // 关闭原连接
                    tcpClient.Close();
                    // 创建新的 TcpClient 对象
                    tcpClient = new TcpClient();

                    await tcpClient.ConnectAsync(serverIpAddress, serverPort);

                    onSuccess?.Invoke(tcpClient);
                }
                else
                {
                    // 检查连接状态
                    bool isConnected = IsConnected(tcpClient.Client);
                    if (!isConnected)
                    {
                        // 关闭原连接
                        tcpClient.Close();
                        // 创建新的 TcpClient 对象
                        tcpClient = new TcpClient();
                        // 连接服务器
                        await tcpClient.ConnectAsync(serverIpAddress, serverPort);

                        onSuccess?.Invoke(tcpClient);
                    }

                }

                return tcpClient;
            }
            catch (Exception ex)
            {
                retryCount++;
                await Task.Delay(retryDelayMilliseconds);
            }
        }

        throw new Exception($"Failed to connect to the server after {maxRetryAttempts} attempts.");
    }
    private static bool IsConnected(Socket socket)
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

}


