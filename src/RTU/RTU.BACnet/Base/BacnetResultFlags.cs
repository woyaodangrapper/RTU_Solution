namespace System.Net.BACnet;

[Flags]
public enum BACnetResultFlags
{
    NONE = 0,
    FIRST_ITEM = 1,
    LAST_ITEM = 2,
    MORE_ITEMS = 4,
}
