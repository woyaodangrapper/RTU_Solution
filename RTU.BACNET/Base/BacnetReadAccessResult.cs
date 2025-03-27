namespace System.Net.BACnet;

public struct BACnetReadAccessResult
{
    public BACnetObjectId objectIdentifier;
    public IList<BACnetPropertyValue> values;

    public BACnetReadAccessResult(BACnetObjectId objectIdentifier, IList<BACnetPropertyValue> values)
    {
        this.objectIdentifier = objectIdentifier;
        this.values = values;
    }
}
