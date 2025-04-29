//// See https://aka.ms/new-console-template for more information
//using Microsoft.Extensions.Logging.Abstractions;
//using RTU.TCPClient;
//using RTU.TCPClient.Extensions;
//using RTU.TCPServer;
//using System.Net.Sockets;
//using Tcp;

//Console.WriteLine("hello world");
//// 线程安全退出
//CancellationTokenSource cts = new();

//var tcpServer = new TcpServer(new("123", "127.0.0.1", 6688), NullLoggerFactory.Instance);

//_ = Task.Factory.StartNew(async () => await tcpServer.TryExecuteAsync());



//TcpClientFactory factory = TcpClientFactory.Instance;


//TcpClient dataClient = await factory.GetTcpClientAsync("127.0.0.1", 6688, onSuccess: client =>
//{
//    Console.WriteLine("数据信道连接成功");
//}, onError: ex =>
//{
//    Console.WriteLine("数据信道连接失败：" + ex.Message);
//});
//TcpClient dataClient2 = await factory.GetTcpClientAsync("127.0.0.1", 6688, onSuccess: client =>
//{
//    Console.WriteLine("数据信道连接成功");
//}, onError: ex =>
//{
//    Console.WriteLine("数据信道连接失败：" + ex.Message);
//});
////发送
//_ = Task.Factory.StartNew(() =>
//{
//    try
//    {
//        //在此模拟发送需要的数据
//        while (!cts.IsCancellationRequested)
//        {
//            var data = new SayHello()
//            {
//                Name = "hello world"
//            }.Serialize();
//            dataClient.SendData(data);
//            dataClient2.SendData(data);

//            Console.WriteLine($"{DateTime.Now}-模拟数据已经发送");
//            Task.Delay(2000).Wait();
//        }

//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"{DateTime.Now}-发送出现错误：{ex.Message}");
//    }



//}, cts.Token);



//// 信道状态
//_ = Task.Factory.StartNew(() =>
//{
//    //在此模拟发送需要的数据
//    while (!cts.IsCancellationRequested)
//    {
//        Task.Delay(30000).Wait();

//        Console.WriteLine("———————————————————————数据/状态信道长期运行中...，当前信道状态————————————————————————");
//        dataClient.GetStatus().WriteAsTable();
//        Console.WriteLine("———————————————————————数据/状态信道长期运行中...，当前信道状态————————————————————————");
//    }

//}, cts.Token);



//Console.WriteLine("———————————————————————————————————————————————\r\n回车退出程序\r\n———————————————————————————————————————————————");

//Console.ReadLine();
//cts.Cancel();

using RTU.Infrastructures.Queue;

var factory = new QueueFactory<string>();

var publisher = factory.CreatePublisher();
var subscriber = factory.CreateSubscriber();


_ = Task.Run(() =>
{
    while (true)
    {
        subscriber.TryDequeue(out var a, default);
        Console.WriteLine(a);
    }
});
// 先把数据放入队列
publisher.TryEnqueue("hello world");


Console.ReadLine();