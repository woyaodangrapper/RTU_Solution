using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RTU.TcpServer.Contracts;
using System.Collections.Concurrent;

namespace RTU.TcpServer;

public class TcpServerFactory : ITcpServerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ChannelOptions _channelOptions;


    private static readonly ConcurrentDictionary<string, ChannelOptions> _instance = new();
    private static readonly Lazy<TcpServerFactory> instance = new(() => new());


    public TcpServerFactory(string name = "default")
       : this(NullLoggerFactory.Instance, GetOrCreate(name))
    {
    }

    public TcpServerFactory(ILoggerFactory loggerFactory, ChannelOptions options)
    {
        _loggerFactory = loggerFactory;
        _channelOptions = options;
    }

    private static ChannelOptions GetOrCreate(string name)
    {
        return _instance.GetOrAdd(name, _ => new ChannelOptions(name));
    }

    public ITcpServer CreateTcpServer()
     => new TcpServer(_channelOptions, _loggerFactory);

    public ITcpServer CreateTcpServer(ChannelOptions options)
     => new TcpServer(_channelOptions, _loggerFactory);
}
