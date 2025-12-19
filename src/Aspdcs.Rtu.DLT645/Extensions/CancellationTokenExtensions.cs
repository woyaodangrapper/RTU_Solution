using Aspdcs.Rtu.Contracts.DLT645;

namespace Aspdcs.Rtu.DLT645.Extensions;

internal static class CancellationTokenExtensions
{
    /// <summary>
    /// 期望从源 IAsyncEnumerable 接收指定数量的帧，或者无限流但带超时控制
    /// </summary>
    /// <param name="cts">CancellationTokenSource 用于主动取消</param>
    /// <param name="expectedCount">
    /// >= 1: 接收指定数量后自动取消
    /// -1: 无限流，每次产出检查超时
    /// </param>
    /// <param name="source">异步数据源</param>
    /// <param name="timeout">超时时间，仅在 expectedCount = -1 时生效</param>
    /// <returns>IAsyncEnumerable&lt;MessageHeader&gt;</returns>
    public static async IAsyncEnumerable<MessageHeader> Expect(
        this CancellationTokenSource cts,
        int expectedCount,
        IAsyncEnumerable<MessageHeader> source,
        TimeSpan timeout)
    {
        if (expectedCount == 0)
            yield break;

        int count = 0;
        DateTime lastYieldTime = DateTime.UtcNow;



        // 对于无限流，等待最后一次产出是否超时
        if (expectedCount == -1)
        {
            // 启动一个后台 Task 监控超时
            _ = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (DateTime.UtcNow - lastYieldTime >= timeout)
#if NET6_0_OR_GREATER
                        await cts.CancelAsync()
                            .ConfigureAwait(false);
                    // 这部分用令牌来完成期望数量不完美，会报错 --- 待优化
#else
                    cts.Cancel();
#endif

                    try
                    {
                        await Task.Delay(50, cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
#if NET6_0_OR_GREATER
                        await cts.CancelAsync()
                            .ConfigureAwait(false);
#else
                    cts.Cancel();
#endif
                    }
                }
            });

        }


        await foreach (var item in source.WithCancellation(cts.Token))
        {
            yield return item;

            if (expectedCount > 0)
            {
                count++;
                if (count >= expectedCount)
                {
                    // 达到期望数量，主动取消底层流

#if NET6_0_OR_GREATER
                    await cts.CancelAsync()
                        .ConfigureAwait(false);
#else
                    cts.Cancel();
#endif

                    yield break;
                }
            }
            else if (expectedCount == -1)
            {
                // 无限流逻辑，记录产出时间
                lastYieldTime = DateTime.UtcNow;
            }
        }

    }
}
