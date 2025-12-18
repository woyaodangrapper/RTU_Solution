#if !NET6_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// 用于兼容 C# 9 init-only 属性的占位类型
    /// </summary>
    public sealed class IsExternalInit { }
}
#endif
