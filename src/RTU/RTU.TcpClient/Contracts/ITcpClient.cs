namespace RTU.TcpClient.Contracts;

/// <summary>Factory to create queue publishers and subscribers. </summary>
public interface ITcpClient
{
    Task TryExecuteAsync();

    Task<bool> TryWriteAsync(byte[] bytes);

    Action<Exception>? OnError { get; set; }
    Action<System.Net.Sockets.TcpClient>? OnSuccess { set; get; }
    Action<System.Net.Sockets.TcpClient, byte[]>? OnMessage { get; set; }
}