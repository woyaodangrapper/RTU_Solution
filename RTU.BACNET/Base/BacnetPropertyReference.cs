namespace System.Net.BACnet;

public struct BACnetPropertyReference
{
    public uint propertyIdentifier;
    public uint propertyArrayIndex;        /* optional */

    public BACnetPropertyReference(uint id, uint arrayIndex)
    {
        propertyIdentifier = id;
        propertyArrayIndex = arrayIndex;
    }

    public BACnetPropertyIds GetPropertyId()
    {
        return (BACnetPropertyIds)propertyIdentifier;
    }

    public override string ToString()
    {
        return $"{GetPropertyId()}";
    }
}
