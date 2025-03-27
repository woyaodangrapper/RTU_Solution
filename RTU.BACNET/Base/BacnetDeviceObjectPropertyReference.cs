namespace System.Net.BACnet;

public struct BACnetDeviceObjectPropertyReference : ASN1.IEncode
{
    public BACnetObjectId objectIdentifier;
    public BACnetPropertyIds propertyIdentifier;
    public uint arrayIndex;
    public BACnetObjectId deviceIndentifier;

    public BACnetDeviceObjectPropertyReference(BACnetObjectId objectIdentifier, BACnetPropertyIds propertyIdentifier, BACnetObjectId? deviceIndentifier = null, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
    {
        this.objectIdentifier = objectIdentifier;
        this.propertyIdentifier = propertyIdentifier;
        this.arrayIndex = arrayIndex;
        this.deviceIndentifier = deviceIndentifier ?? new BACnetObjectId(BACnetObjectTypes.MAX_BACNET_OBJECT_TYPE, 0);
    }

    public void Encode(EncodeBuffer buffer)
    {
        ASN1.bacapp_encode_device_obj_property_ref(buffer, this);
    }

    public BACnetObjectId ObjectId
    {
        get => objectIdentifier;
        set => objectIdentifier = value;
    }

    public int ArrayIndex // shows -1 when it's ASN1.BACNET_ARRAY_ALL
    {
        get => arrayIndex != ASN1.BACNET_ARRAY_ALL
            ? (int)arrayIndex
            : -1;
        set => arrayIndex = value < 0
            ? ASN1.BACNET_ARRAY_ALL
            : (uint)value;
    }

    public BACnetObjectId? DeviceId  // shows null when it's not OBJECT_DEVICE
    {
        get
        {
            return deviceIndentifier.type == BACnetObjectTypes.OBJECT_DEVICE
                ? (BACnetObjectId?)deviceIndentifier
                : null;
        }
        set
        {
            deviceIndentifier = value ?? new BACnetObjectId();
        }
    }

    public BACnetPropertyIds PropertyId
    {
        get => propertyIdentifier;
        set => propertyIdentifier = value;
    }

    public static object Parse(string value)
    {
        var parts = value.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

        BACnetObjectId? deviceId = null;
        BACnetObjectId objectId;

        switch (parts.Length)
        {
            case 2:
                objectId = BACnetObjectId.Parse(parts[0]);
                break;

            case 3:
                deviceId = BACnetObjectId.Parse(parts[0]);
                objectId = BACnetObjectId.Parse(parts[1]);
                break;

            default:
                throw new ArgumentException("Invalid format", nameof(value));
        }

        if (!Enum.TryParse(parts.Last(), out BACnetPropertyIds propertyId))
        {
            if (!uint.TryParse(parts.Last(), out var vendorSpecificPropertyId))
                throw new ArgumentException("Invalid format of property id", nameof(value));

            propertyId = (BACnetPropertyIds)vendorSpecificPropertyId;
        }

        return new BACnetDeviceObjectPropertyReference
        {
            DeviceId = deviceId,
            ObjectId = objectId,
            PropertyId = propertyId,
            ArrayIndex = -1
        };
    }

    public override string ToString()
    {
        return DeviceId != null
            ? $"{DeviceId}.{ObjectId}.{PropertyId}"
            : $"{ObjectId}.{PropertyId}";
    }
}
