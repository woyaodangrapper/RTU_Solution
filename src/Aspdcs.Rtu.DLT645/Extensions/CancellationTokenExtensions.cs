using Aspdcs.Rtu.Contracts.DLT645;
using System.Diagnostics;

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
        Stopwatch? sw = null;


        // 对于无限流，等待最后一次产出是否超时
        if (expectedCount == -1)
        {

            sw = Stopwatch.StartNew();

            _ = Task.Run(async () =>
            {
#pragma warning disable CA1031 // 不捕获常规异常类型
                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        if (sw.Elapsed >= timeout)
                        {
#if NET6_0_OR_GREATER
                            await cts.CancelAsync().ConfigureAwait(false);
#else
                            cts.Cancel();
#endif
                            break;
                        }

                        try
                        {
                            await Task.Delay(50, cts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 捕获后台任务异常，避免未观察异常
                    Console.WriteLine($"{ex.Message}");
                }
#pragma warning restore CA1031 // 不捕获常规异常类型
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
                // 无限流逻辑，重置超时计时
                sw?.Restart();
            }
        }

    }



    /// <summary>
    /// 确保有一个有效的取消令牌，如果传入的令牌无法取消，则创建一个基于 ChannelOptions.Timeout 的兜底超时令牌
    /// </summary>
    internal static CancellationTokenSource CreateTimeoutTokenIfNeeded(TimeSpan timeout, int count, CancellationToken cancellationToken, out CancellationToken effectiveToken)
    {
        // 如果用户已经提供了可取消的令牌，直接使用
        if (cancellationToken.CanBeCanceled)
        {
            effectiveToken = cancellationToken;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return linkedCts;
        }

        // 否则创建一个兜底超时令牌：Timeout * (RetryCount + 1) * 2，给予足够的时间
        var timeoutDuration = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds * (count + 1) * 2);
        var cts = new CancellationTokenSource(timeoutDuration);
        effectiveToken = cts.Token;
        return cts;
    }
}
