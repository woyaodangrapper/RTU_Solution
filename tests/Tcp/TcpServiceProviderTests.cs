using Asprtu.Rtu;
using Asprtu.Rtu.Contracts;
using Asprtu.Rtu.TcpClient;
using Asprtu.Rtu.TcpClient.Contracts;
using Asprtu.Rtu.TcpServer;
using Asprtu.Rtu.TcpServer.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tcp
{
    public class TcpServiceProviderTests
    {
        private static IServiceProvider _provider;

        public static IServiceProvider Instance
        {
            get
            {
                if (_provider == null)
                {
                    var services = new ServiceCollection();

                    services.AddSingleton(sp => new Asprtu.Rtu.TcpServer.Contracts.ChannelOptions("server", "0.0.0.0", 1878));
                    services.AddSingleton(sp => new Asprtu.Rtu.TcpClient.Contracts.ChannelOptions("client", "127.0.0.1", 1878));

                    services.AddSingleton<ILibraryCapacities<TcpServer>, LibraryCapacities<TcpServer>>();
                    services.AddSingleton<ILibraryCapacities<TcpClient>, LibraryCapacities<TcpClient>>();

                    services.AddSingleton(LoggerFactory.Create(builder =>
                    {
                        builder
                            .AddConsole()
                            .SetMinimumLevel(LogLevel.Trace);
                    }));

                    _provider = services.BuildServiceProvider();
                }

                return _provider;
            }
        }

        private readonly ITcpServer _server;
        private readonly ITcpClient _client;
        private readonly Asprtu.Rtu.TcpServer.Contracts.ChannelOptions _serverOptions;
        private readonly Asprtu.Rtu.TcpClient.Contracts.ChannelOptions _clientOptions;

        public TcpServiceProviderTests()
        {
            var scope = Instance.CreateAsyncScope();

            _server = scope.ServiceProvider.GetRequiredService<ILibraryCapacities<TcpServer>>().Contracts;
            _client = scope.ServiceProvider.GetRequiredService<ILibraryCapacities<TcpClient>>().Contracts;

            _serverOptions = scope.ServiceProvider.GetRequiredService<Asprtu.Rtu.TcpServer.Contracts.ChannelOptions>();
            _clientOptions = scope.ServiceProvider.GetRequiredService<Asprtu.Rtu.TcpClient.Contracts.ChannelOptions>();
        }

        [Fact]
        public void Server_Should_Use_Configured_ChannelOptions()
        {
            Assert.NotNull(_server);

            var info = _server.TcpInfo;
            Assert.Equal(_serverOptions.Port, info.LocalEndPoint.Port);
        }

        [Fact]
        public async Task Client_Should_Use_Configured_ChannelOptions()
        {
            Assert.NotNull(_client);

            // ���������
            _ = Task.Run(() => _server.TryExecuteAsync());
            await Task.Delay(300); // �ȴ�������׼������

            // �¼�ͬ������
            var successTcs = new TaskCompletionSource();
            var errorTcs = new TaskCompletionSource<Exception>();

            _client.OnSuccess += client =>
            {
                try
                {
                    var info = _client.TcpInfo;
                    Assert.Equal(_clientOptions.Port, info.RemoteEndPoint.Port);
                    successTcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    errorTcs.TrySetException(ex);
                }
            };

            _client.OnError += ex => errorTcs.TrySetException(new Xunit.Sdk.XunitException($"Client connection failed: {ex.Message}"));

            // �����ͻ�������
            _ = Task.Run(() => _client.TryExecuteAsync());

            // �ȴ����ӽ��
            var completed = await Task.WhenAny(successTcs.Task, errorTcs.Task, Task.Delay(2000));
            if (completed == successTcs.Task)
            {
                await successTcs.Task;
            }
            else if (completed == errorTcs.Task)
            {
                await errorTcs.Task; // �׳��쳣
            }
            else
            {
                throw new TimeoutException("Client did not connect within expected time.");
            }
        }
    }
}