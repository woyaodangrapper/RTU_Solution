namespace System.Net.BACnet;

public struct BACnetLogRecord
{
    public DateTime timestamp;

    /* logDatum: CHOICE { */
    public BACnetTrendLogValueType type;
    //private BACnetBitString log_status;
    //private bool boolean_value;
    //private float real_value;
    //private uint enum_value;
    //private uint unsigned_value;
    //private int signed_value;
    //private BACnetBitString bitstring_value;
    //private bool null_value;
    //private BACnetError failure;
    //private float time_change;
    private object any_value;
    /* } */

    public BACnetBitString statusFlags;

    public BACnetLogRecord(BACnetTrendLogValueType type, object value, DateTime stamp, uint status)
    {
        this.type = type;
        timestamp = stamp;
        statusFlags = BACnetBitString.ConvertFromInt(status);
        any_value = null;
        Value = value;
    }

    public object Value
    {
        get
        {
            switch (type)
            {
                case BACnetTrendLogValueType.TL_TYPE_ANY:
                    return any_value;
                case BACnetTrendLogValueType.TL_TYPE_BITS:
                    return (BACnetBitString)Convert.ChangeType(any_value, typeof(BACnetBitString));
                case BACnetTrendLogValueType.TL_TYPE_BOOL:
                    return (bool)Convert.ChangeType(any_value, typeof(bool));
                case BACnetTrendLogValueType.TL_TYPE_DELTA:
                    return (float)Convert.ChangeType(any_value, typeof(float));
                case BACnetTrendLogValueType.TL_TYPE_ENUM:
                    return (uint)Convert.ChangeType(any_value, typeof(uint));
                case BACnetTrendLogValueType.TL_TYPE_ERROR:
                    if (any_value != null)
                        return (BACnetError)Convert.ChangeType(any_value, typeof(BACnetError));
                    else
                        return new BACnetError(BACnetErrorClasses.ERROR_CLASS_DEVICE, BACnetErrorCodes.ERROR_CODE_ABORT_OTHER);
                case BACnetTrendLogValueType.TL_TYPE_NULL:
                    return null;
                case BACnetTrendLogValueType.TL_TYPE_REAL:
                    return (float)Convert.ChangeType(any_value, typeof(float));
                case BACnetTrendLogValueType.TL_TYPE_SIGN:
                    return (int)Convert.ChangeType(any_value, typeof(int));
                case BACnetTrendLogValueType.TL_TYPE_STATUS:
                    return (BACnetBitString)Convert.ChangeType(any_value, typeof(BACnetBitString));
                case BACnetTrendLogValueType.TL_TYPE_UNSIGN:
                    return (uint)Convert.ChangeType(any_value, typeof(uint));
                default:
                    throw new NotSupportedException();
            }
        }
        set
        {
            switch (type)
            {
                case BACnetTrendLogValueType.TL_TYPE_ANY:
                    any_value = value;
                    break;
                case BACnetTrendLogValueType.TL_TYPE_BITS:
                    if (value == null) value = new BACnetBitString();
                    if (value.GetType() != typeof(BACnetBitString))
                        value = BACnetBitString.ConvertFromInt((uint)Convert.ChangeType(value, typeof(uint)));
                    any_value = (BACnetBitString)value;
                    break;
                case BACnetTrendLogValueType.TL_TYPE_BOOL:
                    if (value == null) value = false;
                    if (value.GetType() != typeof(bool))
                        value = (bool)Convert.ChangeType(value, typeof(bool));
                    any_value = (bool)value;
                    break;
                case BACnetTrendLogValueType.TL_TYPE_DELTA:
                    if (value == null) value = (float)0;
                    if (value.GetType() != typeof(float))
                        value = (float)Convert.ChangeType(value, typeof(float));
                    any_value = (float)value;
                    break;
                case BACnetTrendLogValueType.TL_TYPE_ENUM:
                    if (value == null) value = (uint)0;
                    if (value.GetType() != typeof(uint))
                        value = (uint)Convert.ChangeType(value, typeof(uint));
                    any_value = (uint)value;
                    break;
                case BACnetTrendLogValueType.TL_TYPE_ERROR:
                    if (value == null) value = new BACnetError();
                    if (value.GetType() != typeof(BACnetError))
                        throw new ArgumentException();
                    any_value = (BACnetError)value;
                    break;
                case BACnetTrendLogValueType.TL_TYPE_NULL:
                    if (value != null) throw new ArgumentException();
                    any_value = value;
                    break;
                case BACnetTrendLogValueType.TL_TYPE_REAL:
                    if (value == null) value = (float)0;
                    if (value.GetType() != typeof(float))
                        value = (float)Convert.ChangeType(value, typeof(float));
                    any_value = (float)value;
                    break;
                case BACnetTrendLogValueType.TL_TYPE_SIGN:
                    if (value == null) value = 0;
                    if (value.GetType() != typeof(int))
                        value = (int)Convert.ChangeType(value, typeof(int));
                    any_value = (int)value;
                    break;
                case BACnetTrendLogValueType.TL_TYPE_STATUS:
                    if (value == null) value = new BACnetBitString();
                    if (value.GetType() != typeof(BACnetBitString))
                        value = BACnetBitString.ConvertFromInt((uint)Convert.ChangeType(value, typeof(uint)));
                    any_value = (BACnetBitString)value;
                    break;
                case BACnetTrendLogValueType.TL_TYPE_UNSIGN:
                    if (value == null) value = (uint)0;
                    if (value.GetType() != typeof(uint))
                        value = (uint)Convert.ChangeType(value, typeof(uint));
                    any_value = (uint)value;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public T GetValue<T>()
    {
        return (T)Convert.ChangeType(Value, typeof(T));
    }
}
