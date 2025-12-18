using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.BACnet;

public class BACnetCaptureHostedService : BackgroundService
{
    private readonly ILogger<BACnetCaptureHostedService> _logger;
    private readonly BACnetClient _bacnet;

    public BACnetCaptureHostedService(ILogger<BACnetCaptureHostedService> logger, BACnetClient bacnet)
    {
        _logger = logger;
        _bacnet = bacnet;
        logger.LogInformation("正在广播设备请求...");
    }

    public override void Dispose()
    {
        _bacnet.Dispose();
        GC.SuppressFinalize(this);
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BACnet 监听端口：" + 0xBAC0);
        _bacnet.Start();
        _bacnet.WhoIs();

        _bacnet.OnIam += (sender, adr, deviceId, maxApdu, segmentation, vendorId) =>
        {
            _logger.LogInformation($"发现设备 ID: {deviceId}, 地址: {adr}");
            ReadProperty(_bacnet, adr, deviceId);
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken); // 避免 CPU 100% 占用
        }

        _bacnet.Dispose();
    }

    private void ReadProperty(BACnetClient bacnet, BACnetAddress adr, uint deviceId)
    {
        try
        {
            BACnetObjectId objectId = new BACnetObjectId(BACnetObjectTypes.OBJECT_ANALOG_INPUT, 1);

            if (bacnet.ReadPropertyRequest(adr, objectId, BACnetPropertyIds.PROP_PRESENT_VALUE, out var values))
            {
                _logger.LogInformation($"设备 {deviceId} AI-1 值: {values[0].Value}");
            }
            else
            {
                _logger.LogWarning($"读取失败: 设备 {deviceId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"读取失败: {ex.Message}");
        }
    }
}
