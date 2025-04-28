using Microsoft.Extensions.Logging;

namespace System.Net.BACnet;

/// <summary>
///     This is the standard BACNet PTP transport
/// </summary>
public class BACnetPtpProtocolTransport : BACnetTransportBase
{
    public const int T_HEARTBEAT = 15000;
    public const int T_FRAME_ABORT = 2000;
    private bool _isConnected;
    private readonly bool _isServer;
    private readonly ManualResetEvent _maySend = new(false);
    private IBACnetSerialTransport _port;
    private bool _sequenceCounter;
    private Thread _thread;

    public string Password { get; set; }

    public BACnetPtpProtocolTransport(IBACnetSerialTransport transport, bool isServer)
    {
        _port = transport;
        _isServer = isServer;
        Type = BACnetAddressTypes.PTP;
        HeaderLength = PTP.PTP_HEADER_LENGTH;
        MaxBufferLength = 502;
        MaxAdpuLength = PTP.PTP_MAX_APDU;
    }

    public BACnetPtpProtocolTransport(string portName, int baudRate, bool isServer)
        : this(new BACnetSerialPortTransport(portName, baudRate), isServer)
    {
    }

    public override int Send(byte[] buffer, int offset, int dataLength, BACnetAddress address, bool waitForTransmission,
        int timeout)
    {
        var frameType = BACnetPtpFrameTypes.FRAME_TYPE_DATA0;
        if (_sequenceCounter) frameType = BACnetPtpFrameTypes.FRAME_TYPE_DATA1;
        _sequenceCounter = !_sequenceCounter;

        //add header
        var fullLength = PTP.Encode(buffer, offset - PTP.PTP_HEADER_LENGTH, frameType, dataLength);

        //wait for send allowed
        if (!_maySend.WaitOne(timeout))
            return -BACnetMstpProtocolTransport.ETIMEDOUT;

        Log.LogDebug(frameType.ToString());

        //send
        SendWithXonXoff(buffer, offset - HeaderLength, fullLength);
        return dataLength;
    }

    public override BACnetAddress GetBroadcastAddress()
    {
        return new BACnetAddress(BACnetAddressTypes.PTP, 0xFFFF, new byte[0]);
    }

    public override void Start()
    {
        if (_port == null) return;

        _thread = new Thread(PTPThread)
        {
            Name = "PTP Read",
            IsBackground = true
        };
        _thread.Start();
    }

    public override void Dispose()
    {
        _port?.Close();
        _port = null;
    }

    public override bool Equals(object obj)
    {
        if (obj is not BACnetPtpProtocolTransport) return false;
        var a = (BACnetPtpProtocolTransport)obj;
        return _port.Equals(a._port);
    }

    public override int GetHashCode()
    {
        return _port.GetHashCode();
    }

    public override string ToString()
    {
        return _port.ToString();
    }

    private void SendGreeting()
    {
        Log.LogDebug("Sending Greeting");
        byte[] greeting = { PTP.PTP_GREETING_PREAMBLE1, PTP.PTP_GREETING_PREAMBLE2, 0x43, 0x6E, 0x65, 0x74, 0x0D }; // BACnet\n
        _port.Write(greeting, 0, greeting.Length);
    }

    private static bool IsGreeting(IList<byte> buffer, int offset, int maxOffset)
    {
        byte[] greeting = { PTP.PTP_GREETING_PREAMBLE1, PTP.PTP_GREETING_PREAMBLE2, 0x43, 0x6E, 0x65, 0x74, 0x0D }; // BACnet\n
        maxOffset = Math.Min(offset + greeting.Length, maxOffset);
        for (var i = offset; i < maxOffset; i++)
            if (buffer[i] != greeting[i - offset])
                return false;
        return true;
    }

