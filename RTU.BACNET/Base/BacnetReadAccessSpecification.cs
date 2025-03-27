namespace System.Net.BACnet;

public struct BACnetReadAccessSpecification
{
    public BACnetObjectId objectIdentifier;
    public IList<BACnetPropertyReference> propertyReferences;

    public BACnetReadAccessSpecification(BACnetObjectId objectIdentifier, IList<BACnetPropertyReference> propertyReferences)
    {
        this.objectIdentifier = objectIdentifier;
        this.propertyReferences = propertyReferences;
    }

    public static object Parse(string value)
    {
        var ret = new BACnetReadAccessSpecification();
        if (string.IsNullOrEmpty(value)) return ret;
        var tmp = value.Split(':');
        if (tmp.Length < 2) return ret;
        ret.objectIdentifier.type = (BACnetObjectTypes)Enum.Parse(typeof(BACnetObjectTypes), tmp[0]);
        ret.objectIdentifier.instance = uint.Parse(tmp[1]);
        var refs = new List<BACnetPropertyReference>();
        for (var i = 2; i < tmp.Length; i++)
        {
            refs.Add(new BACnetPropertyReference
            {
                propertyArrayIndex = ASN1.BACNET_ARRAY_ALL,
                propertyIdentifier = (uint)(BACnetPropertyIds)Enum.Parse(typeof(BACnetPropertyIds), tmp[i])
            });
        }
        ret.propertyReferences = refs;
        return ret;
    }

    public override string ToString()
    {
        return propertyReferences.Aggregate(objectIdentifier.ToString(), (current, r) =>
            $"{current}:{(BACnetPropertyIds)r.propertyIdentifier}");
    }
}
