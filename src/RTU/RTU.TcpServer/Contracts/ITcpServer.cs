using RTU.Infrastructures.Contracts.Tcp;
using System.Net.Sockets;

namespace RTU.TcpServer.Contracts;

/// <summary>Factory to create queue publishers and subscribers. </summary>
public interface ITcpServer
{
    /// <summary>
    /// ���ԴӶ����з������ػ�ȡһ����Ϣ��ʹ��Ĭ�ϵ�ȡ�����ƣ���֧��ȡ������
    /// </summary>
    /// <param name="message">
    /// ��������ʱ���������ɹ������ȡ������Ϣ������Ϊ���� <typeparamref name="T"/> ��Ĭ��ֵ��
    /// </param>
    Task TryExecuteAsync();

    /// <summary>
    /// �����첽д���ֽ����鵽 TCP ���ӡ�
    /// </summary>
    /// <param name="bytes">Ҫ���͵��ֽ����顣</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ�д�����ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TryWriteAsync(byte[] bytes, TcpClient? client = null);

    /// <summary>
    /// �����첽�����������ݵ� TCP ���ӡ�
    /// </summary>
    /// <param name="data">Ҫ���͵��������ݡ�</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(int data, TcpClient? client = null);

    /// <summary>
    /// �����첽���͸������ݵ� TCP ���ӡ�
    /// </summary>
    /// <param name="data">Ҫ���͵ĸ������ݡ�</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(float data, TcpClient? client = null);

    /// <summary>
    /// �����첽����˫���ȸ������ݵ� TCP ���ӡ�
    /// </summary>
    /// <param name="data">Ҫ���͵�˫���ȸ������ݡ�</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(double data, TcpClient? client = null);

    /// <summary>
    /// �����첽���Ͳ������ݵ� TCP ���ӡ�
    /// </summary>
    /// <param name="data">Ҫ���͵Ĳ������ݡ�</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(bool data, TcpClient? client = null);

    /// <summary>
    /// �����첽���Ͷ��������ݵ� TCP ���ӡ�
    /// </summary>
    /// <param name="data">Ҫ���͵Ķ��������ݡ�</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(short data, TcpClient? client = null);

    /// <summary>
    /// �����첽���ͳ��������ݵ� TCP ���ӡ�
    /// </summary>
    /// <param name="data">Ҫ���͵ĳ��������ݡ�</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(long data, TcpClient? client = null);

    /// <summary>
    /// �����첽�����ֽ����ݵ� TCP ���ӡ�
    /// </summary>
    /// <param name="data">Ҫ���͵��ֽ����ݡ�</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(byte data, TcpClient? client = null);

    /// <summary>
    /// �����첽�����ַ����ݵ� TCP ���ӡ�
    /// </summary>
    /// <param name="data">Ҫ���͵��ַ����ݡ�</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(char data, TcpClient? client = null);

    /// <summary>
    /// �����첽����ʮ�������ݵ� TCP ���ӡ�
    /// </summary>
    /// <param name="data">Ҫ���͵�ʮ�������ݡ�</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(decimal data, TcpClient? client = null);

    /// <summary>
    /// �����첽�����ַ������ݵ� TCP ���ӡ�
    /// </summary>
    /// <param name="data">Ҫ���͵��ַ������ݡ�</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(string data, TcpClient? client = null);

    /// <summary>
    /// �����첽��������ʱ�����ݵ� TCP ���ӡ�
    /// </summary>
    /// <param name="data">Ҫ���͵�����ʱ�����ݡ�</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync(DateTime data, TcpClient? client = null);

    /// <summary>
    /// �����첽�����Զ�����Ϣ�������ݵ� TCP ���ӡ�
    /// </summary>
    /// <typeparam name="T">��Ϣ���ͣ�����̳��� <see cref="AbstractMessage"/>��</typeparam>
    /// <param name="data">Ҫ���͵���Ϣ���ݡ�</param>
    /// <param name="client">��ѡ�� TCP �ͻ���ʵ����Ĭ��Ϊ null��</param>
    /// <returns>����ɹ��������ݣ����� <see langword="true"/>�����򷵻� <see langword="false"/>��</returns>
    Task<bool> TrySendAsync<T>(T data, TcpClient? client = null)
         where T : AbstractMessage, new();

    /// <summary>
    /// ����ص��¼��������쳣��
    /// </summary>
    Action<Exception>? OnError { get; set; }

    /// <summary>
    /// �ɹ��ص��¼�������ɹ������� TCP ���ӡ�
    /// </summary>
    Action<TcpListener>? OnSuccess { set; get; }

    /// <summary>
    /// ��Ϣ�ص��¼���������յ��� TCP ��Ϣ��
    /// </summary>
    Action<TcpListener, TcpClient, byte[]>? OnMessage { get; set; }

}