    private static void RemoveGreetingGarbage(byte[] buffer, ref int maxOffset)
    {
        while (maxOffset > 0)
        {
            while (maxOffset > 0 && buffer[0] != 0x42)
            {
                if (maxOffset > 1)
                    Array.Copy(buffer, 1, buffer, 0, maxOffset - 1);
                maxOffset--;
            }
            if (maxOffset > 1 && buffer[1] != 0x41)
                buffer[0] = 0xFF;
            else if (maxOffset > 2 && buffer[2] != 0x43)
                buffer[0] = 0xFF;
            else if (maxOffset > 3 && buffer[3] != 0x6E)
                buffer[0] = 0xFF;
            else if (maxOffset > 4 && buffer[4] != 0x65)
                buffer[0] = 0xFF;
            else if (maxOffset > 5 && buffer[5] != 0x74)
                buffer[0] = 0xFF;
            else if (maxOffset > 6 && buffer[6] != 0x0D)
                buffer[0] = 0xFF;
            else
                break;
        }
    }

    private bool WaitForGreeting(int timeout)
    {
        if (_port == null) return false;
        var buffer = new byte[7];
        var offset = 0;
        while (offset < 7)
        {
            var currentTimeout = offset == 0 ? timeout : T_FRAME_ABORT;
            var rx = _port.Read(buffer, offset, 7 - offset, currentTimeout);
            if (rx <= 0) return false;
            offset += rx;

            //remove garbage
            RemoveGreetingGarbage(buffer, ref offset);
        }
        return true;
    }

    private bool Reconnect()
    {
        _isConnected = false;
        _maySend.Reset();

        if (_port == null)
            return false;

        try
        {
            _port.Close();
        }
        catch
        {
        }

        try
        {
            _port.Open();
        }
        catch
        {
            return false;
        }

        //connect procedure
        if (_isServer)
        {
            ////wait for greeting
            //if (!WaitForGreeting(-1))
            //{
            //    Log.LogDebug("Garbage Greeting");
            //    return false;
            //}
            //if (StateLogging)
            //    Log.LogDebug("Got Greeting");

            ////request connection
            //SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_CONNECT_REQUEST);
        }
        else
        {
            //send greeting
            SendGreeting();
        }

        _isConnected = true;
        return true;
    }

    private void RemoveGarbage(byte[] buffer, ref int length)
    {
        //scan for preambles
        for (var i = 0; i < length - 1; i++)
        {
            if ((buffer[i] != PTP.PTP_PREAMBLE1 || buffer[i + 1] != PTP.PTP_PREAMBLE2) && !IsGreeting(buffer, i, length))
                continue;

            if (i <= 0)
                return;

            //move back
            Array.Copy(buffer, i, buffer, 0, length - i);
            length -= i;
            Log.LogDebug("Garbage");
            return;
        }

        //one preamble?
        if (length > 0 &&
            (buffer[length - 1] == PTP.PTP_PREAMBLE1 || buffer[length - 1] == PTP.PTP_GREETING_PREAMBLE1))
        {
            buffer[0] = buffer[length - 1];
            length = 1;
            Log.LogDebug("Garbage");
            return;
        }

        //no preamble?
        if (length <= 0)
            return;

        length = 0;
        Log.LogDebug("Garbage");
    }

    private static void RemoveXonOff(byte[] buffer, int offset, ref int maxOffset, ref bool complimentNext)
    {
        //X'10' (DLE)  => X'10' X'90' 
        //X'11' (XON)  => X'10' X'91' 
        //X'13' (XOFF) => X'10' X'93'

        for (var i = offset; i < maxOffset; i++)
        {
            if (complimentNext)
            {
                buffer[i] &= 0x7F;
                complimentNext = false;
            }
            else if (buffer[i] == 0x11 || buffer[i] == 0x13 || buffer[i] == 0x10)
            {
                if (buffer[i] == 0x10) complimentNext = true;
                if (maxOffset - i > 0)
                    Array.Copy(buffer, i + 1, buffer, i, maxOffset - i);
                maxOffset--;
                i--;
            }
        }
    }

