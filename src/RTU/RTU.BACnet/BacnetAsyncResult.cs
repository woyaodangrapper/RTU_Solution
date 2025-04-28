namespace System.Net.BACnet;

public class BACnetAsyncResult : IAsyncResult, IDisposable
{
    private BACnetClient _comm;
    private readonly byte _waitInvokeId;
    private Exception _error;
    private readonly byte[] _transmitBuffer;
    private readonly int _transmitLength;
    private readonly bool _waitForTransmit;
    private readonly int _transmitTimeout;
    private ManualResetEvent _waitHandle;
    private readonly BACnetAddress _address;

    public bool Segmented { get; private set; }
    public byte[] Result { get; private set; }
    public object AsyncState { get; set; }
    public bool CompletedSynchronously { get; private set; }
    public WaitHandle AsyncWaitHandle => _waitHandle;
    public bool IsCompleted => _waitHandle.WaitOne(0);
    public BACnetAddress Address => _address;

    public Exception Error
    {
        get => _error;
        set
        {
            _error = value;
            CompletedSynchronously = true;
            _waitHandle.Set();
        }
    }

    public BACnetAsyncResult(BACnetClient comm, BACnetAddress adr, byte invokeId, byte[] transmitBuffer, int transmitLength, bool waitForTransmit, int transmitTimeout)
    {
        _transmitTimeout = transmitTimeout;
        _address = adr;
        _waitForTransmit = waitForTransmit;
        _transmitBuffer = transmitBuffer;
        _transmitLength = transmitLength;
        _comm = comm;
        _waitInvokeId = invokeId;
        _comm.OnComplexAck += OnComplexAck;
        _comm.OnError += OnError;
        _comm.OnAbort += OnAbort;
        _comm.OnReject += OnReject;
        _comm.OnSimpleAck += OnSimpleAck;
        _comm.OnSegment += OnSegment;
        _waitHandle = new ManualResetEvent(false);
    }

    public void Resend()
    {
        try
        {
            if (_comm.Transport.Send(_transmitBuffer, _comm.Transport.HeaderLength, _transmitLength, _address, _waitForTransmit, _transmitTimeout) < 0)
            {
                Error = new IOException("Write Timeout");
            }
        }
        catch (Exception ex)
        {
            Error = new Exception($"Write Exception: {ex.Message}");
        }
    }

    private void OnSegment(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, BACnetMaxSegments maxSegments, BACnetMaxAdpu maxAdpu, byte sequenceNumber, byte[] buffer, int offset, int length)
    {
        if (invokeId != _waitInvokeId || !adr.Equals(_address))
            return;

        Segmented = true;
        _waitHandle.Set();
    }

    private void OnSimpleAck(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, byte[] data, int dataOffset, int dataLength)
    {
        if (invokeId != _waitInvokeId || !adr.Equals(_address))
            return;

        _waitHandle.Set();
    }

    private void OnAbort(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, byte invokeId, BACnetAbortReason reason, byte[] buffer, int offset, int length)
    {
        if (invokeId != _waitInvokeId || !adr.Equals(_address))
            return;

        Error = new Exception($"Abort from device, reason: {reason}");
    }

    private void OnReject(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, byte invokeId, BACnetRejectReason reason, byte[] buffer, int offset, int length)
    {
        if (invokeId != _waitInvokeId || !adr.Equals(_address))
            return;

        Error = new Exception($"Reject from device, reason: {reason}");
    }

    private void OnError(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, BACnetErrorClasses errorClass, BACnetErrorCodes errorCode, byte[] buffer, int offset, int length)
    {
        if (invokeId != _waitInvokeId || !adr.Equals(_address))
            return;

        Error = new Exception($"Error from device: {errorClass} - {errorCode}");
    }

    private void OnComplexAck(BACnetClient sender, BACnetAddress adr, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, byte[] buffer, int offset, int length)
    {
        if (invokeId != _waitInvokeId || !adr.Equals(_address))
            return;

        Segmented = false;
        Result = new byte[length];

        if (length > 0)
            Array.Copy(buffer, offset, Result, 0, length);

        //notify waiter even if segmented
        _waitHandle.Set();
    }

    /// <summary>
    /// Will continue waiting until all segments are recieved
    /// </summary>
    public bool WaitForDone(int timeout)
    {
        while (true)
        {
            if (!AsyncWaitHandle.WaitOne(timeout))
                return false;
            if (Segmented)
                _waitHandle.Reset();
            else
                return true;
        }
    }

    public void Dispose()
    {
        if (_comm != null)
        {
            _comm.OnComplexAck -= OnComplexAck;
            _comm.OnError -= OnError;
            _comm.OnAbort -= OnAbort;
            _comm.OnReject -= OnReject;
            _comm.OnSimpleAck -= OnSimpleAck;
            _comm.OnSegment -= OnSegment;
            _comm = null;
        }

        if (_waitHandle != null)
        {
            _waitHandle.Dispose();
            _waitHandle = null;
        }
    }
}
