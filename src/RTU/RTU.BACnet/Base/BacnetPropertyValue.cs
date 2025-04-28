namespace System.Net.BACnet;

public struct BACnetPropertyValue
{
    public BACnetPropertyReference property;
    public IList<BACnetValue> value;
    public byte priority;

    public override string ToString()
    {
        return property.ToString();
    }
}