    private void SendWithXonXoff(byte[] buffer, int offset, int length)
    {
        var escape = new byte[1] { 0x10 };
        var maxOffset = length + offset;

        //scan
        for (var i = offset; i < maxOffset; i++)
        {
            if (buffer[i] != 0x10 && buffer[i] != 0x11 && buffer[i] != 0x13)
                continue;

            _port.Write(buffer, offset, i - offset);
            _port.Write(escape, 0, 1);
            buffer[i] |= 0x80;
            offset = i;
        }

        //leftover
        _port.Write(buffer, offset, maxOffset - offset);
    }

    private void SendFrame(BACnetPtpFrameTypes frameType, byte[] buffer = null, int msgLength = 0)
    {
        if (_port == null)
            return;

        var fullLength = PTP.PTP_HEADER_LENGTH + msgLength + (msgLength > 0 ? 2 : 0);
        if (buffer == null) buffer = new byte[fullLength];
        PTP.Encode(buffer, 0, frameType, msgLength);

        Log.LogDebug(frameType.ToString());

        //send
        SendWithXonXoff(buffer, 0, fullLength);
    }

    private void SendDisconnect(BACnetPtpDisconnectReasons bacnetPtpDisconnectReasons)
    {
        var buffer = new byte[PTP.PTP_HEADER_LENGTH + 1 + 2];
        buffer[PTP.PTP_HEADER_LENGTH] = (byte)bacnetPtpDisconnectReasons;
        SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_DISCONNECT_REQUEST, buffer, 1);
    }

    private BACnetMstpProtocolTransport.GetMessageStatus ProcessRxStatus(byte[] buffer, ref int offset, int rx)
    {
        if (rx == -BACnetMstpProtocolTransport.ETIMEDOUT)
        {
            //drop message
            var status = offset == 0
                ? BACnetMstpProtocolTransport.GetMessageStatus.Timeout
                : BACnetMstpProtocolTransport.GetMessageStatus.SubTimeout;
            buffer[0] = 0xFF;
            RemoveGarbage(buffer, ref offset);
            return status;
        }
        if (rx < 0)
        {
            //drop message
            buffer[0] = 0xFF;
            RemoveGarbage(buffer, ref offset);
            return BACnetMstpProtocolTransport.GetMessageStatus.ConnectionError;
        }

        if (rx != 0)
            return BACnetMstpProtocolTransport.GetMessageStatus.Good;

        //drop message
        buffer[0] = 0xFF;
        RemoveGarbage(buffer, ref offset);
        return BACnetMstpProtocolTransport.GetMessageStatus.ConnectionClose;
    }

    private BACnetMstpProtocolTransport.GetMessageStatus GetNextMessage(byte[] buffer, ref int offset,
        int timeoutMs, out BACnetPtpFrameTypes frameType, out int msgLength)
    {
        BACnetMstpProtocolTransport.GetMessageStatus status;
        var timeout = timeoutMs;

        frameType = BACnetPtpFrameTypes.FRAME_TYPE_HEARTBEAT_XOFF;
        msgLength = 0;
        var complimentNext = false;

        //fetch header
        while (offset < PTP.PTP_HEADER_LENGTH)
        {
            if (_port == null) return BACnetMstpProtocolTransport.GetMessageStatus.ConnectionClose;

            timeout = offset > 0 ? T_FRAME_ABORT : timeoutMs;

            //read 
            var rx = _port.Read(buffer, offset, PTP.PTP_HEADER_LENGTH - offset, timeout);
            status = ProcessRxStatus(buffer, ref offset, rx);
            if (status != BACnetMstpProtocolTransport.GetMessageStatus.Good) return status;

            //remove XON/XOFF
            var newOffset = offset + rx;
            RemoveXonOff(buffer, offset, ref newOffset, ref complimentNext);
            offset = newOffset;

            //remove garbage
            RemoveGarbage(buffer, ref offset);
        }

        //greeting
        if (IsGreeting(buffer, 0, offset))
        {
            //get last byte
            var rx = _port.Read(buffer, offset, 1, timeout);
            status = ProcessRxStatus(buffer, ref offset, rx);
            if (status != BACnetMstpProtocolTransport.GetMessageStatus.Good) return status;
            offset += 1;
            if (IsGreeting(buffer, 0, offset))
            {
                frameType = BACnetPtpFrameTypes.FRAME_TYPE_GREETING;
                Log.LogDebug(frameType.ToString());
                return BACnetMstpProtocolTransport.GetMessageStatus.Good;
            }
            //drop message
            buffer[0] = 0xFF;
            RemoveGarbage(buffer, ref offset);
            return BACnetMstpProtocolTransport.GetMessageStatus.DecodeError;
        }

        //decode
        if (PTP.Decode(buffer, 0, offset, out frameType, out msgLength) < 0)
        {
            //drop message
            buffer[0] = 0xFF;
            RemoveGarbage(buffer, ref offset);
            return BACnetMstpProtocolTransport.GetMessageStatus.DecodeError;
        }

        //valid length?
        var fullMsgLength = msgLength + PTP.PTP_HEADER_LENGTH + (msgLength > 0 ? 2 : 0);
        if (msgLength > MaxBufferLength)
        {
            //drop message
            buffer[0] = 0xFF;
            RemoveGarbage(buffer, ref offset);
            return BACnetMstpProtocolTransport.GetMessageStatus.DecodeError;
        }

        //fetch data
        if (msgLength > 0)
        {
            timeout = T_FRAME_ABORT; //set sub timeout
            while (offset < fullMsgLength)
            {
                //read 
                var rx = _port.Read(buffer, offset, fullMsgLength - offset, timeout);
                status = ProcessRxStatus(buffer, ref offset, rx);
                if (status != BACnetMstpProtocolTransport.GetMessageStatus.Good) return status;

                //remove XON/XOFF
                var newOffset = offset + rx;
                RemoveXonOff(buffer, offset, ref newOffset, ref complimentNext);
                offset = newOffset;
            }

            //verify data crc
            if (PTP.Decode(buffer, 0, offset, out frameType, out msgLength) < 0)
            {
                //drop message
                buffer[0] = 0xFF;
                RemoveGarbage(buffer, ref offset);
                return BACnetMstpProtocolTransport.GetMessageStatus.DecodeError;
            }
        }

        Log.LogDebug(frameType.ToString());

        //done
        return BACnetMstpProtocolTransport.GetMessageStatus.Good;
    }

    private void PTPThread()
    {
        var buffer = new byte[MaxBufferLength];
        try
        {
            while (_port != null)
            {
                //connect if needed
                if (!_isConnected)
                {
                    if (!Reconnect())
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                }

                //read message
                var offset = 0;
                var status = GetNextMessage(buffer, ref offset, T_HEARTBEAT, out var frameType, out var msgLength);

                //action
                switch (status)
                {
                    case BACnetMstpProtocolTransport.GetMessageStatus.ConnectionClose:
                    case BACnetMstpProtocolTransport.GetMessageStatus.ConnectionError:
                        Log.LogWarning("Connection disturbance");
                        Reconnect();
                        break;

                    case BACnetMstpProtocolTransport.GetMessageStatus.DecodeError:
                        Log.LogWarning("PTP decode error");
                        break;

                    case BACnetMstpProtocolTransport.GetMessageStatus.SubTimeout:
                        Log.LogWarning("PTP frame abort");
                        break;

                    case BACnetMstpProtocolTransport.GetMessageStatus.Timeout:
                        SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_HEARTBEAT_XON);
                        //both server and client will send this
                        break;

                    case BACnetMstpProtocolTransport.GetMessageStatus.Good:

                        //action
                        switch (frameType)
                        {
                            case BACnetPtpFrameTypes.FRAME_TYPE_GREETING:
                                //request connection
                                SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_CONNECT_REQUEST);
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_HEARTBEAT_XON:
                                _maySend.Set();
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_HEARTBEAT_XOFF:
                                _maySend.Reset();
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_DATA0:
                                //send confirm
                                SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_DATA_ACK0_XON);

                                //notify the sky!
                                InvokeMessageRecieved(buffer, PTP.PTP_HEADER_LENGTH, msgLength, GetBroadcastAddress());
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_DATA1:
                                //send confirm
                                SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_DATA_ACK1_XON);
                                //notify the sky!
                                InvokeMessageRecieved(buffer, PTP.PTP_HEADER_LENGTH, msgLength, GetBroadcastAddress());
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_DATA_ACK0_XOFF:
                            case BACnetPtpFrameTypes.FRAME_TYPE_DATA_ACK1_XOFF:
                                //so, the other one got the message, eh?
                                _maySend.Reset();
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_DATA_ACK0_XON:
                            case BACnetPtpFrameTypes.FRAME_TYPE_DATA_ACK1_XON:
                                //so, the other one got the message, eh?
                                _maySend.Set();
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_DATA_NAK0_XOFF:
                            case BACnetPtpFrameTypes.FRAME_TYPE_DATA_NAK1_XOFF:
                                _maySend.Reset();
                                //denial, eh?
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_DATA_NAK0_XON:
                            case BACnetPtpFrameTypes.FRAME_TYPE_DATA_NAK1_XON:
                                _maySend.Set();
                                //denial, eh?
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_CONNECT_REQUEST:
                                //also send a password perhaps?
                                if (!string.IsNullOrEmpty(Password))
                                {
                                    var pass = Encoding.ASCII.GetBytes(Password);
                                    var tmpBuffer = new byte[PTP.PTP_HEADER_LENGTH + pass.Length + 2];
                                    Array.Copy(pass, 0, tmpBuffer, PTP.PTP_HEADER_LENGTH, pass.Length);
                                    SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_CONNECT_RESPONSE, tmpBuffer,
                                        pass.Length);
                                }
                                else
                                    SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_CONNECT_RESPONSE);
                                //we're ready
                                SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_HEARTBEAT_XON);
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_CONNECT_RESPONSE:
                                if (msgLength > 0 && !string.IsNullOrEmpty(Password))
                                {
                                    var password = Encoding.ASCII.GetString(buffer, PTP.PTP_HEADER_LENGTH,
                                        msgLength);
                                    if (password != Password)
                                        SendDisconnect(BACnetPtpDisconnectReasons.PTP_DISCONNECT_INVALID_PASSWORD);
                                    else
                                        SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_HEARTBEAT_XON); //we're ready
                                }
                                else
                                {
                                    //we're ready
                                    SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_HEARTBEAT_XON);
                                }
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_DISCONNECT_REQUEST:
                                var reason = BACnetPtpDisconnectReasons.PTP_DISCONNECT_OTHER;
                                if (msgLength > 0)
                                    reason = (BACnetPtpDisconnectReasons)buffer[PTP.PTP_HEADER_LENGTH];
                                SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_DISCONNECT_RESPONSE);
                                Log.LogDebug($"Disconnect requested: {reason}");
                                Reconnect();
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_DISCONNECT_RESPONSE:
                                _maySend.Reset();
                                //hopefully we'll be closing down now
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_TEST_REQUEST:
                                SendFrame(BACnetPtpFrameTypes.FRAME_TYPE_TEST_RESPONSE, buffer, msgLength);
                                break;

                            case BACnetPtpFrameTypes.FRAME_TYPE_TEST_RESPONSE:
                                //good
                                break;
                        }
                        break;
                }
            }
            Log.LogDebug("PTP thread is closing down");
        }
        catch (Exception ex)
        {
            Log.LogError($"Exception in PTP thread", ex);
        }
    }
}
