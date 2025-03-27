namespace System.Net.BACnet;

public struct BACnetGenericTime
{
    public BACnetTimestampTags Tag;
    public DateTime Time;
    public ushort Sequence;

    public BACnetGenericTime(DateTime time, BACnetTimestampTags tag, ushort sequence = 0)
    {
        Time = time;
        Tag = tag;
        Sequence = sequence;
    }

    public override string ToString()
    {
        return $"{Time}";
    }
}
