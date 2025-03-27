namespace System.Net.BACnet;

public struct BACnetError
{
    public BACnetErrorClasses error_class;
    public BACnetErrorCodes error_code;

    public BACnetError(BACnetErrorClasses errorClass, BACnetErrorCodes errorCode)
    {
        error_class = errorClass;
        error_code = errorCode;
    }
    public BACnetError(uint errorClass, uint errorCode)
    {
        error_class = (BACnetErrorClasses)errorClass;
        error_code = (BACnetErrorCodes)errorCode;
    }
    public override string ToString()
    {
        return $"{error_class}: {error_code}";
    }
}
