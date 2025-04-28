namespace System.Net.BACnet.Serialize;

public class APDU
{
    public static BACnetPduTypes GetDecodedType(byte[] buffer, int offset)
    {
        return (BACnetPduTypes)buffer[offset];
    }

    public static void SetDecodedType(byte[] buffer, int offset, BACnetPduTypes type)
    {
        buffer[offset] = (byte)type;
    }

    public static int GetDecodedInvokeId(byte[] buffer, int offset)
    {
        var type = GetDecodedType(buffer, offset);
        switch (type & BACnetPduTypes.PDU_TYPE_MASK)
        {
            case BACnetPduTypes.PDU_TYPE_SIMPLE_ACK:
            case BACnetPduTypes.PDU_TYPE_COMPLEX_ACK:
            case BACnetPduTypes.PDU_TYPE_ERROR:
            case BACnetPduTypes.PDU_TYPE_REJECT:
            case BACnetPduTypes.PDU_TYPE_ABORT:
                return buffer[offset + 1];
            case BACnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST:
                return buffer[offset + 2];
            default:
                return -1;
        }
    }

    public static void EncodeConfirmedServiceRequest(EncodeBuffer buffer, BACnetPduTypes type, BACnetConfirmedServices service, BACnetMaxSegments maxSegments,
        BACnetMaxAdpu maxAdpu, byte invokeId, byte sequenceNumber = 0, byte proposedWindowSize = 0)
    {
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = (byte)((byte)maxSegments | (byte)maxAdpu);
        buffer.buffer[buffer.offset++] = invokeId;

        if ((type & BACnetPduTypes.SEGMENTED_MESSAGE) > 0)
        {
            buffer.buffer[buffer.offset++] = sequenceNumber;
            buffer.buffer[buffer.offset++] = proposedWindowSize;
        }
        buffer.buffer[buffer.offset++] = (byte)service;
    }

    public static int DecodeConfirmedServiceRequest(byte[] buffer, int offset, out BACnetPduTypes type, out BACnetConfirmedServices service,
        out BACnetMaxSegments maxSegments, out BACnetMaxAdpu maxAdpu, out byte invokeId, out byte sequenceNumber, out byte proposedWindowNumber)
    {
        var orgOffset = offset;

        type = (BACnetPduTypes)buffer[offset++];
        maxSegments = (BACnetMaxSegments)(buffer[offset] & 0xF0);
        maxAdpu = (BACnetMaxAdpu)(buffer[offset++] & 0x0F);
        invokeId = buffer[offset++];

        sequenceNumber = 0;
        proposedWindowNumber = 0;
        if ((type & BACnetPduTypes.SEGMENTED_MESSAGE) > 0)
        {
            sequenceNumber = buffer[offset++];
            proposedWindowNumber = buffer[offset++];
        }
        service = (BACnetConfirmedServices)buffer[offset++];

        return offset - orgOffset;
    }

    public static void EncodeUnconfirmedServiceRequest(EncodeBuffer buffer, BACnetPduTypes type, BACnetUnconfirmedServices service)
    {
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = (byte)service;
    }

    public static int DecodeUnconfirmedServiceRequest(byte[] buffer, int offset, out BACnetPduTypes type, out BACnetUnconfirmedServices service)
    {
        var orgOffset = offset;

        type = (BACnetPduTypes)buffer[offset++];
        service = (BACnetUnconfirmedServices)buffer[offset++];

        return offset - orgOffset;
    }

    public static void EncodeSimpleAck(EncodeBuffer buffer, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId)
    {
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = invokeId;
        buffer.buffer[buffer.offset++] = (byte)service;
    }

    public static int DecodeSimpleAck(byte[] buffer, int offset, out BACnetPduTypes type, out BACnetConfirmedServices service, out byte invokeId)
    {
        var orgOffset = offset;

        type = (BACnetPduTypes)buffer[offset++];
        invokeId = buffer[offset++];
        service = (BACnetConfirmedServices)buffer[offset++];

        return offset - orgOffset;
    }

