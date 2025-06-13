using Asprtu.Rtu.TcpClient;
using Asprtu.Rtu.TcpServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tcp
{
    public class TcpCommunicationTests
    {
        private readonly ILoggerFactory _console;

        public TcpCommunicationTests()
        {
            _console = LoggerFactory.Create(builder =>
           {
               builder
                   .AddConsole()
                   .SetMinimumLevel(LogLevel.Trace);
           });
        }

        [Fact]
        public async Task TcpClientServer_Communication_WorksCorrectly()
        {
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();

            var loggerFactory = provider.GetService<ILoggerFactory>();

            var serverFactory = new TcpServerFactory(loggerFactory);
            var server = serverFactory.CreateTcpServer(new("default", "127.0.0.1", 6688));

            var receivedMessages = new List<byte[]>();
            server.OnMessage += (s, client, data) =>
            {
                lock (receivedMessages)
                {
                    receivedMessages.Add(data);
                }
                Console.WriteLine($"Received {data.Length} bytes");
            };

            _ = Task.Run(async () => await server.TryExecuteAsync());
            Thread.Sleep(500);

            var clientFactory = new TcpClientFactory(loggerFactory);
            var client = clientFactory.CreateTcpClient(new("default", "127.0.0.1", 6688));

            _ = Task.Run(async () => await client.TryExecuteAsync());
            Thread.Sleep(500);

            var tasks = new List<Task<bool>>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(client.TrySendAsync(i));
            }

            var results = await Task.WhenAll(tasks);

            // У��ÿ�η��Ͷ��ɹ�
            Assert.All(results, result => Assert.True(result));

            // �ȴ�������Ϣ����
            await Task.Delay(500);

            // У��������յ��� 100 ����Ϣ
            lock (receivedMessages)
            {
                Assert.Equal(100, receivedMessages.Count);
            }
        }
    }
}