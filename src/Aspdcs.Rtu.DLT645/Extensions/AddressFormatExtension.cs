namespace Aspdcs.Rtu.DLT645.Extensions
{
    internal class AddressFormatExtension
    {
        /// <summary>
        /// 解析地址字符串为多个字节数组。
        /// </summary>
        /// <param name="addresses">地址字符串，支持多种格式：
        /// - 单地址："81-00-03-68-90-96" 或 "810003689096"
        /// - 多地址："81-00-03-68-90-96;82-00-03-68-90-96"
        /// </param>
        /// <returns>每个地址的字节数组列表</returns>
        internal static List<byte[]> FormatAddresses(string addresses)
        {
            if (string.IsNullOrWhiteSpace(addresses))
                throw new ArgumentException("地址字符串不能为空", nameof(addresses));

            var list = new List<byte[]>();
            var parts = addresses
                .Replace(" ", "", StringComparison.Ordinal)
                .Replace("\t", "", StringComparison.Ordinal)
                .Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var hex = part.Replace("-", "", StringComparison.Ordinal).Replace(":", "", StringComparison.Ordinal).Trim();
                if (hex.Length % 2 != 0)
                    throw new FormatException($"地址长度必须为偶数: {hex}");

                var bytes = new byte[hex.Length / 2];
                for (int i = 0; i < hex.Length; i += 2)
                {
                    if (!byte.TryParse(hex.AsSpan(i, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
                        throw new FormatException($"无效的十六进制值: {hex.Substring(i, 2)}");
                    bytes[i / 2] = b;
                }

                list.Add(bytes);
            }

            if (list.Count == 0)
                throw new FormatException("未解析到任何有效地址");

            return list;
        }
    }
}