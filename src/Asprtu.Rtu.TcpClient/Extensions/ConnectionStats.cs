using Asprtu.Rtu.Contracts.Tcp;
using System.Net;

namespace Asprtu.Rtu.TcpClient.Extensions;

public class ConnectionStateTracker
{
    private readonly Guid _connectionId = Guid.NewGuid();
    private readonly DateTime _connectedAt = DateTime.UtcNow;

    private long _bytesSent;
    private long _bytesReceived;
    private DateTime _lastActivityAt = DateTime.UtcNow;

    private ConnectionState _state = ConnectionState.Connecting;
    private string _version = "1.0.0";
    private bool _isEncrypted;

    private readonly Dictionary<string, ConnectionMeta> _metadata = [];

    public IReadOnlyDictionary<string, ConnectionMeta> Metadata => _metadata;

    public void AddMetadata(string key, ConnectionMeta value) => _metadata[key] = value;

    public bool RemoveMetadata(string key) => _metadata.Remove(key);

    /// <summary>
    /// Adds the specified number of bytes to the total sent count and updates the last activity timestamp.
    /// </summary>
    /// <remarks>This method is thread-safe and ensures that the total sent count is updated atomically. The
    /// last activity timestamp is set to the current UTC time when this method is called.</remarks>
    /// <param name="bytes">The number of bytes to add to the total sent count. Must be a non-negative value.</param>
    public void AddSent(long bytes)
    {
        Interlocked.Add(ref _bytesSent, bytes);
        _lastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds the specified number of bytes to the total received count and updates the last activity timestamp.
    /// </summary>
    /// <remarks>This method is thread-safe and ensures that the total received count is updated atomically.
    /// The last activity timestamp is set to the current UTC time when this method is called.</remarks>
    /// <param name="bytes">The number of bytes received. Must be a non-negative value.</param>
    public void AddReceived(long bytes)
    {
        Interlocked.Add(ref _bytesReceived, bytes);
        _lastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the current state of the connection.
    /// </summary>
    /// <param name="state">The new state to set for the connection. Must be a valid <see cref="ConnectionState"/> value.</param>
    public void SetState(ConnectionState state) => _state = state;

    /// <summary>
    /// Sets the version of the current instance.
    /// </summary>
    /// <param name="version">The version string to set. Cannot be null or empty.</param>
    public void SetVersion(string version) => _version = version;

    /// <summary>
    /// Enables or disables encryption for the current instance.
    /// </summary>
    /// <param name="encrypted"><see langword="true"/> to enable encryption; <see langword="false"/> to disable it.</param>
    public void SetEncryption(bool encrypted) => _isEncrypted = encrypted;

    public Func<IPEndPoint?, IPEndPoint?, TcpInfo> GetSnapshot => (remote, local) => new TcpInfo(
        metadata: new Dictionary<string, ConnectionMeta>(_metadata)
    )
    {
        ConnectionId = _connectionId,
        ConnectedAt = _connectedAt,
        LastActivityAt = _lastActivityAt,
        BytesSent = Interlocked.Read(ref _bytesSent),
        BytesReceived = Interlocked.Read(ref _bytesReceived),
        Version = _version,
        State = _state,
        IsEncrypted = _isEncrypted,
        RemoteEndPoint = remote,
        LocalEndPoint = local,
        IsConnected = _state == ConnectionState.Active || _state == ConnectionState.Handshaking
    };
}