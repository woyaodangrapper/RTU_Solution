using Asprtu.Rtu.TcpClient;
using Asprtu.Rtu.TcpClient.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

var console = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Trace);
});

var builder = WebApplication.CreateBuilder();

Console.ReadLine();

TcpClientFactory factory = new(console, new("default", "127.0.0.1", 6688));

ITcpClient dataClient = factory.CreateTcpClient();