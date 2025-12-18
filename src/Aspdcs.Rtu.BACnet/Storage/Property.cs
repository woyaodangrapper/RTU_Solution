namespace System.Net.BACnet.Storage;

[Serializable]
public class Property
{
    [XmlIgnore]
    public BACnetPropertyIds Id { get; set; }

    [XmlAttribute("Id")]
    public string IdText
    {
        get
        {
            return Id.ToString();
        }
        set
        {
            Id = (BACnetPropertyIds)Enum.Parse(typeof(BACnetPropertyIds), value);
        }
    }

    [XmlAttribute]
    public BACnetApplicationTags Tag { get; set; }

    [XmlElement]
    public string[] Value { get; set; }

    public static BACnetValue DeserializeValue(string value, BACnetApplicationTags type)
    {
        switch (type)
        {
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_NULL:
                return value == ""
                    ? new BACnetValue(type, null)
                    : new BACnetValue(value);
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN:
                return new BACnetValue(type, bool.Parse(value));
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                return new BACnetValue(type, uint.Parse(value));
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT:
                return new BACnetValue(type, int.Parse(value));
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                return new BACnetValue(type, float.Parse(value, CultureInfo.InvariantCulture));
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE:
                return new BACnetValue(type, double.Parse(value, CultureInfo.InvariantCulture));
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING:
                try
                {
                    return new BACnetValue(type, Convert.FromBase64String(value));
                }
                catch
                {
                    return new BACnetValue(type, value);
                }
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED:
                try
                {
                    return new BACnetValue(type, Convert.FromBase64String(value));
                }
                catch
                {
                    return new BACnetValue(type, value);
                }
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                return new BACnetValue(type, value);
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING:
                return new BACnetValue(type, BACnetBitString.Parse(value));
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                return new BACnetValue(type, uint.Parse(value));
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_DATE:
                return new BACnetValue(type, DateTime.Parse(value));
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_TIME:
                return new BACnetValue(type, DateTime.Parse(value));
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID:
                return new BACnetValue(type, BACnetObjectId.Parse(value));
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_READ_ACCESS_SPECIFICATION:
                return new BACnetValue(type, BACnetReadAccessSpecification.Parse(value));
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_PROPERTY_REFERENCE:
                return new BACnetValue(type, BACnetDeviceObjectPropertyReference.Parse(value));
            default:
                return new BACnetValue(type, null);
        }
    }

    public static string SerializeValue(BACnetValue value, BACnetApplicationTags type)
    {
        switch (type)
        {
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_NULL:
                return value.ToString(); // Modif FC
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                return ((float)value.Value).ToString(CultureInfo.InvariantCulture);
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE:
                return ((double)value.Value).ToString(CultureInfo.InvariantCulture);
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING:
                return Convert.ToBase64String((byte[])value.Value);
            case BACnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED:
                {
                    return value.Value is byte[]? Convert.ToBase64String((byte[])value.Value)
                        : string.Join(";", ((BACnetValue[])value.Value)
                            .Select(v => SerializeValue(v, v.Tag)));
                }
            default:
                return value.Value.ToString();
        }
    }

    [XmlIgnore]
    public IList<BACnetValue> BACnetValue
    {
        get
        {
            if (Value == null)
                return new BACnetValue[0];

            var ret = new BACnetValue[Value.Length];
            for (var i = 0; i < ret.Length; i++)
                ret[i] = DeserializeValue(Value[i], Tag);

            return ret;
        }
        set
        {
            Value = value.Select(v => SerializeValue(v, Tag)).ToArray();
        }
    }
}
