namespace RTU.Infrastructures.Queue;


public class Subscriber
{


    // 消费消息的方法
    public void StartConsuming(CancellationToken cancellationToken)
    {
        Task.Run(() => ConsumeMessages(cancellationToken));
    }

    private void ConsumeMessages(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = _queue.Dequeue();
            ProcessMessage(message);
        }
    }

    // 处理从队列中取出的消息
    private void ProcessMessage(byte[] message)
    {
        Console.WriteLine($"Processing message: {BitConverter.ToString(message)}");
        // 在此添加消息处理逻辑
    }
}
