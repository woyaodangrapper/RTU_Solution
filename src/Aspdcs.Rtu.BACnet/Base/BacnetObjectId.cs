namespace System.Net.BACnet;

[Serializable]
public struct BACnetObjectId : IComparable<BACnetObjectId>
{
    public BACnetObjectTypes type;
    public uint instance;

    public BACnetObjectTypes Type
    {
        get => type;
        set => type = value;
    }

    public uint Instance
    {
        get => instance;
        set => instance = value;
    }

    public BACnetObjectId(BACnetObjectTypes type, uint instance)
    {
        this.type = type;
        this.instance = instance;
    }

    public override string ToString()
    {
        return $"{Type}:{Instance}";
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj != null && obj.ToString().Equals(ToString());
    }

    public int CompareTo(BACnetObjectId other)
    {
        if (Type == other.Type)
            return Instance.CompareTo(other.Instance);

        if (Type == BACnetObjectTypes.OBJECT_DEVICE)
            return -1;

        if (other.Type == BACnetObjectTypes.OBJECT_DEVICE)
            return 1;

        // cast to int for comparison otherwise unpredictable behaviour with outbound enum (proprietary type)
        return ((int)Type).CompareTo((int)other.Type);
    }

    public static bool operator ==(BACnetObjectId a, BACnetObjectId b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(BACnetObjectId a, BACnetObjectId b)
    {
        return !(a == b);
    }

    public static BACnetObjectId Parse(string value)
    {
        var pattern = new Regex($"(?<{nameof(Type)}>.+):(?<{nameof(Instance)}>.+)");

        if (string.IsNullOrEmpty(value) || !pattern.IsMatch(value))
            return new BACnetObjectId();

        var objectType = (BACnetObjectTypes)Enum.Parse(typeof(BACnetObjectTypes),
            pattern.Match(value).Groups[nameof(Type)].Value);

        var objectInstance = uint.Parse(pattern.Match(value).Groups[nameof(Instance)].Value);

        return new BACnetObjectId(objectType, objectInstance);
    }

};
