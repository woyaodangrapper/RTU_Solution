// See https://aka.ms/new-console-template for more information
using System.Net.BACnet;

Console.WriteLine("Hello, World!");


// 设置 BACnet/IP 端口
BACnetClient bacnet = new(new BACnetIpUdpProtocolTransport(0xBAC0));

// 设备发现回调
bacnet.OnIam += (sender, adr, deviceId, maxApdu, segmentation, vendorId) =>
{
    Console.WriteLine($"发现设备 ID: {deviceId}, 地址: {adr}");
    ReadProperty(bacnet, adr, deviceId);
};

void ReadProperty(BACnetClient bacnet, BACnetAddress adr, uint deviceId)
{
    try
    {
        // 读取 Analog Input 1 的 Present Value
        BACnetObjectId objectId = new(BACnetObjectTypes.OBJECT_ANALOG_INPUT, 1);

        // **正确方式：直接传 BACnetPropertyIds**
        if (bacnet.ReadPropertyRequest(adr, objectId, BACnetPropertyIds.PROP_READ_ONLY, out var values))
        {
            Console.WriteLine($"设备 {deviceId} AI-1 值: {values[0].Value}");
        }
        else
        {
            Console.WriteLine($"读取失败: 设备 {deviceId}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"错误: {ex.Message}");
    }
}

// 启动 BACnet 客户端
bacnet.Start();
Console.WriteLine("正在广播设备请求...");
bacnet.WhoIs();

// 等待设备响应
Console.ReadLine();