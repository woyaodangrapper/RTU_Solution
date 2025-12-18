using System.Net;

namespace Aspdcs.Rtu.Contracts.Tcp;

public class TcpInfo
{
    public TcpInfo(IReadOnlyDictionary<string, ConnectionMeta>? metadata = null)
        => Metadata = metadata ?? new Dictionary<string, ConnectionMeta>();

    /// <summary>连接唯一标识符 Unique connection ID</summary>
    public Guid ConnectionId { get; set; }

    /// <summary>对端地址 Remote endpoint (IP and port)</summary>
    public IPEndPoint? RemoteEndPoint { get; set; }

    /// <summary>本地地址 Local endpoint (IP and port)</summary>
    public IPEndPoint? LocalEndPoint { get; set; }

    /// <summary>是否已连接 Is the connection currently active</summary>
    public bool IsConnected { get; set; }

    /// <summary>已发送的字节数 Total bytes sent</summary>
    public long BytesSent { get; set; }

    /// <summary>已接收的字节数 Total bytes received</summary>
    public long BytesReceived { get; set; }

    /// <summary>连接建立时间 Connection start timestamp</summary>
    public DateTime ConnectedAt { get; set; }

    /// <summary>最后活跃时间 Last read/write timestamp</summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>协议版本 Protocol or application version</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>连接状态 Current connection state</summary>
    public ConnectionState State { get; set; }

    // 可选字段 Optional fields

    /// <summary>客户端应用名 Client application name</summary>
    public string? ClientAppName { get; set; }

    /// <summary>客户端版本 Client application version</summary>
    public string? ClientVersion { get; set; }

    /// <summary>自定义标签 Custom tags or labels</summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>最近错误 Last error message if any</summary>
    public string? LastError { get; set; }

    /// <summary>是否加密传输 Whether the connection is encrypted</summary>
    public bool? IsEncrypted { get; set; }

    /// <summary>空闲时间 Idle duration since last activity</summary>
    public TimeSpan IdleTime => DateTime.UtcNow - LastActivityAt;

    /// <summary>连接元数据 Custom structured metadata</summary>
    public IReadOnlyDictionary<string, ConnectionMeta> Metadata { get; }
}

public class ConnectionMeta
{
    /// <summary>用户 ID Associated user ID</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>区域标识 Region or zone info</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>会话令牌 Session or auth token</summary>
    public string SessionToken { get; set; } = string.Empty;
}

public enum ConnectionState
{
    /// <summary>正在连接中 Connecting</summary>
    Connecting,

    /// <summary>握手中 Handshaking</summary>
    Handshaking,

    /// <summary>已初始化 Listening</summary>
    Listening,

    /// <summary>活跃连接 Active and usable</summary>
    Active,

    /// <summary>正在关闭中 Closing</summary>
    Closing,

    /// <summary>已关闭 Closed and disconnected</summary>
    Closed
}