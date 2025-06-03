using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Asprtu.Rtu.Extensions.Tcp;

public static class IPAddressExtensions
{
    public static IPAddress? GetLocalIPAddress()
    {
        string[] excludedKeywords =
        [
            "virtual", "vmware", "hyper-v", "loopback", "tunneling", "docker", "bluetooth"
        ];

        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n =>
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                !excludedKeywords.Any(keyword =>
                    n.Description?.ToUpperInvariant().Contains(keyword.ToUpperInvariant(), StringComparison.InvariantCultureIgnoreCase) == true ||
                    n.Name?.ToUpperInvariant().Contains(keyword, StringComparison.InvariantCultureIgnoreCase) == true)
            )
            .OrderByDescending(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
            .ThenByDescending(n => n.Speed);

        foreach (var networkInterface in networkInterfaces)
        {
            var ipProperties = networkInterface.GetIPProperties();
            var unicastAddresses = ipProperties.UnicastAddresses
                .Where(a =>
                    a.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(a.Address) &&
                    !a.Address.ToString().StartsWith("169.254", StringComparison.InvariantCulture)); // 修复 CA1310

            foreach (var address in unicastAddresses)
            {
                return address.Address;
            }
        }

        return null; // 如果无法找到合适的 IPv4 地址
    }

    public static int GenerateRandomPort()
    {
        const int minPort = 1000;
        const int maxPort = 65535;
        const int maxAttempts = 100;

        Random random = new();
        int port;
        int attempt = 0;

        // 随机生成端口号并检查是否已被占用，最多尝试 maxAttempts 次
        do
        {
#pragma warning disable CA5394 // Open to the outside world
            port = random.Next(minPort, maxPort + 1);
#pragma warning restore CA5394 // Open to the outside world
            attempt++;
        } while (!IsPortAvailable(port) && attempt < maxAttempts);

        if (attempt >= maxAttempts)
        {
            throw new InvalidOperationException($"没有足够的空余端口可以使用，尝试了{maxAttempts}次");

            // 或者返回一个特定的值，表示无法生成可用的端口号
            // return -1;
        }

        return port;
    }

    private static bool IsPortAvailable(int port)
    {
        IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        IPEndPoint[] activeListeners = ipGlobalProperties.GetActiveTcpListeners();
        // 检查端口是否被占用
        return !activeListeners.Any(listener => listener.Port == port);
    }
}