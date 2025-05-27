using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace System.Net.BACnet;

public abstract class BACnetTransportBase : IBACnetTransport
{

    public ILogger Log { get; set; }
    public int HeaderLength { get; protected set; }
    public int MaxBufferLength { get; protected set; }
    public BACnetAddressTypes Type { get; protected set; }
    public BACnetMaxAdpu MaxAdpuLength { get; protected set; }
    public byte MaxInfoFrames { get; set; } = 0xFF;

    protected BACnetTransportBase(ILogger<BACnetTransportBase>? logger = null)
    {
        Log = logger ?? NullLogger<BACnetTransportBase>.Instance;
    }

    public abstract void Start();

    public abstract BACnetAddress GetBroadcastAddress();

    public virtual bool WaitForAllTransmits(int timeout)
    {
        return true; // not used 
    }

    public abstract int Send(byte[] buffer, int offset, int dataLength, BACnetAddress address, bool waitForTransmission, int timeout);

    public event MessageRecievedHandler MessageRecieved;

    protected void InvokeMessageRecieved(byte[] buffer, int offset, int msgLength, BACnetAddress remoteAddress)
    {
        MessageRecieved?.Invoke(this, buffer, offset, msgLength, remoteAddress);
    }

    public abstract void Dispose();
}
