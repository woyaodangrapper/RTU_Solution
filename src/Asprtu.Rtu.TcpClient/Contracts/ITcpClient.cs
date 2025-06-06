using Asprtu.Rtu.Contracts;
using Asprtu.Rtu.Contracts.Tcp;

namespace Asprtu.Rtu.TcpClient.Contracts;

/// <summary>
/// Defines the contract for a TCP client that implements protocol communication capabilities.
/// </summary>
public interface ITcpClient : IContracts
{
    /// <summary>
    /// �����첽ִ��TCP�ͻ��˲�����ͨ���������Ӳ�����
    /// </summary>
    /// <returns>���������</returns>
    Task TryExecuteAsync();

    /// <summary>
    /// �����첽д���ֽ����鵽TCP�ͻ��ˡ�
    /// </summary>
    /// <param name="bytes">Ҫ���͵��ֽ����顣</param>
    /// <returns>����ɹ�д�룬���� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TryWriteAsync(byte[] bytes);

    /// <summary>
    /// �����첽�����������ݵ�TCP�ͻ��ˡ�
    /// </summary>
    /// <param name="data">Ҫ���͵��������ݡ�</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(int data);

    /// <summary>
    /// �����첽���͸������ݵ�TCP�ͻ��ˡ�
    /// </summary>
    /// <param name="data">Ҫ���͵ĸ������ݡ�</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(float data);

    /// <summary>
    /// �����첽����˫���ȸ������ݵ�TCP�ͻ��ˡ�
    /// </summary>
    /// <param name="data">Ҫ���͵�˫���ȸ������ݡ�</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(double data);

    /// <summary>
    /// �����첽���Ͳ������ݵ�TCP�ͻ��ˡ�
    /// </summary>
    /// <param name="data">Ҫ���͵Ĳ������ݡ�</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(bool data);

    /// <summary>
    /// �����첽���Ͷ��������ݵ�TCP�ͻ��ˡ�
    /// </summary>
    /// <param name="data">Ҫ���͵Ķ��������ݡ�</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(short data);

    /// <summary>
    /// �����첽���ͳ��������ݵ�TCP�ͻ��ˡ�
    /// </summary>
    /// <param name="data">Ҫ���͵ĳ��������ݡ�</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(long data);

    /// <summary>
    /// �����첽�����ֽ����ݵ�TCP�ͻ��ˡ�
    /// </summary>
    /// <param name="data">Ҫ���͵��ֽ����ݡ�</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(byte data);

    /// <summary>
    /// �����첽�����ַ����ݵ�TCP�ͻ��ˡ�
    /// </summary>
    /// <param name="data">Ҫ���͵��ַ����ݡ�</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(char data);

    /// <summary>
    /// �����첽����ʮ�������ݵ�TCP�ͻ��ˡ�
    /// </summary>
    /// <param name="data">Ҫ���͵�ʮ�������ݡ�</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(decimal data);

    /// <summary>
    /// �����첽�����ַ������ݵ�TCP�ͻ��ˡ�
    /// </summary>
    /// <param name="data">Ҫ���͵��ַ������ݡ�</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(string data);

    /// <summary>
    /// �����첽��������ʱ�����ݵ�TCP�ͻ��ˡ�
    /// </summary>
    /// <param name="data">Ҫ���͵�����ʱ�����ݡ�</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(DateTime data);

    /// <summary>
    /// �����첽�����Զ�����Ϣ�������ݵ�TCP�ͻ��ˡ�
    /// </summary>
    /// <typeparam name="T">��Ϣ���ͣ�����̳��� <see cref="AbstractMessage"/>��</typeparam>
    /// <param name="data">Ҫ���͵���Ϣ���ݡ�</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync<T>(T data)
         where T : AbstractMessage, new();

    /// <summary>
    /// ��ȡ�뵱ǰ�����Ĺ�����TCP������Ϣ
    /// </summary>
    public TcpInfo TcpInfo { get; }

    /// <summary>
    /// ����ص��������쳣��
    /// </summary>
    Action<Exception>? OnError { get; set; }

    /// <summary>
    /// ���ӳɹ��ص�������ɹ�������TCP���ӡ�
    /// </summary>
    Action<System.Net.Sockets.TcpClient>? OnSuccess { set; get; }

    /// <summary>
    /// ��Ϣ�ص���������յ�����Ϣ��
    /// </summary>
    Action<System.Net.Sockets.TcpClient, byte[]>? OnMessage { get; set; }
}