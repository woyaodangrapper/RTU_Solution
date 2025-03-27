namespace System.Net.BACnet;

public struct DeviceReportingRecipient : ASN1.IEncode
{
    public BACnetBitString WeekofDay;
    public DateTime toTime, fromTime;

    public BACnetObjectId Id;
    public BACnetAddress adr;

    public uint processIdentifier;
    public bool Ack_Required;
    public BACnetBitString evenType;

    public DeviceReportingRecipient(BACnetValue v0, BACnetValue v1, BACnetValue v2, BACnetValue v3, BACnetValue v4, BACnetValue v5, BACnetValue v6)
    {
        Id = new BACnetObjectId();
        adr = null;

        WeekofDay = (BACnetBitString)v0.Value;
        fromTime = (DateTime)v1.Value;
        toTime = (DateTime)v2.Value;
        if (v3.Value is BACnetObjectId id)
        {
            Id = id;
        }
        else
        {
            var netdescr = (BACnetValue[])v3.Value;
            var s = (ushort)(uint)netdescr[0].Value;
            var b = (byte[])netdescr[1].Value;
            adr = new BACnetAddress(BACnetAddressTypes.IP, s, b);
        }
        processIdentifier = (uint)v4.Value;
        Ack_Required = (bool)v5.Value;
        evenType = (BACnetBitString)v6.Value;
    }

    public DeviceReportingRecipient(BACnetBitString weekofDay, DateTime fromTime, DateTime toTime, BACnetObjectId id, uint processIdentifier, bool ackRequired, BACnetBitString evenType)
    {
        adr = null;

        WeekofDay = weekofDay;
        this.toTime = toTime;
        this.fromTime = fromTime;
        Id = id;
        this.processIdentifier = processIdentifier;
        Ack_Required = ackRequired;
        this.evenType = evenType;
    }

    public DeviceReportingRecipient(BACnetBitString weekofDay, DateTime fromTime, DateTime toTime, BACnetAddress adr, uint processIdentifier, bool ackRequired, BACnetBitString evenType)
    {
        Id = new BACnetObjectId();
        WeekofDay = weekofDay;
        this.toTime = toTime;
        this.fromTime = fromTime;
        this.adr = adr;
        this.processIdentifier = processIdentifier;
        Ack_Required = ackRequired;
        this.evenType = evenType;
    }

    public void Encode(EncodeBuffer buffer)
    {
        ASN1.bacapp_encode_application_data(buffer, new BACnetValue(WeekofDay));
        ASN1.bacapp_encode_application_data(buffer, new BACnetValue(BACnetApplicationTags.BACNET_APPLICATION_TAG_TIME, fromTime));
        ASN1.bacapp_encode_application_data(buffer, new BACnetValue(BACnetApplicationTags.BACNET_APPLICATION_TAG_TIME, toTime));

        if (adr != null)
        {
            adr.Encode(buffer);
        }
        else
        {
            // BACnetObjectId is context specific encoded
            ASN1.encode_context_object_id(buffer, 0, Id.type, Id.instance);
        }

        ASN1.bacapp_encode_application_data(buffer, new BACnetValue(processIdentifier));
        ASN1.bacapp_encode_application_data(buffer, new BACnetValue(Ack_Required));
        ASN1.bacapp_encode_application_data(buffer, new BACnetValue(evenType));
    }
}
