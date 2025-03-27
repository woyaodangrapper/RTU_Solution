namespace System.Net.BACnet;

public interface IBACnetTransport : IDisposable
{
    byte MaxInfoFrames { get; set; }
    int HeaderLength { get; }
    int MaxBufferLength { get; }
    BACnetAddressTypes Type { get; }
    BACnetMaxAdpu MaxAdpuLength { get; }

    void Start();
    BACnetAddress GetBroadcastAddress();
    bool WaitForAllTransmits(int timeout);
    int Send(byte[] buffer, int offset, int dataLength, BACnetAddress address, bool waitForTransmission, int timeout);

    event MessageRecievedHandler MessageRecieved;
}
