namespace System.Net.BACnet;

public struct BACnetValue
{
    public BACnetApplicationTags Tag;
    public object Value;

    public BACnetValue(BACnetApplicationTags tag, object value)
    {
        Tag = tag;
        Value = value;
    }

    public BACnetValue(object value)
    {
        Value = value;
        Tag = BACnetApplicationTags.BACNET_APPLICATION_TAG_NULL;

        //guess at the tag
        if (value != null)
            Tag = TagFromType(value.GetType());
    }

    public BACnetApplicationTags TagFromType(Type t)
    {
        if (t == typeof(string))
            return BACnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING;
        if (t == typeof(int) || t == typeof(short) || t == typeof(sbyte))
            return BACnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT;
        if (t == typeof(uint) || t == typeof(ushort) || t == typeof(byte))
            return BACnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
        if (t == typeof(bool))
            return BACnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN;
        if (t == typeof(float))
            return BACnetApplicationTags.BACNET_APPLICATION_TAG_REAL;
        if (t == typeof(double))
            return BACnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE;
        if (t == typeof(BACnetBitString))
            return BACnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING;
        if (t == typeof(BACnetObjectId))
            return BACnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
        if (t == typeof(BACnetError))
            return BACnetApplicationTags.BACNET_APPLICATION_TAG_ERROR;
        if (t == typeof(BACnetDeviceObjectPropertyReference))
            return BACnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_PROPERTY_REFERENCE;
        if (t.IsEnum)
            return BACnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED;

        return BACnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_ENCODED;
    }

    public T As<T>()
    {
        if (typeof(T) == typeof(DateTime))
        {
            switch (Tag)
            {
                case BACnetApplicationTags.BACNET_APPLICATION_TAG_DATE:
                case BACnetApplicationTags.BACNET_APPLICATION_TAG_DATETIME:
                case BACnetApplicationTags.BACNET_APPLICATION_TAG_TIME:
                case BACnetApplicationTags.BACNET_APPLICATION_TAG_TIMESTAMP:
                    return (T)Value;
            }
        }

        if (typeof(T) == typeof(TimeSpan) && Tag == BACnetApplicationTags.BACNET_APPLICATION_TAG_TIME)
            return (T)(dynamic)((DateTime)Value).TimeOfDay;

        if (typeof(T) != typeof(object) && TagFromType(typeof(T)) != Tag)
            throw new ArgumentException($"Value with tag {Tag} can't be converted to {typeof(T).Name}");

        // ReSharper disable once RedundantCast
        // This is needed for casting to enums
        return (T)(dynamic)Value;
    }

    public override string ToString()
    {
        if (Value == null)
            return string.Empty;

        if (Value.GetType() != typeof(byte[]))
            return Value.ToString();

        var tmp = (byte[])Value;
        return tmp.Aggregate(string.Empty, (current, b) =>
            current + b.ToString("X2"));
    }
}