    public static int EncodeComplexAck(EncodeBuffer buffer, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId, byte sequenceNumber = 0, byte proposedWindowNumber = 0)
    {
        var len = 3;
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = invokeId;
        if ((type & BACnetPduTypes.SEGMENTED_MESSAGE) > 0)
        {
            buffer.buffer[buffer.offset++] = sequenceNumber;
            buffer.buffer[buffer.offset++] = proposedWindowNumber;
            len += 2;
        }
        buffer.buffer[buffer.offset++] = (byte)service;
        return len;
    }

    public static int DecodeComplexAck(byte[] buffer, int offset, out BACnetPduTypes type, out BACnetConfirmedServices service, out byte invokeId,
        out byte sequenceNumber, out byte proposedWindowNumber)
    {
        var orgOffset = offset;

        type = (BACnetPduTypes)buffer[offset++];
        invokeId = buffer[offset++];

        sequenceNumber = 0;
        proposedWindowNumber = 0;
        if ((type & BACnetPduTypes.SEGMENTED_MESSAGE) > 0)
        {
            sequenceNumber = buffer[offset++];
            proposedWindowNumber = buffer[offset++];
        }
        service = (BACnetConfirmedServices)buffer[offset++];

        return offset - orgOffset;
    }

    public static void EncodeSegmentAck(EncodeBuffer buffer, BACnetPduTypes type, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize)
    {
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = originalInvokeId;
        buffer.buffer[buffer.offset++] = sequenceNumber;
        buffer.buffer[buffer.offset++] = actualWindowSize;
    }

    public static int DecodeSegmentAck(byte[] buffer, int offset, out BACnetPduTypes type, out byte originalInvokeId, out byte sequenceNumber, out byte actualWindowSize)
    {
        var orgOffset = offset;

        type = (BACnetPduTypes)buffer[offset++];
        originalInvokeId = buffer[offset++];
        sequenceNumber = buffer[offset++];
        actualWindowSize = buffer[offset++];

        return offset - orgOffset;
    }

    public static void EncodeError(EncodeBuffer buffer, BACnetPduTypes type, BACnetConfirmedServices service, byte invokeId)
    {
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = invokeId;
        buffer.buffer[buffer.offset++] = (byte)service;
    }

    public static int DecodeError(byte[] buffer, int offset, out BACnetPduTypes type, out BACnetConfirmedServices service, out byte invokeId)
    {
        var orgOffset = offset;

        type = (BACnetPduTypes)buffer[offset++];
        invokeId = buffer[offset++];
        service = (BACnetConfirmedServices)buffer[offset++];

        return offset - orgOffset;
    }

    public static void EncodeAbort(EncodeBuffer buffer, BACnetPduTypes type, byte invokeId, BACnetAbortReason reason)
    {
        EncodeAbortOrReject(buffer, type, invokeId, reason);
    }

    public static void EncodeReject(EncodeBuffer buffer, BACnetPduTypes type, byte invokeId, BACnetRejectReason reason)
    {
        EncodeAbortOrReject(buffer, type, invokeId, reason);
    }

    private static void EncodeAbortOrReject(EncodeBuffer buffer, BACnetPduTypes type, byte invokeId, dynamic reason)
    {
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = invokeId;
        buffer.buffer[buffer.offset++] = (byte)reason;
    }

    public static int DecodeAbort(byte[] buffer, int offset, out BACnetPduTypes type,
        out byte invokeId, out BACnetAbortReason reason)
    {
        return DecodeAbortOrReject(buffer, offset, out type, out invokeId, out reason);
    }

    public static int DecodeReject(byte[] buffer, int offset, out BACnetPduTypes type,
        out byte invokeId, out BACnetRejectReason reason)
    {
        return DecodeAbortOrReject(buffer, offset, out type, out invokeId, out reason);
    }

    private static int DecodeAbortOrReject<TReason>(byte[] buffer, int offset,
        out BACnetPduTypes type, out byte invokeId, out TReason reason)
    {
        var orgOffset = offset;

        type = (BACnetPduTypes)buffer[offset++];
        invokeId = buffer[offset++];
        reason = (TReason)(dynamic)buffer[offset++];

        return offset - orgOffset;
    }
}
