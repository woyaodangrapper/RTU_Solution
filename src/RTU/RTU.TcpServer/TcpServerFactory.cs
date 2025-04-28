using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RTU.TCPServer.Contracts;

namespace RTU.TCPServer;

public class TcpServerFactory : ITcpServerFactory
{
    private readonly ILoggerFactory _loggerFactory;


    public TcpServerFactory()
    {
        _loggerFactory = NullLoggerFactory.Instance;
    }

    public TcpServerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public ITcpServer CreateTcpServer(ChannelOptions options)
     => new TcpServer(options, _loggerFactory);
}
