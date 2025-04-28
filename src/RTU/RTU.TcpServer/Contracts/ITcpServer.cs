namespace RTU.TCPServer.Contracts;

/// <summary>Factory to create queue publishers and subscribers. </summary>
public interface ITcpServer
{
    Task TryExecuteAsync();

}