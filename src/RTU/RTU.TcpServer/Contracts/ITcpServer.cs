using System.Net.Sockets;

namespace RTU.TcpServer.Contracts;

/// <summary>Factory to create queue publishers and subscribers. </summary>
public interface ITcpServer
{
    Task TryExecuteAsync();

    Task<bool> TryWriteAsync(TcpClient client, byte[] bytes);


    Action<Exception>? OnError { get; set; }
    Action<TcpListener>? OnSuccess { set; get; }
    Action<TcpListener, TcpClient, byte[]>? OnMessage { get; set; }
}