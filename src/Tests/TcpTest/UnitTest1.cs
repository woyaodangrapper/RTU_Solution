using Microsoft.Extensions.Logging.Abstractions;
using RTU.TCPClient;
using RTU.TCPClient.Extensions;
using RTU.TCPServer;
using System.Net.Sockets;

namespace TcpTest
{
    public class UnitTest1
    {
        [Fact]
        public async Task TestOpenAndCloseAsync()
        {
            var tcpServer = new TcpServer(new("123", "127.0.0.1", 6688), NullLoggerFactory.Instance);
            _ = Task.Factory.StartNew(async () => await tcpServer.TryExecuteAsync());

            TcpClientFactory factory = TcpClientFactory.Instance;
            TcpClient dataClient = await factory.GetTcpClientAsync("127.0.0.1", 6688, onSuccess: client =>
            {
                Console.WriteLine("数据信道连接成功");
            }, onError: ex =>
            {
                Console.WriteLine("数据信道连接失败：" + ex.Message);
            });

            var data = new ConsoleApp1.SayHello()
            {
                Name = "hello world"
            }.Serialize();
            dataClient.SendData(data);

        }
    }
}
