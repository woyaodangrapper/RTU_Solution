namespace System.Net.BACnet;

public struct BACnetDateRange : ASN1.IEncode, ASN1.IDecode
{
    public BACnetDate startDate;
    public BACnetDate endDate;

    public BACnetDateRange(BACnetDate start, BACnetDate end)
    {
        startDate = start;
        endDate = end;
    }

    public void Encode(EncodeBuffer buffer)
    {
        ASN1.encode_tag(buffer, (byte)BACnetApplicationTags.BACNET_APPLICATION_TAG_DATE, false, 4);
        startDate.Encode(buffer);
        ASN1.encode_tag(buffer, (byte)BACnetApplicationTags.BACNET_APPLICATION_TAG_DATE, false, 4);
        endDate.Encode(buffer);
    }

    public int Decode(byte[] buffer, int offset, uint count)
    {
        var len = 1; // opening tag
        len += startDate.Decode(buffer, offset + len, count);
        len++;
        len += endDate.Decode(buffer, offset + len, count);
        return len;
    }

    public bool IsAFittingDate(DateTime date)
    {
        date = new DateTime(date.Year, date.Month, date.Day);
        return date >= startDate.toDateTime() && date <= endDate.toDateTime();
    }

    public override string ToString()
    {
        string ret;

        if (startDate.day != 255)
            ret = "From " + startDate;
        else
            ret = "From **/**/**";

        if (endDate.day != 255)
            ret = ret + " to " + endDate;
        else
            ret += " to **/**/**";

        return ret;
    }
};
