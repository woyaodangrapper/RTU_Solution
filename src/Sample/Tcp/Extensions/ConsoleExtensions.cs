namespace Tcp.Extensions;

public static class ConsoleExtensions
{
    public static void WriteAsTable<T>(this IEnumerable<T> data)
    {
        var props = typeof(T).GetProperties();

        // 输出表头
        foreach (var prop in props)
        {
            Console.Write($"{prop.Name}\t");
        }
        Console.WriteLine();

        // 输出每一行数据
        foreach (var item in data)
        {
            foreach (var prop in props)
            {
                var value = prop.GetValue(item)?.ToString() ?? string.Empty;
                Console.Write($"{value}\t");
            }
            Console.WriteLine();
        }
    }
    public static void WriteAsTable<T>(this T data)
    {
        var props = typeof(T).GetProperties();

        // 获取表格各列的最大宽度
        var columnWidths = new int[props.Length];
        for (int i = 0; i < props.Length; i++)
        {
            columnWidths[i] = props[i].Name.Length;
            var value = props[i].GetValue(data)?.ToString() ?? string.Empty;
            if (value.Length > columnWidths[i])
            {
                columnWidths[i] = value.Length;
            }
        }

        // 输出表头
        Console.Write("| ");
        for (int i = 0; i < props.Length; i++)
        {
            Console.Write(props[i].Name.PadRight(columnWidths[i]));
            Console.Write(" | ");
        }
        Console.WriteLine();

        // 输出分隔线
        Console.Write("|-");
        for (int i = 0; i < props.Length; i++)
        {
            Console.Write(new string('-', columnWidths[i]));
            Console.Write("-|-");
        }
        Console.WriteLine();

        // 输出数据行
        Console.Write("| ");
        for (int i = 0; i < props.Length; i++)
        {
            var value = props[i].GetValue(data)?.ToString() ?? string.Empty;
            Console.Write(value.PadRight(columnWidths[i]));
            Console.Write(" | ");
        }
        Console.WriteLine();
    }
}
