using Asprtu.Rtu.Contracts;

namespace Asprtu.Rtu.DLT645.Contracts;

/// <summary>
/// 工厂接口，用于创建 TCP 服务器。
/// </summary>
public interface IDlt645ClientFactory : ILibraryFactory<Dlt645Client>
{

}