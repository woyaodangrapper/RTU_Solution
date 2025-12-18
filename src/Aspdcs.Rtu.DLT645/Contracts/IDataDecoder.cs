using Aspdcs.Rtu.Contracts.DLT645;

namespace Aspdcs.Rtu.DLT645.Contracts;

public interface IDataDecoder
{
    /// <summary>
    /// 将原始字节数据解码为数值或结构化数据
    /// </summary>
    /// <param name="data">原始数据域</param>
    /// <param name="format">数据格式信息</param>
    /// <returns>解码后的对象（可以是 double、int 或自定义类型）</returns>
    SemanticValue Decode(ReadOnlySpan<byte> message, DataFormat format);

    /// <summary>
    /// 将原始字节数据解码为数值或结构化数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    T Decode<T>(ReadOnlySpan<byte> message, DataFormat format) where T : SemanticValue;

    /// <summary>
    ///  将原始字节数据解码为数值或结构化数据
    /// </summary>
    /// <param name="source"></param>
    /// <param name="format"></param>
    /// <param name="onError"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IAsyncEnumerable<SemanticValue> TryDecodeAsync(IAsyncEnumerable<MessageHeader> source,
    DataFormat format,
    Action<Exception>? onError = null,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// 将原始字节数据解码为数值或结构化数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="format"></param>
    /// <param name="onError"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IAsyncEnumerable<T> TryDecodeAsync<T>(IAsyncEnumerable<MessageHeader> source,
    DataFormat format,
    Action<Exception>? onError = null,
     CancellationToken cancellationToken = default) where T : SemanticValue;
}