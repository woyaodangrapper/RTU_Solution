using Asprtu.Rtu.DLT645.Serialization;

namespace Asprtu.Rtu.DLT645.Contracts;

public interface IDataDecoder
{
    /// <summary>
    /// 将原始字节数据解码为数值或结构化数据
    /// </summary>
    /// <param name="data">原始数据域</param>
    /// <param name="format">数据格式信息</param>
    /// <returns>解码后的对象（可以是 double、int 或自定义类型）</returns>
    object Decode(ReadOnlySpan<byte> data, DataFormat format);
}
