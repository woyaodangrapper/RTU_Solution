namespace System.Net.BACnet;

public interface IBACnetSerialTransport : IDisposable
{
    int BytesToRead { get; }

    void Open();
    void Close();
    int Read(byte[] buffer, int offset, int length, int timeoutMs);
    void Write(byte[] buffer, int offset, int length);
}
