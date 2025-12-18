namespace System.Net.BACnet;

[Flags]
public enum BACnetStatusFlags
{
    STATUS_FLAG_IN_ALARM = 1,
    STATUS_FLAG_FAULT = 2,
    STATUS_FLAG_OVERRIDDEN = 4,
    STATUS_FLAG_OUT_OF_SERVICE = 8
}
